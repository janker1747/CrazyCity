using System;
using System.Collections.Generic;
using UnityEngine;

public class 
    
    HealthCargoSection : CargoSection
{
    private readonly Health health = new();
    private CargoUIController _uiController;
    
    public event Action<IReadOnlyList<ActiveCargo>> Expired;
    public Health Health => health;
    
    public HealthCargoSection()
    {
        health.HealthEnded += OnHealthEnded;
    }

    public void Initialize(CargoUIController uiController)
    {
       _uiController = uiController;
    }
    
    public override void AddCargo(ActiveCargo cargo)
    {
        base.AddCargo(cargo);
        _uiController.AddHealthCargo();
        
        int healthAmount = GetHealthAmount(cargo);
        if (healthAmount > 0)
            health.AddHealth(healthAmount);
    }

    public override bool RemoveCargo(ActiveCargo cargo)
    {
        bool removed = base.RemoveCargo(cargo);
        _uiController.RemoveHealthCargo();
        
        if (!removed)
            return false;

        health.RemoveHealth(GetHealthAmount(cargo));
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (Count == 0 || damage <= 0)
            return;

        health.TakeDamage(damage);
        
        if (health.CurrentHealth <= 0)
        {
            OnHealthEnded();
        }
        
        _uiController.RemoveHealthCargo();
    }

    private void OnHealthEnded()
    {
        if (Count == 0)
            return;
    
    
        Expired?.Invoke(CreateSnapshot());
    }

    private int GetHealthAmount(ActiveCargo cargo)
    {
        if (cargo == null || cargo.Cargo == null)
            return 0;

        return Mathf.Max(0, cargo.Cargo.HealthAmount);
    }
    
    
}
