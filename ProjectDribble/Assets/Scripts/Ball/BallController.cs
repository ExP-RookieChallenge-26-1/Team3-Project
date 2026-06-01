using System;
using DefaultNamespace;
using UnityEngine;

public enum BallState
{
    Free,
    Captured
}

[RequireComponent(typeof(BallCapturedDribbleController))]
public class BallController : MonoBehaviour
{
    [SerializeField] private float ballRadius = 0.7f;
    [SerializeField] private Transform captureAnchor;
    [SerializeField] private Vector2 capturedLocalOffset;
    [SerializeField] private Transform capturedTopBound;
    [SerializeField] private Transform capturedBottomBound;
    [SerializeField] private PaddleSpeedModifier topCapturedPaddleModifier;
    [SerializeField] private PaddleSpeedModifier bottomCapturedPaddleModifier;
    
    [Header("Captured Dribble")]
    [SerializeField] private float dribbleVerticalCorrectionDelay = 0.15f;
    [SerializeField] private float dribbleVerticalCorrectionDuration = 0.4f;
    [SerializeField] private float dribbleMaxInitialX = 0.6f;
    [SerializeField] private float dribbleMovingXInfluence = 0.15f;
    [SerializeField] private float dribbleStillVelocityThreshold = 0.05f;
    [SerializeField] private float dribbleSpeedIncreaseCooldown = 0.25f;
    [SerializeField] private float captureReleaseGraceTime = 0.08f;
    [SerializeField] private float capturedReleaseRecaptureDelay = 0.15f;
    [SerializeField] private bool debugDribbleBounce;
    [SerializeField] private bool debugDribbleState;
    [SerializeField] private bool debugCaptureRelease;

    public float actualRadius;
    public Vector2 direction;

    public LayerMask collisionMask;
    public BallData data;

    private Transform tr;
    private CircleCollider2D cc;
    private BallMovement BallMovement;
    private BallCollisionHandler BallCollisionHandler;
    private BallSpeedController BallSpeedController;
    private BallCapturedDribbleController capturedDribbleController;
    private float lastCaptureTime = -999f;
    private float lastReleaseTime = -999f;
    private float lastCaptureReleaseTime = -999f;

    public int ballDamage = 1;
    public BallState CurrentState { get; private set; } = BallState.Free;
    public bool IsFree => CurrentState == BallState.Free;
    public bool IsCaptured => CurrentState == BallState.Captured;
    public bool IsInCaptureCooldown => Time.time < lastCaptureTime + CaptureCooldown;
    public bool IsInReleaseRecaptureDelay => Time.time < lastReleaseTime + ReleaseRecaptureDelay;
    public bool IsInCapturedReleaseRecaptureDelay =>
        Time.time < lastCaptureReleaseTime + CapturedReleaseRecaptureDelay;
    public bool CanCaptureNow =>
        IsFree &&
        !IsInCaptureCooldown &&
        !IsInReleaseRecaptureDelay &&
        !IsInCapturedReleaseRecaptureDelay;

    public event Action OnCaptured;
    public event Action OnReleased;

    void Start()
    {
        BallMovement = GetComponent<BallMovement>();
        BallCollisionHandler = GetComponent<BallCollisionHandler>();
        BallSpeedController = GetComponent<BallSpeedController>();
        EnsureCapturedDribbleController();

        if (data == null && BallMovement != null)
            data = BallMovement.data;
        if (data == null && BallSpeedController != null)
            data = BallSpeedController.data;

        tr = GetComponent<Transform>();
        tr.localScale = new Vector3(ballRadius, ballRadius, ballRadius);
        cc = GetComponent<CircleCollider2D>();
        //actualRadius = cc.radius * ballRadius * 1.25f;
        actualRadius = cc.radius * 1.25f;
        direction = new Vector2(0f, -1f).normalized;
    }

    void Update()
    {
        EnsureCapturedDribbleController();
        capturedDribbleController.RefreshInactiveCaptureSuppression();

        if (IsCaptured)
        {
            capturedDribbleController.Tick();
            return;
        }

        direction = BallMovement.MoveBall(direction, actualRadius, collisionMask);
    }

    public void Capture(Transform anchor)
    {
        Capture(anchor, null);
    }

    public void Capture(Transform anchor, PaddleController paddle)
    {
        EnsureCapturedDribbleController();

        if (capturedDribbleController.IsInactivePaddleCaptureBlocked(paddle))
            return;

        if (!capturedDribbleController.CanStartCapturedDribble())
            return;

        if (anchor != null)
            captureAnchor = anchor;

        if (captureAnchor == null)
            return;

        bool bounceUp = Mathf.Abs(direction.y) < 0.01f || direction.y > 0f;
        string source = paddle != null && paddle.IsPaddleActive
            ? "Entrance"
            : "InactivePaddle";

        StartCapturedDribble(paddle, bounceUp, source);
    }

    public void Capture(
        Transform anchor,
        PaddleController paddle,
        bool bounceUp,
        string source
    )
    {
        EnsureCapturedDribbleController();

        if (capturedDribbleController.IsInactivePaddleCaptureBlocked(paddle))
            return;

        if (!capturedDribbleController.CanStartCapturedDribble())
            return;

        if (anchor != null)
            captureAnchor = anchor;

        if (captureAnchor == null)
            return;

        StartCapturedDribble(paddle, bounceUp, source);
    }

