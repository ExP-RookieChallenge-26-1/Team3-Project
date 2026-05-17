using System;
using TMPro;
using UnityEngine;

public class GaugeManager : MonoBehaviour
{
    [SerializeField] private ScriptableObjectScripts.LaserGaugeData laserGaugeData;
    [SerializeField] private Transform gaugeBar;

    [Header("Gauge")]
    public int filledGaugeSegments = 0;

    [SerializeField] private int currentGaugeValue = 0;
    private int maxGaugeValue;
    private Vector3 maxGaugeScale;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    public event Action<int> OnGaugeValueChanged;

    public int CurrentGaugeValue => currentGaugeValue;

    void Start()
    {
        maxGaugeScale = gaugeBar.localScale;

        maxGaugeValue =
            laserGaugeData.maxGaugeSegments * laserGaugeData.gaugePerSegment;

        OnGaugeValueChanged += UpdateGaugeSegmentByValue;
        OnGaugeValueChanged += TransGaugeBar;
        OnGaugeValueChanged += UpdateGaugeUI;

        SetGaugeValue(laserGaugeData.startGaugeValue);
    }
    
    
    
    private void OnDestroy()
    {
        OnGaugeValueChanged -= UpdateGaugeSegmentByValue;
        OnGaugeValueChanged -= TransGaugeBar;
        OnGaugeValueChanged -= UpdateGaugeUI;
    }

    private void SetGaugeValue(int value)
    {
        currentGaugeValue = Mathf.Clamp(value, 0, maxGaugeValue);

        OnGaugeValueChanged?.Invoke(currentGaugeValue);
    }

    private void UpdateGaugeSegmentByValue(int value)
    {
        filledGaugeSegments = value / laserGaugeData.gaugePerSegment;

        filledGaugeSegments = Mathf.Clamp(
            filledGaugeSegments,
            0,
            laserGaugeData.maxGaugeSegments
        );

        Debug.Log(filledGaugeSegments + " 게이지 세그먼트");
    }

    public void AddGauge()
    {
        SetGaugeValue(currentGaugeValue + 1);
    }

    public bool TryReduceGaugeLevel()
    {
        if (filledGaugeSegments < 1)
            return false;

        SetGaugeValue(currentGaugeValue - laserGaugeData.gaugePerSegment);

        return true;
    }

    private void TransGaugeBar(int value)
    {
        float percent = (float)value / maxGaugeValue;

        Vector3 changedScale = new Vector3(
            maxGaugeScale.x * percent,
            maxGaugeScale.y,
            maxGaugeScale.z
        );

        gaugeBar.localScale = changedScale;
    }

    private void UpdateGaugeUI(int value)
    {
        if (gaugeSegmentText != null)
        {
            gaugeSegmentText.text =
                $"Gauge Segments: {filledGaugeSegments} / {laserGaugeData.maxGaugeSegments}";
        }

        if (gaugeValueText != null)
        {
            gaugeValueText.text = $"Gauge Value: {value}";
        }
    }
}