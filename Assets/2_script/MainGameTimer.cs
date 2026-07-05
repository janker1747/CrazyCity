using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MainGameTimer : MonoBehaviour
{
    [Serializable]
    public class FloatEvent : UnityEvent<float> { }

    public enum TimerDirection
    {
        CountUp
    }

    [Header("Settings")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool useUnscaledTime;

    [Header("UI Display")]
    [SerializeField] private string timeFormat = @"mm\:ss";
    [SerializeField] private bool showMilliseconds;
    [SerializeField] private string millisecondFormat = @"mm\:ss\.ff";

    [Header("Events")]
    [SerializeField] private FloatEvent onTimeChanged = new();

    [Header("BombTimeInterval")]
    [SerializeField] private float _firstBombDelay = 10f;
    [SerializeField] private float _startBombInterval = 8f;
    [SerializeField] private float _intervalDecreasePerMinute = 1f;
    [SerializeField] private float _minBombInterval = 3f;

    private float _nextBombSpawnTime;
    
    
    private float currentTime;
    private bool isRunning;

    public event Action<float> TimeChanged;
    public event Action TimeSpawnBomb;

    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning;

    // Оставлено для совместимости с другим кодом
    public TimerDirection Direction => TimerDirection.CountUp;

    private void Awake()
    {
        ResetTimer();

        if (startOnAwake)
            StartTimer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        float deltaTime =
            useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

        Tick(deltaTime);
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = 0f;
        _nextBombSpawnTime = _firstBombDelay;
        RaiseTimeChanged();
    }

    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
    }

    public void SetCurrentTime(float time)
    {
        currentTime = Mathf.Max(0f, time);
        RaiseTimeChanged();
    }

    private void Tick(float deltaTime)
    {
        currentTime += deltaTime;

        while (currentTime >= _nextBombSpawnTime)
        {
            TimeSpawnBomb?.Invoke();

            float currentInterval = GetCurrentBombInterval();
            _nextBombSpawnTime += currentInterval;
        }

        RaiseTimeChanged();
    }
    
    private float GetCurrentBombInterval()
    {
        int passedMinutes = Mathf.FloorToInt(currentTime / 60f);

        float interval = _startBombInterval - passedMinutes * _intervalDecreasePerMinute;

        return Mathf.Max(interval, _minBombInterval);
    }
    

    private void RaiseTimeChanged()
    {
        onTimeChanged?.Invoke(currentTime);
        TimeChanged?.Invoke(currentTime);

    }
}