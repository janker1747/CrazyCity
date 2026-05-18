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
        Countdown,
        CountUp
    }

    [Header("Settings")]
    [SerializeField, Min(0f)] private float duration = 120f;
    [SerializeField] private TimerDirection direction = TimerDirection.Countdown;
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool useUnscaledTime;

    [Header("UI Display")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private string timeFormat = @"mm\:ss";
    [SerializeField] private bool showMilliseconds;
    [SerializeField] private string millisecondFormat = @"mm\:ss\.ff";

    [Header("Events")]
    [SerializeField] private UnityEvent onTimerFinished = new();
    [SerializeField] private FloatEvent onTimeChanged = new();

    private float currentTime;
    private bool isRunning;
    private bool isFinished;

    public event Action TimerFinished;
    public event Action<float> TimeChanged;

    public float Duration => duration;
    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning;
    public bool IsFinished => isFinished;
    public TimerDirection Direction => direction;
    public float NormalizedTime => duration > 0f ? Mathf.Clamp01(currentTime / duration) : 1f;

    private void Awake()
    {
        // Автоматический поиск TMP компонента, если не назначен
        if (timerText == null)
            timerText = GetComponent<TextMeshProUGUI>();

        ResetTimer();

        if (startOnAwake)
            StartTimer();
    }

    private void Update()
    {
        if (!isRunning || isFinished)
            return;

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Tick(deltaTime);
    }

    public void StartTimer()
    {
        isRunning = true;
        isFinished = false;
    }

    public void StartTimer(bool runBackwards)
    {
        direction = runBackwards ? TimerDirection.CountUp : TimerDirection.Countdown;
        ResetTimer();
        StartTimer();
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        if (isFinished)
            return;

        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = direction == TimerDirection.Countdown ? duration : 0f;
        isFinished = false;
        RaiseTimeChanged();
    }

    public void RestartTimer()
    {
        ResetTimer();
        StartTimer();
    }

    public void SetDuration(float newDuration)
    {
        duration = Mathf.Max(0f, newDuration);
        ResetTimer();
    }

    public void SetDirection(TimerDirection newDirection)
    {
        direction = newDirection;
        ResetTimer();
    }

    public void SetTimerText(TextMeshProUGUI newTimerText)
    {
        timerText = newTimerText;
        UpdateTimerDisplay();
    }

    public void SetTimeFormat(string format, bool showMilliseconds = false)
    {
        timeFormat = format;
        this.showMilliseconds = showMilliseconds;
        UpdateTimerDisplay();
    }

    private void Tick(float deltaTime)
    {
        if (direction == TimerDirection.Countdown)
            currentTime -= deltaTime;
        else
            currentTime += deltaTime;

        currentTime = Mathf.Clamp(currentTime, 0f, duration);
        RaiseTimeChanged();

        bool finished = direction == TimerDirection.Countdown
            ? currentTime <= 0f
            : currentTime >= duration;

        if (finished)
            FinishTimer();
    }

    private void FinishTimer()
    {
        if (isFinished)
            return;

        isFinished = true;
        isRunning = false;
        onTimerFinished?.Invoke();
        TimerFinished?.Invoke();
    }

    private void RaiseTimeChanged()
    {
        onTimeChanged?.Invoke(currentTime);
        TimeChanged?.Invoke(currentTime);
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(currentTime);
        string format = showMilliseconds ? millisecondFormat : timeFormat;
        timerText.text = timeSpan.ToString(format);
    }

    // Метод для форматирования времени вручную, если нужно
    public string GetFormattedTime()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(currentTime);
        
        if (showMilliseconds)
        {
            return string.Format("{0:00}:{1:00}.{2:00}", 
                timeSpan.Minutes, 
                timeSpan.Seconds, 
                timeSpan.Milliseconds / 10);
        }
        else
        {
            return string.Format("{0:00}:{1:00}", 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
    }
}