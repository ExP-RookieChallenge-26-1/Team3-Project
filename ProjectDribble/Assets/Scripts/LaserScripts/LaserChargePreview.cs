using UnityEngine;

public class LaserChargePreview : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Color")]
    [SerializeField] private Color previewColor = Color.red;

    [Header("Visual")]
    [SerializeField] private float lineWidth = 0.05f;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 5;
        lineRenderer.loop = false;
        lineRenderer.useWorldSpace = true;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.startColor = previewColor;
        lineRenderer.endColor = previewColor;

        Hide();
    }

    public void Show(Vector2 startPosition, float width, float range)
    {
        gameObject.SetActive(true);

        float halfWidth = width * 0.5f;

        Vector3 bottomLeft = new Vector3(
            startPosition.x - halfWidth,
            startPosition.y,
            0f
        );

        Vector3 topLeft = new Vector3(
            startPosition.x - halfWidth,
            startPosition.y + range,
            0f
        );

        Vector3 topRight = new Vector3(
            startPosition.x + halfWidth,
            startPosition.y + range,
            0f
        );

        Vector3 bottomRight = new Vector3(
            startPosition.x + halfWidth,
            startPosition.y,
            0f
        );

        lineRenderer.SetPosition(0, bottomLeft);
        lineRenderer.SetPosition(1, topLeft);
        lineRenderer.SetPosition(2, topRight);
        lineRenderer.SetPosition(3, bottomRight);
        lineRenderer.SetPosition(4, bottomLeft);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}