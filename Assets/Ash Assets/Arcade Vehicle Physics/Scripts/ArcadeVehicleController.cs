using System;
using UnityEngine;

namespace ArcadeVP
{
    public class ArcadeVehicleController : MonoBehaviour
    {
        public event Action<bool> OnGrounded;
        public event Action<float> OnSpeedChanged;

        public enum groundCheck
        {
            rayCast,
            sphereCaste
        }

        public enum MovementMode
        {
            Velocity,
            AngularVelocity
        }

        private enum WallRideState
        {
            None,
            Entering,
            Riding
        }

        private enum WallApproachType
        {
            Forward,
            Side
        }

        [Header("Movement")] public MovementMode movementMode;
        public groundCheck GroundCheck;
        public LayerMask drivableSurface;
        public float MaxSpeed = 30f;
        public float accelaration = 10f;
        public float turn = 5f;
        public float gravity = 7f;
        public float downforce = 5f;

        public bool AirControl = false;
        public bool kartLike = false;
        public float driftMultiplier = 1.5f;

        [Header("Ground Check")] [SerializeField]
        private float groundCheckDistance = 0.8f;

        [SerializeField] private float sphereCastExtraRadius = 0.1f;

        [Header("References")] public Rigidbody rb;
        public Rigidbody carBody;

        [HideInInspector] public RaycastHit hit;

        [Header("Curves / Physics")] public AnimationCurve frictionCurve;
        public AnimationCurve turnCurve;
        public PhysicMaterial frictionMaterial;

        [Header("Visuals")] public Transform BodyMesh;
        public Transform[] FrontWheels = new Transform[2];
        public Transform[] RearWheels = new Transform[2];

        [HideInInspector] public Vector3 carVelocity;
        [HideInInspector] public bool allowAutoAlign = true;

        [Range(0, 10)] public float BodyTilt = 2f;

        [Header("Audio settings")] public AudioSource engineSound;
        [Range(0, 1)] public float minPitch = 0.8f;
        [Range(1, 3)] public float MaxPitch = 2f;
        public AudioSource SkidSound;

        [HideInInspector] public float skidWidth;

        [Header("Input override (AI)")] public bool overrideInput = false;
        [Range(-1f, 1f)] public float overrideHorizontal = 0f;
        [Range(-1f, 1f)] public float overrideVertical = 0f;
        [Range(0f, 1f)] public float overrideJump = 0f;

        [Header("Wall Ride")] [SerializeField] private CameraOffsetController _cameraOffsetController;
        [SerializeField] private bool enableWallRide = true;
        [SerializeField] private LayerMask wallRideLayer;
        [SerializeField] private float wallCheckDistance = 3.5f;
        [SerializeField] private float wallRideDuration = 2.5f;

        [Header("Wall Ride Entry")] [SerializeField]
        private float wallLaunchUpImpulse = 50f;

        [SerializeField] private float wallLaunchToWallImpulse = 4f;
        [SerializeField] private float wallLaunchForwardImpulse = 5f;
        [SerializeField] private float wallAttachDelay = 0.18f;

        [Header("Wall Ride Entry - Forward Wall")] [SerializeField]
        private float forwardWallAlignSpeed = 15f;

        [SerializeField] private float forwardWallInitialBlend = 0.5f;

        [Header("Wall Ride Stick")] [SerializeField]
        private float wallStickForce = 35f;

        [SerializeField] private float wallGravityCompensation = 12f;
        [SerializeField] private float wallAlignSpeed = 10f;
        [SerializeField] private float wallDetachCooldown = 0.15f;

        [Header("Wall Ride FX")] [SerializeField]
        private ParticleSystem wallRideEnterEffect;

        [SerializeField] private ParticleSystem wallRideLoopEffect;
        [SerializeField] private ParticleSystem wallRideExitEffect;

        private float radius;
        private float horizontalInput;
        private float verticalInput;
        private float jumpInput;
        private Vector3 origin;

        private float _currentSpeed;
        private RigidbodyConstraints _currentConstraints = RigidbodyConstraints.None;
        private SphereCollider _sphereCollider;

        private WallRideState _wallRideState = WallRideState.None;
        private WallApproachType _wallApproachType = WallApproachType.Side;
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

            StopWallRideFX();
        }

        private void Update()
        {
            ReadInput();

            if (_wallRideState == WallRideState.Riding)
                UpdateWallLoopFXPosition();

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
                frictionMaterial.dynamicFriction = frictionCurve.Evaluate(Mathf.Abs(carVelocity.x / 100f));

            UpdateWallRideState();

            if (grounded())
                GroundedMovement();
            else
                AirMovement();
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
            // Wall Ride имеет приоритет
            if (_wallRideState != WallRideState.None)
            {
                _currentUp = Vector3.Slerp(_currentUp, _wallNormal, 0.2f);
                return;
            }

            // Если стоим на поверхности — используем её нормаль
            if (grounded())
            {
                _currentUp = Vector3.Slerp(_currentUp, hit.normal, 0.2f);
            }
            else
            {
                // В воздухе плавно возвращаемся к мировому вверх
                _currentUp = Vector3.Slerp(_currentUp, Vector3.up, 0.02f);
            }
        }

