using DefaultNamespace;
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
        return HandleCollision(hit, incomingDirection, hit.normal);
    }

    public BallCollisionResult HandleCollision(
        RaycastHit2D hit,
        Vector2 incomingDirection,
        Vector2 collisionNormal
    )
    {
        if (ballController != null && ballController.IsCaptured)
        {
            return new BallCollisionResult(incomingDirection.normalized, false);
        }

        Debug.Log("Collision: " + hit.collider.name);

        
        
        // 사운드 재생 - 공 충돌음 재생
        SoundManager.Instance.Play2D(GetCollisionSoundId(hit.collider), true);

        PaddleBallReflector paddleReflector =
            hit.collider.GetComponentInParent<PaddleBallReflector>();

        if (paddleReflector != null)
        {
            PaddleController hitPaddle =
                paddleReflector.GetComponentInParent<PaddleController>();

            if (
                ballController != null &&
                ballController.TryStartPendingCaptureFromPaddleHit(
                    hitPaddle,
                    paddleReflector.ReflectUp
                )
            )
            {
                return new BallCollisionResult(ballController.direction.normalized, false);
            }
        }

        bool isFloorHit = IsFloorCollider(hit.collider);
        IBallSpeedModifier speedModifier =
            hit.collider.GetComponentInParent<IBallSpeedModifier>();

        if (isFloorHit)
        {
            ballSpeedController.ResetToBaseSpeed();
        }

        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            float damageToApply = ballPowerController.CurrentDamage();

           
            //Debug.Log($"[BallDamage] Apply Damage: {damageToApply}, CurrentDamage Before Loss: {ballPowerController.CurrentDamage()}");

            bool destroyed = damageable.TakeDamage(damageToApply);

            ballPowerController.ApplyBlockDamageLoss();

            if (!isFloorHit && speedModifier != null)
            {
                speedModifier.ModifySpeed(ballSpeedController);
            }

            if (destroyed)
            {
                return CreateResult(incomingDirection, true);
            }
        }
        else if (!isFloorHit && speedModifier != null)
        {
            speedModifier.ModifySpeed(ballSpeedController);
        }

        IBallHitReceiver receiver = hit.collider.GetComponentInParent<IBallHitReceiver>();

        if (receiver != null)
        {
            receiver.OnBallHit(ballController);
        }

        IBallReflector reflector = hit.collider.GetComponentInParent<IBallReflector>();

        if (reflector != null)
        {
            Vector2 reflectedDirection = reflector.GetReflectDirection(
                ballController,
                hit,
                incomingDirection
            );

            return CreateResult(reflectedDirection, true);
        }

        Vector2 normal = collisionNormal.sqrMagnitude > 0.0001f
            ? collisionNormal.normalized
            : hit.normal.normalized;

        Vector2 defaultDirection = Vector2.Reflect(incomingDirection, normal).normalized;

        return CreateResult(defaultDirection, true);
    }

    private BallCollisionResult CreateResult(Vector2 direction, bool shouldMoveRemainingDistance)
    {
        Vector2 correctedDirection = ballController.CorrectDirection(direction);
        return new BallCollisionResult(correctedDirection, shouldMoveRemainingDistance);
    }

    private bool IsFloorCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        string objectName = collider.name.ToLowerInvariant();
        string parentName = collider.transform.parent != null
            ? collider.transform.parent.name.ToLowerInvariant()
            : string.Empty;

        return IsFloorName(objectName) || IsFloorName(parentName);
    }

    private SoundId GetCollisionSoundId(Collider2D collider)
    {
        if (collider == null)
            return SoundId.BallBounce;

        if (collider.GetComponentInParent<PaddleBallReflector>() != null)
            return SoundId.PaddleBounce;

        BlockCell block = collider.GetComponentInParent<BlockCell>();
        if (block != null)
            return block.IsFixed ? SoundId.FixedBlockHit : SoundId.BlockHit;

        if (collider.GetComponentInParent<CeilingBrick>() != null)
            return SoundId.BlockHit;

        if (collider.GetComponentInParent<WallBallHitReceiver>() != null || IsFloorCollider(collider))
            return SoundId.WallBounce;

        return SoundId.BallBounce;
    }
    

    private bool IsFloorName(string objectName)
    {
        return objectName.Contains("ground")
               || objectName.Contains("floor")
               || objectName.Contains("bottom");
    }
}
