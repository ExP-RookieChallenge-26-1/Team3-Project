using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
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
        public int hp = 999;
    }

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

    [Header("Grid Size")]
    [SerializeField] private int width = 7;
    [SerializeField] private int height = 20;

    [Header("Grid Area")]
    [SerializeField] private Transform gridArea;

    [Header("References")]
    [SerializeField] private BlockPool blockPool;

    [Header("Default Block")]
    [SerializeField] private int defaultHp = 1;

    [Header("Fixed Blocks")]
    [SerializeField] private List<FixedBlockData> fixedBlocks = new();

    [Header("Start Blocks")]
    [SerializeField] private List<Vector2Int> startCells = new()
    {
        new Vector2Int(3, 0)
    };

    [Header("Growth Timing")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private int minGrowPerTick = 1;
    [SerializeField] private int maxGrowPerTick = 2;

    [Header("Row Weight")]
    [SerializeField] private float rowWeightMultiplier = 0.2f;

    [Header("Row Priority")]
    [SerializeField] private int rowPriorityStep = 0;

    [Header("Connection Rule")]
    [SerializeField] private bool onlyGrowFromStartConnectedBlocks = true;

    [Header("Growth Directions")]
    [SerializeField]
    private List<GrowthDirection> directions = new()
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

    private bool[,] occupied;
    private bool[,] fixedOccupied;

    private float cellWidth;
    private float cellHeight;

    private Coroutine growRoutine;

    private void Awake()
    {
        CreateGrid();

        blockPool.CreatePool(
            width,
            height,
            GridToWorld,
            GetCellSize,
            this
        );
    }

    private void Start()
    {
        SpawnFixedBlocks();
        SpawnStartBlocks();

        growRoutine = StartCoroutine(GrowRoutine());
    }

    private void CreateGrid()
    {
        occupied = new bool[width, height];
        fixedOccupied = new bool[width, height];
    }

    private void CalculateGridSize()
    {
        Vector3 areaSize = gridArea.lossyScale;

        cellWidth = areaSize.x / width;
        cellHeight = areaSize.y / height;
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
        return new Vector2(cellWidth, cellHeight);
    }

    private IEnumerator GrowRoutine()
    {
        while (true)
        {
            RespawnMissingStartBlocks();

            int growCount = Random.Range(minGrowPerTick, maxGrowPerTick + 1);

            for (int i = 0; i < growCount; i++)
            {
                List<GrowthCandidate> candidates = GetGrowthCandidates();

                if (candidates.Count <= 0)
                    break;

                GrowthCandidate selected = PickByPriorityAndWeight(candidates);
                SpawnBlock(selected.cell, defaultHp, false);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnFixedBlocks()
    {
        foreach (FixedBlockData data in fixedBlocks)
        {
            SpawnBlock(data.cell, data.hp, true);
        }
    }

    private void SpawnStartBlocks()
    {
        foreach (Vector2Int cell in startCells)
        {
            SpawnBlock(cell, defaultHp, false);
        }
    }

    private void RespawnMissingStartBlocks()
    {
        foreach (Vector2Int cell in startCells)
        {
            if (!IsValidCoord(cell))
                continue;

            if (!occupied[cell.x, cell.y])
                SpawnBlock(cell, defaultHp, false);
        }
    }

    public void SpawnBlock(Vector2Int coord)
    {
        SpawnBlock(coord, defaultHp, false);
    }

    public void SpawnBlock(Vector2Int coord, int hp, bool isFixed)
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

    public void RemoveBlock(Vector2Int coord)
    {
        if (!IsValidCoord(coord))
            return;

        if (!occupied[coord.x, coord.y])
            return;

        if (fixedOccupied[coord.x, coord.y])
            return;

        occupied[coord.x, coord.y] = false;
        fixedOccupied[coord.x, coord.y] = false;

        blockPool.DeactivateBlock(coord);
    }

    public void NotifyBlockDestroyed(Vector2Int coord, bool isFixed)
    {
        if (isFixed)
            return;

        RemoveBlock(coord);
    }

    private List<GrowthCandidate> GetGrowthCandidates()
    {
        List<GrowthCandidate> candidates = new();

        bool[,] connected = null;

        if (onlyGrowFromStartConnectedBlocks)
            connected = GetStartConnectedCells();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!occupied[x, y])
                    continue;

                if (fixedOccupied[x, y])
                    continue;

                if (onlyGrowFromStartConnectedBlocks && !connected[x, y])
                    continue;

                Vector2Int parent = new Vector2Int(x, y);

                foreach (GrowthDirection dir in directions)
                {
                    if (dir.weight <= 0f)
                        continue;

                    Vector2Int next = parent + dir.direction;

                    float finalWeight = dir.weight + next.y * rowWeightMultiplier;
                    int finalPriority = dir.priority + next.y * rowPriorityStep;

                    AddCandidate(candidates, next, finalPriority, finalWeight);
                }
            }
        }

        return candidates;
    }

    private bool[,] GetStartConnectedCells()
    {
        bool[,] connected = new bool[width, height];
        Queue<Vector2Int> queue = new();

        foreach (Vector2Int startCell in startCells)
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

    private bool IsValidCoord(Vector2Int coord)
    {
        return coord.x >= 0 &&
               coord.x < width &&
               coord.y >= 0 &&
               coord.y < height;
    }

    private void OnDrawGizmos()
    {
        DrawGridGizmos(Color.gray);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGridGizmos(Color.green);
    }

    private void DrawGridGizmos(Color color)
    {
        if (gridArea == null)
            return;

        CalculateGridSize();

        Gizmos.color = color;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                Vector3 center = GridToWorld(coord);
                Vector3 size = new Vector3(cellWidth, cellHeight, 0f);

                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}