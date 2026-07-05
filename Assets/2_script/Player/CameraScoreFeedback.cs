using Cinemachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CameraScoreFeedback : MonoBehaviour
{
    [Header("Camera Move")] [SerializeField]
    private float _offsetAmount = 0.3f;

    [SerializeField] private float _offsetDuration = 0.15f;

    [Header("FOV")] [SerializeField] private float _fovAddScore = 5f;
    [SerializeField] private float _fovRemoveScore = -5f;
    [SerializeField] private float _fovDuration = 0.15f;

    [Header("Vignette")] [SerializeField] private CanvasGroup _vignetteImage;
    [SerializeField] private float _addScoreVignetteAlpha = 0.15f;
    [SerializeField] private float _removeScoreVignetteAlpha = 0.35f;
    [SerializeField] private float _vignetteDuration = 0.15f;

    [Header("SpeedBoostUI")] [SerializeField]
    private CanvasGroup _speedBoostUI;

    [SerializeField] private float _speedBoostDuration = 0.15f;

    private CinemachineVirtualCamera _vcam;
    private CinemachineTransposer _transposer;

    private Vector3 _baseOffset;
    private float _baseFov;

    private Tween _speedBoostTween;
    private Tween _offsetTween;
    private Tween _fovTween;
    private Tween _vignetteTween;

    private void Awake()
    {
        _vcam = GetComponent<CinemachineVirtualCamera>();
        _transposer = _vcam.GetCinemachineComponent<CinemachineTransposer>();

        _baseOffset = _transposer.m_FollowOffset;
        _baseFov = _vcam.m_Lens.FieldOfView;

        _speedBoostUI.alpha = 0f;
        _vignetteImage.alpha = 0f;
    }

    public void HandleAddScore(int amount)
    {
        if (!enabled)
            return;

        PlayOffset(_offsetAmount);
        PlayFov(_fovAddScore);
        PlayVignette(_addScoreVignetteAlpha);
    }

    public void HandleRemoveScore(int amount)
    {
        if (!enabled)
            return;

        PlayOffset(-_offsetAmount);
        PlayFov(_fovRemoveScore);
        PlayVignette(_removeScoreVignetteAlpha);
    }

    private void PlayOffset(float zOffset)
    {
        _offsetTween?.Kill();

        _offsetTween = DOTween.To(
                () => _transposer.m_FollowOffset,
                value => _transposer.m_FollowOffset = value,
                _baseOffset + new Vector3(0f, 0f, zOffset),
                _offsetDuration)
            .SetLoops(2, LoopType.Yoyo);
    }

    private void PlayFov(float offset)
    {
        _fovTween?.Kill();

        _fovTween = DOTween.To(
                () => _vcam.m_Lens.FieldOfView,
                value => _vcam.m_Lens.FieldOfView = value,
                _baseFov + offset,
                _fovDuration)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    private void PlayVignette(float targetAlpha)
    {
        if (_vignetteImage == null)
            return;

        _vignetteTween?.Kill();

        _vignetteImage.alpha = 0f;

        _vignetteTween = DOTween.Sequence()
            .Append(_vignetteImage.DOFade(targetAlpha, _vignetteDuration * 0.5f).SetEase(Ease.OutQuad))
            .Append(_vignetteImage.DOFade(0f, _vignetteDuration * 0.5f).SetEase(Ease.InQuad));
    }

    public void ShowSpeedBoost()
    {
        if (_speedBoostUI == null)
            return;

        _speedBoostTween?.Kill();

        _speedBoostTween = _speedBoostUI
            .DOFade(1f, _speedBoostDuration)
            .SetEase(Ease.OutQuad);
    }

    public void HideSpeedBoost()
    {
        if (_speedBoostUI == null)
            return;

        _speedBoostTween?.Kill();

        _speedBoostTween = _speedBoostUI
            .DOFade(0f, _speedBoostDuration)
            .SetEase(Ease.InQuad);
    }

    public void DisableFeedback()
    {
        _offsetTween?.Kill();
        _fovTween?.Kill();
        _vignetteTween?.Kill();
        _speedBoostTween?.Kill();

        _transposer.m_FollowOffset = _baseOffset;
        _vcam.m_Lens.FieldOfView = _baseFov;

        _speedBoostUI.alpha = 0f;
        _vignetteImage.alpha = 0f;

        enabled = false;
    }

    public void EnableFeedback()
    {
        enabled = true;
    }
}