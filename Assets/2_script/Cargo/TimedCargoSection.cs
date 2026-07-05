using System;
using System.Collections.Generic;
using UnityEngine;

public class TimedCargoSection : CargoSection
{
    private CargoUIController timer;

    public event Action<IReadOnlyList<ActiveCargo>> Expired;

    public void Initialize(CargoUIController timerController)
    {
        if (timer != null)
            timer.TimerCompleted -= OnTimerCompleted;

        timer = timerController;

        if (timer != null)
            timer.TimerCompleted += OnTimerCompleted;
    }

    public void Dispose()
    {
        if (timer != null)
            timer.TimerCompleted -= OnTimerCompleted;

        timer = null;
    }

    public override void AddCargo(ActiveCargo cargo)
    {
        base.AddCargo(cargo);

        if (cargo != null && cargo.Cargo != null)
            timer?.AddCargoTime(cargo.Cargo.DeliveryTime);
    }

    public override bool RemoveCargo(ActiveCargo cargo)
    {
        bool removed = base.RemoveCargo(cargo);

        if (removed && Count == 0)
            timer?.ResetTimer();

        return removed;
    }

    public void Tick(float deltaTime, float timerScale)
    {
        if (Count == 0)
            return;

        timer?.Tick(deltaTime, Mathf.Max(0f, timerScale));
    }

    private void OnTimerCompleted()
    {
        if (Count == 0)
            return;

        Expired?.Invoke(CreateSnapshot());
    }
}
