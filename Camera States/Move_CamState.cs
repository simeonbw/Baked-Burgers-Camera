using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move_CamState : CameraBaseState
{
    private Vector3 _targetPos;

    public Move_CamState(CameraStateMachine context, Vector3 targetPosition) : base(context) 
    {
        _targetPos = targetPosition;
    }

    public override void EnterState()
    {

    }

    public override void UpdateState(float deltaTime)
    {
        //Move eye towards target position
        _context.LookTarget.position = Vector3.MoveTowards(_context.LookTarget.position, _targetPos, _context.MoveSpeed * deltaTime);

        CheckSwitchState();
    }

    public override void ExitState()
    {

    }

    protected override void CheckSwitchState()
    {
        if (_context.LookTarget.position == _targetPos) //check if the eye is looking at the target
        {
            //Switch to Wait state
            SwitchState(CameraStateFactory.Wait(_context));
        }

        else if (_context.CheckForPlayer(out Transform player)) //Check if player is in eye
        {
            if (_context.PlayerInLOS(player)) //Check if player is in line of sight
            {
                //Switch to Chase state
                SwitchState(CameraStateFactory.Chase(_context, player));
            }
        }
    }
}
