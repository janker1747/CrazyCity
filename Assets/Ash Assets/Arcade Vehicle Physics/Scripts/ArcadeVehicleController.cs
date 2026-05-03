using System;
using UnityEngine;

namespace ArcadeVP
{
    public class ArcadeVehicleController : MonoBehaviour
    {
        public event Action<bool> OnGrounded;
        public event Action<float> OnSpeedChanged;

        public enum groundCheck { rayCast, sphereCaste }
        public enum MovementMode { Velocity, AngularVelocity }
        private enum WallRideState { None, Entering, Riding }

        [Header("Movement")]
        public MovementMode movementMode;
        public groundCheck GroundCheck;
        public LayerMask drivableSurface;
        public float MaxSpeed = 30f;
        public float accelaration = 10f;
        public float turn = 5f;
        public float gravity = 7f;
        public float downforce = 5f;

        [Tooltip("if true : can turn vehicle in air")]
        public bool AirControl = false;

        [Tooltip("if true : vehicle will drift instead of brake while holding space")]
        public bool kartLike = false;

        [Tooltip("turn more while drifting (while holding space) only if kart Like is true")]
        public float driftMultiplier = 1.5f;

        [Header("References")]
        public Rigidbody rb;
        public Rigidbody carBody;

        [HideInInspector] public RaycastHit hit;

        [Header("Curves / Physics")]
        public AnimationCurve frictionCurve;
        public AnimationCurve turnCurve;
        public PhysicMaterial frictionMaterial;

        [Header("Visuals")]
        public Transform BodyMesh;
        public Transform[] FrontWheels = new Transform[2];
        public Transform[] RearWheels = new Transform[2];

        [HideInInspector] public Vector3 carVelocity;
        [HideInInspector] public bool allowAutoAlign = true;

        [Range(0, 10)]
        public float BodyTilt = 2f;

        [Header("Audio settings")]
        public AudioSource engineSound;
        [Range(0, 1)] public float minPitch = 0.8f;
        [Range(1, 3)] public float MaxPitch = 2f;
        public AudioSource SkidSound;

        [HideInInspector] public float skidWidth;

        [Header("Input override (AI)")]
        [Tooltip("If true, movement uses override values instead of player Input axes")]
        public bool overrideInput = false;
        [Range(-1f, 1f)] public float overrideHorizontal = 0f;
        [Range(-1f, 1f)] public float overrideVertical = 0f;
        [Range(0f, 1f)] public float overrideJump = 0f;

        [Header("Wall Ride")]
        [SerializeField] private bool enableWallRide = true;
        [SerializeField] private LayerMask wallRideLayer;
        [SerializeField] private float wallCheckDistance = 3.5f;
        [SerializeField] private float wallRideDuration = 2.5f;

        [Header("Wall Ride Entry")]
        [SerializeField] private float wallLaunchUpImpulse = 7f;
        [SerializeField] private float wallLaunchToWallImpulse = 4f;
        [SerializeField] private float wallLaunchForwardImpulse = 5f;
        [SerializeField] private float wallAttachDelay = 0.18f;

        [Header("Wall Ride Stick")]
        [SerializeField] private float wallStickForce = 35f;
        [SerializeField] private float wallGravityCompensation = 12f;
        [SerializeField] private float wallAlignSpeed = 10f;
        [SerializeField] private float wallDetachCooldown = 0.15f;

        private float radius;
        private float horizontalInput;
        private float verticalInput;
        private float jumpInput;
        private Vector3 origin;

        private float _currentSpeed;
        private RigidbodyConstraints _currentConstraints = RigidbodyConstraints.None;
        private SphereCollider _sphereCollider;

        private WallRideState _wallRideState = WallRideState.None;
        private Vector3 _wallNormal = Vector3.up;
        private float _wallRideTimer;
        private float _wallAttachTimer;
        private float _wallDetachTimer;

