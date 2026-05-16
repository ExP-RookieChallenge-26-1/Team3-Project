using System;
using UnityEngine;

public class ChargeZone : MonoBehaviour
{
    private bool isDribbling;
    
    public event Action<bool> OnDribblingChanged;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            OnDribblingChanged?.Invoke(true);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            OnDribblingChanged?.Invoke(false);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
