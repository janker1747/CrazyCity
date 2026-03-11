using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarGameLoader : MonoBehaviour
{
    [SerializeField] private List<Transform> _spawnPlayerPoint;

    private Player _player;

    private void Awake()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        int randomIndex = Random.Range(0, _spawnPlayerPoint.Count);
        Transform spawnPosition = _spawnPlayerPoint[randomIndex];

        _player = Instantiate(CarSelectionManager.Instance.GetPlayerPrefab(), spawnPosition.position, spawnPosition.rotation);
    }
}