        private Vector3 _currentUp = Vector3.up;
        private bool _isWallRideSurfaceUnderVehicle;

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
            if (carBody == null)
                carBody = rb;
        }

        private void Awake()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (carBody == null)
                carBody = rb;

            if (rb != null)
                _sphereCollider = rb.GetComponent<SphereCollider>();

            if (_sphereCollider != null)
                radius = _sphereCollider.radius;
        }

        private void Start()
        {
            if (rb == null)
            {
                Debug.LogError($"{nameof(ArcadeVehicleController)} on {name}: Rigidbody rb is not assigned.");
                enabled = false;
                return;
            }

            if (carBody == null)
            {
                Debug.LogError($"{nameof(ArcadeVehicleController)} on {name}: Rigidbody carBody is not assigned.");
                enabled = false;
                return;
            }

            if (_sphereCollider == null)
            {
                Debug.LogError($"{nameof(ArcadeVehicleController)} on {name}: SphereCollider on rb is required.");
                enabled = false;
                return;
            }

            radius = _sphereCollider.radius;

            if (movementMode == MovementMode.AngularVelocity)
                Physics.defaultMaxAngularSpeed = 100f;
        }

        private void Update()
        {
            ReadInput();
            Visuals();
            AudioManager();
        }

        private void FixedUpdate()
        {
            if (rb == null || carBody == null)
                return;

            UpdateUpVector();

            carVelocity = carBody.transform.InverseTransformDirection(carBody.velocity);

            float speed = carVelocity.magnitude;
            if (Mathf.Abs(speed - _currentSpeed) > 0.1f)
            {
                _currentSpeed = speed;
                OnSpeedChanged?.Invoke(_currentSpeed);
            }

            if (Mathf.Abs(carVelocity.x) > 0f && frictionMaterial != null && frictionCurve != null)
            {
                frictionMaterial.dynamicFriction = frictionCurve.Evaluate(Mathf.Abs(carVelocity.x / 100f));
            }

            UpdateWallRideState();

            if (_wallRideState == WallRideState.Entering)
            {
                return;
            }

            if (grounded())
            {
                GroundedMovement();
            }
            else
            {
                AirMovement();
            }
        }

        private void ReadInput()
        {
            if (overrideInput)
            {
                horizontalInput = overrideHorizontal;
                verticalInput = overrideVertical;
                jumpInput = overrideJump;
            }
            else
            {
                horizontalInput = Input.GetAxis("Horizontal");
                verticalInput = Input.GetAxis("Vertical");
                jumpInput = Input.GetAxis("Jump");
            }
        }

        private void UpdateUpVector()
        {
            if (_wallRideState == WallRideState.Riding)
            {
                _currentUp = _wallNormal;
            }
            else
            {
                _currentUp = Vector3.up;
            }
        }

        public void TryEnterWallRide(RaycastHit wallHit)
        {
            if (!enableWallRide)
                return;

            if (_wallRideState != WallRideState.None)
                return;

            if (_wallDetachTimer > 0f)
                return;

            _wallNormal = wallHit.normal.normalized;
            _wallRideTimer = wallRideDuration;
            _wallAttachTimer = wallAttachDelay;
            _wallRideState = WallRideState.Entering;

            Vector3 velocity = rb.velocity;
            Vector3 awayFromWall = Vector3.Project(velocity, _wallNormal);
            rb.velocity = velocity - awayFromWall;

            Vector3 launchImpulse =
                Vector3.up * wallLaunchUpImpulse +
                (-_wallNormal) * wallLaunchToWallImpulse +
                transform.forward * wallLaunchForwardImpulse;

            rb.AddForce(launchImpulse, ForceMode.Impulse);
        }

        private void UpdateWallRideState()
{
    if (_wallDetachTimer > 0f)
        _wallDetachTimer -= Time.fixedDeltaTime;

    if (_wallRideState == WallRideState.None)
        return;

    _wallRideTimer -= Time.fixedDeltaTime;
    if (_wallRideTimer <= 0f)
    {
        ExitWallRide();
        return;
    }

    Vector3 wallRayOrigin = rb.position + Vector3.up * 0.5f;

    bool hasWall = Physics.Raycast(
        wallRayOrigin,
        -_wallNormal,
        out RaycastHit wallHit,
        wallCheckDistance,
        wallRideLayer);

    if (hasWall)
        _wallNormal = wallHit.normal.normalized;

    if (_wallRideState == WallRideState.Entering)
    {
        _wallAttachTimer -= Time.fixedDeltaTime;

        float enterT = 1f - Mathf.Exp(-wallAlignSpeed * Time.fixedDeltaTime);
        UpdateWallRideRotation(enterT);

        if (_wallAttachTimer <= 0f)
        {
            if (!hasWall)
            {
                ExitWallRide();
                return;
            }

            _wallRideState = WallRideState.Riding;
        }

        return;
    }

    // Фаза езды по стене
    if (!hasWall)
    {
        ExitWallRide();
        return;
    }

    // Прижимаем к стене
    rb.AddForce(-_wallNormal * wallStickForce * rb.mass, ForceMode.Force);

    // Компенсируем сползание вниз.
    // Важно: используем Vector3.up, а не _currentUp,
    // потому что _currentUp на стене = normal стены.
    rb.AddForce(Vector3.up * wallGravityCompensation * rb.mass, ForceMode.Force);

    // Минимальная скорость вдоль стены, чтобы машина не начала сразу скользить вниз
    Vector3 wallForward = Vector3.ProjectOnPlane(carBody.transform.forward, _wallNormal).normalized;

    if (wallForward.sqrMagnitude > 0.001f)
    {
        Vector3 velocityOnWall = Vector3.ProjectOnPlane(rb.velocity, _wallNormal);
        float forwardSpeed = Vector3.Dot(velocityOnWall, wallForward);

        float minWallSpeed = 8f;

        if (forwardSpeed < minWallSpeed)
        {
            Vector3 targetVelocity = velocityOnWall + wallForward * (minWallSpeed - forwardSpeed);

            rb.velocity = Vector3.MoveTowards(
                rb.velocity,
                targetVelocity,
                25f * Time.fixedDeltaTime);
        }
    }

    float rideT = 1f - Mathf.Exp(-wallAlignSpeed * Time.fixedDeltaTime);
    UpdateWallRideRotation(rideT);
}

        private void UpdateWallRideRotation(float blendToWall)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, _currentUp).normalized;

            if (projectedForward.sqrMagnitude < 0.001f)
                projectedForward = Vector3.Cross(transform.right, _currentUp).normalized;

            Vector3 targetUp = Vector3.Slerp(transform.up, _currentUp, blendToWall);
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, targetUp);

            float t = 1f - Mathf.Exp(-wallAlignSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                t));

            if (carBody != null && carBody != rb)
            {
                carBody.MoveRotation(Quaternion.Slerp(
                    carBody.rotation,
                    targetRotation,
                    wallAlignSpeed * Time.fixedDeltaTime));
            }
        }

        private void ExitWallRide()
        {
            _wallRideState = WallRideState.None;
            _currentUp = Vector3.up;
            _wallDetachTimer = wallDetachCooldown;
        }

        private void GroundedMovement()
        {
            float sign = Mathf.Sign(carVelocity.z);
            float turnMultiplier = 1f;

            if (turnCurve != null)
                turnMultiplier = turnCurve.Evaluate(carVelocity.magnitude / Mathf.Max(MaxSpeed, 0.01f));

            if (kartLike && jumpInput > 0.1f)
                turnMultiplier *= driftMultiplier;

            Vector3 torqueAxis = _currentUp;

            if (verticalInput > 0.1f || carVelocity.z > 1f)
            {
                carBody.AddTorque(torqueAxis * horizontalInput * sign * turn * 100f * turnMultiplier);
            }
            else if (verticalInput < -0.1f || carVelocity.z < -1f)
            {
                carBody.AddTorque(torqueAxis * horizontalInput * sign * turn * 100f * turnMultiplier);
            }

            if (!kartLike)
            {
                RigidbodyConstraints target = jumpInput > 0.1f
                    ? RigidbodyConstraints.FreezeRotationX
                    : RigidbodyConstraints.None;

                if (_currentConstraints != target)
                {
                    rb.constraints = target;
                    _currentConstraints = target;
                }
            }

            Vector3 driveForward = Vector3.ProjectOnPlane(carBody.transform.forward, _currentUp).normalized;
            if (driveForward.sqrMagnitude < 0.001f)
                driveForward = transform.forward;

            if (movementMode == MovementMode.AngularVelocity)
            {
                if (Mathf.Abs(verticalInput) > 0.1f && (kartLike || jumpInput < 0.1f))
                {
                    rb.angularVelocity = Vector3.Lerp(
                        rb.angularVelocity,
                        carBody.transform.right * verticalInput * MaxSpeed / Mathf.Max(radius, 0.01f),
                        accelaration * Time.deltaTime);
                }
            }
            else if (movementMode == MovementMode.Velocity)
            {
                if (Mathf.Abs(verticalInput) > 0.1f && (kartLike || jumpInput < 0.1f))
                {
                    rb.velocity = Vector3.Lerp(
                        rb.velocity,
                        driveForward * verticalInput * MaxSpeed,
                        (accelaration / 10f) * Time.deltaTime);
                }
            }

            rb.AddForce(-_currentUp * downforce * rb.mass, ForceMode.Force);

            carBody.MoveRotation(Quaternion.Slerp(
                carBody.rotation,
                Quaternion.FromToRotation(carBody.transform.up, _currentUp) * carBody.transform.rotation,
                0.12f));
        }

        private void AirMovement()
        {
            if (_wallRideState == WallRideState.Entering)
                return;

            if (AirControl)
            {
                float turnMultiplier = 1f;

                if (turnCurve != null)
                    turnMultiplier = turnCurve.Evaluate(carVelocity.magnitude / Mathf.Max(MaxSpeed, 0.01f));

                carBody.AddTorque(_currentUp * horizontalInput * turn * 100f * turnMultiplier);
            }

            if (allowAutoAlign && _wallRideState == WallRideState.None)
            {
                carBody.MoveRotation(Quaternion.Slerp(
                    carBody.rotation,
                    Quaternion.FromToRotation(carBody.transform.up, Vector3.up) * carBody.transform.rotation,
                    0.02f));
            }

            rb.AddForce(-_currentUp * gravity, ForceMode.Acceleration);
        }

        public void AudioManager()
        {
            if (engineSound != null)
            {
                engineSound.pitch = Mathf.Lerp(
                    minPitch,
                    MaxPitch,
                    Mathf.Abs(carVelocity.z) / Mathf.Max(MaxSpeed, 0.01f));
            }

            if (SkidSound != null)
            {
                SkidSound.mute = !(Mathf.Abs(carVelocity.x) > 10f && grounded());
            }
        }

        public void Visuals()
        {
            if (rb == null)
                return;

            if (FrontWheels != null)
            {
                foreach (Transform fw in FrontWheels)
                {
                    if (fw == null)
                        continue;

                    fw.localRotation = Quaternion.Slerp(
                        fw.localRotation,
                        Quaternion.Euler(
                            fw.localRotation.eulerAngles.x,
                            30f * horizontalInput,
                            fw.localRotation.eulerAngles.z),
                        0.7f * Time.deltaTime / Mathf.Max(Time.fixedDeltaTime, 0.0001f));

                    if (fw.childCount > 0)
                        fw.GetChild(0).localRotation = rb.transform.localRotation;
                }
            }

            if (RearWheels != null && RearWheels.Length >= 2)
            {
                if (RearWheels[0] != null)
                    RearWheels[0].localRotation = rb.transform.localRotation;

                if (RearWheels[1] != null)
                    RearWheels[1].localRotation = rb.transform.localRotation;
            }

            if (BodyMesh == null)
                return;

            if (carVelocity.z > 1f)
            {
                BodyMesh.localRotation = Quaternion.Slerp(
                    BodyMesh.localRotation,
                    Quaternion.Euler(
                        Mathf.Lerp(0f, -5f, carVelocity.z / Mathf.Max(MaxSpeed, 0.01f)),
                        BodyMesh.localRotation.eulerAngles.y,
                        BodyTilt * horizontalInput),
                    0.4f * Time.deltaTime / Mathf.Max(Time.fixedDeltaTime, 0.0001f));
            }
            else
            {
                BodyMesh.localRotation = Quaternion.Slerp(
                    BodyMesh.localRotation,
                    Quaternion.Euler(0f, 0f, 0f),
                    0.4f * Time.deltaTime / Mathf.Max(Time.fixedDeltaTime, 0.0001f));
            }

            if (kartLike && BodyMesh.parent != null)
            {
                if (jumpInput > 0.1f)
                {
                    BodyMesh.parent.localRotation = Quaternion.Slerp(
                        BodyMesh.parent.localRotation,
                        Quaternion.Euler(0f, 45f * horizontalInput * Mathf.Sign(carVelocity.z), 0f),
                        0.1f * Time.deltaTime / Mathf.Max(Time.fixedDeltaTime, 0.0001f));
                }
                else
                {
                    BodyMesh.parent.localRotation = Quaternion.Slerp(
                        BodyMesh.parent.localRotation,
                        Quaternion.Euler(0f, 0f, 0f),
                        0.1f * Time.deltaTime / Mathf.Max(Time.fixedDeltaTime, 0.0001f));
                }
            }
        }

        public bool grounded()
        {
            if (rb == null || _sphereCollider == null)
            {
                OnGrounded?.Invoke(false);
                return false;
            }

            origin = rb.position + _sphereCollider.radius * _currentUp;
            Vector3 direction = -_currentUp;
            float maxDistance = _sphereCollider.radius + 0.2f;

            LayerMask groundMask = drivableSurface;
            if (_wallRideState != WallRideState.None)
                groundMask |= wallRideLayer;

            if (GroundCheck == groundCheck.rayCast)
            {
                if (Physics.Raycast(rb.position, direction, out hit, maxDistance, groundMask))
                {
                    _isWallRideSurfaceUnderVehicle = ((1 << hit.collider.gameObject.layer) & wallRideLayer) != 0;
                    OnGrounded?.Invoke(true);
                    return true;
                }

                _isWallRideSurfaceUnderVehicle = false;
                OnGrounded?.Invoke(false);
                return false;
            }
            else if (GroundCheck == groundCheck.sphereCaste)
            {
                if (Physics.SphereCast(origin, radius + 0.1f, direction, out hit, maxDistance, groundMask))
                {
                    _isWallRideSurfaceUnderVehicle = ((1 << hit.collider.gameObject.layer) & wallRideLayer) != 0;
                    OnGrounded?.Invoke(true);
                    return true;
                }

                _isWallRideSurfaceUnderVehicle = false;
                OnGrounded?.Invoke(false);
                return false;
            }

            _isWallRideSurfaceUnderVehicle = false;
            OnGrounded?.Invoke(false);
            return false;
        }

        private void OnDrawGizmos()
        {
            if (rb == null)
                return;

            SphereCollider sphere = rb.GetComponent<SphereCollider>();
            if (sphere == null)
                return;

            radius = sphere.radius;
            float width = 0.02f;

            if (!Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(
                    rb.transform.position + ((radius + width) * Vector3.down),
                    new Vector3(2f * radius, 2f * width, 4f * radius));

                BoxCollider box = GetComponent<BoxCollider>();
                if (box != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position, box.size);
                }
            }
        }
    }
}