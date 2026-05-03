using UnityEngine;

[CreateAssetMenu(fileName = "MoonStoneCargo", menuName = "Cargo/Moon Stone Cargo")]
public class MoonStoneCargo : Cargo
{
    [Header("Moon Stone")]
    [SerializeField, Min(0f)] private float gravityMultiplier = 0.5f;

    public override void OnPickup(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning($"{nameof(MoonStoneCargo)}: player is missing on pickup.");
            return;
        }

        player.SetGravityMultiplier(gravityMultiplier);
    }

    public override void OnDeliver(Player player)
    {
        RestoreGravity(player);
    }

    public override void OnFail(Player player)
    {
        RestoreGravity(player);
    }

    private void RestoreGravity(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning($"{nameof(MoonStoneCargo)}: player is missing while restoring gravity.");
            return;
        }

        player.SetGravityMultiplier(1f);
    }
}
