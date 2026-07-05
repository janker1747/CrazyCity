using System;
using UnityEngine;

namespace _2_script.Enemy_
{
    public class Enemy  : MonoBehaviour
    {
        [SerializeField] public AICarChase _policeAi;
        [SerializeField] private EnemyCollisionHandler _policeCollisionHandler;

        private void OnEnable()
        {
            _policeCollisionHandler.OnCollidedWithPlayer += _policeAi.HandleReverse;
        }

        private void OnDisable()
        {
            _policeCollisionHandler.OnCollidedWithPlayer -= _policeAi.HandleReverse;
        }
    }
}