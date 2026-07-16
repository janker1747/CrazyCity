using DG.Tweening;
using TMPro;
using UnityEngine;

public class WalletView : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;

    [Header("Display")]
    [SerializeField] private string _prefix = "";
    [SerializeField] private string _suffix = "";
    [SerializeField] private float _animationDuration = 0.35f;
    [SerializeField] private bool _animateChanges = true;

    private int _displayedGold;
    private Tween _goldTween;

    private void OnEnable()
    {
        Wallet wallet = GameData.Instance.Wallet;

        wallet.GoldChanged += OnGoldChanged;

        _displayedGold = wallet.CurrentGold;
        UpdateText(_displayedGold);
    }

    private void OnDisable()
    {
        GameData.Instance.Wallet.GoldChanged -= OnGoldChanged;

        _goldTween?.Kill();
        _goldTween = null;
    }

    private void OnGoldChanged(int newGold)
    {
        if (!_animateChanges || !gameObject.activeInHierarchy)
        {
            _displayedGold = newGold;
            UpdateText(_displayedGold);
            return;
        }

        _goldTween?.Kill();

        _goldTween = DOTween
            .To(
                () => _displayedGold,
                value =>
                {
                    _displayedGold = value;
                    UpdateText(_displayedGold);
                },
                newGold,
                Mathf.Max(0.01f, _animationDuration))
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private void UpdateText(int gold)
    {
        if (_goldText == null)
            return;

        _goldText.text = $"{_prefix}{gold}{_suffix}";
    }
}