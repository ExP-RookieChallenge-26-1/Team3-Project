using Interfaces;
using UnityEngine;

public class BallCollisionHandler : MonoBehaviour
{
    

    private BallController ballController;
    private BallSpeedController ballSpeedController;
    private BallPowerController ballPowerController;


    private void Start()
    {
        ballController = GetComponent<BallController>();
        ballSpeedController = GetComponent<BallSpeedController>();
        ballPowerController = GetComponent<BallPowerController>();
    }

    public BallCollisionResult HandleCollision(RaycastHit2D hit, Vector2 incomingDirection)
    {
        Debug.Log("충돌함: " + hit.collider.name);

        // 1. 충돌 시점에 속도 변화 적용
        IBallSpeedModifier speedModifier =
            hit.collider.GetComponentInParent<IBallSpeedModifier>();

        if (speedModifier != null)
        {
            speedModifier.ModifySpeed(ballSpeedController);
        }
        
        
        
        // 2. 데미지 처리
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            bool destroyed = damageable.TakeDamage(ballPowerController.CurrentDamage());

            // 죽었으면 반사하지 않고 기존 방향 유지
            if (destroyed)
            {
                return new BallCollisionResult(incomingDirection.normalized, true);
            }
        }

        // 3. 공에 맞았을 때의 일반 반응
        IBallHitReceiver receiver = hit.collider.GetComponentInParent<IBallHitReceiver>();

        if (receiver != null)
        {
            receiver.OnBallHit(ballController);
        }

        // 4. 특수 반사 처리
        IBallReflector reflector = hit.collider.GetComponentInParent<IBallReflector>();

        if (reflector != null)
        {
            Vector2 reflectedDirection = reflector.GetReflectDirection(
                ballController,
                hit,
                incomingDirection
            );

            return new BallCollisionResult(reflectedDirection.normalized, true);
        }

        // 5. 기본 반사
        Vector2 defaultDirection = Vector2.Reflect(incomingDirection, hit.normal).normalized;

        return new BallCollisionResult(defaultDirection, true);
    }
}