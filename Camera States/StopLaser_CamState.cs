using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopLaser_CamState : CameraBaseState
{
    private float _currentTimer;
    private float _delay;

    public StopLaser_CamState(CameraStateMachine context, float delay) : base(context) 
    {
        _delay = delay;
    }

    public override void EnterState()
    {
        _currentTimer = 0;
    }

    public override void UpdateState(float deltaTime)
    {
        if (_currentTimer == 1)
        {
            //Message clients to play stop audio
            _context.LaserPlayer.RPC_PlayClipNetworked("Stop");

            //Message clients to deactivate laser
            _context.RPC_DeactivateLaser();

            //Update eye
            _context.SetEyeClosed(0.2f);
            _context.SetEyeIntensity(0);

            //Message clients to update pupil decal
            _context.RPC_UpdatePupilDecal(false);

            //Switch to Wait state
            SwitchState(CameraStateFactory.Wait(_context));
        }
        else
        {
            //Update timer
            _currentTimer = Mathf.Clamp01(_currentTimer + (deltaTime / _delay));

            //Update eye according to timer
            _context.SetEyeClosed(_currentTimer);
            _context.SetEyeIntensity(1 - _currentTimer);
        }
    }

    public override void ExitState()
    {

    }

    protected override void CheckSwitchState()
    {

    }
}
