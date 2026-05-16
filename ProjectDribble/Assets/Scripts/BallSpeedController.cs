using UnityEngine;

public class BallSpeedController : MonoBehaviour
{
    private Transform tr;
    private CircleCollider2D cc;

    private BallMovement BallMovement;
    private BallController BallController;
    [SerializeField] private float PaddleSpeedIncrease = 5f;

    [SerializeField] private float BlockSpeedDecrease = -5f;


    float moveDistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BallMovement = GetComponent<BallMovement>();
        BallController = GetComponent<BallController>();
        tr = GetComponent<Transform>();
        cc = GetComponent<CircleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
       if (BallMovement.speed < BallMovement.baseSpeed)
       {
           BallMovement.SetBallSpeed(BallMovement.baseSpeed);
       }
       if (BallMovement.speed >= BallMovement.maxSpeed)
       {
           BallMovement.SetBallSpeed(BallMovement.maxSpeed);
       }
        moveDistance = BallMovement.moveDistance;
    }

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

}