using DefaultNamespace;
using UnityEngine;

public class BallCapturedDribbleController : MonoBehaviour
{
    private enum CapturePhase
    {
        None,
        ApproachingAnchor,
        Dribbling
    }

    [Header("Capture Approach")]
    [Tooltip("Reserved for approach tuning. Current approach speed still follows BallSpeedController.")]
    [SerializeField] private float captureApproachSpeed = 18f;
    [SerializeField] private float captureApproachCompleteDistance = 0.08f;

    [Header("Captured Bounds - Ellipse")]
    [SerializeField] private bool useEllipseCapturedBounds = true;
    [SerializeField] private Transform capturedTopEllipseCenter;
    [SerializeField] private Transform capturedBottomEllipseCenter;
    [SerializeField] private float capturedEllipseHalfWidth = 2.8f;
    [SerializeField] private float capturedEllipseHalfHeight = 0.35f;

    [Header("Captured Bounds - Gizmo")]
    [SerializeField] private Color capturedTopEllipseGizmoColor = new Color(0.2f, 0.7f, 1f, 1f);
    [SerializeField] private Color capturedBottomEllipseGizmoColor = new Color(1f, 0.75f, 0.2f, 1f);

    [Header("Release Alignment")]
    [SerializeField] private float verticalAlignDelay = 0.35f;
    [SerializeField] private float verticalAlignDuration = 0.25f;

    private BallController ball;
    private BallSpeedController speedController;
    private Transform pendingCaptureAnchor;
    private PaddleController pendingCapturePaddle;
    private bool pendingCaptureBounceUp = true;
    private Transform captureAnchor;
    private PaddleController capturedPaddle;
    private CapturePhase capturePhase = CapturePhase.None;
    private float capturedYDirection = 1f;
    private bool isInCaptureZone;
    private float lastCapturedPaddleHitTime = -999f;
    private float lastDribbleSpeedIncreaseTime = -999f;
    private float capturedStartTime = -999f;
    private float capturedHoldTime;
    private PaddleController inactiveCaptureSuppressedPaddle;
    private bool suppressInactiveCaptureUntilPaddleActive;

    public void Initialize(BallController ballController, BallSpeedController ballSpeedController)
    {
        ball = ballController;

        if (speedController == ballSpeedController)
            return;

        if (speedController != null)
            speedController.OnSpeedStateChanged -= HandleSpeedStateChanged;

        speedController = ballSpeedController;

        if (speedController != null)
            speedController.OnSpeedStateChanged += HandleSpeedStateChanged;
    }

    public bool IsInactivePaddleCaptureBlocked(PaddleController paddle)
    {
        if (paddle == null || paddle.IsPaddleActive)
            return false;

        if (ShouldLogCapture())
            Debug.Log("[CaptureBlocked] reason=inactivePaddleDirectCapture");

        return true;
    }

    public bool CanStartCapturedDribble()
    {
        if (speedController != null && speedController.IsWeakened)
        {
            if (ShouldLogCapture())
                Debug.Log("[CaptureBlocked] reason=ballWeakened");

            return false;
        }

        if (ball.CanCaptureNow)
            return true;

        if (ShouldLogCapture() && (ball.IsInCapturedReleaseRecaptureDelay || ball.IsInReleaseRecaptureDelay))
            Debug.Log("[CaptureBlocked] reason=releaseRecaptureDelay");

        return false;
    }

    public bool CanCaptureFromInactivePaddle(PaddleController paddle)
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
            if (ShouldLogCapture())
                Debug.Log("[CaptureBlocked] reason=paddleReleasedUntilActive");

