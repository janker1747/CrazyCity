using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GarageUIManager : MonoBehaviour
{
    [Serializable]
    private class CarPreviewEntry
    {
        [Tooltip("Данные машины")]
        public CarItemSO CarData;

        [Tooltip("3D-модель машины, находящаяся на сцене")]
        public GameObject Visual;

        [Tooltip("Уникальный постоянный ID для сохранения покупки")]
        public string SaveId;

        [Tooltip("Стоимость машины")]
        [Min(0)]
        public int Cost;

        [Tooltip("Открыта ли машина изначально")]
        public bool IsOpen;

        [NonSerialized]
        public Vector3 StartLocalPosition;
    }

    [Header("3D Car Preview")]
    [SerializeField] private List<CarPreviewEntry> _carPreviews;

    [Header("Car Information")]
    [SerializeField] private TMP_Text _carName;

    [Header("Car Switch Animation")]
    [SerializeField, Min(0.1f)] private float _moveDistance = 6f;
    [SerializeField, Min(0.05f)] private float _animationTime = 0.35f;

    [Header("Buttons")]
    [SerializeField] private RectTransform _nextButton;
    [SerializeField] private RectTransform _backButton;
    [SerializeField, Min(0.05f)] private float _buttonAnimTime = 0.12f;

    [Header("Sliders")]
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _damageSlider;
    [SerializeField, Min(0.05f)] private float _sliderAnimationTime = 0.4f;

    [Header("Ride / Buy Button")]
    [SerializeField] private Button _startButton;
    [SerializeField] private ChoisePlayerUI _choisePlayerUI;

    [SerializeField] private string _rideButtonText = "RIDE";
    [SerializeField] private string _buyButtonText = "BUY";
    [SerializeField] private string _notEnoughMoneyText = "NOT ENOUGH";

    [SerializeField, Min(0.1f)]
    private float _notEnoughMessageDuration = 1f;

    private TMP_Text _buttonText;
    private Sequence _switchSequence;
    private Tween _buttonTextTween;

    private bool _isAnimating;
    private int _currentPreviewIndex = -1;

    private void Awake()
    {
        if (_startButton != null)
        {
            _buttonText = _startButton.GetComponentInChildren<TMP_Text>();
            _startButton.onClick.AddListener(OnRideBuyButtonClicked);
        }
        else
        {
            Debug.LogError("Start button is not assigned.", this);
        }
    }

    private void Start()
    {
        InitializePreviews();
        ShowCurrentCarImmediately();
    }

    public void GoForward()
    {
        if (_isAnimating)
            return;

        AnimateButton(_nextButton);
        SwitchCar(true);
    }

    public void GoBack()
    {
        if (_isAnimating)
            return;

        AnimateButton(_backButton);
        SwitchCar(false);
    }

    /// <summary>
    /// Обработчик главной кнопки гаража.
    /// Открытая машина запускает игру, закрытая — покупается.
    /// </summary>
    public void OnRideBuyButtonClicked()
    {
        if (_isAnimating)
            return;

        CarPreviewEntry currentPreview = GetCurrentPreview();

        if (currentPreview == null)
        {
            Debug.LogError("Current car preview was not found.", this);
            return;
        }

        if (currentPreview.IsOpen)
        {
            StartGame();
            return;
        }

        TryBuyCar(currentPreview);
    }

    public Player SetPlayer()
    {
        CarItemSO currentCar = GetCurrentCar();

        if (currentCar == null)
        {
            Debug.LogError(
                "Cannot set player because the selected car was not found.",
                this);

            return null;
        }

        CarPreviewEntry preview = GetCurrentPreview();

        if (preview == null || !preview.IsOpen)
        {
            Debug.LogError(
                $"Cannot select locked car: {currentCar.PlayerName}",
                this);

            return null;
        }

        return currentCar.PlayerPrefab;
    }

    private void StartGame()
    {
        if (_choisePlayerUI == null)
        {
            Debug.LogError(
                "ChoisePlayerUI is not assigned in GarageUIManager.",
                this);

            return;
        }

        _choisePlayerUI.StartGame();
    }

    private void TryBuyCar(CarPreviewEntry preview)
    {
        if (preview == null || preview.CarData == null)
            return;

        if (preview.IsOpen)
        {
            UpdateRideBuyButton();
            return;
        }

        int price = Mathf.Max(0, preview.Cost);

        if (price > 0)
        {
            var wallet = GameData.Instance.Wallet;

            if (!wallet.TrySpendGold(price))
            {
                ShowNotEnoughMoneyMessage();
                return;
            }
        }

        UnlockCar(preview);

        AnimateButton(_startButton.transform as RectTransform);

        Debug.Log(
            $"Car purchased: {preview.CarData.PlayerName}. " +
            $"Remaining gold: {GameData.Instance.Wallet.CurrentGold}",
            this);
    }

    private void UnlockCar(CarPreviewEntry preview)
    {
        preview.IsOpen = true;

        string saveKey = GetCarSaveKey(preview);

        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        UpdateRideBuyButton();
    }

    private void InitializePreviews()
    {
        if (_carPreviews == null)
            return;

        for (int i = 0; i < _carPreviews.Count; i++)
        {
            CarPreviewEntry preview = _carPreviews[i];

            if (preview == null)
                continue;

            LoadCarUnlockState(preview);

            if (preview.Visual == null)
                continue;

            preview.StartLocalPosition =
                preview.Visual.transform.localPosition;

            preview.Visual.SetActive(false);
        }
    }

    private void LoadCarUnlockState(CarPreviewEntry preview)
    {
        string saveKey = GetCarSaveKey(preview);

        // Если машина ещё никогда не сохранялась,
        // используется значение IsOpen из Inspector.
        int defaultValue = preview.IsOpen ? 1 : 0;

        preview.IsOpen =
            PlayerPrefs.GetInt(saveKey, defaultValue) == 1;
    }

    private string GetCarSaveKey(CarPreviewEntry preview)
    {
        string carId = preview.SaveId;

        if (string.IsNullOrWhiteSpace(carId))
        {
            if (preview.CarData != null)
                carId = preview.CarData.name;
            else
                carId = "UnknownCar";
        }

        return $"GARAGE_CAR_UNLOCKED_{carId}";
    }

    private void ShowCurrentCarImmediately()
    {
        CarItemSO currentCar = GetCurrentCar();

        if (currentCar == null)
            return;

        _currentPreviewIndex = FindPreviewIndex(currentCar);

        if (_currentPreviewIndex < 0)
        {
            Debug.LogError(
                $"There is no 3D preview assigned for car: " +
                $"{currentCar.PlayerName}",
                this);

            return;
        }

        CarPreviewEntry preview =
            _carPreviews[_currentPreviewIndex];

        if (preview.Visual == null)
        {
            Debug.LogError(
                $"Visual is not assigned for car: " +
                $"{currentCar.PlayerName}",
                this);

            return;
        }

        preview.Visual.transform.localPosition =
            preview.StartLocalPosition;

        preview.Visual.SetActive(true);

        UpdateCarUI(currentCar, false);
    }

    private void SwitchCar(bool forward)
    {
        CarItemSO previousCar = GetCurrentCar();

        if (previousCar == null)
            return;

        int previousIndex = FindPreviewIndex(previousCar);

        if (forward)
            CarSelectionManager.Instance.NextCar();
        else
            CarSelectionManager.Instance.PreviousCar();

        CarItemSO newCar = GetCurrentCar();

        if (newCar == null)
            return;

        int newIndex = FindPreviewIndex(newCar);

        if (newIndex < 0)
        {
            Debug.LogError(
                $"There is no 3D preview assigned for car: " +
                $"{newCar.PlayerName}",
                this);

            return;
        }

        if (previousIndex < 0 || previousIndex == newIndex)
        {
            ShowPreviewImmediately(newIndex);
            UpdateCarUI(newCar, true);
            return;
        }

        AnimateCarSwitch(
            previousIndex,
            newIndex,
            newCar,
            forward);
    }

    private void AnimateCarSwitch(
        int previousIndex,
        int newIndex,
        CarItemSO newCar,
        bool forward)
    {
        CarPreviewEntry previousPreview =
            _carPreviews[previousIndex];

        CarPreviewEntry newPreview =
            _carPreviews[newIndex];

        if (previousPreview.Visual == null ||
            newPreview.Visual == null)
        {
            ShowPreviewImmediately(newIndex);
            UpdateCarUI(newCar, true);
            return;
        }

        _isAnimating = true;

        Transform previousTransform =
            previousPreview.Visual.transform;

        Transform newTransform =
            newPreview.Visual.transform;

        previousTransform.DOKill();
        newTransform.DOKill();
        _switchSequence?.Kill();

        float exitDirection = forward ? -1f : 1f;
        float enterDirection = -exitDirection;

        Vector3 previousExitPosition =
            previousPreview.StartLocalPosition +
            Vector3.right * exitDirection * _moveDistance;

        Vector3 newEnterPosition =
            newPreview.StartLocalPosition +
            Vector3.right * enterDirection * _moveDistance;

        newPreview.Visual.SetActive(false);

        _switchSequence = DOTween.Sequence();

        _switchSequence.Append(
            previousTransform
                .DOLocalMove(
                    previousExitPosition,
                    _animationTime)
                .SetEase(Ease.InBack));

        _switchSequence.AppendCallback(() =>
        {
            previousPreview.Visual.SetActive(false);

            previousTransform.localPosition =
                previousPreview.StartLocalPosition;

            newTransform.localPosition =
                newEnterPosition;

            newPreview.Visual.SetActive(true);

            _currentPreviewIndex = newIndex;

            UpdateCarUI(newCar, true);
        });

        _switchSequence.Append(
            newTransform
                .DOLocalMove(
                    newPreview.StartLocalPosition,
                    _animationTime)
                .SetEase(Ease.OutBack));

        _switchSequence.OnComplete(() =>
        {
            _isAnimating = false;
            _switchSequence = null;
        });
    }

    private void ShowPreviewImmediately(int previewIndex)
    {
        for (int i = 0; i < _carPreviews.Count; i++)
        {
            CarPreviewEntry preview = _carPreviews[i];

            if (preview?.Visual == null)
                continue;

            preview.Visual.transform.DOKill();

            preview.Visual.transform.localPosition =
                preview.StartLocalPosition;

            preview.Visual.SetActive(i == previewIndex);
        }

        _currentPreviewIndex = previewIndex;
    }

    private void UpdateCarUI(CarItemSO car, bool animate)
    {
        if (car == null)
            return;

        if (_carName != null)
            _carName.text = car.PlayerName;

        UpdateSlider(_speedSlider, car.speed, animate);
        UpdateSlider(_healthSlider, car.health, animate);
        UpdateSlider(_damageSlider, car.damage, animate);

        UpdateRideBuyButton();
    }

    private void UpdateRideBuyButton()
    {
        _buttonTextTween?.Kill();
        _buttonTextTween = null;

        if (_startButton == null || _buttonText == null)
            return;

        CarPreviewEntry preview = GetCurrentPreview();

        if (preview == null)
        {
            _buttonText.text = _rideButtonText;
            _startButton.interactable = false;
            return;
        }

        _startButton.interactable = true;

        if (preview.IsOpen)
        {
            _buttonText.text = _rideButtonText;
        }
        else
        {
            int price = Mathf.Max(0, preview.Cost);

            _buttonText.text =
                price > 0
                    ? $"{_buyButtonText} {price}"
                    : $"{_buyButtonText} FREE";
        }
    }

    private void ShowNotEnoughMoneyMessage()
    {
        if (_buttonText == null)
            return;

        _buttonTextTween?.Kill();

        _buttonText.text = _notEnoughMoneyText;

        _buttonTextTween = DOVirtual.DelayedCall(
                _notEnoughMessageDuration,
                UpdateRideBuyButton)
            .SetUpdate(true);

        RectTransform buttonTransform =
            _startButton.transform as RectTransform;

        if (buttonTransform == null)
            return;

        buttonTransform.DOKill();

        buttonTransform
            .DOShakeAnchorPos(
                duration: 0.3f,
                strength: 12f,
                vibrato: 12,
                randomness: 45f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                buttonTransform.anchoredPosition =
                    Vector2.zero;
            });
    }

    private void UpdateSlider(
        Slider slider,
        float value,
        bool animate)
    {
        if (slider == null)
            return;

        slider.DOKill();

        if (!animate)
        {
            slider.SetValueWithoutNotify(value);
            return;
        }

        slider
            .DOValue(value, _sliderAnimationTime)
            .SetEase(Ease.OutQuad);
    }

    private CarItemSO GetCurrentCar()
    {
        if (CarSelectionManager.Instance == null)
        {
            Debug.LogError(
                "CarSelectionManager.Instance is null.",
                this);

            return null;
        }

        return CarSelectionManager.Instance.GetCurrentCar();
    }

    private CarPreviewEntry GetCurrentPreview()
    {
        CarItemSO currentCar = GetCurrentCar();

        if (currentCar == null)
            return null;

        int previewIndex = FindPreviewIndex(currentCar);

        if (previewIndex < 0 ||
            previewIndex >= _carPreviews.Count)
        {
            return null;
        }

        return _carPreviews[previewIndex];
    }

    private int FindPreviewIndex(CarItemSO car)
    {
        if (car == null || _carPreviews == null)
            return -1;

        for (int i = 0; i < _carPreviews.Count; i++)
        {
            if (_carPreviews[i]?.CarData == car)
                return i;
        }

        return -1;
    }

    private void AnimateButton(RectTransform button)
    {
        if (button == null)
            return;

        button.DOKill();

        button.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            button.DOScale(0.8f, _buttonAnimTime));

        sequence.Append(
            button
                .DOScale(1f, _buttonAnimTime)
                .SetEase(Ease.OutBack));
    }

    private void OnDisable()
    {
        _switchSequence?.Kill();
        _switchSequence = null;

        _buttonTextTween?.Kill();
        _buttonTextTween = null;

        _isAnimating = false;

        _nextButton?.DOKill();
        _backButton?.DOKill();
        _startButton?.transform.DOKill();

        if (_carPreviews == null)
            return;

        foreach (CarPreviewEntry preview in _carPreviews)
        {
            if (preview?.Visual != null)
                preview.Visual.transform.DOKill();
        }
    }

    private void OnDestroy()
    {
        if (_startButton != null)
        {
            _startButton.onClick.RemoveListener(
                OnRideBuyButtonClicked);
        }
    }
}