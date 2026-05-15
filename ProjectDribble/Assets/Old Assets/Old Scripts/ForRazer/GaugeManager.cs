using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GaugeManager : MonoBehaviour
{
    
    

    [Header("Gauge")]
    public int filledGaugeSegments = 0;
    private int currentGaugeValue=30;
    private int gaugePerSegment = 10;
    [SerializeField] private int maxGaugeSegments = 3;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI gaugeSegmentText;
    [SerializeField] private TextMeshProUGUI gaugeValueText;

    public int CurrentGaugeValue => currentGaugeValue;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateGaugeUI();
    }

    public void AddGauge()
    {
        
        // 최대면 추가 X 
        int maxGaugeValue = maxGaugeSegments * gaugePerSegment;

        if (!(currentGaugeValue >= maxGaugeValue))
            currentGaugeValue++;
        
        ChangeGaugeLevel(currentGaugeValue / gaugePerSegment);

        UpdateGaugeUI();
    }
    
    
    public void ChangeGaugeLevel(int level)
    {

        if (level <= 0)
        {
            if(!(filledGaugeSegments<=0) && !(currentGaugeValue<0))
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
        
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
    private void UpdateGaugeUI()
    {
        if (gaugeSegmentText != null)
        {
            gaugeSegmentText.text = $"Gauge Segments: {filledGaugeSegments} / {maxGaugeSegments}";
        }

        if (gaugeValueText != null)
        {
            gaugeValueText.text = $"Gauge Value: {currentGaugeValue}";
        }
    }

    
    
}
