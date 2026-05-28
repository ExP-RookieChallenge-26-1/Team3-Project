using UnityEngine;

[CreateAssetMenu(fileName = "BallData", menuName = "Scriptable Objects/BallData")]
public class BallData : ScriptableObject
{
    public float baseSpeed = 30f;
    public float maxSpeed = 55f;
    public float ballDamage = 1f;
    public float outerPaddleSpeedIncrease = 5f;
    public float innerPaddleSpeedIncrease = 5f;
    public float BlockSpeedDecrease = -5f;
    public float DamageMultiplier = 0.2f;
    
}
