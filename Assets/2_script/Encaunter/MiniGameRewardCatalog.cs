using UnityEngine;

[CreateAssetMenu(menuName = "Crazy City/Mini Game Reward Catalog")]
public sealed class MiniGameRewardCatalog : ScriptableObject
{
    [SerializeField] private Cargo[] regularCargo;
    [SerializeField] private Cargo[] timedCargo;
    [SerializeField] private Cargo[] healthCargo;

    public Cargo GetRandomRegular() => GetRandom(regularCargo);
    public Cargo GetRandomTimed() => GetRandom(timedCargo);
    public Cargo GetRandomHealth() => GetRandom(healthCargo);

    private static Cargo GetRandom(Cargo[] cargos)
    {
        if (cargos == null || cargos.Length == 0)
            return null;

        return cargos[Random.Range(0, cargos.Length)];
    }
}
