using UnityEngine;

public class InsideHouseZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponentInChildren<CameraOffsetController>();
        if (controller != null)
            controller.EnterZone();
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponentInChildren<CameraOffsetController>();
        if (controller != null)
            controller.ExitZone();
    }
}
