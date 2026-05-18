using UnityEngine;
using Cinemachine;

public class CameraOffsetController : MonoBehaviour
{
    [SerializeField] private CameraScoreFeedback _cameraFeedBack;
    [SerializeField] private CinemachineVirtualCamera normalCamera; // LockToTargetWithWorldUp
    [SerializeField] private CinemachineVirtualCamera wallRideCamera; // LockToTarget
    [SerializeField] private float blendDuration = 0.5f;

    public Vector3 targetOffset = new Vector3(0f, -1.08f, -2.41f);
    public float transitionSpeed = 2f;

    private CinemachineBrain brain;
    private CinemachineTransposer normalTransposer;
    private CinemachineTransposer wallRideTransposer;
    private Vector3 originalOffset;
    private Vector3 currentNormalTarget;
    private Vector3 currentWallRideTarget;

    private bool isInZone = false;
    private bool isWallRide = false;

    void Awake()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        brain.m_DefaultBlend.m_Time = blendDuration;
        brain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;

        normalTransposer = normalCamera.GetCinemachineComponent<CinemachineTransposer>();
        wallRideTransposer = wallRideCamera.GetCinemachineComponent<CinemachineTransposer>();

        originalOffset = normalTransposer.m_FollowOffset;
        currentNormalTarget = originalOffset;
        currentWallRideTarget = originalOffset;

        // Начальное состояние
        wallRideCamera.Priority = 0;
        normalCamera.Priority = 10;
    }

    void Update()
    {
        // Плавное изменение оффсетов
        normalTransposer.m_FollowOffset = Vector3.Lerp(
            normalTransposer.m_FollowOffset,
            currentNormalTarget,
            Time.deltaTime * transitionSpeed
        );

        wallRideTransposer.m_FollowOffset = Vector3.Lerp(
            wallRideTransposer.m_FollowOffset,
            currentWallRideTarget,
            Time.deltaTime * transitionSpeed
        );
    }

    public void EnterZone()
    {
        isInZone = true;
        currentNormalTarget = targetOffset;
        currentWallRideTarget = targetOffset;
        _cameraFeedBack.DisableFeedback();
    }

    public void ExitZone()
    {
        isInZone = false;
        currentNormalTarget = originalOffset;
        currentWallRideTarget = originalOffset;
        _cameraFeedBack.EnableFeedback();
    }

    public void EnterWallRide()
    {
        isWallRide = true;
        // Плавно переключаемся на wall ride камеру
        normalCamera.Priority = 0;
        wallRideCamera.Priority = 10;
        _cameraFeedBack.DisableFeedback();
    }

    public void ExitWallRide()
    {
        isWallRide = false;
        // Плавно возвращаемся к обычной камере
        wallRideCamera.Priority = 0;
        normalCamera.Priority = 10;
        _cameraFeedBack.EnableFeedback();
    }
}