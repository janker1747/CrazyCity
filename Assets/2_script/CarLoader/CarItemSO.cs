using UnityEngine;

[CreateAssetMenu(
    fileName = "CarItem",
    menuName = "CarItems/CarItem")]
public class CarItemSO : ScriptableObject
{
    [Header("Player Prefab")]
    [SerializeField] public Player PlayerPrefab;

    [Header("Information")]
    [SerializeField] public string PlayerName;

    [Header("Characteristics")]
    [SerializeField] public float speed;
    [SerializeField] public float health;
    [SerializeField] public float damage;
}