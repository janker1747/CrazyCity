using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarItem", menuName = "CarItems/CarItem")]
public class CarItemSO : ScriptableObject
{
    [SerializeField] public Player PlayerPrefab;
    [SerializeField] public Sprite PlayerSprite;
    [SerializeField] public string PlayerName;

    [SerializeField] public float speed;
    [SerializeField] public float health;
    [SerializeField] public float damage;

}
