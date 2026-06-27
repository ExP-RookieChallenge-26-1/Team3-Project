using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BallRecallInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BallRespawner ballRespawner;
    [SerializeField] private BallController ballController;

    [Header("Swipe Setting")]
    [SerializeField] private float minSwipeDistance = 80f;
    [SerializeField] private float verticalDominance = 1.5f;

    [Header("Hold Setting")]
    [SerializeField] private float holdDuration = 0.4f;

    private Vector2 startPointerPosition;

    private bool isPointerDown;
    private bool isDownSwipeHolding;
    private bool hasRecalledThisSwipe;

    private float holdTimer;

    private void Awake()
    {
        if (ballRespawner == null)
            ballRespawner = FindAnyObjectByType<BallRespawner>();

        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();
    }

    private void Update()
    {
        if (!CanProcessRecallInput())
        {
            CancelHold();
            isPointerDown = false;
            hasRecalledThisSwipe = false;
            return;
        }

        HandleTouchInput();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandlePointerInput();
#endif
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            if (IsPointerOverUI(touch.touchId.ReadValue()))
                return;

            BeginPointer(touch.position.ReadValue());
        }

        if (touch.press.isPressed)
        {
            if (IsPointerOverUI(touch.touchId.ReadValue()))
            {
                CancelHold();
                return;
            }

            MovePointer(touch.position.ReadValue());
        }

        if (touch.press.wasReleasedThisFrame)
        {
            EndPointer();
        }
    }

    private void HandlePointerInput()
    {
        if (Pointer.current == null)
            return;

        Vector2 currentPosition = Pointer.current.position.ReadValue();

        if (Pointer.current.press.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            BeginPointer(currentPosition);
        }

        if (Pointer.current.press.isPressed)
        {
            if (IsPointerOverUI())
            {
                CancelHold();
                return;
            }

            MovePointer(currentPosition);
        }

        if (Pointer.current.press.wasReleasedThisFrame)
        {
            EndPointer();
        }
    }

    private void BeginPointer(Vector2 position)
    {
        FeedbackManager.Instance?.StopRecallHoldFeedback();
        startPointerPosition = position;

        isPointerDown = true;
        isDownSwipeHolding = false;
        hasRecalledThisSwipe = false;

        holdTimer = 0f;
    }

    private void MovePointer(Vector2 currentPosition)
    {
        if (!isPointerDown)
            return;

        if (hasRecalledThisSwipe)
            return;

        Vector2 delta = currentPosition - startPointerPosition;

        bool isDownSwipe = delta.y < -minSwipeDistance;
        bool isMostlyVertical = Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * verticalDominance;

        if (isDownSwipe && isMostlyVertical)
        {
            HoldDownSwipe();
        }
        else
        {
            CancelHold();
        }
    }

    private void HoldDownSwipe()
    {
        isDownSwipeHolding = true;
        holdTimer += Time.deltaTime;

        float progress = holdDuration > 0f ? holdTimer / holdDuration : 1f;
        FeedbackManager.Instance?.StartRecallHoldFeedback(progress);

        if (holdTimer >= holdDuration)
        {
            RecallBall();
            hasRecalledThisSwipe = true;
        }
    }

    private void CancelHold()
    {
        if (isDownSwipeHolding)
            FeedbackManager.Instance?.StopRecallHoldFeedback();

        isDownSwipeHolding = false;
        holdTimer = 0f;
    }

    private void EndPointer()
    {
        FeedbackManager.Instance?.StopRecallHoldFeedback();
        isPointerDown = false;
        isDownSwipeHolding = false;
        hasRecalledThisSwipe = false;

        holdTimer = 0f;
    }

    private void OnDisable()
    {
        FeedbackManager.Instance?.StopRecallHoldFeedback();
    }

    private void RecallBall()
    {
        FeedbackManager.Instance?.StopRecallHoldFeedback();

        if (ballRespawner == null)
        {
            Debug.LogWarning("BallRespawner is null");
            return;
        }

        ballRespawner.RecallBallToPaddle();
    }

    private bool CanProcessRecallInput()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null ||
            !gameManager.IsGameStarted ||
            gameManager.IsPaused ||
            gameManager.IsPausedByTutorial ||
            gameManager.IsPlayerInputBlocked ||
            gameManager.IsEnding)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
            return false;

        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();

        return ballController != null &&
               ballController.gameObject.activeInHierarchy &&
               !ballController.IsCaptured;
    }

    private bool IsPointerOverUI(int pointerId = -1)
    {
        if (EventSystem.current == null)
            return false;

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
