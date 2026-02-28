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

    [Header("Turn on spot")]
    [Tooltip("If angle to target is greater than this, car will attempt to turn on spot (if speed is low).")]
    public float turnOnSpotAngleThreshold = 120f;

    [Tooltip("Speed below which car will consider turning on spot.")]
    public float turnOnSpotSpeedThreshold = 2f;

    [Tooltip("Maximum duration of turning on spot before giving up.")]
    public float turnOnSpotMaxDuration = 2f;

    [Tooltip("Angle difference at which car exits turn-on-spot mode.")]
    public float turnOnSpotExitAngle = 20f;

    private bool isTurningOnSpot;
    private float turnOnSpotTimer;
    private float turnDirection; // 1 = right, -1 = left

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
            return;

        // Проверка застревания с исключением для близкой цели
        CheckIfStuck();

        agent.SetDestination(target.position);

        // --- Reverse logic (priority) ---
        if (isReversing)
        {
            reverseTimer -= Time.deltaTime;
            if (reverseTimer > 0f)
            {
                float turnDir = Mathf.Sign(vehicleController.carBody.transform
                    .InverseTransformPoint(target.position).x);
                ApplyInputs(turnDir, -1f, 0f);
                agent.nextPosition = vehicleController.carBody.position;
                return;
            }
            else
            {
                isReversing = false;
            }
        }

        // --- Turn on spot logic ---
        if (isTurningOnSpot)
        {
            if (HandleTurnOnSpotAndApply())
                return;
        }

        if (!isTurningOnSpot && ShouldTurnOnSpot())
        {
            StartTurnOnSpot();
            if (isTurningOnSpot)
            {
                HandleTurnOnSpotAndApply();
                return;
            }
        }

        // --- Нормальное преследование с тараном ---
        Vector3 nextPoint;
        if (agent.hasPath && agent.path.corners.Length > 1)
            nextPoint = agent.path.corners[1];
        else
            nextPoint = target.position;

        Vector3 localTarget = vehicleController.carBody.transform.InverseTransformPoint(nextPoint);
        float distance = Vector3.Distance(vehicleController.carBody.position, nextPoint);
        float distToTarget = Vector3.Distance(vehicleController.carBody.position, target.position);

        // Управление: всегда пытаемся ехать вперёд, если цель не сзади
        float horizontal = Mathf.Clamp(localTarget.x, -1f, 1f) * steeringStrength;
        float forward = maxForwardInput;

        // Небольшое замедление на дальних подходах (опционально)
        if (distance < slowDownDistance)
        {
            float t = Mathf.InverseLerp(0f, slowDownDistance, distance);
            forward *= Mathf.Lerp(0.5f, 1f, t); // даже на минимальной дистанции не ниже 0.5
        }

        // Если цель сзади, сбрасываем газ (чтобы не уехать в другую сторону)
        if (localTarget.z < 0f)
        {
            forward *= 0.3f; // немного газа для возможного разворота
        }

        // Если игрок совсем рядом – газ на полную, чтобы таранить
        if (distToTarget < 2f)
            forward = maxForwardInput;

        ApplyInputs(horizontal, forward, 0f);
        agent.nextPosition = vehicleController.carBody.position;
    }


    private void CheckIfStuck()
    {
        if (isReversing || isTurningOnSpot) return;

        float distToTarget = Vector3.Distance(vehicleController.carBody.position, target.position);
        if (distToTarget < 2f) // рядом с игроком – не считаем застреванием
        {
            stuckTimer = 0f;
            lastPosition = vehicleController.carBody.position;
            return;
        }

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

    private bool ShouldTurnOnSpot()
    {
        if (isReversing || isTurningOnSpot) return false;

        Vector3 toTarget = (target.position - vehicleController.carBody.position).normalized;
        float angle = Vector3.Angle(vehicleController.carBody.transform.forward, toTarget);
        float speed = vehicleController.carBody.velocity.magnitude;

        // Не включаем разворот, если игрок уже близко – просто тараним
        float distToTarget = Vector3.Distance(vehicleController.carBody.position, target.position);
        if (distToTarget < 5f) return false;

        return angle > turnOnSpotAngleThreshold && speed < turnOnSpotSpeedThreshold;
    }

    private void StartTurnOnSpot()
    {
        isTurningOnSpot = true;
        turnOnSpotTimer = 0f;

        // Determine turn direction using local space
        Vector3 toTarget = (target.position - vehicleController.carBody.position).normalized;
        Vector3 localDir = vehicleController.carBody.transform.InverseTransformDirection(toTarget);
        turnDirection = Mathf.Sign(localDir.x); // positive = target on right -> turn right
    }

    private bool HandleTurnOnSpotAndApply()
    {
        turnOnSpotTimer += Time.deltaTime;

        Vector3 toTarget = (target.position - vehicleController.carBody.position).normalized;
        float angle = Vector3.Angle(vehicleController.carBody.transform.forward, toTarget);

        if (angle < turnOnSpotExitAngle || turnOnSpotTimer > turnOnSpotMaxDuration)
        {
            isTurningOnSpot = false;
            return false;
        }

        // Во время разворота даём чуть больше газа, чтобы не стоять
        ApplyInputs(turnDirection, 0.3f, 0.5f);
        agent.nextPosition = vehicleController.carBody.position;
        return true;
    }

    private void ApplyInputs(float horizontal, float forward, float handbrake)
    {
        vehicleController.overrideInput = true;
        vehicleController.overrideHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vehicleController.overrideVertical = Mathf.Clamp(forward, -1f, 1f);
        vehicleController.overrideJump = Mathf.Clamp(handbrake, 0f, 1f);
    }
}