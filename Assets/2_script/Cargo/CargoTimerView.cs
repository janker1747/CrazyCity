using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CargoTimerView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fill;

    [Header("Danger")]
    [SerializeField, Range(0f, 1f)]
    private float dangerThreshold = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Animation")]
    [SerializeField] private float colorTweenDuration = 0.15f;

    [Header("Fuel Effect")]
    [SerializeField] private bool animateFuelEffect = true;

    [SerializeField] private float fuelBrightness = 0.15f;
    [SerializeField] private float fuelSpeed = 2f;

    private bool isDanger;

    private Tween colorTween;

    private Material runtimeMaterial;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    public void SetTime(float remaining, float total)
    {
        float normalized =
            total <= 0f
                ? 0f
                : Mathf.Clamp01(remaining / total);

        UpdateFill(normalized);
        UpdateEffects(normalized);
    }

    private void Awake()
    {
        SetupMaterial();
    }

    private void Update()
    {
        UpdateFuelEffect();
    }

    private void SetupMaterial()
    {
        if (fill == null)
            return;

        if (fill.material != null)
            runtimeMaterial = Instantiate(fill.material);
        else
            runtimeMaterial = new Material(Shader.Find("UI/Default"));

        fill.material = runtimeMaterial;
    }

    private void UpdateFill(float normalized)
    {
        if (fill == null)
            return;

        fill.fillAmount = normalized;
    }

    private void UpdateEffects(float normalized)
    {
        bool shouldBeDanger = normalized <= dangerThreshold;

        if (isDanger == shouldBeDanger)
            return;

        isDanger = shouldBeDanger;

        UpdateColor();
    }

    private void UpdateColor()
    {
        if (fill == null)
            return;

        Color targetColor =
            isDanger
                ? dangerColor
                : normalColor;

        colorTween?.Kill();

        colorTween = fill
            .DOColor(targetColor, colorTweenDuration)
            .SetEase(Ease.OutQuad);
    }

    private void UpdateFuelEffect()
    {
        if (!animateFuelEffect)
            return;

        if (fill == null)
            return;

        if (runtimeMaterial == null)
            return;

        if (isDanger)
            return;

        float wave =
            Mathf.Sin(Time.unscaledTime * fuelSpeed) * fuelBrightness;

        Color animatedColor = normalColor;

        animatedColor.r += wave;
        animatedColor.g += wave;
        animatedColor.b += wave;

        animatedColor.r = Mathf.Clamp01(animatedColor.r);
        animatedColor.g = Mathf.Clamp01(animatedColor.g);
        animatedColor.b = Mathf.Clamp01(animatedColor.b);

        runtimeMaterial.SetColor(ColorProperty, animatedColor);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ResetView()
    {
        colorTween?.Kill();

        isDanger = false;

        if (fill != null)
        {
            fill.fillAmount = 0f;
            fill.color = normalColor;
        }

        if (runtimeMaterial != null)
            runtimeMaterial.SetColor(ColorProperty, normalColor);
    }

    private void OnDestroy()
    {
        colorTween?.Kill();

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}