            return false;
        }

        return true;
    }

    public void Begin(Transform anchor, PaddleController paddle, bool bounceUp, string source)
    {
        BeginPendingCapture(anchor, paddle, bounceUp, source);
    }

    public bool IsInCaptureZone => isInCaptureZone;
    public bool HasPendingCapture =>
        isInCaptureZone &&
        pendingCaptureAnchor != null &&
        pendingCapturePaddle != null;

    public void BeginImmediate(Transform anchor, PaddleController paddle, bool bounceUp, string source)
    {
        if (paddle != null && paddle == inactiveCaptureSuppressedPaddle && paddle.IsPaddleActive)
            ClearInactiveCaptureSuppression("paddle active before capture");

        captureAnchor = anchor;
        capturedPaddle = paddle;
        capturedYDirection = bounceUp ? 1f : -1f;
        capturePhase = CapturePhase.Dribbling;
        lastDribbleSpeedIncreaseTime = -999f;
        capturedStartTime = Time.time;
        capturedHoldTime = 0f;

        float paddleVelocityX = capturedPaddle != null ? capturedPaddle.VelocityX : 0f;
        ball.direction = GetDribbleBounceDirection(bounceUp, paddleVelocityX);

        ball.EnterCapturedState();

        LogCapture("[BallState] Captured");
        LogCapturedDribble("[BallState] Captured Dribble Start");
        LogStartCapturedDribble(source, bounceUp, paddleVelocityX);
        ball.NotifyCaptured();
    }

    public void BeginPendingCapture(
        Transform anchor,
        PaddleController paddle,
        bool bounceUp,
        string source
    )
    {
        EnterCaptureZone(anchor, paddle, bounceUp);
    }

    public void EnterCaptureZone(Transform anchor, PaddleController paddle, bool bounceUp)
    {
        if (anchor == null)
            return;

        if (paddle == null)
            return;

        if (
            isInCaptureZone &&
            pendingCaptureAnchor == anchor &&
            pendingCapturePaddle == paddle
        )
            return;

        isInCaptureZone = true;
        pendingCaptureAnchor = anchor;
        pendingCapturePaddle = paddle;
        pendingCaptureBounceUp = bounceUp;

        LogCapturePhase("[CaptureZone] Enter/Stay: pending candidate maintained");
    }

    public void ExitCaptureZone(PaddleController paddle)
    {
        if (!isInCaptureZone)
            return;

        if (paddle != null && pendingCapturePaddle != null && pendingCapturePaddle != paddle)
            return;

        isInCaptureZone = false;
        pendingCaptureAnchor = null;
        pendingCapturePaddle = null;
        pendingCaptureBounceUp = true;

        LogCapturePhase("[CaptureZone] Exit: pending candidate cleared");
    }

    public bool TryStartCaptureFromPaddleHit(PaddleController hitPaddle, bool bounceUp)
    {
        if (!CanUsePendingCapture(hitPaddle))
            return false;

        StartApproachFromPendingCapture(hitPaddle, bounceUp);
        return true;
    }

    private bool CanUsePendingCapture(PaddleController hitPaddle)
    {
        if (capturePhase != CapturePhase.None)
            return false;

        if (!HasPendingCapture)
            return false;

        if (hitPaddle == null)
            return false;

        if (!hitPaddle.IsPaddleActive)
        {
            LogCapturePhase("[Capture] Capture blocked: paddle inactive");
            return false;
        }

        if (hitPaddle != pendingCapturePaddle)
        {
            LogCapturePhase("[Capture] Capture blocked: paddle mismatch");
            return false;
        }

        if (!CanStartCapturedDribble())
        {
            LogCapturePhase("[Capture] Capture blocked: cooldown");
            return false;
        }

        return true;
    }

    private void StartApproachFromPendingCapture(PaddleController hitPaddle, bool bounceUp)
    {
        captureAnchor = pendingCaptureAnchor;
        capturedPaddle = pendingCapturePaddle;

        LogCapturePhase("[Capture] Pending -> Capture by paddle hit");
        BeginApproachAnchor(hitPaddle, bounceUp);
    }

    private void HandleSpeedStateChanged(BallSpeedState state)
    {
        if (state != BallSpeedState.Normal)
            return;

        if (!CanUsePendingCapture(pendingCapturePaddle))
            return;

        LogCapturePhase("[Capture] Weakened ended -> pending capture resumed");
        StartApproachFromPendingCapture(pendingCapturePaddle, pendingCaptureBounceUp);
    }

    private void OnDestroy()
    {
        if (speedController != null)
            speedController.OnSpeedStateChanged -= HandleSpeedStateChanged;
    }

    public void CancelPendingCapture(PaddleController paddle)
    {
        ExitCaptureZone(paddle);
    }

    public void Tick()
    {
        if (capturePhase == CapturePhase.ApproachingAnchor)
        {
            TickApproachAnchor();
            return;
        }

        if (capturePhase != CapturePhase.Dribbling)
            return;

        TickDribble();
    }

    private void TickDribble()
    {
        if (captureAnchor == null)
            return;

        if (UpdateCapturedReleaseState())
            return;

        capturedHoldTime += Time.deltaTime;

        Vector3 pos = transform.position;
        Vector3 anchorPos = captureAnchor.position;

        float dribbleSpeed = speedController != null
            ? speedController.CurrentSpeed
            : ball.CapturedDribbleSpeedFallback;

        MoveCapturedPosition(ref pos, anchorPos, dribbleSpeed);
        ResolveCapturedBounds(pos, anchorPos, out float bottomY, out float topY);
        ApplyCapturedBoundBounce(ref pos, bottomY, topY);

        transform.position = pos;

        UpdateCapturedDribbleDirection();

        LogCapturedDribble($"[BallState] Captured Dribble Speed: {dribbleSpeed}");
    }

    private void MoveCapturedPosition(ref Vector3 pos, Vector3 anchorPos, float dribbleSpeed)
    {
        float targetX = anchorPos.x + ball.CapturedLocalOffset.x;

        pos.x = Mathf.Lerp(pos.x, targetX, ball.CapturedXFollowSpeed * Time.deltaTime);
        pos.y += capturedYDirection * dribbleSpeed * Time.deltaTime;
    }

    private void ResolveCapturedBounds(
        Vector2 pos,
        Vector2 anchorPos,
        out float bottomY,
        out float topY
    )
    {
        topY = GetCapturedTopY(pos, anchorPos);
        bottomY = GetCapturedBottomY(pos, anchorPos);
        CorrectInvalidCapturedBounds(ref bottomY, ref topY);
    }

    private void ApplyCapturedBoundBounce(ref Vector3 pos, float bottomY, float topY)
    {
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
    }

    private void UpdateCapturedDribbleDirection()
    {
        float paddleVelocityX = capturedPaddle != null ? capturedPaddle.VelocityX : 0f;
        ball.direction = GetDribbleBounceDirection(capturedYDirection > 0f, paddleVelocityX);

        LogMoveCapturedBall(paddleVelocityX);
    }

    private float GetCapturedTopY(Vector2 pos, Vector2 anchorPos)
    {
        if (useEllipseCapturedBounds && capturedTopEllipseCenter != null)
        {
            return CapturedEllipseBounds.GetInnerBoundY(
                capturedTopEllipseCenter,
                pos.x,
                true,
                ball.actualRadius,
                capturedEllipseHalfWidth,
                capturedEllipseHalfHeight
            );
        }

        if (ball.CapturedTopBound != null)
            return ball.CapturedTopBound.position.y - ball.actualRadius;

        return anchorPos.y + ball.CapturedTopOffset;
    }

    private float GetCapturedBottomY(Vector2 pos, Vector2 anchorPos)
    {
        if (useEllipseCapturedBounds && capturedBottomEllipseCenter != null)
        {
            return CapturedEllipseBounds.GetInnerBoundY(
                capturedBottomEllipseCenter,
                pos.x,
                false,
                ball.actualRadius,
                capturedEllipseHalfWidth,
                capturedEllipseHalfHeight
            );
        }

        if (ball.CapturedBottomBound != null)
            return ball.CapturedBottomBound.position.y + ball.actualRadius;

        return anchorPos.y + ball.CapturedBottomOffset;
    }

    private void CorrectInvalidCapturedBounds(ref float bottomY, ref float topY)
    {
        if (bottomY < topY)
            return;

        float centerY = (topY + bottomY) * 0.5f;
        float minGap = ball.actualRadius * 2f;

        bottomY = centerY - minGap * 0.5f;
        topY = centerY + minGap * 0.5f;

        if (ShouldLogCapture())
            Debug.LogWarning("[CapturedDribble] Invalid ellipse bounds corrected");
    }

    private void OnDrawGizmosSelected()
    {
        DrawCapturedEllipseGizmo(capturedTopEllipseCenter, capturedTopEllipseGizmoColor);
        DrawCapturedEllipseGizmo(capturedBottomEllipseCenter, capturedBottomEllipseGizmoColor);
    }

    private void DrawCapturedEllipseGizmo(Transform ellipseCenter, Color color)
    {
        if (!useEllipseCapturedBounds || ellipseCenter == null)
            return;

        CapturedEllipseBounds.DrawGizmo(
            ellipseCenter,
            color,
            capturedEllipseHalfWidth,
            capturedEllipseHalfHeight
        );
    }

    public void ResetCapture()
    {
        capturePhase = CapturePhase.None;
        captureAnchor = null;
        capturedPaddle = null;
        capturedStartTime = -999f;
        capturedHoldTime = 0f;
    }

    public void RefreshInactiveCaptureSuppression()
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

    private bool UpdateCapturedReleaseState()
    {
        if (!ball.IsCaptured)
            return false;

        bool gracePassed = Time.time >= capturedStartTime + ball.CaptureReleaseGraceTime;
        bool active = capturedPaddle != null && capturedPaddle.IsPaddleActive;

        if (ShouldLogCapture())
        {
            Debug.Log(
                $"[CapturedState] active={active}, gracePassed={gracePassed}, isCaptured={ball.IsCaptured}"
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

    private void BeginApproachAnchor(PaddleController hitPaddle, bool bounceUp)
    {
        capturedPaddle = hitPaddle;
        capturedYDirection = bounceUp ? 1f : -1f;
        capturedStartTime = Time.time;
        capturePhase = CapturePhase.ApproachingAnchor;

        Vector2 targetPos = GetApproachTargetPosition();
        Vector2 currentPos = transform.position;
        Vector2 directionToAnchor = targetPos - currentPos;

        if (directionToAnchor.sqrMagnitude > 0.0001f)
            ball.direction = directionToAnchor.normalized;

        ball.EnterCapturedState();
        LogCapturePhase("[Capture] Paddle Hit -> Approach Anchor");
    }

    private void TickApproachAnchor()
    {
        if (captureAnchor == null)
        {
            ReleaseCapturedDribble("captureAnchor null");
            return;
        }

        if (UpdateCapturedReleaseState())
            return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = GetApproachTargetPosition();
        Vector2 nextPos = Vector2.MoveTowards(
            currentPos,
            targetPos,
            speedController.CurrentSpeed * Time.deltaTime
        );

        Vector2 approachDirection = targetPos - currentPos;

        if (approachDirection.sqrMagnitude > 0.0001f)
            ball.direction = approachDirection.normalized;

        transform.position = nextPos;

        if (Vector2.Distance(nextPos, targetPos) <= captureApproachCompleteDistance)
            BeginCapturedDribble();
    }

    private void BeginCapturedDribble()
    {
        Vector2 targetPos = GetApproachTargetPosition();
        transform.position = targetPos;
        capturePhase = CapturePhase.Dribbling;
        capturedStartTime = Time.time;
        capturedHoldTime = 0f;
        lastDribbleSpeedIncreaseTime = -999f;

        float paddleVelocityX = capturedPaddle != null ? capturedPaddle.VelocityX : 0f;
        ball.direction = GetDribbleBounceDirection(capturedYDirection > 0f, paddleVelocityX);

        LogCapturePhase("[Capture] Approach Complete -> Dribbling");
        LogCapture("[BallState] Captured");
        LogCapturedDribble("[BallState] Captured Dribble Start");
        LogStartCapturedDribble("ApproachAnchor", capturedYDirection > 0f, paddleVelocityX);
        ball.NotifyCaptured();
    }

    private Vector2 GetApproachTargetPosition()
    {
        Vector2 anchorPos = captureAnchor.position;

        return new Vector2(
            anchorPos.x + ball.CapturedLocalOffset.x,
            anchorPos.y
        );
    }

    private void ReleaseCapturedDribble(string reason)
    {
        if (!ball.IsCaptured)
            return;

        PaddleController releasedPaddle = capturedPaddle;
        float releaseSpeed = GetCapturedReleaseSpeed();
        Vector2 releaseDirection = GetAlignedReleaseDirection(ball.direction);
        ball.Launch(releaseSpeed, releaseDirection);

        ResetCapture();
        ball.ReleaseCapturedStateFromDribble();

        if (ShouldLogCapture())
            Debug.Log($"[ReleaseCapturedDribble] reason={reason}, time={Time.time:0.00}");

        if (reason == "paddle inactive" && releasedPaddle != null)
            SuppressInactiveCaptureUntilPaddleActive(releasedPaddle);

        LogCapturePhase("[Capture] Release");
        ball.NotifyReleased();
    }

    private float GetCapturedReleaseSpeed()
    {
        if (speedController != null && speedController.CurrentSpeed > 0f)
            return speedController.CurrentSpeed;

        return ball != null ? ball.Velocity.magnitude : 0f;
    }

    private Vector2 GetAlignedReleaseDirection(Vector2 rawReleaseDirection)
    {
        Vector2 raw = rawReleaseDirection.sqrMagnitude > 0.0001f
            ? rawReleaseDirection.normalized
            : Vector2.up;

        if (capturedHoldTime < verticalAlignDelay)
            return raw;

        float alignEndTime = verticalAlignDelay + Mathf.Max(0.0001f, verticalAlignDuration);
        float t = Mathf.InverseLerp(verticalAlignDelay, alignEndTime, capturedHoldTime);

        return Vector2.Lerp(raw, Vector2.up, Mathf.Clamp01(t)).normalized;
    }

    private void ApplyCapturedPaddleHit(bool isTopBound)
    {
        if (Time.time < lastCapturedPaddleHitTime + ball.CapturedPaddleHitCooldown)
        {
            LogCapturedDribble("[CapturedPaddleHit] Skipped by cooldown.");
                               return;
        }

        if (speedController == null)
            return;

        SoundPlayOptions feedbackOptions = new()
        {
            pitchScale = 1.08f,
            ratio = speedController.GetSpeedRatio(),
            volumeScale = 0.7f
        };

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayBallBounceFeedback(
                BallFeedbackSurface.Paddle,
                SoundId.BallBounce,
                feedbackOptions);
        }
        else
        {
            SoundManager.Instance?.Play(SoundId.BallBounce, feedbackOptions);
        }
        
        TryIncreaseSpeedFromDribble();

        lastCapturedPaddleHitTime = Time.time;
        

        LogCapturedDribble(isTopBound
            ? "[CapturedPaddleHit] Top bound hit. Apply paddle bonus."
            : "[CapturedPaddleHit] Bottom bound hit. Apply paddle bonus.");
    }

    private void SuppressInactiveCaptureUntilPaddleActive(PaddleController paddle)
    {
        inactiveCaptureSuppressedPaddle = paddle;
        suppressInactiveCaptureUntilPaddleActive = true;

        if (ShouldLogCapture())
            Debug.Log($"[CaptureBlocked] reason=paddleReleasedUntilActive, paddle={paddle.name}");
    }

    private void ClearInactiveCaptureSuppression(string reason)
    {
        suppressInactiveCaptureUntilPaddleActive = false;
        inactiveCaptureSuppressedPaddle = null;

        if (ShouldLogCapture())
            Debug.Log($"[CaptureUnblocked] reason={reason}");
    }

    private Vector2 GetDribbleBounceDirection(bool bounceUp, float paddleVelocityX)
    {
        float y = bounceUp ? 1f : -1f;
        float elapsed = Mathf.Max(0f, Time.time - capturedStartTime);
        float correctionT = Mathf.InverseLerp(
            ball.DribbleVerticalCorrectionDelay,
            ball.DribbleVerticalCorrectionDelay + ball.DribbleVerticalCorrectionDuration,
            elapsed
        );

        correctionT = Mathf.Clamp01(correctionT);

        float currentX = Mathf.Clamp(ball.direction.x, -ball.DribbleMaxInitialX, ball.DribbleMaxInitialX);
        float movingX = paddleVelocityX * ball.DribbleMovingXInfluence;
        float targetX = Mathf.Abs(paddleVelocityX) < ball.DribbleStillVelocityThreshold
            ? 0f
            : movingX;

        float x = Mathf.Lerp(currentX, targetX, correctionT);
        Vector2 finalDirection = new Vector2(x, y).normalized;

        if (ball.DebugDribbleBounce)
        {
            Debug.Log(
                $"[DribbleBounce] elapsed={elapsed:0.00}, correctionT={correctionT:0.00}, currentX={currentX:0.00}, targetX={targetX:0.00}, finalDir={finalDirection}"
            );
        }

        return finalDirection;
    }

    private void LogStartCapturedDribble(string source, bool bounceUp, float paddleVelocityX)
    {
        if (!ball.DebugDribbleState)
            return;

        string paddleName = capturedPaddle != null
            ? capturedPaddle.name
            : "None";
        string paddleSide = bounceUp ? "Lower" : "Upper";
        bool paddleActive = capturedPaddle != null && capturedPaddle.IsPaddleActive;

        Debug.Log(
            $"[StartCapturedDribble] source={source}, paddle={paddleSide}, paddleObject={paddleName}, active={paddleActive}, bounceUp={bounceUp}, paddleVelX={paddleVelocityX:0.00}, dir={ball.direction}, isCaptured={ball.IsCaptured}, time={Time.time:0.00}"
        );
    }

    private void LogMoveCapturedBall(float paddleVelocityX)
    {
        if (!ball.DebugDribbleState)
            return;

        Debug.Log(
            $"[MoveCapturedBall] dir={ball.direction}, capturedYDirection={capturedYDirection:0}, paddleVelX={paddleVelocityX:0.00}"
        );
    }

    private void TryIncreaseSpeedFromDribble()
    {
        if (Time.time < lastDribbleSpeedIncreaseTime + ball.DribbleSpeedIncreaseCooldown)
        {
            if (ball.DebugDribbleBounce)
                Debug.Log("[DribbleSpeed] skipped by cooldown");

            return;
        }

        lastDribbleSpeedIncreaseTime = Time.time;
        speedController.AddSpeedByDribble();

        if (ball.DebugDribbleBounce)
            Debug.Log("[DribbleSpeed] increase applied");
    }

    public void LogCapturedDribble(string message)
    {
        if (ball.DebugCapturedDribbleLog)
            Debug.Log(message);
    }

    private void LogCapturePhase(string message)
    {
        LogCapture(message);
    }

    private void LogCapture(string message)
    {
        if (ShouldLogCapture())
            Debug.Log(message);
    }

    private bool ShouldLogCapture()
    {
        return ball == null || ball.data == null || ball.data.DebugCaptureLog;
    }
}
