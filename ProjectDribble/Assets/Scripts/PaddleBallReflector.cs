using Interfaces;
using UnityEngine;

public class PaddleBallReflector : MonoBehaviour, IBallReflector
{
    [Header("Reflect Direction")]
    [SerializeField] private bool reflectUp = true;


    public PaddleData data;

    float maxBounceAngle;
    float paddleWidth;

    void Start()
    {
        maxBounceAngle = data.maxBounceAngle;
        paddleWidth = data.collisionPaddleWidth;
    }

    public Vector2 GetReflectDirection(
        BallController ball,
        RaycastHit2D hit,
        Vector2 incomingDirection
    )
    {
        float xOffset =
            (ball.transform.position.x - transform.position.x) / (paddleWidth / 2f);

        xOffset = Mathf.Clamp(xOffset, -1f, 1f);

        Vector2 baseDir = reflectUp ? Vector2.up : Vector2.down;

        float targetAngle = Mathf.Lerp(0f, maxBounceAngle, Mathf.Abs(xOffset));

        Quaternion rotation = Quaternion.Euler(0f, 0f, -xOffset * targetAngle);

        return (rotation * baseDir).normalized;
    }
}