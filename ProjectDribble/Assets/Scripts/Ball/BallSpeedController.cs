using UnityEngine;

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
    [SerializeField] private float currentSpeed;
    private float lastBlockSpeedSlowTime = -999f;

    public float CurrentSpeed => currentSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        baseSpeed = data.baseSpeed;
        maxSpeed = data.maxSpeed;
        PaddleSpeedIncrease = data.outerPaddleSpeedIncrease;
        BlockSpeedDecrease = data.BlockSpeedDecrease;

        BallMovement = GetComponent<BallMovement>();
        BallController = GetComponent<BallController>();
        BallPowerController = GetComponent<BallPowerController>();
        tr = GetComponent<Transform>();
        cc = GetComponent<CircleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
       if (BallMovement.speed < baseSpeed)
       {
           BallMovement.SetBallSpeed(baseSpeed);
       }
       if (BallMovement.speed >= maxSpeed)
       {
           BallMovement.SetBallSpeed(maxSpeed);
       }
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
        if (BallMovement.speed < baseSpeed)
        {
            BallMovement.SetBallSpeed(baseSpeed);
        }

        if (BallMovement.speed > maxSpeed)
        {
            BallMovement.SetBallSpeed(maxSpeed);
        }
    }

    public float GetSpeedRatio()
    {
        return Mathf.InverseLerp(baseSpeed, maxSpeed, currentSpeed);
    }
}
