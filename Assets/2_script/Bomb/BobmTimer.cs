using DG.Tweening;
using UnityEngine;

public class BombTimer : MonoBehaviour
{
    [SerializeField] private GameObject _fillingSprite;
    [SerializeField] private float _duration = 1;

    private Vector3 _spriteScale;
    private Tween _tween;

    public event System.Action OnTimerCompleted;

    private void Awake()
    {
        _spriteScale = _fillingSprite.transform.localScale;
    }
    
    private void OnEnable()
    {
        _tween?.Kill();
        _fillingSprite.transform.localScale = Vector3.zero;
    }

    public void StartTimer()
    {
        _tween?.Kill();

        _tween = _fillingSprite.transform
            .DOScale(_spriteScale, _duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                OnTimerCompleted?.Invoke();
            });
    }
}