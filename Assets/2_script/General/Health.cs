using System;
using UnityEngine;

public class Health
{
    private float _maxHealth;
    private float _currentHealth;

    public event Action HealthEnded;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;

    public void Initialize(float maxHealth)
    {
        _maxHealth = Mathf.Max(0, maxHealth);
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (_maxHealth <= 0 || damage <= 0)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        if (_currentHealth <= 0)
            HealthEnded?.Invoke();
    }

    public void AddHealth(float amount)
    {
        if (amount <= 0)
            return;

        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
    }

    public void AddMaxHealth(float amount)
    {
        if (amount <= 0)
            return;

        _maxHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
    }

    public void RemoveHealth(int amount)
    {
        if (amount <= 0)
            return;

        _maxHealth = Mathf.Max(0, _maxHealth - amount);
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
    }
}
