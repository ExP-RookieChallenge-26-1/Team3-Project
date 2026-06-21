using TMPro;
using UnityEngine;

public class GaugeUI : MonoBehaviour
{
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private LaserUnlockState laserUnlockState;
    [SerializeField] private GameObject gaugeVisualRoot;
    [SerializeField] private CanvasGroup gaugeCanvasGroup;

    [Header("Gauge Bar")]
    [SerializeField] private Transform gaugeBar;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    private Vector3 maxGaugeScale;

    private void Awake()
    {
        if (gaugeManager == null)
            gaugeManager = FindAnyObjectByType<GaugeManager>();

        if (laserUnlockState == null)
            laserUnlockState = FindAnyObjectByType<LaserUnlockState>();

        if (gaugeCanvasGroup == null)
            gaugeCanvasGroup = GetComponent<CanvasGroup>();

        if (gaugeBar != null)
        {
            maxGaugeScale = gaugeBar.localScale;
        }
    }

    private void OnEnable()
    {
        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged += UpdateGaugeBar;
            gaugeManager.OnGaugeValueChanged += UpdateGaugeValueText;
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
            gaugeManager.OnGaugeValueChanged -= UpdateGaugeBar;
            gaugeManager.OnGaugeValueChanged -= UpdateGaugeValueText;
            gaugeManager.OnGaugeSegmentChanged -= UpdateGaugeSegmentText;
        }

        if (laserUnlockState != null)
        {
            laserUnlockState.OnLaserUnlocked -= HandleLaserUnlockChanged;
            laserUnlockState.OnLaserLocked -= HandleLaserUnlockChanged;
        }
    }

    private void RefreshAll()
    {
        if (gaugeManager == null)
            return;

        RefreshVisibility();
        UpdateGaugeBar(gaugeManager.CurrentGaugeValue);
        UpdateGaugeValueText(gaugeManager.CurrentGaugeValue);
        UpdateGaugeSegmentText(gaugeManager.FilledGaugeSegments);
    }

    private void UpdateGaugeBar(int value)
    {
        if (gaugeBar == null)
            return;

        float normalized = gaugeManager.MaxGaugeValue > 0
            ? Mathf.Clamp01((float)value / gaugeManager.MaxGaugeValue)
            : 0f;

        gaugeBar.localScale = new Vector3(
            maxGaugeScale.x * (1f - normalized),
            maxGaugeScale.y,
            maxGaugeScale.z
        );
    }

    private void UpdateGaugeValueText(int value)
    {
        if (gaugeValueText == null)
            return;

        gaugeValueText.text = $"Gauge Value: {value}";
    }

    private void UpdateGaugeSegmentText(int segment)
    {
        if (gaugeSegmentText == null)
            return;

        gaugeSegmentText.text =
            $"Gauge Segments: {segment} / {gaugeManager.MaxGaugeSegments}";
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
            gaugeVisualRoot.SetActive(unlocked);

        if (gaugeCanvasGroup == null && gaugeVisualRoot == null)
        {
            if (gaugeBar != null)
                gaugeBar.gameObject.SetActive(true);

            if (gaugeSegmentText != null)
                gaugeSegmentText.gameObject.SetActive(unlocked);

            if (gaugeValueText != null)
                gaugeValueText.gameObject.SetActive(unlocked);
        }
    }
}
