using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjectScripts
{
    

[CreateAssetMenu(menuName = "Game/Brick Manager Data")]
public class BrickManagerData : ScriptableObject
{
    [Header("Brick Prefabs")]
    public GameObject brickPrefab;
    public GameObject fixedBrickPrefab;

    [Header("Fixed Bricks")]
    public List<BrickManager.FixedBrickData> fixedBricks = new();

    [Header("Grid")]
    public int rowCount = 15;
    public int columnCount = 7;
    public float cellWidth = 1.5f;
    public float cellHeight = 0.8f;
    public float brickSizeRatio = 0.8f;

    [Header("Map Position")]
    public Vector2 startPosition = new Vector2(-4.5f, 7.5f);

    [Header("Growth Timing")]
    public float spawnInterval = 0.7f;
    public int minGrowPerTick = 1;
    public int maxGrowPerTick = 2;

    [Header("Row Weight")]
    public float rowWeightMultiplier = 0.2f;

    [Header("Row Priority")]
    public int rowPriorityStep = 0;

    [Header("Connection Rule")]
    public bool onlyGrowFromStartConnectedBricks = true;

    [Header("Growth Directions")]
    public List<BrickManager.GrowthDirection> directions = new()
    {
        new BrickManager.GrowthDirection
        {
            name = "Down",
            direction = new Vector2Int(0, 1),
            priority = 1,
            weight = 5f
        },
        new BrickManager.GrowthDirection
        {
            name = "Left",
            direction = new Vector2Int(-1, 0),
            priority = 1,
            weight = 3f
        },
        new BrickManager.GrowthDirection
        {
            name = "Right",
            direction = new Vector2Int(1, 0),
            priority = 1,
            weight = 3f
        }
    };

    [Header("Start Bricks")]
    public List<Vector2Int> startCells = new()
    {
        new Vector2Int(0, 0),
        new Vector2Int(6, 0)
    };
}
}