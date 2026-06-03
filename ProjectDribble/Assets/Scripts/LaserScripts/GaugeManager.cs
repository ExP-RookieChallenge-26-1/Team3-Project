using System;
using DefaultNamespace;
using UnityEngine;

public class GaugeManager : MonoBehaviour
{
    [SerializeField] private ScriptableObjects.LaserData _data;

    [Header("Gauge")]
    [SerializeField] private int currentGaugeValue = 0;

    public int CurrentGaugeValue => currentGaugeValue;
    public int FilledGaugeSegments { get; private set; }
    public int MaxGaugeValue { get; private set; }
    public int MaxGaugeSegments => _data.maxGaugeSegments;
    public int GaugePerSegment => _data.gaugePerSegment;

    public event Action<int> OnGaugeValueChanged;
    public event Action<int> OnGaugeSegmentChanged;

    private void Start()
    {
        MaxGaugeValue = _data.maxGaugeSegments * _data.gaugePerSegment;
        SetGaugeValue(_data.startGaugeValue);
    }

    public void InitializeGauge(int startValue)
    {
        MaxGaugeValue = _data.maxGaugeSegments * _data.gaugePerSegment;
        SetGaugeValue(startValue);
    }

    public void ResetGauge()
    {
        InitializeGauge(_data.startGaugeValue);
    }

    private void SetGaugeValue(int value)
    {
        int previousSegments = FilledGaugeSegments;

        currentGaugeValue = Mathf.Clamp(value, 0, MaxGaugeValue);

        UpdateGaugeSegmentByValue(currentGaugeValue);

        OnGaugeValueChanged?.Invoke(currentGaugeValue);

        if (previousSegments != FilledGaugeSegments)
        {
            OnGaugeSegmentChanged?.Invoke(FilledGaugeSegments);

            if (FilledGaugeSegments > previousSegments)
            {
                float ratio = MaxGaugeSegments > 0
                    ? FilledGaugeSegments / (float)MaxGaugeSegments
                    : 0f;
                SoundManager.Instance.Play(SoundId.GaugeSegmentFilled, ratio);
            }
        }
    }

    private void UpdateGaugeSegmentByValue(int value)
    {
        FilledGaugeSegments = value / _data.gaugePerSegment;

        FilledGaugeSegments = Mathf.Clamp(
            FilledGaugeSegments,
            0,
            _data.maxGaugeSegments
        );

    }

    public void AddGauge()
    {
        SetGaugeValue(currentGaugeValue + 1);
    }

    public void AddGauge(int amount)
    {
        SetGaugeValue(currentGaugeValue + amount);
    }

    public bool TryReduceGaugeLevel()
    {
        if (FilledGaugeSegments < 1)
            return false;

        SetGaugeValue(currentGaugeValue - _data.gaugePerSegment);

        return true;
    }
}
