using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lost_CamState : CameraBaseState
{
    private Vector3 _currentTargetPos;
    private float _currentTimer;
    private int _currentLostCheckCount;

    public Lost_CamState(CameraStateMachine context, Vector3 lastPos) : base(context) 
    {
        _currentTargetPos = lastPos;  
    }

    public override void EnterState()
    {
        _currentTimer = 0;
        _currentLostCheckCount = 0;
    }

    public override void UpdateState(float deltaTime)
    {
        if (_context.LookTarget.position != _currentTargetPos)
        {
            //Move eye to target position
            _context.LookTarget.position = Vector3.MoveTowards(_context.LookTarget.position, _currentTargetPos, _context.MoveSpeed * deltaTime);
        }
        else
        {
            if (_currentTimer == _context.LostCheckDelay) //Check if timer is complete
            {
                if (_currentLostCheckCount >= _context.LostCheckCount - 1)
                {
                    _currentLostCheckCount = 0;

                    //Update eye
                    _context.SetEyeClosed(0.2f);

                    //Switch to Wait state
                    SwitchState(CameraStateFactory.Wait(_context));
                }
                //Get random look position
                Vector3 randPos = Random.insideUnitCircle * 3;
                randPos = _currentTargetPos + new Vector3(randPos.x, 0, randPos.y);

                //Cast ray to ensure random point is on ground and update point
                Physics.Raycast(randPos + Vector3.up * 5, Vector3.down, out RaycastHit groundHit, Mathf.Infinity, _context.GroundLayer);
                randPos.y = groundHit.point.y;
                _currentTargetPos = randPos;

                _currentLostCheckCount++;

                //Reset timer
                _currentTimer = 0;
            }
            else
            {
                //Update timer
                _currentTimer = Mathf.Clamp(_currentTimer + deltaTime, 0, _context.LostCheckDelay);
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
