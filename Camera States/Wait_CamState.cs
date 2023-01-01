using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wait_CamState : CameraBaseState
{
    private float _currentTimer;
    private Vector3 _targetPos;

    public Wait_CamState(CameraStateMachine context) : base(context) { }

    public override void EnterState()
    {
        _currentTimer = 0;
    }

    public override void UpdateState(float deltaTime)
    {
        if (_currentTimer == _context.LookVolumes[_context.CurrentLookVolumeIndex].WaitTimePerLook) //Check if timer has run on current look volume duration
        {
            Vector3 lookPos;

            if (_context.LookVolumes[_context.CurrentLookVolumeIndex].Look(out lookPos)) //Check if still looking within this look volume
            {
                _targetPos = lookPos;
            }
            else //Otherwise move on to next
            {
                //Continue in order of look patrolling
                _context.CurrentLookVolumeIndex = _context.CurrentLookVolumeIndex + 1 == _context.LookVolumes.Length ? 0 : _context.CurrentLookVolumeIndex + 1;
                //Look at next volume
                _context.LookVolumes[_context.CurrentLookVolumeIndex].Look(out lookPos);
                _targetPos = lookPos;
            }

            //Adjust the position to nearest point not on obstacle
            _context.ObstacleAdjustment(_targetPos, out _targetPos);

            //Switch to Move state
            SwitchState(CameraStateFactory.Move(_context, _targetPos));
        }
        else
        {
            //Update timer
            _currentTimer = Mathf.Clamp(_currentTimer + deltaTime, 0, _context.LookVolumes[_context.CurrentLookVolumeIndex].WaitTimePerLook);
        }

        if (_context.CheckForPlayer(out Transform player)) //Check if player is within eye
        {
            if (_context.PlayerInLOS(player)) //Check if target player is in line of sight
            {
                //Switch to Chase state
                SwitchState(CameraStateFactory.Chase(_context, player));
            }
        }
    }

    public override void ExitState()
    {
        
    }

    protected override void CheckSwitchState()
    {
        
    }
}
