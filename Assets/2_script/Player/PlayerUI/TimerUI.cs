using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private Image _timerImage;
    [SerializeField] private TMP_Text _timerText;

    private float _duration;
    private float _timeLeft;
    private bool _active;

    private Tween _tickTween;
    private Tween _showTween;
    private Tween _hideTween;

    private float _nextTextUpdate;

    private void Awake()
    {
        _timerImage.fillAmount = 0f;
        _timerImage.gameObject.SetActive(false);
        _timerImage.transform.localScale = Vector3.zero;

        _timerText.text = "";
        _timerText.gameObject.SetActive(false);
    }

    public void StartTimer(float duration)
    {
        _duration = duration;
        _timeLeft = duration;
        _active = true;

        _timerImage.fillAmount = 1f;
        _timerImage.gameObject.SetActive(true);
        _timerText.gameObject.SetActive(true);

        _nextTextUpdate = Time.time;

        PlayShowAnimation();
    }

    public void StopTimer()
    {
        _active = false;

        _tickTween?.Kill(true); 
        _tickTween = null;

        PlayHideAnimation();
    }


    private void Update()
    {
        if (!_active) return;

        _timeLeft -= Time.deltaTime;
        _timerImage.fillAmount = _timeLeft / _duration;

        if (Time.time >= _nextTextUpdate)
        {
            _nextTextUpdate = Time.time + 1f;
            _timerText.text = Mathf.CeilToInt(_timeLeft).ToString();
        }

        if (_timeLeft <= 3f && (_tickTween == null || !_tickTween.IsActive()))
        {
            PlayTickAnimation();
        }

        if (_timeLeft <= 0f)
        {
            _timerText.text = "0";
            StopTimer();
        }
    }

    private void PlayShowAnimation()
    {
        _showTween?.Kill();

        _timerImage.transform.localScale = Vector3.zero;
        _timerText.transform.localScale = Vector3.zero;

        _showTween = DOTween.Sequence()
            .Join(_timerImage.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
            .Join(_timerText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
    }

    private void PlayHideAnimation()
    {
        _hideTween?.Kill();

        _hideTween = DOTween.Sequence()
            .Join(_timerImage.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack))
            .Join(_timerText.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                _timerImage.gameObject.SetActive(false);
                _timerText.gameObject.SetActive(false);
            });
    }

    private void PlayTickAnimation()
    {
        _tickTween?.Kill();

        _tickTween = DOTween.Sequence()
            .Append(_timerImage.transform.DOScale(1.15f, 0.25f).SetEase(Ease.OutSine))
            .Join(_timerText.transform.DOScale(1.2f, 0.25f).SetEase(Ease.OutSine))
            .Append(_timerImage.transform.DOScale(1f, 0.25f).SetEase(Ease.InSine))
            .Join(_timerText.transform.DOScale(1f, 0.25f).SetEase(Ease.InSine))
            .SetLoops(-1);
    }
}
