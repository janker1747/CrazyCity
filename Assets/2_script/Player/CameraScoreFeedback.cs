using UnityEngine;
using Cinemachine;
using DG.Tweening;

public class CameraScoreFeedback : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;
    private CinemachineTransposer transposer;
    private Vector3 baseOffset;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        baseOffset = transposer.m_FollowOffset;
    }

    public void HandleAddScore(int amount)
    {
        if (!enabled) return;

        transposer.DOKill();

        DOTween.To(
            () => transposer.m_FollowOffset,
            x => transposer.m_FollowOffset = x,
            baseOffset + new Vector3(0, 0, 0.3f),
            0.15f
        ).SetLoops(2, LoopType.Yoyo);
    }

    public void HandleRemoveScore(int amount)
    {
        if (!enabled) return;

        transposer.DOKill();

        DOTween.To(
            () => transposer.m_FollowOffset,
            x => transposer.m_FollowOffset = x,
            baseOffset + new Vector3(0, 0, -0.3f),
            0.15f
        ).SetLoops(2, LoopType.Yoyo);
    }

    public void DisableFeedback()
    {
        transposer.DOKill(); 
        enabled = false;
    }

    public void EnableFeedback()
    {
        enabled = true;
    }
}
