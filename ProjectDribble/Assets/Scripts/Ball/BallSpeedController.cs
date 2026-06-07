using System;
using System.Collections;
using UnityEngine;

public enum BallSpeedState
{
    Normal,
    LaserBoost,
    Weakened
}

public class BallSpeedController : MonoBehaviour
{
    private Transform tr;
    private CircleCollider2D cc;

    private BallMovement BallMovement;
    private BallController BallController;
    private BallPowerController BallPowerController;
    public BallData data;

    float moveDistance;

    float baseSpeed;
    float maxSpeed;
    float PaddleSpeedIncrease;
    float BlockSpeedDecrease;

    private float normalMaxSpeed;
    private float laserMinBoostSpeed;
    private float laserMaxSpeed;
    private float laserBoostAmount;
    private float laserReturnThreshold;
    private float weakenedSpeed;
    private float weakenedDuration;
    [SerializeField] private float currentSpeed;

    private BallSpeedState currentSpeedState = BallSpeedState.Normal;
    private Coroutine weakenedCoroutine;
    private float lastBlockSpeedSlowTime = -999f;

    public float CurrentSpeed => currentSpeed;
    public BallSpeedState CurrentSpeedState => currentSpeedState;
    public float CurrentMaxSpeed => GetCurrentMaxSpeed();
    public bool IsLaserBoosted => currentSpeedState == BallSpeedState.LaserBoost;
    public bool IsWeakened => currentSpeedState == BallSpeedState.Weakened;
    public float SpeedRatio01 => GetSpeedRatio();
    public event Action<BallSpeedState> OnSpeedStateChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadSpeedSettingsFromData();

