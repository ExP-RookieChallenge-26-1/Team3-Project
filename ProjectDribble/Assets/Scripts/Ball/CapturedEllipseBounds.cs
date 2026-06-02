using UnityEngine;

public static class CapturedEllipseBounds
{
    private const int SegmentCount = 64;

    public static float GetInnerBoundY(
        Transform ellipseCenter,
        float ballX,
        bool isTop,
        float ballRadius,
        float halfWidth,
        float halfHeight
    )
    {
        float safeHalfWidth = Mathf.Max(0.0001f, halfWidth);
        float normalizedX = (ballX - ellipseCenter.position.x) / safeHalfWidth;
        normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);

        float safeHalfHeight = Mathf.Max(0f, halfHeight);
        float ellipseY = safeHalfHeight * Mathf.Sqrt(1f - normalizedX * normalizedX);

        if (isTop)
            return ellipseCenter.position.y + ellipseY - ballRadius;

        return ellipseCenter.position.y - ellipseY + ballRadius;
    }

    public static void DrawGizmo(
        Transform ellipseCenter,
        Color color,
        float halfWidth,
        float halfHeight
    )
    {
        if (ellipseCenter == null)
            return;

        float safeHalfWidth = Mathf.Max(0.0001f, halfWidth);
        float safeHalfHeight = Mathf.Max(0f, halfHeight);
        Vector3 center = ellipseCenter.position;
        Vector3 previous = GetPoint(center, safeHalfWidth, safeHalfHeight, 0f);

        Gizmos.color = color;

        for (int i = 1; i <= SegmentCount; i++)
        {
            float t = i / (float)SegmentCount;
            Vector3 next = GetPoint(center, safeHalfWidth, safeHalfHeight, t);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.DrawLine(
            center + Vector3.left * safeHalfWidth,
            center + Vector3.right * safeHalfWidth
        );

        Gizmos.DrawLine(
            center + Vector3.down * safeHalfHeight,
            center + Vector3.up * safeHalfHeight
        );
    }

    private static Vector3 GetPoint(Vector3 center, float halfWidth, float halfHeight, float t)
    {
        float angle = t * Mathf.PI * 2f;

        return new Vector3(
            center.x + Mathf.Cos(angle) * halfWidth,
            center.y + Mathf.Sin(angle) * halfHeight,
            center.z
        );
    }
}
