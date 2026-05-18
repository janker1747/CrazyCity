using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health 
{
   private int _maxHealth;
   private int _currentHealth;
   
   public event Action HealthEnded;
   
   public void Initialize(int maxHealth)
   {
      _maxHealth = maxHealth;
      _currentHealth = _maxHealth;
   }

   public void TakeDamage(int damage)
   {
      _currentHealth -= damage;
      
      if (_currentHealth <= 0)
      {
         _currentHealth = 0;
         HealthEnded?.Invoke();
      }
   }
   
   public void AddHealth(int amount)
   {
      _currentHealth += amount;
   }
}
