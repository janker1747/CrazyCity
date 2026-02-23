using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float _maxHealth;
    private float _currentHealth;

    public Action OnTakeDamage;

    public float CurrentHealth => _currentHealth;

    private void Start()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        OnTakeDamage?.Invoke();
    }

    public void ApplyHeal(float heal)
    {
        _currentHealth += heal;
    }
}
