using DefaultNamespace;
using UnityEngine;

public class BallCapturedDribbleController : MonoBehaviour
{
    private BallController ball;
    private BallSpeedController speedController;
    private Transform captureAnchor;
    private PaddleController capturedPaddle;
    private float capturedYDirection = 1f;
    private float lastCapturedPaddleHitTime = -999f;
    private float lastDribbleSpeedIncreaseTime = -999f;
    private float capturedStartTime = -999f;
    private PaddleController inactiveCaptureSuppressedPaddle;
    private bool suppressInactiveCaptureUntilPaddleActive;

    public void Initialize(BallController ballController, BallSpeedController ballSpeedController)
    {
        ball = ballController;
        speedController = ballSpeedController;
    }

    public bool IsInactivePaddleCaptureBlocked(PaddleController paddle)
    {
        if (paddle == null || paddle.IsPaddleActive)
            return false;

        if (ball.DebugCaptureRelease)
            Debug.Log("[CaptureBlocked] reason=inactivePaddleDirectCapture");

        return true;
    }

    public bool CanStartCapturedDribble()
    {
        if (ball.CanCaptureNow)
            return true;

        if (ball.DebugCaptureRelease && (ball.IsInCapturedReleaseRecaptureDelay || ball.IsInReleaseRecaptureDelay))
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
            if (ball.DebugCaptureRelease)
                Debug.Log("[CaptureBlocked] reason=paddleReleasedUntilActive");

            return false;
        }

        return true;
    }

    public void Begin(Transform anchor, PaddleController paddle, bool bounceUp, string source)
    {
        if (paddle != null && paddle == inactiveCaptureSuppressedPaddle && paddle.IsPaddleActive)
            ClearInactiveCaptureSuppression("paddle active before capture");

        captureAnchor = anchor;
        capturedPaddle = paddle;
        capturedYDirection = bounceUp ? 1f : -1f;
        lastDribbleSpeedIncreaseTime = -999f;
        capturedStartTime = Time.time;

        float paddleVelocityX = capturedPaddle != null ? capturedPaddle.VelocityX : 0f;
        ball.direction = GetDribbleBounceDirection(bounceUp, paddleVelocityX);

        ball.EnterCapturedState();

        Debug.Log("[BallState] Captured");
        LogCapturedDribble("[BallState] Captured Dribble Start");
        LogStartCapturedDribble(source, bounceUp, paddleVelocityX);
        ball.NotifyCaptured();
    }

    public void Tick()
    {
        if (captureAnchor == null)
            return;

        if (UpdateCapturedReleaseState())
            return;

        Vector3 pos = transform.position;
        Vector3 anchorPos = captureAnchor.position;

        float targetX = anchorPos.x + ball.CapturedLocalOffset.x;
        float dribbleSpeed = speedController != null
            ? speedController.CurrentSpeed
            : ball.CapturedDribbleSpeedFallback;

        pos.x = Mathf.Lerp(pos.x, targetX, ball.CapturedXFollowSpeed * Time.deltaTime);
        pos.y += capturedYDirection * dribbleSpeed * Time.deltaTime;

        float topY = ball.CapturedTopBound != null
            ? ball.CapturedTopBound.position.y - ball.actualRadius
            : anchorPos.y + ball.CapturedTopOffset;
        float bottomY = ball.CapturedBottomBound != null
            ? ball.CapturedBottomBound.position.y + ball.actualRadius
            : anchorPos.y + ball.CapturedBottomOffset;

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
        ball.direction = GetDribbleBounceDirection(capturedYDirection > 0f, paddleVelocityX);

        LogMoveCapturedBall(paddleVelocityX);

        LogCapturedDribble($"[BallState] Captured Dribble Speed: {dribbleSpeed}");
    }

    public void ResetCapture()
    {
        captureAnchor = null;
        capturedPaddle = null;
        capturedStartTime = -999f;
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

        if (ball.DebugCaptureRelease)
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

    private void ReleaseCapturedDribble(string reason)
    {
        if (!ball.IsCaptured)
            return;

        PaddleController releasedPaddle = capturedPaddle;

        if (ball.direction.sqrMagnitude < 0.0001f)
            ball.direction = new Vector2(0f, capturedYDirection).normalized;

        ResetCapture();
        ball.ReleaseCapturedStateFromDribble();

        if (ball.DebugCaptureRelease)
            Debug.Log($"[ReleaseCapturedDribble] reason={reason}, time={Time.time:0.00}");

        if (reason == "paddle inactive" && releasedPaddle != null)
            SuppressInactiveCaptureUntilPaddleActive(releasedPaddle);

        ball.NotifyReleased();
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

        TryIncreaseSpeedFromDribble();

        lastCapturedPaddleHitTime = Time.time;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play2D(SoundId.BallBounce);

        LogCapturedDribble(isTopBound
            ? "[CapturedPaddleHit] Top bound hit. Apply paddle bonus."
            : "[CapturedPaddleHit] Bottom bound hit. Apply paddle bonus.");
    }

    private void SuppressInactiveCaptureUntilPaddleActive(PaddleController paddle)
    {
        inactiveCaptureSuppressedPaddle = paddle;
        suppressInactiveCaptureUntilPaddleActive = true;

        if (ball.DebugCaptureRelease)
            Debug.Log($"[CaptureBlocked] reason=paddleReleasedUntilActive, paddle={paddle.name}");
    }

    private void ClearInactiveCaptureSuppression(string reason)
    {
        suppressInactiveCaptureUntilPaddleActive = false;
        inactiveCaptureSuppressedPaddle = null;

        if (ball.DebugCaptureRelease)
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
}
