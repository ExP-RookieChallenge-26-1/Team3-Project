using UnityEngine;

public class PaddleVelocityTracker : MonoBehaviour
{
    public float CurrentVelocityX { get; private set; }

    private Vector3 previousPosition;

    void Awake()
    {
        previousPosition = transform.position;
    }

    void LateUpdate()
    {
        if (Time.deltaTime <= 0f)
        {
            CurrentVelocityX = 0f;
            return;
        }

        CurrentVelocityX = (transform.position.x - previousPosition.x) / Time.deltaTime;
        previousPosition = transform.position;
    }
}