        private bool IsForwardWall(RaycastHit wallHit)
        {
            // Check if the wall normal is roughly opposite to the vehicle's forward direction
            // This means the wall is in front of the vehicle
            float dotForward = Vector3.Dot(transform.forward, wallHit.normal);
            // If the wall normal points towards the vehicle (against forward), it's a forward wall
            return dotForward < -0.5f;
        }

        public void TryEnterWallRide(RaycastHit wallHit)
        {
            if (!enableWallRide)
                return;

            if (_wallRideState != WallRideState.None)
                return;

            if (_wallDetachTimer > 0f)
                return;

            // Determine if this is a forward wall approach
            bool isForwardWall = IsForwardWall(wallHit);
            _wallApproachType = isForwardWall ? WallApproachType.Forward : WallApproachType.Side;

            _wallNormal = wallHit.normal.normalized;
            _wallRideTimer = wallRideDuration;
            _wallAttachTimer = wallAttachDelay;
            _wallRideState = WallRideState.Entering;

            Vector3 velocity = rb.velocity;

            if (isForwardWall)
            {
                // For forward wall: preserve more forward velocity and reduce side rotation
                // Remove the component of velocity going into the wall (towards wall normal)
                Vector3 intoWall = Vector3.Project(velocity, -_wallNormal);
                // Keep velocity along the wall surface
                rb.velocity = velocity - intoWall * 0.9f;

                // Launch impulse - less side push for forward walls
                Vector3 launchImpulse =
                    Vector3.up * wallLaunchUpImpulse +
                    (-_wallNormal) * (wallLaunchToWallImpulse * 0.5f) + // Reduced side push
                    transform.forward * (wallLaunchForwardImpulse * 1.5f); // Increased forward push

                rb.AddForce(launchImpulse, ForceMode.Impulse);
            }
            else
            {
                // Original side wall behavior
                Vector3 awayFromWall = Vector3.Project(velocity, _wallNormal);
                rb.velocity = velocity - awayFromWall;

                Vector3 launchImpulse =
                    Vector3.up * wallLaunchUpImpulse +
                    (-_wallNormal) * wallLaunchToWallImpulse +
                    transform.forward * wallLaunchForwardImpulse;

                rb.AddForce(launchImpulse, ForceMode.Impulse);
            }

            _cameraOffsetController.EnterWallRide();
            MoveWallFXToContact(wallHit.point, wallHit.normal);

            if (wallRideEnterEffect != null)
                wallRideEnterEffect.Play();
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

                if (_wallApproachType == WallApproachType.Forward)
                {
                    UpdateWallRideRotationForward();
                }
                else
                {
                    UpdateWallRideRotation(0.35f);
                }

                if (_wallAttachTimer <= 0f)
                {
                    if (!hasWall)
                    {
                        ExitWallRide();
                        return;
                    }

                    _wallRideState = WallRideState.Riding;

                    if (wallRideLoopEffect != null && !wallRideLoopEffect.isPlaying)
                        wallRideLoopEffect.Play();
                }

                return;
            }

            if (!hasWall)
            {
                ExitWallRide();
                return;
            }

            rb.AddForce(-_wallNormal * wallStickForce * rb.mass, ForceMode.Force);
            rb.AddForce(_currentUp * wallGravityCompensation * rb.mass, ForceMode.Force);

