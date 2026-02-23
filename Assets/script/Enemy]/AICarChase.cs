using UnityEngine;
using ArcadeVP;

public class AICarChase : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Target to chase (e.g. player vehicle root transform).")]
    public Transform target;

    [Tooltip("ArcadeVehicleController on this car. If left empty, will be fetched from the same GameObject.")]
    public ArcadeVehicleController vehicleController;

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
    }

    private void Update()
    {
        if (target == null || vehicleController == null || vehicleController.carBody == null)
        {
            return;
        }

        Vector3 toTargetWorld = target.position - vehicleController.carBody.position;
        float distance = toTargetWorld.magnitude;

        if (distance < 0.1f)
        {
            ApplyInputs(0f, 0f);
            return;
        }

        Vector3 localTarget = vehicleController.carBody.transform.InverseTransformPoint(target.position);

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
    }

    private void ApplyInputs(float horizontal, float forward)
    {
        vehicleController.overrideInput = true;
        vehicleController.overrideHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vehicleController.overrideVertical = Mathf.Clamp(forward, -1f, 1f);
        vehicleController.overrideJump = 0f; 
    }
}

