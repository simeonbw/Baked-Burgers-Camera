using Fusion;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

public class CameraStateMachine : NetworkBehaviour
{
    private CameraBaseState _currentState;
    public CameraBaseState CurrentState { get => _currentState; set => _currentState = value; }

    #region Idle Behaviour

    [BoxGroup("Idle Behaviour")]
    [SerializeField] private float _moveSpeed = 1f;
    public float MoveSpeed { get => _moveSpeed; }

    [BoxGroup("Idle Behaviour")]
    [SerializeField] private CameraLookVolume[] _lookVolumes;
    public CameraLookVolume[] LookVolumes { get => _lookVolumes; }

    private int _currentLookVolumeIndex = 0;
    public int CurrentLookVolumeIndex { get => _currentLookVolumeIndex; set => _currentLookVolumeIndex = value; }

    #endregion

    #region Chase Behaviour

    [BoxGroup("Chase Behaviour")]
    [SerializeField] private float _chaseSpeed = 25f;
    public float ChaseSpeed { get => _chaseSpeed; }

    [BoxGroup("Chase Behaviour")]
    [SerializeField] private float _focusTime = 2f;
    public float FocusTime { get => _focusTime; }

    [BoxGroup("Chase Behaviour")]
    [SerializeField] private float _maxChaseDistance = 5f;
    public float MaxChaseDistance { get => _maxChaseDistance; }

    #endregion

    #region Lost Behaviour

    [BoxGroup("Player Lost Behaviour")]
    [SerializeField] private int _lostCheckCount;
    public int LostCheckCount { get => _lostCheckCount; }

    [BoxGroup("Player Lost Behaviour")]
    [SerializeField] private float _lostCheckDelay = 2f;
    public float LostCheckDelay { get => _lostCheckDelay; }

    #endregion

    #region Laser

    [BoxGroup("Laser")]
    [SerializeField] private VisualEffect _laser;

    [BoxGroup("Laser")]
    [SerializeField] private float _laserMoveSpeed = 30f;
    public float LaserMoveSpeed { get => _laserMoveSpeed; }

    [BoxGroup("Laser")]
    [SerializeField] private float _laserChargeTime = 1f;
    public float LaserChargeTime { get => _laserChargeTime; }

    #endregion

    #region Eye

    [BoxGroup("Eye")]
    [SerializeField, OnValueChanged("OnEyeRadiusUpdated")] private float _eyeRadius = 1f;

    [BoxGroup("Eye")]
    [SerializeField] private DecalProjector _eyeLidsProjector;
    [BoxGroup("Eye")]
    [SerializeField] private DecalProjector _eyeProjector;
    [BoxGroup("Eye")]
    [SerializeField] private DecalProjector _eyePupilProjector;

    [BoxGroup("Eye")]
    [SerializeField] private Texture2D _defaultPupil;
    [BoxGroup("Eye")]
    [SerializeField] private Texture2D _targetedPupil;

    [Networked] private float _eyeIntensity { get; set; }
    [Networked] private float _eyeClosed { get; set; }
    public void SetEyeClosed(float value) { _eyeClosed = Mathf.Clamp(value, 0.2f, 0.8f); }
    public void SetEyeIntensity(float value) { _eyeIntensity = value; }

    #endregion

    #region Layer Masks

    [BoxGroup("Layer Masks")]
    [SerializeField] private LayerMask _ground;
    public LayerMask GroundLayer { get => _ground; }

    [BoxGroup("Layer Masks")]
    [SerializeField] private LayerMask _player;
    public LayerMask PlayerLayer { get => _player; }

    [BoxGroup("Layer Masks")]
    [SerializeField] private LayerMask _obstacleMask;

    #endregion

    #region Scene Refs

    [BoxGroup("Scene References")]
    [SerializeField] private Transform _cameraTransform;
    public Transform CameraTransform { get => _cameraTransform; }

    [BoxGroup("Scene References")]
    [SerializeField] private Transform _lookTarget;
    public Transform LookTarget { get => _lookTarget; }

    [SerializeField] private AudioPlayer _laserPlayer;
    public AudioPlayer LaserPlayer { get => _laserPlayer; }

    [SerializeField] private AudioPlayer _cameraPlayer;
    public AudioPlayer CameraPlayer { get => _cameraPlayer; }

    #endregion

