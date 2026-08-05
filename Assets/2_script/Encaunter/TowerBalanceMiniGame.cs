using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TowerBalanceMiniGame : MonoBehaviour
{
    [Serializable]
    public sealed class FloatEvent : UnityEvent<float>
    {
    }

    [Header("Scene references")]
    [SerializeField] private TowerBalanceInput input;
    [SerializeField] private RectTransform towerRoot;
    [SerializeField] private RectTransform balanceIndicator;
    [SerializeField] private Image balanceIndicatorImage;
    [SerializeField] private RectTransform[] boxes;

    [Header("Balance")]
    [SerializeField, Min(1f)] private float maxLeanDegrees = 24f;
    [SerializeField, Min(0.1f)] private float balanceZoneDegrees = 4.5f;
    [SerializeField, Min(0f)] private float fallAcceleration = 48f;
    [SerializeField, Min(0f)] private float controlAcceleration = 42f;
    [SerializeField, Min(0f)] private float velocityDamping = 0.85f;
    [SerializeField, Min(1f)] private float maxAngularVelocity = 46f;
    [SerializeField, Min(0f)] private float disturbanceStrength = 8f;
    [SerializeField] private Vector2 initialLeanRange = new Vector2(5.5f, 7f);
    [SerializeField] private Vector2 initialAngularVelocityRange = new Vector2(3.5f, 5.5f);

    [Header("Win condition")]
    [SerializeField] private Vector2 holdDurationRange = new Vector2(5f, 7f);
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool useUnscaledTime;

    [Header("Visuals")]
    [SerializeField, Min(0f)] private float indicatorTravel = 500f;
    [SerializeField, Range(0f, 1f)] private float boxBendRotation = 0.35f;
    [SerializeField, Min(0f)] private float boxBendOffset = 55f;
    [SerializeField] private Color balancedColor = new Color(0.25f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.72f, 0.15f, 1f);
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.2f, 0.15f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent onMiniGameStarted;
    [SerializeField] private FloatEvent onBalanceProgressChanged;
    [SerializeField] private UnityEvent onMiniGameCompleted;

    private Quaternion towerStartRotation;
    private Vector2 indicatorStartPosition;
    private Quaternion indicatorStartRotation;
    private readonly List<BoxVisualState> boxStates = new List<BoxVisualState>();

    private float currentLean;
    private float angularVelocity;
    private float balanceTime;
    private float requiredBalanceTime;
    private float disturbance;
    private float disturbanceTarget;
    private float disturbanceTimer;
    private float lastReportedProgress = -1f;
    private bool visualStateCached;
    private bool isRunning;
    private bool isCompleted;
    private bool hasReceivedPlayerInput;

    public float CurrentLean => currentLean;
    public float BalanceProgress => requiredBalanceTime > 0f
        ? Mathf.Clamp01(balanceTime / requiredBalanceTime)
        : 0f;
    public float RequiredBalanceTime => requiredBalanceTime;
    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        ResolveReferences();
        CacheVisualState();
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartGame();
    }

    private void Update()
    {
        if (!isRunning || isCompleted || input == null)
            return;

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        deltaTime = Mathf.Min(deltaTime, 0.05f);
        if (deltaTime <= 0f)
            return;

        if (input.LeftPressed || input.RightPressed)
            hasReceivedPlayerInput = true;

        UpdateDisturbance(deltaTime);
        UpdateBalance(deltaTime);
        UpdateWinProgress(deltaTime);
        ApplyVisuals();
    }

    public void StartGame()
    {
        ResolveReferences();
        CacheVisualState();

        if (input == null || towerRoot == null || balanceIndicator == null)
        {
            Debug.LogError(
                "[TowerBalanceMiniGame] Tower, input or balance indicator is missing.",
                this);
            isRunning = false;
            return;
        }

        RestoreVisualState();
        input.SetLeftPressed(false);
        input.SetRightPressed(false);

        float minimumLean = Mathf.Max(0f, Mathf.Min(initialLeanRange.x, initialLeanRange.y));
        float maximumLean = Mathf.Max(minimumLean, Mathf.Max(initialLeanRange.x, initialLeanRange.y));
        float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float minimumVelocity = Mathf.Max(
            0f,
            Mathf.Min(initialAngularVelocityRange.x, initialAngularVelocityRange.y));
        float maximumVelocity = Mathf.Max(
            minimumVelocity,
            Mathf.Max(initialAngularVelocityRange.x, initialAngularVelocityRange.y));

        currentLean = UnityEngine.Random.Range(minimumLean, maximumLean) * direction;
        angularVelocity = UnityEngine.Random.Range(minimumVelocity, maximumVelocity) * direction;
        balanceTime = 0f;
        requiredBalanceTime = UnityEngine.Random.Range(
            Mathf.Min(holdDurationRange.x, holdDurationRange.y),
            Mathf.Max(holdDurationRange.x, holdDurationRange.y));
        requiredBalanceTime = Mathf.Max(0.1f, requiredBalanceTime);

        disturbance = 0f;
        disturbanceTarget = 0f;
        disturbanceTimer = 0f;
        lastReportedProgress = -1f;
        isCompleted = false;
        isRunning = true;
        hasReceivedPlayerInput = false;

        ApplyVisuals();
        ReportProgress(true);
        onMiniGameStarted?.Invoke();
    }

    public void RestartGame()
    {
        StartGame();
    }

    private void UpdateDisturbance(float deltaTime)
    {
        disturbanceTimer -= deltaTime;
        if (disturbanceTimer <= 0f)
        {
            disturbanceTimer = UnityEngine.Random.Range(0.35f, 0.8f);
            disturbanceTarget = UnityEngine.Random.Range(-disturbanceStrength, disturbanceStrength);
        }

        disturbance = Mathf.MoveTowards(
            disturbance,
            disturbanceTarget,
            disturbanceStrength * 2.5f * deltaTime);
    }

    private void UpdateBalance(float deltaTime)
    {
        float normalizedLean = Mathf.Clamp(currentLean / maxLeanDegrees, -1f, 1f);
        float acceleration = normalizedLean * fallAcceleration;
        acceleration += input.Direction * controlAcceleration;
        acceleration += disturbance;

        angularVelocity += acceleration * deltaTime;
        angularVelocity *= Mathf.Exp(-velocityDamping * deltaTime);
        angularVelocity = Mathf.Clamp(
            angularVelocity,
            -maxAngularVelocity,
            maxAngularVelocity);

        currentLean += angularVelocity * deltaTime;

        if (Mathf.Abs(currentLean) < maxLeanDegrees)
            return;

        float fallDirection = Mathf.Sign(currentLean);
        currentLean = fallDirection * maxLeanDegrees * 0.72f;
        angularVelocity = -fallDirection * maxAngularVelocity * 0.28f;
        balanceTime = 0f;
        ReportProgress(true);
    }

    private void UpdateWinProgress(float deltaTime)
    {
        if (!hasReceivedPlayerInput)
        {
            balanceTime = 0f;
            ReportProgress(false);
            return;
        }

        if (Mathf.Abs(currentLean) <= balanceZoneDegrees)
            balanceTime += deltaTime;
        else if (balanceTime > 0f)
            balanceTime = 0f;

        ReportProgress(false);

        if (balanceTime < requiredBalanceTime)
            return;

        balanceTime = requiredBalanceTime;
        isCompleted = true;
        isRunning = false;
        angularVelocity = 0f;
        ReportProgress(true);

        Debug.Log(
            $"<color=green>[TowerBalanceMiniGame] SUCCESS</color> " +
            $"Balance held for {requiredBalanceTime:F1} seconds.",
            this);

        onMiniGameCompleted?.Invoke();
    }

    private void ApplyVisuals()
    {
        if (!visualStateCached)
            return;

        float normalizedLean = Mathf.Clamp(currentLean / maxLeanDegrees, -1f, 1f);
        towerRoot.localRotation = towerStartRotation * Quaternion.Euler(0f, 0f, -currentLean);

        Vector2 indicatorPosition = indicatorStartPosition;
        indicatorPosition.x += normalizedLean * indicatorTravel;
        balanceIndicator.anchoredPosition = indicatorPosition;
        balanceIndicator.localRotation = indicatorStartRotation *
                                         Quaternion.Euler(0f, 0f, -currentLean * 0.6f);

        float absoluteLean = Mathf.Abs(currentLean);
        if (balanceIndicatorImage != null)
        {
            float warningStart = Mathf.Max(balanceZoneDegrees, 0.01f);
            if (absoluteLean <= balanceZoneDegrees)
            {
                balanceIndicatorImage.color = balancedColor;
            }
            else
            {
                float dangerAmount = Mathf.InverseLerp(
                    warningStart,
                    maxLeanDegrees,
                    absoluteLean);
                balanceIndicatorImage.color = Color.Lerp(warningColor, dangerColor, dangerAmount);
            }
        }

        float bendSin = Mathf.Sin(currentLean * Mathf.Deg2Rad);
        for (int i = 0; i < boxStates.Count; i++)
        {
            BoxVisualState state = boxStates[i];
            float bendWeight = Mathf.Pow(state.heightWeight, 1.35f);
            Vector2 position = state.startPosition;
            position.x += bendSin * boxBendOffset * bendWeight;

            state.rectTransform.anchoredPosition = position;
            state.rectTransform.localRotation = state.startRotation * Quaternion.Euler(
                0f,
                0f,
                -currentLean * boxBendRotation * bendWeight);
        }
    }

    private void ResolveReferences()
    {
        if (towerRoot == null)
            towerRoot = FindChildRect(transform, "Tower");

        if (input == null && towerRoot != null)
            input = towerRoot.GetComponent<TowerBalanceInput>();

        if (balanceIndicator == null)
        {
            RectTransform slider = FindChildRect(transform, "Slider");
            if (slider != null && slider.childCount > 0)
                balanceIndicator = slider.GetChild(0) as RectTransform;
        }

        if (balanceIndicatorImage == null && balanceIndicator != null)
            balanceIndicatorImage = balanceIndicator.GetComponent<Image>();

        if ((boxes == null || boxes.Length == 0) && towerRoot != null)
        {
            List<RectTransform> foundBoxes = new List<RectTransform>();
            for (int i = 0; i < towerRoot.childCount; i++)
            {
                RectTransform child = towerRoot.GetChild(i) as RectTransform;
                if (child != null && child.name.StartsWith("BOX", StringComparison.OrdinalIgnoreCase))
                    foundBoxes.Add(child);
            }

            boxes = foundBoxes.ToArray();
        }
    }

    private void CacheVisualState()
    {
        if (visualStateCached || towerRoot == null || balanceIndicator == null)
            return;

        towerStartRotation = towerRoot.localRotation;
        indicatorStartPosition = balanceIndicator.anchoredPosition;
        indicatorStartRotation = balanceIndicator.localRotation;

        boxStates.Clear();
        if (boxes != null && boxes.Length > 0)
        {
            float minimumY = float.MaxValue;
            float maximumY = float.MinValue;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] == null)
                    continue;

                minimumY = Mathf.Min(minimumY, boxes[i].anchoredPosition.y);
                maximumY = Mathf.Max(maximumY, boxes[i].anchoredPosition.y);
            }

            for (int i = 0; i < boxes.Length; i++)
            {
                RectTransform box = boxes[i];
                if (box == null)
                    continue;

                float heightWeight = Mathf.Approximately(minimumY, maximumY)
                    ? 1f
                    : Mathf.InverseLerp(minimumY, maximumY, box.anchoredPosition.y);

                boxStates.Add(new BoxVisualState(
                    box,
                    box.anchoredPosition,
                    box.localRotation,
                    heightWeight));
            }
        }

        visualStateCached = true;
    }

    private void RestoreVisualState()
    {
        if (!visualStateCached)
            return;

        towerRoot.localRotation = towerStartRotation;
        balanceIndicator.anchoredPosition = indicatorStartPosition;
        balanceIndicator.localRotation = indicatorStartRotation;

        for (int i = 0; i < boxStates.Count; i++)
        {
            BoxVisualState state = boxStates[i];
            state.rectTransform.anchoredPosition = state.startPosition;
            state.rectTransform.localRotation = state.startRotation;
        }
    }

    private void ReportProgress(bool force)
    {
        float progress = BalanceProgress;
        if (!force && Mathf.Abs(progress - lastReportedProgress) < 0.001f)
            return;

        lastReportedProgress = progress;
        onBalanceProgressChanged?.Invoke(progress);
    }

    private static RectTransform FindChildRect(Transform parent, string objectName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return child as RectTransform;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxLeanDegrees = Mathf.Max(1f, maxLeanDegrees);
        balanceZoneDegrees = Mathf.Clamp(balanceZoneDegrees, 0.1f, maxLeanDegrees * 0.9f);

        initialLeanRange.x = Mathf.Clamp(initialLeanRange.x, 0f, maxLeanDegrees * 0.8f);
        initialLeanRange.y = Mathf.Clamp(initialLeanRange.y, initialLeanRange.x, maxLeanDegrees * 0.8f);

        initialAngularVelocityRange.x = Mathf.Max(0f, initialAngularVelocityRange.x);
        initialAngularVelocityRange.y = Mathf.Max(
            initialAngularVelocityRange.x,
            initialAngularVelocityRange.y);

        holdDurationRange.x = Mathf.Max(0.1f, holdDurationRange.x);
        holdDurationRange.y = Mathf.Max(holdDurationRange.x, holdDurationRange.y);
    }
#endif

    private sealed class BoxVisualState
    {
        public readonly RectTransform rectTransform;
        public readonly Vector2 startPosition;
        public readonly Quaternion startRotation;
        public readonly float heightWeight;

        public BoxVisualState(
            RectTransform rectTransform,
            Vector2 startPosition,
            Quaternion startRotation,
            float heightWeight)
        {
            this.rectTransform = rectTransform;
            this.startPosition = startPosition;
            this.startRotation = startRotation;
            this.heightWeight = heightWeight;
        }
    }
}
