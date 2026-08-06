using BoolAction = System.Action<bool>;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class TapAnimationStepper : MonoBehaviour, IPointerDownHandler, IMiniGameController
{
    public event BoolAction Finished;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("Клип постепенного выполнения")]
    [SerializeField] private AnimationClip progressClip;

    [Tooltip("Полный путь до состояния в Animator")]
    [SerializeField] private string progressStateName =
        "Base Layer.BoxCompleteAnima";

    [SerializeField] private string idleStateName =
        "Base Layer.Idle";

    [SerializeField] private int animatorLayer;

    [Header("Кадры остановки")]
    [Tooltip("При Samples = 60 значения 10, 20, 30 означают 0:10, 0:20, 0:30")]
    [SerializeField] private int[] stopFrames =
    {
        10,
        20,
        30,
        40,
        50,
        60
    };

    [Header("Переход между кадрами")]
    [Tooltip("0 — моментальный скачок")]
    [SerializeField, Min(0f)] private float jumpDuration = 0.07f;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onStepChanged;
    [SerializeField] private UnityEvent onCompleted;

    private int progressStateHash;
    private int idleStateHash;

    private int currentStep = -1;
    private float currentFrame;
    private Coroutine jumpCoroutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        progressStateHash = Animator.StringToHash(progressStateName);
        idleStateHash = Animator.StringToHash(idleStateName);
    }

    private void Start()
    {
        ResetProgress();
    }

    /// <summary>
    /// Срабатывает при нажатии на UI-объект.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        NextStep();
    }

    /// <summary>
    /// Перейти к следующему тайм-коду.
    /// Можно также вызывать из Button или другого скрипта.
    /// </summary>
    public void NextStep()
    {
        if (animator == null ||
            progressClip == null ||
            stopFrames == null ||
            stopFrames.Length == 0)
        {
            return;
        }

        if (currentStep >= stopFrames.Length - 1)
            return;

        currentStep++;

        float targetFrame = stopFrames[currentStep];

        if (jumpCoroutine != null)
            StopCoroutine(jumpCoroutine);

        jumpCoroutine = StartCoroutine(JumpToFrame(targetFrame));

        onStepChanged?.Invoke(currentStep);

        if (currentStep == stopFrames.Length - 1)
        {
            onCompleted?.Invoke();
            Finished?.Invoke(true);
        }
    }

    public void Begin()
    {
        ResetProgress();
    }

    private IEnumerator JumpToFrame(float targetFrame)
    {
        float startFrame = currentFrame;

        if (jumpDuration <= 0f)
        {
            currentFrame = targetFrame;
            SampleFrame(currentFrame);
            jumpCoroutine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < jumpDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / jumpDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentFrame = Mathf.Lerp(startFrame, targetFrame, t);
            SampleFrame(currentFrame);

            yield return null;
        }

        currentFrame = targetFrame;
        SampleFrame(currentFrame);

        jumpCoroutine = null;
    }

    private void SampleFrame(float frame)
    {
        float totalFrames = progressClip.length * progressClip.frameRate;

        if (totalFrames <= 0f)
            return;

        float normalizedTime = Mathf.Clamp01(frame / totalFrames);

        animator.speed = 0f;
        animator.Play(progressStateHash, animatorLayer, normalizedTime);
        animator.Update(0f);
    }

    public void ResetProgress()
    {
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }

        currentStep = -1;
        currentFrame = 0f;

        animator.speed = 1f;
        animator.Play(idleStateHash, animatorLayer, 0f);
        animator.Update(0f);
    }

    public void SetStep(int step)
    {
        if (stopFrames == null || stopFrames.Length == 0)
            return;

        currentStep = Mathf.Clamp(step, 0, stopFrames.Length - 1);
        currentFrame = stopFrames[currentStep];

        SampleFrame(currentFrame);
        onStepChanged?.Invoke(currentStep);
    }
}
