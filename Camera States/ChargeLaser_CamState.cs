using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeLaser_CamState : CameraBaseState
{
    private Transform _player;
    private float _currentTimer;

    public ChargeLaser_CamState(CameraStateMachine context, Transform player) : base(context) 
    {
        _player = player;
    }

    public override void EnterState()
    {
        //Play charge audio
        _context.CameraPlayer.RPC_PlayClipNetworked("Charge");
        _currentTimer = 0;
    }

    public override void UpdateState(float deltaTime)
    {
        CheckSwitchState();

        //Tick laser charge timer
        _currentTimer = Mathf.Clamp01(_currentTimer + (deltaTime / _context.LaserChargeTime));

        //Update eye
        _context.SetEyeIntensity(_currentTimer);
    }

    public override void ExitState()
    {

    }

    protected override void CheckSwitchState()
    {
        if (_currentTimer == 1)
        {
            //Switch to Fire Laser state when timer is complete
            SwitchState(CameraStateFactory.FireLaser(_context, _player));
        }
    }
}
