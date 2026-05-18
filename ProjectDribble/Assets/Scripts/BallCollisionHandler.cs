using UnityEngine;

public class BallCollisionHandler : MonoBehaviour
{
    private Transform tr;
    private CircleCollider2D cc;
    private BallMovement BallMovement;
    private BallSpeedController BallSpeedController;


    float moveDistance;

    void Start()
    {
        BallMovement = GetComponent<BallMovement>();
        BallSpeedController = GetComponent<BallSpeedController>();
        tr = GetComponent<Transform>();
        cc = GetComponent<CircleCollider2D>();
        moveDistance = BallMovement.moveDistance;
    }
    

// 현재 밑에 코드는 사용되지는 않음
    public Vector2 CheckAndHandleCollision(Vector2 direction, float actualRadius, LayerMask collisionMask)
    {
        // CircleCast로 앞길에 장애물이 있는지 미리 레이저를 쏴봅니다.
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, actualRadius, direction, moveDistance, collisionMask);

        // 장애물이 감지되었다면 방향을 꺾어줍니다.
        if (hit.collider != null)
        {
            // 충돌 대상(패들, 벽 등)에 따른 반사 방향 업데이트 함수 호출 (기존 코드 사용)
            //direction = UpdateDirection(hit, _outsideMaxBounceAngle, _insideMaxBounceAngle, direction);

            return direction;

            // 💡 팁: 충돌했을 때 공이 벽 안으로 파고드는 것을 방지하고 싶다면, 
            // UpdateDirection(hit) 직후에 라인 레이저의 Reset이나 이펙트 처리를 여기서 진행하면 좋습니다.
        }

        //ResolveOverlap(actualRadius, collisionMask);
        return direction;
    }

}
