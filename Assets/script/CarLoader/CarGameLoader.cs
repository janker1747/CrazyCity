using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarGameLoader : MonoBehaviour
{
    [SerializeField] private List<Transform> _spawnPlayerPoint;

    private GameManager _gameManager;
    private Player _player;

    private void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
        _player = _gameManager.SetPlayer();

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        int randomIndex = Random.Range(0, _spawnPlayerPoint.Count);
        Transform spawnPosition = _spawnPlayerPoint[randomIndex];

        _player = Instantiate(_player, spawnPosition.position, spawnPosition.rotation);
    }
}
