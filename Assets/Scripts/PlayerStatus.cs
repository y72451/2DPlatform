using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu]
public class PlayerStatus : ScriptableObject
{
    private int HP;
    private int MaxHP;
    private int MP;
    private int MaxMP;

    public int moveSpeed = 0;

    public int GetPayerMoveSpeed()
    {
        return moveSpeed;
    }
}