            UpdateWallRideRotation(1f);
        }

        /// <summary>
        /// Special rotation handling for forward wall rides - prevents the vehicle from turning sideways
        /// </summary>
        private void UpdateWallRideRotationForward()
        {
            // For forward walls, prioritize keeping the vehicle facing forward along the wall
            Vector3 wallRight = Vector3.Cross(_wallNormal, Vector3.up).normalized;

            if (wallRight.sqrMagnitude < 0.001f)
            {
                // Wall is a ceiling/floor - fallback
                wallRight = transform.right;
            }

            // Project the vehicle's forward direction onto the wall plane
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, _wallNormal).normalized;

            if (projectedForward.sqrMagnitude < 0.001f)
            {
                // If forward is directly into the wall, use the wall's right direction as forward
                projectedForward = wallRight;
            }

            // Calculate the target up direction (wall normal)
            Vector3 targetUp = Vector3.Slerp(transform.up, _wallNormal, forwardWallInitialBlend);

            // Create target rotation that keeps the vehicle facing along the wall rather than sideways
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, targetUp);

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                forwardWallAlignSpeed * Time.fixedDeltaTime));

            if (carBody != null && carBody != rb)
            {
                carBody.MoveRotation(Quaternion.Slerp(
                    carBody.rotation,
                    targetRotation,
                    forwardWallAlignSpeed * Time.fixedDeltaTime));
            }
        }

        private void UpdateWallRideRotation(float blendToWall)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, _currentUp).normalized;

            if (projectedForward.sqrMagnitude < 0.001f)
                projectedForward = Vector3.Cross(transform.right, _currentUp).normalized;

            Vector3 targetUp = Vector3.Slerp(transform.up, _currentUp, blendToWall);
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, targetUp);

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                wallAlignSpeed * Time.fixedDeltaTime));

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
            if (wallRideLoopEffect != null)
                wallRideLoopEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _cameraOffsetController.ExitWallRide();
            Vector3 exitPos = rb.position + (-_wallNormal * 0.8f);
            MoveExitFX(exitPos, _wallNormal);

            if (wallRideExitEffect != null)
                wallRideExitEffect.Play();

            _wallRideState = WallRideState.None;
            _wallApproachType = WallApproachType.Side;
            _currentUp = hit.normal;
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
                carBody.AddTorque(torqueAxis * horizontalInput * sign * turn * 100f * turnMultiplier);
            else if (verticalInput < -0.1f || carVelocity.z < -1f)
                carBody.AddTorque(torqueAxis * horizontalInput * sign * turn * 100f * turnMultiplier);

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

            Quaternion targetRotation = Quaternion.FromToRotation(carBody.transform.up, _currentUp) *
                                        carBody.transform.rotation;
            carBody.MoveRotation(Quaternion.Slerp(carBody.rotation, targetRotation, 6f * Time.fixedDeltaTime));
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

            rb.velocity = Vector3.Lerp(
                rb.velocity,
                rb.velocity - _currentUp * gravity,
                Time.deltaTime * gravity);
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
                SkidSound.mute = !(Mathf.Abs(carVelocity.x) > 10f && grounded());
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
            float maxDistance = groundCheckDistance;

            LayerMask groundMask = drivableSurface;

            if (_wallRideState != WallRideState.None)
                groundMask |= wallRideLayer;

            if (GroundCheck == groundCheck.rayCast)
            {
                if (Physics.Raycast(origin, direction, out hit, maxDistance, groundMask))
                {
                    _isWallRideSurfaceUnderVehicle = ((1 << hit.collider.gameObject.layer) & wallRideLayer) != 0;
                    OnGrounded?.Invoke(true);
                    return true;
                }

                _isWallRideSurfaceUnderVehicle = false;
                OnGrounded?.Invoke(false);
                return false;
            }

            if (GroundCheck == groundCheck.sphereCaste)
            {
                if (Physics.SphereCast(
                        origin,
                        radius + sphereCastExtraRadius,
                        direction,
                        out hit,
                        maxDistance,
                        groundMask))
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

        private void MoveWallFXToContact(Vector3 hitPoint, Vector3 wallNormal)
        {
            Quaternion rot = Quaternion.LookRotation(wallNormal);

            if (wallRideEnterEffect != null)
            {
                wallRideEnterEffect.transform.position = hitPoint;
                wallRideEnterEffect.transform.rotation = rot;
            }

            if (wallRideLoopEffect != null)
            {
                wallRideLoopEffect.transform.position = hitPoint;
                wallRideLoopEffect.transform.rotation = rot;
            }
        }

        private void UpdateWallLoopFXPosition()
        {
            if (wallRideLoopEffect == null)
                return;

            Vector3 pos = rb.position + (-_wallNormal * 0.9f);

            wallRideLoopEffect.transform.position = pos;
            wallRideLoopEffect.transform.rotation = Quaternion.LookRotation(_wallNormal);
        }

        private void MoveExitFX(Vector3 pos, Vector3 wallNormal)
        {
            if (wallRideExitEffect == null)
                return;

            wallRideExitEffect.transform.position = pos;
            wallRideExitEffect.transform.rotation = Quaternion.LookRotation(wallNormal);
        }

        private void StopWallRideFX()
        {
            if (wallRideEnterEffect != null)
                wallRideEnterEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (wallRideLoopEffect != null)
                wallRideLoopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (wallRideExitEffect != null)
                wallRideExitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDrawGizmos()
        {
            if (rb == null)
                return;

            SphereCollider sphere = rb.GetComponent<SphereCollider>();

            if (sphere == null)
                return;

            Vector3 upDir = Application.isPlaying ? _currentUp : Vector3.up;
            Vector3 groundOrigin = rb.position + sphere.radius * upDir;
            Vector3 groundEnd = groundOrigin + (-upDir * groundCheckDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundOrigin, groundEnd);
            Gizmos.DrawWireSphere(groundEnd, 0.08f);

            if (Application.isPlaying && _wallRideState != WallRideState.None)
            {
                Vector3 wallOrigin = rb.position + Vector3.up * 0.5f;
                Vector3 wallEnd = wallOrigin + (-_wallNormal * wallCheckDistance);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(wallOrigin, wallEnd);
                Gizmos.DrawWireSphere(wallEnd, 0.08f);
            }
        }
    }
}