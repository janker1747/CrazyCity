using System.Collections;
using System.Collections.Generic;
using _2_script.Enemy_;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class TimeStopManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner _spawner;
    
    [Header("Visual Effects")]
    [SerializeField] private float _maxThreshold = 0.5f;
    [SerializeField] private string _voronoiParameterKey = "_Voroni_Parameter";
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private List<Rigidbody> _DebugCars;

    [Header("References")]
    [SerializeField] private List<Rigidbody> _rigidbodies = new List<Rigidbody>();

    private Dictionary<Rigidbody, MeshRenderer> _renderers = new Dictionary<Rigidbody, MeshRenderer>();
    private Dictionary<Rigidbody, Coroutine> _coroutines = new Dictionary<Rigidbody, Coroutine>();

    private void OnEnable()
    {
        _spawner.OnSpawn += Register;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.F))
            Freeze();

        if (Input.GetKeyUp(KeyCode.G))
            Unfreeze();

        if (Input.GetKey(KeyCode.H))
        {
            foreach (var r in _renderers.Values)
            {
                r.material.SetFloat(_voronoiParameterKey, _maxThreshold);
            }
        }
    }

    public void Register(Enemy enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        
        _rigidbodies.Add(rb);

        Transform[] children = rb.GetComponentsInChildren<Transform>();

        foreach (var child in children)
        {
            if (child.CompareTag("PoliceMesh"))
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();

                if (renderer != null)
                {
                    _renderers[rb] = renderer;
                }

                break; 
            }
        }
    }

    public void Freeze()
    {
        foreach (var rb in _rigidbodies)
        {
            if (rb == null) continue;

            rb.isKinematic = true;
            StartFade(rb, 0f, _maxThreshold);
        }
    }

    public void Unfreeze()
    {
        foreach (var rb in _rigidbodies)
        {
            if (rb == null) continue;

            rb.isKinematic = false;
            StartFade(rb, _maxThreshold, 0f);
        }
    }

    private void StartFade(Rigidbody rb, float from, float to)
    {
        if (!_renderers.TryGetValue(rb, out var renderer)) return;

        if (_coroutines.TryGetValue(rb, out var coroutine))
            StopCoroutine(coroutine);

        _coroutines[rb] = StartCoroutine(Fade(renderer, from, to));
    }

    private IEnumerator Fade(MeshRenderer renderer, float from, float to)
    {
        float time = 0;

        while (time < _fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / _fadeDuration;

            float value = Mathf.Lerp(from, to, t);
            renderer.sharedMaterial.SetFloat(_voronoiParameterKey, value);

            yield return null;
        }

        renderer.sharedMaterial.SetFloat(_voronoiParameterKey, to);
    }

    private void OnDisable()
    {
        foreach (var rb in _rigidbodies)
        {
            if (rb == null) continue;

            StartFade(rb, _maxThreshold, 0f);
        }
        
        _spawner.OnSpawn -= Register;
    }
    
    private void OnDestroy()
    {
        _rigidbodies.Clear();
    }
}