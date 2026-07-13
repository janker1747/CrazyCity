using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverDataView : MonoBehaviour
{
    [Header("Car")]
    [SerializeField] private Image _carImage;
    [SerializeField] private TMP_Text _carNameText;

    [Header("Result")]
    [SerializeField] private TMP_Text _gradeText;

    [Header("Delivered Cargo")]
    [SerializeField] private Transform _content;
    [SerializeField] private Image _spritePrefab;
    [SerializeField] private Vector2 _defaultIconSize = new Vector2(152f, 152f);
    [SerializeField] private float _spawnDelay = 0.05f;
    [SerializeField] private float _animationDuration = 0.25f;

    private readonly List<Image> _spawnedSprites = new List<Image>();

    private void OnEnable()
    {
        StopAllCoroutines();
        ShowGameData();
        StartCoroutine(SpawnDeliveredSprites());
    }

    public void ShowGameData()
    {
        GameData gameData = GameData.Instance;

        string grade = gameData.Grade;

        if (_gradeText != null)
            _gradeText.text = grade;

        ShowCar(gameData.CarItem);
    }

    public void ClearSprites()
    {
        for (int i = _spawnedSprites.Count - 1; i >= 0; i--)
        {
            if (_spawnedSprites[i] != null)
                Destroy(_spawnedSprites[i].gameObject);
        }

        _spawnedSprites.Clear();
    }

    private void ShowCar(CarItemSO carItem)
    {
        if (carItem == null)
            return;

        if (_carNameText != null)
            _carNameText.text = carItem.PlayerName;
   
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

        image.transform
            .DOScale(1f, Mathf.Max(0f, _animationDuration))
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}
