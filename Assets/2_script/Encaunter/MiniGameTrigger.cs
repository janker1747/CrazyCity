using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class MiniGameTrigger : MonoBehaviour
{
    public event Action<MiniGameTrigger, bool> Resolved;

    [Header("Mini-game")]
    [SerializeField] private MiniGameId miniGame = MiniGameId.TowerBalance;
    [SerializeField] private bool oneShot = true;

    [Header("Result events")]
    [SerializeField] private UnityEvent onCompleted;
    [SerializeField] private UnityEvent onFailed;

    private bool isRunning;
    private bool hasTriggered;

    public bool IsRunning => isRunning;
    public bool HasTriggered => hasTriggered;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player != null)
            Play(player);
    }

    /// <summary>
    /// Can also be called directly when the mini-game is started by a button,
    /// dialogue, encounter or another system instead of a physical trigger.
    /// </summary>
    public async void Play(Player player)
    {
        if (player == null || isRunning || MiniGameLauncher.IsRunning)
            return;

        if (oneShot && hasTriggered)
            return;

        isRunning = true;
        hasTriggered = true;
        bool hasResult = false;
        bool succeeded = false;

        try
        {
            MiniGameResult result = await MiniGameLauncher.PlayAsync(miniGame, player);

            if (this == null)
                return;

            succeeded = result.IsCompleted;
            if (succeeded)
                onCompleted?.Invoke();
            else
                onFailed?.Invoke();

            hasResult = true;
        }
        catch (Exception exception)
        {
            // Allow another attempt if the scene could not be started at all.
            hasTriggered = false;
            Debug.LogException(exception, this);
        }
        finally
        {
            if (this != null)
            {
                isRunning = false;

                if (hasResult)
                    Resolved?.Invoke(this, succeeded);
            }
        }
    }

    /// <summary>Allows a one-shot trigger to be used again.</summary>
    public void ResetTrigger()
    {
        if (!isRunning)
            hasTriggered = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null && !trigger.isTrigger)
        {
            Debug.LogWarning(
                $"{nameof(MiniGameTrigger)} on '{name}' requires Collider.IsTrigger to be enabled.",
                this);
        }
    }
#endif
}
