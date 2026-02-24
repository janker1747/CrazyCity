using UnityEngine;
using UnityEngine.AI;
using ArcadeVP;

public class AICarChase : MonoBehaviour
{
    private float stuckTimer;
    private Vector3 lastPosition;
    public float stuckCheckTime = 1.0f;
    public float minMoveDistance = 0.5f;

    private bool isReversing;
    private float reverseTimer;
    public float reverseDuration = 1.5f;

    [Header("References")]
    [Tooltip("Target to chase (e.g. player vehicle root transform).")]
    public Transform target;

    [Tooltip("ArcadeVehicleController on this car. If left empty, will be fetched from the same GameObject.")]
    public ArcadeVehicleController vehicleController;

    [Tooltip("NavMeshAgent used only for path calculation.")]
    public NavMeshAgent agent;

    [Header("Chase behaviour")]
    [Tooltip("How strongly the car steers towards the target.")]
    public float steeringStrength = 1.0f;

    [Tooltip("Distance at which the car begins to slow down.")]
    public float slowDownDistance = 10f;

    [Tooltip("Distance at which the car stops behind the target.")]
    public float stopDistance = 1f;

    [Tooltip("Maximum forward input value the AI will use (0..1).")]
    [Range(0f, 1f)]
    public float maxForwardInput = 1f;

    private void Awake()
    {
        if (vehicleController == null)
        {
            vehicleController = GetComponent<ArcadeVehicleController>();
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        if (target == null || vehicleController == null ||
            vehicleController.carBody == null || agent == null)
        {
            return;
        }

        CheckIfStuck();

        agent.SetDestination(target.position);

        Vector3 nextPoint;

        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            nextPoint = agent.path.corners[1];
        }
        else
        {
            nextPoint = target.position;
        }

        Vector3 localTarget = vehicleController.carBody.transform
            .InverseTransformPoint(nextPoint);

        float distance = Vector3.Distance(vehicleController.carBody.position, nextPoint);

        if (isReversing)
        {
            reverseTimer -= Time.deltaTime;

            if (reverseTimer > 0f)
            {
                float turnDir = Mathf.Sign(localTarget.x);
                ApplyInputs(turnDir, -1f);
                agent.nextPosition = vehicleController.carBody.position;
                return;
            }
            else
            {
                isReversing = false;
            }
        }

        float horizontal = Mathf.Clamp(localTarget.x, -1f, 1f) * steeringStrength;
        float forward = 0f;

        if (distance > stopDistance)
        {
            forward = maxForwardInput;

            if (distance < slowDownDistance)
            {
                float t = Mathf.InverseLerp(stopDistance, slowDownDistance, distance);
                forward *= t;
            }

            if (localTarget.z < 0f)
            {
                forward *= 0.5f;
            }
        }

        ApplyInputs(horizontal, forward);

        agent.nextPosition = vehicleController.carBody.position;
    }

    private void CheckIfStuck()
    {
        if (Vector3.Distance(vehicleController.carBody.position, lastPosition) < minMoveDistance)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer > stuckCheckTime)
            {
                isReversing = true;
                reverseTimer = reverseDuration;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = vehicleController.carBody.position;
    }

    private void ApplyInputs(float horizontal, float forward)
    {
        vehicleController.overrideInput = true;
        vehicleController.overrideHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vehicleController.overrideVertical = Mathf.Clamp(forward, -1f, 1f);
        vehicleController.overrideJump = 0f;
    }
}