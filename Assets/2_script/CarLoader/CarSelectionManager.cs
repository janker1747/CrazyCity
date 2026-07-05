using System.Collections.Generic;
using UnityEngine;

public class CarSelectionManager : MonoBehaviour
{
    public static CarSelectionManager Instance;

    [SerializeField] private List<CarItemSO> _cars;

    private int _currentIndex;
    private GameData _gameData;

    private void Awake()
    {
        _gameData = GameData.Instance;
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveCurrentCar();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NextCar()
    {
        _currentIndex++;

        if (_currentIndex >= _cars.Count)
            _currentIndex = 0;

        SaveCurrentCar();
    }

    public void PreviousCar()
    {
        _currentIndex--;

        if (_currentIndex < 0)
            _currentIndex = _cars.Count - 1;

        SaveCurrentCar();
    }

    public CarItemSO GetCurrentCar()
    {
        return _cars[_currentIndex];
    }

    public Player GetPlayerPrefab()
    {
        SaveCurrentCar();
        return _cars[_currentIndex].PlayerPrefab;
    }

    private void SaveCurrentCar()
    {
        if (_cars == null || _cars.Count == 0)
            return;

        _gameData.SetCar(_cars[_currentIndex]);
    }
}
