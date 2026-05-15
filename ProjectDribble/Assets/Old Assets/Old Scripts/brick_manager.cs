using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickManager : MonoBehaviour
{
    private int currentVersion = 0;

    [System.Serializable]
    public class GrowthDirection
    {
        public string name;
        public Vector2Int direction;
        public int priority = 0;
        public float weight = 1f;
    }

    [System.Serializable]
    public class FixedBrickData
    {
        public Vector2Int cell;
        public GameObject prefabOverride;
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

    [Header("Brick Prefabs")]
    [SerializeField] private GameObject brickPrefab;
    [SerializeField] private GameObject fixedBrickPrefab;
    [SerializeField] private Transform brickParent;

    [Header("Fixed Bricks")]
    [SerializeField] private List<FixedBrickData> fixedBricks = new();

    [Header("Grid")]
    [SerializeField] private int rowCount = 7;
    [SerializeField] private int columnCount = 20;
    [SerializeField] private float cellWidth = 0.8f;
    [SerializeField] private float cellHeight = 0.4f;
    [SerializeField] private float brickSizeRatio = 0.8f;

    [Header("Map Position")]
    [SerializeField] private Vector2 startPosition = new Vector2(-7.6f, 4f);

    [Header("Growth Timing")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private int minGrowPerTick = 1;
    [SerializeField] private int maxGrowPerTick = 2;

    [Header("Row Weight")]
    [SerializeField] private float rowWeightMultiplier = 0.2f;

    [Header("Row Priority")]
    [SerializeField] private int rowPriorityStep = 0;

    [Header("Connection Rule")]
    [SerializeField] private bool onlyGrowFromStartConnectedBricks = true;

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

    [Header("Start Bricks")]
    [SerializeField]
    private List<Vector2Int> startCells = new()
    {
        new Vector2Int(10, 0)
    };

    private bool[,] occupied;
    private bool[,] fixedOccupied;

    private Coroutine growRoutine;
    private bool clearing;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        ResetBricks();
    }

    public void ResetBricks()
    {
        IsInitialized = false;

        currentVersion++;

        StopGrowing();
        ClearBricks();

        occupied = new bool[columnCount, rowCount];
        fixedOccupied = new bool[columnCount, rowCount];

        SpawnFixedBricks();
        SpawnStartBricks();

        growRoutine = StartCoroutine(GrowRoutine());

        IsInitialized = true;
    }

    private IEnumerator GrowRoutine()
    {
        while (true)
        {
            RespawnMissingStartBricks();

            int growCount = Random.Range(minGrowPerTick, maxGrowPerTick + 1);

            for (int i = 0; i < growCount; i++)
            {
                List<GrowthCandidate> candidates = GetGrowthCandidates();

                if (candidates.Count <= 0)
                    break;

                GrowthCandidate selected = PickByPriorityAndWeight(candidates);
                SpawnBrick(selected.cell);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void RespawnMissingStartBricks()
    {
        foreach (Vector2Int cell in startCells)
        {
            if (!IsValidCell(cell))
                continue;

            if (!occupied[cell.x, cell.y])
                SpawnBrick(cell);
        }
    }

    private void SpawnFixedBricks()
    {
        foreach (FixedBrickData data in fixedBricks)
        {
            if (!IsValidCell(data.cell))
                continue;

            SpawnFixedBrick(data);
        }
    }

    private void SpawnStartBricks()
    {
        foreach (Vector2Int cell in startCells)
        {
            if (!IsValidCell(cell))
                continue;

            SpawnBrick(cell);
        }
    }

    private void SpawnFixedBrick(FixedBrickData data)
    {
        if (occupied[data.cell.x, data.cell.y])
            return;

        GameObject prefab = data.prefabOverride != null
            ? data.prefabOverride
            : fixedBrickPrefab != null
                ? fixedBrickPrefab
                : brickPrefab;

        if (prefab == null)
        {
            Debug.LogError("Fixed Brick Prefab�� �����ϴ�.");
            return;
        }

        occupied[data.cell.x, data.cell.y] = true;
        fixedOccupied[data.cell.x, data.cell.y] = true;

        GameObject brick = Instantiate(
            prefab,
            GridToWorld(data.cell),
            Quaternion.identity,
            brickParent
        );

        brick.layer = 3; // 생성된 벽돌의 레이어를 stage로 설정
        brick.tag = "need_correction"; // 생성된 벽돌의 레이어를 stage로 설정

        brick.name = $"FixedBrick_{data.cell.y}_{data.cell.x}";
        FitBrickToCell(brick);

        BrickCell brickCell = brick.GetComponent<BrickCell>();
        if (brickCell == null)
            brickCell = brick.AddComponent<BrickCell>();

        brickCell.Init(this, data.cell, true, currentVersion);
    }

    private void SpawnBrick(Vector2Int cell)
    {
        if (!IsValidCell(cell))
            return;

        if (occupied[cell.x, cell.y])
            return;

        if (brickPrefab == null)
        {
            Debug.LogError("Brick Prefab�� �����ϴ�.");
            return;
        }

        occupied[cell.x, cell.y] = true;

        GameObject brick = Instantiate(
            brickPrefab,
            GridToWorld(cell),
            Quaternion.identity,
            brickParent
        );

        brick.layer = 3; // 생성된 벽돌의 레이어를 stage로 설정

        brick.name = $"Brick_{cell.y}_{cell.x}";
        FitBrickToCell(brick);

        BrickCell brickCell = brick.GetComponent<BrickCell>();
        if (brickCell == null)
            brickCell = brick.AddComponent<BrickCell>();

        brickCell.Init(this, cell, false, currentVersion);
    }

    private List<GrowthCandidate> GetGrowthCandidates()
    {
        List<GrowthCandidate> candidates = new();

        bool[,] connected = null;

        if (onlyGrowFromStartConnectedBricks)
            connected = GetStartConnectedCells();

        for (int y = 0; y < rowCount; y++)
        {
            for (int x = 0; x < columnCount; x++)
            {
                if (!occupied[x, y])
                    continue;

                if (fixedOccupied[x, y])
                    continue;

                if (onlyGrowFromStartConnectedBricks && !connected[x, y])
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
        bool[,] connected = new bool[columnCount, rowCount];
        Queue<Vector2Int> queue = new();

        foreach (Vector2Int startCell in startCells)
        {
            if (!IsValidCell(startCell))
                continue;

            if (!occupied[startCell.x, startCell.y])
                continue;

            if (fixedOccupied[startCell.x, startCell.y])
                continue;

            if (connected[startCell.x, startCell.y])
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

                if (!IsValidCell(next))
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
        if (!IsValidCell(cell))
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

    private Vector3 GridToWorld(Vector2Int cell)
    {
        float x = startPosition.x + cell.x * cellWidth;
        float y = startPosition.y - cell.y * cellHeight;

        return new Vector3(x, y, 0f);
    }

    private void FitBrickToCell(GameObject brick)
    {
        brick.transform.localScale = new Vector3(
            cellWidth * brickSizeRatio,
            cellHeight * brickSizeRatio,
            1f
        );
    }

    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 &&
               cell.x < columnCount &&
               cell.y >= 0 &&
               cell.y < rowCount;
    }

    public void NotifyBrickDestroyed(Vector2Int cell, bool isFixed, int version)
    {
        if (version != currentVersion)
            return;

        if (clearing)
            return;

        if (!IsValidCell(cell))
            return;

        if (isFixed)
            return;

        occupied[cell.x, cell.y] = false;
    }

    private void ClearBricks()
    {
        if (brickParent == null)
            brickParent = transform;

        clearing = true;

        foreach (Transform child in brickParent)
            Destroy(child.gameObject);

        clearing = false;
    }

    private void StopGrowing()
    {
        if (growRoutine != null)
        {
            StopCoroutine(growRoutine);
            growRoutine = null;
        }
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        if (occupied == null)
            return true;

        if (!IsValidCell(cell))
            return true;

        return occupied[cell.x, cell.y];
    }
    public Vector3 GetCellWorldPosition(Vector2Int cell)
    {
        return GridToWorld(cell);
    }

    public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
    {
        int x = Mathf.RoundToInt((worldPosition.x - startPosition.x) / cellWidth);
        int y = Mathf.RoundToInt((startPosition.y - worldPosition.y) / cellHeight);

        cell = new Vector2Int(x, y);

        return IsValidCell(cell);
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
        Gizmos.color = color;

        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                Vector3 center = new Vector3(
                    startPosition.x + col * cellWidth,
                    startPosition.y - row * cellHeight,
                    0f
                );

                Vector3 size = new Vector3(cellWidth, cellHeight, 0f);

                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}