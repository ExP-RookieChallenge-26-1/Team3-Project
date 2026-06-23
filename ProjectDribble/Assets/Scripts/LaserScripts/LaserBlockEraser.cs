using System.Collections.Generic;
using UnityEngine;

public sealed class LaserHitPreviewResult
{
    public readonly List<BlockCell> Blocks = new();
    public readonly List<int> CeilingSegmentIndices = new();
    public Vector2 EndPoint;
}

public class LaserBlockEraser : MonoBehaviour
{
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private CeilingManager ceilingManager;

    private void Awake()
    {
        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();
        if (ceilingManager == null)
            ceilingManager = FindAnyObjectByType<CeilingManager>();
    }

    public Vector2 EraseByLaser(Vector2 origin, float width, float range, float startOffset)
    {
        return EraseByLaser(origin, width, range, startOffset, false, 0f);
    }

    public Vector2 EraseByLaser(
        Vector2 origin, float width, float range, float startOffset,
        bool affectsBelowPaddle, float bottomOffset)
    {
        LaserHitPreviewResult result = CalculateLaserTargets(
            origin, width, range, startOffset, affectsBelowPaddle, bottomOffset);

        for (int i = 0; i < result.Blocks.Count; i++)
        {
            BlockCell block = result.Blocks[i];
            if (block != null && block.IsAlive)
                block.OnLaserHit();
        }

        return result.EndPoint;
    }

    public LaserHitPreviewResult CalculateLaserTargets(
        Vector2 origin, float width, float range, float startOffset,
        bool affectsBelowPaddle, float bottomOffset)
    {
        LaserHitPreviewResult result = new();
        if (blockManager == null || width <= 0f)
        {
            result.EndPoint = origin;
            return result;
        }

        Vector2 start = origin + Vector2.up * startOffset;
        int minX = blockManager.WorldXToGridX(start.x - width * 0.5f);
        int maxX = blockManager.WorldXToGridX(start.x + width * 0.5f);
        int centerX = blockManager.WorldXToGridX(origin.x);
        float bottomY = affectsBelowPaddle
            ? Mathf.Min(
                origin.y - Mathf.Max(0f, bottomOffset),
                blockManager.GetBottomBoundaryY()
            )
            : start.y;
        float boundaryY = blockManager.GetTopBoundaryY();
        float topY = Mathf.Min(boundaryY, start.y + Mathf.Max(0f, range));
        float centerColumnEndY = topY;

        for (int x = minX; x <= maxX; x++)
        {
            float columnEndY = CollectColumnTargets(x, bottomY, topY, result.Blocks);
            if (x == centerX)
                centerColumnEndY = columnEndY;
        }

        result.EndPoint = new Vector2(origin.x, centerColumnEndY);
        bool reachesCeiling = Mathf.Approximately(topY, boundaryY) &&
                              Mathf.Approximately(centerColumnEndY, topY);
        if (reachesCeiling && ceilingManager != null &&
            ceilingManager.TryGetAliveSegmentIndexAtWorldX(origin.x, out int segmentIndex))
        {
            result.CeilingSegmentIndices.Add(segmentIndex);
        }

        return result;
    }

    public float GetPlayAreaTopY()
    {
        return blockManager != null ? blockManager.GetTopBoundaryY() : transform.position.y;
    }

    public void SetCeilingTargetPreview(int segmentIndex, bool active, float alpha)
    {
        ceilingManager?.SetLaserTargetPreview(segmentIndex, active, alpha);
    }

    private float CollectColumnTargets(int x, float startY, float endY, List<BlockCell> targets)
    {
        for (int y = blockManager.Height - 1; y >= 0; y--)
        {
            Vector2Int coord = new Vector2Int(x, y);
            if (!blockManager.IsValidCoord(coord) || !blockManager.IsOccupied(coord))
                continue;

            Vector3 cellCenter = blockManager.GridToWorld(coord);
            if (cellCenter.y < startY || cellCenter.y > endY)
                continue;

            BlockCell block = blockManager.GetBlockCell(coord);
            if (block == null || !block.IsAlive)
                continue;

            targets.Add(block);
            if (block.IsFixed)
                return cellCenter.y - blockManager.GetCellHeight() * 0.5f;
        }

        return endY;
    }
}
