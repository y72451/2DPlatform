using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerControl;

public class PlayerControl : MonoBehaviour
{
    public PlayerStatus plrStatus;
    public ActionStates ActionState;
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
    public bool isRushing = false;
    private float rushTime = 0.5f;

    private GroundInfo frontGroundInfo;
    private GroundInfo rearGroundInfo;

    //跳躍控制
    public JumpParameter jumpParameter;
    private float jumpTimer = 0f;
    private float currentYSpeed = 0f;
    public bool isJumping = false;
    public bool isFalling = true;

    //private Rigidbody2D rb;
    public float slopeCheckDistance = 15.0f;

    //角色相關物件
    public Transform ShootPos;
    public Transform FrontLegPos;
    public Transform RearLegPos;
    public Transform CenterGroundPos;
    public Transform DustPos;

    //暫時變數
    private bool isAttacking = false;
    private bool isShooting = false;

    //攻擊變數
    private float attackTimer = 0f;             //實際攻擊動作執行時間
    private float attackDuration = 0.2f;        //攻擊動畫時間

    private float comboTimer = 0f;           //連擊計時器
    private float comboWindowDelay = 0.5f;   //可以繼續派成連擊的窗口時間
    private int comboCount = 0;              //斬擊連擊次數
    
    private bool isComboAttack = false;           //是否需要在下一次進行連擊

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
        #region move

        //
        float moveSpeed = plrStatus.moveSpeed;
        frontGroundInfo = GroundDetector.DecteGround(FrontLegPos, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));
        rearGroundInfo = GroundDetector.DecteGround(RearLegPos, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));
        GroundInfo centerGroundInfo = GroundDetector.DecteGround(CenterGroundPos, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));

        //處理面向
        Vector2 inputDir = InputMgr.GetMovementVector();
        direction = InputMgr.GetDirection();
        if (inputDir.x != 0)
        {
            Facing newFacing = inputDir.x > 0 ? Facing.Right : Facing.Left;
            if (newFacing != currentFacing)
            {
                currentFacing = newFacing;
                Flip(currentFacing);
            }
        }

        if (!IsOnAir())
        {

        }
        if (!isRushing)
        {
            if (inputDir != Vector2.zero)
            {
                // 1. 建立純粹的世界座標水平輸入向量
                Vector2 horizontalMove = new Vector2(inputDir.x, 0f);
                Vector2 moveDir = horizontalMove.normalized; //預設平行移動(平地或空中)

                if (centerGroundInfo.isGrounded && centerGroundInfo.slopeAngle > 0 && centerGroundInfo.slopeAngle <= 45f)
                {
                    // 2. 利用 Vector3.ProjectOnPlane 將水平輸入「投影」到斜坡法線上
                    // 這會自動幫你算出貼合斜坡的完美斜向向量，無論向左或向右都絕對正確
                    moveDir = Vector3.ProjectOnPlane(horizontalMove, centerGroundInfo.slopeNormal).normalized;
                }

                // Debug.Log("Apply PlaneMove (World): " + moveDir);
                // 3. 【關鍵修正】明確指定 Space.World！
                // 這樣位移就完全不會受到 Flip() 轉 Y 軸的干擾
                transform.Translate(moveSpeed * Time.deltaTime * moveDir, Space.World);

                if (!IsOnAir() && !isAttacking)
                {
                    PlrAnim.SetInteger("ActionCode", 1);
                }
            }
            else if (inputDir == Vector2.zero)
            {
                if (!IsOnAir() && !isAttacking)
                {
                    PlrAnim.SetInteger("ActionCode", 0);
                }
            }
        }

        if (isRushing)
        {
            this.gameObject.transform.Translate(new Vector2(-1, 0) * Time.deltaTime * moveSpeed * 3);
            rushTime = rushTime - Time.deltaTime;
            if (rushTime <= 0)
            {
                rushTime = 0.5f;
                isRushing = false;
            }
        }

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
        #endregion move

        #region attack
        //Note 目前同一12f(0.2s)

        if (!isAttacking && comboCount > 0)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer >= comboWindowDelay)
            {
                // 寬限期真的過了，徹底忘記連擊
                comboCount = 0;
                // Debug.Log("Combo Memory Lost");
            }
        }


        if (isAttacking)
        {
            attackTimer = attackTimer + Time.deltaTime;
            if (attackTimer >= attackDuration)
            {
                Debug.Log("AttackTimer:" + attackTimer);
                isAttacking = false;
                attackTimer = 0f;
                comboTimer = 0f; // 動畫結束的瞬間，啟動連擊記憶倒數

                // 檢查剛才有沒有預先輸入 (Input Buffering)
                if (isComboAttack && comboCount > 0 && comboCount < 3)
                {
                    // 有預先輸入，處理連擊
                    ExecuteNextSlash();
                }
                else
                {
                    if (isRushing)
                    {
                        PlrAnim.SetInteger("ActionCode", 3);
                    }
                    else if (IsOnAir())
                    {
                        PlrAnim.SetInteger("ActionCode", 2);
                        Debug.Log("Back to jump position");
                        //Debug.Break();
                    }
                    else
                    {
                        PlrAnim.SetInteger("ActionCode", 0);
                    }

                    Debug.Log("End of AttackAnim");
                    Debug.Break();
                }

            }
        }

        //持續射擊
        if (isShooting)
        {
            if (isShooting && isRushing == false && (isJumping || isFalling) == false)
            {
                OnShootingPress();
            }
        }

        #endregion attack

    }
    //跳躍
    void HandleJump()
    {
        
        isJumping = true;
        jumpTimer = 0f;
        PlrAnim.SetInteger("ActionCode", 2);
    }

    //衝刺
    void HandleRush()
    {
        if (!isRushing)
        {
            isRushing = true;
            PlrAnim.SetInteger("ActionCode", 3);
            PrefabMgr.setEff(1, DustPos, currentFacing == Facing.Right);
            /*
            if (plrDebugOption != null && plrDebugOption.GetSpecialRush() == true)
            {
                PlrAnim.SetInteger("ActionCode", 16);
            }
            else
            {
                PlrAnim.SetInteger("ActionCode", 3);
            }      
            */

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
        //comboTimer = 0f;
        isComboAttack = false;


        // 判斷要發動哪一種攻擊，並設定對應的狀態
        if (IsOnAir())
        {
            PlrAnim.SetInteger("ActionCode", 14); // 空中砍
            comboCount = -1; // -1 代表「這是特殊攻擊，沒有下一段」
                             // Debug.Log("Air Slash");
            //Debug.Break();
        }
        else if (isRushing)
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
        if (direction == 5 && !isRushing && !IsOnAir())
        {
            PlrAnim.SetInteger("ActionCode", 9);
        }
        else if (!IsOnAir() && (direction == 4 || direction == 6))
        {
            PlrAnim.SetInteger("ActionCode", 11);
        }
        else if (isRushing)
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
        if (direction == 5 && !isRushing && !IsOnAir())
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
            if (isRushing)
            {
                PlrAnim.SetInteger("ActionCode", 3);
            }
            else if (isRushing)
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
        if (col.gameObject.CompareTag("Terrain_Ground") == true && !isJumping)
        {
            isFalling = true;
            PlrAnim.SetInteger("ActionCode", 2);
        }
    }

    bool IsOnAir()
    {
        return (isJumping || isFalling);
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
