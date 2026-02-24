using System;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPool<Ball> _ballPool;
    [SerializeField] private Transform _spawnPoint; // точка вылета
    [SerializeField] private float _propTorqueForce = 50f;
    [SerializeField] private float _launchAngle = 45f;
    [SerializeField] private float _launchSpeed = 40f;

    public void Launch()
    {
        if (_ballPool == null || _spawnPoint == null)
            return;

        Ball ball = _ballPool.GetObject(); // берём шар из пула

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        // ставим шар в точку спавна
        ball.transform.position = _spawnPoint.position;
        ball.transform.rotation = _spawnPoint.rotation;

        // сброс движения
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // вычисляем направление вылета
        Vector3 launchDirection = Quaternion.Euler(-_launchAngle, 0f, 0f) * _spawnPoint.forward;

        // применяем силу и крутящий момент
        rb.AddForce(launchDirection * _launchSpeed, ForceMode.Impulse);
        rb.AddTorque(_spawnPoint.right * _propTorqueForce, ForceMode.Impulse);

        // безопасная подписка на возврат в пул
        Action returnAction = null;
        returnAction = () =>
        {
            _ballPool.ReturnObject(ball);
            ball.ReturnMe -= returnAction;
        };
        ball.ReturnMe += returnAction;
    }
}