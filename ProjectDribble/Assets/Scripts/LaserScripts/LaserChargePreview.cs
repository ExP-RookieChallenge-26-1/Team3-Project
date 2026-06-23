using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

public class LaserChargePreview : MonoBehaviour
{
    [SerializeField] private LaserData laserData;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private SpriteRenderer previewAreaRenderer;
    [SerializeField, Range(0f, 1f)] private float previewMinAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float previewMaxAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float targetMinAlpha = 0.25f;
    [SerializeField, Range(0f, 1f)] private float targetMaxAlpha = 0.8f;
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float targetRefreshInterval = 0.075f;
    [SerializeField] private float previewStartYOffset;
    [SerializeField] private float previewTopPadding;
    [SerializeField] private LaserBlockEraser laserBlockEraser;

    private readonly List<BlockCell> highlightedBlocks = new();
    private readonly List<int> highlightedCeilingSegments = new();
    private Vector2 laserOrigin;
    private float laserWidth;
    private float laserRange;
    private float laserStartOffset;
    private float laserBottomOffset;
    private bool affectsBelowPaddle;
    private float nextTargetRefreshTime;
    private static Sprite runtimeAreaSprite;

    public float PlayAreaTopY => laserBlockEraser != null
        ? laserBlockEraser.GetPlayAreaTopY() + previewTopPadding
        : transform.position.y;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
            lineRenderer.enabled = false;
        if (laserBlockEraser == null)
            laserBlockEraser = FindAnyObjectByType<LaserBlockEraser>();

        EnsureAreaRenderer();
        Hide();
    }

    private void Update()
    {
        float pulse01 = (Mathf.Sin(Time.time * Mathf.Max(0f, pulseSpeed)) + 1f) * 0.5f;
        if (Time.time >= nextTargetRefreshTime)
        {
            RefreshTargets();
            nextTargetRefreshTime = Time.time + Mathf.Max(0.01f, targetRefreshInterval);
        }

        SetAreaAlpha(Mathf.Lerp(previewMinAlpha, previewMaxAlpha, pulse01));
        SetTargetAlpha(Mathf.Lerp(targetMinAlpha, targetMaxAlpha, pulse01));
    }

    public void Show(Vector2 startPosition, float width, float range)
    {
        Show(startPosition, width, range, 0f);
    }

    public void Show(Vector2 startPosition, float width, float topRange, float bottomOffset)
    {
        Show(startPosition, width, topRange, 0f, bottomOffset > 0f, bottomOffset,
            startPosition.y - bottomOffset, startPosition.y + topRange);
    }

    public void Show(
        Vector2 origin, float width, float range, float startOffset,
        bool includeBelowPaddle, float bottomOffset, float paddleTopY, float playAreaTopY)
    {
        if (width <= 0f)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        EnsureAreaRenderer();
        laserOrigin = origin;
        laserWidth = width;
        laserRange = range;
        laserStartOffset = startOffset;
        affectsBelowPaddle = includeBelowPaddle;
        laserBottomOffset = bottomOffset;

        float bottomY = paddleTopY + previewStartYOffset;
        float topY = Mathf.Max(bottomY, playAreaTopY);
        float height = topY - bottomY;
        previewAreaRenderer.transform.position = new Vector3(origin.x, (bottomY + topY) * 0.5f, 0f);
        previewAreaRenderer.transform.localScale = new Vector3(width, height, 1f);
        previewAreaRenderer.enabled = true;

        if (Time.time >= nextTargetRefreshTime)
        {
            RefreshTargets();
            SetTargetAlpha(targetMinAlpha);
            nextTargetRefreshTime = Time.time + Mathf.Max(0.01f, targetRefreshInterval);
        }
    }

    public void Hide()
    {
        ClearTargets();
        if (previewAreaRenderer != null)
            previewAreaRenderer.enabled = false;
        gameObject.SetActive(false);
    }

    private void RefreshTargets()
    {
        ClearTargets();
        if (laserBlockEraser == null)
            return;

        LaserHitPreviewResult result = laserBlockEraser.CalculateLaserTargets(
            laserOrigin, laserWidth, laserRange, laserStartOffset,
            affectsBelowPaddle, laserBottomOffset);

        highlightedBlocks.AddRange(result.Blocks);
        highlightedCeilingSegments.AddRange(result.CeilingSegmentIndices);
    }

    private void SetTargetAlpha(float alpha)
    {
        for (int i = highlightedBlocks.Count - 1; i >= 0; i--)
        {
            BlockCell block = highlightedBlocks[i];
            if (block == null || !block.IsAlive)
            {
                highlightedBlocks.RemoveAt(i);
                continue;
            }
            block.SetLaserTargetPreview(true, alpha);
        }

        for (int i = 0; i < highlightedCeilingSegments.Count; i++)
            laserBlockEraser?.SetCeilingTargetPreview(highlightedCeilingSegments[i], true, alpha);
    }

    private void ClearTargets()
    {
        for (int i = 0; i < highlightedBlocks.Count; i++)
            highlightedBlocks[i]?.SetLaserTargetPreview(false, 0f);
        for (int i = 0; i < highlightedCeilingSegments.Count; i++)
            laserBlockEraser?.SetCeilingTargetPreview(highlightedCeilingSegments[i], false, 0f);

        highlightedBlocks.Clear();
        highlightedCeilingSegments.Clear();
    }

    private void EnsureAreaRenderer()
    {
        if (previewAreaRenderer != null)
            return;

        GameObject areaObject = new GameObject("PreviewArea");
        areaObject.transform.SetParent(transform, false);
        previewAreaRenderer = areaObject.AddComponent<SpriteRenderer>();
        previewAreaRenderer.sprite = GetRuntimeAreaSprite();
        previewAreaRenderer.sortingLayerName = "Default";
        previewAreaRenderer.sortingOrder = -10;
        previewAreaRenderer.color = new Color(1f, 0.05f, 0.02f, previewMinAlpha);
    }

    private void SetAreaAlpha(float alpha)
    {
        if (previewAreaRenderer == null)
            return;
        Color color = previewAreaRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        previewAreaRenderer.color = color;
    }

    private static Sprite GetRuntimeAreaSprite()
    {
        if (runtimeAreaSprite == null)
        {
            runtimeAreaSprite = Sprite.Create(
                Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            runtimeAreaSprite.name = "LaserPreviewAreaSprite";
        }
        return runtimeAreaSprite;
    }

    private void OnDisable()
    {
        ClearTargets();
    }
}
