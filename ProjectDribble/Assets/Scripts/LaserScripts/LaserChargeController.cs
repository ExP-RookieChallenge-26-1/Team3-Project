using UnityEngine;
using UnityEngine.Serialization;
using ScriptableObjectScripts;

public class LaserChargeController : MonoBehaviour
{
    [SerializeField] private LaserGaugeData  laserGaugeData;
    [SerializeField] private ChargeZone chargeZone;
    [FormerlySerializedAs("guageManger")] [SerializeField] private GaugeManager guageManager;
    [SerializeField] private LaserShoot laserShoot;
    
    private bool isDribbling = false;
    private float chargeTimer = 0f;
    private int chargeCount = 0;
    
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
            FireChargedLaser();
            Reset();
        }
        
    }
    
    void Update()
    {
        if (CheckTimer())
        {
            TryIncreaseChargeLevel();
        }
    }
    
    // 드리블 타임 체크
    // 기준 시간 이상이면 true 반환
    private bool CheckTimer()
    {
        if (isDribbling)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= laserGaugeData.chargeTime)
            {
                return true;
            }
        }
        return false;
    }

    // 차징 레벨 올리기 시도
    private void TryIncreaseChargeLevel()
    {
        
        if (guageManager.filledGaugeSegments > 0)
        {
            if (chargeCount < laserGaugeData.maxChargeCount)
            {
                IncreaseChargeLevel();
            }

            
            
        }
    }
    private void IncreaseChargeLevel()
    {
        guageManager.ChangeGaugeLevel(-1);
        chargeTimer = 0f;
        chargeCount++;
    }

    private void FireChargedLaser()
    {
        if (chargeCount > 0)
        {
            laserShoot.ShootLaser(chargeCount);
            chargeCount = 0;
        }
    }

    private void Reset()
    {
        chargeCount = 0;
        chargeTimer = 0;
    }
}
