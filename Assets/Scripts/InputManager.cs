using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    int Direction;
    public PlayerControls playerInput;
    public static Vector2 DirCodeToVector(int code)
    {
        switch (code)
        {
            case 6: return Vector2.right;
            case 4: return Vector2.left;
            case 8: return Vector2.up;
            case 2: return Vector2.down;
            case 1: return new Vector2(-1, -1).normalized;
            case 3: return new Vector2(1, -1).normalized;
            case 7: return new Vector2(-1, 1).normalized;
            case 9: return new Vector2(1, 1).normalized;
            case 5:
            default:
                return Vector2.zero;
        }
    }

    // 將新系統的 Vector2 轉為 1~9
    public int VectorToDirCode(Vector2 moveInput)
    {
        // 使用 Mathf.RoundToInt 處理微小的類比偏差，並將 -1~1 映射到 0~2
        int x = Mathf.RoundToInt(moveInput.x) + 1; // -1->0, 0->1, 1->2
        int y = Mathf.RoundToInt(moveInput.y) + 1; // -1->0, 0->1, 1->2

        // 經典公式：x + (y * 3) + 1
        // (0,0) 中立 -> 1 + (1*3) + 1 = 5
        // (-1,-1) 左下 -> 0 + (0*3) + 1 = 1
        // (1,1) 右上 -> 2 + (2*3) + 1 = 9
        return x + (y * 3) + 1;
    }
    enum AttackMode
    {
        None,
        Slash,
        Shoot,
    }
    AttackMode attackMode = AttackMode.None;
    // Start is called before the first frame update
    void Start()
    {

    }

    private void Awake()
    {
        playerInput = new PlayerControls();
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        DetectAttack();
    }

    //取得由Iput System監控的輸入資訊
    Vector2 GetPlrInputDir()
    {
        // 直接跟系統要現在的搖桿/按鍵向量
        return playerInput.Player.Move.ReadValue<Vector2>();
    }

    public int GetDirection()
    {
        // 轉換成九宮格並回傳
        return VectorToDirCode(GetPlrInputDir());
    }

    public Vector2 GetDirectionVector()
    {
        return DirCodeToVector(VectorToDirCode(GetPlrInputDir()));
    }

    public int GetAttackMode()
    {
        return (int)attackMode;
    }

    void DetectAttack()
    {

        if (playerInput.Player.Attack.WasPressedThisFrame())
        {
            attackMode = AttackMode.Slash;
        }
        // WasReleasedThisFrame() 對應舊版的 Input.GetKeyUp()
        else if (playerInput.Player.Attack.WasReleasedThisFrame())
        {
            attackMode = AttackMode.None;
        }

    }
}
