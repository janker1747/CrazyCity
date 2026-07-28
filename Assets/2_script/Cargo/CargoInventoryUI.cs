using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CargoInventoryUI : MonoBehaviour
{
    [Header("Cargo")]
    [SerializeField] private PlayerCargoModule cargoModule;
    [SerializeField] private RectTransform iconsRoot;
    [SerializeField] private Image cargoIconPrefab;

    [Header("Combo UI")]
    [SerializeField] private CanvasGroup comboUI;
    [SerializeField] private Image tensDigitImage;
    [SerializeField] private Image onesDigitImage;

    [Tooltip("Спрайты цифр от 0 до 9. Индекс элемента должен соответствовать цифре.")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];

    [Tooltip("Показывать 05 вместо 5.")]
    [SerializeField] private bool showLeadingZero;

    [SerializeField, Min(0f)] private float comboFadeDuration = 0.15f;

    [Header("Cargo Animations")]
    [SerializeField, Min(0f)] private float addDuration = 0.25f;
    [SerializeField, Min(0f)] private float removeDuration = 0.12f;

    private readonly Dictionary<ActiveCargo, Image> icons = new();

    private bool comboIsVisible;
    private int lastComboAmount = -1;

    private void Awake()
    {
        if (cargoModule == null)
        {
            Player player = FindObjectOfType<Player>();

            if (player != null)
                cargoModule = player.CargoModule;
        }

        InitializeComboUI();
    }

    private void OnEnable()
    {
        Subscribe();
        Rebuild();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (comboUI != null)
            comboUI.DOKill();

        if (tensDigitImage != null)
            tensDigitImage.transform.DOKill();

        if (onesDigitImage != null)
            onesDigitImage.transform.DOKill();
    }

    public void SetCargoModule(PlayerCargoModule playerCargoModule)
    {
        Unsubscribe();

        cargoModule = playerCargoModule;

        if (!isActiveAndEnabled)
            return;

        Subscribe();
        Rebuild();
    }

    private void InitializeComboUI()
    {
        if (comboUI == null)
            return;

        comboUI.alpha = 0f;
        comboUI.interactable = false;
        comboUI.blocksRaycasts = false;

        comboIsVisible = false;
    }

    private void Subscribe()
    {
        if (cargoModule == null)
            return;

        cargoModule.CargoAdded += AddCargoIcon;
        cargoModule.CargoRemoved += RemoveCargoIcon;
        cargoModule.CargoOrderChanged += RefreshIconOrder;
        cargoModule.ComboChanged += UpdateCombo;
    }

    private void Unsubscribe()
    {
        if (cargoModule == null)
            return;

        cargoModule.CargoAdded -= AddCargoIcon;
        cargoModule.CargoRemoved -= RemoveCargoIcon;
        cargoModule.CargoOrderChanged -= RefreshIconOrder;
        cargoModule.ComboChanged -= UpdateCombo;
    }

    private void Rebuild()
    {
        foreach (Image icon in icons.Values)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }

        icons.Clear();

        if (cargoModule == null)
        {
            SetCombo(0, false);
            return;
        }

        IReadOnlyList<ActiveCargo> activeCargos = cargoModule.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
            AddCargoIcon(activeCargos[i], false);

        SetCombo(cargoModule.CurrentComboAmount, false);
        RefreshIconOrder();
    }

    private void AddCargoIcon(ActiveCargo activeCargo)
    {
        AddCargoIcon(activeCargo, true);
    }

    private void AddCargoIcon(ActiveCargo activeCargo, bool animate)
    {
        if (activeCargo == null ||
            activeCargo.Cargo == null ||
            icons.ContainsKey(activeCargo))
            return;

        if (iconsRoot == null || cargoIconPrefab == null)
            return;

        Image icon = Instantiate(cargoIconPrefab, iconsRoot);

        icon.sprite = activeCargo.Cargo.Icon;
        icon.gameObject.SetActive(true);

        icons[activeCargo] = icon;

        if (!animate)
            return;

        icon.transform.localScale = Vector3.zero;
        icon.transform
            .DOScale(1f, addDuration)
            .SetEase(Ease.OutBack);
    }

    private void RemoveCargoIcon(ActiveCargo activeCargo)
    {
        if (activeCargo == null ||
            !icons.TryGetValue(activeCargo, out Image icon))
            return;

        icons.Remove(activeCargo);

        if (icon == null)
            return;

        icon.transform.DOKill();

        icon.transform
            .DOScale(0f, removeDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (icon != null)
                    Destroy(icon.gameObject);
            });
    }

    private void UpdateCombo(int comboAmount)
    {
        SetCombo(comboAmount, true);
    }

    private void SetCombo(int comboAmount, bool animate)
    {
        int clampedCombo = Mathf.Clamp(comboAmount, 0, 99);

        UpdateComboDigits(clampedCombo);
        UpdateComboVisibility(clampedCombo > 0, animate);

        if (animate &&
            clampedCombo > 0 &&
            clampedCombo != lastComboAmount)
        {
            PlayComboChangedAnimation();
        }

        lastComboAmount = clampedCombo;
    }

    private void UpdateComboDigits(int comboAmount)
    {
        if (!DigitSpritesAreValid())
            return;

        int tens = comboAmount / 10;
        int ones = comboAmount % 10;

        if (onesDigitImage != null)
        {
            onesDigitImage.sprite = digitSprites[ones];
            onesDigitImage.enabled = true;
        }

        if (tensDigitImage == null)
            return;

        bool shouldShowTens = showLeadingZero || comboAmount >= 10;

        tensDigitImage.enabled = shouldShowTens;

        if (shouldShowTens)
            tensDigitImage.sprite = digitSprites[tens];
    }

    private void UpdateComboVisibility(bool shouldShow, bool animate)
    {
        if (comboUI == null)
            return;

        if (comboIsVisible == shouldShow &&
            Mathf.Approximately(comboUI.alpha, shouldShow ? 1f : 0f))
            return;

        comboIsVisible = shouldShow;

        comboUI.DOKill();

        float targetAlpha = shouldShow ? 1f : 0f;

        if (!animate || comboFadeDuration <= 0f)
        {
            comboUI.alpha = targetAlpha;
            return;
        }

        comboUI
            .DOFade(targetAlpha, comboFadeDuration)
            .SetEase(shouldShow ? Ease.OutQuad : Ease.InQuad);
    }

    private void PlayComboChangedAnimation()
    {
        if (tensDigitImage != null && tensDigitImage.enabled)
        {
            tensDigitImage.transform.DOKill();
            tensDigitImage.transform.localScale = Vector3.one;

            tensDigitImage.transform
                .DOPunchScale(Vector3.one * 0.15f, 0.15f, 1, 0f);
        }

        if (onesDigitImage != null)
        {
            onesDigitImage.transform.DOKill();
            onesDigitImage.transform.localScale = Vector3.one;

            onesDigitImage.transform
                .DOPunchScale(Vector3.one * 0.15f, 0.15f, 1, 0f);
        }
    }

    private bool DigitSpritesAreValid()
    {
        if (digitSprites == null || digitSprites.Length < 10)
        {
            Debug.LogWarning(
                $"{nameof(CargoInventoryUI)}: массив digitSprites должен содержать 10 спрайтов.",
                this);

            return false;
        }

        return true;
    }

    private void RefreshIconOrder()
    {
        if (cargoModule == null)
            return;

        IReadOnlyList<ActiveCargo> activeCargos = cargoModule.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            ActiveCargo cargo = activeCargos[i];

            if (cargo == null ||
                !icons.TryGetValue(cargo, out Image icon) ||
                icon == null)
                continue;

            icon.transform.SetSiblingIndex(i);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (digitSprites == null || digitSprites.Length != 10)
            System.Array.Resize(ref digitSprites, 10);
    }
#endif
}