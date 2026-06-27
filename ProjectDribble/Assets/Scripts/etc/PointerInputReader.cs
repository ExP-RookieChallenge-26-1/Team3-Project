using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointerInputReader : MonoBehaviour
{
    public bool IsPressed { get; private set; }
    public Vector2 ScreenPosition { get; private set; }

    public bool WasPressedThisFrame { get; private set; }
    public bool WasReleasedThisFrame { get; private set; }

    private bool wasPressedLastFrame;

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.IsPlayerInputBlocked)
        {
            IsPressed = false;
            ScreenPosition = Vector2.zero;
            WasPressedThisFrame = false;
            WasReleasedThisFrame = wasPressedLastFrame;
            wasPressedLastFrame = false;
            return;
        }

        IsPressed = TryReadPointer(out Vector2 screenPosition);
        ScreenPosition = screenPosition;

        WasPressedThisFrame = IsPressed && !wasPressedLastFrame;
        WasReleasedThisFrame = !IsPressed && wasPressedLastFrame;

        wasPressedLastFrame = IsPressed;
    }

    private bool TryReadPointer(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        // 모바일 터치
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.isPressed)
            {
                screenPosition = touch.position.ReadValue();
                return !IsPointerOverUI(touch.touchId.ReadValue());
            }
        }

        // 에디터 / PC 마우스
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return !IsPointerOverUI();
        }

        return false;
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
