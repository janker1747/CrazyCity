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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _gameData = GameData.Instance;
        SaveCurrentCar();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NextCar()
    {
        if (!HasCars())
            return;

        _currentIndex++;

        if (_currentIndex >= _cars.Count)
            _currentIndex = 0;

        SaveCurrentCar();
    }

    public void PreviousCar()
    {
        if (!HasCars())
            return;

        _currentIndex--;

        if (_currentIndex < 0)
            _currentIndex = _cars.Count - 1;

        SaveCurrentCar();
    }

    public CarItemSO GetCurrentCar()
    {
        if (!HasCars())
            return null;

        return _cars[_currentIndex];
    }

    public Player GetPlayerPrefab()
    {
        SaveCurrentCar();
        CarItemSO currentCar = GetCurrentCar();
        return currentCar != null ? currentCar.PlayerPrefab : null;
    }

    public void ConfirmSelection()
    {
        SaveCurrentCar();
    }

    private void SaveCurrentCar()
    {
        if (!HasCars())
            return;

        _gameData.SetCar(_cars[_currentIndex]);
    }

    private bool HasCars()
    {
        return _cars != null && _cars.Count > 0;
    }
}
