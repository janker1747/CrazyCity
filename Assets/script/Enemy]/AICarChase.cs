using UnityEngine;
using UnityEngine.AI;
using ArcadeVP;

public class AICarChase : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public ArcadeVehicleController vehicleController;
    public NavMeshAgent agent;

    [Header("Chase")]
    public float steeringStrength = 1f;
    public float maxForwardInput = 1f;
    public float slowDownDistance = 10f;
    public float stopDistance = 1.5f;

    [Header("Stuck Handling")]
    public float stuckCheckTime = 1.5f;
    public float minMoveDistance = 0.3f;
    public float reverseDuration = 1.2f;

    private Vector3 lastPosition;
    private float stuckTimer;

    private bool isReversing;
    private float reverseTimer;

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

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (target == null || vehicleController == null || agent == null)
            return;

        if (vehicleController.carBody == null)
            return;

        CheckStuck();

        // Обновляем путь
        agent.SetDestination(target.position);

        // Если reversing — приоритет
        if (isReversing)
        {
            HandleReverse();
            return;
        }

        // Проверка валидности пути
        bool hasValidPath =
            agent.hasPath &&
            agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.path.corners.Length > 1;

        Vector3 nextPoint;

        if (hasValidPath)
            nextPoint = agent.path.corners[1];
        else
            nextPoint = target.position;

        MoveTowards(nextPoint);
    }

    private void MoveTowards(Vector3 nextPoint)
    {
        Transform carTransform = vehicleController.carBody.transform;

        Vector3 localTarget = carTransform.InverseTransformPoint(nextPoint);

        float distanceToTarget = Vector3.Distance(carTransform.position, target.position);

        float horizontal = Mathf.Clamp(localTarget.x, -1f, 1f) * steeringStrength;

        float forward = maxForwardInput;

        // Замедление
        float distance = Vector3.Distance(carTransform.position, nextPoint);
        if (distance < slowDownDistance)
        {
            float t = Mathf.InverseLerp(0f, slowDownDistance, distance);
            forward *= Mathf.Lerp(0.5f, 1f, t);
        }

        // Если цель сбоку/сзади — слегка корректируем
        if (localTarget.z < 0f)
        {
            forward *= 0.4f;
        }

        // Если близко — таран
        if (distanceToTarget < stopDistance)
        {
            forward = maxForwardInput;
        }

        ApplyInputs(horizontal, forward, 0f);

        // синхронизация NavMesh с Rigidbody
        agent.nextPosition = vehicleController.carBody.position;
    }

    private void HandleReverse()
    {
        reverseTimer -= Time.deltaTime;

        if (reverseTimer <= 0f)
        {
            isReversing = false;
            return;
        }

        Transform carTransform = vehicleController.carBody.transform;

        Vector3 localTarget = carTransform.InverseTransformPoint(target.position);
        float turn = Mathf.Sign(localTarget.x);

        ApplyInputs(turn, -1f, 0f);

        agent.nextPosition = vehicleController.carBody.position;
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