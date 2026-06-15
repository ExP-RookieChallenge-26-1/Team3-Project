using UnityEngine;

public class BlockPool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockCell flowBlockPrefab;
    [SerializeField] private BlockCell normalBlockPrefab;
    [SerializeField] private BlockCell fixedBlockPrefab;
    [SerializeField] private Transform normalBlockParent;
    [SerializeField] private Transform fixedBlockParent;
    [SerializeField] private Transform flowBlockParent;

    private bool warnedNormalFallback;

    private BlockCell[,] pool;

    private System.Func<Vector2Int, Vector3> gridToWorld;
    private Vector2 cellSize;
    private BlockManager manager;

    public void CreatePool(
        int width,
        int height,
        System.Func<Vector2Int, Vector3> gridToWorld,
        System.Func<Vector2> getCellSize,
        BlockManager manager
    )
    {
        if (flowBlockPrefab == null)
        {
            Debug.LogWarning("BlockPool: flowBlockPrefab is null. Flow block pool was not created.");
            return;
        }

        this.gridToWorld = gridToWorld;
        this.manager = manager;

        pool = new BlockCell[width, height];
        cellSize = getCellSize();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);

                BlockCell block = Instantiate(
                    flowBlockPrefab,
                    gridToWorld(coord),
                    Quaternion.identity,
                    flowBlockParent
                );

                ApplyBlockSize(block);

                block.Init(manager, coord);
                block.gameObject.SetActive(false);

                pool[x, y] = block;
            }
        }
    }

    public void ActivateBlock(Vector2Int coord, float hp)
    {
        if (pool == null)
        {
            Debug.LogWarning("BlockPool: Cannot activate flow block because pool is null.");
            return;
        }

        BlockCell block = pool[coord.x, coord.y];

        block.transform.position = gridToWorld(coord);
        block.transform.SetParent(flowBlockParent, false);

        ApplyBlockSize(block);

        block.Activate(coord, hp, BlockType.Flow);
    }

    public BlockCell CreateNormalBlock(Vector2Int coord, float hp)
    {
        BlockCell prefab = normalBlockPrefab;

        if (prefab == null)
        {
            prefab = flowBlockPrefab;

            if (!warnedNormalFallback)
            {
                Debug.LogWarning("BlockPool: normalBlockPrefab is null. Falling back to flowBlockPrefab.");
                warnedNormalFallback = true;
            }
        }

        Transform parent = normalBlockParent != null ? normalBlockParent : flowBlockParent;

        BlockCell block = Instantiate(
            prefab,
            gridToWorld(coord),
            Quaternion.identity,
            parent
        );

        ApplyBlockSize(block);

        block.Init(manager, coord);
        block.Activate(coord, hp, BlockType.Normal);

        return block;
    }

    public BlockCell CreateFixedBlock(Vector2Int coord, float hp)
    {
        if (fixedBlockPrefab == null)
        {
            Debug.LogWarning("BlockPool: fixedBlockPrefab is null. Fixed block was not created.");
            return null;
        }

        BlockCell block = Instantiate(
            fixedBlockPrefab,
            gridToWorld(coord),
            Quaternion.identity,
            fixedBlockParent
        );

        ApplyBlockSize(block);

        block.Init(manager, coord);
        block.Activate(coord, hp, BlockType.Fixed);

        return block;
    }

    public void DeactivateBlock(Vector2Int coord)
    {
        BlockCell block = pool[coord.x, coord.y];

        block.Deactivate();
        block.transform.SetParent(flowBlockParent, false);
    }

    public BlockCell GetBlock(Vector2Int coord)
    {
        return pool[coord.x, coord.y];
    }

    private void ApplyBlockSize(BlockCell block)
    {
        block.transform.localScale = new Vector3(cellSize.x, cellSize.y , 1f);
    }
}
