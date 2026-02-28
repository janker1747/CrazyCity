using System;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPool<Ball> _ballPool;
    [SerializeField] private Transform _spawnPoint; 
    [SerializeField] private float _propTorqueForce = 50f;
    [SerializeField] private float _launchAngle = 45f;
    [SerializeField] private float _launchSpeed = 40f;

    public void Launch()
    {
        if (_ballPool == null || _spawnPoint == null)
            return;

        Ball ball = _ballPool.GetObject(); 

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        ball.transform.position = _spawnPoint.position;
        ball.transform.rotation = _spawnPoint.rotation;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 launchDirection = Quaternion.Euler(-_launchAngle, 0f, 0f) * _spawnPoint.forward;

        rb.AddForce(launchDirection * _launchSpeed, ForceMode.Impulse);
        rb.AddTorque(_spawnPoint.right * _propTorqueForce, ForceMode.Impulse);

        Action returnAction = null;
        returnAction = () =>
        {
            _ballPool.ReturnObject(ball);
            ball.ReturnMe -= returnAction;
        };
        ball.ReturnMe += returnAction;
    }
}