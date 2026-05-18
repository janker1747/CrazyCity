using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoInventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerCargoModule cargoModule;
    [SerializeField] private RectTransform iconsRoot;
    [SerializeField] private Image cargoIconPrefab;
    [SerializeField] private TMP_Text comboText;
    [SerializeField, Min(0f)] private float addDuration = 0.25f;
    [SerializeField, Min(0f)] private float removeDuration = 0.12f;

    private readonly Dictionary<ActiveCargo, Image> icons = new();

    private void Awake()
    {
        if (cargoModule == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
                cargoModule = player.CargoModule;
        }
    }

    private void OnEnable()
    {
        Subscribe();
        Rebuild();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (cargoModule == null)
            return;

        cargoModule.CargoAdded += AddCargoIcon;
        cargoModule.CargoRemoved += RemoveCargoIcon;
        cargoModule.ComboChanged += UpdateCombo;
    }

    private void Unsubscribe()
    {
        if (cargoModule == null)
            return;

        cargoModule.CargoAdded -= AddCargoIcon;
        cargoModule.CargoRemoved -= RemoveCargoIcon;
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
            UpdateCombo(0);
            return;
        }

        IReadOnlyList<ActiveCargo> activeCargos = cargoModule.ActiveCargos;
        for (int i = 0; i < activeCargos.Count; i++)
            AddCargoIcon(activeCargos[i], false);

        UpdateCombo(cargoModule.CurrentComboAmount);
    }

    private void AddCargoIcon(ActiveCargo activeCargo)
    {
        AddCargoIcon(activeCargo, true);
    }

    private void AddCargoIcon(ActiveCargo activeCargo, bool animate)
    {
        if (activeCargo == null || activeCargo.Cargo == null || icons.ContainsKey(activeCargo))
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
        icon.transform.DOScale(1f, addDuration).SetEase(Ease.OutBack);
    }

    private void RemoveCargoIcon(ActiveCargo activeCargo)
    {
        if (activeCargo == null || !icons.TryGetValue(activeCargo, out Image icon))
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
        if (comboText != null)
            comboText.text = comboAmount.ToString();
    }
}
