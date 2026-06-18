using System.Collections;
using UnityEngine;

public class DamageFlashVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color flashColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float recoverDuration = 0.12f;

    private Coroutine flashRoutine;
    private Color baseColor = Color.white;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        CaptureBaseColor();
    }

    public void PlayFlash()
    {
        if (targetRenderer == null || !isActiveAndEnabled)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void ResetVisual()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        ApplyRgb(baseColor);
    }

    public void CaptureBaseColor()
    {
        if (targetRenderer == null)
            return;

        baseColor = targetRenderer.color;
    }

    private IEnumerator FlashRoutine()
    {
        Color startColor = targetRenderer.color;
        baseColor = startColor;

        ApplyRgb(flashColor);

        if (flashDuration > 0f)
            yield return new WaitForSeconds(flashDuration);

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, recoverDuration);

        while (elapsed < duration && targetRenderer != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color color = Color.Lerp(flashColor, baseColor, t);
            ApplyRgb(color);
            yield return null;
        }

        ApplyRgb(baseColor);
        flashRoutine = null;
    }

    private void ApplyRgb(Color rgbSource)
    {
        if (targetRenderer == null)
            return;

        Color color = targetRenderer.color;
        color.r = rgbSource.r;
        color.g = rgbSource.g;
        color.b = rgbSource.b;
        targetRenderer.color = color;
    }
}
