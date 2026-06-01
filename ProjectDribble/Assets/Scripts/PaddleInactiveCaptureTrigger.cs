using UnityEngine;

public class PaddleInactiveCaptureTrigger : MonoBehaviour
{
    [SerializeField] private PaddleMovement paddle;
    [SerializeField] private Transform captureAnchor;
    [SerializeField] private bool bounceUp = true;
    [SerializeField] private bool debugPaddleActiveState;

    private void Awake()
    {
        if (paddle == null)
            paddle = GetComponentInParent<PaddleMovement>();

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
        if (paddle == null || paddle.IsPaddleActive)
            return;

        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null || !ball.IsFree)
            return;

        if (!ball.CanCaptureFromInactivePaddle(paddle))
        {
            if (debugPaddleActiveState)
                Debug.Log("[CaptureBlocked] reason=inactiveTriggerCaptureNotAllowed");

            return;
        }

        if (debugPaddleActiveState)
        {
            Debug.Log(
                $"[InactivePaddleTrigger] Capture paddle={paddle.name}, bounceUp={bounceUp}"
            );
        }

        ball.CaptureFromInactiveTrigger(captureAnchor, paddle, bounceUp);
    }
}
