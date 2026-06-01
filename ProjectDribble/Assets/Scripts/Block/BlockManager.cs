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

        public GrowthCandidate(Vector2Int cell, int priority, float weight)
        {
            this.cell = cell;
            this.priority = priority;
            this.weight = weight;
        }
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

    private float cellWidth;
    private float cellHeight;

    private Coroutine growRoutine;

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
            RespawnMissingStartBlocks();

            int growCount = Random.Range(
                data.minGrowPerTick,
                data.maxGrowPerTick + 1
            );

            for (int i = 0; i < growCount; i++)
            {
                List<GrowthCandidate> candidates = GetGrowthCandidates();

                if (candidates.Count <= 0)
                    break;

                GrowthCandidate selected = PickByPriorityAndWeight(candidates);
                SpawnBlock(selected.cell, data.defaultHp, false);
            }

            yield return new WaitForSeconds(data.spawnInterval);
        }
    }

    private void SpawnFixedBlocks()
    {
        foreach (StageBlockData.FixedBlockData fixedBlock in data.fixedBlocks)
        {
            SpawnBlock(fixedBlock.cell, fixedBlock.hp, true);
        }
    }

    private void SpawnStartBlocks()
    {
        foreach (Vector2Int cell in data.startCells)
        {
            SpawnBlock(cell, data.defaultHp, false);
        }
    }

    private void RespawnMissingStartBlocks()
    {
        foreach (Vector2Int cell in data.startCells)
        {
            if (!IsValidCoord(cell))
                continue;

            if (!occupied[cell.x, cell.y])
                SpawnBlock(cell, data.defaultHp, false);
        }
    }

    public void SpawnBlock(Vector2Int coord)
    {
        SpawnBlock(coord, data.defaultHp, false);
    }

    public void SpawnBlock(Vector2Int coord, float hp, bool isFixed)
    {
        if (!IsValidCoord(coord))
            return;

        if (occupied[coord.x, coord.y])
            return;

        occupied[coord.x, coord.y] = true;
        fixedOccupied[coord.x, coord.y] = isFixed;

        if (isFixed)
        {
            blockPool.CreateFixedBlock(coord, hp);
        }
        else
        {
            blockPool.ActivateBlock(coord, hp);
        }
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
    
    private List<GrowthCandidate> GetGrowthCandidates()
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

        foreach (Vector2Int startCell in data.startCells)
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
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
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

    private void AddCandidate(
        List<GrowthCandidate> candidates,
        Vector2Int cell,
        int priority,
        float weight
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
                    candidates[i] = new GrowthCandidate(cell, priority, weight);
                }
                else if (priority == candidates[i].priority)
                {
                    candidates[i] = new GrowthCandidate(
                        cell,
                        priority,
                        candidates[i].weight + weight
                    );
                }

                return;
            }
        }

        candidates.Add(new GrowthCandidate(cell, priority, weight));
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
