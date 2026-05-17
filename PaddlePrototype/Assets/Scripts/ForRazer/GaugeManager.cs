using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GaugeManager : MonoBehaviour
{
    [SerializeField] private ScriptableObjectScripts.LaserGaugeData laserGaugeData;
    [SerializeField] private Transform gaugeBar;

    [Header("Gauge")]
    public int filledGaugeSegments = 0;
    private int currentGaugeValue=30;
    private int gaugePerSegment = 10;
    [SerializeField] private int maxGaugeSegments = 3;
    private Vector3 maxGaugeScale;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    public int CurrentGaugeValue => currentGaugeValue;

    private int maxGaugeValue;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        maxGaugeScale = gaugeBar.localScale;
        currentGaugeValue = laserGaugeData.startGaugeValue;
        gaugePerSegment = laserGaugeData.gaugePerSegment;
        maxGaugeSegments = laserGaugeData.maxGaugeSegments;
        
        UpdateGaugeUI();
        TransGaugeBar(currentGaugeValue/maxGaugeSegments);
        maxGaugeValue = maxGaugeSegments * gaugePerSegment;
    }

    public void AddGauge()
    {
        
        // 최대면 추가 X 
        

        if (currentGaugeValue < maxGaugeValue)
            currentGaugeValue++;
        
        ChangeGaugeLevel(currentGaugeValue / gaugePerSegment);

        float percent = (float)currentGaugeValue / (float)maxGaugeValue;
        
        TransGaugeBar(percent);
        UpdateGaugeUI();
    }
    
    
    public void ChangeGaugeLevel(int level)
    {

        if (level < 0)
        {
            if((filledGaugeSegments>0) && (currentGaugeValue>0))
            {
                filledGaugeSegments--;
                currentGaugeValue -= gaugePerSegment;
            }
            
        }   
        else
        {
            filledGaugeSegments = level; 
        }
        
        filledGaugeSegments = Mathf.Clamp(filledGaugeSegments, 0, maxGaugeSegments);
        
        Debug.Log(filledGaugeSegments + "차징 게이지");
        
        UpdateGaugeUI();
        float percent = (float)currentGaugeValue / (float)maxGaugeValue;
        TransGaugeBar(percent);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    private void TransGaugeBar(float percent)
    {
        Vector3 changedScale = new Vector3(maxGaugeScale.x * percent, maxGaugeScale.y,maxGaugeScale.z);

        gaugeBar.localScale = changedScale;
    }
    
    
    private void UpdateGaugeUI()
    {
        /*
        if (gaugeSegmentText != null)
        {
            gaugeSegmentText.text = $"Gauge Segments: {filledGaugeSegments} / {maxGaugeSegments}";
        }

        if (gaugeValueText != null)
        {
            gaugeValueText.text = $"Gauge Value: {currentGaugeValue}";
        }
        */
    }

    
    
}
