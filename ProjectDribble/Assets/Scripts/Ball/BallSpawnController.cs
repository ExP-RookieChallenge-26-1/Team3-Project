using UnityEngine;

public class BallSpawnController : MonoBehaviour
{
    [SerializeField] private BallMovement ballMovement;

    private BallController ballController;
    private BallSpeedController ballSpeedController;
    private Vector2 lastStartPosition;
    private Vector2 lastStartDirection = Vector2.down;
    private float lastStartSpeed;

    private void Awake()
    {
        if (ballMovement == null)
        {
            ballMovement = GetComponent<BallMovement>();
        }

        ballController = GetComponent<BallController>();
        ballSpeedController = GetComponent<BallSpeedController>();
    }

    public void InitializeBall(
        Vector2 worldPosition,
        Vector2 startDirection
    )
    {
        lastStartPosition = worldPosition;
        lastStartDirection = startDirection.sqrMagnitude > 0f
            ? startDirection.normalized
            : Vector2.down;

        transform.position = worldPosition;

        if (ballController != null)
        {
            ballController.SetBallDirection(lastStartDirection.x, lastStartDirection.y);
        }

        if (ballMovement != null)
        {
            ballMovement.ResetMovementState();
            ballMovement.SetBallSpeed(); //base speed로
        }

        ballSpeedController?.ResetToBaseSpeed();
    }

    
    public void ResetBallState()
    {
        InitializeBall(lastStartPosition, lastStartDirection);
    }
}
