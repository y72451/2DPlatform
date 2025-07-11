using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class JumpParameter : ScriptableObject
{
    public float jumpHeight = 3f;           // �Q�n���h��
    public float jumpDuration = 0.5f;       // �W�ɮɶ��]��^
    public float fallSpeed = 6f;            // ���t�U���t��
}
