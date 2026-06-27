using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PaddleCorruptionVisual : MonoBehaviour
{
    [Serializable]
    private sealed class CorruptionStage
    {
        [Tooltip("Applies this stage when current health ratio is less than or equal to this value. Lower thresholds represent stronger damage states.")]
        [Range(0f, 1f)] public float healthRatioThreshold;
        public Sprite sprite;
    }

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
    [SerializeField] private List<CorruptionStage> stages = new List<CorruptionStage>();

    private DecorativeFlickerSprite overlayFlicker;
    private Sprite fallbackOverlaySprite;
    private float currentCorruption;
    private float currentHealthRatio = 1f;
    private float visibilityAlphaMultiplier = 1f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private float lastLoggedHealthRatio = float.NaN;
    private float lastLoggedThreshold = float.NaN;
    private Sprite lastLoggedSprite;
    private bool lastLoggedRendererEnabled;
    private bool hasLoggedState;
#endif

    private void Awake()
    {
        if (baseRenderer == null)
            baseRenderer = GetComponent<SpriteRenderer>();

        if (corruptionOverlayRenderer != null)
        {
            fallbackOverlaySprite = corruptionOverlayRenderer.sprite;
            overlayFlicker = corruptionOverlayRenderer.GetComponent<DecorativeFlickerSprite>();
        }

        ApplyCorruption(0f);
    }

    public void ApplyHealthRatio(float healthRatio)
    {
        currentHealthRatio = Mathf.Clamp01(healthRatio);
        ApplyCorruption(CalculateCorruption(currentHealthRatio));
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
        float startHealthRatio = GetEffectiveCorruptionStartHealthRatio();

        if (startHealthRatio <= 0f)
            return healthRatio <= 0f ? 1f : 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(startHealthRatio, 0f, healthRatio));
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

        bool hasStageMode = HasValidStages();
        bool hasSelectedStage = TryGetStageForHealthRatio(currentHealthRatio, out CorruptionStage selectedStage);
        float alpha = Mathf.Lerp(overlayMinAlpha, overlayMaxAlpha, currentCorruption);
        Color color = overlayColor;
        color.a = Mathf.Clamp01(alpha * visibilityAlphaMultiplier);

        Sprite overlaySprite = hasSelectedStage ? selectedStage.sprite : fallbackOverlaySprite;

        if (overlaySprite != null)
            corruptionOverlayRenderer.sprite = overlaySprite;

        corruptionOverlayRenderer.color = color;
        corruptionOverlayRenderer.enabled = hasStageMode
            ? hasSelectedStage && corruptionOverlayRenderer.sprite != null
            : color.a > 0.001f && corruptionOverlayRenderer.sprite != null;
        overlayFlicker?.SetBaseColor(color);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogOverlayStateIfChanged(currentHealthRatio, selectedStage, color.a);
#endif
    }

    private float GetEffectiveCorruptionStartHealthRatio()
    {
        float startHealthRatio = corruptionStartHealthRatio;

        if (stages == null)
            return Mathf.Clamp01(startHealthRatio);

        for (int i = 0; i < stages.Count; i++)
        {
            CorruptionStage stage = stages[i];

            if (stage == null)
                continue;

            startHealthRatio = Mathf.Max(startHealthRatio, stage.healthRatioThreshold);
        }

        return Mathf.Clamp01(startHealthRatio);
    }

    private bool HasValidStages()
    {
        if (stages == null)
            return false;

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i]?.sprite != null)
                return true;
        }

        return false;
    }

    private bool TryGetStageForHealthRatio(float healthRatio, out CorruptionStage selectedStage)
    {
        selectedStage = null;

        if (stages == null || stages.Count == 0)
            return false;

        float selectedThreshold = float.PositiveInfinity;

        for (int i = 0; i < stages.Count; i++)
        {
            CorruptionStage stage = stages[i];

            if (stage == null)
                continue;

            if (stage.sprite == null)
                continue;

            float threshold = Mathf.Clamp01(stage.healthRatioThreshold);

            if (healthRatio > threshold || threshold > selectedThreshold)
                continue;

            selectedStage = stage;
            selectedThreshold = threshold;
        }

        return selectedStage != null;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void LogOverlayStateIfChanged(
        float healthRatio,
        CorruptionStage selectedStage,
        float alpha)
    {
        float selectedThreshold = selectedStage != null
            ? Mathf.Clamp01(selectedStage.healthRatioThreshold)
            : -1f;
        Sprite selectedSprite = selectedStage != null ? selectedStage.sprite : null;
        bool rendererEnabled = corruptionOverlayRenderer.enabled;

        if (hasLoggedState &&
            Mathf.Approximately(lastLoggedHealthRatio, healthRatio) &&
            Mathf.Approximately(lastLoggedThreshold, selectedThreshold) &&
            lastLoggedSprite == selectedSprite &&
            lastLoggedRendererEnabled == rendererEnabled)
        {
            return;
        }

        hasLoggedState = true;
        lastLoggedHealthRatio = healthRatio;
        lastLoggedThreshold = selectedThreshold;
        lastLoggedSprite = selectedSprite;
        lastLoggedRendererEnabled = rendererEnabled;

        string selectedSpriteName = selectedSprite != null ? selectedSprite.name : "None";
        string rendererSpriteName = corruptionOverlayRenderer.sprite != null
            ? corruptionOverlayRenderer.sprite.name
            : "None";

        Debug.Log(
            $"[PaddleCorruptionVisual] hpRatio={healthRatio:0.###}, stages={(stages != null ? stages.Count : 0)}, " +
            $"selectedThreshold={selectedThreshold:0.###}, sprite={selectedSpriteName}, alpha={alpha:0.###}, " +
            $"rendererEnabled={rendererEnabled}, rendererSprite={rendererSpriteName}, " +
            $"activeSelf={corruptionOverlayRenderer.gameObject.activeSelf}",
            this);
    }
#endif

    private void OnValidate()
    {
        corruptionStartHealthRatio = Mathf.Clamp01(corruptionStartHealthRatio);
        overlayMinAlpha = Mathf.Clamp01(overlayMinAlpha);
        overlayMaxAlpha = Mathf.Clamp01(overlayMaxAlpha);

        if (overlayMaxAlpha < overlayMinAlpha)
            overlayMaxAlpha = overlayMinAlpha;

        if (stages == null)
            return;

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i] == null)
                continue;

            stages[i].healthRatioThreshold = Mathf.Clamp01(stages[i].healthRatioThreshold);
        }
    }
}
