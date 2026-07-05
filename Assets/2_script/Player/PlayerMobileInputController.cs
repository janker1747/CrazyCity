using ArcadeVP;
using UnityEngine;

public class PlayerMobileInputController : MonoBehaviour
{
    [SerializeField] private ArcadeVehicleController vehicleController;
    [SerializeField] private WallRideJumper wallRideJumper;
    [SerializeField] private bool overrideInputWhileEnabled = true;

    private bool leftPressed;
    private bool rightPressed;
    private bool forwardPressed;
    private bool backPressed;

    private void Awake()
    {
        if (vehicleController == null)
            vehicleController = GetComponent<ArcadeVehicleController>();

        if (wallRideJumper == null)
            wallRideJumper = GetComponent<WallRideJumper>();
    }

    private void OnEnable()
    {
        SetOverrideInput(overrideInputWhileEnabled);
        ApplyInput();
    }

    private void OnDisable()
    {
        leftPressed = false;
        rightPressed = false;
        forwardPressed = false;
        backPressed = false;

        ApplyInput();
        SetOverrideInput(false);
    }

    public void LeftDown()
    {
        leftPressed = true;
        ApplyInput();
    }

    public void LeftUp()
    {
        leftPressed = false;
        ApplyInput();
    }

    public void RightDown()
    {
        rightPressed = true;
        ApplyInput();
    }

    public void RightUp()
    {
        rightPressed = false;
        ApplyInput();
    }

    public void ForwardDown()
    {
        forwardPressed = true;
        ApplyInput();
    }

    public void ForwardUp()
    {
        forwardPressed = false;
        ApplyInput();
    }

    public void BackDown()
    {
        backPressed = true;
        ApplyInput();
    }

    public void BackUp()
    {
        backPressed = false;
        ApplyInput();
    }

    public void StartWallRide()
    {
        if (wallRideJumper != null)
            wallRideJumper.RequestStartWallRide();
    }

    private void ApplyInput()
    {
        if (vehicleController == null)
            return;

        vehicleController.overrideHorizontal =
            GetAxis(leftPressed, rightPressed);

        vehicleController.overrideVertical =
            GetAxis(backPressed, forwardPressed);

        vehicleController.overrideJump = 0f;
    }

    private float GetAxis(bool negative, bool positive)
    {
        if (negative == positive)
            return 0f;

        return positive ? 1f : -1f;
    }

    private void SetOverrideInput(bool value)
    {
        if (vehicleController != null)
            vehicleController.overrideInput = value;
    }
}
