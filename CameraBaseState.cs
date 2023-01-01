using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CameraBaseState
{
    protected CameraStateMachine _context;

    public CameraBaseState(CameraStateMachine context)
    {
        _context = context;
    }

    /// <summary>
    /// Call this function on a new camera state the camera will be entering.
    /// </summary>
    public abstract void EnterState();

    /// <summary>
    /// Call this in the Update function of the State Machine in order to update State logic.
    /// </summary>
    public abstract void UpdateState(float deltaTime);

    /// <summary>
    /// Call this function on the state the camera is currently in before entering a new state.
    /// </summary>
    public abstract void ExitState();

    /// <summary>
    /// This function can be used in order to check when a different state should be entered.
    /// </summary>
    protected abstract void CheckSwitchState();

    /// <summary>
    /// Call this function when switching to a new state.
    /// </summary>
    public void SwitchState(CameraBaseState newState)
    {
        //Exit current state
        _context.CurrentState.ExitState();

        //Enter new state
        newState.EnterState();

        //Set current state to new state
        _context.CurrentState = newState;
    }
}
