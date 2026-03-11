using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GarageUIManager : MonoBehaviour
{
    [SerializeField] private List<CarItemSO> _cars;
    [SerializeField] private Image _carSprite;
    [SerializeField] private TMP_Text _carName;

    [Header("Animation switch car Setting")]
    [SerializeField] private RectTransform _carUIRoot;
    [SerializeField] private float _moveDistance = 600f;
    [SerializeField] private float _animationTime = 0.35f;

    [Header("Button animation setting")]
    [SerializeField] private RectTransform _nextButton;
    [SerializeField] private RectTransform _backButton;
    [SerializeField] private float _buttonAnimTime = 0.12f;

    [Header("Sliders")]
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _damageSlider;


    private bool _isAnimating;

    private int _playerIndex = 0;

    private Player _player;

    private void Awake()
    {
        UpdateCarUI();
        DontDestroyOnLoad(this);
    }

    public void GoForward()
    {
        AnimateButton(_nextButton);

        CarSelectionManager.Instance.NextCar();

        AnimateForward();
    }

    public void GoBack()
    {
        AnimateButton(_backButton);

        CarSelectionManager.Instance.PreviousCar();

        AnimateBack();
    }

    private void UpdateCarUI()
    {
        CarItemSO car = CarSelectionManager.Instance.GetCurrentCar();

        _carSprite.sprite = car.PlayerSprite;

        _carName.text = car.PlayerName;        
        _speedSlider.DOValue(car.speed, 0.4f); 
        _healthSlider.DOValue(car.health, 0.4f);
        _damageSlider.DOValue(car.damage, 0.4f);
    }

    public void ChoisePlayer()
    {
        _carSprite.sprite = _cars[_playerIndex].PlayerSprite;
        _carName.text = _cars[_playerIndex].PlayerName;
    }

    public Player SetPlayer()
    {
        _player = _cars[_playerIndex].PlayerPrefab;

        return _player;
    }

    private void AnimateForward()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(_carUIRoot.DOAnchorPosX(-_moveDistance, _animationTime).SetEase(Ease.InBack));

        sequence.AppendCallback(() =>
        {
            _carUIRoot.anchoredPosition = new Vector2(_moveDistance, 0);
            UpdateCarUI();
        });

        sequence.Append(_carUIRoot.DOAnchorPosX(0, _animationTime).SetEase(Ease.OutBack));

        sequence.OnComplete(() =>
        {
            _isAnimating = false;
        });
    }

    private void AnimateBack()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(_carUIRoot.DOAnchorPosX(_moveDistance, _animationTime).SetEase(Ease.InBack));

        sequence.AppendCallback(() =>
        {
            _carUIRoot.anchoredPosition = new Vector2(-_moveDistance, 0);
            UpdateCarUI();
        });

        sequence.Append(_carUIRoot.DOAnchorPosX(0, _animationTime).SetEase(Ease.OutBack));

        sequence.OnComplete(() =>
        {
            _isAnimating = false;
        });
    }

    private void AnimateButton(RectTransform button)
    {
        button.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Append(button.DOScale(0.8f, _buttonAnimTime));
        seq.Append(button.DOScale(1f, _buttonAnimTime).SetEase(Ease.OutBack));
    }
}

