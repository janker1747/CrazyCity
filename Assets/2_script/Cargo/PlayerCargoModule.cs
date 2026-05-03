using UnityEngine;

public class PlayerCargoModule : MonoBehaviour
{
    private Player player;
    private CargoManager cargoManager;
    private CargoArrowUI cargoArrowUI;
    private Cargo currentCargo;
    private DeliveryPoint currentDeliveryPoint;
    private float elapsedTime;
    private float damageMultiplier = 1f;

    public Cargo CurrentCargo => currentCargo;
    public bool CanTakeCargo => currentCargo == null;

    public void Initialize(Player owner, CargoManager manager, CargoArrowUI arrowUI)
    {
        player = owner;
        cargoManager = manager;
        cargoArrowUI = arrowUI;

        if (cargoArrowUI != null && player != null)
        {
            cargoArrowUI.SetPlayer(player.transform);
            cargoArrowUI.Hide();
        }
    }

    public void Tick(float deltaTime)
    {
        if (currentCargo == null)
            return;

        elapsedTime += deltaTime;

        if (currentCargo.DeliveryTime > 0f && elapsedTime >= currentCargo.DeliveryTime)
            FailDelivery();
    }

    public bool TryTakeCargo(Cargo cargo)
    {
        if (player == null)
            player = GetComponent<Player>();

        if (player == null)
        {
            Debug.LogWarning($"{nameof(PlayerCargoModule)} on {name}: Player component is missing.");
            return false;
        }

        if (cargo == null)
        {
            Debug.LogWarning($"{nameof(PlayerCargoModule)} on {name}: cargo is null.");
            return false;
        }

        if (!CanTakeCargo)
            return false;

        if (cargoManager == null)
        {
            Debug.LogWarning($"{nameof(PlayerCargoModule)} on {name}: CargoManager is not assigned.");
            return false;
        }

        DeliveryPoint deliveryPoint = cargoManager.CreateDeliveryPointForPlayer(player.transform.position, player);
        if (deliveryPoint == null)
        {
            Debug.LogWarning($"{nameof(PlayerCargoModule)} on {name}: failed to create delivery point.");
            return false;
        }

        currentCargo = cargo;
        currentDeliveryPoint = deliveryPoint;
        elapsedTime = 0f;
        damageMultiplier = 1f;

        currentCargo.OnPickup(player);

        if (cargoArrowUI != null)
            cargoArrowUI.Show(currentDeliveryPoint.transform);
        else
            Debug.LogWarning($"{nameof(PlayerCargoModule)} on {name}: CargoArrowUI is not assigned.");

        return true;
    }

    public void CompleteDelivery(bool success)
    {
        if (currentCargo == null)
        {
            Debug.LogWarning($"{nameof(PlayerCargoModule)} on {name}: no cargo to complete.");
            return;
        }

        Cargo completedCargo = currentCargo;

        if (success)
        {
            completedCargo.OnDeliver(player);
            int reward = Mathf.Max(0, completedCargo.CalculateValue(elapsedTime, damageMultiplier));

            if (reward > 0 && player != null && player.ScoreSystem != null)
                player.ScoreSystem.AddScore(reward);
        }
        else
        {
            completedCargo.OnFail(player);
        }

        ClearCargoState();
    }

    public void FailDelivery()
    {
        if (currentCargo == null)
            return;

        CompleteDelivery(false);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Clamp01(multiplier);
    }

    private void ClearCargoState()
    {
        currentCargo = null;
        currentDeliveryPoint = null;
        elapsedTime = 0f;
        damageMultiplier = 1f;

        if (cargoArrowUI != null)
            cargoArrowUI.Hide();

        if (cargoManager != null)
            cargoManager.OnDeliveryFinished();
    }
}
