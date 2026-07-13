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

    private Sequence _switchSequence;
    private bool _isAnimating;
    private int _currentPreviewIndex = -1;

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

    public Player SetPlayer()
    {
        CarItemSO currentCar = GetCurrentCar();

        if (currentCar == null)
        {
            Debug.LogError("Cannot set player because the selected car was not found.");
            return null;
        }

        return currentCar.PlayerPrefab;
    }

    private void InitializePreviews()
    {
        for (int i = 0; i < _carPreviews.Count; i++)
        {
            CarPreviewEntry preview = _carPreviews[i];

            if (preview.Visual == null)
                continue;

            preview.StartLocalPosition = preview.Visual.transform.localPosition;
            preview.Visual.SetActive(false);
        }
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
                $"There is no 3D preview assigned for car: {currentCar.PlayerName}");
            return;
        }

        CarPreviewEntry preview = _carPreviews[_currentPreviewIndex];

        preview.Visual.transform.localPosition = preview.StartLocalPosition;
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
                $"There is no 3D preview assigned for car: {newCar.PlayerName}");
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
        _isAnimating = true;

        CarPreviewEntry previousPreview = _carPreviews[previousIndex];
        CarPreviewEntry newPreview = _carPreviews[newIndex];

        Transform previousTransform = previousPreview.Visual.transform;
        Transform newTransform = newPreview.Visual.transform;

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
                .DOLocalMove(previousExitPosition, _animationTime)
                .SetEase(Ease.InBack));

        _switchSequence.AppendCallback(() =>
        {
            previousPreview.Visual.SetActive(false);
            previousTransform.localPosition =
                previousPreview.StartLocalPosition;

            newTransform.localPosition = newEnterPosition;
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

            if (preview.Visual == null)
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
        if (_carName != null)
            _carName.text = car.PlayerName;

        UpdateSlider(_speedSlider, car.speed, animate);
        UpdateSlider(_healthSlider, car.health, animate);
        UpdateSlider(_damageSlider, car.damage, animate);
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

        slider.DOValue(value, _sliderAnimationTime)
            .SetEase(Ease.OutQuad);
    }

    private CarItemSO GetCurrentCar()
    {
        if (CarSelectionManager.Instance == null)
        {
            Debug.LogError(
                "CarSelectionManager.Instance is null.");
            return null;
        }

        return CarSelectionManager.Instance.GetCurrentCar();
    }

    private int FindPreviewIndex(CarItemSO car)
    {
        for (int i = 0; i < _carPreviews.Count; i++)
        {
            if (_carPreviews[i].CarData == car)
                return i;
        }

        return -1;
    }

    private void AnimateButton(RectTransform button)
    {
        if (button == null)
            return;

        button.DOKill();

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

        _isAnimating = false;

        _nextButton?.DOKill();
        _backButton?.DOKill();

        foreach (CarPreviewEntry preview in _carPreviews)
        {
            if (preview.Visual != null)
                preview.Visual.transform.DOKill();
        }
    }
}