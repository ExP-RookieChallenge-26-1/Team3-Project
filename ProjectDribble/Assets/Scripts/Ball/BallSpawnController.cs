using UnityEngine;

public class BallSpawnController : MonoBehaviour
{
    [SerializeField] private BallMovement ballMovement;

    private BallController ballController;
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
    }

    public void InitializeBall(
        Vector2 worldPosition,
        Vector2 startDirection,
        float startSpeed
    )
    {
        lastStartPosition = worldPosition;
        lastStartDirection = startDirection.sqrMagnitude > 0f
            ? startDirection.normalized
            : Vector2.down;
        lastStartSpeed = startSpeed;

        transform.position = worldPosition;

        if (ballController != null)
        {
            ballController.SetBallDirection(lastStartDirection.x, lastStartDirection.y);
        }

        if (ballMovement != null)
        {
            ballMovement.ResetMovementState();
            ballMovement.SetBallSpeed(startSpeed);
        }
    }

    public void ResetBallState()
    {
        InitializeBall(lastStartPosition, lastStartDirection, lastStartSpeed);
    }
}
