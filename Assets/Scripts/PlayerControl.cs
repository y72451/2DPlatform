using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerControl;

public class PlayerControl : MonoBehaviour
{
    public PlayerStatus plrStatus;
    private int direction;
    InputManager InputMgr;
    PrefabManager PrefabMgr;

    public Animator PlrAnim;

    //角色方向控制
    public enum Facing
    {
        Left = 1,
        Right = -1
    }

    public Facing currentFacing = Facing.Left;
    private float rushTime = 0.5f;

    private GroundInfo frontGroundInfo;
    private GroundInfo rearGroundInfo;
    private GroundInfo centerGroundInfo;

    public MovementState currentMoveState = MovementState.Idle;
    public CombatState currentCombatState = CombatState.None;

    //跳躍控制
    public JumpParameter jumpParameter;
    private float jumpTimer = 0f;
    private float currentYSpeed = 0f;

    //private Rigidbody2D rb;
    public float slopeCheckDistance = 15.0f;

    //角色相關物件
    public Transform ShootPos;
    public Transform FrontLegPos;
    public Transform RearLegPos;
    public Transform CenterGroundPos;
    public Transform DustPos;


    //暫時變數
    public bool isAttacking = false;
    private bool isShooting = false;

    //攻擊變數
    private float attackTimer = 0f;             //實際攻擊動作執行時間
    private float attackDuration = 0.2f;        //攻擊動畫時間

    private float comboTimer = 0f;           //連擊計時器
    private float comboWindowDelay = 2.5f;   //可以繼續派成連擊的窗口時間
    private int comboCount = 0;              //斬擊連擊次數
    
    public bool isComboAttack = false;           //是否需要在下一次進行連擊

    private bool spcialRush = false;

    private float ShootTime = 0.15f;

    //Debug項目
    public PlayerDebugOption plrDebugOption;

    // Start is called before the first frame update
    void Start()
    {
        InputMgr = GameObject.Find("InputManager").GetComponent<InputManager>();
        PrefabMgr = GameObject.Find("PrefabManager").GetComponent<PrefabManager>();
        //rb = GetComponent<Rigidbody2D>();
        InputMgr.OnJumpEvent += HandleJump;
        InputMgr.OnRushEvent += HandleRush;
        InputMgr.OnSlashEvent += HandleSlash;
        InputMgr.OnShootStartEvent += HandleShootStart;
        InputMgr.OnShootEndEvent += HandleShootEnd;
    }

