using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private T _prefab;
    [SerializeField] private int _poolSize;

    private List<T> _pool;

    protected virtual void Awake()
    {
        _pool = new List<T>();

        for (int i = 0; i < _poolSize; i++)
        {
            T obj = CreateObject();
            obj.gameObject.SetActive(false);
            _pool.Add(obj);
        }
    }

    public T GetObject(Transform spawnPoint)
    {
        if (spawnPoint == null)
            return null;

        return GetObject(spawnPoint.position, spawnPoint.rotation);
    }

    public T GetObject(Vector3 position, Quaternion rotation)
    {
        foreach (T obj in _pool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                obj.transform.SetPositionAndRotation(
                    position,
                    rotation);

                obj.gameObject.SetActive(true);

                return obj;
            }
        }

        T newObj = CreateObject(position, rotation);

        _pool.Add(newObj);

        return newObj;
    }

    public void ReturnObject(T obj)
    {
        obj.gameObject.SetActive(false);
    }

    protected virtual T CreateObject()
    {
        return Instantiate(_prefab);
    }

    protected virtual T CreateObject(Vector3 position, Quaternion rotation)
    {
        return Instantiate(_prefab, position, rotation);
    }
}
