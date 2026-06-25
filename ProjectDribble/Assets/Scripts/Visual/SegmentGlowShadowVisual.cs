using UnityEngine;

public sealed class SegmentGlowShadowVisual : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Generated Glow")]
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField] private string generatedChildName = "Auto Glow Shadow";

    [Header("Appearance")]
    [SerializeField] private Material silhouetteGlowMaterial;
    [SerializeField] private Color glowColor = Color.green;
    [SerializeField, Min(0f)] private float scaleMultiplier = 1.12f;
    [SerializeField] private int sortingOrderOffset = -1;

    [Header("Transform")]
    [SerializeField] private Vector2 localOffset = Vector2.zero;

    [Header("Pulse")]
    [SerializeField, Range(0f, 1f)] private float alphaMin = 0.1f;
    [SerializeField, Range(0f, 1f)] private float alphaMax = 0.55f;
    [SerializeField, Min(0f)] private float pulseSpeed = 1f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );
    [SerializeField] private bool useUnscaledTime;

    [Header("Disconnected")]
    [SerializeField] private Color disconnectedGlowColor = new Color(0.45f, 0.5f, 0.42f, 1f);
    [SerializeField, Range(0f, 1f)] private float disconnectedAlpha = 0.08f;

    private bool isVisible;
    private bool isConnected;
    private SpriteRenderer scaleSourceRenderer;
    private Vector3 targetBaseScale = Vector3.one;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        EnsureTargetRenderer();
        EnsureGlowRenderer();
        RefreshFromTarget();
        ApplyState();
    }

    private void Update()
    {
        RefreshTransformFromTarget();

        if (targetRenderer != null && glowRenderer != null &&
            glowRenderer.sprite != targetRenderer.sprite)
        {
            RefreshFromTarget();
        }

        if (!isVisible || !isConnected || glowRenderer == null)
            return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = Mathf.Repeat(time * Mathf.Max(0f, pulseSpeed) + phaseOffset, 1f);
        float value = alphaCurve != null && alphaCurve.length > 0
            ? Mathf.Clamp01(alphaCurve.Evaluate(t))
            : t;

        SetColorAndAlpha(glowColor, Mathf.Lerp(alphaMin, alphaMax, value));
    }

    public void Initialize(SpriteRenderer renderer)
    {
        if (renderer != null)
        {
            if (glowRenderer == renderer)
                glowRenderer = null;

            targetRenderer = renderer;
        }

        EnsureTargetRenderer();
        EnsureGlowRenderer();
        RefreshFromTarget();
        ApplyState();
    }

    public void ApplyVisualProfile(CeilingSegmentVisualProfile profile)
    {
        if (profile == null)
            return;

        glowColor = profile.glowColor;
        scaleMultiplier = Mathf.Max(0f, profile.scaleMultiplier);
        alphaMin = Mathf.Clamp01(profile.glowAlphaMin);
        alphaMax = Mathf.Clamp01(profile.glowAlphaMax);
        pulseSpeed = Mathf.Max(0f, profile.pulseSpeed);
        phaseOffset = profile.phaseOffset;
        disconnectedGlowColor = profile.disconnectedGlowColor;
        disconnectedAlpha = Mathf.Clamp01(profile.disconnectedGlowAlpha);

        if (profile.alphaCurve != null && profile.alphaCurve.length > 0)
            alphaCurve = profile.alphaCurve;

        RefreshFromTarget();
        ApplyState();
    }

    public void SetState(bool visible, bool connected)
    {
        isVisible = visible;
        isConnected = connected;
        EnsureTargetRenderer();
        EnsureGlowRenderer();
        RefreshFromTarget();
        ApplyState();
    }

    public void RefreshFromTarget()
    {
        if (targetRenderer == null || glowRenderer == null)
            return;

        RefreshTransformFromTarget();

        glowRenderer.sprite = targetRenderer.sprite;
        glowRenderer.sharedMaterial = silhouetteGlowMaterial != null
            ? silhouetteGlowMaterial
            : targetRenderer.sharedMaterial;
        glowRenderer.sortingLayerID = targetRenderer.sortingLayerID;
        glowRenderer.sortingOrder = targetRenderer.sortingOrder + sortingOrderOffset;
        glowRenderer.maskInteraction = targetRenderer.maskInteraction;
        glowRenderer.flipX = targetRenderer.flipX;
        glowRenderer.flipY = targetRenderer.flipY;
        glowRenderer.drawMode = targetRenderer.drawMode;
        glowRenderer.size = targetRenderer.size;
        glowRenderer.gameObject.layer = targetRenderer.gameObject.layer;
    }

    private void EnsureTargetRenderer()
    {
        if (targetRenderer != null && targetRenderer != glowRenderer)
            return;

        SpriteRenderer ownRenderer = GetComponent<SpriteRenderer>();

        if (ownRenderer != null && ownRenderer != glowRenderer)
        {
            targetRenderer = ownRenderer;
            return;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer candidate = renderers[i];

            if (candidate == null || candidate == glowRenderer)
                continue;

            if (candidate.gameObject.name == generatedChildName)
                continue;

            targetRenderer = candidate;
            return;
        }
    }

    private void EnsureGlowRenderer()
    {
        if (glowRenderer == targetRenderer)
            glowRenderer = null;

        if (targetRenderer == null || glowRenderer != null)
            return;

        GameObject glowObject = new GameObject(generatedChildName);
        glowObject.transform.SetParent(targetRenderer.transform, false);
        glowRenderer = glowObject.AddComponent<SpriteRenderer>();
    }

    private void RefreshTransformFromTarget()
    {
        if (targetRenderer == null || glowRenderer == null)
            return;

        Transform targetTransform = targetRenderer.transform;
        Transform glowTransform = glowRenderer.transform;

        if (glowTransform.parent != targetTransform)
            glowTransform.SetParent(targetTransform, false);

        glowTransform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);
        glowTransform.localRotation = Quaternion.identity;

        if (scaleSourceRenderer != targetRenderer)
        {
            scaleSourceRenderer = targetRenderer;
            targetBaseScale = targetTransform.localScale;
        }

        Vector3 currentScale = targetTransform.localScale;
        glowTransform.localScale = new Vector3(
            GetScaleRatio(targetBaseScale.x, currentScale.x) * scaleMultiplier,
            GetScaleRatio(targetBaseScale.y, currentScale.y) * scaleMultiplier,
            GetScaleRatio(targetBaseScale.z, currentScale.z)
        );
    }

    private static float GetScaleRatio(float baseValue, float currentValue)
    {
        return Mathf.Abs(currentValue) > Mathf.Epsilon ? baseValue / currentValue : 1f;
    }

    private void ApplyState()
    {
        if (glowRenderer == null)
            return;

        glowRenderer.enabled = isVisible && targetRenderer != null && targetRenderer.enabled;

        if (!glowRenderer.enabled)
            return;

        if (isConnected)
            SetColorAndAlpha(glowColor, alphaMin);
        else
            SetColorAndAlpha(disconnectedGlowColor, disconnectedAlpha);
    }

    private void SetColorAndAlpha(Color color, float alpha)
    {
        if (glowRenderer == null)
            return;

        color.a = Mathf.Clamp01(alpha);

        if (silhouetteGlowMaterial == null)
        {
            glowRenderer.SetPropertyBlock(null);
            glowRenderer.color = color;
            return;
        }

        glowRenderer.color = Color.white;
        propertyBlock ??= new MaterialPropertyBlock();
        glowRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(GlowColorId, color);
        propertyBlock.SetColor(ColorId, color);
        glowRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        isVisible = false;

        if (glowRenderer != null)
            glowRenderer.enabled = false;
    }

}
