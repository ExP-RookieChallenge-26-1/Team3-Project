using UnityEngine;

public class LaserBlockEraser : MonoBehaviour
{
    [SerializeField] private BlockManager blockManager;

    public Vector2 EraseByLaser(
        Vector2 origin,
        float width,
        float range,
        float startOffset
    )
    {
        Vector2 start = origin + Vector2.up * startOffset;

        float leftX = start.x - width * 0.5f;
        float rightX = start.x + width * 0.5f;

        int minX = blockManager.WorldXToGridX(leftX);
        int maxX = blockManager.WorldXToGridX(rightX);

        int centerX = blockManager.WorldXToGridX(origin.x);

        float topY = blockManager.GetTopBoundaryY();

        float centerColumnEndY = topY;

        for (int x = minX; x <= maxX; x++)
        {
            float columnEndY = EraseColumn(x, start.y, topY);

            if (x == centerX)
            {
                centerColumnEndY = columnEndY;
            }
        }

        return new Vector2(origin.x, centerColumnEndY);
    }

    private float EraseColumn(int x, float startY, float endY)
    {
        // y = 0이 위쪽, y = Height - 1이 아래쪽.
        // 레이저는 아래에서 위로 올라가므로 아래쪽 행부터 위쪽 행으로 검사.
        for (int y = blockManager.Height - 1; y >= 0; y--)
        {
            Vector2Int coord = new Vector2Int(x, y);

            if (!blockManager.IsValidCoord(coord))
                continue;

            if (!blockManager.IsOccupied(coord))
                continue;

            Vector3 cellCenter = blockManager.GridToWorld(coord);

            if (cellCenter.y < startY)
                continue;

            if (cellCenter.y > endY)
                continue;

            BlockCell block = FindBlockCellAtCoord(coord);

            if (block == null)
            {
                Debug.LogWarning("occupied는 true인데 BlockCell을 찾지 못함: " + coord);
                continue;
            }

            if (block.IsFixed)
            {
                float cellHeight = blockManager.GetCellHeight();

                // 고정 블럭을 만나면 이 column만 막힘.
                // 옆 column은 계속 진행됨.
                return cellCenter.y - cellHeight * 0.5f;
            }

            // 일반 블럭이면 BlockCell의 레이저 반응 함수 호출.
            block.OnLaserHit();
        }

        
        // 이 column은 끝까지 막히지 않음.
        return endY;
    }
    private BlockCell FindBlockCellAtCoord(Vector2Int coord)
    {
        Vector3 cellCenter = blockManager.GridToWorld(coord);
        Vector2 cellSize = blockManager.GetCellSize();

        Collider2D hit = Physics2D.OverlapBox(
            cellCenter,
            cellSize * 0.8f,
            0f
        );

        if (hit == null)
            return null;

        return hit.GetComponentInParent<BlockCell>();
    }
}