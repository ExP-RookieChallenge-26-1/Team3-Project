using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDefinition", menuName = "ScriptableObjects/StageDefinition")]
public class StageDefinition : ScriptableObject
{
    [Header("Blocks")]
    public StageBlockData blockData;

    [Header("Ceiling / Player")]
    public int ceilingMaxHpOverride = 100;
    public int playerMaxHpOverride = 10;

    [Header("Gauge")]
    public int startGaugeValue = 0;

    [Header("Ball Spawn")]
    public Vector2 ballStartPosition = new Vector2(0,-11.5f);
    public Vector2 ballStartDirection = Vector2.down;
    public float ballStartSpeed = 30f;
}
