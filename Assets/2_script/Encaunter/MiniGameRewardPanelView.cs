using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MiniGameRewardPanelView : MonoBehaviour
{
    [Serializable]
    private sealed class CargoRewardSlot
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image icon;
        [SerializeField] private Text cargoName;

        public void Show(Cargo cargo)
        {
            bool hasCargo = cargo != null;
            root.SetActive(hasCargo);

            if (!hasCargo)
                return;

            icon.sprite = cargo.Icon;
            icon.enabled = cargo.Icon != null;
            cargoName.text = cargo.CargoName;
        }
    }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Common")]
    [SerializeField] private Text titleText;
    [SerializeField] private Color successTitleColor = new Color(0.35f, 1f, 0.45f);
    [SerializeField] private Color failureTitleColor = new Color(1f, 0.3f, 0.25f);
    [SerializeField] private Button continueButton;

    [Header("Success")]
    [SerializeField] private GameObject successContent;
    [SerializeField] private Text walletRewardText;
    [SerializeField] private CargoRewardSlot[] cargoSlots = new CargoRewardSlot[3];

    [Header("Failure")]
    [SerializeField] private GameObject failureContent;
    [SerializeField] private Text scorePenaltyText;

    public event Action ContinueRequested;

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    public void ShowSuccess(int coins, IReadOnlyList<Cargo> cargosByCategory)
    {
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        titleText.text = "MINI-GAME COMPLETE";
        titleText.color = successTitleColor;
        successContent.SetActive(true);
        failureContent.SetActive(false);
        walletRewardText.text = $"WALLET  +{coins} COINS";

        for (int i = 0; i < cargoSlots.Length; i++)
        {
            Cargo cargo = cargosByCategory != null && i < cargosByCategory.Count
                ? cargosByCategory[i]
                : null;

            cargoSlots[i].Show(cargo);
        }
    }

    public void ShowFailure(int penalty)
    {
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        titleText.text = "MINI-GAME FAILED";
        titleText.color = failureTitleColor;
        successContent.SetActive(false);
        failureContent.SetActive(true);
        scorePenaltyText.text = $"-{penalty} SCORE";
    }

    private void OnContinueClicked()
    {
        ContinueRequested?.Invoke();
    }
}
