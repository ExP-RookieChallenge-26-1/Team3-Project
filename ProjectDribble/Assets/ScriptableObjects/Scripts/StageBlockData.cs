using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class CeilingSegmentVisualProfile
{
    [Header("Glow")]
    [FormerlySerializedAs("pulseColor")]
    [FormerlySerializedAs("glitchColor")]
    public Color glowColor = Color.green;
    [Min(0f)] public float scaleMultiplier = 1.12f;
    [Range(0f, 1f)] public float glowAlphaMin = 0.15f;
    [FormerlySerializedAs("connectedAlpha")]
    [Range(0f, 1f)] public float glowAlphaMax = 0.65f;
    [FormerlySerializedAs("glowPulseSpeed")]
    [FormerlySerializedAs("pulseSpeed")]
    [Min(0f)] public float pulseSpeed = 1f;
    [FormerlySerializedAs("glowPhaseOffset")]
    [FormerlySerializedAs("phaseOffset")]
    public float phaseOffset;
    [FormerlySerializedAs("glowAlphaCurve")]
    [FormerlySerializedAs("pulseCurve")]
    public AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Disconnected")]
    [FormerlySerializedAs("disconnectedColor")]
    public Color disconnectedGlowColor = new Color(0.45f, 0.5f, 0.42f, 1f);
    [FormerlySerializedAs("disconnectedAlpha")]
    [Range(0f, 1f)] public float disconnectedGlowAlpha = 0.1f;
}

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
        [Tooltip("Inspector label for this growth direction. Runtime logic does not read this value.")]
        public string name;
        [Tooltip("Legacy growth direction vector. Used by startCells/directions based growth.")]
        public Vector2Int direction;
        [Tooltip("Lower priority values are selected before higher priority values.")]
        public int priority = 0;
        [Tooltip("Weighted random selection value. Values less than or equal to 0 are ignored.")]
        public float weight = 1f;
    }

    [System.Serializable]
    public class StemDirectionOption
    {
        [Tooltip("Stem growth direction option used when useStemGrowth is true.")]
        public StemDirectionType direction;
        [Tooltip("Lower priority values are selected before higher priority values for this stem.")]
        public int priority = 0;
        [Tooltip("Weighted random selection value for this stem direction. Values less than or equal to 0 are ignored.")]
        public float weight = 1f;
    }

    [System.Serializable]
    public class FixedBlockData
    {
        [Tooltip("Grid coordinate for a fixed block. Fixed blocks are excluded from Flow growth/connection checks.")]
        public Vector2Int cell;
        [Tooltip("Fixed block HP. Fixed blocks are not destroyed by ball damage, but can be removed by laser.")]
        public float hp = 999;
    }

    [System.Serializable]
    public class GrowthStemData
    {
        [Header("Stem Visual")]
        [Tooltip("Optional Flow block prefab used only by this stem. If empty, BlockPool's default Flow prefab is used.")]
        public BlockCell flowBlockPrefabOverride;

        [Header("Stem Layout")]
        [Tooltip("Ceiling segment index this stem grows from. -1 keeps legacy startCoord based stem behavior.")]
        public int ceilingSegmentIndex = -1;
        [Tooltip("Stem growth start coordinate. A Flow block at this coordinate becomes the start-connected root for this stem.")]
        public Vector2Int startCoord;
        [Tooltip("Minimum grid X coordinate this stem can grow within.")]
        public int minX;
        [Tooltip("Maximum grid X coordinate this stem can grow within.")]
        public int maxX;
        [Tooltip("Maximum growth length from startCoord.y. The valid Y range is startCoord.y through startCoord.y + maxLength.")]
        public int maxLength;

        [Header("Deprecated / Unused Candidates")]
        [Tooltip("Currently unused by runtime code. Kept for StageBlockData asset compatibility; candidate for future removal or reconnecting to a feature.")]
        public int width = 1;

        [Header("Stem Growth")]
        [Tooltip("Stem-level growth weight. Values less than or equal to 0 prevent this stem from producing growth candidates.")]
        public float growWeight = 1f;
        [Tooltip("Maximum number of Flow blocks owned by this stem that may exist in one row.")]
        public int maxBlocksPerRow = 2;

        [Header("Direction Options")]
        [Tooltip("Stem growth direction options. Used in stem growth mode instead of the legacy directions list.")]
        public StemDirectionOption[] directionOptions = new StemDirectionOption[0];

        [Header("Stem Timing")]
        [Tooltip("Initial stem data toggle. Runtime systems can also disable a stem after a ceiling segment is destroyed.")]
        public bool enabled = true;
        [Tooltip("If greater than 0, this stem uses this spawn interval. If 0 or negative, it falls back to StageBlockData.spawnInterval.")]
        public float spawnInterval = -1f;
        [Tooltip("If 0 or greater, this stem overrides the global minGrowPerTick. If negative, it falls back to StageBlockData.minGrowPerTick.")]
        public int minGrowPerTick = -1;
        [Tooltip("If 0 or greater, this stem overrides the global maxGrowPerTick. If negative, it falls back to StageBlockData.maxGrowPerTick.")]
        public int maxGrowPerTick = -1;
        [Tooltip("Delay before this stem starts ticking. Negative values are clamped to 0 at runtime.")]
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

    [Header("Grid")]
    [Tooltip("Grid width in cells.")]
    public int width = 7;
    [Tooltip("Grid height in cells.")]
    public int height = 18;

    [Header("Block Layout")]
    [Tooltip("Default HP used for Flow blocks and Normal blocks. Fixed blocks use their own hp value.")]
    public float defaultHp = 10;

    [Header("Growth Mode")]
    [Tooltip("Turns off the growth loop only. Initial normal, fixed, and start block spawning is not disabled by this option.")]
    public bool disableGrowth = false;
    [Tooltip("If true, use growthStems based stem growth. If false, use legacy startCells/directions based growth.")]
    public bool useStemGrowth = false;
    public bool UseStemGrowth => useStemGrowth;

    [Header("Stem Growth")]
    [Tooltip("Stem growth settings list. Used only when useStemGrowth is true.")]
    public GrowthStemData[] growthStems = new GrowthStemData[0];
    public bool HasStemGrowthData => growthStems != null && growthStems.Length > 0;

    [Header("Fixed Blocks")]
    [Tooltip("Fixed block coordinates and HP. Fixed blocks are not destroyed by ball damage, can be removed by laser, and are excluded from Flow growth/connection checks.")]
    public List<FixedBlockData> fixedBlocks = new();

    [Header("Normal Blocks")]
    [Tooltip("Basic brick-breaker block coordinates. Normal blocks are destroyable by ball damage, removable by laser, and excluded from Flow growth/connection checks.")]
    public List<Vector2Int> normalBlocks = new();

    [Header("Runtime / Respawn Options")]
    [Tooltip("If true, missing startCells or growthStems.startCoord Flow blocks are recreated during growth ticks. If false, destroying a start block can stop that legacy growth or stem.")]
    public bool respawnMissingStartBlocks = true;

    [Header("Legacy Growth")]
    [Tooltip("Legacy growth start coordinates. Used only when useStemGrowth is false. Stem mode uses growthStems[].startCoord instead.")]
    public List<Vector2Int> startCells = new()
    {
        new Vector2Int(3, 0)
    };

    [Header("Growth Timing")]
    [Tooltip("Global growth interval. Legacy growth uses this directly; stem growth uses it as fallback when a stem spawnInterval is 0 or negative.")]
    public float spawnInterval = 1f;
    [Tooltip("Global minimum blocks to grow per tick. Legacy growth uses this directly; stem growth uses it as fallback when a stem minGrowPerTick is negative.")]
    public int minGrowPerTick = 1;
    [Tooltip("Global maximum blocks to grow per tick. Legacy growth uses this directly; stem growth uses it as fallback when a stem maxGrowPerTick is negative.")]
    public int maxGrowPerTick = 2;

    [Header("Legacy Growth Row Bias")]
    [Tooltip("Legacy growth only. Added to direction weight based on target row; stem growth does not use this.")]
    public float rowWeightMultiplier = 0.2f;
    [Tooltip("Legacy growth only. Added to direction priority based on target row; stem growth does not use this.")]
    public int rowPriorityStep = 0;

    [Header("Connection Rules")]
    [Tooltip("If true, only Flow blocks connected to startCells or growthStems.startCoord can be growth parents. This does not directly test ceiling connection; it prevents detached Flow pieces from continuing to grow.")]
    public bool onlyGrowFromStartConnectedBlocks = true;

    [Header("Legacy Growth Directions")]
    [Tooltip("Legacy growth direction list. Used when useStemGrowth is false. Stem growth uses growthStems[].directionOptions instead.")]
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
