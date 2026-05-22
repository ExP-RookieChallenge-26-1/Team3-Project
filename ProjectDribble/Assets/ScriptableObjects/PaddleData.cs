using UnityEngine;

[CreateAssetMenu(fileName = "PaddleData", menuName = "Scriptable Objects/PaddleData")]
public class PaddleData : ScriptableObject
{
    public float moveSpeed = 50f;
    public float paddleWidth = 0.7f; // 패들 부모 객체가 쓰는 값
    public float maxBounceAngle = 50f;
    public float collisionPaddleWidth = 3f; // 패들의 각 부분이 쓰는 값
}
