using System;
using UnityEngine;

public class CargoUIController : MonoBehaviour
{
    [SerializeField] private CargoTimerView timerView;
    [SerializeField] private BaggageHealthUI healthUI;

    private float totalTime;
    private float elapsedTime;
    private int currentHealth;

    public float RemainingTime => Mathf.Max(0f, totalTime - elapsedTime);

    public float NormalizedTime =>
        totalTime <= 0f
            ? 0f
            : elapsedTime / totalTime;

    public bool IsRunning => totalTime > 0f;

    public int CurrentHealth => currentHealth;

    public event Action TimerCompleted;

    public void AddCargoTime(float time)
    {
        if (time <= 0f)
            return;

        totalTime += time;

        UpdateTimerUI();
    }

    public void AddHealthCargo(int amount = 1)
    {
        if (amount <= 0)
            return;

        for (int i = 0; i < amount; i++)
        {
            healthUI.AddSprite();
            currentHealth++;
        }
    }

    public void RemoveHealthCargo(int amount = 1)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        int removeCount = Mathf.Min(amount, currentHealth);

        for (int i = 0; i < removeCount; i++)
        {
            healthUI.RemoveLast();
            currentHealth--;
        }
    }

    public void ClearHealthCargo()
    {
        currentHealth = 0;

        healthUI.Clear();
    }

    public void Tick(float deltaTime, float timerScale = 1f)
    {
        if (totalTime <= 0f)
            return;

        elapsedTime += deltaTime * Mathf.Max(0f, timerScale);

        if (elapsedTime >= totalTime)
        {
            elapsedTime = totalTime;

            UpdateTimerUI();

            TimerCompleted?.Invoke();

            return;
        }

        UpdateTimerUI();
    }

    public void ResetTimer()
    {
        totalTime = 0f;
        elapsedTime = 0f;

        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerView == null)
            return;

        timerView.SetTime(RemainingTime, totalTime);
    }
}