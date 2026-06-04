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

        Debug.Log(
            $"Collision: {hit.collider.name}, frame={Time.frameCount}, inDir={incomingDirection}, hitNormal={hit.normal}, usedNormal={collisionNormal}, distance={hit.distance:0.0000}"
        );



        float speedRatio = ballSpeedController.GetSpeedRatio();

        SoundPlayOptions options;
        SoundId soundId = GetCollisionSound(hit.collider, speedRatio, out options);
        BlockCell hitBlock = hit.collider.GetComponentInParent<BlockCell>();
        bool shouldDelayBlockBounce = hitBlock != null && !hitBlock.IsFixed;

        if (!shouldDelayBlockBounce)
        {
            SoundManager.Instance.Play(soundId, options);
        }

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
            bool blockWasBroken = hitBlock != null && !hitBlock.IsAlive;

            ballPowerController.ApplyBlockDamageLoss();

            if (!isFloorHit && speedModifier != null)
            {
                speedModifier.ModifySpeed(ballSpeedController);
            }

            if (blockWasBroken)
            {
                return CreateResult(incomingDirection, true);
            }

            if (shouldDelayBlockBounce)
            {
                SoundManager.Instance.Play(soundId, options);
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


    private SoundId GetCollisionSound(
        Collider2D collider,
        float speedRatio,
        out SoundPlayOptions options
    )
    {
        options = SoundPlayOptions.Default;
        options.ratio = speedRatio;

        if (collider == null)
            return SoundId.BallBounce;

        if (collider.GetComponentInParent<PaddleBallReflector>() != null)
        {
            options.volumeScale = 1.1f;
            options.pitchScale = 1.08f;
            return SoundId.BallBounce;
        }

        BlockCell block = collider.GetComponentInParent<BlockCell>();
        if (block != null)
        {
            if (block.IsFixed)
            {
                options.volumeScale = 1.05f;
                options.pitchScale = 1.1f;
            }
            else
            {
                options.volumeScale = 1f;
                options.pitchScale = 1f;
            }

            return SoundId.BallBounce;
        }

        if (collider.GetComponentInParent<CeilingBrick>() != null)
        {
            options.volumeScale = 1.15f;
            options.pitchScale = 1.15f;
            return SoundId.BallBounce;
        }

        if (collider.GetComponentInParent<WallBallHitReceiver>() != null || IsFloorCollider(collider))
        {
            if (IsFloorCollider(collider))
            {
                return SoundId.BallGroundBounce;
            }

            options.volumeScale = 0.5f;
            options.pitchScale = 0.92f;
            return SoundId.BallBounce;
        }

        return SoundId.BallBounce;
    }
    

    private bool IsFloorName(string objectName)
    {
        return objectName.Contains("ground")
               || objectName.Contains("floor")
               || objectName.Contains("bottom");
    }
}