    private void OnEyeRadiusUpdated()
    {
        if (_eyeLidsProjector != null)
        {
            _eyeLidsProjector.size = new Vector3(_eyeRadius * 2, _eyeRadius * 2, _eyeLidsProjector.size.z);
        }
        if (_eyeProjector != null)
        {
            _eyeProjector.size = new Vector3(_eyeRadius * 2, _eyeRadius * 2, _eyeProjector.size.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _maxChaseDistance);
    }

    private bool _spawned = false;

    public override void Spawned()
    {
        base.Spawned();
        _spawned = true;
        _currentState = CameraStateFactory.Wait(this);
        _currentState.EnterState();

        _eyeLidsProjector.material = new Material(_eyeLidsProjector.material.shader);
        _eyeLidsProjector.material.SetInt("_DrawOrder", 2);
        _eyeProjector.material = new Material(_eyeProjector.material.shader);
        _eyePupilProjector.material = new Material(_eyePupilProjector.material.shader);
        _eyePupilProjector.material.SetTexture("_MainTexture", _defaultPupil);
        _eyePupilProjector.material.SetInt("_DrawOrder", 1);
        SetEyeClosed(0.2f);

        if (LobbyPlayer.Local.IsLeader)
        {
            Object.AssignInputAuthority(LobbyPlayer.Local.Object.InputAuthority);
        }
        
    }

    private void Update()
    {
        if (_spawned)
        {
            //Update eye shader
            _eyeProjector.material.SetFloat("_RedIntensity", _eyeIntensity);
            _eyeLidsProjector.material.SetFloat("_EyeClose", _eyeClosed);
        }
    }

    public override void FixedUpdateNetwork()
    {

        base.FixedUpdateNetwork();

        if (!Object.HasInputAuthority) { return; }

        //Update camera rotation
        _cameraTransform.rotation = Quaternion.LookRotation(_lookTarget.position - _cameraTransform.position);
        _lookTarget.rotation = Quaternion.Euler(0, _cameraTransform.eulerAngles.y, 0);

        _currentState.UpdateState(Runner.DeltaTime);
    }

    /// <summary>
    /// Adjusts currentTargetPos to nearest point not on obstacle and outputs it as newTargetPos
    /// </summary>
    /// <param name="currentTargetPos"></param>
    /// <param name="newTargetPos"></param>
    /// <returns>
    /// True if the position was adjusted
    /// </returns>
    public bool ObstacleAdjustment(Vector3 currentTargetPos, out Vector3 newTargetPos)
    {
        float dist = _maxChaseDistance + 5f;

        Vector3 dir = (currentTargetPos - _cameraTransform.position).normalized;

        Ray ray = new Ray(_cameraTransform.position + dir, dir);

        Debug.DrawRay(_cameraTransform.position + dir, dir * dist, Color.blue, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, dist, _obstacleMask))
        {
            newTargetPos = new Vector3(hit.point.x, GroundHeightAtPos(hit.point), hit.point.z);
            return true;
        }
        newTargetPos = currentTargetPos;
        return false;
    }

    /// <summary>
    /// Get ground height at pos in World Space
    /// </summary>
    /// <param name="pos"></param>
    /// <returns>
    /// the height position
    /// </returns>
    private float GroundHeightAtPos(Vector3 pos)
    {
        Ray ray = new Ray(pos + (Vector3.up * 10), Vector3.down);
        Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _ground);
        return hit.point.y;
    }

    /// <summary>
    /// Check if player is withing the detection Eye
    /// </summary>
    /// <param name="player">
    /// Outputs the player if found, otherwise null.
    /// </param>
    /// <returns>
    /// True if player is within Eye
    /// </returns>
    public bool CheckForPlayer(out Transform player)
    {
        Collider[] collisions = Physics.OverlapSphere(_lookTarget.position, _eyeRadius, _player);

        if (collisions.Length > 0)
        {
            if (collisions.Length == 1)
            {
                NetworkPlayer nP = collisions[0].GetComponentInParent<NetworkPlayer>();
                if ((nP != null && nP.IsDead) || Vector3.Distance(_cameraTransform.position, collisions[0].transform.position) > _maxChaseDistance)
                {
                    player = null;
                    return false;
                }

                player = collisions[0].transform;
                return true;
            }
            else
            {
                NetworkPlayer nP1 = collisions[0].GetComponentInParent<NetworkPlayer>();
                NetworkPlayer nP2 = collisions[1].GetComponentInParent<NetworkPlayer>();

                if(nP1 != null && nP2 != null)
                {
                    if (!nP1.IsDead && !nP2.IsDead)
                    {
                        float p1Dist = Vector3.Distance(_cameraTransform.position, collisions[0].transform.position);
                        float p2Dist = Vector3.Distance(_cameraTransform.position, collisions[1].transform.position);

                        bool p2Closest = p1Dist > p2Dist;

                        player = p2Closest ? collisions[1].transform : collisions[0].transform;
                        
                        if((p2Closest && p2Dist > _maxChaseDistance) || (!p2Closest && p1Dist > _maxChaseDistance))
                        {
                            player = null;
                            return false;
                        }

                        return true;
                    }
                    else if (!nP1.IsDead)
                    {
                        player = collisions[0].transform;
                        return true;
                    }
                    else if (!nP2.IsDead)
                    {
                        player = collisions[1].transform;
                        return true;
                    }
                    else
                    {
                        player = null;
                        return false;
                    }
                }
            }
        }

        player = null;
        return false;
    }

    /// <summary>
    /// Returns true if the player is in Line of Sight
    /// </summary>
    /// <param name="player"></param>
    public bool PlayerInLOS(Transform player)
    {
        float dist = Vector3.Distance(_cameraTransform.position + _cameraTransform.forward, player.position) - 1;

        Vector3 dir = (player.position - _cameraTransform.position).normalized;

        Ray ray = new Ray(_cameraTransform.position + dir, dir);

        Debug.DrawRay(_cameraTransform.position + dir, dir * dist, Color.red, 1f);

        return !Physics.Raycast(ray, dist, _obstacleMask);
    }

    #region RPC's

    /// <summary>
    /// Send RPC to clients to activate the laser
    /// </summary>
    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_ActivateLaser()
    {
        if (!_laser.gameObject.activeSelf)
        {
            _laser.gameObject.SetActive(true);
        }

    }

    /// <summary>
    /// Send RPC to clients to deactivate the laser
    /// </summary>
    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_DeactivateLaser()
    {
        if (_laser.gameObject.activeSelf)
        {
            _laser.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Send RPC to clients to update the pupil decal
    /// </summary>
    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_UpdatePupilDecal(NetworkBool targeted)
    {
        _eyePupilProjector.material.SetTexture("_MainTexture", targeted ? _targetedPupil : _defaultPupil);
    }

    #endregion
}
