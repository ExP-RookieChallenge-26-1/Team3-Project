using UnityEngine;


public class BallController : MonoBehaviour
{

    [SerializeField] private float ballRadius = 0.7f;

    public float actualRadius;
    public Vector2 direction;

    public LayerMask collisionMask; // 벽과 패들 레이어를 선택하세요
    
    private Transform tr;

    private CircleCollider2D cc;
    private BallMovement BallMovement;
    private BallCollisionHandler BallCollisionHandler;
    private BallSpeedController BallSpeedController;
    
    public int ballDamage = 1;

    void Start()
    {
        BallMovement = GetComponent<BallMovement>();
        BallCollisionHandler = GetComponent<BallCollisionHandler>();
        BallSpeedController = GetComponent<BallSpeedController>();
        tr = GetComponent<Transform>();
        tr.localScale = new Vector3(ballRadius,ballRadius,ballRadius);
        cc = GetComponent<CircleCollider2D>();
        actualRadius = cc.radius * ballRadius*1.25f;
        direction = new Vector2(0.5f, 1f).normalized;
    }

    // Update is called once per frame
    void Update()
    {
        // 1. 이동하기 전에 먼저 벽이나 패들이 있는지 인식하고 방향을 바꿉니다.

        // 2. 결정된 방향(direction)으로 순수하게 직선 이동만 합니다.
        direction = BallMovement.MoveBall(direction, actualRadius, collisionMask);

    }

    public void SetBallDirection(float x,float y)
    {
        direction = new Vector2(x, y).normalized;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
    }
    
    
}
