using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BallRecallInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BallRespawner ballRespawner;

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

    private void Update()
    {
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
        startPointerPosition = position;

        isPointerDown = true;
        isDownSwipeHolding = false;
        hasRecalledThisSwipe = false;

        holdTimer = 0f;

        Debug.Log("BeginPointer: " + position);
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

        Debug.Log("Holding Down Swipe: " + holdTimer);

        if (holdTimer >= holdDuration)
        {
            RecallBall();
            hasRecalledThisSwipe = true;
        }
    }

    private void CancelHold()
    {
        isDownSwipeHolding = false;
        holdTimer = 0f;
    }

    private void EndPointer()
    {
        isPointerDown = false;
        isDownSwipeHolding = false;
        hasRecalledThisSwipe = false;

        holdTimer = 0f;

        Debug.Log("EndPointer");
    }

    private void RecallBall()
    {
        Debug.Log("Recall Ball");

        if (ballRespawner == null)
        {
            Debug.LogWarning("BallRespawner is null");
            return;
        }

        ballRespawner.RecallBallToPaddle();
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