    public void CaptureFromInactiveTrigger(
        Transform anchor,
        PaddleController paddle,
        bool bounceUp
    )
    {
        EnsureCapturedDribbleController();

        if (paddle == null || paddle.IsPaddleActive)
            return;

        if (!CanCaptureFromInactivePaddle(paddle))
            return;

        if (anchor != null)
            captureAnchor = anchor;

        if (captureAnchor == null)
            return;

        StartCapturedDribble(paddle, bounceUp, "InactivePaddleTrigger");
    }

    public void Release(Vector2 releaseDirection)
    {
        EnsureCapturedDribbleController();

        if (releaseDirection.sqrMagnitude > 0.0001f)
            direction = CorrectDirection(releaseDirection);

        capturedDribbleController.ResetCapture();
        captureAnchor = null;
        CurrentState = BallState.Free;
        lastReleaseTime = Time.time;
        Debug.Log("[BallState] Released");
        OnReleased?.Invoke();
    }

    public void Shoot(Vector2 shootDirection, float speed)
    {
        Release(shootDirection);

        if (BallMovement != null)
            BallMovement.SetBallSpeed(speed);
    }

    public void ReleaseUpward()
    {
        EnsureCapturedDribbleController();

        Vector2 releaseDirection = direction;
        releaseDirection.y = Mathf.Abs(releaseDirection.y);

        if (releaseDirection.sqrMagnitude < 0.01f)
            releaseDirection = Vector2.up;

        capturedDribbleController.LogCapturedDribble("[BallState] Released Upward");
        Release(releaseDirection.normalized);
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

        /* 
        if (Mathf.Abs(corrected.y) < data.MinDirectionY)
        {
            float signY = corrected.y == 0f
                ? (UnityEngine.Random.value < 0.5f ? -1f : 1f)
                : Mathf.Sign(corrected.y);

            corrected.y = signY * data.MinDirectionY;
        }
*/
        corrected = corrected.normalized;

        if ((corrected - beforeDir).sqrMagnitude > 0.0001f)
            Debug.Log($"[BallDirection] Corrected direction from {beforeDir} to {corrected}");

        return corrected;
    }

    private void StartCapturedDribble(PaddleController paddle, bool bounceUp, string source)
    {
        capturedDribbleController.Begin(captureAnchor, paddle, bounceUp, source);
    }

    public bool CanCaptureFromInactivePaddle(PaddleController paddle)
    {
        EnsureCapturedDribbleController();
        return capturedDribbleController.CanCaptureFromInactivePaddle(paddle);
    }

    private void EnsureCapturedDribbleController()
    {
        if (capturedDribbleController == null)
            capturedDribbleController = GetComponent<BallCapturedDribbleController>();

        if (capturedDribbleController == null)
            capturedDribbleController = gameObject.AddComponent<BallCapturedDribbleController>();

        if (BallSpeedController == null)
            BallSpeedController = GetComponent<BallSpeedController>();

        capturedDribbleController.Initialize(this, BallSpeedController);
    }

    public void EnterCapturedState()
    {
        CurrentState = BallState.Captured;
        lastCaptureTime = Time.time;
    }

    public void ReleaseCapturedStateFromDribble()
    {
        captureAnchor = null;
        CurrentState = BallState.Free;
        lastCaptureReleaseTime = Time.time;
        lastReleaseTime = Time.time;
    }

    public void NotifyCaptured()
    {
        OnCaptured?.Invoke();
    }

    public void NotifyReleased()
    {
        OnReleased?.Invoke();
    }

    public Vector2 CapturedLocalOffset => capturedLocalOffset;
    public Transform CapturedTopBound => capturedTopBound;
    public Transform CapturedBottomBound => capturedBottomBound;
    public float DribbleVerticalCorrectionDelay => dribbleVerticalCorrectionDelay;
    public float DribbleVerticalCorrectionDuration => dribbleVerticalCorrectionDuration;
    public float DribbleMaxInitialX => dribbleMaxInitialX;
    public float DribbleMovingXInfluence => dribbleMovingXInfluence;
    public float DribbleStillVelocityThreshold => dribbleStillVelocityThreshold;
    public float DribbleSpeedIncreaseCooldown => dribbleSpeedIncreaseCooldown;
    public float CaptureReleaseGraceTime => captureReleaseGraceTime;
    public bool DebugDribbleBounce => debugDribbleBounce;
    public bool DebugDribbleState => debugDribbleState;
    public bool DebugCaptureRelease => debugCaptureRelease;
    public float CapturedDribbleSpeedFallback => data != null ? data.CapturedDribbleSpeedFallback : 20f;
    public float CapturedTopOffset => data != null ? data.CapturedTopOffset : 0.5f;
    public float CapturedBottomOffset => data != null ? data.CapturedBottomOffset : -0.5f;
    public float CapturedXFollowSpeed => data != null ? data.CapturedXFollowSpeed : 30f;
    public float CapturedPaddleHitCooldown => data != null ? data.CapturedPaddleHitCooldown : 0.05f;
    public float CapturedReleaseRecaptureDelay => Mathf.Max(0f, capturedReleaseRecaptureDelay);
    public bool DebugCapturedDribbleLog => data == null || data.DebugCapturedDribbleLog;

    private float CaptureCooldown => data != null ? data.CaptureCooldown : 0.12f;
    private float ReleaseRecaptureDelay => data != null ? data.ReleaseRecaptureDelay : 0.15f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
    }
}
