using ArcadeVP;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private float turnSlowDownAngle = 70f;
    [SerializeField, Range(0.1f, 1f)] private float minTurnInput = 0.3f;

    [Header("Path")]
    [SerializeField] private float pathUpdateInterval = 0.25f;
    [SerializeField] private float targetPredictionDistance = 2f;
    [SerializeField] private float navMeshSampleDistance = 6f;
    [SerializeField] private float steeringSmoothness = 4f;

    [Header("Stuck Handling")]
    [SerializeField] private float stuckCheckTime = 1.5f;
    [SerializeField] private float minMoveDistance = 0.8f;
    [SerializeField] private float reverseDuration = 1.2f;
    [SerializeField] private float recoveryCooldown = 1f;

    private Vector3 lastPosition;
    private float stuckTimer;
    private float reverseTimer;
    private float recoveryCooldownTimer;
    private float currentSteer;
    private float pathUpdateTimer;
    private float reverseSteer = 1f;
    private bool isReversing;

    private Transform CarTransform => vehicleController.carBody.transform;

    private void Awake()
    {
        if (vehicleController == null)
            vehicleController = GetComponent<ArcadeVehicleController>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        ResetStuckCheck();
    }

    private void OnEnable()
    {
        pathUpdateTimer = 0f;
        recoveryCooldownTimer = 0f;
        isReversing = false;
        ResetStuckCheck();
    }

    private void OnDisable()
    {
        if (vehicleController == null)
            return;

        ApplyInputs(0f, 0f, 0f);
    }

    public void Initialize(Transform chaseTarget)
    {
        target = chaseTarget;
        pathUpdateTimer = 0f;
        isReversing = false;
        ResetStuckCheck();
    }

    private void Update()
    {
        if (!HasRequiredReferences())
            return;

        recoveryCooldownTimer = Mathf.Max(0f, recoveryCooldownTimer - Time.deltaTime);
        SyncAgentWithCar();
        UpdatePath();

        if (isReversing)
        {
            UpdateReverse();
            return;
        }

        if (!TryGetSteeringPoint(out Vector3 steeringPoint))
        {
            ApplyInputs(0f, 0f, 0f);
            ResetStuckCheck();
            return;
        }

        MoveTowards(steeringPoint);
        CheckStuck();
    }

    private bool HasRequiredReferences()
    {
        return target != null &&
               vehicleController != null &&
               vehicleController.carBody != null &&
               agent != null;
    }

    private void SyncAgentWithCar()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.nextPosition = CarTransform.position;
    }

    private void UpdatePath()
    {
        pathUpdateTimer -= Time.deltaTime;

        if (pathUpdateTimer > 0f || !agent.isOnNavMesh)
            return;

        pathUpdateTimer = Mathf.Max(0.05f, pathUpdateInterval);

        Vector3 predictedTarget =
            target.position + target.forward * targetPredictionDistance;

        if (NavMesh.SamplePosition(
                predictedTarget,
                out NavMeshHit hit,
                navMeshSampleDistance,
                agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }
    }

    private bool TryGetSteeringPoint(out Vector3 steeringPoint)
    {
        steeringPoint = default;

        if (!agent.isOnNavMesh ||
            agent.pathPending ||
            !agent.hasPath ||
            agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }

        // steeringTarget is the nearest path corner. Using a later corner makes cars cut through buildings.
        steeringPoint = agent.steeringTarget;
        return (steeringPoint - CarTransform.position).sqrMagnitude > 0.01f;
    }

    private void MoveTowards(Vector3 steeringPoint)
    {
        Transform car = CarTransform;
        Vector3 direction = Vector3.ProjectOnPlane(
            steeringPoint - car.position,
            Vector3.up);

        if (direction.sqrMagnitude < 0.01f)
        {
            ApplyInputs(0f, 0f, 0f);
            return;
        }

        float angle = Vector3.SignedAngle(car.forward, direction.normalized, Vector3.up);
        float targetSteer = Mathf.Clamp(angle / 45f, -1f, 1f) * steeringStrength;
        currentSteer = Mathf.MoveTowards(
            currentSteer,
            targetSteer,
            steeringSmoothness * Time.deltaTime);

        float remainingDistance = GetRemainingDistance();
        float forwardInput = CalculateForwardInput(remainingDistance, Mathf.Abs(angle));

        ApplyInputs(currentSteer, forwardInput, 0f);
    }

    private float GetRemainingDistance()
    {
        if (agent.isOnNavMesh &&
            agent.hasPath &&
            !float.IsInfinity(agent.remainingDistance))
        {
            return agent.remainingDistance;
        }

        Vector3 offset = target.position - CarTransform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float CalculateForwardInput(float remainingDistance, float turnAngle)
    {
        if (remainingDistance <= stopDistance)
            return 0f;

        float distanceFactor = Mathf.InverseLerp(stopDistance, slowDownDistance, remainingDistance);
        float turnFactor = Mathf.InverseLerp(180f, turnSlowDownAngle, turnAngle);
        float throttleFactor = Mathf.Min(
            Mathf.Lerp(0.35f, 1f, distanceFactor),
            Mathf.Lerp(minTurnInput, 1f, turnFactor));

        return maxForwardInput * throttleFactor;
    }

    private void CheckStuck()
    {
        if (recoveryCooldownTimer > 0f)
        {
            ResetStuckCheck();
            return;
        }

        stuckTimer += Time.deltaTime;

        if (stuckTimer < stuckCheckTime)
            return;

        float movedDistance = Vector3.Distance(CarTransform.position, lastPosition);
        bool shouldBeMoving = GetRemainingDistance() > stopDistance + 1f;

        if (shouldBeMoving && movedDistance < minMoveDistance)
            StartReverse();

        ResetStuckCheck();
    }

    public void HandleReverse()
    {
        if (!HasRequiredReferences())
            return;

        StartReverse();
    }

    private void StartReverse()
    {
        if (isReversing || recoveryCooldownTimer > 0f)
            return;

        isReversing = true;
        reverseTimer = reverseDuration;
        currentSteer = 0f;

        Vector3 recoveryPoint = target.position;
        if (TryGetSteeringPoint(out Vector3 steeringPoint))
            recoveryPoint = steeringPoint;

        float localSide = CarTransform.InverseTransformPoint(recoveryPoint).x;
        reverseSteer = Mathf.Abs(localSide) > 0.25f
            ? -Mathf.Sign(localSide)
            : -reverseSteer;
    }

    private void UpdateReverse()
    {
        reverseTimer -= Time.deltaTime;

        if (reverseTimer <= 0f)
        {
            isReversing = false;
            recoveryCooldownTimer = recoveryCooldown;
            pathUpdateTimer = 0f;
            ResetStuckCheck();
            return;
        }

        ApplyInputs(reverseSteer, -1f, 0f);
    }

    private void ResetStuckCheck()
    {
        stuckTimer = 0f;

        if (vehicleController != null && vehicleController.carBody != null)
            lastPosition = vehicleController.carBody.position;
        else
            lastPosition = transform.position;
    }

    private void ApplyInputs(float horizontal, float forward, float handbrake)
    {
        vehicleController.overrideInput = true;
        vehicleController.overrideHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vehicleController.overrideVertical = Mathf.Clamp(forward, -1f, 1f);
        vehicleController.overrideJump = Mathf.Clamp01(handbrake);
    }
}
