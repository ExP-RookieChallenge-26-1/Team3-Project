using UnityEngine;

public class GlitchOverlayVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glitchRenderer;
    [SerializeField] private PulseVisual pulseVisual;

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

    private void Awake()
    {
        if (glitchRenderer == null)
            glitchRenderer = GetComponent<SpriteRenderer>();

        if (pulseVisual == null)
            pulseVisual = GetComponent<PulseVisual>();

        ResetVisual();
    }

    public void SetState(bool visible, bool connected, float danger01)
    {
        SetState(visible, connected, danger01, 1);
    }

    public void SetState(bool visible, bool connected, float danger01, int glitchStage)
    {
        if (glitchRenderer == null)
            return;

        if (!visible)
        {
            glitchRenderer.enabled = false;
            if (pulseVisual != null)
                pulseVisual.SetPulsing(false);
            return;
        }

        glitchRenderer.enabled = true;
        glitchRenderer.sprite = GetSpriteByStage(glitchStage);

        float alpha = connected ? connectedAlpha : disconnectedAlpha;
        SetAlpha(alpha);

        if (pulseVisual != null)
        {
            pulseVisual.SetBaseAlpha(alpha);
            pulseVisual.SetPulsing(connected);
        }
    }

    public void ResetVisual()
    {
        if (pulseVisual != null)
            pulseVisual.SetPulsing(false);

        if (glitchRenderer == null)
            return;

        glitchRenderer.enabled = false;
        glitchRenderer.sprite = glitchStage1;
        SetAlpha(disconnectedAlpha);

        if (pulseVisual != null)
            pulseVisual.SetBaseAlpha(disconnectedAlpha);
    }

    private Sprite GetSpriteByStage(int glitchStage)
    {
        switch (glitchStage)
        {
            case 3:
                if (glitchStage3 != null)
                    return glitchStage3;
                if (glitchStage2 != null)
                    return glitchStage2;
                return glitchStage1;
            case 2:
                if (glitchStage2 != null)
                    return glitchStage2;
                return glitchStage1;
            default:
                return glitchStage1;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (glitchRenderer == null)
            return;

        Color color = glitchRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        glitchRenderer.color = color;
    }
}
