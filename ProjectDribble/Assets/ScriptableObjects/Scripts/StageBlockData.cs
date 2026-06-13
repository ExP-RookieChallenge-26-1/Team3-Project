using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageBlockData", menuName = "Scriptable Objects/StageBlockData")]
public class StageBlockData : ScriptableObject
{
    public enum StemDirectionType
    {
        Up,
        Down,
        Left,
        Right,
        DownLeft,
        DownRight
    }

    [System.Serializable]
    public class GrowthDirection
    {
        public string name;
        public Vector2Int direction;
        public int priority = 0;
        public float weight = 1f;
    }

    [System.Serializable]
    public class StemDirectionOption
    {
        public StemDirectionType direction;
        public int priority = 0;
        public float weight = 1f;
    }

    [System.Serializable]
    public class FixedBlockData
    {
        public Vector2Int cell;
        public float hp = 999;
    }

    [System.Serializable]
    public class GrowthStemData
    {
        public Vector2Int startCoord;
        public int minX;
        public int maxX;
        public int maxLength;
        public int width = 1;
        public float growWeight = 1f;
        public int maxBlocksPerRow = 2;

        [Header("Direction Options")]
        public StemDirectionOption[] directionOptions = new StemDirectionOption[0];

        [Header("Stem Timing")]
        public bool enabled = true;
        public float spawnInterval = -1f;
        public int minGrowPerTick = -1;
        public int maxGrowPerTick = -1;
        public float initialDelay = 0f;

        public IEnumerable<GrowthDirection> GetPreferredDirections()
        {
            if (directionOptions != null && directionOptions.Length > 0)
            {
                for (int i = 0; i < directionOptions.Length; i++)
                {
                    StemDirectionOption option = directionOptions[i];

                    if (option == null)
                        continue;

                    yield return CreateGrowthDirection(option.direction, option.priority, option.weight);
                }

                yield break;
            }

            yield return CreateGrowthDirection(StemDirectionType.Down, 0, 1f);
            yield return CreateGrowthDirection(StemDirectionType.Left, 0, 1f);
            yield return CreateGrowthDirection(StemDirectionType.Right, 0, 1f);
            yield return CreateGrowthDirection(StemDirectionType.DownLeft, 0, 1f);
            yield return CreateGrowthDirection(StemDirectionType.DownRight, 0, 1f);
        }
    }

    private static GrowthDirection CreateGrowthDirection(
        StemDirectionType directionType,
        int priority,
        float weight
    )
    {
        return new GrowthDirection
        {
            name = directionType.ToString(),
            direction = ToVector2Int(directionType),
            priority = priority,
            weight = weight
        };
    }

    public static Vector2Int ToVector2Int(StemDirectionType directionType)
    {
        switch (directionType)
        {
            case StemDirectionType.Up:
                return new Vector2Int(0, -1);
            case StemDirectionType.Down:
                return new Vector2Int(0, 1);
            case StemDirectionType.Left:
                return new Vector2Int(-1, 0);
            case StemDirectionType.Right:
                return new Vector2Int(1, 0);
            case StemDirectionType.DownLeft:
                return new Vector2Int(-1, 1);
            case StemDirectionType.DownRight:
                return new Vector2Int(1, 1);
            default:
                return Vector2Int.zero;
        }
    }

    [Header("Grid Size")]
    public int width = 7;
    public int height = 18;

    [Header("Flow Block")]
    public float defaultHp = 10;

    [Header("Growth Mode")]
    public bool disableGrowth = false;
    public bool useStemGrowth = false;
    public bool UseStemGrowth => useStemGrowth;

    [Header("Stem Growth")]
    public GrowthStemData[] growthStems = new GrowthStemData[0];
    public bool HasStemGrowthData => growthStems != null && growthStems.Length > 0;

    [Header("Fixed Blocks")]
    public List<FixedBlockData> fixedBlocks = new();

    [Header("Normal Blocks")]
    public List<Vector2Int> normalBlocks = new();

    [Header("Start Blocks")]
    public bool respawnMissingStartBlocks = true;
    public List<Vector2Int> startCells = new()
    {
        new Vector2Int(3, 0)
    };

    [Header("Tutorial")]
    public List<Vector2Int> tutorialTargetBlocks = new();

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
        },
        new GrowthDirection
        {
            name = "Up",
            direction = new Vector2Int(0, -1),
            priority = 1,
            weight = 1f
        }
    };
}
