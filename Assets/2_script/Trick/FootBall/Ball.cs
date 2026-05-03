using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private float _lifeTime;

    WaitForSeconds _sleepTime;

    public event Action ReturnMe;
    private void Awake()
    {
        _sleepTime = new WaitForSeconds(_lifeTime);
    }

    private void OnEnable()
    {
        StartCoroutine(lifeRoutine());   
    }

    private IEnumerator lifeRoutine()
    {
        yield return _sleepTime;
        ReturnMe?.Invoke();
        ReturnMe = null; 
    }
}
