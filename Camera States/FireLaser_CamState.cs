using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireLaser_CamState : CameraBaseState
{
    private Transform _player;
    private NetworkPlayer _playerTarget;

    public FireLaser_CamState(CameraStateMachine context, Transform playerTarget) : base(context) 
    {
        _player = playerTarget;
        _playerTarget = _player.GetComponentInParent<NetworkPlayer>();
    }

    public override void EnterState()
    {
        //Message clients to play Fire audio
        _context.LaserPlayer.RPC_PlayClipNetworked("Fire");

        //Message clients to activate laser
        _context.RPC_ActivateLaser();

        //Message clients to update eye
        _context.RPC_UpdatePupilDecal(true);
    }

    public override void UpdateState(float deltaTime)
    {
        if (!_context.PlayerInLOS(_player) || _playerTarget.IsDead)// Check if player is not in line of sight OR is dead
        {
            //Adjust the position to nearest point not on obstacle
            _context.ObstacleAdjustment(_player.position, out Vector3 newPos);
            //Set the look target of the camera to adjusted position
            _context.LookTarget.position = newPos;
            //Switch to Stop Laser state
            SwitchState(CameraStateFactory.StopLaser(_context, 0f));
        }

        if(Vector3.Distance(_context.LookTarget.position, _context.CameraTransform.position) > _context.MaxChaseDistance) // Check if the camera is trying to follow beyond max distance
        {
            //Switch to Stop Laser state
            SwitchState(CameraStateFactory.StopLaser(_context, 1f));
        }

        if (Physics.Raycast(_context.CameraTransform.position, (_context.LookTarget.position + Vector3.up) - _context.CameraTransform.position, out RaycastHit hit, Mathf.Infinity, _context.PlayerLayer)) // Cast for the player
        {
            if(hit.collider.gameObject == _playerTarget.gameObject) //Check if the hit object = the target player object
            {
                //Message clients to kill player
                _playerTarget.RPC_KillPlayer();
            }
            else
            {
                //The cast hit a non target player and in that case message cliets to kill them too
                hit.collider.GetComponentInParent<NetworkPlayer>().RPC_KillPlayer();
            }

            //Switch to Stop Laser state
            SwitchState(CameraStateFactory.StopLaser(_context, 1.5f));
        }

        //Move camera look target towards the position of the player
        _context.LookTarget.position = Vector3.MoveTowards(_context.LookTarget.position, _player.position, _context.LaserMoveSpeed * deltaTime);
    }

    public override void ExitState()
    {

    }

    protected override void CheckSwitchState()
    {

    }
}
