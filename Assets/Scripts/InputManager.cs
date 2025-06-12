using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    int Direction;
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
        detectDirection();
        detectAttack();
    }

    public int GetDirection()
    {
        return Direction;
    }

    public int GetAttackMode()
    {
        return (int)attackMode;
    }

    void detectDirection()
    {
        Direction = 5;
        bool counterBalance = false;
        if(Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
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
                Direction = counterBalance ? 5: 6;
            }
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                counterBalance= true;
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

    void detectAttack()
    {
        
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            attackMode = AttackMode.Slash;
        }
        else if(Input.GetKeyDown(KeyCode.Mouse1))
        {
            attackMode= AttackMode.Shoot;
        }
        
    }
}
