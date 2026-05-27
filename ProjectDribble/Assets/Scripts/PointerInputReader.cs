using UnityEngine;
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
                return true;
            }
        }

        // 에디터 / PC 마우스
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }
}