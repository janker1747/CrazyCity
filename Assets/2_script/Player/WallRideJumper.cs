using UnityEngine;
using ArcadeVP;

public class WallRideJumper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArcadeVehicleController vehicle;
    [SerializeField] private Transform rayOrigin;

    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallRideLayer;

    [SerializeField] private float sideRayDistance = 3f;
    [SerializeField] private float forwardRayDistance = 4f;

    [SerializeField] private float rayHeightOffset = 0.5f;

    [Header("Input")]
    [SerializeField] private KeyCode wallRideKey = KeyCode.LeftShift;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRays = true;

    private RaycastHit _leftHit;
    private RaycastHit _rightHit;
    private RaycastHit _forwardHit;

    private bool _hasLeftWall;
    private bool _hasRightWall;
    private bool _hasForwardWall;

    private void Reset()
    {
        vehicle = GetComponent<ArcadeVehicleController>();
    }

    private void Awake()
    {
        if (vehicle == null)
            vehicle = GetComponent<ArcadeVehicleController>();

        if (rayOrigin == null)
            rayOrigin = transform;
    }

    private void Update()
    {
        ScanWalls();

        if (vehicle == null)
            return;

        if (Input.GetKeyDown(wallRideKey))
            RequestStartWallRide();
    }

    public void RequestStartWallRide()
    {
        ScanWalls();
        TryStartWallRide();
    }

    private void ScanWalls()
    {
        Vector3 origin =
            rayOrigin.position + Vector3.up * rayHeightOffset;

        Vector3 leftDir = -transform.right;
        Vector3 rightDir = transform.right;
        Vector3 forwardDir = transform.forward;

        _hasLeftWall = Physics.Raycast(
            origin,
            leftDir,
            out _leftHit,
            sideRayDistance,
            wallRideLayer
        );

        _hasRightWall = Physics.Raycast(
            origin,
            rightDir,
            out _rightHit,
            sideRayDistance,
            wallRideLayer
        );

        _hasForwardWall = Physics.Raycast(
            origin,
            forwardDir,
            out _forwardHit,
            forwardRayDistance,
            wallRideLayer
        );

        if (drawDebugRays)
        {
            Debug.DrawRay(
                origin,
                leftDir * sideRayDistance,
                _hasLeftWall ? Color.green : Color.red
            );

            Debug.DrawRay(
                origin,
                rightDir * sideRayDistance,
                _hasRightWall ? Color.green : Color.red
            );

            Debug.DrawRay(
                origin,
                forwardDir * forwardRayDistance,
                _hasForwardWall ? Color.cyan : Color.magenta
            );
        }
    }

    private void TryStartWallRide()
    {
        RaycastHit selectedHit;

        // ПРИОРИТЕТ ПЕРЕДНЕЙ СТЕНЫ
        if (_hasForwardWall)
        {
            selectedHit = _forwardHit;
        }
        else if (_hasLeftWall && _hasRightWall)
        {
            selectedHit =
                _leftHit.distance <= _rightHit.distance
                    ? _leftHit
                    : _rightHit;
        }
        else if (_hasLeftWall)
        {
            selectedHit = _leftHit;
        }
        else if (_hasRightWall)
        {
            selectedHit = _rightHit;
        }
        else
        {
            return;
        }

        vehicle.TryEnterWallRide(selectedHit);
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        Vector3 origin =
            rayOrigin.position + Vector3.up * rayHeightOffset;

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            origin,
            origin + transform.right * sideRayDistance
        );

        Gizmos.DrawLine(
            origin,
            origin - transform.right * sideRayDistance
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            origin,
            origin + transform.forward * forwardRayDistance
        );

        Gizmos.DrawSphere(
            origin + transform.right * sideRayDistance,
            0.08f
        );

        Gizmos.DrawSphere(
            origin - transform.right * sideRayDistance,
            0.08f
        );

        Gizmos.DrawSphere(
            origin + transform.forward * forwardRayDistance,
            0.08f
        );
    }
}
