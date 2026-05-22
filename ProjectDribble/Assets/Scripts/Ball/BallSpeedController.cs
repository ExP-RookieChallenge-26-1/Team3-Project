using UnityEngine;

public class BallSpeedController : MonoBehaviour
{
    private Transform tr;
    private CircleCollider2D cc;

    private BallMovement BallMovement;
    private BallController BallController;
    public BallData data;

    float moveDistance;

    float baseSpeed;
    float maxSpeed;
    float PaddleSpeedIncrease;
    float BlockSpeedDecrease;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        baseSpeed = data.baseSpeed;
        maxSpeed = data.maxSpeed;
        PaddleSpeedIncrease = data.PaddleSpeedIncrease;
        BlockSpeedDecrease = data.BlockSpeedDecrease;

        BallMovement = GetComponent<BallMovement>();
        BallController = GetComponent<BallController>();
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
            BallMovement.AddBallSpeed(PaddleSpeedIncrease);
        }

        if (collider.name == "wall_up")
        {
            BallMovement.AddBallSpeed(-5.0f);
        }

        ClampSpeed();
    }
    
    
    public void AddSpeed(float amount)
    {
        BallMovement.AddBallSpeed(amount);
        ClampSpeed();
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
}