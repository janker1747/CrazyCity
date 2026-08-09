using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 3;
    [SerializeField] private bool _resetOnAwake = true;

    private readonly Health _health = new Health();
    private bool _isDead;

    public event Action<float> HealthChanged;
    public event Action HealthEnded;

    public float MaxHealth => _health.MaxHealth;
    public float CurrentHealth => _health.CurrentHealth;
    public bool IsDead => _isDead;

    private void Awake()
    {
        if (_resetOnAwake)
            Initialize(_maxHealth);
    }

    private void OnEnable()
    {
        _health.HealthEnded += OnHealthEnded;
    }

    private void OnDisable()
    {
        _health.HealthEnded -= OnHealthEnded;
    }

    public void Initialize(float maxHealth)
    {
        _maxHealth = Mathf.Max(0, maxHealth);
        _isDead = _maxHealth <= 0;
        _health.Initialize(_maxHealth);
        HealthChanged?.Invoke(CurrentHealth);
    }

    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0)
        {
            HealthEnded?.Invoke();
        }
        else
        {
            _health.TakeDamage(damage);
            HealthChanged?.Invoke(CurrentHealth);
        }
    }

    public void AddHealth(float amount)
    {
        if (_isDead || amount <= 0)
            return;

        _health.AddHealth(amount);
        HealthChanged?.Invoke(CurrentHealth);
    }

    public void AddMaxHealth(float amount)
    {
        if (_isDead || amount <= 0)
            return;

        _maxHealth += amount;
        _health.AddMaxHealth(amount);
        HealthChanged?.Invoke(CurrentHealth);
    }

    private void OnHealthEnded()
    {
        if (_isDead)
            return;

        _isDead = true;
        HealthChanged?.Invoke(CurrentHealth);
        HealthEnded?.Invoke();
    }
}
