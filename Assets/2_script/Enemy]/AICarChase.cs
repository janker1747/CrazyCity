using UnityEngine;
using UnityEngine.AI;
using ArcadeVP;

public class AICarChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private NavMeshAgent agent;

    [Header("Chase")]
    [SerializeField] private float steeringStrength = 1f;
    [SerializeField] private float maxForwardInput = 1f;
    [SerializeField] private float slowDownDistance = 10f;
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Path")]
    [SerializeField] private float pathUpdateInterval = 0.2f;
    [SerializeField] private int lookAheadCornerIndex = 2;
    [SerializeField] private float steeringSmoothness = 3f;

    [Header("Stuck Handling")]
    [SerializeField] private float stuckCheckTime = 1.5f;
    [SerializeField] private float minMoveDistance = 0.3f;
    [SerializeField] private float reverseDuration = 1.2f;

    private Vector3 lastPosition;
    private float stuckTimer;

    private bool isReversing;
    private float reverseTimer;

    private float currentSteer;
    private float pathUpdateTimer;

    private float modeBlend = 0f;
    private float modeBlendSpeed = 3f;


    private void Awake()
    {
        if (vehicleController == null)
            vehicleController = GetComponent<ArcadeVehicleController>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;

        lastPosition = transform.position;
    }

    public void Initialize(Transform chaseTarget)
    {
        target = chaseTarget;
    }

    private void Update()
    {
        if (target == null || vehicleController == null || agent == null)
            return;

        modeBlend = Mathf.Lerp(modeBlend, isReversing ? 1f : 0f, Time.deltaTime * modeBlendSpeed);

        CheckStuck();

        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = pathUpdateInterval;

            Vector3 predicted = target.position + target.forward * 3f;
            agent.SetDestination(predicted);
        }

        if (isReversing)
        {
            HandleReverse();
            return;
        }

        Vector3 nextPoint = GetSmoothedPoint();
        MoveTowards(nextPoint);
    }

    private Vector3 GetSmoothedPoint()
    {
        if (!agent.hasPath || agent.path.corners.Length < 2)
            return target.position;

        Vector3[] c = agent.path.corners;

        int index = Mathf.Min(lookAheadCornerIndex, c.Length - 1);
        int prev = Mathf.Max(0, index - 1);

        return Vector3.Lerp(c[prev], c[index], 0.5f);
    }

    private void MoveTowards(Vector3 nextPoint)
    {
        Transform car = vehicleController.carBody.transform;

        Vector3 dir = (nextPoint - car.position).normalized;
        float angle = Vector3.SignedAngle(car.forward, dir, Vector3.up);

        float targetSteer = Mathf.Clamp(angle / 45f, -1f, 1f) * steeringStrength;

        currentSteer = Mathf.Lerp(currentSteer, targetSteer, Time.deltaTime * steeringSmoothness);

        float dist = Vector3.Distance(car.position, nextPoint);

        float forward = maxForwardInput;

        if (dist < slowDownDistance)
        {
            float t = Mathf.InverseLerp(0f, slowDownDistance, dist);
            forward *= Mathf.Lerp(0.4f, 1f, t);
        }

        float blendedForward = Mathf.Lerp(forward, -1f, modeBlend);

        ApplyInputs(currentSteer, blendedForward, 0f);

        agent.nextPosition = car.position;
    }

    public void HandleReverse()
    {
        reverseTimer -= Time.deltaTime;

        if (reverseTimer <= 0f)
        {
            isReversing = false;
            return;
        }

        Transform car = vehicleController.carBody.transform;

        Vector3 localTarget = car.InverseTransformPoint(target.position);
        float turn = Mathf.Sign(localTarget.x);

        ApplyInputs(turn, -1f, 0f);

        agent.nextPosition = car.position;
    }

    private void CheckStuck()
    {
        if (isReversing)
            return;

        float moved = Vector3.Distance(vehicleController.carBody.position, lastPosition);

        if (moved < minMoveDistance)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckCheckTime)
            {
                StartReverse();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = vehicleController.carBody.position;
    }

    private void StartReverse()
    {
        isReversing = true;
        reverseTimer = reverseDuration;
    }

    private void ApplyInputs(float horizontal, float forward, float handbrake)
    {
        vehicleController.overrideInput = true;
        vehicleController.overrideHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vehicleController.overrideVertical = Mathf.Clamp(forward, -1f, 1f);
        vehicleController.overrideJump = Mathf.Clamp(handbrake, 0f, 1f);
    }
}
