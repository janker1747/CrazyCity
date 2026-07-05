using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaggageHealthUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private Image iconPrefab;
    [SerializeField] private Vector2 defaultIconSize = new(40f, 40f);

    private readonly List<Image> icons = new();

    public Image AddSprite()
    {
        Sprite sprite = iconPrefab.sprite;
        return AddSprite(sprite, content);
    }

    public Image AddSprite(Sprite sprite, Transform targetContent)
    {
        if (targetContent == null || sprite == null)
            return null;

        Image icon = CreateIcon(targetContent);
        icon.sprite = sprite;
        icon.gameObject.SetActive(true);
        icons.Add(icon);

        return icon;
    }

    public void SetContent(Transform targetContent)
    {
        content = targetContent;
    }

    public void RemoveLast()
    {
        if (icons.Count == 0)
            return;

        int lastIndex = icons.Count - 1;
        Image icon = icons[lastIndex];
        icons.RemoveAt(lastIndex);

        if (icon != null)
            Destroy(icon.gameObject);
    }

    public void Clear()
    {
        for (int i = icons.Count - 1; i >= 0; i--)
        {
            if (icons[i] != null)
                Destroy(icons[i].gameObject);
        }

        icons.Clear();
    }

    private Image CreateIcon(Transform targetContent)
    {
        if (iconPrefab != null)
            return Instantiate(iconPrefab, targetContent);

        GameObject iconObject = new GameObject(
            "Baggage Health Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        iconObject.transform.SetParent(targetContent, false);

        Image icon = iconObject.GetComponent<Image>();
        icon.rectTransform.sizeDelta = defaultIconSize;

        return icon;
    }
}
