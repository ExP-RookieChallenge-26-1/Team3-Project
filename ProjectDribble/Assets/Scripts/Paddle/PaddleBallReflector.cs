using Interfaces;
using UnityEngine;

public class PaddleBallReflector : MonoBehaviour, IBallReflector
{
    [Header("Reflect Direction")]
    [SerializeField] private bool reflectUp = true;


    public PaddleData data;

    float maxBounceAngle;
    float paddleWidth;

    public bool ReflectUp => reflectUp;

    void Start()
    {
        if (gameObject.name == "roof_paddle")
            maxBounceAngle = data.outerMaxBounceAngle;
        else
        {
            maxBounceAngle = data.innerMaxBounceAngle;
        }
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

        float offsetAbs = Mathf.Abs(xOffset);

        // 바깥쪽에서 각도가 천천히 커지도록 보정
        float angleRatio = offsetAbs * offsetAbs;

        float targetAngle = Mathf.Lerp(0f, maxBounceAngle, angleRatio);

        Quaternion rotation = Quaternion.Euler(0f, 0f, -xOffset * targetAngle);

        return (rotation * baseDir).normalized;
    }
}
