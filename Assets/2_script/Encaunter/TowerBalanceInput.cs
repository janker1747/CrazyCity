using UnityEngine;
using UnityEngine.Events;

public sealed class TowerBalanceInput : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private UnityEvent onLeftPressed;
    [SerializeField] private UnityEvent onLeftReleased;

    [SerializeField] private UnityEvent onRightPressed;
    [SerializeField] private UnityEvent onRightReleased;

    private bool leftPressed;
    private bool rightPressed;

    public bool LeftPressed => leftPressed;
    public bool RightPressed => rightPressed;

    /// <summary>
    /// -1 — удерживается левая сторона.
    ///  0 — ничего или обе стороны.
    ///  1 — удерживается правая сторона.
    /// </summary>
    public float Direction
    {
        get
        {
            if (leftPressed == rightPressed)
                return 0f;

            return leftPressed ? -1f : 1f;
        }
    }

    public void SetLeftPressed(bool value)
    {
        if (leftPressed == value)
            return;

        leftPressed = value;

        if (leftPressed)
            onLeftPressed?.Invoke();
        else
            onLeftReleased?.Invoke();
    }

    public void SetRightPressed(bool value)
    {
        if (rightPressed == value)
            return;

        rightPressed = value;

        if (rightPressed)
            onRightPressed?.Invoke();
        else
            onRightReleased?.Invoke();
    }

    private void OnDisable()
    {
        leftPressed = false;
        rightPressed = false;
    }
}