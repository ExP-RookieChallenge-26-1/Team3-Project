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
        if (ballController != null && ballController.IsCaptured)
        {
            return new BallCollisionResult(incomingDirection.normalized, false);
        }

        Debug.Log("Collision: " + hit.collider.name);

        SoundManager.Instance.Play2D(SoundId.BallBounce);

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

            Debug.Log($"[BallDamage] Apply Damage: {damageToApply}, CurrentDamage Before Loss: {ballPowerController.CurrentDamage()}");

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

        Vector2 defaultDirection = Vector2.Reflect(incomingDirection, hit.normal).normalized;

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

    private bool IsFloorName(string objectName)
    {
        return objectName.Contains("ground")
               || objectName.Contains("floor")
               || objectName.Contains("bottom")
               || objectName.Contains("wall_down");
    }
}
