using System;
using UnityEngine;

public class ChargeZone : MonoBehaviour
{
    private bool isDribbling;

    public event Action<bool> OnDribblingChanged;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ball"))
            return;

        SetDribbling(true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Ball"))
            return;

        SetDribbling(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Ball"))
            return;

        SetDribbling(false);
    }

    private void SetDribbling(bool value)
    {
        if (isDribbling == value)
            return;

        isDribbling = value;
        

        OnDribblingChanged?.Invoke(isDribbling);
    }
}