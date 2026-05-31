using System;
using UnityEngine;

public enum BallState
{
    Free,
    Captured
}

public class BallController : MonoBehaviour
{
    [SerializeField] private float ballRadius = 0.7f;
    [SerializeField] private Transform captureAnchor;
    [SerializeField] private Vector2 capturedLocalOffset;

    public float actualRadius;
    public Vector2 direction;

    public LayerMask collisionMask;
    public BallData data;

    private Transform tr;
    private CircleCollider2D cc;
    private BallMovement BallMovement;
    private BallCollisionHandler BallCollisionHandler;
    private BallSpeedController BallSpeedController;

    public int ballDamage = 1;
    public BallState CurrentState { get; private set; } = BallState.Free;
    public bool IsFree => CurrentState == BallState.Free;
    public bool IsCaptured => CurrentState == BallState.Captured;

    public event Action OnCaptured;
    public event Action OnReleased;

    void Start()
    {
        BallMovement = GetComponent<BallMovement>();
        BallCollisionHandler = GetComponent<BallCollisionHandler>();
        BallSpeedController = GetComponent<BallSpeedController>();

        if (data == null && BallMovement != null)
            data = BallMovement.data;
        if (data == null && BallSpeedController != null)
            data = BallSpeedController.data;

        tr = GetComponent<Transform>();
        tr.localScale = new Vector3(ballRadius, ballRadius, ballRadius);
        cc = GetComponent<CircleCollider2D>();
        actualRadius = cc.radius * ballRadius * 1.25f;
        direction = new Vector2(0f, -1f).normalized;
    }

    void Update()
    {
        if (IsCaptured)
        {
            FollowCaptureAnchor();
            return;
        }

        direction = BallMovement.MoveBall(direction, actualRadius, collisionMask);
    }

    public void Capture(Transform anchor)
    {
        if (anchor != null)
            captureAnchor = anchor;

        if (captureAnchor == null)
            return;

        CurrentState = BallState.Captured;
        FollowCaptureAnchor();
        Debug.Log("[BallState] Captured");
        OnCaptured?.Invoke();
    }

    public void Release(Vector2 releaseDirection)
    {
        if (releaseDirection.sqrMagnitude > 0.0001f)
            direction = CorrectDirection(releaseDirection);

        captureAnchor = null;
        CurrentState = BallState.Free;
        Debug.Log("[BallState] Released");
        OnReleased?.Invoke();
    }

    public void Shoot(Vector2 shootDirection, float speed)
    {
        Release(shootDirection);

        if (BallMovement != null)
            BallMovement.SetBallSpeed(speed);
    }

    public void SetBallDirection(float x, float y)
    {
        direction = CorrectDirection(new Vector2(x, y));
    }

    public Vector2 CorrectDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return Vector2.down;

        Vector2 beforeDir = dir.normalized;
        Vector2 corrected = beforeDir;

        if (data == null)
            return corrected;

        if (Mathf.Abs(corrected.x) < data.MinDirectionX)
        {
            float signX = corrected.x == 0f
                ? (UnityEngine.Random.value < 0.5f ? -1f : 1f)
                : Mathf.Sign(corrected.x);

            corrected.x = signX * data.MinDirectionX;
        }

        if (Mathf.Abs(corrected.y) < data.MinDirectionY)
        {
            float signY = corrected.y == 0f
                ? (UnityEngine.Random.value < 0.5f ? -1f : 1f)
                : Mathf.Sign(corrected.y);

            corrected.y = signY * data.MinDirectionY;
        }

        corrected = corrected.normalized;

        if ((corrected - beforeDir).sqrMagnitude > 0.0001f)
            Debug.Log($"[BallDirection] Corrected direction from {beforeDir} to {corrected}");

        return corrected;
    }

    private void FollowCaptureAnchor()
    {
        if (captureAnchor == null)
            return;

        transform.position = captureAnchor.position + (Vector3)capturedLocalOffset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
    }
}
