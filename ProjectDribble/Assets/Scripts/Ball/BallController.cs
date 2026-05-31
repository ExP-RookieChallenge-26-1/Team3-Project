using System;
using DefaultNamespace;
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
    [SerializeField] private Transform capturedTopBound;
    [SerializeField] private Transform capturedBottomBound;
    [SerializeField] private PaddleSpeedModifier topCapturedPaddleModifier;
    [SerializeField] private PaddleSpeedModifier bottomCapturedPaddleModifier;

    public float actualRadius;
    public Vector2 direction;

    public LayerMask collisionMask;
    public BallData data;

    private Transform tr;
    private CircleCollider2D cc;
    private BallMovement BallMovement;
    private BallCollisionHandler BallCollisionHandler;
    private BallSpeedController BallSpeedController;
    private PaddleMovement capturedPaddle;
    private float capturedYDirection = 1f;
    private float lastCaptureTime = -999f;
    private float lastReleaseTime = -999f;
    private float lastCapturedPaddleHitTime = -999f;

    public int ballDamage = 1;
    public BallState CurrentState { get; private set; } = BallState.Free;
    public bool IsFree => CurrentState == BallState.Free;
    public bool IsCaptured => CurrentState == BallState.Captured;
    public bool IsInCaptureCooldown => Time.time < lastCaptureTime + CaptureCooldown;
    public bool IsInReleaseRecaptureDelay => Time.time < lastReleaseTime + ReleaseRecaptureDelay;
    public bool CanCaptureNow => IsFree && !IsInCaptureCooldown && !IsInReleaseRecaptureDelay;

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
            MoveCapturedBall();
            return;
        }

        direction = BallMovement.MoveBall(direction, actualRadius, collisionMask);
    }

    public void Capture(Transform anchor)
    {
        Capture(anchor, null);
    }

    public void Capture(Transform anchor, PaddleMovement paddle)
    {
        if (!CanCaptureNow)
            return;

        if (anchor != null)
            captureAnchor = anchor;

        if (captureAnchor == null)
            return;

        capturedPaddle = paddle;
        capturedYDirection = Mathf.Abs(direction.y) < 0.01f
            ? 1f
            : Mathf.Sign(direction.y);

        CurrentState = BallState.Captured;
        lastCaptureTime = Time.time;
        Debug.Log("[BallState] Captured");
        LogCapturedDribble("[BallState] Captured Dribble Start");
        OnCaptured?.Invoke();
    }

    public void Release(Vector2 releaseDirection)
    {
        if (releaseDirection.sqrMagnitude > 0.0001f)
            direction = CorrectDirection(releaseDirection);

        captureAnchor = null;
        capturedPaddle = null;
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
        Vector2 releaseDirection = direction;
        releaseDirection.y = Mathf.Abs(releaseDirection.y);

        if (releaseDirection.sqrMagnitude < 0.01f)
            releaseDirection = Vector2.up;

        LogCapturedDribble("[BallState] Released Upward");
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

    private void MoveCapturedBall()
    {
        if (captureAnchor == null)
            return;

        if (capturedPaddle != null && !capturedPaddle.IsPaddleActive)
        {
            ReleaseUpward();
            return;
        }

        Vector3 pos = transform.position;
        Vector3 anchorPos = captureAnchor.position;

        float targetX = anchorPos.x + capturedLocalOffset.x;
        float xBeforeMove = pos.x;
        float dribbleSpeed = BallSpeedController != null
            ? BallSpeedController.CurrentSpeed
            : CapturedDribbleSpeedFallback;

        pos.x = Mathf.Lerp(pos.x, targetX, CapturedXFollowSpeed * Time.deltaTime);
        pos.y += capturedYDirection * dribbleSpeed * Time.deltaTime;

        float topY = capturedTopBound != null
            ? capturedTopBound.position.y - actualRadius
            : anchorPos.y + CapturedTopOffset;
        float bottomY = capturedBottomBound != null
            ? capturedBottomBound.position.y + actualRadius
            : anchorPos.y + CapturedBottomOffset;

        if (pos.y >= topY)
        {
            pos.y = topY;
            capturedYDirection = -1f;
            LogCapturedDribble("[BallState] Captured Dribble Bounce Top");
            ApplyCapturedPaddleHit(true);
        }
        else if (pos.y <= bottomY)
        {
            pos.y = bottomY;
            capturedYDirection = 1f;
            LogCapturedDribble("[BallState] Captured Dribble Bounce Bottom");
            ApplyCapturedPaddleHit(false);
        }

        transform.position = pos;

        Vector2 capturedDirection = new Vector2(pos.x - xBeforeMove, capturedYDirection);

        if (capturedDirection.sqrMagnitude > 0.0001f)
            direction = CorrectDirection(capturedDirection);

        LogCapturedDribble($"[BallState] Captured Dribble Speed: {dribbleSpeed}");
    }

    private void ApplyCapturedPaddleHit(bool isTopBound)
    {
        if (Time.time < lastCapturedPaddleHitTime + CapturedPaddleHitCooldown)
        {
            LogCapturedDribble("[CapturedPaddleHit] Skipped by cooldown.");
            return;
        }

        if (BallSpeedController == null)
            return;

        PaddleSpeedModifier modifier = isTopBound
            ? topCapturedPaddleModifier
            : bottomCapturedPaddleModifier;

        if (modifier != null)
        {
            modifier.ModifySpeed(BallSpeedController);
        }
        else
        {
            float fallbackSpeedBonus = GetCapturedPaddleFallbackSpeedBonus(isTopBound);
            BallSpeedController.AddSpeedByPaddle(fallbackSpeedBonus);
        }

        lastCapturedPaddleHitTime = Time.time;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play2D(SoundId.BallBounce);

        LogCapturedDribble(isTopBound
            ? "[CapturedPaddleHit] Top bound hit. Apply paddle bonus."
            : "[CapturedPaddleHit] Bottom bound hit. Apply paddle bonus.");
    }

    private float GetCapturedPaddleFallbackSpeedBonus(bool isTopBound)
    {
        if (data == null)
            return 0f;

        return isTopBound
            ? data.outerPaddleSpeedIncrease
            : data.innerPaddleSpeedIncrease;
    }

    private void LogCapturedDribble(string message)
    {
        if (DebugCapturedDribbleLog)
            Debug.Log(message);
    }

    private float CaptureCooldown => data != null ? data.CaptureCooldown : 0.12f;
    private float ReleaseRecaptureDelay => data != null ? data.ReleaseRecaptureDelay : 0.15f;
    private float CapturedDribbleSpeedFallback => data != null ? data.CapturedDribbleSpeedFallback : 20f;
    private float CapturedTopOffset => data != null ? data.CapturedTopOffset : 0.5f;
    private float CapturedBottomOffset => data != null ? data.CapturedBottomOffset : -0.5f;
    private float CapturedXFollowSpeed => data != null ? data.CapturedXFollowSpeed : 30f;
    private float CapturedPaddleHitCooldown => data != null ? data.CapturedPaddleHitCooldown : 0.05f;
    private bool DebugCapturedDribbleLog => data == null || data.DebugCapturedDribbleLog;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
    }
}
