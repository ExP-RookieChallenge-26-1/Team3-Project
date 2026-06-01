using UnityEngine;

public enum CaptureEntranceSide
{
    Left,
    Right,
    Up
}

public class PaddleCaptureEntrance : MonoBehaviour
{
    [SerializeField] private BallData data;
    [SerializeField] private PaddleController paddle;
    [SerializeField] private Transform captureAnchor;
    [SerializeField] private CaptureEntranceSide side;
    [SerializeField] private bool debugPaddleActiveState;

    private void Awake()
    {
        if (paddle == null)
            paddle = GetComponentInParent<PaddleController>();

        if (captureAnchor == null && paddle != null)
            captureAnchor = paddle.transform;

        Collider2D trigger = GetComponent<Collider2D>();

        if (trigger != null)
        {
            trigger.enabled = true;
            trigger.isTrigger = true;
        }
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
        if (paddle == null)
        {
            Log("[PaddleCapture] Failed: Paddle is missing");
            return;
        }

        if (!paddle.IsPaddleActive)
        {
            Log("[PaddleCapture] Failed: Paddle is not active");
            return;
        }

        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null)
            return;

        LogBallPaddleEnter();

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
            if (ball.IsInCapturedReleaseRecaptureDelay || ball.IsInReleaseRecaptureDelay)
                Log("[CaptureBlocked] reason=releaseRecaptureDelay");

            Log("[PaddleCapture] Failed: Capture cooldown");
            return;
        }

        Log("[PaddleCapture] Capture entrance triggered");
        Log("[PaddleCapture] Success");
        ball.Capture(captureAnchor, paddle);
    }

    private void LogBallPaddleEnter()
    {
        if (!debugPaddleActiveState)
            return;

        Collider2D trigger = GetComponent<Collider2D>();
        bool isPaddleActive = paddle != null && paddle.IsPaddleActive;
        string paddleName = paddle != null ? paddle.name : "None";
        bool isTrigger = trigger != null && trigger.isTrigger;

        Debug.Log(
            $"[BallPaddleEnter] paddle={paddleName}, isPaddleActive={isPaddleActive}, trigger={isTrigger}"
        );
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
        
        if (side == CaptureEntranceSide.Up)
            return ballDirection.y < -minEntranceDirectionX;

        return false;
    }

    private void Log(string message)
    {
        bool shouldLog = data == null || data.DebugCaptureLog;

        if (shouldLog)
            Debug.Log(message);
    }
}
