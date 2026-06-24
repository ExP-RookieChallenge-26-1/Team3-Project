using UnityEngine;

public class CeilingCore : MonoBehaviour
{
    [SerializeField] private SpriteRenderer coreRenderer;
    [SerializeField] private PulseVisual pulseVisual;
    [SerializeField] private DamageFlashVisual damageFlashVisual;
    [SerializeField] private SegmentGlowShadowVisual glowShadowVisual;

    [SerializeField] private float connectedAlpha = 1f;
    [SerializeField] private float disconnectedAlpha = 0.25f;

    private int segmentIndex = -1;
    private bool isAlive = true;
    private bool isConnected;
    private bool isVisible = true;

    public int SegmentIndex => segmentIndex;

    private void Awake()
    {
        if (coreRenderer == null)
            coreRenderer = GetComponent<SpriteRenderer>();

        if (pulseVisual == null)
            pulseVisual = GetComponent<PulseVisual>();

        if (damageFlashVisual == null)
            damageFlashVisual = GetComponent<DamageFlashVisual>();

        EnsureGlowShadowVisual();
    }

    public void Initialize(int segmentIndex)
    {
        this.segmentIndex = segmentIndex;
        isAlive = true;
        isConnected = false;
        isVisible = true;
        EnsureGlowShadowVisual();
        ResetVisual();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyState();
    }

    public void ApplyVisualProfile(CeilingSegmentVisualProfile profile)
    {
        if (profile == null)
            return;

        EnsureGlowShadowVisual();
        glowShadowVisual?.ApplyVisualProfile(profile);
        ApplyState();
    }

    public void SetCoreSprite(Sprite sprite)
    {
        if (coreRenderer != null && sprite != null)
            coreRenderer.sprite = sprite;
    }

    public void SetConnectedState(bool connected)
    {
        isConnected = connected;
        ApplyState();
    }

    public void SetPulseUseUnscaledTime(bool enabled)
    {
        pulseVisual?.SetUseUnscaledTime(enabled);
    }

    public void SetAliveState(bool alive)
    {
        isAlive = alive;

        if (!isAlive)
            isConnected = false;

        ApplyState();
    }

    public void PlayDamageFlash()
    {
        if (!isAlive)
            return;

        damageFlashVisual?.PlayFlash();
    }

    public void ResetVisual()
    {
        damageFlashVisual?.ResetVisual();
        ApplyState();
    }

    private void ApplyState()
    {
        bool activePulse = isVisible && isAlive && isConnected;
        float alpha = activePulse ? connectedAlpha : disconnectedAlpha;

        if (coreRenderer != null)
            coreRenderer.enabled = isVisible && isAlive;

        SetAlpha(alpha);
        glowShadowVisual?.SetState(isVisible && isAlive, isConnected);

        if (pulseVisual != null)
        {
            pulseVisual.SetBaseAlpha(alpha);
            pulseVisual.SetPulsing(activePulse);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (coreRenderer == null)
            return;

        Color color = coreRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        coreRenderer.color = color;
    }

    private void EnsureGlowShadowVisual()
    {
        if (glowShadowVisual == null)
            glowShadowVisual = GetComponent<SegmentGlowShadowVisual>();

        glowShadowVisual?.Initialize(coreRenderer);
    }
}
