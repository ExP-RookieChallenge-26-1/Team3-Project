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
        TryEnterCaptureZone(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryEnterCaptureZone(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null)
            return;

        ball.ExitCaptureZone(paddle);
    }

    private void TryEnterCaptureZone(Collider2D other)
    {
        if (paddle == null)
        {
            Log("[CaptureZone] Failed: Paddle is missing");
            return;
        }

        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null)
            return;

        LogBallPaddleEnter();

        if (data == null)
            data = ball.data;

        ball.EnterCaptureZone(captureAnchor, paddle, GetBounceUp());
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

    private bool GetBounceUp()
    {
        return side != CaptureEntranceSide.Up;
    }

    private void Log(string message)
    {
        bool shouldLog = data == null || data.DebugCaptureLog;

        if (shouldLog)
            Debug.Log(message);
    }
}
