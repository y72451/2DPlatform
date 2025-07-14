using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    int Direction;
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

    // Update is called once per frame
    void Update()
    {
        DetectDirection();
        DetectAttack();
    }

    public int GetDirection()
    {
        return Direction;
    }

    public Vector2 GetDirectionVector()
    {
        return DirCodeToVector(Direction);
    }

    public int GetAttackMode()
    {
        return (int)attackMode;
    }

    void DetectDirection()
    {
        Direction = 5;
        bool counterBalance = false;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                counterBalance = true;
            }
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                Direction = counterBalance ? 8 : 9;
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                Direction = counterBalance ? 2 : 3;
            }
            else
            {
                Direction = counterBalance ? 5 : 6;
            }
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                counterBalance = true;
            }
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                Direction = counterBalance ? 8 : 7;
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                Direction = counterBalance ? 2 : 1;
            }
            else
            {
                Direction = counterBalance ? 5 : 4;
            }
        }
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            Direction = 8;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            Direction = 2;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                Direction = 0;
            }
        }
    }

    void DetectAttack()
    {

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            attackMode = AttackMode.Slash;
        }
        else if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            attackMode = AttackMode.Shoot;
        }

    }
}
