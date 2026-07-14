using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiPlayer : MonoBehaviour
{
    [SerializeField] private CameraScoreFeedback _cameraFeedback;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private TimerUI _timer;
    [SerializeField] private Button _useBoostItem;
    [SerializeField] private Image _boostIcon;
    [SerializeField] private Image m_RockWandImage;
    [SerializeField] private Image m_ShieldImage;
    [SerializeField] private Image m_PowerCollisionImage;

    private Dictionary<string, Image> _boostImages;
    private Button _boundBoostButton;

    public event Action OnButtonClick;

    public TimerUI Timer { get { return _timer; } }

    private void Awake()
    {
        _boostImages = new Dictionary<string, Image>
        {
            { "RockWand", m_RockWandImage },
            { "shield", m_ShieldImage },
            { "PowerCollision", m_PowerCollisionImage }
        };

        SetImageActive(m_RockWandImage, false);
        SetImageActive(m_ShieldImage, false);
        SetImageActive(m_PowerCollisionImage, false);

        BindBoostButton();
    }

    private void OnDestroy()
    {
        UnbindBoostButton();
    }

    public void BindSceneUI(
        TMP_Text speedText,
        Button useBoostButton,
        Image boostIcon)
    {
        UnbindBoostButton();

        _text = speedText;
        _useBoostItem = useBoostButton;
        _boostIcon = boostIcon;

        BindBoostButton();
    }

    private void OnClick()
    {
        OnButtonClick?.Invoke();
    }

    private IEnumerator DisableUISpeedBoost()
    {
        yield return new WaitForSeconds(0.25f);
        _cameraFeedback.HideSpeedBoost();  
    }
    
    public void HandleBoost(Sprite icon)
    {
        if (_useBoostItem == null || _boostIcon == null)
            return;

        _useBoostItem.gameObject.SetActive(true);
        _boostIcon.sprite = icon;
    }

    public void EnableImage(string key)
    {
        if (_boostImages.TryGetValue(key, out var img) && img != null)
            img.gameObject.SetActive(true);
    }

    public void DisableImage(string key)
    {
        if (_boostImages.TryGetValue(key, out var img) && img != null)
            img.gameObject.SetActive(false);
    }

    public void UpdateText(float currentSpeed)
    {
        if (_text == null)
            return;

        int speed = Convert.ToInt32(currentSpeed);
        _text.text = speed.ToString();
    }

    public void ActivateUiSpeedBoost()
    { 
        _cameraFeedback.ShowSpeedBoost();
        StartCoroutine(DisableUISpeedBoost());
    }

    private void BindBoostButton()
    {
        if (_useBoostItem == null)
            return;

        _boundBoostButton = _useBoostItem;
        _boundBoostButton.onClick.AddListener(HandleBoostButtonClick);
        _boundBoostButton.gameObject.SetActive(false);
    }

    private void UnbindBoostButton()
    {
        if (_boundBoostButton != null)
            _boundBoostButton.onClick.RemoveListener(HandleBoostButtonClick);

        _boundBoostButton = null;
    }

    private void HandleBoostButtonClick()
    {
        if (_useBoostItem != null)
            _useBoostItem.gameObject.SetActive(false);

        OnClick();
    }

    private static void SetImageActive(Image image, bool active)
    {
        if (image != null)
            image.gameObject.SetActive(active);
    }
}
