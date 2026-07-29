using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverDataView : MonoBehaviour
{
    [Header("Car")]
    [SerializeField] private TMP_Text _carNameText;

    [Header("Result")]
    [SerializeField] private TMP_Text _gradeText;

    [Header("Coins")]
    [SerializeField] private TMP_Text _earnedCoinsText;
    [SerializeField] private TMP_Text _totalCoinsText;
    [SerializeField] private RectTransform _coinsAnimationRoot;
    [SerializeField] private float _coinsAnimationDuration = 0.5f;

    [Header("Delivered Cargo")]
    [SerializeField] private Transform _content;
    [SerializeField] private Image _spritePrefab;
    [SerializeField] private Vector2 _defaultIconSize = new Vector2(152f, 152f);
    [SerializeField] private float _spawnDelay = 0.05f;
    [SerializeField] private float _animationDuration = 0.25f;

    private readonly List<Image> _spawnedSprites = new List<Image>();

    private Sequence _coinsSequence;

    private void OnEnable()
    {
        StopAllCoroutines();

        ShowGameData();
        StartCoroutine(SpawnDeliveredSprites());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        _coinsSequence?.Kill();
        _coinsSequence = null;

        if (_coinsAnimationRoot != null)
            _coinsAnimationRoot.DOKill();
    }

    public void ShowGameData()
    {
        GameData gameData = GameData.Instance;

        string grade = gameData.Grade;

        if (_gradeText != null)
            _gradeText.text = grade;

        ShowCar(gameData.CarItem);

        // Начисляет награду только один раз.
        int earnedCoins = gameData.ClaimRunCoins();
        int totalCoins = gameData.Wallet.CurrentGold;

        ShowCoins(earnedCoins, totalCoins);
    }

    private void ShowCoins(int earnedCoins, int totalCoins)
    {
        _coinsSequence?.Kill();

        int displayedEarnedCoins = 0;
        int displayedTotalCoins = Mathf.Max(0, totalCoins - earnedCoins);

        if (_earnedCoinsText != null)
            _earnedCoinsText.text = "+0";

        if (_totalCoinsText != null)
            _totalCoinsText.text = displayedTotalCoins.ToString();

        _coinsSequence = DOTween.Sequence();

        if (_earnedCoinsText != null)
        {
            Tween earnedTween = DOTween.To(
                () => displayedEarnedCoins,
                value =>
                {
                    displayedEarnedCoins = value;
                    _earnedCoinsText.text = $"+{displayedEarnedCoins}";
                },
                earnedCoins,
                Mathf.Max(0.01f, _coinsAnimationDuration));

            _coinsSequence.Join(earnedTween);
        }

        if (_totalCoinsText != null)
        {
            Tween totalTween = DOTween.To(
                () => displayedTotalCoins,
                value =>
                {
                    displayedTotalCoins = value;
                    _totalCoinsText.text = displayedTotalCoins.ToString();
                },
                totalCoins,
                Mathf.Max(0.01f, _coinsAnimationDuration));

            _coinsSequence.Join(totalTween);
        }

        if (_coinsAnimationRoot != null)
        {
            _coinsAnimationRoot.DOKill();
            _coinsAnimationRoot.localScale = Vector3.one;

            _coinsAnimationRoot
                .DOPunchScale(
                    new Vector3(0.15f, 0.15f, 0f),
                    Mathf.Max(0.01f, _coinsAnimationDuration),
                    5,
                    0.5f)
                .SetUpdate(true);
        }

        _coinsSequence
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    public void ClearSprites()
    {
        for (int i = _spawnedSprites.Count - 1; i >= 0; i--)
        {
            Image spawnedImage = _spawnedSprites[i];

            if (spawnedImage == null)
                continue;

            spawnedImage.transform.DOKill();
            Destroy(spawnedImage.gameObject);
        }

        _spawnedSprites.Clear();
    }

    private void ShowCar(CarItemSO carItem)
    {
        if (carItem == null)
            return;

        if (_carNameText != null)
            _carNameText.text = carItem.PlayerName;

        // Здесь можно назначить спрайт машины,
        // когда будет известно название поля в CarItemSO.
        //
        // if (_carImage != null)
        //     _carImage.sprite = carItem.Icon;
    }

    private IEnumerator SpawnDeliveredSprites()
    {
        ClearSprites();

        if (_content == null)
            yield break;

        List<Sprite> sprites = GameData.Instance.Sprites;

        for (int i = 0; i < sprites.Count; i++)
        {
            Sprite sprite = sprites[i];

            if (sprite == null)
                continue;

            Image image = CreateSpriteImage(sprite);
            AnimateSprite(image);

            if (_spawnDelay > 0f)
                yield return new WaitForSecondsRealtime(_spawnDelay);
        }
    }

    private Image CreateSpriteImage(Sprite sprite)
    {
        Image image;

        if (_spritePrefab != null)
        {
            image = Instantiate(_spritePrefab, _content);
        }
        else
        {
            GameObject imageObject = new GameObject(
                "Delivered Cargo Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            imageObject.transform.SetParent(_content, false);

            image = imageObject.GetComponent<Image>();
            image.rectTransform.sizeDelta = _defaultIconSize;
        }

        image.sprite = sprite;
        image.gameObject.SetActive(true);
        image.transform.localScale = Vector3.zero;

        _spawnedSprites.Add(image);

        return image;
    }

    private void AnimateSprite(Image image)
    {
        if (image == null)
            return;

        image.transform.DOKill();

        image.transform
            .DOScale(1f, Mathf.Max(0.01f, _animationDuration))
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}