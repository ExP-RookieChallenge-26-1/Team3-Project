using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageBlockData", menuName = "Scriptable Objects/StageBlockData")]
public class StageBlockData : ScriptableObject
{
    [System.Serializable]
    public class GrowthDirection
    {
        public string name;
        public Vector2Int direction;
        public int priority = 0;
        public float weight = 1f;
    }

    [System.Serializable]
    public class FixedBlockData
    {
        public Vector2Int cell;
        public float hp = 999;
    }

    [Header("Grid Size")]
    public int width = 7;
    public int height = 18;

    [Header("Flow Block")]
    public float defaultHp = 10;

    [Header("Fixed Blocks")]
    public List<FixedBlockData> fixedBlocks = new();

    [Header("Start Blocks")]
    public List<Vector2Int> startCells = new()
    {
        new Vector2Int(3, 0)
    };

    [Header("Growth Timing")]
    public float spawnInterval = 1f;
    public int minGrowPerTick = 1;
    public int maxGrowPerTick = 2;

    [Header("Row Weight")]
    public float rowWeightMultiplier = 0.2f;

    [Header("Row Priority")]
    public int rowPriorityStep = 0;

    [Header("Connection Rule")]
    public bool onlyGrowFromStartConnectedBlocks = true;

    [Header("Growth Directions")]
    public List<GrowthDirection> directions = new()
    {
        new GrowthDirection
        {
            name = "Down",
            direction = new Vector2Int(0, 1),
            priority = 0,
            weight = 5f
        },
        new GrowthDirection
        {
            name = "Left",
            direction = new Vector2Int(-1, 0),
            priority = 1,
            weight = 1f
        },
        new GrowthDirection
        {
            name = "Right",
            direction = new Vector2Int(1, 0),
            priority = 1,
            weight = 1f
        }
    };
}