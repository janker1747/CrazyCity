using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TowerBalanceInputZone :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    public enum InputSide
    {
        Left,
        Right
    }

    [SerializeField] private InputSide side;
    [SerializeField] private TowerBalanceInput input;

    private int activePointerId = int.MinValue;
    private bool isPressed;

    private void Awake()
    {
        if (input == null)
            input = GetComponentInParent<TowerBalanceInput>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPressed)
            return;

        isPressed = true;
        activePointerId = eventData.pointerId;

        SetInput(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
            return;

        if (eventData.pointerId != activePointerId)
            return;

        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // На телефоне палец может выйти за пределы зоны.
        // Если хочешь продолжать удержание даже за пределами,
        // удали содержимое этого метода.

        if (!isPressed)
            return;

        if (eventData.pointerId != activePointerId)
            return;

        Release();
    }

    private void OnDisable()
    {
        Release();
    }

    private void SetInput(bool value)
    {
        if (input == null)
            return;

        switch (side)
        {
            case InputSide.Left:
                input.SetLeftPressed(value);
                break;

            case InputSide.Right:
                input.SetRightPressed(value);
                break;
        }
    }

    private void Release()
    {
        if (!isPressed)
            return;

        SetInput(false);

        isPressed = false;
        activePointerId = int.MinValue;
    }
}