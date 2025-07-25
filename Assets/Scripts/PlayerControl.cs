using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public PlayerStatus plrStatus;
    public ActionStates ActionState;
    private int direction;
    InputManager InputMgr;
    PrefabManager PrefabMgr;

    public Animator PlrAnim;

    public bool isFaceRight = false;
    public bool isRushing = false;
    private float rushTime = 0.5f;

    //跳躍控制
    public JumpParameter jumpParameter;
    private float jumpTimer = 0f;
    private float currentYSpeed = 0f;
    public bool isJumping = false;
    public bool isFalling = true;

    //private Rigidbody2D rb;
    public float slopeCheckDistance = 2f;
    bool isSloap = false;

    //角色相關物件
    public Transform ShootPos;
    public Transform ButtonPos;
    public Transform DustPos;

    //暫時變數
    private bool isAttacking = false;
    private bool isShooting = false;
    private float AttackTime = 0.2f;    //攻擊動畫時間
    ///private float AttackComboDelay = 0.1f;
    private int comboCount = 0; //斬擊連擊次數
    private float AttackRunTime = 0f;
    private bool comboAttack = false;
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
        isFaceRight = false;
    }

    // Update is called once per frame
    void Update()
    {
        #region move

        //
        float moveSpeed = plrStatus.moveSpeed;
        float sloapAngle = 90;
        sloapAngle = CheckSlope();

        //移動方向
        direction = InputMgr.GetDirection();
        if (!IsOnAir())
        {

        }
        if (!isRushing)
        {
            if (direction != 5 && direction != 0)
            {
                if (direction == 6)
                {
                    if (!isFaceRight)
                    {
                        isFaceRight = true;
                        this.gameObject.transform.rotation = Quaternion.Euler(0, 180, 0);
                    }
                    if (sloapAngle == 90)
                    {
                        this.gameObject.transform.Translate(new Vector2(isFaceRight ? -1 : 1, 0) * Time.deltaTime * moveSpeed);
                    }
                    else if (sloapAngle <= 45)
                    {
                        this.gameObject.transform.Translate(new Vector2(isFaceRight ? -1 : 1, Mathf.Abs(Mathf.Tan(sloapAngle))) * Time.deltaTime * moveSpeed);
                    }
                }
                else if (direction == 4)
                {
                    if (isFaceRight)
                    {
                        isFaceRight = false;
                        this.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
                    }
                    if (sloapAngle == 90)
                    {
                        this.gameObject.transform.Translate(new Vector2(isFaceRight ? 1 : -1, 0) * Time.deltaTime * moveSpeed);
                    }
                    else if (sloapAngle <= 45)
                    {

                        this.gameObject.transform.Translate(new Vector2(isFaceRight ? 1 : -1, Mathf.Tan(sloapAngle)) * Time.deltaTime * moveSpeed);
                    }
                }
                if (!IsOnAir())
                {
                    PlrAnim.SetInteger("ActionCode", 1);
                }
            }
            else if (direction == 5)
            {
                if (!IsOnAir() && !isAttacking)
                {
                    PlrAnim.SetInteger("ActionCode", 0);
                }
            }
        }


        //衝刺
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isRushing)
            {
                isRushing = true;
                PlrAnim.SetInteger("ActionCode", 3);
                PrefabMgr.setEff(1, DustPos, isFaceRight);
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

        //跳躍
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
            jumpTimer = 0f;
            PlrAnim.SetInteger("ActionCode", 2);
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
            if (IsGrounded())
            {
                isFalling = false;
                currentYSpeed = 0f;
                jumpTimer = 0f;
                ResetHeight();
            }
        }
        #endregion move

        #region attack

        //斬擊
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (!isAttacking)
            {
                isAttacking = true;
            }
            //站立狀態
            if (direction == 5 && !isRushing && !IsOnAir())
            {
                if (comboCount == 0)
                {
                    comboAttack = false;
                    Debug.Log("First Slash");
                    PlrAnim.SetInteger("ActionCode", 6);
                }

                if (AttackRunTime > 0 && comboCount < 2)
                {
                    AttackRunTime = 0; //連擊會刷新攻擊判定時間
                    comboAttack = true;
                    comboCount++;
                    if (comboCount > 2)
                    {
                        comboCount = 0;
                    }
                    Debug.Log("Slash Combo" + comboCount);
                    PlrAnim.SetInteger("ActionCode", 6 + comboCount);

                }

            }
            if (!IsOnAir() && (direction == 4 || direction == 6))
            {
                PlrAnim.SetInteger("ActionCode", 10);
            }
            else if (isRushing)
            {
                PlrAnim.SetInteger("ActionCode", 12);
            }
            else if (IsOnAir())
            {
                PlrAnim.SetInteger("ActionCode", 14);
            }

        }

        //TODO處理攻擊動畫銜接

        //Note 目前同一12f(0.2s)
        if (isAttacking)
        {
            AttackRunTime = AttackRunTime + Time.deltaTime;
            if (AttackRunTime >= AttackTime)
            {
                isAttacking = false;
                AttackRunTime = 0;
                Debug.Log("End of AttackAnim");
                comboCount = 0;


                if (isRushing)
                {
                    PlrAnim.SetInteger("ActionCode", 3);
                }
                if (IsOnAir())
                {
                    PlrAnim.SetInteger("ActionCode", 2);
                }
            }
        }



        //射擊
        if (Input.GetKeyDown(KeyCode.Mouse1))
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

        //持續射擊
        if (Input.GetKey(KeyCode.Mouse1))
        {
            if (isShooting && isRushing == false && (isJumping || isFalling) == false)
            {
                OnShootingPress();
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
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

        #endregion attack

    }

    void ShootBullet()
    {
        PrefabMgr.setEff(2, ShootPos, isFaceRight);
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

    bool IsGrounded()
    {
        Vector2 footPos = new Vector2(ButtonPos.position.x, ButtonPos.position.y);
        float rayLength = 0.05f;
        LayerMask groundMask = LayerMask.GetMask("Terrain_Ground");

        return Physics2D.Raycast(footPos, Vector2.down, rayLength, groundMask);
    }

    float CheckSlope()
    {
        float slopeAngle = 90;
        Vector2 position = transform.position;
        if (ButtonPos != null)
        {
            position = ButtonPos.position;
        }
        Vector2 direction = isFaceRight ? Vector2.right : -Vector2.right; ;
        // 建立一條射線檢查坡度
        RaycastHit2D hit = Physics2D.Raycast(position, direction, slopeCheckDistance, LayerMask.GetMask("Terrain_Ground"));

        if (hit.collider != null)
        {
            // 計算坡度角度
            slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            Debug.Log("Slope angle: " + slopeAngle);
            // 如果坡度角度小於一定的角度（例如30度），則允許角色爬坡
            if (slopeAngle < 45f)
            {
                Debug.Log("There is slope");
            }
        }
        else
        {
            Debug.Log("Hit is null");
        }
        return slopeAngle;
    }

    void ResetHeight()
    {
        float characterHeightOffset = 0f;//

        // 射線往下偵測，確認地面位置
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.05f, LayerMask.GetMask("Terrain_Ground"));
        if (hit.collider != null)
        {
            // 將角色的 y 座標設定為：地面位置 + 腳底偏移
            float correctY = hit.point.y + characterHeightOffset;
            transform.position = new Vector3(transform.position.x, correctY, transform.position.z);
            Debug.Log("Reset Height");
        }
    }

}
