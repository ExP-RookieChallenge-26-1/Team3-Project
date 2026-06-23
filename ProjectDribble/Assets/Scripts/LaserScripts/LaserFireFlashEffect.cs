using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class LaserFireFlashEffect : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;

    [Header("Animation")]
    [Min(0.01f)]
    [SerializeField] private float duration = 0.12f;
    [FormerlySerializedAs("yOffset")]
    [SerializeField] private float fireFlashYOffset = 0.4f;
    [SerializeField] private float effectZ = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float peakAlpha = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float endAlpha = 0f;
    [Min(0f)]
    [SerializeField] private float startScale = 0.9f;
    [Min(0f)]
    [SerializeField] private float peakScale = 1.08f;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 10;
    [SerializeField] private SpriteRenderer targetRenderer;

    private Coroutine playRoutine;
    private void Awake()
    {
        EnsureRenderer();
        targetRenderer.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (targetRenderer != null)
            targetRenderer.gameObject.SetActive(false);
    }

    public void Play(int chargeLevel, Vector3 paddleCenterPosition)
    {
        if (chargeLevel <= 0)
            return;

        Sprite sprite = chargeLevel == 1 ? level1Sprite : level2Sprite;
        if (sprite == null)
            return;

        EnsureRenderer();

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        targetRenderer.sprite = sprite;
        targetRenderer.sortingLayerName = sortingLayerName;
        targetRenderer.sortingOrder = sortingOrder;

        Vector3 position = paddleCenterPosition;
        position.y += fireFlashYOffset;
        position.z = effectZ;
        targetRenderer.transform.position = position;

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        targetRenderer.gameObject.SetActive(true);

        float riseDuration = duration * 0.35f;
        float fallDuration = duration - riseDuration;
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = riseDuration > 0f ? Mathf.Clamp01(elapsed / riseDuration) : 1f;
            ApplyFrame(
                Mathf.Lerp(startAlpha, peakAlpha, progress),
                Mathf.Lerp(startScale, peakScale, progress));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = fallDuration > 0f ? Mathf.Clamp01(elapsed / fallDuration) : 1f;
            ApplyFrame(
                Mathf.Lerp(peakAlpha, endAlpha, progress),
                Mathf.Lerp(peakScale, startScale, progress));
            yield return null;
        }

        ApplyFrame(endAlpha, startScale);
        targetRenderer.gameObject.SetActive(false);
        playRoutine = null;
    }

    private void ApplyFrame(float alpha, float scale)
    {
        Color color = targetRenderer.color;
        color.a = alpha;
        targetRenderer.color = color;
        targetRenderer.transform.localScale = Vector3.one * scale;
    }

    private void EnsureRenderer()
    {
        if (targetRenderer != null)
            return;

        GameObject visual = new GameObject("LaserFireFlashVisual");
        visual.transform.SetParent(transform, false);
        targetRenderer = visual.AddComponent<SpriteRenderer>();
    }
}
