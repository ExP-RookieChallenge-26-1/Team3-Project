using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class DecorativeFlickerSprite : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private bool canFlicker = true;
    [SerializeField, Min(0f)] private float flickerAmount = 0.2f;
    [SerializeField, Min(0f)] private float flickerSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.25f;
    [SerializeField] private float phase;
    [SerializeField] private bool randomizePhaseOnAwake = true;
    [SerializeField] private bool captureColorOnEnable = true;

    private Color baseColor = Color.white;
    private bool hasBaseColor;

    private void Awake()
    {
        EnsureRenderer();

        if (randomizePhaseOnAwake)
            phase = Random.Range(0f, Mathf.PI * 2f);

        CaptureBaseColor();
    }

    private void OnEnable()
    {
        EnsureRenderer();

        if (captureColorOnEnable || !hasBaseColor)
            CaptureBaseColor();
    }

    private void Update()
    {
        if (targetRenderer == null || !hasBaseColor)
            return;

        targetRenderer.color = SharedFlickerSignal.ApplyAlphaFlicker(
            baseColor,
            canFlicker,
            flickerAmount,
            flickerSpeed,
            phase,
            minAlpha
        );
    }

    private void OnDisable()
    {
        RestoreBaseColor();
    }

    public void SetFlickerEnabled(bool enabled)
    {
        canFlicker = enabled;

        if (!canFlicker)
            RestoreBaseColor();
    }

    public void CaptureBaseColor()
    {
        if (targetRenderer == null)
            return;

        baseColor = targetRenderer.color;
        hasBaseColor = true;
    }

    private void RestoreBaseColor()
    {
        if (targetRenderer == null || !hasBaseColor)
            return;

        targetRenderer.color = baseColor;
    }

    private void EnsureRenderer()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }
}
