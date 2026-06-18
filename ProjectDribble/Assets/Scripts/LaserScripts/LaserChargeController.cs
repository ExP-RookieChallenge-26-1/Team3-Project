using DefaultNamespace;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private LaserShooter laserShoot;
    [SerializeField] private LaserUnlockState laserUnlockState;
    
    private bool isDribbling = false;
    private float chargeTimer = 0f;
    private int chargeCount = 0;

    private void Awake()
    {
        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();

        if (laserUnlockState == null)
            laserUnlockState = FindAnyObjectByType<LaserUnlockState>();
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

        SoundManager.Instance.StopLoop();
    }
    // 차징존에 공이 들어오고 나갈 때 호출되는 이벤트 핸들러
    private void HandleDribblingChanged(bool value)
    {
        if (ballController != null)
            return;

        if (!IsLaserUnlocked())
        {
            Reset();
            isDribbling = false;
            return;
        }

        if (IsPointerOverUI())
        {
            Reset();
            isDribbling = false;
            return;
        }

        isDribbling = value;
        
        if (isDribbling == false)
        {
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;

            if(!mousePressed)
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
        if (!IsLaserUnlocked())
            return;

        if (IsPointerOverUI())
            return;

        isDribbling = true;
        chargeTimer = 0f;
        
        Debug.Log("[LaserCharge] Start charging by BallCaptured event");
    }

    private void HandleBallReleased()
    {
        if (!IsLaserUnlocked())
        {
            Reset();
            isDribbling = false;
            return;
        }

        if (!isDribbling)
            return;

        isDribbling = false;

        if (IsPointerOverUI())
        {
            Reset();
            return;
        }

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

        if (!isDribbling || !IsLaserUnlocked())
        {
            laserChargePreview.Hide();
            return;
        }

        float width = _data.GetWidthForCharge(chargeCount);

        if (width <= 0f)
        {
            laserChargePreview.Hide();
            return;
        }

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
        if (!IsLaserUnlocked())
            return false;

        if (IsPointerOverUI())
            return false;

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
        if (!IsLaserUnlocked() || guageManager == null)
            return;
        
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
        if (!IsLaserUnlocked() || guageManager == null)
            return;
        
        guageManager.TryReduceGaugeLevel();
        chargeTimer = 0f;
        chargeCount++;
        SoundManager.Instance.SetLoopRatio(GetChargeRatio());
        SoundManager.Instance.PlayLoop(SoundId.LaserCharge, GetChargeRatio());
    }

    private void TryFireChargedLaser()
    {
        if (!IsLaserUnlocked())
            return;

        if (chargeCount > 0)
        {
            float chargeRatio = GetChargeRatio();
            SoundManager.Instance.StopLoop();
            SoundManager.Instance.Play(SoundId.LaserFire, chargeRatio);
            laserShoot.ShootLaser(chargeCount);
            chargeCount = 0;
        }
    }

    private void ReturnGauge()
    {
        SoundManager.Instance.StopLoop();

        if (!IsLaserUnlocked() || guageManager == null)
            return;

        int returnAmount = chargeCount * _data.gaugePerSegment;

        for (int i = 0; i < returnAmount; i++)
        {
            guageManager.AddGauge();
        }
    }
    
    
    private void Reset()
    {
        SoundManager.Instance.StopLoop();
        chargeCount = 0;
        chargeTimer = 0;
    }

    private float GetChargeRatio()
    {
        if (_data == null || _data.maxChargeCount <= 0)
            return 0f;

        return Mathf.Clamp01(chargeCount / (float)_data.maxChargeCount);
    }

    private bool IsLaserUnlocked()
    {
        return laserUnlockState != null && laserUnlockState.IsLaserUnlocked;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.isPressed)
                return EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue());
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
