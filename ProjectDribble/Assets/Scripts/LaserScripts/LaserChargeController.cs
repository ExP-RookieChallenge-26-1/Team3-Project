using ScriptableObjects;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class LaserChargeController : MonoBehaviour
{
    [SerializeField] private LaserData _data;
    
    [SerializeField] private LaserChargePreview laserChargePreview;
    [SerializeField] private Transform laserStartPoint;
    [SerializeField] private ChargeZone chargeZone;
    [FormerlySerializedAs("guageManger")] [SerializeField] private GaugeManager guageManager;
    [SerializeField] private LaserShoot laserShoot;
    
    [SerializeField] private float chargeTime= 3f;
    private bool isDribbling = false;
    private float chargeTimer = 0f;
    private int chargeCount = 0;
    private int maxChargeCount = 3;
    private void OnEnable()
    {
        chargeZone.OnDribblingChanged += HandleDribblingChanged;
    }

    private void OnDisable()
    {
        chargeZone.OnDribblingChanged -= HandleDribblingChanged;
    }
    // 차징존에 공이 들어오고 나갈 때 호출되는 이벤트 핸들러
    private void HandleDribblingChanged(bool value)
    {
        isDribbling = value;
        
        if (isDribbling == false)
        {
            if(!Mouse.current.leftButton.isPressed)
            {
                TryFireChargedLaser();
            }
            else
            {
                
                ReturnGauge();
            }
            
            Reset();
        }
        
    }
    
    void Update()
    {
        UpdateChargePreview();

        if (CheckTimer())
        {
            TryIncreaseChargeLevel();
        }
    }
    
    private void UpdateChargePreview()
    {
        if (laserChargePreview == null)
            return;

        if (!isDribbling)
        {
            laserChargePreview.Hide();
            return;
        }

        float width = _data.baseWidth + _data.widthPerCharge * chargeCount;
        float range = _data.range;

        laserChargePreview.Show(
            laserStartPoint.position,
            width,
            range
        );
    }
    
    // 드리블 타임 체크
    // 기준 시간 이상이면 true 반환
    private bool CheckTimer()
    {
        if (isDribbling)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= chargeTime)
            {
                return true;
            }
        }
        return false;
    }

    // 차징 레벨 올리기 시도
    private void TryIncreaseChargeLevel()
    {
        
        if (guageManager.FilledGaugeSegments > 0)
        {
            if (chargeCount < maxChargeCount)
            {
                IncreaseChargeLevel();
            }
            
            
        }
    }
    private void IncreaseChargeLevel()
    {
        guageManager.TryReduceGaugeLevel();
        chargeTimer = 0f;
        chargeCount++;
    }

    private void TryFireChargedLaser()
    {
        if (chargeCount > 0)
        {
            laserShoot.ShootLaser(chargeCount);
            chargeCount = 0;
        }
    }

    private void ReturnGauge()
    {
        int returnAmount = chargeCount * _data.gaugePerSegment;

        for (int i = 0; i < returnAmount; i++)
        {
            guageManager.AddGauge();
        }
    }
    
    
    private void Reset()
    {
        chargeCount = 0;
        chargeTimer = 0;
    }
}
