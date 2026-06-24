using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public event System.Action OnNormalBlocksCleared;
    public event System.Action<BlockCell> OnFixedBlockHitByBall;
    public event System.Action<BlockCell> OnFixedBlockDestroyedByLaser;

    // Legacy compatibility event. New tutorial code should use OnNormalBlocksCleared.
    // Candidate for removal after all old tutorial target block usages are gone.
    public event System.Action OnTutorialTargetBlocksCleared;

    private struct GrowthCandidate
    {
        public Vector2Int cell;
        public int priority;
        public float weight;
        public int stemIndex;

        public GrowthCandidate(Vector2Int cell, int priority, float weight, int stemIndex = -1)
        {
            this.cell = cell;
            this.priority = priority;
            this.weight = weight;
            this.stemIndex = stemIndex;
        }
    }

    private class StemGrowthRuntimeState
    {
        public int stemIndex;
        public float timer;
        public bool enabled;
    }

    [Header("Stage Data")]
    [SerializeField] private StageBlockData data;

    [Header("Grid Area")]
    [SerializeField] private Transform gridArea;

    [Header("References")]
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private BallController ballController;
    [SerializeField] private CeilingManager ceilingManager;

    [Header("Stem Danger Visual")]
    [SerializeField] private int dangerWarningRows = 7;

    [Header("Glitch Stage By Row")]
    [SerializeField] private int glitchStage2RowsFromBottom = 8;
    [SerializeField] private int glitchStage3RowsFromBottom = 4;

    [Header("Ball Spawn Safety")]
    [SerializeField] private float ballSpawnSafetyMargin = 0.25f;

    [SerializeField] private GaugeManager gaugeManager;
    private bool[,] occupied;
    private bool[,] fixedOccupied;
    private BlockType[,] blockTypes;
    private int[,] stemOwner;

    private float cellWidth;
    private float cellHeight;

    private Coroutine growRoutine;
    private readonly List<StemGrowthRuntimeState> stemGrowthStates = new();
    private readonly CeilingSegmentVisualProfile fallbackGlowProfile = new();
    private bool normalBlocksClearedNotified;
    private bool useCeilingForCurrentStage = true;

    private static readonly Vector2Int[] StemConnectionDirections =
    {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1)
    };

    private void Awake()
    {
        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();

        if (ceilingManager == null)
            ceilingManager = FindAnyObjectByType<CeilingManager>();
    }

    private void Start()
    {
        if (data != null && occupied == null)
            InitializeStageBlocks(data);
    }

    public void InitializeStageBlocks(StageBlockData stageData)
    {
        InitializeStageBlocks(stageData, true);
    }

    public void InitializeStageBlocks(StageBlockData stageData, bool useCeiling)
    {
        if (stageData == null)
        {
            Debug.LogWarning("BlockManager: StageBlockData is null. Block grid was not initialized.");
            return;
        }

        data = stageData;
        useCeilingForCurrentStage = useCeiling;
        Debug.Log(
            $"BlockManager: InitializeStageBlocks data={data.name}, size={data.width}x{data.height}, " +
            $"fixed={data.fixedBlocks?.Count ?? 0}, normal={data.normalBlocks?.Count ?? 0}, " +
            $"stems={data.growthStems?.Length ?? 0}, startCells={data.startCells?.Count ?? 0}, " +
            $"useStemGrowth={data.UseStemGrowth}."
        );
        ValidateCeilingStemConfiguration();
        ResetBlocks();
        StartGrowth();
    }

    public void ResetBlocks()
    {
        if (data == null)
        {
            Debug.LogWarning("BlockManager: Cannot reset blocks without StageBlockData.");
            return;
        }

        if (blockPool == null)
        {
            Debug.LogWarning("BlockManager: Cannot reset blocks because BlockPool is missing.");
            return;
        }

        StopGrowth();
        ClearAllSpawnedBlocks();

        CreateGrid();
        ResetNormalBlockTracking();

        blockPool.CreatePool(
            data.width,
            data.height,
            GridToWorld,
            GetCellSize,
            this
        );

        SpawnFixedBlocks();
        SpawnNormalBlocks();
        SpawnStartBlocks();
        CheckNormalBlocksCleared();
    }

    public void StartGrowth()
    {
        StopGrowth();

        if (data == null || data.disableGrowth)
            return;

        if (data.UseStemGrowth)
            InitializeStemGrowthStates();

        growRoutine = StartCoroutine(GrowRoutine());
    }

    public void StopGrowth()
    {
        if (growRoutine != null)
        {
            StopCoroutine(growRoutine);
            growRoutine = null;
        }
    }

    private void CreateGrid()
    {
        if (data == null)
            return;

        occupied = new bool[data.width, data.height];
        fixedOccupied = new bool[data.width, data.height];
        blockTypes = new BlockType[data.width, data.height];
        stemOwner = new int[data.width, data.height];

        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                stemOwner[x, y] = -1;
                blockTypes[x, y] = BlockType.Empty;
            }
        }
    }

    private void CalculateGridSize()
    {
        if (data == null || gridArea == null)
        {
            cellWidth = 1f;
            cellHeight = 1f;
            return;
        }

        Vector3 areaSize = gridArea.lossyScale;

        cellWidth = areaSize.x / data.width;
        cellHeight = areaSize.y / data.height;
    }

    public Vector3 GridToWorld(Vector2Int coord)
    {
        CalculateGridSize();

        Vector3 center = gridArea.position;

        float left = center.x - gridArea.lossyScale.x * 0.5f;
        float top = center.y + gridArea.lossyScale.y * 0.5f;

        float x = left + cellWidth * (coord.x + 0.5f);
        float y = top - cellHeight * (coord.y + 0.5f);

        return new Vector3(x, y, center.z);
    }

    public Vector2 GetCellSize()
    {
        CalculateGridSize();
        //return new Vector2(cellWidth, cellHeight);
        return new Vector2(1f, 1f);
    }

    private IEnumerator GrowRoutine()
    {
        while (true)
        {
            if (data.UseStemGrowth)
            {
                UpdateStemGrowth();
                yield return null;
            }
            else
            {
                UpdateLegacyGrowth();
                yield return new WaitForSeconds(data.spawnInterval);
            }
        }
    }

    private void UpdateLegacyGrowth()
    {
        RespawnMissingStartBlocks();

        int growCount = Random.Range(
            data.minGrowPerTick,
            data.maxGrowPerTick + 1
        );

        for (int i = 0; i < growCount; i++)
        {
            List<GrowthCandidate> candidates = GetLegacyGrowthCandidates();

            if (candidates.Count <= 0)
                break;

            GrowthCandidate selected = PickByPriorityAndWeight(candidates);
            SpawnBlock(selected.cell, data.defaultHp, false, selected.stemIndex);
        }
    }

    #region Stem Growth

    private void InitializeStemGrowthStates()
    {
        stemGrowthStates.Clear();

        if (data.growthStems == null)
            return;

        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];
            float initialDelay = stem != null ? Mathf.Max(0f, stem.initialDelay) : 0f;

            stemGrowthStates.Add(new StemGrowthRuntimeState
            {
                stemIndex = i,
                timer = -initialDelay,
                enabled = stem != null && stem.enabled && IsCeilingStemAvailable(stem)
            });
        }
    }

    private void ValidateCeilingStemConfiguration()
    {
        if (data == null || !data.UseStemGrowth || data.growthStems == null)
            return;

        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (!UsesCeilingSegment(stem))
                continue;

            if (!useCeilingForCurrentStage)
            {
                Debug.LogWarning(
                    $"BlockManager: Skipping growth stem {i} (ceilingSegmentIndex={stem.ceilingSegmentIndex}) " +
                    "because this stage has useCeiling disabled."
                );
                continue;
            }

            if (ceilingManager == null)
            {
                Debug.LogWarning(
                    $"BlockManager: Skipping growth stem {i} (ceilingSegmentIndex={stem.ceilingSegmentIndex}) " +
                    "because CeilingManager is missing."
                );
                continue;
            }

            if (!ceilingManager.TryGetSegmentXRange(stem.ceilingSegmentIndex, out _, out _))
            {
                Debug.LogWarning(
                    $"BlockManager: Skipping growth stem {i}; ceilingSegmentIndex={stem.ceilingSegmentIndex} " +
                    "does not exist in the current ceiling configuration."
                );
            }
        }
    }

    private bool IsCeilingStemAvailable(StageBlockData.GrowthStemData stem)
    {
        if (!UsesCeilingSegment(stem))
            return true;

        return useCeilingForCurrentStage &&
               ceilingManager != null &&
               ceilingManager.TryGetSegmentXRange(stem.ceilingSegmentIndex, out _, out _);
    }

    public void DisableStemGrowth(int stemIndex)
    {
        if (stemIndex < 0)
            return;

        if (stemGrowthStates == null || stemIndex >= stemGrowthStates.Count)
            return;

        stemGrowthStates[stemIndex].enabled = false;
        RefreshStemConnectionVisuals();
    }

    public void DisableStemGrowthByStartXRange(int startX, int endX)
    {
        if (data == null || data.growthStems == null)
            return;

        int minX = Mathf.Min(startX, endX);
        int maxX = Mathf.Max(startX, endX);

        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (stem == null)
                continue;

            if (UsesCeilingSegment(stem))
                continue;

            if (stem.startCoord.x < minX || stem.startCoord.x > maxX)
                continue;

            DisableStemGrowth(i);
        }
    }

    public void DisableStemGrowthByCeilingSegment(int ceilingSegmentIndex)
    {
        if (data == null || data.growthStems == null)
            return;

        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (stem == null)
                continue;

            if (stem.ceilingSegmentIndex != ceilingSegmentIndex)
                continue;

            DisableStemGrowth(i);
        }
    }

    private void UpdateStemGrowth()
    {
        RespawnMissingStartBlocks();

        if (data == null || data.growthStems == null || data.growthStems.Length == 0)
            return;

        if (stemGrowthStates.Count != data.growthStems.Length)
            InitializeStemGrowthStates();

        for (int i = 0; i < stemGrowthStates.Count; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (stem == null)
                continue;

            if (!stem.enabled)
                continue;

            StemGrowthRuntimeState state = stemGrowthStates[i];

            if (!state.enabled)
                continue;

            state.timer += Time.deltaTime;

            float interval = GetStemSpawnInterval(stem);

            if (interval <= 0f)
                continue;

            if (state.timer < interval)
                continue;

            state.timer = 0f;

            int growCount = GetStemGrowCount(stem);

            for (int j = 0; j < growCount; j++)
            {
                bool spawned = TryGrowSingleStem(i);

                if (!spawned)
                    break;
            }
        }
    }

    private float GetStemSpawnInterval(StageBlockData.GrowthStemData stem)
    {
        return stem.spawnInterval > 0f
            ? stem.spawnInterval
            : data.spawnInterval;
    }

    private int GetStemMinGrowPerTick(StageBlockData.GrowthStemData stem)
    {
        return stem.minGrowPerTick >= 0
            ? stem.minGrowPerTick
            : data.minGrowPerTick;
    }

    private int GetStemMaxGrowPerTick(StageBlockData.GrowthStemData stem)
    {
        return stem.maxGrowPerTick >= 0
            ? stem.maxGrowPerTick
            : data.maxGrowPerTick;
    }

    private int GetStemGrowCount(StageBlockData.GrowthStemData stem)
    {
        int min = GetStemMinGrowPerTick(stem);
        int max = GetStemMaxGrowPerTick(stem);

        if (min < 0)
            min = 0;

        if (max < min)
            max = min;

        return Random.Range(min, max + 1);
    }

    private bool TryGrowSingleStem(int stemIndex)
    {
        List<GrowthCandidate> candidates = CollectStemGrowthCandidatesForStem(stemIndex);

        if (candidates == null || candidates.Count == 0)
            return false;

        GrowthCandidate selected = PickByPriorityAndWeight(candidates);
        SpawnBlock(selected.cell, data.defaultHp, false, stemIndex);

        return true;
    }

    private List<GrowthCandidate> CollectStemGrowthCandidatesForStem(int stemIndex)
    {
        List<GrowthCandidate> candidates = new();

        if (data.growthStems == null || stemIndex < 0 || stemIndex >= data.growthStems.Length)
            return candidates;

        StageBlockData.GrowthStemData stem = data.growthStems[stemIndex];

        if (stem == null || stem.growWeight <= 0f)
            return candidates;

        if (UsesCeilingSegment(stem))
            return CollectCeilingSegmentStemGrowthCandidates(stemIndex, stem);

        bool[,] connected = null;

        if (data.onlyGrowFromStartConnectedBlocks)
            connected = GetStartConnectedCells();

        IEnumerable<StageBlockData.GrowthDirection> directions = GetStemGrowthDirections(stem);

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector2Int parent = new Vector2Int(x, y);

                if (!IsStemGrowthCoord(parent, stem))
                    continue;

                if (!occupied[x, y])
                    continue;

                if (fixedOccupied[x, y])
                    continue;

                if (stemOwner[x, y] != stemIndex)
                    continue;

                if (data.onlyGrowFromStartConnectedBlocks && !connected[x, y])
                    continue;

                foreach (StageBlockData.GrowthDirection dir in directions)
                {
                    if (dir.weight <= 0f)
                        continue;

                    Vector2Int next = parent + dir.direction;

                    if (!IsStemGrowthCoord(next, stem))
                        continue;

                    if (occupied[next.x, next.y])
                        continue;

                    int rowLimit = Mathf.Max(1, stem.maxBlocksPerRow);

                    if (CountStemBlocksInRow(stemIndex, stem, next.y) >= rowLimit)
                        continue;

                    float finalWeight = dir.weight * stem.growWeight;

                    AddCandidate(
                        candidates,
                        next,
                        dir.priority,
                        finalWeight,
                        stemIndex
                    );
                }
            }
        }

        return candidates;
    }

    #endregion

    private void SpawnFixedBlocks()
    {
        if (data == null || data.fixedBlocks == null)
            return;

        foreach (StageBlockData.FixedBlockData fixedBlock in data.fixedBlocks)
        {
            if (fixedBlock == null)
                continue;

            SpawnBlock(fixedBlock.cell, fixedBlock.hp, true);
        }
    }

    private void SpawnNormalBlocks()
    {
        if (data == null || data.normalBlocks == null)
            return;

        for (int i = 0; i < data.normalBlocks.Count; i++)
        {
            SpawnNormalBlock(data.normalBlocks[i], data.defaultHp);
        }
    }

    private void SpawnStartBlocks()
    {
        if (data.UseStemGrowth && data.HasStemGrowthData)
        {
            SpawnStemStartBlocks();
            return;
        }

        SpawnLegacyStartBlocks();
    }

    private void SpawnLegacyStartBlocks()
    {
        if (data == null || data.startCells == null)
            return;

        foreach (Vector2Int cell in data.startCells)
        {
            SpawnBlock(cell, data.defaultHp, false);
        }
    }

    private void SpawnStemStartBlocks()
    {
        int spawned = 0;
        int ceilingSegmentStemCount = 0;

        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (stem == null)
                continue;

            if (UsesCeilingSegment(stem))
            {
                ceilingSegmentStemCount++;
                continue;
            }

            SpawnBlock(stem.startCoord, data.defaultHp, false, i);
            spawned++;
        }

        if (spawned == 0 && ceilingSegmentStemCount > 0)
        {
            Debug.Log(
                "BlockManager: No stem start blocks spawned immediately because all stems use ceilingSegmentIndex. " +
                "Flow blocks will spawn from CeilingManager segment candidates during growth."
            );
        }
    }

    private void RespawnMissingStartBlocks()
    {
        if (data != null && !data.respawnMissingStartBlocks)
            return;

        if (data.UseStemGrowth && data.HasStemGrowthData)
        {
            RespawnMissingStemStartBlocks();
            return;
        }

        RespawnMissingLegacyStartBlocks();
    }

    private void RespawnMissingLegacyStartBlocks()
    {
        foreach (Vector2Int cell in data.startCells)
        {
            if (!IsValidCoord(cell))
                continue;

            if (!occupied[cell.x, cell.y])
                SpawnBlock(cell, data.defaultHp, false);
        }
    }

    private void RespawnMissingStemStartBlocks()
    {
        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (stem == null)
                continue;

            if (!IsStemGrowthRuntimeEnabled(i))
                continue;

            if (UsesCeilingSegment(stem))
                continue;

            Vector2Int cell = stem.startCoord;

            if (!IsValidCoord(cell))
                continue;

            if (!occupied[cell.x, cell.y])
                SpawnBlock(cell, data.defaultHp, false, i);
        }
    }

    private bool IsStemGrowthRuntimeEnabled(int stemIndex)
    {
        if (stemGrowthStates == null || stemIndex < 0 || stemIndex >= stemGrowthStates.Count)
            return true;

        return stemGrowthStates[stemIndex].enabled;
    }

    public void SpawnBlock(Vector2Int coord)
    {
        SpawnBlock(coord, data.defaultHp, false);
    }

    public void SpawnBlock(Vector2Int coord, float hp, bool isFixed, int stemIndex = -1)
    {
        SpawnBlock(coord, hp, isFixed ? BlockType.Fixed : BlockType.Flow, stemIndex);
    }

    private void SpawnNormalBlock(Vector2Int coord, float hp)
    {
        SpawnBlock(coord, hp, BlockType.Normal, -1);
    }

    private void SpawnBlock(Vector2Int coord, float hp, BlockType blockType, int stemIndex = -1)
    {
        if (data == null || occupied == null || fixedOccupied == null || blockTypes == null || stemOwner == null)
            return;

        if (blockPool == null)
            return;

        if (!IsValidCoord(coord))
            return;

        if (occupied[coord.x, coord.y])
            return;

        occupied[coord.x, coord.y] = true;
        fixedOccupied[coord.x, coord.y] = blockType == BlockType.Fixed;
        blockTypes[coord.x, coord.y] = blockType;
        stemOwner[coord.x, coord.y] = blockType == BlockType.Flow ? stemIndex : -1;

        if (blockType == BlockType.Fixed)
        {
            blockPool.CreateFixedBlock(coord, hp);
        }
        else if (blockType == BlockType.Normal)
        {
            blockPool.CreateNormalBlock(coord, hp);
        }
        else
        {
            blockPool.ActivateBlock(coord, hp, GetStemFlowBlockPrefabOverride(stemIndex));
        }

        RefreshStemConnectionVisuals();
        CheckNormalBlocksCleared();
    }

    public void RemoveBlock(Vector2Int coord, bool force = false)
    {
        if (!IsValidCoord(coord))
            return;

        if (!occupied[coord.x, coord.y])
            return;

        if (fixedOccupied[coord.x, coord.y] && !force)
            return;

        occupied[coord.x, coord.y] = false;

        fixedOccupied[coord.x, coord.y] = false;
        blockTypes[coord.x, coord.y] = BlockType.Empty;
        stemOwner[coord.x, coord.y] = -1;

        blockPool.DeactivateBlock(coord);

        RefreshStemConnectionVisuals();
        CheckNormalBlocksCleared();
    }

    public List<Vector2Int> GetAbsorbableFlowBlockCoords(
        Vector3 centerWorld,
        float radius,
        int maxCount
    )
    {
        List<Vector2Int> candidates = new();

        if (maxCount <= 0)
            return candidates;

        if (data == null || occupied == null || fixedOccupied == null)
            return candidates;

        float radiusSqr = radius * radius;

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);

                if (!IsAbsorbableFlowBlock(coord))
                    continue;

                Vector3 worldPosition = GridToWorld(coord);

                if (radius > 0f && (worldPosition - centerWorld).sqrMagnitude > radiusSqr)
                    continue;

                candidates.Add(coord);
            }
        }

        candidates.Sort((a, b) =>
        {
            float distanceA = (GridToWorld(a) - centerWorld).sqrMagnitude;
            float distanceB = (GridToWorld(b) - centerWorld).sqrMagnitude;
            return distanceA.CompareTo(distanceB);
        });

        if (candidates.Count > maxCount)
            candidates.RemoveRange(maxCount, candidates.Count - maxCount);

        return candidates;
    }

    public bool TryRemoveAbsorbableFlowBlock(Vector2Int coord)
    {
        if (!IsAbsorbableFlowBlock(coord))
            return false;

        RemoveBlock(coord);
        return true;
    }

    private bool IsAbsorbableFlowBlock(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return false;

        if (!occupied[coord.x, coord.y])
            return false;

        if (blockTypes[coord.x, coord.y] != BlockType.Flow)
            return false;

        if (fixedOccupied[coord.x, coord.y])
            return false;

        if (IsStartCoord(coord))
            return false;

        return true;
    }

    private bool IsStartCoord(Vector2Int coord)
    {
        foreach (Vector2Int startCoord in GetActiveStartCoords())
        {
            if (startCoord == coord)
                return true;
        }

        return false;
    }

    public BlockCell GetBlockCell(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return null;

        if (blockPool != null)
            return blockPool.GetBlock(coord);

        Vector3 cellCenter = GridToWorld(coord);
        Vector2 cellSize = GetCellSize();

        Collider2D hit = Physics2D.OverlapBox(
            cellCenter,
            cellSize,
            0f
        );

        if (hit == null)
            return null;

        return hit.GetComponentInParent<BlockCell>();
    }
    public void NotifyBlockDestroyed(Vector2Int coord, bool isFixed)
    {
        if (isFixed)
            return;

        RemoveBlock(coord);
    }

    public void NotifyFixedBlockHitByBall(BlockCell block)
    {
        if (block == null || !block.IsFixed)
            return;

        OnFixedBlockHitByBall?.Invoke(block);
    }

    public void NotifyFixedBlockDestroyedByLaser(BlockCell block)
    {
        if (block == null)
            return;

        OnFixedBlockDestroyedByLaser?.Invoke(block);
    }

    public void AddGauge()
    {
        if (gaugeManager == null)
            return;

        gaugeManager.AddGauge();
    }

    private List<GrowthCandidate> GetLegacyGrowthCandidates()
    {
        List<GrowthCandidate> candidates = new();

        bool[,] connected = null;

        if (data.onlyGrowFromStartConnectedBlocks)
            connected = GetStartConnectedCells();
            

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!occupied[x, y])
                    continue;

                if (fixedOccupied[x, y])
                    continue;

                if (blockTypes[x, y] != BlockType.Flow)
                    continue;

                if (data.onlyGrowFromStartConnectedBlocks && !connected[x, y])
                    continue;

                Vector2Int parent = new Vector2Int(x, y);

                foreach (StageBlockData.GrowthDirection dir in data.directions)
                {
                    if (dir.weight <= 0f)
                        continue;

                    Vector2Int next = parent + dir.direction;

                    float finalWeight =
                        dir.weight + next.y * data.rowWeightMultiplier;

                    int finalPriority =
                        dir.priority + next.y * data.rowPriorityStep;

                    AddCandidate(
                        candidates,
                        next,
                        finalPriority,
                        finalWeight
                    );
                }
            }
        }

        return candidates;
    }

    public bool[,] GetStartConnectedCells()
    {
        bool[,] connected = new bool[data.width, data.height];

        if (data.UseStemGrowth && data.HasStemGrowthData)
        {
            for (int stemIndex = 0; stemIndex < data.growthStems.Length; stemIndex++)
            {
                StageBlockData.GrowthStemData stem = data.growthStems[stemIndex];

                if (!IsStemConnectionRootActive(stemIndex, stem))
                    continue;

                if (UsesCeilingSegment(stem))
                {
                    foreach (Vector2Int root in GetCeilingSegmentTouchingStemCells(stemIndex, stem))
                        AddConnectedStemCells(root, stemIndex, connected);
                }
                else
                {
                    AddConnectedStemCells(stem.startCoord, stemIndex, connected);
                }
            }
        }
        else
        {
            foreach (Vector2Int startCell in data.startCells)
                AddConnectedStemCells(startCell, -1, connected);
        }

        return connected;
    }

    private void AddConnectedStemCells(Vector2Int root, int ownerIndex, bool[,] connected)
    {
        if (!IsValidConnectedStemCell(root, ownerIndex))
            return;

        Queue<Vector2Int> queue = new();
        connected[root.x, root.y] = true;
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in StemConnectionDirections)
            {
                Vector2Int next = current + dir;

                if (!IsValidConnectedStemCell(next, ownerIndex))
                    continue;

                if (connected[next.x, next.y])
                    continue;

                connected[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }
    }

    private bool IsValidConnectedStemCell(Vector2Int coord, int ownerIndex)
    {
        return IsValidCoord(coord) &&
               occupied[coord.x, coord.y] &&
               !fixedOccupied[coord.x, coord.y] &&
               blockTypes[coord.x, coord.y] == BlockType.Flow &&
               stemOwner[coord.x, coord.y] == ownerIndex;
    }

    private void RefreshStemConnectionVisuals()
    {
        if (occupied == null || fixedOccupied == null || blockPool == null)
            return;

        bool[,] connected = GetStartConnectedCells();
        List<bool> coreConnections = CreateCoreConnectionStateList();

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!occupied[x, y])
                    continue;

                if (fixedOccupied[x, y])
                    continue;

                if (blockTypes[x, y] != BlockType.Flow)
                    continue;

                Vector2Int coord = new Vector2Int(x, y);
                BlockCell block = blockPool.GetBlock(coord);

                if (block == null)
                    continue;

                int distanceFromBottom = GetDistanceFromBottom(y);
                float danger01 = connected[x, y]
                    ? GetDanger01FromBottomDistance(distanceFromBottom)
                    : 0f;
                int glitchStage = GetGlitchStageFromBottomDistance(distanceFromBottom);

                block.SetStemVisual(
                    connected[x, y],
                    danger01,
                    glitchStage,
                    GetStemGlowProfile(stemOwner[x, y])
                );

                if (connected[x, y])
                    MarkConnectedCeilingCore(coreConnections, stemOwner[x, y]);
            }
        }

        ceilingManager?.SetCoreConnections(coreConnections.ToArray());
    }

    private CeilingSegmentVisualProfile GetStemGlowProfile(int stemIndex)
    {
        if (ceilingManager == null || data == null || data.growthStems == null)
            return ceilingManager != null
                ? ceilingManager.GetSegmentGlowProfile(-1)
                : fallbackGlowProfile;

        if (stemIndex < 0 || stemIndex >= data.growthStems.Length)
            return ceilingManager.GetSegmentGlowProfile(-1);

        StageBlockData.GrowthStemData stem = data.growthStems[stemIndex];
        int segmentIndex = stem != null ? stem.ceilingSegmentIndex : -1;
        return ceilingManager.GetSegmentGlowProfile(segmentIndex);
    }

    private List<bool> CreateCoreConnectionStateList()
    {
        int segmentCount = 0;

        if (data != null && data.growthStems != null)
        {
            for (int i = 0; i < data.growthStems.Length; i++)
            {
                StageBlockData.GrowthStemData stem = data.growthStems[i];

                if (!UsesCeilingSegment(stem))
                    continue;

                segmentCount = Mathf.Max(segmentCount, stem.ceilingSegmentIndex + 1);
            }
        }

        List<bool> states = new(segmentCount);

        for (int i = 0; i < segmentCount; i++)
            states.Add(false);

        return states;
    }

    private void MarkConnectedCeilingCore(List<bool> coreConnections, int stemIndex)
    {
        if (coreConnections == null)
            return;

        if (stemOwner == null || data == null || data.growthStems == null)
            return;

        if (stemIndex < 0 || stemIndex >= data.growthStems.Length)
            return;

        StageBlockData.GrowthStemData stem = data.growthStems[stemIndex];

        if (!UsesCeilingSegment(stem))
            return;

        int segmentIndex = stem.ceilingSegmentIndex;

        if (segmentIndex < 0)
            return;

        while (coreConnections.Count <= segmentIndex)
            coreConnections.Add(false);

        coreConnections[segmentIndex] = true;
    }

    private int GetDistanceFromBottom(int y)
    {
        int bottomY = data.height - 1;
        return Mathf.Abs(bottomY - y);
    }

    private float GetDanger01FromBottomDistance(int distanceFromBottom)
    {
        if (dangerWarningRows <= 0)
            return 0f;

        if (distanceFromBottom > dangerWarningRows)
            return 0f;

        return 1f - Mathf.Clamp01(distanceFromBottom / (float)dangerWarningRows);
    }

    private int GetGlitchStageFromBottomDistance(int distanceFromBottom)
    {
        int stage2Rows = Mathf.Max(0, glitchStage2RowsFromBottom);
        int stage3Rows = Mathf.Max(0, glitchStage3RowsFromBottom);

        if (stage3Rows > stage2Rows)
        {
            int temp = stage2Rows;
            stage2Rows = stage3Rows;
            stage3Rows = temp;
        }

        if (distanceFromBottom <= stage3Rows)
            return 3;

        if (distanceFromBottom <= stage2Rows)
            return 2;

        return 1;
    }

    private IEnumerable<Vector2Int> GetActiveStartCoords()
    {
        if (data.UseStemGrowth && data.HasStemGrowthData)
        {
            for (int stemIndex = 0; stemIndex < data.growthStems.Length; stemIndex++)
            {
                StageBlockData.GrowthStemData stem = data.growthStems[stemIndex];

                if (!IsStemConnectionRootActive(stemIndex, stem) || UsesCeilingSegment(stem))
                    continue;

                yield return stem.startCoord;
            }

            yield break;
        }

        foreach (Vector2Int startCell in data.startCells)
            yield return startCell;
    }

    private bool IsStemConnectionRootActive(
        int stemIndex,
        StageBlockData.GrowthStemData stem
    )
    {
        if (stem == null || !stem.enabled || !IsStemGrowthRuntimeEnabled(stemIndex))
            return false;

        return !UsesCeilingSegment(stem) || IsCeilingSegmentAliveForStem(stem);
    }

    private BlockCell GetStemFlowBlockPrefabOverride(int stemIndex)
    {
        if (data == null || data.growthStems == null)
            return null;

        if (stemIndex < 0 || stemIndex >= data.growthStems.Length)
            return null;

        StageBlockData.GrowthStemData stem = data.growthStems[stemIndex];
        return stem != null ? stem.flowBlockPrefabOverride : null;
    }

    private IEnumerable<StageBlockData.GrowthDirection> GetStemGrowthDirections(
        StageBlockData.GrowthStemData stem
    )
    {
        IEnumerable<StageBlockData.GrowthDirection> preferredDirections = stem.GetPreferredDirections();

        if (HasAnyGrowthDirection(preferredDirections))
            return stem.GetPreferredDirections();

        return data.directions;
    }

    private bool HasAnyGrowthDirection(IEnumerable<StageBlockData.GrowthDirection> directions)
    {
        if (directions == null)
            return false;

        foreach (StageBlockData.GrowthDirection direction in directions)
        {
            if (direction != null)
                return true;
        }

        return false;
    }

    private int CountStemBlocksInRow(
        int stemIndex,
        StageBlockData.GrowthStemData stem,
        int y
    )
    {
        if (stemOwner == null)
            return 0;

        if (y < 0 || y >= data.height)
            return 0;

        int count = 0;
        int minX = Mathf.Max(0, stem.minX);
        int maxX = Mathf.Min(data.width - 1, stem.maxX);

        for (int x = minX; x <= maxX; x++)
        {
            if (stemOwner[x, y] == stemIndex)
                count++;
        }

        return count;
    }

    private bool IsStemGrowthCoord(
        Vector2Int coord,
        StageBlockData.GrowthStemData stem
    )
    {
        if (!IsValidCoord(coord))
            return false;

        int topY = UsesCeilingSegment(stem)
            ? GetCeilingSegmentStemTopY(stem)
            : stem.startCoord.y;

        return coord.x >= stem.minX &&
               coord.x <= stem.maxX &&
               coord.y >= topY &&
               coord.y <= topY + stem.maxLength;
    }

    private List<GrowthCandidate> CollectCeilingSegmentStemGrowthCandidates(
        int stemIndex,
        StageBlockData.GrowthStemData stem
    )
    {
        List<GrowthCandidate> candidates = new();

        if (!IsCeilingSegmentAliveForStem(stem))
            return candidates;

        bool[,] connected = GetCeilingSegmentConnectedCellsForStem(stemIndex, stem);

        if (!HasConnectedStemCell(connected))
        {
            foreach (Vector2Int spawnCoord in GetCeilingSegmentSpawnCandidates(stem))
            {
                AddCandidate(candidates, spawnCoord, 0, stem.growWeight, stemIndex);
            }

            return candidates;
        }

        IEnumerable<StageBlockData.GrowthDirection> directions = GetStemGrowthDirections(stem);

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!connected[x, y])
                    continue;

                Vector2Int parent = new Vector2Int(x, y);

                foreach (StageBlockData.GrowthDirection dir in directions)
                {
                    if (dir.weight <= 0f)
                        continue;

                    Vector2Int next = parent + dir.direction;

                    if (!IsStemGrowthCoord(next, stem))
                        continue;

                    if (occupied[next.x, next.y])
                        continue;

                    int rowLimit = Mathf.Max(1, stem.maxBlocksPerRow);

                    if (CountStemBlocksInRow(stemIndex, stem, next.y) >= rowLimit)
                        continue;

                    AddCandidate(
                        candidates,
                        next,
                        dir.priority,
                        dir.weight * stem.growWeight,
                        stemIndex
                    );
                }
            }
        }

        return candidates;
    }

    private bool UsesCeilingSegment(StageBlockData.GrowthStemData stem)
    {
        return stem != null && stem.ceilingSegmentIndex >= 0;
    }

    private bool IsCeilingSegmentAliveForStem(StageBlockData.GrowthStemData stem)
    {
        if (!UsesCeilingSegment(stem))
            return true;

        return IsCeilingStemAvailable(stem) &&
               ceilingManager.IsSegmentAliveByIndex(stem.ceilingSegmentIndex);
    }

    private List<Vector2Int> GetCeilingSegmentSpawnCandidates(StageBlockData.GrowthStemData stem)
    {
        List<Vector2Int> candidates = new();

        if (!UsesCeilingSegment(stem))
            return candidates;

        if (ceilingManager == null)
            return candidates;

        if (!ceilingManager.TryGetSegmentXRange(stem.ceilingSegmentIndex, out int segmentStartX, out int segmentEndX))
            return candidates;

        int minX = Mathf.Max(0, stem.minX, Mathf.Min(segmentStartX, segmentEndX));
        int maxX = Mathf.Min(data.width - 1, stem.maxX, Mathf.Max(segmentStartX, segmentEndX));
        int y = GetCeilingSegmentStemTopY(stem);

        for (int x = minX; x <= maxX; x++)
        {
            Vector2Int coord = new Vector2Int(x, y);

            if (!IsValidCoord(coord))
                continue;

            candidates.Add(coord);
        }

        return candidates;
    }

    private int GetCeilingSegmentStemTopY(StageBlockData.GrowthStemData stem)
    {
        return Mathf.Clamp(stem.startCoord.y, 0, data.height - 1);
    }

    private IEnumerable<Vector2Int> GetCeilingSegmentTouchingStemCells(
        int stemIndex,
        StageBlockData.GrowthStemData stem
    )
    {
        foreach (Vector2Int coord in GetCeilingSegmentSpawnCandidates(stem))
        {
            if (!occupied[coord.x, coord.y])
                continue;

            if (fixedOccupied[coord.x, coord.y])
                continue;

            if (blockTypes[coord.x, coord.y] != BlockType.Flow)
                continue;

            if (stemOwner[coord.x, coord.y] != stemIndex)
                continue;

            yield return coord;
        }
    }

    private bool[,] GetCeilingSegmentConnectedCellsForStem(
        int stemIndex,
        StageBlockData.GrowthStemData stem
    )
    {
        bool[,] connected = new bool[data.width, data.height];
        Queue<Vector2Int> queue = new();

        foreach (Vector2Int root in GetCeilingSegmentTouchingStemCells(stemIndex, stem))
        {
            connected[root.x, root.y] = true;
            queue.Enqueue(root);
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in StemConnectionDirections)
            {
                Vector2Int next = current + dir;

                if (!IsStemGrowthCoord(next, stem))
                    continue;

                if (!occupied[next.x, next.y])
                    continue;

                if (fixedOccupied[next.x, next.y])
                    continue;

                if (blockTypes[next.x, next.y] != BlockType.Flow)
                    continue;

                if (stemOwner[next.x, next.y] != stemIndex)
                    continue;

                if (connected[next.x, next.y])
                    continue;

                connected[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        return connected;
    }

    private bool HasConnectedStemCell(bool[,] connected)
    {
        if (connected == null)
            return false;

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (connected[x, y])
                    return true;
            }
        }

        return false;
    }

    private void AddCandidate(
        List<GrowthCandidate> candidates,
        Vector2Int cell,
        int priority,
        float weight,
        int stemIndex = -1
    )
    {
        if (!IsValidCoord(cell))
            return;

        if (occupied[cell.x, cell.y])
            return;

        if (IsInsideBallSpawnSafetyArea(cell))
            return;

        if (weight <= 0f)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].cell == cell)
            {
                if (priority < candidates[i].priority)
                {
                    candidates[i] = new GrowthCandidate(cell, priority, weight, stemIndex);
                }
                else if (priority == candidates[i].priority)
                {
                    candidates[i] = new GrowthCandidate(
                        cell,
                        priority,
                        candidates[i].weight + weight,
                        candidates[i].stemIndex
                    );
                }

                return;
            }
        }

        candidates.Add(new GrowthCandidate(cell, priority, weight, stemIndex));
    }

    private bool IsInsideBallSpawnSafetyArea(Vector2Int cell)
    {
        if (ballController == null)
            return false;

        CalculateGridSize();

        Vector2 ballPosition = ballController.transform.position;
        Vector2 cellCenter = GridToWorld(cell);
        Vector2 halfCellSize = new Vector2(cellWidth, cellHeight) * 0.5f;
        float safeRadius = Mathf.Max(0f, ballController.actualRadius + ballSpawnSafetyMargin);

        Vector2 closestPoint = new Vector2(
            Mathf.Clamp(ballPosition.x, cellCenter.x - halfCellSize.x, cellCenter.x + halfCellSize.x),
            Mathf.Clamp(ballPosition.y, cellCenter.y - halfCellSize.y, cellCenter.y + halfCellSize.y)
        );

        return (ballPosition - closestPoint).sqrMagnitude <= safeRadius * safeRadius;
    }

    private GrowthCandidate PickByPriorityAndWeight(List<GrowthCandidate> candidates)
    {
        int bestPriority = int.MaxValue;

        foreach (GrowthCandidate candidate in candidates)
        {
            if (candidate.priority < bestPriority)
                bestPriority = candidate.priority;
        }

        List<GrowthCandidate> priorityCandidates = new();

        foreach (GrowthCandidate candidate in candidates)
        {
            if (candidate.priority == bestPriority)
                priorityCandidates.Add(candidate);
        }

        return PickWeightedCandidate(priorityCandidates);
    }

    private GrowthCandidate PickWeightedCandidate(List<GrowthCandidate> candidates)
    {
        float totalWeight = 0f;

        foreach (GrowthCandidate candidate in candidates)
            totalWeight += candidate.weight;

        float randomValue = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (GrowthCandidate candidate in candidates)
        {
            current += candidate.weight;

            if (randomValue <= current)
                return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    public bool IsOccupied(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return false;

        if (occupied == null)
            return false;

        return occupied[coord.x, coord.y];
    }

    // LaserBlockEraser에서 접근 필요
    public bool IsValidCoord(Vector2Int coord)
    {
        if (data == null)
            return false;

        return coord.x >= 0 &&
               coord.x < data.width &&
               coord.y >= 0 &&
               coord.y < data.height;
    }

    private void OnDrawGizmos()
    {
        DrawGridGizmos(Color.gray);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGridGizmos(Color.green);
    }

    // 레이저 발사시 블록 탐지를 위한 참조
    public int Width => data != null ? data.width : 0;
    public int Height => data != null ? data.height : 0;

    public bool IsFixed(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return false;

        if (fixedOccupied == null)
            return false;

        return fixedOccupied[coord.x, coord.y];
    }

    public bool IsNormal(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return false;

        if (blockTypes == null)
            return false;

        return blockTypes[coord.x, coord.y] == BlockType.Normal;
    }
    
    public float GetTopBoundaryY()
    {
        CalculateGridSize();

        if (gridArea == null)
            return 0f;

        float top = gridArea.position.y + gridArea.lossyScale.y * 0.5f;
        return top;
    }

    public float GetBottomBoundaryY()
    {
        CalculateGridSize();

        if (gridArea == null)
            return 0f;

        return gridArea.position.y - gridArea.lossyScale.y * 0.5f;
    }
    
    public int WorldXToGridX(float worldX)
    {
        if (data == null || gridArea == null)
            return 0;

        CalculateGridSize();

        float left = gridArea.position.x - gridArea.lossyScale.x * 0.5f;
        int x = Mathf.FloorToInt((worldX - left) / cellWidth);

        return Mathf.Clamp(x, 0, data.width - 1);
    }

    public float GetCellHeight()
    {
        CalculateGridSize();
        return cellHeight;
    }
    
    
    
    private void DrawGridGizmos(Color color)
    {
        if (data == null)
            return;

        if (gridArea == null)
            return;

        CalculateGridSize();

        Gizmos.color = color;

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                Vector3 center = GridToWorld(coord);
                Vector3 size = new Vector3(cellWidth, cellHeight, 0f);

                Gizmos.DrawWireCube(center, size);
            }
        }
    }

    private void ClearAllSpawnedBlocks()
    {
        BlockCell[] blocks = FindObjectsOfType<BlockCell>(true);

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != null)
            {
                Destroy(blocks[i].gameObject);
            }
        }
    }

    private void ResetNormalBlockTracking()
    {
        normalBlocksClearedNotified = false;
    }

    private void CheckNormalBlocksCleared()
    {
        if (normalBlocksClearedNotified)
            return;

        if (data == null || data.normalBlocks == null || data.normalBlocks.Count == 0)
            return;

        for (int i = 0; i < data.normalBlocks.Count; i++)
        {
            Vector2Int coord = data.normalBlocks[i];

            if (!IsValidCoord(coord))
                continue;

            if (occupied[coord.x, coord.y])
                return;
        }

        normalBlocksClearedNotified = true;
        OnNormalBlocksCleared?.Invoke();
        OnTutorialTargetBlocksCleared?.Invoke();
    }
}
