using UnityEngine;

public class PulseVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private bool useUnscaledTime;

    private bool isPulsing;
    private float baseAlpha = 1f;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer != null)
            baseAlpha = targetRenderer.color.a;
    }

    private void Update()
    {
        if (!isPulsing || targetRenderer == null)
            return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f;
        SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, t));
    }

    public void SetPulsing(bool enabled)
    {
        isPulsing = enabled;

        if (!isPulsing)
            SetAlpha(baseAlpha);
    }

    public void SetBaseAlpha(float alpha)
    {
        baseAlpha = Mathf.Clamp01(alpha);

        if (!isPulsing)
            SetAlpha(baseAlpha);
    }

    public void ResetVisual()
    {
        isPulsing = false;
        SetAlpha(baseAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (targetRenderer == null)
            return;

        Color color = targetRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        targetRenderer.color = color;
    }
}
