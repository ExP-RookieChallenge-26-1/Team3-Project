using TMPro;
using UnityEngine;

public class GaugeUI : MonoBehaviour
{
    [SerializeField] private GaugeManager gaugeManager;

    [Header("Gauge Bar")]
    [SerializeField] private Transform gaugeBar;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    private Vector3 maxGaugeScale;

    private void Awake()
    {
        if (gaugeBar != null)
        {
            maxGaugeScale = gaugeBar.localScale;
        }
    }

    private void OnEnable()
    {
        if (gaugeManager == null)
            return;

        gaugeManager.OnGaugeValueChanged += UpdateGaugeBar;
        gaugeManager.OnGaugeValueChanged += UpdateGaugeValueText;
        gaugeManager.OnGaugeSegmentChanged += UpdateGaugeSegmentText;
    }

    private void OnDisable()
    {
        if (gaugeManager == null)
            return;

        gaugeManager.OnGaugeValueChanged -= UpdateGaugeBar;
        gaugeManager.OnGaugeValueChanged -= UpdateGaugeValueText;
        gaugeManager.OnGaugeSegmentChanged -= UpdateGaugeSegmentText;
    }

    private void Start()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (gaugeManager == null)
            return;

        UpdateGaugeBar(gaugeManager.CurrentGaugeValue);
        UpdateGaugeValueText(gaugeManager.CurrentGaugeValue);
        UpdateGaugeSegmentText(gaugeManager.FilledGaugeSegments);
    }

    private void UpdateGaugeBar(int value)
    {
        if (gaugeBar == null)
            return;

        float percent = 0f;

        if (gaugeManager.MaxGaugeValue > 0)
        {
            percent = (float)value / gaugeManager.MaxGaugeValue;
        }

        gaugeBar.localScale = new Vector3(
            maxGaugeScale.x * percent,
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
}