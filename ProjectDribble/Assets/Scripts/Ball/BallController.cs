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
    private PaddleMovement capturedPaddle;
    private float capturedYDirection = 1f;
    private float lastCaptureTime = -999f;
    private float lastReleaseTime = -999f;
    private float lastCapturedPaddleHitTime = -999f;
    private float lastDribbleSpeedIncreaseTime = -999f;
    private float capturedStartTime = -999f;
    private float lastCaptureReleaseTime = -999f;
    private PaddleMovement inactiveCaptureSuppressedPaddle;
    private bool suppressInactiveCaptureUntilPaddleActive;

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
        RefreshInactiveCaptureSuppression();

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
        if (IsInactivePaddleCaptureBlocked(paddle))
            return;

        if (!CanStartCapturedDribble())
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
        PaddleMovement paddle,
        bool bounceUp,
        string source
    )
    {
        if (IsInactivePaddleCaptureBlocked(paddle))
            return;

        if (!CanStartCapturedDribble())
            return;

        if (anchor != null)
            captureAnchor = anchor;

        if (captureAnchor == null)
            return;

        StartCapturedDribble(paddle, bounceUp, source);
    }

    public void CaptureFromInactiveTrigger(
        Transform anchor,
        PaddleMovement paddle,
        bool bounceUp
    )
    {
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

    private bool IsInactivePaddleCaptureBlocked(PaddleMovement paddle)
    {
        if (paddle == null || paddle.IsPaddleActive)
            return false;

        if (debugCaptureRelease)
            Debug.Log("[CaptureBlocked] reason=inactivePaddleDirectCapture");

        return true;
    }

    public void Release(Vector2 releaseDirection)
    {
        if (releaseDirection.sqrMagnitude > 0.0001f)
            direction = CorrectDirection(releaseDirection);

        captureAnchor = null;
        capturedPaddle = null;
        capturedStartTime = -999f;
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

    private void MoveCapturedBall()
    {
        if (captureAnchor == null)
            return;

        if (UpdateCapturedReleaseState())
            return;

        Vector3 pos = transform.position;
        Vector3 anchorPos = captureAnchor.position;

        float targetX = anchorPos.x + capturedLocalOffset.x;
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

        float paddleVelocityX = capturedPaddle != null ? capturedPaddle.VelocityX : 0f;
        direction = GetDribbleBounceDirection(capturedYDirection > 0f, paddleVelocityX);

        LogMoveCapturedBall(paddleVelocityX);

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

        TryIncreaseSpeedFromDribble();

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

    private void StartCapturedDribble(PaddleMovement paddle, bool bounceUp, string source)
    {
        if (paddle != null && paddle == inactiveCaptureSuppressedPaddle && paddle.IsPaddleActive)
            ClearInactiveCaptureSuppression("paddle active before capture");

        capturedPaddle = paddle;
        capturedYDirection = bounceUp ? 1f : -1f;
        CurrentState = BallState.Captured;
        lastCaptureTime = Time.time;
        lastDribbleSpeedIncreaseTime = -999f;
        capturedStartTime = Time.time;

        float paddleVelocityX = capturedPaddle != null ? capturedPaddle.VelocityX : 0f;
        direction = GetDribbleBounceDirection(bounceUp, paddleVelocityX);

        Debug.Log("[BallState] Captured");
        LogCapturedDribble("[BallState] Captured Dribble Start");
        LogStartCapturedDribble(source, bounceUp, paddleVelocityX);
        OnCaptured?.Invoke();
    }

    private bool CanStartCapturedDribble()
    {
        if (CanCaptureNow)
            return true;

        if (debugCaptureRelease && (IsInCapturedReleaseRecaptureDelay || IsInReleaseRecaptureDelay))
            Debug.Log("[CaptureBlocked] reason=releaseRecaptureDelay");

        return false;
    }

    public bool CanCaptureFromInactivePaddle(PaddleMovement paddle)
    {
        RefreshInactiveCaptureSuppression();

        if (!CanStartCapturedDribble())
            return false;

        if (
            suppressInactiveCaptureUntilPaddleActive &&
            paddle != null &&
            paddle == inactiveCaptureSuppressedPaddle
        )
        {
            if (debugCaptureRelease)
                Debug.Log("[CaptureBlocked] reason=paddleReleasedUntilActive");

            return false;
        }

        return true;
    }

    private bool UpdateCapturedReleaseState()
    {
        if (!IsCaptured)
            return false;

        bool gracePassed = Time.time >= capturedStartTime + captureReleaseGraceTime;
        bool active = capturedPaddle != null && capturedPaddle.IsPaddleActive;

        if (debugCaptureRelease)
        {
            Debug.Log(
                $"[CapturedState] active={active}, gracePassed={gracePassed}, isCaptured={IsCaptured}"
            );
        }

        if (capturedPaddle == null)
        {
            ReleaseCapturedDribble("capturedPaddle null");
            return true;
        }

        if (gracePassed && !capturedPaddle.IsPaddleActive)
        {
            ReleaseCapturedDribble("paddle inactive");
            return true;
        }

        return false;
    }

    private void ReleaseCapturedDribble(string reason)
    {
        if (!IsCaptured)
            return;

        PaddleMovement releasedPaddle = capturedPaddle;

        if (direction.sqrMagnitude < 0.0001f)
            direction = new Vector2(0f, capturedYDirection).normalized;

        captureAnchor = null;
        capturedPaddle = null;
        capturedStartTime = -999f;
        CurrentState = BallState.Free;
        lastCaptureReleaseTime = Time.time;
        lastReleaseTime = Time.time;

        if (debugCaptureRelease)
            Debug.Log($"[ReleaseCapturedDribble] reason={reason}, time={Time.time:0.00}");

        if (reason == "paddle inactive" && releasedPaddle != null)
            SuppressInactiveCaptureUntilPaddleActive(releasedPaddle);

        OnReleased?.Invoke();
    }

    private void SuppressInactiveCaptureUntilPaddleActive(PaddleMovement paddle)
    {
        inactiveCaptureSuppressedPaddle = paddle;
        suppressInactiveCaptureUntilPaddleActive = true;

        if (debugCaptureRelease)
            Debug.Log($"[CaptureBlocked] reason=paddleReleasedUntilActive, paddle={paddle.name}");
    }

    private void RefreshInactiveCaptureSuppression()
    {
        if (!suppressInactiveCaptureUntilPaddleActive)
            return;

        if (inactiveCaptureSuppressedPaddle == null)
        {
            ClearInactiveCaptureSuppression("paddle missing");
            return;
        }

        if (inactiveCaptureSuppressedPaddle.IsPaddleActive)
            ClearInactiveCaptureSuppression("paddle active");
    }

    private void ClearInactiveCaptureSuppression(string reason)
    {
        suppressInactiveCaptureUntilPaddleActive = false;
        inactiveCaptureSuppressedPaddle = null;

        if (debugCaptureRelease)
            Debug.Log($"[CaptureUnblocked] reason={reason}");
    }

    private Vector2 GetDribbleBounceDirection(bool bounceUp, float paddleVelocityX)
    {
        float y = bounceUp ? 1f : -1f;
        float elapsed = Mathf.Max(0f, Time.time - capturedStartTime);
        float correctionT = Mathf.InverseLerp(
            dribbleVerticalCorrectionDelay,
            dribbleVerticalCorrectionDelay + dribbleVerticalCorrectionDuration,
            elapsed
        );

        correctionT = Mathf.Clamp01(correctionT);

        float currentX = Mathf.Clamp(direction.x, -dribbleMaxInitialX, dribbleMaxInitialX);
        float movingX = paddleVelocityX * dribbleMovingXInfluence;
        float targetX = Mathf.Abs(paddleVelocityX) < dribbleStillVelocityThreshold
            ? 0f
            : movingX;

        float x = Mathf.Lerp(currentX, targetX, correctionT);
        Vector2 finalDirection = new Vector2(x, y).normalized;

        if (debugDribbleBounce)
        {
            Debug.Log(
                $"[DribbleBounce] elapsed={elapsed:0.00}, correctionT={correctionT:0.00}, currentX={currentX:0.00}, targetX={targetX:0.00}, finalDir={finalDirection}"
            );
        }

        return finalDirection;
    }

    private void LogStartCapturedDribble(string source, bool bounceUp, float paddleVelocityX)
    {
        if (!debugDribbleState)
            return;

        string paddleName = capturedPaddle != null
            ? capturedPaddle.name
            : "None";
        string paddleSide = bounceUp ? "Lower" : "Upper";
        bool paddleActive = capturedPaddle != null && capturedPaddle.IsPaddleActive;

        Debug.Log(
            $"[StartCapturedDribble] source={source}, paddle={paddleSide}, paddleObject={paddleName}, active={paddleActive}, bounceUp={bounceUp}, paddleVelX={paddleVelocityX:0.00}, dir={direction}, isCaptured={IsCaptured}, time={Time.time:0.00}"
        );
    }

    private void LogMoveCapturedBall(float paddleVelocityX)
    {
        if (!debugDribbleState)
            return;

        Debug.Log(
            $"[MoveCapturedBall] dir={direction}, capturedYDirection={capturedYDirection:0}, paddleVelX={paddleVelocityX:0.00}"
        );
    }

    private void TryIncreaseSpeedFromDribble()
    {
        if (Time.time < lastDribbleSpeedIncreaseTime + dribbleSpeedIncreaseCooldown)
        {
            if (debugDribbleBounce)
                Debug.Log("[DribbleSpeed] skipped by cooldown");

            return;
        }

        lastDribbleSpeedIncreaseTime = Time.time;
        BallSpeedController.AddSpeedByDribble();

        if (debugDribbleBounce)
            Debug.Log("[DribbleSpeed] increase applied");
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
    private float CapturedReleaseRecaptureDelay => Mathf.Max(0f, capturedReleaseRecaptureDelay);
    private bool DebugCapturedDribbleLog => data == null || data.DebugCapturedDribbleLog;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
    }
}
