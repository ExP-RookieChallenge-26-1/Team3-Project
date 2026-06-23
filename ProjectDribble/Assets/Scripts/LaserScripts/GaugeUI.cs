using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GaugeUI : MonoBehaviour
{
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private LaserUnlockState laserUnlockState;
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
    [Min(0f)]
    [SerializeField] private float flashDuration = 0.12f;
    [Range(0f, 1f)]
    [SerializeField] private float minFlashAlpha = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float maxFlashAlpha = 0.9f;
    [Min(1f)]
    [SerializeField] private float popScale = 1.12f;
    [Min(0f)]
    [SerializeField] private float popDuration = 0.16f;

    [Header("Available Laser Pulse")]
    [Min(0f)]
    [SerializeField] private float availablePulseSpeed = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float availablePulseMinAlpha = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float availablePulseMaxAlpha = 1f;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    private const string GeneratedSlotNamePrefix = "GaugeSlot_";

    private readonly List<SpriteRenderer> slotRenderers = new List<SpriteRenderer>();
    private readonly List<Transform> slotTransforms = new List<Transform>();
    private readonly List<Vector3> slotBaseScales = new List<Vector3>();
    private float[] flashEndTimes;
    private float[] flashAlphas;
    private float[] popStartTimes;
    private int filledSlotCount;
    private int availableSlotCount;
    private int lastGaugeValue;

    private void Awake()
    {
        if (gaugeManager == null)
            gaugeManager = FindAnyObjectByType<GaugeManager>();

        if (laserUnlockState == null)
            laserUnlockState = FindAnyObjectByType<LaserUnlockState>();

        if (gaugeCanvasGroup == null)
            gaugeCanvasGroup = GetComponent<CanvasGroup>();

        DisableLegacyGauge();
        BuildSlots();
    }

    private void OnEnable()
    {
        DisableLegacyGauge();

        if (slotRenderers.Count != totalSlots)
            BuildSlots();

        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged += HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged += UpdateGaugeSegmentText;
        }

        if (laserUnlockState != null)
        {
            laserUnlockState.OnLaserUnlocked += HandleLaserUnlockChanged;
            laserUnlockState.OnLaserLocked += HandleLaserUnlockChanged;
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

        if (laserUnlockState != null)
        {
            laserUnlockState.OnLaserUnlocked -= HandleLaserUnlockChanged;
            laserUnlockState.OnLaserLocked -= HandleLaserUnlockChanged;
        }
    }

    private void Update()
    {
        UpdateSlotVisuals();
    }

    private void RefreshAll()
    {
        if (gaugeManager == null)
            return;

        RefreshVisibility();
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

        if (value > lastGaugeValue && popStartTimes != null)
        {
            int popEnd = Mathf.Min(newFilledSlots, popStartTimes.Length);
            for (int i = previousFilledSlots; i < popEnd; i++)
                popStartTimes[i] = Time.unscaledTime;

            int nextSlot = newFilledSlots;
            float progress = CalculateNextSlotProgress(value, nextSlot);
            if (nextSlot < slotRenderers.Count && progress > 0f)
            {
                flashEndTimes[nextSlot] = Time.unscaledTime + flashDuration;
                flashAlphas[nextSlot] = Mathf.Lerp(minFlashAlpha, maxFlashAlpha, progress);
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

            GameObject visual = slotPrefab != null
                ? Instantiate(slotPrefab, slot.transform)
                : new GameObject("Visual");
            visual.transform.SetParent(slot.transform, false);

            SpriteRenderer renderer = visual.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
                renderer = visual.AddComponent<SpriteRenderer>();

            if (slotSprite != null)
                renderer.sprite = slotSprite;

            if (renderer.sprite == null)
            {
                slot.SetActive(false);
                continue;
            }

            Vector2 spriteSize = renderer.sprite.rect.size;
            renderer.transform.localScale = new Vector3(
                spriteSize.x > 0f ? slotSize.x / spriteSize.x : 1f,
                spriteSize.y > 0f ? slotSize.y / spriteSize.y : 1f,
                1f);
            renderer.transform.localPosition = -Vector3.Scale(
                renderer.sprite.bounds.center,
                renderer.transform.localScale);

            if (backgroundRenderer != null)
            {
                renderer.sortingLayerID = backgroundRenderer.sortingLayerID;
                renderer.sortingOrder = backgroundRenderer.sortingOrder + 1;
            }

            slotRenderers.Add(renderer);
            slotTransforms.Add(slot.transform);
            slotBaseScales.Add(slot.transform.localScale);
            slot.SetActive(false);
        }

        flashEndTimes = new float[slotRenderers.Count];
        flashAlphas = new float[slotRenderers.Count];
        popStartTimes = new float[slotRenderers.Count];

        for (int i = 0; i < popStartTimes.Length; i++)
            popStartTimes[i] = float.NegativeInfinity;
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
        slotRenderers.Clear();
        slotTransforms.Clear();
        slotBaseScales.Clear();

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
        if (slotRenderers.Count == 0)
            return;

        float time = Time.unscaledTime;
        float pulse = (Mathf.Sin(time * availablePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float pulseAlpha = Mathf.Lerp(availablePulseMinAlpha, availablePulseMaxAlpha, pulse);

        for (int i = 0; i < slotRenderers.Count; i++)
        {
            SpriteRenderer renderer = slotRenderers[i];
            if (renderer == null)
                continue;

            bool filled = i < filledSlotCount;
            bool flashing = !filled && time < flashEndTimes[i];
            slotTransforms[i].gameObject.SetActive(filled || flashing);

            if (!filled && !flashing)
                continue;

            Color color = renderer.color;
            color.a = flashing ? flashAlphas[i] : i < availableSlotCount ? pulseAlpha : 1f;
            renderer.color = color;

            float popProgress = popDuration > 0f
                ? Mathf.Clamp01((time - popStartTimes[i]) / popDuration)
                : 1f;
            float scaleMultiplier = popProgress < 1f
                ? Mathf.Lerp(popScale, 1f, popProgress)
                : 1f;
            slotTransforms[i].localScale = slotBaseScales[i] * scaleMultiplier;
        }
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

    private void HandleLaserUnlockChanged()
    {
        RefreshAll();
    }

    private void RefreshVisibility()
    {
        bool unlocked = laserUnlockState != null && laserUnlockState.IsLaserUnlocked;

        if (gaugeCanvasGroup != null)
        {
            gaugeCanvasGroup.alpha = unlocked ? 1f : 0f;
            gaugeCanvasGroup.interactable = unlocked;
            gaugeCanvasGroup.blocksRaycasts = unlocked;
        }

        if (gaugeVisualRoot != null && gaugeVisualRoot != gameObject)
        {
            gaugeVisualRoot.SetActive(unlocked);
            return;
        }

        if (gaugeBackground != null)
            gaugeBackground.gameObject.SetActive(unlocked);
        else if (slotContainer != null)
            slotContainer.gameObject.SetActive(unlocked);

        if (gaugeSegmentText != null)
            gaugeSegmentText.gameObject.SetActive(unlocked);

        if (gaugeValueText != null)
            gaugeValueText.gameObject.SetActive(unlocked);
    }
}
