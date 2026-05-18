using System;
using UnityEngine;

public class CargoUIController : MonoBehaviour
{
    [SerializeField] private CargoTimerView timerView;

    private float totalTime;
    private float elapsedTime;

    public float RemainingTime => Mathf.Max(0f, totalTime - elapsedTime);
    public float NormalizedTime => totalTime <= 0f ? 0f : elapsedTime / totalTime;

    public bool IsRunning => totalTime > 0f;

    public event Action TimerCompleted;

    public void AddCargoTime(float time)
    {
        if (time <= 0f)
            return;

        totalTime += time;

        UpdateUI();
    }

    public void Tick(float deltaTime, float timerScale = 1f)
    {
        if (totalTime <= 0f)
            return;

        elapsedTime += deltaTime * Mathf.Max(0f, timerScale);

        UpdateUI();

        if (elapsedTime >= totalTime)
        {
            elapsedTime = totalTime;
            TimerCompleted?.Invoke();
        }
    }

    public void ResetTimer()
    {
        totalTime = 0f;
        elapsedTime = 0f;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerView == null)
            return;

        timerView.SetTime(RemainingTime, totalTime);
    }
}