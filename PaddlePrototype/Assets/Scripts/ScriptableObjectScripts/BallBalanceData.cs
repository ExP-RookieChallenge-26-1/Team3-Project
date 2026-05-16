using UnityEngine;

namespace ScriptableObjectScripts
{
    

[CreateAssetMenu(menuName = "Game/Ball Balance")]
public class BallBalanceData : ScriptableObject
{
    [Header("Base")]
    public float baseSpeed = 25f;
    public float maxSpeed = 40f;
    public float basePower = 10f;

    [Header("Paddle")]
    public float paddleSpeedIncrease = 3f;
    public float paddlePowerIncrease = 10f;

    [Header("Block")]
    public float blockSpeedDecrease = 6f;
    public float blockPowerDecrease = 10f;

    [Header("Bounce")]
    public float centerPullStrength = 1f;
    public float centerZone = 0.2f;

    public float outsideMaxBounceAngle = 50f;
    public float insideMaxBounceAngle = 40f;
    
}
}