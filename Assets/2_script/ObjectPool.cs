using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private T _prefab;
    [SerializeField] private int _poolSize;

    private new List<T> _pool;

    protected virtual void Awake()
    {
        _pool = new List<T>();

        for (int i = 0; i < _poolSize; i++)
        {
            T obj = Instantiate(_prefab);
            obj.gameObject.SetActive(false);
            _pool.Add(obj);
        }
    }

    public T GetObject(Transform spawnPoint)
    {
        foreach (T obj in _pool)
        {
            obj.gameObject.SetActive(true);
            return obj;
        }

        T newObj = Instantiate(_prefab, spawnPoint);
        _pool.Add(newObj);
        return newObj;
    }

    public T GetObject()
    {
        foreach (T obj in _pool)
        {
            obj.gameObject.SetActive(true);
            return obj;
        }

        T newObj = Instantiate(_prefab);
        _pool.Add(newObj);
        return newObj;
    }

    public void ReturnObject(T obj)
    {
        obj.gameObject.SetActive(false);
    }
}