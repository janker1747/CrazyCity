using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Image _healthCircle;
    [SerializeField] private float _animationDuration = 0.25f;

    private Tweener _fillTweener;

    private void OnEnable()
    {
        _playerHealth.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        _playerHealth.HealthChanged -= OnHealthChanged;

        _fillTweener?.Kill();
    }

    private void OnHealthChanged(float currentHealth)
    {
        float targetFill = currentHealth / _playerHealth.MaxHealth;

        _fillTweener?.Kill();

        _fillTweener = _healthCircle
            .DOFillAmount(targetFill, _animationDuration)
            .SetEase(Ease.OutQuad);
    }
}