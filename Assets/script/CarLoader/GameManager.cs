using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<Player> _playersPrefab = new List<Player>();

    private int _playerIndex = 0;

    private Player _player;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void ChoisePlayer(int index)
    {
        _playerIndex = index;
    }

    public Player SetPlayer()
    {
        _player = _playersPrefab[_playerIndex];

        return _player;
    }
}

