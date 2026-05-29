using UnityEngine;

public class BallRespawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform upperPaddle;
    [SerializeField] private Transform lowerPaddle;
    [SerializeField] private BallSpawnController ballSpawnController;
    [Header("Recall Setting")]
    [SerializeField] private float recallStartSpeed = 8f;
    [SerializeField] private Vector2 recallStartDirection = Vector2.up;
    
    [SerializeField] private BallController ballController;
    [SerializeField] private BallMovement ballMovement;

    

    public void RecallBallToPaddle()
    {
        Vector2 recallPosition = GetPaddleCenterPosition();

        ballSpawnController.InitializeBall(
            recallPosition,
            recallStartDirection
        );
    }

    private Vector2 GetPaddleCenterPosition()
    {
        if (upperPaddle == null || lowerPaddle == null)
        {
            return transform.position;
        }

        Vector2 upperPos = upperPaddle.position;
        Vector2 lowerPos = lowerPaddle.position;

        return (upperPos + lowerPos) * 0.5f;
    }
}