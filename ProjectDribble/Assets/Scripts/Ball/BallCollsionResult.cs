using UnityEngine;

public struct BallCollisionResult
{
    public Vector2 nextDirection;
    public bool shouldMoveRemainingDistance;

    public BallCollisionResult(Vector2 nextDirection, bool shouldMoveRemainingDistance)
    {
        this.nextDirection = nextDirection;
        this.shouldMoveRemainingDistance = shouldMoveRemainingDistance;
    }
}
