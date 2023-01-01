using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraStateFactory
{
    public static Wait_CamState Wait(CameraStateMachine context)
    {
        return new Wait_CamState(context);
    }
    public static Move_CamState Move(CameraStateMachine context, Vector3 targetPosition)
    {
        return new Move_CamState(context, targetPosition);
    }
    public static ChargeLaser_CamState ChargeLaser(CameraStateMachine context, Transform playerTarget)
    {
        return new ChargeLaser_CamState(context, playerTarget);
    }
    public static FireLaser_CamState FireLaser(CameraStateMachine context, Transform playerTarget)
    {
        return new FireLaser_CamState(context, playerTarget);
    }
    public static StopLaser_CamState StopLaser(CameraStateMachine context, float delay)
    {
        return new StopLaser_CamState(context, delay);
    }
    public static Chase_CamState Chase(CameraStateMachine context, Transform targetPlayer)
    {
        return new Chase_CamState(context, targetPlayer);
    }
    public static Lost_CamState Lost(CameraStateMachine context, Vector3 lastPos)
    {
        return new Lost_CamState(context, lastPos);
    }

}
