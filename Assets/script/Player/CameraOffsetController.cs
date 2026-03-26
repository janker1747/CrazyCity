using UnityEngine;
using Cinemachine;

public class CameraOffsetController : MonoBehaviour
{
    [SerializeField] private CameraScoreFeedback _cameraFeedBack;
    public Vector3 targetOffset = new Vector3(0f, -1.08f, -2.41f);
    public float transitionSpeed = 2f;

    private CinemachineTransposer transposer;
    private Vector3 originalOffset;
    private Vector3 currentTarget;
    private bool isInZone = false;

    void Awake()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();
        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        originalOffset = transposer.m_FollowOffset;
        currentTarget = originalOffset;
    }

    void Update()
    {
        transposer.m_FollowOffset = Vector3.Lerp(
            transposer.m_FollowOffset,
            currentTarget,
            Time.deltaTime * transitionSpeed
        );
    }

    public void EnterZone()
    {
        isInZone = true;
        currentTarget = targetOffset;
        _cameraFeedBack.DisableFeedback();
    }

    public void ExitZone()
    {
        isInZone = false;
        currentTarget = originalOffset;
        _cameraFeedBack.EnableFeedback();
    }
}
