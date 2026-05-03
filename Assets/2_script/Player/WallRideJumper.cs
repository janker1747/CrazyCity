// WallRideJumper.cs - обновленная версия
using UnityEngine;
using ArcadeVP;

public class WallRideJumper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArcadeVehicleController vehicle;
    [SerializeField] private Transform rayOrigin;

    [Header("Wall detection")]
    [SerializeField] private LayerMask wallRideLayer;
    [SerializeField] private float rayDistance = 3f;
    [SerializeField] private float rayHeightOffset = 0.5f;

    [Header("Input")]
    [SerializeField] private KeyCode wallRideKey = KeyCode.LeftShift;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRays = true;

    private RaycastHit _leftHit;
    private RaycastHit _rightHit;

    private bool _hasLeftWall;
    private bool _hasRightWall;

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
            TryStartWallRide();
    }

    private void ScanWalls()
    {
        Vector3 origin = rayOrigin.position + Vector3.up * rayHeightOffset;
        Vector3 leftDir = -transform.right;
        Vector3 rightDir = transform.right;

        _hasLeftWall = Physics.Raycast(origin, leftDir, out _leftHit, rayDistance, wallRideLayer);
        _hasRightWall = Physics.Raycast(origin, rightDir, out _rightHit, rayDistance, wallRideLayer);

        if (drawDebugRays)
        {
            Debug.DrawRay(origin, leftDir * rayDistance, _hasLeftWall ? Color.green : Color.red);
            Debug.DrawRay(origin, rightDir * rayDistance, _hasRightWall ? Color.green : Color.red);
        }
    }

    private void TryStartWallRide()
    {
        RaycastHit selectedHit;

        if (_hasLeftWall && _hasRightWall)
            selectedHit = _leftHit.distance <= _rightHit.distance ? _leftHit : _rightHit;
        else if (_hasLeftWall)
            selectedHit = _leftHit;
        else if (_hasRightWall)
            selectedHit = _rightHit;
        else
            return;

        vehicle.TryEnterWallRide(selectedHit);
    }

    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        Vector3 origin = rayOrigin.position + Vector3.up * rayHeightOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + transform.right * rayDistance);
        Gizmos.DrawLine(origin, origin - transform.right * rayDistance);

        Gizmos.DrawSphere(origin + transform.right * rayDistance, 0.08f);
        Gizmos.DrawSphere(origin - transform.right * rayDistance, 0.08f);
    }
}