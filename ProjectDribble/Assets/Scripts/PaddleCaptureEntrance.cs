using UnityEngine;

public class PaddleCaptureEntrance : MonoBehaviour
{
    [SerializeField] private PaddleMovement paddle;
    [SerializeField] private Transform captureAnchor;

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
            return;

        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null || !ball.IsFree)
            return;

        Debug.Log("[PaddleCapture] Capture entrance triggered");
        ball.Capture(captureAnchor);
    }
}
