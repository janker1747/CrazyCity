using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoxDragItem :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private BoxReturnMiniGame miniGame;
    private RectTransform rectTransform;

    private Vector3 pointerOffset;
    private bool interactable;
    private bool isDragging;

    public RectTransform RectTransform => rectTransform;

    public void Initialize(BoxReturnMiniGame owner)
    {
        miniGame = owner;
        rectTransform = transform as RectTransform;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (!value)
            isDragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactable || rectTransform == null)
            return;

        RectTransform parent =
            rectTransform.parent as RectTransform;

        if (parent == null)
            return;

        rectTransform.DOKill();
        rectTransform.SetAsLastSibling();

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 pointerWorldPosition))
        {
            return;
        }

        pointerOffset =
            rectTransform.position - pointerWorldPosition;

        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactable || !isDragging)
            return;

        RectTransform parent =
            rectTransform.parent as RectTransform;

        if (parent == null)
            return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 pointerWorldPosition))
        {
            rectTransform.position =
                pointerWorldPosition + pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!interactable || !isDragging)
            return;

        isDragging = false;
        miniGame.OnBoxReleased(this);
    }

    private void OnDisable()
    {
        isDragging = false;
    }
}