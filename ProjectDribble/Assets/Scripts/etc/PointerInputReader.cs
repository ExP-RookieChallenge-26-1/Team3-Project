using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

[DefaultExecutionOrder(-10)]
public class PointerInputReader : MonoBehaviour
{
    public bool IsPressed { get; private set; }
    public Vector2 ScreenPosition { get; private set; }

    public bool WasPressedThisFrame { get; private set; }
    public bool WasReleasedThisFrame { get; private set; }

    private readonly List<RaycastResult> raycastResults = new();
    private ButtonControl dragButton;
    private Vector2Control dragPosition;

    private void Update()
    {
        GameManager gameManager = GameManager.Instance;
        bool wasPressed = IsPressed;
        if (gameManager != null && gameManager.IsPlayerInputBlocked)
        {
            dragButton = null;
        }
        else if (dragButton == null || !dragButton.isPressed)
        {
            dragButton = null;
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && touch.press.isPressed)
                TryBeginDrag(touch.press, touch.position, touch.touchId.ReadValue());
            else if (Mouse.current != null)
                TryBeginDrag(Mouse.current.leftButton, Mouse.current.position, -1);
        }

        IsPressed = dragButton != null;
        ScreenPosition = IsPressed ? dragPosition.ReadValue() : Vector2.zero;

        WasPressedThisFrame = IsPressed && !wasPressed;
        WasReleasedThisFrame = !IsPressed && wasPressed;
        if (WasPressedThisFrame || WasReleasedThisFrame)
            gameManager?.OnGameplayPointerChanged(IsPressed);
    }

    private void TryBeginDrag(ButtonControl button, Vector2Control position, int pointerId)
    {
        if (!button.wasPressedThisFrame || !button.isPressed ||
            IsPointerOverUI(position.ReadValue(), pointerId))
            return;

        dragButton = button;
        dragPosition = position;
    }

    private bool IsPointerOverUI(Vector2 position, int pointerId)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        var pointer = new PointerEventData(eventSystem) { position = position, pointerId = pointerId };
        eventSystem.RaycastAll(pointer, raycastResults);
        foreach (RaycastResult hit in raycastResults)
        {
            if (hit.module is GraphicRaycaster)
                return true;
        }

        return false;
    }

    private void OnDisable()
    {
        dragButton = null;
        dragPosition = null;
        IsPressed = false;
    }
}
