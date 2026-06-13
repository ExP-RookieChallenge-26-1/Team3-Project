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
    Stage3 = 3
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

    [Header("Tutorial")]
    public bool isTutorialStage;
    public TutorialStageId tutorialStageId = TutorialStageId.None;
}
