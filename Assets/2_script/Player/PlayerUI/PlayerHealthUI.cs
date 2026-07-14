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
        if (_playerHealth != null)
            _playerHealth.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.HealthChanged -= OnHealthChanged;

        _fillTweener?.Kill();
    }

    public void SetPlayerHealth(PlayerHealth playerHealth)
    {
        if (isActiveAndEnabled && _playerHealth != null)
            _playerHealth.HealthChanged -= OnHealthChanged;

        _playerHealth = playerHealth;

        if (_playerHealth == null)
            return;

        if (isActiveAndEnabled)
            _playerHealth.HealthChanged += OnHealthChanged;

        OnHealthChanged(_playerHealth.CurrentHealth);
    }

    private void OnHealthChanged(float currentHealth)
    {
        if (_playerHealth == null || _playerHealth.MaxHealth <= 0f || _healthCircle == null)
            return;

        float targetFill = currentHealth / _playerHealth.MaxHealth;

        _fillTweener?.Kill();

        _fillTweener = _healthCircle
            .DOFillAmount(targetFill, _animationDuration)
            .SetEase(Ease.OutQuad);
    }
}
