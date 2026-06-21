using UnityEngine;

public class CeilingCore : MonoBehaviour
{
    [SerializeField] private SpriteRenderer coreRenderer;
    [SerializeField] private PulseVisual pulseVisual;
    [SerializeField] private DamageFlashVisual damageFlashVisual;

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
    }

    public void Initialize(int segmentIndex)
    {
        this.segmentIndex = segmentIndex;
        isAlive = true;
        isConnected = false;
        isVisible = true;
        ResetVisual();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        ApplyState();
    }

    public void SetConnectedState(bool connected)
    {
        isConnected = connected;
        ApplyState();
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
}
