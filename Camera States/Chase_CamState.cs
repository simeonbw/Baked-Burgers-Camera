using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chase_CamState : CameraBaseState
{
    private Transform _player;
    private float _currentTimer;

    public Chase_CamState(CameraStateMachine context, Transform targetPlayer) : base(context) 
    {
        _player = targetPlayer;
    }

    public override void EnterState()
    {
        _currentTimer = 0;
    }

    public override void UpdateState(float deltaTime)
    {
        if (_context.CheckForPlayer(out Transform p)) //Check if player is in the detection eye
        {
            if (p != _player)
            {
                //If detected player is not the original target, then switch target to detected player
                _player = p;
            }

            if (!_context.PlayerInLOS(_player)) //Check is player is in Line of Sight
            {
                //If not adjust the search position to nearest point not on obstacle
                _context.ObstacleAdjustment(_player.position, out Vector3 newPos);
                //Switch to Lost state and provide last seen position
                CameraStateFactory.Lost(_context, newPos);
                return;
            }

            //Move camera look target towards the position of the player
            _context.LookTarget.position = Vector3.MoveTowards(_context.LookTarget.position, _player.position, _context.ChaseSpeed * deltaTime);

            if (_currentTimer == 1)
            {
                _currentTimer = 0;
                //Update eye shader
                _context.SetEyeClosed(0.8f);
                //Switch to Charge Laser state
                SwitchState(CameraStateFactory.ChargeLaser(_context, _player));

                return;
            }
            else
            {
                //Tick Focus timer
                _currentTimer = Mathf.Clamp01(_currentTimer + (deltaTime / _context.FocusTime));
                //Update eye shader
                _context.SetEyeClosed(_currentTimer);
            }
        }
        else
        {
            //Switch to Lost state and provide the players position
            CameraStateFactory.Lost(_context, _player.position);
        }
    }

    public override void ExitState()
    {

    }
    
    protected override void CheckSwitchState()
    {

    }
}