    private void OnDestroy()
    {
        // 養成好習慣：物件銷毀時取消訂閱
        if (InputMgr != null)
        {
            InputMgr.OnJumpEvent -= HandleJump;
            InputMgr.OnSlashEvent -= HandleSlash;
            InputMgr.OnShootStartEvent -= HandleShootStart;
            InputMgr.OnShootEndEvent -= HandleShootEnd;
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*Review
        避免在Update中時刻處理核心邏輯，嘗試改用事件驅動
        設置進入點跟離開點
         */
        #region move

        //
        
        frontGroundInfo = GroundDetector.DetectGround(FrontLegPos, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));
        rearGroundInfo = GroundDetector.DetectGround(RearLegPos, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));
        centerGroundInfo = GroundDetector.DetectGround(CenterGroundPos, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));

        CheckMovementTransitions();
        ExecuteMovementState();

        /*
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            // 逐漸減速（上升）
            float jumpProgress = jumpTimer / jumpParameter.jumpDuration;
            currentYSpeed = Mathf.Lerp(jumpParameter.jumpHeight / jumpParameter.jumpDuration, 0, jumpProgress);
            transform.Translate(Vector2.up * currentYSpeed * Time.deltaTime);
            if (jumpProgress >= 1f)
            {
                isJumping = false;
                isFalling = true;
            }
        }
        else if (isFalling)
        {
            transform.Translate(Vector2.down * jumpParameter.fallSpeed * Time.deltaTime);
            if (frontGroundInfo.isGrounded)
            {
                isFalling = false;
                currentYSpeed = 0f;
                jumpTimer = 0f;
                ResetHeight();
            }
        }
        */
        #endregion move

        #region attack
        //Note 目前同一12f(0.2s)

        if (isAttacking && comboCount > 0)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer >= comboWindowDelay)
            {
                // 寬限期真的過了，徹底忘記連擊
                comboCount = 0;
                isAttacking = false;
                Debug.Log("Combo Memory Lost");
            }
        }


        /*summary
         攻擊之後會開始計時動畫時間，時間到之後回歸原本的動畫
         
         */
        
        
        if (isAttacking)
        {
            attackTimer = attackTimer + Time.deltaTime;
            if (attackTimer >= attackDuration)
            {
                Debug.Log("AttackTimer:" + attackTimer);
                isAttacking= false;
                comboTimer = 0f; // 動畫結束的瞬間，啟動連擊記憶倒數

                // 檢查剛才有沒有預先輸入 (Input Buffering)
                if (isComboAttack && comboCount > 0 && comboCount < 3)
                {
                    // 有預先輸入，處理連擊
                    ExecuteNextSlash();
                }
                else
                {
                    if (currentMoveState == MovementState.Rush)
                    {
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Rush);
                    }
                    else if (IsOnAir())
                    {
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Jump);
                        Debug.Log("Back to jump position");
                        //Debug.Break();
                    }
                    else
                    {
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
                    }

                    Debug.Log("End of AttackAnim");
                    //Debug.Break();
                }

            }
        }

        //持續射擊
        if (isShooting)
        {
            if (isShooting && currentMoveState == MovementState.Rush == false && (IsOnAir()) == false)
            {
                OnShootingPress();
            }
        }

        #endregion attack

    }
    //跳躍
    void HandleJump()
    {
        
        currentMoveState = MovementState.Jump;
        jumpTimer = 0f;
        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Jump);
    }

    //衝刺
    void HandleRush()
    {
        if (currentMoveState != MovementState.Rush)
        {
            currentMoveState = MovementState.Rush;
            rushTime = 0.5f;
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.Rush);
            PrefabMgr.setEff(1, DustPos, currentFacing == Facing.Right);

            //取消攻擊
            if (isShooting)
            {
                isShooting = false;
                ShootTime = 0;
            }
            if (isAttacking)
            {
                isAttacking = false;
            }
        }
    }

    void HandleSlash()
    {
        //連擊的判斷處理
        if( isAttacking )
        {
            if(comboCount > 0 && comboCount <3)
            {
                isComboAttack = true;
            }
            return;
        }

        //第一擊的處理流程

        isAttacking = true;
        attackTimer = 0f;
        comboTimer = 0f;
        isComboAttack = false;


        // 判斷要發動哪一種攻擊，並設定對應的狀態
        if (IsOnAir())
        {
            PlrAnim.SetInteger("ActionCode", 14); // 空中砍
            comboCount = -1; // -1 代表「這是特殊攻擊，沒有下一段」
                             // Debug.Log("Air Slash");
            //Debug.Break();
        }
        else if (currentMoveState == MovementState.Rush)
        {
            PlrAnim.SetInteger("ActionCode", 12); // 衝刺砍
            comboCount = -1;
            // Debug.Log("Rush Slash");
        }
        else if (direction == 4 || direction == 6)
        {
            PlrAnim.SetInteger("ActionCode", 10); // 跑砍
            comboCount = -1;
            // Debug.Log("Run Slash");
        }
        else
        {
            // 站立狀態 (direction == 5)
            PlrAnim.SetInteger("ActionCode", 6); // 平砍第一段
            comboCount = 1; // 記錄：已經打完第1段，準備好接第2段
                            // Debug.Log("First Slash");
        }

    }

    void HandleShootStart()
    {
        if (!isAttacking)
        {
            isAttacking = true;
        }
        if (!isShooting)
        {
            isShooting = true;
        }
        //站立狀態
        if (direction == 5 && currentMoveState != MovementState.Rush && !IsOnAir())
        {
            PlrAnim.SetInteger("ActionCode", 9);
        }
        else if (!IsOnAir() && (direction == 4 || direction == 6))
        {
            PlrAnim.SetInteger("ActionCode", 11);
        }
        else if (currentMoveState == MovementState.Rush)
        {
            PlrAnim.SetInteger("ActionCode", 13);
        }
        else if (IsOnAir())
        {
            PlrAnim.SetInteger("ActionCode", 15);
            
        }
        ShootBullet();
    }

    void HandleShootEnd()
    {
        isShooting = false;
        ShootTime = 0f; // 重置計時器

        // 注意：你原本的代碼在這裡強制把 isAttacking 設為 false。
        // 這可能會導致：如果玩家同時在揮劍又放開射擊鍵，揮劍狀態會被硬生生中斷。
        // 你可能需要更精細的狀態管理（例如區分 isMeleeAttacking 和 isShooting），
        // 但為了符合你原本的邏輯，這裡先保留：
        isAttacking = false;
    }

    void Flip(Facing facing)
    {
        transform.rotation = Quaternion.Euler(0f, facing == Facing.Right ? 180f : 0f, 0f);
    }

    private void CheckMovementTransitions()
    {
        // 如果正在衝刺，通常不允許其他移動狀態中斷 (直到衝刺計時結束)
        if (currentMoveState == MovementState.Rush) return;

        // 1. 取得最新輸入
        Vector2 inputDir = InputMgr.GetMovementVector();

        // 2. 處理衝刺觸發
        if (InputMgr.IsRushPressed() && !IsOnAir())
        {
            currentMoveState = MovementState.Rush;
            rushTime = 0.5f; // 重置計時器
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.Rush);
            return;
        }

        // ==========================================
        // 3. 物理狀態與邏輯狀態的同步 (踩空與落地)
        // ==========================================

        // A. 踩空判斷：如果物理上沒踩到地板，且邏輯上「不是正在向上跳」
        if (!centerGroundInfo.isGrounded && currentMoveState != MovementState.Jump)
        {
            // 強制進入下墜狀態 (這完美解決了開場在半空，或是走路摔下懸崖的問題)
            if (currentMoveState != MovementState.Fall)
            {
                currentMoveState = MovementState.Fall;

                // 如果沒在攻擊，切換成落下/跳躍的動畫
                if (currentCombatState == CombatState.None)
                {
                    PlrAnim.SetInteger("ActionCode", (int)AnimCode.Jump); // 或是如果你有專屬的 Fall 動畫也可以填入
                }
            }
        }
        // B. 落地判斷：如果物理上踩到地板，且邏輯上「正在下墜」
        else if (centerGroundInfo.isGrounded && currentMoveState == MovementState.Fall)
        {
            currentMoveState = MovementState.Idle;
            ResetHeight(); // 物理對齊地板

            // 如果沒在攻擊，切回待機動畫
            if (currentCombatState == CombatState.None)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
            }
        }

        // 4. 處理滯空與落地 (結合你之前修好的中心點射線)
        if (IsOnAir())
        {
            if (centerGroundInfo.isGrounded && currentMoveState == MovementState.Fall)
            {
                // 落地了！
                currentMoveState = MovementState.Idle;
                ResetHeight(); // 物理對齊

                // 只有沒在攻擊時才切換落地動畫
                if (currentCombatState == CombatState.None)
                {
                    PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
                }
            }
            return; // 在空中就不往下判斷跑步了
        }

        // 5. 處理平地移動與待機
        if (inputDir.x != 0)
        {
            currentMoveState = MovementState.Run;
            // 只有沒在攻擊時才切換跑步動畫
            if (currentCombatState == CombatState.None)
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.Run);
        }
        else
        {
            currentMoveState = MovementState.Idle;
            if (currentCombatState == CombatState.None)
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
        }
    }

    private void ExecuteMovementState()
    {

        float moveSpeed = plrStatus.moveSpeed;
        // 處理面向 (Flip)
        Vector2 inputDir = InputMgr.GetMovementVector();
        if (inputDir.x != 0 && currentMoveState != MovementState.Rush)
        {
            Facing newFacing = inputDir.x > 0 ? Facing.Right : Facing.Left;
            if (newFacing != currentFacing)
            {
                currentFacing = newFacing;
                Flip(currentFacing);
            }
        }

        switch (currentMoveState)
        {
            case MovementState.Idle:
                // 待機不產生額外位移
                break;

            case MovementState.Run:
                // 塞入你之前寫好的完美斜坡投影邏輯
                Vector2 horizontalMove = new Vector2(inputDir.x, 0f);
                Vector2 moveDir = horizontalMove.normalized;

                HorizontalMovement(inputDir, 1f,moveSpeed);
                break;

            case MovementState.Jump:

                HorizontalMovement(inputDir, 0.8f,moveSpeed);
                // 塞入跳躍上升邏輯
                jumpTimer += Time.deltaTime;
                float jumpProgress = jumpTimer / jumpParameter.jumpDuration;
                currentYSpeed = Mathf.Lerp(jumpParameter.jumpHeight / jumpParameter.jumpDuration, 0, jumpProgress);
                transform.Translate(Vector2.up * currentYSpeed * Time.deltaTime);

                if (jumpProgress >= 1f)
                {
                    currentMoveState = MovementState.Fall; // 狀態機自動切換到下墜
                }
                break;

            case MovementState.Fall:

                HorizontalMovement(inputDir, 0.8f,moveSpeed);
                // 塞入重力下墜邏輯
                transform.Translate(Vector2.down * jumpParameter.fallSpeed * Time.deltaTime);
                break;

            case MovementState.Rush:
                // 塞入衝刺邏輯
                transform.Translate(new Vector2((int)currentFacing * -1, 0) * Time.deltaTime * moveSpeed * 3, Space.World);
                rushTime -= Time.deltaTime;
                if (rushTime <= 0)
                {
                    currentMoveState = MovementState.Idle; // 衝刺結束回歸待機
                    if (currentCombatState == CombatState.None)
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
                }
                break;
        }
    }

    // 傳入 inputDir，並預留一個 speedMultiplier (速度倍率)，預設為 1
    private void HorizontalMovement(Vector2 inputDir, float speedMultiplier = 1f, float moveSpeed = 1f)
    {
        // 如果沒有輸入，就不做水平位移
        if (inputDir.x == 0) return;

        Vector2 horizontalMove = new Vector2(inputDir.x, 0f);
        Vector2 moveDir = horizontalMove.normalized;

        // 斜坡投影：只有在踩著地板的時候才會觸發 (所以空中呼叫時，這段會自動被跳過，非常安全)
        if (centerGroundInfo.isGrounded && centerGroundInfo.slopeAngle > 0 && centerGroundInfo.slopeAngle <= 45f)
        {
            moveDir = Vector3.ProjectOnPlane(horizontalMove, centerGroundInfo.slopeNormal).normalized;
        }

        // 實際移動 (套用速度倍率)
        transform.Translate(moveSpeed * speedMultiplier * Time.deltaTime * moveDir, Space.World);
    }

    void ExecuteNextSlash()
    {

        isAttacking = true;
        attackTimer = 0f;
        isComboAttack = false;

        // 如果目前 comboCount 是 1，播 ActionCode 7 (第二刀)
        // 如果目前 comboCount 是 2，播 ActionCode 8 (第三刀)
        PlrAnim.SetInteger("ActionCode", 6 + comboCount);

        comboCount++; // 打完後段數繼續往上加

    }

    void ShootBullet()
    {
        PrefabMgr.setEff(2, ShootPos, currentFacing == Facing.Right);
    }

    void OnShootingPress(int ShotWeapon = 0)
    {
        ShootTime += Time.deltaTime;
        if (ShootTime > 0.15f)
        {
            ShootTime = 0;
            ShootBullet();
        }
        if (direction == 5 && currentMoveState != MovementState.Rush && !IsOnAir())
        {
            PlrAnim.SetInteger("ActionCode", 9);
        }
        else if (!IsOnAir() && (direction == 4 || direction == 6))
        {
            PlrAnim.SetInteger("ActionCode", 11);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Debug.Log("OnTriggerEnter2D");
        if (col.gameObject.CompareTag("Terrain_Ground") == true)
        {
            if (isAttacking)
            {
                isAttacking = false;
            }
            if (isShooting)
            {
                isShooting = false;
                ShootTime = 0;
            }
            if (currentMoveState == MovementState.Rush)
            {
                PlrAnim.SetInteger("ActionCode", 3);
            }
            else if (currentMoveState == MovementState.Rush)
            {
                PlrAnim.SetInteger("ActionCode", 1);
            }
            else
            {
                PlrAnim.SetInteger("ActionCode", 0);
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        Debug.Log("OnTriggerExit2D");
        if (col.gameObject.CompareTag("Terrain_Ground") == true && currentMoveState != MovementState.Jump)
        {
            currentMoveState = MovementState.Fall;
            PlrAnim.SetInteger("ActionCode", 2);
        }
    }

    bool IsOnAir()
    {
        return currentMoveState == MovementState.Jump || currentMoveState == MovementState.Fall;
    }


    void ResetHeight()
    {
        float characterHeightOffset = 0f;

        // 射線往下偵測，確認地面位置
        RaycastHit2D hit = Physics2D.Raycast(FrontLegPos.position, Vector2.down, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));
        if (hit.collider != null)
        {
            // 將角色的 y 座標設定為：地面位置 + 腳底偏移
            float correctY = hit.point.y + characterHeightOffset;
            transform.position = new Vector3(transform.position.x, correctY, transform.position.z);
            Debug.Log("Reset Height");
        }
    }

}
