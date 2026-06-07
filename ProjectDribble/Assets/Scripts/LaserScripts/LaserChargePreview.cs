using ScriptableObjects;
using UnityEngine;

public class LaserChargePreview : MonoBehaviour
{
    [SerializeField] private LaserData laserData;
    [SerializeField] private LineRenderer lineRenderer;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 5;
        lineRenderer.loop = false;
        lineRenderer.useWorldSpace = true;

        if (laserData != null)
        {
            lineRenderer.startWidth = laserData.previewLineWidth;
            lineRenderer.endWidth = laserData.previewLineWidth;

            lineRenderer.startColor = laserData.previewColor;
            lineRenderer.endColor = laserData.previewColor;
        }

        Hide();
    }

    public void Show(Vector2 startPosition, float width, float range)
    {
        if (width <= 0f)
        {
            Hide();
            return;
        }

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
