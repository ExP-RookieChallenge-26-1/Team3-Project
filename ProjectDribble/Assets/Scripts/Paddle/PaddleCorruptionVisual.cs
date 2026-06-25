using UnityEngine;

public sealed class PaddleCorruptionVisual : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer baseRenderer;
    [SerializeField] private SpriteRenderer corruptionOverlayRenderer;

    [Header("Base Tint")]
    [SerializeField] private bool enableBaseTint;
    [SerializeField] private Color normalBaseColor = Color.white;
    [SerializeField] private Color corruptedBaseTint = new Color(0.4f, 1f, 0.4f, 1f);

    [Header("Overlay")]
    [SerializeField, Range(0f, 1f)] private float corruptionStartHealthRatio = 0.7f;
    [SerializeField, Range(0f, 1f)] private float overlayMinAlpha;
    [SerializeField, Range(0f, 1f)] private float overlayMaxAlpha = 0.7f;
    [SerializeField] private Color overlayColor = Color.green;

    private DecorativeFlickerSprite overlayFlicker;
    private float currentCorruption;
    private float visibilityAlphaMultiplier = 1f;

    private void Awake()
    {
        if (baseRenderer == null)
            baseRenderer = GetComponent<SpriteRenderer>();

        if (corruptionOverlayRenderer != null)
            overlayFlicker = corruptionOverlayRenderer.GetComponent<DecorativeFlickerSprite>();

        ApplyCorruption(0f);
    }

    public void ApplyHealthRatio(float healthRatio)
    {
        ApplyCorruption(CalculateCorruption(healthRatio));
    }

    public void SetVisibilityAlpha(float alpha)
    {
        visibilityAlphaMultiplier = Mathf.Clamp01(alpha);
        ApplyCorruption(currentCorruption);
    }

    public void ResetVisual()
    {
        visibilityAlphaMultiplier = 1f;
        ApplyCorruption(0f);
    }

    private float CalculateCorruption(float healthRatio)
    {
        healthRatio = Mathf.Clamp01(healthRatio);

        if (corruptionStartHealthRatio <= 0f)
            return healthRatio <= 0f ? 1f : 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(corruptionStartHealthRatio, 0f, healthRatio));
    }

    private void ApplyCorruption(float corruption)
    {
        currentCorruption = Mathf.Clamp01(corruption);
        ApplyBaseTint();
        ApplyOverlay();
    }

    private void ApplyBaseTint()
    {
        if (!enableBaseTint || baseRenderer == null)
            return;

        Color color = Color.Lerp(normalBaseColor, corruptedBaseTint, currentCorruption);
        color.a = baseRenderer.color.a;
        baseRenderer.color = color;
    }

    private void ApplyOverlay()
    {
        if (corruptionOverlayRenderer == null)
            return;

        float alpha = Mathf.Lerp(overlayMinAlpha, overlayMaxAlpha, currentCorruption);
        Color color = overlayColor;
        color.a = Mathf.Clamp01(alpha * visibilityAlphaMultiplier);

        corruptionOverlayRenderer.color = color;
        corruptionOverlayRenderer.enabled = color.a > 0.001f;
        overlayFlicker?.SetBaseColor(color);
    }

    private void OnValidate()
    {
        corruptionStartHealthRatio = Mathf.Clamp01(corruptionStartHealthRatio);
        overlayMinAlpha = Mathf.Clamp01(overlayMinAlpha);
        overlayMaxAlpha = Mathf.Clamp01(overlayMaxAlpha);

        if (overlayMaxAlpha < overlayMinAlpha)
            overlayMaxAlpha = overlayMinAlpha;
    }
}
