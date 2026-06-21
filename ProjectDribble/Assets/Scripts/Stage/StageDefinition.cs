using ScriptableObjects;
using UnityEngine;

public enum CeilingSegmentMode
{
    ThreeSegments = 0,
    OneSegment = 1,
    TwoSegments = 2
}

public enum TutorialStageId
{
    None = 0,
    Stage1 = 1,
    Stage2 = 2,
    Stage3 = 3,
    Stage4 = 4,
    Stage5 = 5,
    Stage6 = 6
}

[CreateAssetMenu(fileName = "StageDefinition", menuName = "ScriptableObjects/StageDefinition")]
public class StageDefinition : ScriptableObject
{
    [Header("Blocks")]
    public StageBlockData blockData;

    [Header("Ceiling / Player")]
    public CeilingSegmentMode ceilingSegmentMode = CeilingSegmentMode.ThreeSegments;
    public int ceilingMaxHpOverride = 100;
    public int playerMaxHpOverride = 10;

    [Header("Gauge")]
    public int startGaugeValue = 0;

    [Header("Ball Spawn")]
    public Vector2 ballStartPosition = new Vector2(0,-11.5f);
    public Vector2 ballStartDirection = Vector2.down;
    public float ballStartSpeed = 30f;

    [Header("Ball Tuning Override")]
    public bool overrideBallTuning;
    [Min(0.01f)] public float maxSpeedOverride = 45f;
    [Min(0f)] public float maxDamageOverride = 5f;
    [Min(0f)] public float speedGainMultiplierOverride = 0.5f;
    [Min(0f)] public float powerGainMultiplierOverride = 0.5f;

    [Header("Tutorial")]
    public bool isTutorialStage;
    public TutorialStageId tutorialStageId = TutorialStageId.None;

    private void OnValidate()
    {
        maxSpeedOverride = Mathf.Max(0.01f, maxSpeedOverride);
        maxDamageOverride = Mathf.Max(0f, maxDamageOverride);
        speedGainMultiplierOverride = Mathf.Max(0f, speedGainMultiplierOverride);
        powerGainMultiplierOverride = Mathf.Max(0f, powerGainMultiplierOverride);
    }
}
