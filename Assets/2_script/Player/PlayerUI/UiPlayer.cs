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

    public event Action OnButtonClick;

    public TimerUI Timer { get { return _timer; } }

    private void Awake()
    {
        _useBoostItem.onClick.AddListener(() => _useBoostItem.gameObject.SetActive(false));
        _useBoostItem.onClick.AddListener(() => OnClick());
        _useBoostItem.gameObject.SetActive(false);

        _boostImages = new Dictionary<string, Image>
    {
        { "RockWand", m_RockWandImage },
        { "shield", m_ShieldImage },
        { "PowerCollision", m_PowerCollisionImage }
    };

        m_RockWandImage.gameObject.SetActive(false);
        m_ShieldImage.gameObject.SetActive(false);
        m_PowerCollisionImage.gameObject.SetActive(false);
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
        _useBoostItem.gameObject.SetActive(true);
        _boostIcon.sprite = icon;
    }

    public void EnableImage(string key)
    {
        if (_boostImages.TryGetValue(key, out var img))
            img.gameObject.SetActive(true);
    }

    public void DisableImage(string key)
    {
        if (_boostImages.TryGetValue(key, out var img))
            img.gameObject.SetActive(false);
    }

    public void UpdateText(float currentSpeed)
    {
        int speed = Convert.ToInt32(currentSpeed);
       _text.text = speed.ToString();
    }

    public void ActivateUiSpeedBoost()
    { 
        _cameraFeedback.ShowSpeedBoost();
        StartCoroutine(DisableUISpeedBoost());
    }
}
