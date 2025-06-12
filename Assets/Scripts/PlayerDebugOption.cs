using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu]
public class PlayerDebugOption : MonoBehaviour
{
    public bool SpecialRush = false;


    public bool GetSpecialRush()
    {
        return SpecialRush;
    }

}
