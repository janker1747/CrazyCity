using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialGroup : MonoBehaviour
{
    [Header("Tutorial Panels")]
    [SerializeField] private List<GameObject> panels = new();

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("Optional")]
    [SerializeField] private TMP_Text pageCounter;
    [SerializeField] private bool resetOnEnable = true;
    [SerializeField] private bool loopPanels;

    private int currentPanelIndex;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextPanel);

        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousPanel);
    }

    private void OnEnable()
    {
        if (resetOnEnable)
            currentPanelIndex = 0;

        ShowCurrentPanel();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextPanel);

        if (previousButton != null)
            previousButton.onClick.RemoveListener(ShowPreviousPanel);
    }

    public void ShowNextPanel()
    {
        if (panels.Count == 0)
            return;

        if (currentPanelIndex < panels.Count - 1)
        {
            currentPanelIndex++;
        }
        else if (loopPanels)
        {
            currentPanelIndex = 0;
        }

        ShowCurrentPanel();
    }

    public void ShowPreviousPanel()
    {
        if (panels.Count == 0)
            return;

        if (currentPanelIndex > 0)
        {
            currentPanelIndex--;
        }
        else if (loopPanels)
        {
            currentPanelIndex = panels.Count - 1;
        }

        ShowCurrentPanel();
    }

    public void ShowPanel(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panels.Count)
        {
            Debug.LogWarning(
                $"Tutorial panel with index {panelIndex} does not exist.",
                this
            );

            return;
        }

        currentPanelIndex = panelIndex;
        ShowCurrentPanel();
    }

    public void RestartTutorial()
    {
        currentPanelIndex = 0;
        ShowCurrentPanel();
    }

    private void ShowCurrentPanel()
    {
        RemoveMissingPanels();

        if (panels.Count == 0)
        {
            UpdateNavigation();
            return;
        }

        currentPanelIndex = Mathf.Clamp(
            currentPanelIndex,
            0,
            panels.Count - 1
        );

        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(i == currentPanelIndex);
        }

        UpdateNavigation();
    }

    private void UpdateNavigation()
    {
        bool hasPanels = panels.Count > 0;

        if (previousButton != null)
        {
            previousButton.interactable =
                hasPanels && (loopPanels || currentPanelIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.interactable =
                hasPanels &&
                (loopPanels || currentPanelIndex < panels.Count - 1);
        }

        if (pageCounter != null)
        {
            pageCounter.text = hasPanels
                ? $"{currentPanelIndex + 1} / {panels.Count}"
                : "0 / 0";
        }
    }

    private void RemoveMissingPanels()
    {
        panels.RemoveAll(panel => panel == null);
    }
}