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
    [SerializeField] private float cornerLookAheadDistance = 5f;
    [SerializeField] private float directChaseDistance = 8f;
    [SerializeField] private float cachedSteeringLifetime = 1f;
    [SerializeField] private float targetVelocitySmoothing = 6f;

    [Header("Navigation Recovery")]
    [SerializeField] private float navMeshRecoveryDistance = 12f;
    [SerializeField] private float missingPathRecoveryTime = 0.75f;

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
    private float missingPathTimer;
    private float reverseSteer = 1f;
    private bool isReversing;
    private Vector3 lastTargetPosition;
    private Vector3 smoothedTargetVelocity;
    private Vector3 cachedSteeringPoint;
    private float cachedSteeringTimer;
    private NavMeshPath calculatedPath;

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
        missingPathTimer = 0f;
        recoveryCooldownTimer = 0f;
        isReversing = false;
        ResetTargetTracking();
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
        missingPathTimer = 0f;
        isReversing = false;
        ResetTargetTracking();
        ResetStuckCheck();
    }

    private void Update()
    {
        if (!HasRequiredReferences())
            return;

        recoveryCooldownTimer = Mathf.Max(0f, recoveryCooldownTimer - Time.deltaTime);
        cachedSteeringTimer = Mathf.Max(0f, cachedSteeringTimer - Time.deltaTime);
        UpdateTargetMotion();
        SyncAgentWithCar();
        UpdatePath();

        if (isReversing)
        {
            UpdateReverse();
            return;
        }

        if (!TryGetSteeringPoint(out Vector3 steeringPoint))
        {
            RecoverMissingPath();

            if (TryGetFallbackSteeringPoint(out steeringPoint))
            {
                MoveTowards(steeringPoint);
                CheckStuck();
                return;
            }

            ApplyInputs(0f, 0f, 0f);
            return;
        }

        missingPathTimer = 0f;
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
        if (!EnsureAgentOnNavMesh())
            return;

        if (NavMesh.SamplePosition(
                CarTransform.position,
                out NavMeshHit hit,
                navMeshSampleDistance,
                agent.areaMask))
        {
            agent.nextPosition = hit.position;
        }
    }

    private void UpdatePath()
    {
        pathUpdateTimer -= Time.deltaTime;

        if (pathUpdateTimer > 0f || !EnsureAgentOnNavMesh())
            return;

        pathUpdateTimer = Mathf.Max(0.05f, pathUpdateInterval);

        Vector3 predictionDirection =
            smoothedTargetVelocity.sqrMagnitude > 0.25f
                ? smoothedTargetVelocity.normalized
                : target.forward;

        Vector3 predictedTarget =
            target.position + predictionDirection * targetPredictionDistance;

        if (NavMesh.SamplePosition(
                predictedTarget,
                out NavMeshHit hit,
                navMeshSampleDistance,
                agent.areaMask))
        {
            if (TryApplyPath(hit.position, true))
                return;
        }

        // The player may temporarily leave the road. A wider fallback keeps the
        // police heading to the nearest reachable street instead of following an old path.
        if (NavMesh.SamplePosition(
                target.position,
                out hit,
                navMeshRecoveryDistance,
                agent.areaMask))
        {
            TryApplyPath(hit.position, false);
        }
    }

    private bool TryApplyPath(Vector3 destination, bool requireCompletePath)
    {
        NavMeshPath path = GetCalculatedPath();
        path.ClearCorners();

        if (!agent.CalculatePath(destination, path) ||
            path.status == NavMeshPathStatus.PathInvalid ||
            path.corners == null ||
            path.corners.Length < 2)
        {
            return false;
        }

        if (requireCompletePath &&
            path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        if (!agent.SetPath(path))
            return false;

        CacheSteeringPoint(path.corners[1]);
        return true;
    }

    private NavMeshPath GetCalculatedPath()
    {
        if (calculatedPath == null)
            calculatedPath = new NavMeshPath();

        return calculatedPath;
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

        steeringPoint = agent.steeringTarget;
        Vector3[] corners = agent.path.corners;

        // Look ahead only while a direct segment stays on the NavMesh. This smooths
        // harmless bends but never lets the car cut across a building corner.
        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 offset = corners[i] - CarTransform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > cornerLookAheadDistance * cornerLookAheadDistance)
                break;

            if (NavMesh.Raycast(agent.nextPosition, corners[i], out _, agent.areaMask))
                break;

            steeringPoint = corners[i];
        }

        bool hasSteeringPoint =
            (steeringPoint - CarTransform.position).sqrMagnitude > 0.01f;

        if (hasSteeringPoint)
            CacheSteeringPoint(steeringPoint);

        return hasSteeringPoint;
    }

    private bool TryGetFallbackSteeringPoint(out Vector3 steeringPoint)
    {
        steeringPoint = default;

        if (cachedSteeringTimer > 0f)
        {
            Vector3 cachedOffset = cachedSteeringPoint - CarTransform.position;
            cachedOffset.y = 0f;

            if (cachedOffset.sqrMagnitude > 0.25f)
            {
                steeringPoint = cachedSteeringPoint;
                return true;
            }
        }

        Vector3 targetOffset = target.position - CarTransform.position;
        targetOffset.y = 0f;

        if (targetOffset.sqrMagnitude <= directChaseDistance * directChaseDistance)
        {
            steeringPoint = target.position;
            return targetOffset.sqrMagnitude > 0.01f;
        }

        if (!agent.isOnNavMesh)
            return false;

        Vector3 forwardDestination =
            agent.nextPosition +
            Vector3.ProjectOnPlane(CarTransform.forward, Vector3.up).normalized *
            cornerLookAheadDistance;

        if (NavMesh.Raycast(
                agent.nextPosition,
                forwardDestination,
                out NavMeshHit forwardHit,
                agent.areaMask))
        {
            forwardDestination = forwardHit.position;
        }

        Vector3 forwardOffset = forwardDestination - CarTransform.position;
        forwardOffset.y = 0f;

        if (forwardOffset.sqrMagnitude <= 1f)
            return false;

        steeringPoint = forwardDestination;
        return true;
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent.isOnNavMesh)
            return true;

        if (!NavMesh.SamplePosition(
                CarTransform.position,
                out NavMeshHit hit,
                navMeshRecoveryDistance,
                agent.areaMask))
        {
            return false;
        }

        bool warped = agent.Warp(hit.position);

        if (warped)
            pathUpdateTimer = 0f;

        return warped;
    }

    private void RecoverMissingPath()
    {
        missingPathTimer += Time.deltaTime;

        if (missingPathTimer < missingPathRecoveryTime)
            return;

        EnsureAgentOnNavMesh();
        pathUpdateTimer = 0f;
        UpdatePath();

        bool shouldBeMoving = GetRemainingDistance() > stopDistance + 1f;
        if (shouldBeMoving && missingPathTimer >= missingPathRecoveryTime + stuckCheckTime)
            StartReverse();
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
        Vector3 targetOffset = target.position - car.position;
        targetOffset.y = 0f;

        if (remainingDistance <= stopDistance &&
            targetOffset.magnitude > stopDistance)
        {
            remainingDistance = targetOffset.magnitude;
        }

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
            missingPathTimer = 0f;
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

    private void ResetTargetTracking()
    {
        lastTargetPosition = target != null ? target.position : Vector3.zero;
        smoothedTargetVelocity = Vector3.zero;
        cachedSteeringTimer = 0f;
    }

    private void UpdateTargetMotion()
    {
        if (target == null || Time.deltaTime <= 0f)
            return;

        Vector3 measuredVelocity =
            (target.position - lastTargetPosition) / Time.deltaTime;

        measuredVelocity.y = 0f;

        float blend =
            1f - Mathf.Exp(-Mathf.Max(0f, targetVelocitySmoothing) * Time.deltaTime);

        smoothedTargetVelocity =
            Vector3.Lerp(smoothedTargetVelocity, measuredVelocity, blend);

        lastTargetPosition = target.position;
    }

    private void CacheSteeringPoint(Vector3 steeringPoint)
    {
        cachedSteeringPoint = steeringPoint;
        cachedSteeringTimer = Mathf.Max(0f, cachedSteeringLifetime);
    }

    private void ApplyInputs(float horizontal, float forward, float handbrake)
    {
        vehicleController.overrideInput = true;
        vehicleController.overrideHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vehicleController.overrideVertical = Mathf.Clamp(forward, -1f, 1f);
        vehicleController.overrideJump = Mathf.Clamp01(handbrake);
    }
}
