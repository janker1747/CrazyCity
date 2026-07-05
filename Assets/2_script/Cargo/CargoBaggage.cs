using System;
using System.Collections.Generic;
using UnityEngine;

public class CargoBaggage
{
    private readonly List<ActiveCargo> activeCargos = new();
    private readonly RegularCargoSection regularSection = new();
    private readonly TimedCargoSection timedSection = new();
    private readonly HealthCargoSection healthSection = new();

    private int currentComboAmount;
    private int nextPickupOrder;

    public event Action<ActiveCargo> CargoAdded;
    public event Action<ActiveCargo> CargoRemoved;
    public event Action CargoOrderChanged;
    public event Action<int> ComboChanged;
    public event Action<IReadOnlyList<ActiveCargo>> TimedCargosExpired;
    public event Action<IReadOnlyList<ActiveCargo>> HealthCargosExpired;

    public IReadOnlyList<ActiveCargo> ActiveCargos => activeCargos;
    public int Count => activeCargos.Count;
    public int CurrentComboAmount => currentComboAmount;

    public ActiveCargo CurrentCargo =>
        activeCargos.Count > 0 ? activeCargos[0] : null;

    public event Action AddHealthCargo;

   
    public void Initialize(CargoUIController cargoUIController)
    {
        timedSection.Initialize(cargoUIController);
        healthSection.Initialize(cargoUIController);
    
        timedSection.Expired += OnTimedCargosExpired;
        healthSection.Expired += OnHealthCargosExpired;

    }

    public void Dispose()
    {
        timedSection.Expired -= OnTimedCargosExpired;
        timedSection.Dispose();

        healthSection.Expired -= OnHealthCargosExpired;
    }

    public ActiveCargo AddCargo(Cargo cargo)
    {
        ActiveCargo activeCargo = new ActiveCargo(cargo)
        {
            PickupOrder = nextPickupOrder
        };

        if (cargo.HasHealth == true)
        {
            AddHealthCargo?.Invoke();
        }

        nextPickupOrder++;

        activeCargos.Add(activeCargo);
        GetSection(cargo).AddCargo(activeCargo);
        SortCargos();

        CargoAdded?.Invoke(activeCargo);
        CargoOrderChanged?.Invoke();
        NotifyComboChanged();

        return activeCargo;
    }

    public bool RemoveCargo(ActiveCargo activeCargo)
    {
        int cargoIndex = activeCargos.IndexOf(activeCargo);
        return RemoveCargoAt(cargoIndex, out _);
    }

    public bool RemoveCargoAt(int cargoIndex, out ActiveCargo removedCargo)
    {
        removedCargo = null;

        if (cargoIndex < 0 || cargoIndex >= activeCargos.Count)
            return false;

        removedCargo = activeCargos[cargoIndex];
        activeCargos.RemoveAt(cargoIndex);

        regularSection.RemoveCargo(removedCargo);
        timedSection.RemoveCargo(removedCargo);
        healthSection.RemoveCargo(removedCargo);

        CargoRemoved?.Invoke(removedCargo);
        NotifyComboChanged();

        return true;
    }

    public ActiveCargo GetCargoAt(int index)
    {
        if (index < 0 || index >= activeCargos.Count)
            return null;

        return activeCargos[index];
    }

    public int IndexOf(ActiveCargo cargo)
    {
        return activeCargos.IndexOf(cargo);
    }

    public bool AreNeighbors(ActiveCargo firstCargo, ActiveCargo secondCargo)
    {
        int firstIndex = IndexOf(firstCargo);
        int secondIndex = IndexOf(secondCargo);

        if (firstIndex < 0 || secondIndex < 0)
            return false;

        return Mathf.Abs(firstIndex - secondIndex) == 1;
    }

    public int FindCargoIndex(Cargo cargo)
    {
        if (cargo == null)
            return -1;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            if (activeCargos[i] != null &&
                activeCargos[i].Cargo == cargo)
            {
                return i;
            }
        }

        return -1;
    }

    public int CountCargo(Cargo cargo)
    {
        if (cargo == null)
            return 0;

        int count = 0;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            if (activeCargos[i] != null &&
                activeCargos[i].Cargo == cargo)
            {
                count++;
            }
        }

        return count;
    }

    public void Tick(float deltaTime, float timerScale)
    {
        for (int i = activeCargos.Count - 1; i >= 0; i--)
        {
            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
            {
                RemoveCargoAt(i, out _);
                continue;
            }

            activeCargo.ElapsedTime += deltaTime;
        }

        timedSection.Tick(deltaTime, timerScale);
    }

    public bool TakeHealthDamage(int damage)
    {
        if (healthSection.Count <= 0)
        {
            return false;
        }

        healthSection.TakeDamage(damage);
        return true;
    }

    private CargoSection GetSection(Cargo cargo)
    {
        if (cargo.HasDeliveryTimer)
            return timedSection;

        if (cargo.HealthAmount > 0)
            return healthSection;

        return regularSection;
    }

    private void SortCargos()
    {
        activeCargos.Sort(CompareCargos);
    }

    private int CompareCargos(ActiveCargo first, ActiveCargo second)
    {
        if (first == second)
            return 0;

        if (first == null)
            return 1;

        if (second == null)
            return -1;

        int orderCompare = first.BaggageOrder.CompareTo(second.BaggageOrder);
        if (orderCompare != 0)
            return orderCompare;

        return first.PickupOrder.CompareTo(second.PickupOrder);
    }

    private void OnTimedCargosExpired(IReadOnlyList<ActiveCargo> cargos)
    {
        TimedCargosExpired?.Invoke(cargos);
    }

    private void OnHealthCargosExpired(IReadOnlyList<ActiveCargo> cargos)
    {
        HealthCargosExpired?.Invoke(cargos);
    }

    private void NotifyComboChanged()
    {
        currentComboAmount = CalculateCurrentComboAmount();
        ComboChanged?.Invoke(currentComboAmount);
    }

    private int CalculateCurrentComboAmount()
    {
        int comboAmount = 0;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            if (activeCargos[i] != null)
                comboAmount += activeCargos[i].ComboAmount;
        }

        return comboAmount;
    }
}