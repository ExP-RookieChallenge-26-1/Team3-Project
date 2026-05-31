using UnityEngine;

public enum CaptureEntranceSide
{
    Left,
    Right
}

public class PaddleCaptureEntrance : MonoBehaviour
{
    [SerializeField] private BallData data;
    [SerializeField] private PaddleMovement paddle;
    [SerializeField] private Transform captureAnchor;
    [SerializeField] private CaptureEntranceSide side;

    private void Awake()
    {
        if (paddle == null)
            paddle = GetComponentInParent<PaddleMovement>();

        if (captureAnchor == null && paddle != null)
            captureAnchor = paddle.transform;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCapture(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCapture(other);
    }

    private void TryCapture(Collider2D other)
    {
        if (paddle == null || !paddle.IsPaddleActive)
        {
            Log("[PaddleCapture] Failed: Paddle is not active");
            return;
        }

        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null)
            return;

        if (data == null)
            data = ball.data;

        if (!ball.IsFree)
        {
            Log("[PaddleCapture] Failed: Ball is not free");
            return;
        }

        if (!IsMovingIntoEntrance(ball.direction))
        {
            Log("[PaddleCapture] Failed: Wrong entrance direction");
            return;
        }

        if (!ball.CanCaptureNow)
        {
            Log("[PaddleCapture] Failed: Capture cooldown");
            return;
        }

        Log("[PaddleCapture] Capture entrance triggered");
        Log("[PaddleCapture] Success");
        ball.Capture(captureAnchor, paddle);
    }

    private bool IsMovingIntoEntrance(Vector2 ballDirection)
    {
        float minEntranceDirectionX = data != null
            ? data.MinEntranceDirectionX
            : 0.1f;

        if (side == CaptureEntranceSide.Left)
            return ballDirection.x > minEntranceDirectionX;

        if (side == CaptureEntranceSide.Right)
            return ballDirection.x < -minEntranceDirectionX;

        return false;
    }

    private void Log(string message)
    {
        bool shouldLog = data == null || data.DebugCaptureLog;

        if (shouldLog)
            Debug.Log(message);
    }
}
