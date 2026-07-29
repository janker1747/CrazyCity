using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputButton :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    public enum InputAction
    {
        Left,
        Right,
        Forward,
        Back,
        WallRide
    }

    private PlayerMobileInputController controller;
    private InputAction inputAction;
    private int activePointerId = int.MinValue;

    public void Configure(
        PlayerMobileInputController inputController,
        InputAction action)
    {
        ReleaseInput();
        controller = inputController;
        inputAction = action;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller == null || activePointerId != int.MinValue)
            return;

        activePointerId = eventData.pointerId;
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ReleaseInput();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ReleaseInput();
    }

    private void OnDisable()
    {
        ReleaseInput();
    }

    private void ReleaseInput()
    {
        if (activePointerId == int.MinValue)
            return;

        activePointerId = int.MinValue;
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        if (controller == null)
            return;

        switch (inputAction)
        {
            case InputAction.Left:
                if (pressed) controller.LeftDown();
                else controller.LeftUp();
                break;

            case InputAction.Right:
                if (pressed) controller.RightDown();
                else controller.RightUp();
                break;

            case InputAction.Forward:
                if (pressed) controller.ForwardDown();
                else controller.ForwardUp();
                break;

            case InputAction.Back:
                if (pressed) controller.BackDown();
                else controller.BackUp();
                break;

            case InputAction.WallRide:
                if (pressed)
                    controller.StartWallRide();
                break;
        }
    }
}
