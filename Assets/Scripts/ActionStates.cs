using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu]
public class ActionStates : ScriptableObject
{
    public enum ActionState
    {
        Idle,
        Move,
        Jump,
        Rush,
        Drop,
        Land,
        Slash1,
        Slash2,
        Slash3,
        StandShoot,
        RunSlash,
        RunShoot,
        RushSlash,
        RushShoot,
        JumpSlash,
        JumpShoot,
        SpecialRush,
    }
    
    private ActionState state = ActionState.Idle;

    public ActionState GetActionState()
    {
        return this.state;
    }

    public void SetActionState(int code)
    {
        int length = Enum.GetValues(typeof(ActionStates)).Length;
        if (code > length || code < 0)
        {
            this.state = ActionState.Idle;
        }
        try
        {
            this.state = (ActionState)code;
        }
        catch
        {
            Debug.LogError("Set Error");
        }
    }

    public void SetActionState(ActionState state)
    {
        this.state = state;
    }
    
}
