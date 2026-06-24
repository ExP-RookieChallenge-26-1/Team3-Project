using UnityEngine;

public class GlitchOverlayVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glitchRenderer;
    [SerializeField] private PulseVisual pulseVisual;
    [SerializeField] private SegmentGlowShadowVisual glowShadowVisual;

    [Header("Stage Sprites")]
    [SerializeField] private Sprite[] stage1Sprites;
    [SerializeField] private Sprite[] stage2Sprites;
    [SerializeField] private Sprite[] stage3Sprites;

    [Header("Legacy Stage Sprite Fallbacks")]
    [SerializeField] private Sprite glitchStage1;
    [SerializeField] private Sprite glitchStage2;
    [SerializeField] private Sprite glitchStage3;

    [SerializeField] private float connectedAlpha = 1f;
    [SerializeField] private float disconnectedAlpha = 0.25f;

    // Legacy danger thresholds are kept for prefab compatibility, but sprite stage is now selected by row distance.
#pragma warning disable 0414
    [SerializeField] private float stage2Threshold = 0.34f;
    [SerializeField] private float stage3Threshold = 0.67f;
#pragma warning restore 0414

    private int currentStage = -1;
    private Sprite currentSelectedSprite;

    private void Awake()
    {
        if (glitchRenderer == null)
            glitchRenderer = GetComponent<SpriteRenderer>();

        if (pulseVisual == null)
            pulseVisual = GetComponent<PulseVisual>();

        EnsureGlowShadowVisual();

        ResetVisual();
    }

    public void SetState(bool visible, bool connected, float danger01)
    {
        SetState(visible, connected, danger01, 1);
    }

    public void SetState(bool visible, bool connected, float danger01, int glitchStage)
    {
        SetState(visible, connected, danger01, glitchStage, null);
    }

    public void SetState(
        bool visible,
        bool connected,
        float danger01,
        int glitchStage,
        CeilingSegmentVisualProfile profile
    )
    {
        if (glitchRenderer == null)
            return;

        if (!visible || glitchStage <= 0 || glitchStage > 3)
        {
            ResetSelectedSprite();
            glitchRenderer.enabled = false;
            glowShadowVisual?.SetState(false, false);
            if (pulseVisual != null)
                pulseVisual.SetPulsing(false);
            return;
        }

        if (glitchStage != currentStage || currentSelectedSprite == null)
        {
            currentStage = glitchStage;
            currentSelectedSprite = PickRandomSpriteForStage(glitchStage);
        }

        bool hasSprite = currentSelectedSprite != null;
        glitchRenderer.enabled = hasSprite;
        glitchRenderer.sprite = currentSelectedSprite;

        if (!hasSprite)
        {
            glowShadowVisual?.SetState(false, false);
            pulseVisual?.SetPulsing(false);
            return;
        }

        float alpha = connected ? connectedAlpha : disconnectedAlpha;
        SetAlpha(alpha);

        EnsureGlowShadowVisual();
        glowShadowVisual?.ApplyVisualProfile(profile);
        glowShadowVisual?.SetState(true, connected);

        if (pulseVisual != null)
        {
            pulseVisual.SetBaseAlpha(alpha);
            pulseVisual.SetPulsing(connected);
        }
    }

    public void ResetVisual()
    {
        ResetSelectedSprite();

        if (pulseVisual != null)
            pulseVisual.SetPulsing(false);

        if (glitchRenderer == null)
            return;

        glitchRenderer.enabled = false;
        glitchRenderer.sprite = null;
        SetAlpha(disconnectedAlpha);

        if (pulseVisual != null)
            pulseVisual.SetBaseAlpha(disconnectedAlpha);

        glowShadowVisual?.SetState(false, false);
    }

    private Sprite PickRandomSpriteForStage(int glitchStage)
    {
        switch (glitchStage)
        {
            case 3:
                return PickRandomFromArrayOrFallback(stage3Sprites, glitchStage3)
                    ?? PickRandomFromArrayOrFallback(stage2Sprites, glitchStage2)
                    ?? PickRandomFromArrayOrFallback(stage1Sprites, glitchStage1);
            case 2:
                return PickRandomFromArrayOrFallback(stage2Sprites, glitchStage2)
                    ?? PickRandomFromArrayOrFallback(stage1Sprites, glitchStage1);
            case 1:
                return PickRandomFromArrayOrFallback(stage1Sprites, glitchStage1);
            default:
                return null;
        }
    }

    private Sprite PickRandomFromArrayOrFallback(Sprite[] sprites, Sprite fallback)
    {
        int validCount = 0;

        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    validCount++;
            }
        }

        if (validCount == 0)
            return fallback;

        int selectedIndex = Random.Range(0, validCount);

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null)
                continue;

            if (selectedIndex == 0)
                return sprites[i];

            selectedIndex--;
        }

        return fallback;
    }

    private void ResetSelectedSprite()
    {
        currentStage = -1;
        currentSelectedSprite = null;
    }

    private void SetAlpha(float alpha)
    {
        if (glitchRenderer == null)
            return;

        Color color = glitchRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        glitchRenderer.color = color;
    }

    private void EnsureGlowShadowVisual()
    {
        if (glowShadowVisual == null)
            glowShadowVisual = GetComponent<SegmentGlowShadowVisual>();

        glowShadowVisual?.Initialize(glitchRenderer);
    }
}
