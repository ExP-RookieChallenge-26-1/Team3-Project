using UnityEngine;

public class BallSpeedColorController : MonoBehaviour
{
    [SerializeField] private BallData data;
    [SerializeField] private BallSpeedController speedController;
    [SerializeField] private BallMovement ballMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void Update()
    {
        if (data == null || !data.useSpeedColorChange || spriteRenderer == null)
            return;

        float currentSpeed = GetCurrentSpeed();
        spriteRenderer.color = GetColorForSpeed(currentSpeed);
    }

    private void AutoAssignReferences()
    {
        if (speedController == null)
            speedController = GetComponent<BallSpeedController>();

        if (ballMovement == null)
            ballMovement = GetComponent<BallMovement>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (data == null && speedController != null)
            data = speedController.data;

        if (data == null && ballMovement != null)
            data = ballMovement.data;

        if (data == null)
        {
            BallController ballController = GetComponent<BallController>();
            if (ballController != null)
                data = ballController.data;
        }
    }

    private float GetCurrentSpeed()
    {
        if (speedController != null)
            return speedController.CurrentSpeed;

        if (ballMovement != null)
            return ballMovement.speed;

        return 0f;
    }

    private Color GetColorForSpeed(float currentSpeed)
    {
        float startSpeed = data.speedColorStartSpeed;
        float endSpeed = data.speedColorEndSpeed;

        if (endSpeed <= startSpeed)
            return currentSpeed <= startSpeed ? data.normalSpeedColor : data.maxSpeedColor;

        if (currentSpeed <= startSpeed)
            return data.normalSpeedColor;

        if (currentSpeed >= endSpeed)
            return data.maxSpeedColor;

        float t = Mathf.InverseLerp(startSpeed, endSpeed, currentSpeed);
        return Color.Lerp(data.normalSpeedColor, data.maxSpeedColor, t);
    }
}
