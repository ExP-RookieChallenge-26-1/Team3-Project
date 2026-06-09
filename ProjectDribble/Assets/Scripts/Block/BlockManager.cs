using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
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
    }

    [Header("Stage Data")]
    [SerializeField] private StageBlockData data;

    [Header("Grid Area")]
    [SerializeField] private Transform gridArea;

    [Header("References")]
    [SerializeField] private BlockPool blockPool;

    [SerializeField] private GaugeManager gaugeManager;
    private bool[,] occupied;
    private bool[,] fixedOccupied;
    private int[,] stemOwner;

    private float cellWidth;
    private float cellHeight;

    private Coroutine growRoutine;
    private readonly List<StemGrowthRuntimeState> stemGrowthStates = new();

    private void Awake()
    {
        CreateGrid();

        blockPool.CreatePool(
            data.width,
            data.height,
            GridToWorld,
            GetCellSize,
            this
        );
    }

    private void Start()
    {
        SpawnFixedBlocks();
        SpawnStartBlocks();

        StartGrowth();
    }

    public void InitializeStageBlocks(StageBlockData stageData)
    {
        if (stageData != null)
        {
            data = stageData;
        }

        ResetBlocks();
        StartGrowth();
    }

    public void ResetBlocks()
    {
        StopGrowth();
        ClearAllSpawnedBlocks();

        CreateGrid();

        blockPool.CreatePool(
            data.width,
            data.height,
            GridToWorld,
            GetCellSize,
            this
        );

        SpawnFixedBlocks();
        SpawnStartBlocks();
    }

    public void StartGrowth()
    {
        StopGrowth();

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
        occupied = new bool[data.width, data.height];
        fixedOccupied = new bool[data.width, data.height];
        stemOwner = new int[data.width, data.height];

        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                stemOwner[x, y] = -1;
            }
        }
    }

    private void CalculateGridSize()
    {
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
                timer = -initialDelay
            });
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
        foreach (StageBlockData.FixedBlockData fixedBlock in data.fixedBlocks)
        {
            SpawnBlock(fixedBlock.cell, fixedBlock.hp, true);
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
        foreach (Vector2Int cell in data.startCells)
        {
            SpawnBlock(cell, data.defaultHp, false);
        }
    }

    private void SpawnStemStartBlocks()
    {
        for (int i = 0; i < data.growthStems.Length; i++)
        {
            StageBlockData.GrowthStemData stem = data.growthStems[i];

            if (stem == null)
                continue;

            SpawnBlock(stem.startCoord, data.defaultHp, false, i);
        }
    }

    private void RespawnMissingStartBlocks()
    {
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

            Vector2Int cell = stem.startCoord;

            if (!IsValidCoord(cell))
                continue;

            if (!occupied[cell.x, cell.y])
                SpawnBlock(cell, data.defaultHp, false, i);
        }
    }

    public void SpawnBlock(Vector2Int coord)
    {
        SpawnBlock(coord, data.defaultHp, false);
    }

    public void SpawnBlock(Vector2Int coord, float hp, bool isFixed, int stemIndex = -1)
    {
        if (!IsValidCoord(coord))
            return;

        if (occupied[coord.x, coord.y])
            return;

        occupied[coord.x, coord.y] = true;
        fixedOccupied[coord.x, coord.y] = isFixed;
        stemOwner[coord.x, coord.y] = isFixed ? -1 : stemIndex;

        if (isFixed)
        {
            blockPool.CreateFixedBlock(coord, hp);
        }
        else
        {
            blockPool.ActivateBlock(coord, hp);
        }

        RefreshStemConnectionVisuals();
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

        bool wasFixed = fixedOccupied[coord.x, coord.y];
        fixedOccupied[coord.x, coord.y] = false;
        stemOwner[coord.x, coord.y] = -1;

        if (wasFixed)
        {
            BlockCell block = GetBlockCell(coord); // 없다면 만들어야 함
            if (block != null)
                Destroy(block.gameObject);
        }
        else
        {
            blockPool.DeactivateBlock(coord);
        }

        RefreshStemConnectionVisuals();
    }

    public BlockCell GetBlockCell(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return null;

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

    public void AddGauge()
    {
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
        Queue<Vector2Int> queue = new();

        foreach (Vector2Int startCell in GetActiveStartCoords())
        {
            if (!IsValidCoord(startCell))
                continue;

            if (!occupied[startCell.x, startCell.y])
                continue;

            if (fixedOccupied[startCell.x, startCell.y])
                continue;

            connected[startCell.x, startCell.y] = true;
            queue.Enqueue(startCell);
        }

        Vector2Int[] checkDirs =
        {
            // 상하좌우
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),

            // 대각선
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in checkDirs)
            {
                Vector2Int next = current + dir;

                if (!IsValidCoord(next))
                    continue;

                if (!occupied[next.x, next.y])
                    continue;

                if (fixedOccupied[next.x, next.y])
                    continue;

                if (connected[next.x, next.y])
                    continue;

                connected[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        return connected;
    }

    private void RefreshStemConnectionVisuals()
    {
        if (occupied == null || fixedOccupied == null || blockPool == null)
            return;

        bool[,] connected = GetStartConnectedCells();

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!occupied[x, y])
                    continue;

                if (fixedOccupied[x, y])
                    continue;

                Vector2Int coord = new Vector2Int(x, y);
                BlockCell block = blockPool.GetBlock(coord);

                if (block == null)
                    continue;

                block.SetStemConnection(connected[x, y]);
                Debug.Log($"coord={coord}, occupied={occupied[x,y]}, fixed={fixedOccupied[x,y]}, connected={connected[x,y]}");
            }
        }
    }

    private IEnumerable<Vector2Int> GetActiveStartCoords()
    {
        if (data.UseStemGrowth && data.HasStemGrowthData)
        {
            foreach (StageBlockData.GrowthStemData stem in data.growthStems)
            {
                if (stem == null)
                    continue;

                yield return stem.startCoord;
            }

            yield break;
        }

        foreach (Vector2Int startCell in data.startCells)
            yield return startCell;
    }

    private IEnumerable<StageBlockData.GrowthDirection> GetStemGrowthDirections(
        StageBlockData.GrowthStemData stem
    )
    {
        if (stem.preferredDirections != null && stem.preferredDirections.Length > 0)
            return stem.preferredDirections;

        return data.directions;
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

        return coord.x >= stem.minX &&
               coord.x <= stem.maxX &&
               coord.y >= stem.startCoord.y &&
               coord.y <= stem.startCoord.y + stem.maxLength;
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

        return occupied[coord.x, coord.y];
    }

    // LaserBlockEraser에서 접근 필요
    public bool IsValidCoord(Vector2Int coord)
    {
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
    public int Width => data.width;
    public int Height => data.height;

    public bool IsFixed(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return false;

        return fixedOccupied[coord.x, coord.y];
    }
    
    public float GetTopBoundaryY()
    {
        CalculateGridSize();

        float top = gridArea.position.y + gridArea.lossyScale.y * 0.5f;
        return top;
    }
    
    public int WorldXToGridX(float worldX)
    {
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
}
