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
    [SerializeField] private BallController ballController;
    [FormerlySerializedAs("guageManger")] [SerializeField] private GaugeManager guageManager;
    [SerializeField] private LaserShoot laserShoot;
    
    private bool isDribbling = false;
    private float chargeTimer = 0f;
    private int chargeCount = 0;

    private void Awake()
    {
        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();
    }
   
    private void OnEnable()
    {
        if (chargeZone != null)
            chargeZone.OnDribblingChanged += HandleDribblingChanged;

        if (ballController != null)
        {
            ballController.OnCaptured += HandleBallCaptured;
            ballController.OnReleased += HandleBallReleased;
        }
    }

    private void OnDisable()
    {
        if (chargeZone != null)
            chargeZone.OnDribblingChanged -= HandleDribblingChanged;

        if (ballController != null)
        {
            ballController.OnCaptured -= HandleBallCaptured;
            ballController.OnReleased -= HandleBallReleased;
        }
    }
    // 차징존에 공이 들어오고 나갈 때 호출되는 이벤트 핸들러
    private void HandleDribblingChanged(bool value)
    {
        if (ballController != null)
            return;

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

    private void HandleBallCaptured()
    {
        isDribbling = true;
        chargeTimer = 0f;
        Debug.Log("[LaserCharge] Start charging by BallCaptured event");
    }

    private void HandleBallReleased()
    {
        if (!isDribbling)
            return;

        isDribbling = false;
        Debug.Log("[LaserCharge] Stop charging by BallReleased event");
        TryFireChargedLaser();
        Reset();
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

        int previewChargeCount = Mathf.Clamp(
            chargeCount + 1,
            1,
            _data.maxChargeCount
        );

        float width = _data.baseWidth + _data.widthPerCharge * previewChargeCount;
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

            if (chargeTimer >= _data.chargeTime)
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
            if (chargeCount < _data.maxChargeCount)
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
