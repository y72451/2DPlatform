using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class JumpParameter : ScriptableObject
{
    public float jumpHeight = 3f;           // 想要跳多高
    public float jumpDuration = 0.5f;       // 上升時間（秒）
    public float fallSpeed = 6f;            // 等速下落速度
}