        BallMovement = GetComponent<BallMovement>();
        BallController = GetComponent<BallController>();
        BallPowerController = GetComponent<BallPowerController>();
        tr = GetComponent<Transform>();
        cc = GetComponent<CircleCollider2D>();
        currentSpeed = BallMovement.speed;
        ClampSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        ClampSpeed();
        currentSpeed = BallMovement.speed;
        moveDistance = BallMovement.moveDistance;
    }

    /*
    public void AdjustSpeed(Vector2 direction, float actualRadius, LayerMask collisionMask)
    {
        // CircleCast로 앞길에 장애물이 있는지 미리 레이저를 쏴봅니다.
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, actualRadius, direction, moveDistance, collisionMask);

        // 장애물이 감지되었다면 방향을 꺾어줍니다.
        if (hit.collider != null && hit.collider.name == "paddle_down")
        {
            BallMovement.AddBallSpeed(PaddleSpeedIncrease);
        }
        if (hit.collider != null && hit.collider.name == "wall_up")
        {
            BallMovement.AddBallSpeed(-5.0f);
        }
    }
*/
    
    public void HandleCollisionSpeed(Collider2D collider)
    {
        if (collider == null)
            return;

        if (collider.name == "paddle_down")
        {
            //BallMovement.AddBallSpeed(PaddleSpeedIncrease);
        }

        if (collider.name == "wall_up")
        {
            BallMovement.AddBallSpeed(-5.0f);
        }

        ClampSpeed();
    }

    public void ResetToBaseSpeed()
    {
        SetSpeedState(BallSpeedState.Normal);
        BallMovement.SetBallSpeed(baseSpeed);
        currentSpeed = BallMovement.speed;
        BallPowerController?.ResetToBaseDamage();
        Debug.Log($"[BallState] Reset to Base. Speed: {BallMovement.speed}, Damage: {BallPowerController?.CurrentDamage() ?? 0f}");
    }

    public void TryApplyBlockSpeedSlow(float fallbackSlowAmount = 0f)
    {
        float cooldown = data != null ? data.BlockSpeedSlowCooldown : 0f;
        bool isCooldownReady = Time.time >= lastBlockSpeedSlowTime + cooldown;
        float speedBeforeSlow = BallMovement.speed;

        //Debug.Log($"[BallSpeed] Try Slow. Cooldown Ready: {isCooldownReady}, CurrentSpeed: {speedBeforeSlow}");

        if (!isCooldownReady)
            return;

        float slowAmount = data != null
            ? data.GetBlockSlowAmount(speedBeforeSlow)
            : fallbackSlowAmount;

        BallMovement.AddBallSpeed(-slowAmount);
        ClampSpeed();
        currentSpeed = BallMovement.speed;

        lastBlockSpeedSlowTime = Time.time;

        //Debug.Log($"[BallSpeed] Block Slow Applied. SlowAmount: {slowAmount}, CurrentSpeed After Slow: {BallMovement.speed}");
    }
    
    public void ApplyBlockSlow(float fallbackSlowAmount = 0f)
    {
        TryApplyBlockSpeedSlow(fallbackSlowAmount);
    }
    
    public void AddSpeed(float amount)
    {
        if (IsWeakened && amount > 0f)
        {
            BallMovement.SetBallSpeed(weakenedSpeed);
            currentSpeed = BallMovement.speed;
            return;
        }

        BallMovement.AddBallSpeed(amount);
        ClampSpeed();
        currentSpeed = BallMovement.speed;
    }

    public void AddSpeedByPaddle(float fallbackSpeedBonus)
    {
        float speedBonus = fallbackSpeedBonus;

        AddSpeed(speedBonus);
        BallPowerController?.AddPaddleDamage();
    }

    public void AddSpeedByDribble()
    {
        float speedBonus = data != null ? data.DribbleSpeedBonus : 0f;

        AddSpeed(speedBonus);
        BallPowerController?.AddDribbleDamage();
    }
    

    private void ClampSpeed()
    {
        if (IsWeakened)
        {
            BallMovement.SetBallSpeed(weakenedSpeed);
            return;
        }

        if (IsLaserBoosted && BallMovement.speed <= laserReturnThreshold)
            SetSpeedState(BallSpeedState.Normal);

        float minSpeed = baseSpeed;
        float maxAllowedSpeed = GetCurrentMaxSpeed();

        if (BallMovement.speed < minSpeed)
        {
            BallMovement.SetBallSpeed(minSpeed);
        }

        if (BallMovement.speed > maxAllowedSpeed)
        {
            BallMovement.SetBallSpeed(maxAllowedSpeed);
        }
    }

    public float GetSpeedRatio()
    {
        switch (currentSpeedState)
        {
            case BallSpeedState.LaserBoost:
                return Mathf.InverseLerp(laserReturnThreshold, laserMaxSpeed, currentSpeed);
            case BallSpeedState.Weakened:
                return Mathf.InverseLerp(weakenedSpeed, baseSpeed, currentSpeed);
            default:
                return Mathf.InverseLerp(baseSpeed, normalMaxSpeed, currentSpeed);
        }
    }

    public void ApplyLaserBoost()
    {
        if (IsWeakened)
            return;

        SetSpeedState(BallSpeedState.LaserBoost);
        float boostedSpeed = Mathf.Max(BallMovement.speed, laserMinBoostSpeed);
        boostedSpeed = Mathf.Min(boostedSpeed + laserBoostAmount, laserMaxSpeed);
        BallMovement.SetBallSpeed(boostedSpeed);
        currentSpeed = BallMovement.speed;
    }

    public void ApplyGroundWeakened()
    {
        if (weakenedCoroutine != null)
            StopCoroutine(weakenedCoroutine);

        SetSpeedState(BallSpeedState.Weakened);
        BallMovement.SetBallSpeed(weakenedSpeed);
        currentSpeed = BallMovement.speed;
        BallPowerController?.ResetToBaseDamage();
        weakenedCoroutine = StartCoroutine(RecoverFromWeakened());
    }

    private IEnumerator RecoverFromWeakened()
    {
        yield return new WaitForSeconds(weakenedDuration);

        SetSpeedState(BallSpeedState.Normal);
        BallMovement.SetBallSpeed(baseSpeed);
        currentSpeed = BallMovement.speed;
        weakenedCoroutine = null;
    }

    private float GetCurrentMaxSpeed()
    {
        return IsLaserBoosted ? laserMaxSpeed : normalMaxSpeed;
    }

    private void SetSpeedState(BallSpeedState nextState)
    {
        if (currentSpeedState == nextState)
            return;

        currentSpeedState = nextState;
        OnSpeedStateChanged?.Invoke(currentSpeedState);
    }

    private void LoadSpeedSettingsFromData()
    {
        if (data == null)
        {
            baseSpeed = 30f;
            maxSpeed = 70f;
            normalMaxSpeed = maxSpeed;
            laserMinBoostSpeed = 80f;
            laserMaxSpeed = 100f;
            laserBoostAmount = 20f;
            laserReturnThreshold = 70f;
            weakenedSpeed = 25f;
            weakenedDuration = 0.75f;
            PaddleSpeedIncrease = 0f;
            BlockSpeedDecrease = 0f;
            return;
        }

        baseSpeed = data.baseSpeed;
        normalMaxSpeed = data.NormalMaxSpeed;
        laserMinBoostSpeed = data.LaserMinBoostSpeed;
        laserMaxSpeed = data.LaserMaxSpeed;
        laserBoostAmount = data.LaserBoostAmount;
        laserReturnThreshold = data.LaserReturnThreshold;
        weakenedSpeed = data.WeakenedSpeed;
        weakenedDuration = data.WeakenedDuration;
        PaddleSpeedIncrease = data.outerPaddleSpeedIncrease;
        BlockSpeedDecrease = data.BlockSpeedDecrease;
    }
}
