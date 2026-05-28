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
        InputMgr.OnShootStartEvent += HandleShootStart;
        InputMgr.OnShootEndEvent += HandleShootEnd;
    }

    private void OnDestroy()
    {
        // 養成好習慣：物件銷毀時取消訂閱
        if (InputMgr != null)
        {
            InputMgr.OnJumpEvent -= HandleJump;
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

        #endregion move

        #region attack
        CheckCombatTransitions();
        ExecuteCombatState();
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
            if (currentCombatState != CombatState.None)
            {
                CancelAttack();
            }
        }
    }


    void HandleShootStart()
    {
        currentCombatState = CombatState.Shoot;

        // 立即擊發第一發子彈，消除輸入延遲
        ShootBullet();

        // 重置連發計時器，這樣 ExecuteCombatState 才會乖乖等 0.15 秒後再開第二槍
        ShootTime = 0f;

    }

    void HandleShootEnd()
    {
        if (currentCombatState == CombatState.Shoot)
            CancelAttack();
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
                CancelAttack();                
            }
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
        }

        // 4. 處理滯空與落地
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
                Vector2 horizontalMove = new Vector2(inputDir.x, 0f);
                Vector2 moveDir = horizontalMove.normalized;

                HorizontalMovement(inputDir, 1f,moveSpeed);
                break;

            case MovementState.Jump:

                HorizontalMovement(inputDir, 0.8f,moveSpeed);
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
                transform.Translate(Vector2.down * jumpParameter.fallSpeed * Time.deltaTime);
                break;

            case MovementState.Rush:
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


    private void CheckCombatTransitions()
    {
        // ----------------------------------------
        // 處理近戰斬擊 (Slash)
        // ----------------------------------------
        if (InputMgr.IsSlashPressed())
        {
            // 情況 A：目前不在攻擊狀態 (發動新一擊，或是延遲派生的下一擊)
            if (currentCombatState == CombatState.None)
            {
                // ==========================================
                // 【關鍵修復】將「特殊攻擊」與「平地連擊」分流
                // ==========================================

                // 如果是空中、跑動、衝刺，無視段數，強制發動特殊攻擊
                if (IsOnAir() || currentMoveState == MovementState.Run || currentMoveState == MovementState.Rush)
                {
                    // 先傳 Slash1 進去當作啟動器，StartSlashState 裡面會自動把它替換成正確的動畫
                    StartSlashState(CombatState.Slash1);
                }
                // 正常站在地上的連擊邏輯
                else
                {
                    // 加上 comboCount == -1 的防呆，避免上一刀是跑砍/跳斬，落地後卡住不能攻擊
                    if (comboCount == 0 || comboCount >= 3 || comboCount == -1)
                    {
                        comboCount = 0;
                        StartSlashState(CombatState.Slash1);
                    }
                    else if (comboCount == 1) StartSlashState(CombatState.Slash2);
                    else if (comboCount == 2) StartSlashState(CombatState.Slash3);
                }
            }
            // 情況 B：目前正在攻擊中 (玩家狂按按鍵 -> Input Buffering 預先輸入)
            else
            {
                if (comboCount < 3 && comboCount != -1)
                {
                    isComboAttack = true; // 記住玩家已經按了下一刀
                }
            }
        }
        if (InputMgr.IsShootPressed()) 
        { 
            if (currentCombatState == CombatState.None)
            {
                currentCombatState = CombatState.Shoot;
            }
        }
    }

    // 將設定狀態、動畫、計時器歸零的動作封裝起來，保持代碼乾淨
    private void StartSlashState(CombatState slashState)
    {
        currentCombatState = slashState;
        attackTimer = 0f;
        isComboAttack = false;

        // 根據目前的移動狀態，決定要播什麼動畫
        if (currentMoveState == MovementState.Jump || currentMoveState == MovementState.Fall)
        {
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.JumpSlash);
            attackDuration = 0.25f; // 空中斬可能需要停頓久一點
                                    // comboCount = -1; // 如果空中斬不允許連擊，可以這樣設定
        }
        else if (currentMoveState == MovementState.Run)
        {
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.RunSlash);
            attackDuration = 0.2f;
        }
        else if (currentMoveState == MovementState.Rush)
        {
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.RushSlash);
            attackDuration = 0.2f;
        }
        else
        {
            // 站立平砍
            if (slashState == CombatState.Slash1) PlrAnim.SetInteger("ActionCode", (int)AnimCode.Slash1);
            else if (slashState == CombatState.Slash2) PlrAnim.SetInteger("ActionCode", (int)AnimCode.Slash2);
            else if (slashState == CombatState.Slash3) PlrAnim.SetInteger("ActionCode", (int)AnimCode.Slash3);

            attackDuration = 0.2f; // 平砍的動畫時間
            comboCount++; // 打出後，段數加 1，準備給下一刀使用
        }
    }

    private void ExecuteCombatState()
    {
        // ----------------------------------------
        // 1. 處理「連擊記憶」的消退 (只有在沒攻擊時才倒數)
        // ----------------------------------------
        if (currentCombatState == CombatState.None && comboCount > 0)
        {
            comboTimer += Time.deltaTime;

            float comboWindow = 0.4f; // 允許延遲派生的寬限時間
            if (comboTimer >= comboWindow)
            {
                comboCount = 0; // 寬限期過，徹底忘記連擊段數
                                // 由於目前是 None 狀態，角色自然會處於 Idle，不需要特別切換動畫
            }
        }

        // ----------------------------------------
        // 2. 處理「攻擊硬直與收招」
        // ----------------------------------------
        if (currentCombatState == CombatState.Slash1 ||
            currentCombatState == CombatState.Slash2 ||
            currentCombatState == CombatState.Slash3)
        {
            attackTimer += Time.deltaTime;

            // 動畫播完了！
            if (attackTimer >= attackDuration)
            {
                // 解除攻擊狀態
                currentCombatState = CombatState.None;
                attackTimer = 0f;
                comboTimer = 0f; // 重置記憶計時器，開始倒數延遲派生！

                // 檢查是否要無縫接軌下一刀
                if (isComboAttack && comboCount > 0 && comboCount < 3)
                {
                    // 如果是 Slash1 結束，就接 Slash2
                    if (comboCount == 1)
                    {
                        StartSlashState(CombatState.Slash2);
                    }
                    else if (comboCount == 2)
                    {
                        StartSlashState(CombatState.Slash3);
                    }
                }
                else
                {
                    // 沒有預先輸入，準備收招 (恢復移動動畫)
                    // 這裡我們把動畫的控制權「交還」給移動系統
                    if (currentMoveState == MovementState.Idle)
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
                    else if (currentMoveState == MovementState.Run)
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Run);
                    else if (IsOnAir())
                        PlrAnim.SetInteger("ActionCode", (int)AnimCode.Jump); // 或是 Fall
                }
            }
        }
        // ----------------------------------------
        // 3. 處理射擊
        // ----------------------------------------
        if(currentCombatState == CombatState.Shoot)
        {
            ShootTime += Time.deltaTime;
            if (ShootTime > 0.15f && (currentMoveState != MovementState.Jump||currentMoveState != MovementState.Rush ))
            {
                ShootTime = 0;
                ShootBullet();
            }
            if (currentMoveState == MovementState.Idle)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.StandShoot);
            }
            else if (currentMoveState == MovementState.Run)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.RunShoot);
            }
            else if (currentMoveState == MovementState.Jump)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.JumpShoot);
            }
            else if (currentMoveState == MovementState.Rush)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.RushShoot);
            }
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

    void ShootBullet()
    {
        PrefabMgr.setEff(2, ShootPos, currentFacing == Facing.Right);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Debug.Log("OnTriggerEnter2D");
        if (col.gameObject.CompareTag("Terrain_Ground") == true)
        {
            //取消攻擊
            if (currentCombatState != CombatState.None)
            {
                CancelAttack();
            }
            if (currentMoveState == MovementState.Rush)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.Rush );
            }
            else if (currentMoveState == MovementState.Run)
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.Run );
            }
            else
            {
                PlrAnim.SetInteger("ActionCode", (int)AnimCode.Idle);
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        Debug.Log("OnTriggerExit2D");
        if (col.gameObject.CompareTag("Terrain_Ground") == true && currentMoveState != MovementState.Jump)
        {
            currentMoveState = MovementState.Fall;
            PlrAnim.SetInteger("ActionCode", (int)AnimCode.Jump );
        }
    }

    bool IsOnAir()
    {
        return currentMoveState == MovementState.Jump || currentMoveState == MovementState.Fall;
    }

    bool IsShashing()
    {
        return currentCombatState == CombatState.Slash1 ||currentCombatState == CombatState.Slash2 || currentCombatState == CombatState.Slash3;
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

    void CancelAttack()
    {
        Debug.LogWarning("CancelAttack");
        currentCombatState = CombatState.None;

        // 徹底清空所有近戰與遠程的計時器與標記
        attackTimer = 0f;
        comboTimer = 0f;
        comboCount = 0;
        isComboAttack = false;
        ShootTime = 0;
    }

}
