using System.Collections.Generic;
using UnityEngine;

public class CeilingSegmentRootVisual : MonoBehaviour
{
    [Header("Tiles")]
    [SerializeField] private SpriteRenderer tilePrefab;
    [SerializeField] private Transform tileParent;

    [Header("Pulse")]
    [SerializeField] private AnimationCurve pulseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 1f),
        new Keyframe(1f, 0f)
    );
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float minAlpha = 0.55f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private Color pulseColor = Color.green;
    [SerializeField] private bool useUnscaledTime;

    private readonly List<SpriteRenderer> tiles = new();
    private readonly List<Color> baseColors = new();
    private int segmentIndex = -1;
    private bool isPulsing;

    public int SegmentIndex => segmentIndex;
    public int TileCount => tiles.Count;

    private void Awake()
    {
        if (tileParent == null)
            tileParent = transform;

        if (tilePrefab == null)
            tilePrefab = GetComponentInChildren<SpriteRenderer>(true);

        if (tilePrefab != null)
            tilePrefab.gameObject.SetActive(false);

        if (pulseCurve == null || pulseCurve.length == 0)
        {
            pulseCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, 1f),
                new Keyframe(1f, 0f)
            );
        }
    }

    private void Update()
    {
        if (!isPulsing || tiles.Count == 0)
            return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float t = Mathf.Repeat(time * Mathf.Max(0f, pulseSpeed), 1f);
        ApplyPulse(Mathf.Clamp01(pulseCurve.Evaluate(t)));
    }

    public void Initialize(int index)
    {
        segmentIndex = index;
    }

    public void BuildTiles(IReadOnlyList<Vector3> positions)
    {
        ClearTiles();

        if (tilePrefab == null || positions == null)
            return;

        Vector3 originalScale = tilePrefab.transform.localScale;

        for (int i = 0; i < positions.Count; i++)
        {
            SpriteRenderer tile = Instantiate(tilePrefab, tileParent);
            tile.name = $"RootTile_{i}";
            tile.transform.position = positions[i];
            tile.transform.localScale = originalScale;
            tile.gameObject.SetActive(true);
            tile.enabled = true;
            tiles.Add(tile);
            baseColors.Add(tile.color);
        }
    }

    public void SetActiveState(bool active)
    {
        isPulsing = active;

        if (!active)
            RestoreVisual();

        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null)
                tiles[i].enabled = active;
        }
    }

    private void ApplyPulse(float value)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            SpriteRenderer tile = tiles[i];

            if (tile == null)
                continue;

            Color color = Color.Lerp(baseColors[i], pulseColor, value);
            color.a = Mathf.Lerp(minAlpha, maxAlpha, value);
            tile.color = color;
        }
    }

    private void RestoreVisual()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] != null)
                tiles[i].color = baseColors[i];
        }
    }

    private void ClearTiles()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (tiles[i] != null)
                Destroy(tiles[i].gameObject);
        }

        tiles.Clear();
        baseColors.Clear();
    }

    private void OnDisable()
    {
        isPulsing = false;
        RestoreVisual();
    }
}
