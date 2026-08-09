using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceContract : MonoBehaviour
{
    private const int FirstRequirement = 10;
    private const int RequirementIncrease = 15;
    private const float SpeedMultiplier = 1.05f;
    private const float ScoreMultiplier = 2.5f;

    [Header("UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private RectTransform cardsRoot;
    [SerializeField] private Image xpFillingImage;
    [SerializeField] private Button removeBombButton;
    [SerializeField] private Button addSpeedButton;
    [SerializeField] private Button addHealthButton;
    [SerializeField] private Button scoreButton;

    [Header("Game References")]
    [SerializeField] private CargoManager cargoManager;
    [SerializeField] private Player player;
    [SerializeField] private BombSpawner bombSpawner;

    [Header("Animation")]
    [SerializeField, Range(0.01f, 1f)] private float slowTimeScale = 0.1f;
    [SerializeField, Min(0f)] private float slowTimeDuration = 0.25f;
    [SerializeField, Min(0f)] private float panelAnimationDuration = 0.25f;
    [SerializeField, Range(0.1f, 1f)] private float panelStartScale = 0.85f;

    private int deliveredPackages;
    private int previousChoiceRequirement;
    private int nextChoiceRequirement = FirstRequirement;
    private float timeScaleBeforePanel = 1f;
    private bool isPanelOpen;
    private bool isInitialized;
    private Tween panelTween;
    private Tween timeScaleTween;

    private void Start()
    {
        Initialize(cargoManager);
    }

    public static void TryInitialize(CargoManager manager)
    {
        foreach (ChoiceContract contract in Resources.FindObjectsOfTypeAll<ChoiceContract>())
        {
            if (contract.gameObject.scene.IsValid())
                contract.Initialize(manager);
        }
    }

    private void Initialize(CargoManager manager)
    {
        if (isInitialized)
            return;

        isInitialized = true;
        if (cargoManager == null)
            cargoManager = manager;

        ResolveReferences();
        SubscribeToDeliveries();
        BindButtons();
        UpdateProgress();
        SetPanelVisible(false, false);
    }

    private void OnDestroy()
    {
        if (cargoManager != null)
            cargoManager.CargoDelivered -= OnCargoDelivered;

        panelTween?.Kill();
        timeScaleTween?.Kill();
    }

    private void OnCargoDelivered()
    {
        deliveredPackages++;
        UpdateProgress();

        if (!isPanelOpen && deliveredPackages >= nextChoiceRequirement)
            SetPanelVisible(true, true);
    }

    private void RemoveBomb()
    {
        ResolveReferences();
        bombSpawner?.RemoveBombs();
        CompleteChoice();
    }

    private void AddSpeed()
    {
        ResolveReferences();
        player?.AddSpeedMultiplier(SpeedMultiplier);
        CompleteChoice();
    }

    private void AddHealth()
    {
        ResolveReferences();
        player?.Health.AddMaxHealth(1f);
        CompleteChoice();
    }

    private void AddScoreMultiplier()
    {
        ResolveReferences();
        player?.ScoreSystem.AddContractMultiplier(ScoreMultiplier);
        CompleteChoice();
    }

    private void CompleteChoice()
    {
        previousChoiceRequirement = nextChoiceRequirement;
        nextChoiceRequirement += RequirementIncrease;
        UpdateProgress();
        SetPanelVisible(false, true);
    }

    private void SetPanelVisible(bool visible, bool animate)
    {
        if (choicePanel == null)
            return;

        isPanelOpen = visible;
        panelTween?.Kill();

        if (!visible)
        {
            choicePanel.SetActive(false);
            if (animate)
                ChangeTimeScale(timeScaleBeforePanel);

            return;
        }

        timeScaleBeforePanel = Time.timeScale;
        choicePanel.SetActive(true);
        choicePanel.transform.SetAsLastSibling();
        ChangeTimeScale(slowTimeScale);

        RectTransform animatedRoot = cardsRoot != null
            ? cardsRoot
            : choicePanel.transform as RectTransform;

        if (animatedRoot == null)
            return;

        animatedRoot.localScale = Vector3.one * panelStartScale;
        panelTween = animatedRoot
            .DOScale(Vector3.one, panelAnimationDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void ChangeTimeScale(float targetTimeScale)
    {
        timeScaleTween?.Kill();
        timeScaleTween = DOTween
            .To(() => Time.timeScale, value => Time.timeScale = value,
                Mathf.Clamp(targetTimeScale, 0.01f, 1f), slowTimeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void UpdateProgress()
    {
        if (xpFillingImage == null)
            return;

        xpFillingImage.fillAmount = Mathf.Clamp01(
            (float)(deliveredPackages - previousChoiceRequirement) /
            (nextChoiceRequirement - previousChoiceRequirement));
    }

    private void ResolveReferences()
    {
        if (cargoManager == null)
            cargoManager = FindObjectOfType<CargoManager>();

        if (player == null)
            player = FindObjectOfType<Player>();

        if (bombSpawner == null)
            bombSpawner = FindObjectOfType<BombSpawner>();
    }

    private void SubscribeToDeliveries()
    {
        if (cargoManager != null)
            cargoManager.CargoDelivered += OnCargoDelivered;
    }

    private void BindButtons()
    {
        if (removeBombButton != null)
            removeBombButton.onClick.AddListener(RemoveBomb);

        if (addSpeedButton != null)
            addSpeedButton.onClick.AddListener(AddSpeed);

        if (addHealthButton != null)
            addHealthButton.onClick.AddListener(AddHealth);

        if (scoreButton != null)
            scoreButton.onClick.AddListener(AddScoreMultiplier);
    }
}
