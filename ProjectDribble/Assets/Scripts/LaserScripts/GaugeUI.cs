using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GaugeUI : MonoBehaviour
{
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private LaserChargeController laserChargeController;
    [SerializeField] private GameObject gaugeVisualRoot;
    [SerializeField] private CanvasGroup gaugeCanvasGroup;

    [Header("Legacy Gauge (Disabled)")]
    [SerializeField] private Transform gaugeBar;
    [SerializeField] private GameObject legacyGaugeFill;

    [Header("Slot Gauge")]
    [Min(1)]
    [SerializeField] private int totalSlots = 12;
    [SerializeField] private Vector2 slotSize = new Vector2(25f, 80f);
    [Min(0f)]
    [SerializeField] private float slotSpacing = 3f;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform gaugeBackground;
    [SerializeField] private Transform slotContainer;

    [Header("Slot Feedback")]
    [Range(0f, 1f)]
    [SerializeField] private float minPartialAlpha = 0.15f;
    [Range(0f, 1f)]
    [SerializeField] private float maxPartialAlpha = 0.8f;
    [Min(0f)]
    [SerializeField] private float flashDuration = 0.12f;
    [Range(0f, 1f)]
    [SerializeField] private float minFlashGlowAlpha = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float maxFlashGlowAlpha = 1f;
    [Min(1f)]
    [SerializeField] private float flashGlowScale = 1.12f;

    [Header("Available Laser Pulse")]
    [Min(0f)]
    [SerializeField] private float availablePulseSpeed = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float availablePulseMinAlpha = 0.08f;
    [Range(0f, 1f)]
    [SerializeField] private float availablePulseMaxAlpha = 0.22f;

    [Header("Charging Pulse")]
    [Min(0f)]
    [SerializeField] private float chargingPulseSpeed = 4f;
    [Range(0f, 1f)]
    [SerializeField] private float chargingPulseMinAlpha = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] private float chargingPulseMaxAlpha = 1f;
    [Min(1f)]
    [SerializeField] private float chargingGlowScale = 1.15f;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    private const string GeneratedSlotNamePrefix = "GaugeSlot_";

    private readonly List<SpriteRenderer> mainRenderers = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> glowRenderers = new List<SpriteRenderer>();
    private readonly List<Transform> slotTransforms = new List<Transform>();
    private readonly List<Vector3> glowBaseScales = new List<Vector3>();
    private float[] flashEndTimes;
    private float[] flashAlphas;
    private int filledSlotCount;
    private int availableSlotCount;
    private int lastGaugeValue;

    private void Awake()
    {
        if (gaugeManager == null)
            gaugeManager = FindAnyObjectByType<GaugeManager>();

        if (laserChargeController == null)
            laserChargeController = FindAnyObjectByType<LaserChargeController>();

        if (gaugeCanvasGroup == null)
            gaugeCanvasGroup = GetComponent<CanvasGroup>();

        DisableLegacyGauge();
        BuildSlots();
    }

    private void OnEnable()
    {
        DisableLegacyGauge();

        if (mainRenderers.Count != totalSlots || glowRenderers.Count != totalSlots)
            BuildSlots();

        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged += HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged += UpdateGaugeSegmentText;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged -= HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged -= UpdateGaugeSegmentText;
        }
    }

    private void Update()
    {
        UpdateSlotVisuals();
    }

    private void RefreshAll()
    {
        EnsureGaugeVisible();

        if (gaugeManager == null)
            return;

        lastGaugeValue = gaugeManager.CurrentGaugeValue;
        RefreshSlotCounts(lastGaugeValue);
        UpdateGaugeValueText(lastGaugeValue);
        UpdateGaugeSegmentText(gaugeManager.FilledGaugeSegments);
        UpdateSlotVisuals();
    }

    private void HandleGaugeValueChanged(int value)
    {
        int previousFilledSlots = CalculateFilledSlots(lastGaugeValue);
        int newFilledSlots = CalculateFilledSlots(value);

        bool charging = laserChargeController != null && laserChargeController.IsCharging;
        bool returningGauge = laserChargeController != null && laserChargeController.IsReturningGauge;
        if (value > lastGaugeValue && flashEndTimes != null && !charging && !returningGauge)
        {
            int flashSlot = newFilledSlots > previousFilledSlots
                ? newFilledSlots - 1
                : newFilledSlots;
            float progress = newFilledSlots > previousFilledSlots
                ? 1f
                : CalculateNextSlotProgress(value, flashSlot);
            if (flashSlot >= 0 && flashSlot < glowRenderers.Count && progress > 0f)
            {
                flashEndTimes[flashSlot] = Time.unscaledTime + flashDuration;
                flashAlphas[flashSlot] = Mathf.Lerp(
                    minFlashGlowAlpha,
                    maxFlashGlowAlpha,
                    progress);
            }
        }

        lastGaugeValue = value;
        RefreshSlotCounts(value);
        UpdateGaugeValueText(value);
        UpdateSlotVisuals();
    }

    private void RefreshSlotCounts(int value)
    {
        filledSlotCount = CalculateFilledSlots(value);

        int availableGauge = gaugeManager.FilledGaugeSegments * gaugeManager.GaugePerSegment;
        availableSlotCount = CalculateFilledSlots(availableGauge);
    }

    private int CalculateFilledSlots(int value)
    {
        if (gaugeManager == null || gaugeManager.MaxGaugeValue <= 0 || totalSlots <= 0)
            return 0;

        value = Mathf.Clamp(value, 0, gaugeManager.MaxGaugeValue);
        int baseRequirement = gaugeManager.MaxGaugeValue / totalSlots;
        int remainder = gaugeManager.MaxGaugeValue % totalSlots;
        int smallerSlotCount = totalSlots - remainder;
        int consumed = 0;
        int filled = 0;

        for (int i = 0; i < totalSlots; i++)
        {
            int requirement = baseRequirement + (i >= smallerSlotCount ? 1 : 0);
            consumed += requirement;

            if (value < consumed)
                break;

            filled++;
        }

        return filled;
    }

    private float CalculateNextSlotProgress(int value, int slotIndex)
    {
        if (gaugeManager == null || gaugeManager.MaxGaugeValue <= 0 || slotIndex >= totalSlots)
            return 0f;

        int baseRequirement = gaugeManager.MaxGaugeValue / totalSlots;
        int remainder = gaugeManager.MaxGaugeValue % totalSlots;
        int smallerSlotCount = totalSlots - remainder;
        int slotStartValue = 0;

        for (int i = 0; i < slotIndex; i++)
            slotStartValue += baseRequirement + (i >= smallerSlotCount ? 1 : 0);

        int requirement = baseRequirement + (slotIndex >= smallerSlotCount ? 1 : 0);
        if (requirement <= 0)
            return 1f;

        return Mathf.Clamp01((value - slotStartValue) / (float)requirement);
    }

    private void BuildSlots()
    {
        if (totalSlots <= 0 || (slotSprite == null && slotPrefab == null))
            return;

        EnsureSlotContainer();
        ClearGeneratedSlots();

        SpriteRenderer backgroundRenderer = gaugeBackground != null
            ? gaugeBackground.GetComponent<SpriteRenderer>()
            : null;
        float pixelsPerUnit = backgroundRenderer != null && backgroundRenderer.sprite != null
            ? backgroundRenderer.sprite.pixelsPerUnit
            : slotSprite != null ? slotSprite.pixelsPerUnit : 100f;
        float totalWidth = slotSize.x * totalSlots + slotSpacing * (totalSlots - 1);
        float startX = -totalWidth * 0.5f + slotSize.x * 0.5f;

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slot = slotContainer is RectTransform
                ? new GameObject(string.Empty, typeof(RectTransform))
                : new GameObject();
            slot.name = $"{GeneratedSlotNamePrefix}{i + 1:00}";
            slot.transform.SetParent(slotContainer, false);
            float x = startX + i * (slotSize.x + slotSpacing);

            if (slot.transform is RectTransform slotRect)
            {
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = new Vector2(x, 0f);
                slotRect.sizeDelta = slotSize;
            }
            else
            {
                slot.transform.localPosition = new Vector3(x / pixelsPerUnit, 0f, -0.01f);
            }

            slot.transform.localRotation = Quaternion.identity;

            GameObject mainImage = slotPrefab != null
                ? Instantiate(slotPrefab, slot.transform)
                : new GameObject("MainImage");
            mainImage.name = "MainImage";
            mainImage.transform.SetParent(slot.transform, false);

            SpriteRenderer mainRenderer = mainImage.GetComponentInChildren<SpriteRenderer>();
            if (mainRenderer == null)
                mainRenderer = mainImage.AddComponent<SpriteRenderer>();

            if (slotSprite != null)
                mainRenderer.sprite = slotSprite;

            if (mainRenderer.sprite == null)
            {
                slot.SetActive(false);
                continue;
            }

            GameObject glowImage = new GameObject("GlowImage");
            glowImage.transform.SetParent(slot.transform, false);
            SpriteRenderer glowRenderer = glowImage.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = mainRenderer.sprite;

            ConfigureSlotRenderer(mainRenderer);
            ConfigureSlotRenderer(glowRenderer);

            if (backgroundRenderer != null)
            {
                mainRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
                mainRenderer.sortingOrder = backgroundRenderer.sortingOrder + 1;
                glowRenderer.sortingLayerID = backgroundRenderer.sortingLayerID;
                glowRenderer.sortingOrder = backgroundRenderer.sortingOrder + 2;
            }

            Color glowColor = glowRenderer.color;
            glowColor.a = 0f;
            glowRenderer.color = glowColor;

            mainRenderers.Add(mainRenderer);
            glowRenderers.Add(glowRenderer);
            slotTransforms.Add(slot.transform);
            glowBaseScales.Add(glowRenderer.transform.localScale);
            slot.SetActive(false);
        }

        flashEndTimes = new float[mainRenderers.Count];
        flashAlphas = new float[mainRenderers.Count];
    }

    private void ConfigureSlotRenderer(SpriteRenderer renderer)
    {
        Vector2 spriteSize = renderer.sprite.rect.size;
        renderer.transform.localScale = new Vector3(
            spriteSize.x > 0f ? slotSize.x / spriteSize.x : 1f,
            spriteSize.y > 0f ? slotSize.y / spriteSize.y : 1f,
            1f);
        CenterRenderer(renderer, renderer.transform.localScale);
    }

    private static void CenterRenderer(SpriteRenderer renderer, Vector3 scale)
    {
        renderer.transform.localScale = scale;
        renderer.transform.localPosition = -Vector3.Scale(renderer.sprite.bounds.center, scale);
    }

    private void EnsureSlotContainer()
    {
        Transform parent = gaugeBackground != null ? gaugeBackground : transform;
        if (slotContainer == null)
        {
            Transform existing = parent.Find("GaugeSlots");
            if (existing != null)
                slotContainer = existing;
        }

        if (slotContainer == null)
        {
            GameObject container = parent is RectTransform
                ? new GameObject("GaugeSlots", typeof(RectTransform))
                : new GameObject("GaugeSlots");
            slotContainer = container.transform;
        }

        if (slotContainer.parent != parent)
            slotContainer.SetParent(parent, false);

        slotContainer.localRotation = Quaternion.identity;
        slotContainer.localScale = Vector3.one;

        if (slotContainer is RectTransform containerRect)
        {
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = Vector2.zero;
            return;
        }

        SpriteRenderer backgroundRenderer = gaugeBackground != null
            ? gaugeBackground.GetComponent<SpriteRenderer>()
            : null;
        slotContainer.localPosition = backgroundRenderer != null && backgroundRenderer.sprite != null
            ? backgroundRenderer.sprite.bounds.center
            : Vector3.zero;
    }

    private void ClearGeneratedSlots()
    {
        mainRenderers.Clear();
        glowRenderers.Clear();
        slotTransforms.Clear();
        glowBaseScales.Clear();

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = slotContainer.GetChild(i);
            if (!child.name.StartsWith(GeneratedSlotNamePrefix))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void UpdateSlotVisuals()
    {
        if (mainRenderers.Count == 0)
            return;

        float time = Time.unscaledTime;
        float partialProgress = CalculateNextSlotProgress(lastGaugeValue, filledSlotCount);
        bool isCharging = laserChargeController != null && laserChargeController.IsCharging;
        int chargingStartSlot = 0;
        int chargingEndSlot = 0;

        if (isCharging && laserChargeController.ConsumedGaugeValue > 0)
        {
            chargingStartSlot = CalculateFilledSlots(lastGaugeValue);
            chargingEndSlot = CalculateFilledSlots(
                lastGaugeValue + laserChargeController.ConsumedGaugeValue);
        }

        float availablePulse = EvaluatePulse(time, availablePulseSpeed);
        float availableAlpha = Mathf.Lerp(
            availablePulseMinAlpha,
            availablePulseMaxAlpha,
            availablePulse);
        float chargingPulse = EvaluatePulse(time, chargingPulseSpeed);
        float chargingAlpha = Mathf.Lerp(
            chargingPulseMinAlpha,
            chargingPulseMaxAlpha,
            chargingPulse);

        for (int i = 0; i < mainRenderers.Count; i++)
        {
            SpriteRenderer mainRenderer = mainRenderers[i];
            SpriteRenderer glowRenderer = glowRenderers[i];
            if (mainRenderer == null || glowRenderer == null)
                continue;

            bool filled = i < filledSlotCount;
            bool partial = i == filledSlotCount && partialProgress > 0f;
            bool charging = isCharging && i >= chargingStartSlot && i < chargingEndSlot;
            bool flashing = !charging && time < flashEndTimes[i];
            bool available = !isCharging && filled && i < availableSlotCount;

            mainRenderer.gameObject.SetActive(filled || partial);
            if (filled || partial)
            {
                Color mainColor = mainRenderer.color;
                mainColor.a = filled
                    ? 1f
                    : Mathf.Lerp(minPartialAlpha, maxPartialAlpha, partialProgress);
                mainRenderer.color = mainColor;
            }

            float glowAlpha = 0f;
            float glowScale = 1f;

            if (charging)
            {
                glowAlpha = chargingAlpha;
                glowScale = chargingGlowScale;
            }
            else if (flashing)
            {
                float remaining = flashDuration > 0f
                    ? Mathf.Clamp01((flashEndTimes[i] - time) / flashDuration)
                    : 0f;
                glowAlpha = flashAlphas[i] * remaining;
                glowScale = Mathf.Lerp(1f, flashGlowScale, remaining);
            }
            else if (available)
            {
                glowAlpha = availableAlpha;
                glowScale = 1.04f;
            }

            glowRenderer.gameObject.SetActive(glowAlpha > 0f);
            if (glowAlpha > 0f)
            {
                Color glowColor = glowRenderer.color;
                glowColor.a = glowAlpha;
                glowRenderer.color = glowColor;
                CenterRenderer(glowRenderer, glowBaseScales[i] * glowScale);
            }

            slotTransforms[i].gameObject.SetActive(
                mainRenderer.gameObject.activeSelf || glowRenderer.gameObject.activeSelf);
        }
    }

    private static float EvaluatePulse(float time, float speed)
    {
        return (Mathf.Sin(time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
    }

    private void DisableLegacyGauge()
    {
        if (gaugeBar != null)
            gaugeBar.gameObject.SetActive(false);

        if (legacyGaugeFill == null)
        {
            Transform legacyFill = transform.Find("GaugePP");
            if (legacyFill != null)
                legacyGaugeFill = legacyFill.gameObject;
        }

        if (legacyGaugeFill != null)
            legacyGaugeFill.SetActive(false);
    }

    private void UpdateGaugeValueText(int value)
    {
        if (gaugeValueText != null)
            gaugeValueText.text = $"Gauge Value: {value}";
    }

    private void UpdateGaugeSegmentText(int segment)
    {
        if (gaugeSegmentText != null)
        {
            gaugeSegmentText.text =
                $"Gauge Segments: {segment} / {gaugeManager.MaxGaugeSegments}";
        }
    }

    private void EnsureGaugeVisible()
    {
        if (gaugeCanvasGroup != null)
        {
            gaugeCanvasGroup.alpha = 1f;
            gaugeCanvasGroup.interactable = true;
            gaugeCanvasGroup.blocksRaycasts = true;
        }

        if (gaugeVisualRoot != null && gaugeVisualRoot != gameObject)
        {
            gaugeVisualRoot.SetActive(true);
            return;
        }

        if (gaugeBackground != null)
            gaugeBackground.gameObject.SetActive(true);
        else if (slotContainer != null)
            slotContainer.gameObject.SetActive(true);

        if (gaugeSegmentText != null)
            gaugeSegmentText.gameObject.SetActive(true);

        if (gaugeValueText != null)
            gaugeValueText.gameObject.SetActive(true);
    }
}
