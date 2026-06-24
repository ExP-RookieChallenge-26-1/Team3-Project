using UnityEngine;

public class PulseVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private AnimationCurve pulseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 1f),
        new Keyframe(1f, 0f)
    );
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private Color pulseColor = Color.green;
    [SerializeField] private float scaleAmount = 0.06f;
    [SerializeField] private bool useScalePulse = true;
    [SerializeField] private bool useUnscaledTime;

    private bool isPulsing;
    private float baseAlpha = 1f;
    private Color baseColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private bool canScaleTarget;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer == null)
            return;

        baseColor = targetRenderer.color;
        baseAlpha = baseColor.a;
        baseScale = targetRenderer.transform.localScale;
        canScaleTarget = targetRenderer.GetComponentsInChildren<Collider2D>(true).Length == 0;

        if (pulseCurve == null || pulseCurve.length == 0)
        {
            pulseCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 1f),
                new Keyframe(1f, 0f)
            );
        }
    }

    private void Update()
    {
        if (!isPulsing || targetRenderer == null)
            return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = Mathf.Repeat(time * Mathf.Max(0f, pulseSpeed), 1f);
        float value = Mathf.Clamp01(pulseCurve.Evaluate(t));
        ApplyPulse(value);
    }

    public void SetPulsing(bool enabled)
    {
        isPulsing = enabled;

        if (!isPulsing)
            RestoreVisual();
    }

    public void SetBaseAlpha(float alpha)
    {
        baseAlpha = Mathf.Clamp01(alpha);

        if (!isPulsing)
            RestoreVisual();
    }

    public void SetUseUnscaledTime(bool enabled)
    {
        useUnscaledTime = enabled;
    }

    public void ResetVisual()
    {
        isPulsing = false;
        RestoreVisual();
    }

    private void OnDisable()
    {
        isPulsing = false;
        RestoreVisual();
    }

    private void ApplyPulse(float value)
    {
        if (targetRenderer == null)
            return;

        Color color = Color.Lerp(baseColor, pulseColor, value);
        color.a = Mathf.Lerp(minAlpha, maxAlpha, value);
        targetRenderer.color = color;

        if (useScalePulse && canScaleTarget)
            targetRenderer.transform.localScale = baseScale * (1f + scaleAmount * value);
    }

    private void RestoreVisual()
    {
        if (targetRenderer == null)
            return;

        Color color = baseColor;
        color.a = baseAlpha;
        targetRenderer.color = color;

        if (canScaleTarget)
            targetRenderer.transform.localScale = baseScale;
    }
}
