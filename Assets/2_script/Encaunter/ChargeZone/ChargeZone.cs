using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Charges while the player stays inside its trigger, then opens the standard
/// MiniGame reward panel. The next charge takes longer after every success.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class ChargeZone : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer chargeSprite;
    [SerializeField, Min(0.01f)] private float dischargeDuration = 0.35f;

    [Header("Charge")]
    [SerializeField, Min(0.1f)] private float initialChargeDuration = 3f;
    [SerializeField, Min(0f)] private float chargeDurationIncrease = 1f;
    [SerializeField, Min(0.1f)] private float cooldownDuration = 40f;
    [SerializeField, Min(0f)] private float cooldownDurationIncrease = 5f;

    [Header("Cooldown UI")]
    [Tooltip("Four Filled Images that show cooldown recovery from 0 to 1.")]
    [SerializeField] private Image[] cooldownFillImages = new Image[4];

    [Header("Events")]
    [SerializeField] private UnityEvent onChargeStarted;
    [SerializeField] private UnityEvent onChargeCompleted;
    [SerializeField] private UnityEvent onCooldownFinished;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    private Vector3 spriteInitialScale;
    private float chargeElapsed;
    private float visualProgress;
    private float cooldownRemaining;
    private float cooldownFillAmount;
    private int successfulCharges;
    private Player chargingPlayer;
    private bool isCharging;
    private bool isOnCooldown;

    public bool IsCharging => isCharging;
    public bool IsOnCooldown => isOnCooldown;
    public float ChargeProgress => Mathf.Clamp01(chargeElapsed / CurrentChargeDuration);
    public float CooldownRemaining => cooldownRemaining;
    public float CooldownProgress => cooldownFillAmount;
    public int SuccessfulCharges => successfulCharges;
    public float CurrentChargeDuration => initialChargeDuration +
                                          successfulCharges * chargeDurationIncrease;
    public float CurrentCooldownDuration => cooldownDuration +
                                             successfulCharges * cooldownDurationIncrease;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        if (chargeSprite == null)
        {
            Debug.LogError($"{nameof(ChargeZone)} on '{name}' requires a charge sprite.", this);
            enabled = false;
            return;
        }

        spriteInitialScale = chargeSprite.transform.localScale;
        chargeSprite.transform.localScale = Vector3.zero;
        UpdateCooldownFillImages();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        if (isOnCooldown)
        {
            cooldownRemaining -= deltaTime;
            cooldownFillAmount = 1f - Mathf.Clamp01(cooldownRemaining / CurrentCooldownDuration);
            SetVisualProgress(0f, deltaTime);
            UpdateCooldownFillImages();

            if (cooldownRemaining <= 0f)
            {
                cooldownRemaining = 0f;
                cooldownFillAmount = 1f;
                isOnCooldown = false;
                UpdateCooldownFillImages();
                onCooldownFinished?.Invoke();
            }

            return;
        }

        if (playerColliders.Count == 0)
        {
            CancelCharge(deltaTime);
            return;
        }

        if (!isCharging)
        {
            isCharging = true;
            onChargeStarted?.Invoke();
        }

        chargeElapsed += deltaTime;
        SetVisualProgress(ChargeProgress, deltaTime);

        if (chargeElapsed >= CurrentChargeDuration)
            CompleteCharge();
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        playerColliders.Add(other);
        chargingPlayer = player;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerColliders.Remove(other) || playerColliders.Count > 0)
            return;

        chargingPlayer = null;
    }

    private void OnDisable()
    {
        playerColliders.Clear();
        chargingPlayer = null;
        isCharging = false;
        chargeElapsed = 0f;
        visualProgress = 0f;

        if (chargeSprite != null)
            chargeSprite.transform.localScale = Vector3.zero;
    }

    private void CancelCharge(float deltaTime)
    {
        if (isCharging || chargeElapsed > 0f)
        {
            isCharging = false;
            chargeElapsed = 0f;
        }

        SetVisualProgress(0f, deltaTime);
    }

    private void CompleteCharge()
    {
        Player rewardedPlayer = chargingPlayer;

        chargeElapsed = CurrentChargeDuration;
        isCharging = false;
        isOnCooldown = true;
        cooldownFillAmount = 0f;
        successfulCharges++;
        cooldownRemaining = CurrentCooldownDuration;
        UpdateCooldownFillImages();
        onChargeCompleted?.Invoke();

        if (rewardedPlayer != null)
            ShowRewardPanel(rewardedPlayer);
    }

    private async void ShowRewardPanel(Player player)
    {
        try
        {
            await MiniGameLauncher.ShowRewardAsync(player);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void SetVisualProgress(float targetProgress, float deltaTime)
    {
        float speed = targetProgress > visualProgress
            ? 1f / Mathf.Max(CurrentChargeDuration, 0.01f)
            : 1f / dischargeDuration;

        visualProgress = Mathf.MoveTowards(
            visualProgress,
            Mathf.Clamp01(targetProgress),
            speed * deltaTime);
        chargeSprite.transform.localScale = spriteInitialScale * visualProgress;
    }

    private void UpdateCooldownFillImages()
    {
        if (cooldownFillImages == null)
            return;

        float fillAmount = CooldownProgress;
        foreach (Image cooldownFillImage in cooldownFillImages)
        {
            if (cooldownFillImage != null)
                cooldownFillImage.fillAmount = fillAmount;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        initialChargeDuration = Mathf.Max(0.1f, initialChargeDuration);
        chargeDurationIncrease = Mathf.Max(0f, chargeDurationIncrease);
        cooldownDuration = Mathf.Max(0.1f, cooldownDuration);
        cooldownDurationIncrease = Mathf.Max(0f, cooldownDurationIncrease);
        dischargeDuration = Mathf.Max(0.01f, dischargeDuration);

        if (cooldownFillImages == null || cooldownFillImages.Length != 4)
            Array.Resize(ref cooldownFillImages, 4);

        Collider trigger = GetComponent<Collider>();
        if (trigger != null && !trigger.isTrigger)
        {
            Debug.LogWarning(
                $"{nameof(ChargeZone)} on '{name}' requires Collider.IsTrigger to be enabled.",
                this);
        }
    }
#endif
}
