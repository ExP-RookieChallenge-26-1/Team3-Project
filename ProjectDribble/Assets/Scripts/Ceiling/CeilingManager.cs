using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class CeilingManager : MonoBehaviour
{
    public event Action OnStageCleared;
    public event Action<CeilingSegment> OnCeilingSegmentDestroyed;

    private const int LeftSegmentStartX = 0;
    private const int LeftSegmentEndX = 1;
    private const int CenterSegmentStartX = 2;
    private const int CenterSegmentEndX = 4;
    private const int RightSegmentStartX = 5;
    private const int RightSegmentEndX = 6;

    [Header("Data")]
    [SerializeField] private HealthData healthData;

    private float currentHp;
    private float runtimeMaxHp;
    private CeilingSegmentMode ceilingSegmentMode = CeilingSegmentMode.ThreeSegments;
    private bool isStageCleared;
    private bool isInitialized;
    private bool isCeilingEnabled;
    private bool damageEnabled = true;

    public bool IsCeilingEnabled => isCeilingEnabled;

    [Header("Brick Spawn")]
    [SerializeField] private CeilingBrick ceilingBrickPrefab;

    [SerializeField] private int columnCount = 7;
    [SerializeField] private int rowCount = 2;

    [SerializeField] private Vector2 brickSize = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 startPosition;

    [Header("Sprites")]
    [SerializeField] private Sprite[] ceilingSprites;

    [Header("Ceiling Core")]
    [SerializeField] private CeilingCore ceilingCorePrefab;
    [SerializeField] private Transform ceilingCoreParent;
    [SerializeField] private Vector2 ceilingCoreOffset;

    [Header("Segments")]
    [SerializeField] private int leftSegmentMaxHp;
    [SerializeField] private int centerSegmentMaxHp;
    [SerializeField] private int rightSegmentMaxHp;

    [Header("Segment Root Visuals")]
    [SerializeField] private CeilingSegmentRootVisual segmentRootVisualPrefab;
    [SerializeField] private Transform segmentRootVisualParent;
    [SerializeField] private float segmentRootVisualYOffset = -0.3f;
    [SerializeField] private float segmentRootVisualEdgePadding;

    [Header("Ball Control")]
    [SerializeField] private BallRespawner ballRespawner;

    [Header("Gauge")]
    [SerializeField] private GaugeManager _gaugeManager;
    [Header("Block Growth")]
    [SerializeField] private BlockManager blockManager;

    private readonly List<CeilingBrick> aliveBricks = new();
    private readonly List<CeilingSegment> segments = new();
    private readonly List<CeilingCore> ceilingCores = new();
    private readonly List<CeilingSegmentRootVisual> segmentRootVisuals = new();

    private void Awake()
    {
        runtimeMaxHp = healthData != null ? Mathf.Max(1, healthData.ceilingMaxHp) : 1f;

        if (healthData == null)
            Debug.LogWarning("CeilingManager: HealthData is missing. Using a fallback ceiling HP of 1.");

        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeCeiling(runtimeMaxHp);
        }
    }

    public void InitializeCeiling(float maxHp)
    {
        InitializeCeiling(maxHp, ceilingSegmentMode);
    }

    public void InitializeCeiling(float maxHp, CeilingSegmentMode segmentMode)
    {
        runtimeMaxHp = Mathf.Max(1, maxHp);
        ceilingSegmentMode = segmentMode;
        isInitialized = true;
        isCeilingEnabled = true;
        damageEnabled = true;
        ResetCeilingState();
    }

    public void DisableCeiling()
    {
        isInitialized = true;
        isCeilingEnabled = false;
        damageEnabled = false;
        isStageCleared = false;
        currentHp = 0f;

        ClearCeilingCores();
        ClearSegmentRootVisuals();
        ClearCeilingBricks();
        segments.Clear();
    }

    public void SetDamageEnabled(bool enabled)
    {
        damageEnabled = isCeilingEnabled && enabled;
    }

    public void SetCeilingVisible(bool visible)
    {
        for (int i = 0; i < aliveBricks.Count; i++)
        {
            CeilingBrick brick = aliveBricks[i];

            if (brick == null || !brick.gameObject.activeSelf)
                continue;

            SpriteRenderer[] renderers = brick.GetComponentsInChildren<SpriteRenderer>(true);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                renderers[rendererIndex].enabled = visible;
        }

        for (int i = 0; i < ceilingCores.Count; i++)
            ceilingCores[i]?.SetVisible(visible);

        for (int i = 0; i < segmentRootVisuals.Count; i++)
        {
            CeilingSegmentRootVisual visual = segmentRootVisuals[i];

            if (visual != null)
                visual.SetActiveState(visible && IsSegmentAliveByIndex(i));
        }
    }

    public void SetCeilingCollisionEnabled(bool enabled)
    {
        for (int i = 0; i < aliveBricks.Count; i++)
        {
            CeilingBrick brick = aliveBricks[i];

            if (brick == null || !brick.gameObject.activeSelf)
                continue;

            Collider2D[] colliders = brick.GetComponentsInChildren<Collider2D>(true);

            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                colliders[colliderIndex].enabled = enabled;
        }
    }

    public void ResetCeilingState()
    {
        isStageCleared = false;
        currentHp = 0;

        ClearCeilingCores();
        ClearSegmentRootVisuals();
        ClearCeilingBricks();
        ResetSegments();
        CreateCeilingBricks();
        CreateCeilingCores();
        CreateSegmentRootVisuals();
    }

    private void ResetSegments()
    {
        segments.Clear();

        float maxHp = runtimeMaxHp > 0
            ? runtimeMaxHp
            : healthData != null ? Mathf.Max(1, healthData.ceilingMaxHp) : 1f;

        switch (ceilingSegmentMode)
        {
            case CeilingSegmentMode.OneSegment:
                AddOneSegment(maxHp);
                break;
            case CeilingSegmentMode.TwoSegments:
                AddTwoSegments(maxHp);
                break;
            default:
                AddThreeSegments(maxHp);
                break;
        }

        UpdateCurrentHpFromSegments();
    }

    private void AddOneSegment(float maxHp)
    {
        segments.Add(new CeilingSegment(
            "All",
            0,
            Mathf.Max(0, columnCount - 1),
            maxHp
        ));
    }

    private void AddTwoSegments(float maxHp)
    {
        int splitX = Mathf.Max(1, columnCount / 2);
        int leftStartX = 0;
        int leftEndX = splitX - 1;
        int rightStartX = splitX;
        int rightEndX = Mathf.Max(rightStartX, columnCount - 1);

        float leftHp = leftSegmentMaxHp > 0
            ? leftSegmentMaxHp
            : CalculateDefaultSegmentHp(maxHp, leftEndX - leftStartX + 1);
        float rightHp = rightSegmentMaxHp > 0
            ? rightSegmentMaxHp
            : CalculateDefaultSegmentHp(maxHp, rightEndX - rightStartX + 1);

        segments.Add(new CeilingSegment("Left", leftStartX, leftEndX, leftHp));
        segments.Add(new CeilingSegment("Right", rightStartX, rightEndX, rightHp));
    }

    private void AddThreeSegments(float maxHp)
    {
        float leftHp = leftSegmentMaxHp > 0 ? leftSegmentMaxHp : CalculateDefaultSegmentHp(maxHp, 2);
        float centerHp = centerSegmentMaxHp > 0 ? centerSegmentMaxHp : CalculateDefaultSegmentHp(maxHp, 3);
        float rightHp = rightSegmentMaxHp > 0 ? rightSegmentMaxHp : CalculateDefaultSegmentHp(maxHp, 2);

        segments.Add(new CeilingSegment("Left", LeftSegmentStartX, LeftSegmentEndX, leftHp));
        segments.Add(new CeilingSegment("Center", CenterSegmentStartX, CenterSegmentEndX, centerHp));
        segments.Add(new CeilingSegment("Right", RightSegmentStartX, RightSegmentEndX, rightHp));
    }

    private int CalculateDefaultSegmentHp(float totalHp, int segmentWidth)
    {
        float widthRatio = segmentWidth / (float)Mathf.Max(1, columnCount);
        return Mathf.Max(1, Mathf.RoundToInt(totalHp * widthRatio));
    }

    private void CreateCeilingBricks()
    {
        if (ceilingBrickPrefab == null)
        {
            Debug.LogWarning("CeilingManager: CeilingBrick prefab is missing. Ceiling bricks were not created.");
            return;
        }

        for (int y = 0; y < rowCount; y++)
        {
            for (int x = 0; x < columnCount; x++)
            {
                int index = y * columnCount + x;

                Vector2 spawnPosition = startPosition + new Vector2(
                    x * brickSize.x,
                    -y * brickSize.y
                );

                CeilingBrick brick = Instantiate(
                    ceilingBrickPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    transform
                );

                Sprite sprite = null;

                if (ceilingSprites != null && index < ceilingSprites.Length)
                    sprite = ceilingSprites[index];

                brick.Init(this, new Vector2Int(x, y), sprite);
                aliveBricks.Add(brick);
            }
        }
    }

    private void ClearCeilingBricks()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (ceilingCoreParent != null && child == ceilingCoreParent)
                continue;

            if (segmentRootVisualParent != null && child == segmentRootVisualParent)
                continue;

            Destroy(child.gameObject);
        }

        aliveBricks.Clear();
    }

    public void TakeDamage(float damage, CeilingBrick hitBrick)
    {
        if (hitBrick == null)
            return;

        DamageSegmentByCoord(hitBrick.Coord, damage);
    }

    public void DamageSegmentByCoord(Vector2Int coord, float damage)
    {
        DamageSegmentByX(coord.x, damage);
    }

    public void DamageSegmentByX(int x, float damage)
    {
        if (!isCeilingEnabled || !damageEnabled)
            return;

        if (isStageCleared)
            return;

        CeilingSegment segment = GetSegmentByX(x);

        if (segment == null)
        {
            Debug.LogWarning($"CeilingManager: x={x} does not belong to a ceiling segment.");
            return;
        }

        if (segment.IsDestroyed)
        {
            Debug.Log($"Ceiling segment {segment.SegmentName} is already destroyed.");
            return;
        }

        bool destroyed = segment.ApplyDamage(damage);
        int segmentIndex = segments.IndexOf(segment);
        UpdateCurrentHpFromSegments();

        Debug.Log(
            $"Ceiling segment {segment.SegmentName} damaged by {damage}. HP {segment.CurrentHp}/{segment.MaxHp}"
        );

        PlayCoreDamageFlash(segmentIndex);
        UpdateSegmentBlockVisuals(segment);

        if (destroyed)
        {
            SetCoreAlive(segmentIndex, false);
            SetSegmentRootVisualState(segmentIndex, false);
            SoundManager.Instance?.Play(SoundId.CeilingBreak);
            Debug.Log($"Ceiling segment {segment.SegmentName} destroyed.");
            DisableStemGrowthForSegment(segment);
            OnCeilingSegmentDestroyed?.Invoke(segment);
            BreakSegmentBricks(segment);
            if (ballRespawner != null)
                ballRespawner.RecallBallToPaddle();
            else
                Debug.LogWarning("CeilingManager: BallRespawner is missing; the ball was not recalled.");
            //_gaugeManager.AddGauge(_gaugeManager.GaugePerSegment);
        }
        else
        {
            float ratio = 1f - segment.GetHpPercent();
            float hitRatio = Mathf.Clamp01(ratio);
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayCeilingHitFeedback(hitRatio);
            else
                SoundManager.Instance?.Play(SoundId.CeilingHit, hitRatio);
        }

        if (AreAllSegmentsDestroyed())
        {
            Die();
        }
    }

    public bool IsSegmentDestroyedByX(int x)
    {
        CeilingSegment segment = GetSegmentByX(x);
        return segment != null && segment.IsDestroyed;
    }

    public bool IsSegmentAliveByIndex(int segmentIndex)
    {
        CeilingSegment segment = GetSegmentByIndex(segmentIndex);
        return segment != null && !segment.IsDestroyed;
    }

    public bool TryGetSegmentXRange(int segmentIndex, out int startX, out int endX)
    {
        CeilingSegment segment = GetSegmentByIndex(segmentIndex);

        if (segment == null)
        {
            startX = 0;
            endX = -1;
            return false;
        }

        startX = segment.StartX;
        endX = segment.EndX;
        return true;
    }

    public float GetSegmentHpPercentByX(int x)
    {
        CeilingSegment segment = GetSegmentByX(x);
        return segment == null ? 0f : segment.GetHpPercent();
    }

    public bool TryGetAliveSegmentIndexAtWorldX(float worldX, out int segmentIndex)
    {
        if (!isCeilingEnabled)
        {
            segmentIndex = -1;
            return false;
        }

        int x = Mathf.RoundToInt((worldX - startPosition.x) / Mathf.Max(0.0001f, brickSize.x));
        CeilingSegment segment = GetSegmentByX(x);
        segmentIndex = segments.IndexOf(segment);
        return segmentIndex >= 0 && !segment.IsDestroyed;
    }

    public void SetLaserTargetPreview(int segmentIndex, bool active, float alpha)
    {
        if (segmentIndex < 0 || segmentIndex >= segmentRootVisuals.Count)
            return;

        CeilingSegmentRootVisual visual = segmentRootVisuals[segmentIndex];
        visual?.SetLaserTargetPreview(active && IsSegmentAliveByIndex(segmentIndex), alpha);
    }

    public bool AreAllSegmentsDestroyed()
    {
        return segments.Count > 0 && segments.TrueForAll(segment => segment.IsDestroyed);
    }

    public void SetCoreConnected(int segmentIndex, bool connected)
    {
        CeilingCore core = GetCoreByIndex(segmentIndex);

        if (core == null)
            return;

        core.SetConnectedState(connected && IsSegmentAliveByIndex(segmentIndex));
    }

    public void SetCoreConnections(bool[] connectedSegments)
    {
        for (int i = 0; i < ceilingCores.Count; i++)
        {
            CeilingCore core = ceilingCores[i];

            if (core == null)
                continue;

            bool connected = connectedSegments != null &&
                             i < connectedSegments.Length &&
                             connectedSegments[i];

            core.SetConnectedState(connected && IsSegmentAliveByIndex(i));
        }
    }

    public void UpdateSegmentBlockVisuals(CeilingSegment segment)
    {
        if (segment == null)
            return;

        float hpRatio = Mathf.Clamp01(segment.GetHpPercent());
        int totalBlockCount = GetSegmentTotalBlockCount(segment);
        int targetAliveCount = Mathf.CeilToInt(totalBlockCount * hpRatio);

        int minVisibleCount = GetMinVisibleBlockCount(segment, totalBlockCount);

        if (segment.CurrentHp > 0)
            targetAliveCount = Mathf.Max(targetAliveCount, minVisibleCount);
        else
            targetAliveCount = 0;

        targetAliveCount = Mathf.Clamp(targetAliveCount, 0, totalBlockCount);

        int currentAliveCount = GetAliveBlockCountInSegment(segment);
        int removeCount = Mathf.Max(0, currentAliveCount - targetAliveCount);

     /*   Debug.Log(
            $"Ceiling segment {segment.SegmentName} visual update. HP ratio {hpRatio:F2}, total blocks {totalBlockCount}, target alive {targetAliveCount}, current alive {currentAliveCount}, remove {removeCount}"
        );
*/
        if (removeCount > 0)
        {
            DestroyRandomBlocksInSegment(segment, removeCount);
        }
    }

    private int GetMinVisibleBlockCount(CeilingSegment segment, int totalBlockCount)
    {
        int segmentWidth = Mathf.Max(0, segment.EndX - segment.StartX + 1);
        int minVisibleCount = segments.Count == 1
            ? 5
            : segmentWidth <= 2 ? 2 : 3;
        return Mathf.Clamp(minVisibleCount, 0, totalBlockCount);
    }

    private List<CeilingBrick> SelectDistributedAnchorBricks(
        CeilingSegment segment,
        List<CeilingBrick> candidates,
        int anchorCount
    )
    {
        List<CeilingBrick> selected = new();

        for (int anchorIndex = 0; anchorIndex < anchorCount; anchorIndex++)
        {
            float anchorRatio = anchorCount <= 1
                ? 0.5f
                : anchorIndex / (float)(anchorCount - 1);
            float anchorX = Mathf.Lerp(segment.StartX, segment.EndX, anchorRatio);
            int preferredRow = rowCount > 0 ? anchorIndex % rowCount : 0;
            CeilingBrick closestBrick = null;
            float closestXDistance = float.MaxValue;
            int closestRowDistance = int.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                CeilingBrick candidate = candidates[i];

                if (selected.Contains(candidate))
                    continue;

                float xDistance = Mathf.Abs(candidate.Coord.x - anchorX);
                int rowDistance = Mathf.Abs(candidate.Coord.y - preferredRow);

                if (xDistance < closestXDistance ||
                    (Mathf.Approximately(xDistance, closestXDistance) && rowDistance < closestRowDistance))
                {
                    closestBrick = candidate;
                    closestXDistance = xDistance;
                    closestRowDistance = rowDistance;
                }
            }

            if (closestBrick != null)
                selected.Add(closestBrick);
        }

        return selected;
    }

    public void DestroyRandomBlocksInSegment(CeilingSegment segment, int count)
    {
        if (segment == null || count <= 0)
            return;

        List<CeilingBrick> segmentAliveBricks = GetAliveBricksInSegment(segment);
        int destroyCount = Mathf.Clamp(count, 0, segmentAliveBricks.Count);
        int anchorCount = 0;

        if (segment.CurrentHp > 0)
        {
            anchorCount = GetMinVisibleBlockCount(segment, segmentAliveBricks.Count);
            destroyCount = Mathf.Min(destroyCount, segmentAliveBricks.Count - anchorCount);
        }

        List<CeilingBrick> protectedBricks = SelectDistributedAnchorBricks(
            segment,
            segmentAliveBricks,
            anchorCount
        );

        for (int i = 0; i < protectedBricks.Count; i++)
            segmentAliveBricks.Remove(protectedBricks[i]);

        for (int i = 0; i < destroyCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, segmentAliveBricks.Count);
            CeilingBrick brick = segmentAliveBricks[randomIndex];
            segmentAliveBricks.RemoveAt(randomIndex);

            aliveBricks.Remove(brick);
            Debug.Log($"Ceiling segment {segment.SegmentName} random block destroyed at {brick.Coord}.");
            brick.Break();
        }
    }

    private void BreakSegmentBricks(CeilingSegment segment)
    {
        for (int i = aliveBricks.Count - 1; i >= 0; i--)
        {
            CeilingBrick brick = aliveBricks[i];

            if (brick == null || !segment.ContainsX(brick.Coord.x))
                continue;

            aliveBricks.RemoveAt(i);
            brick.Break();
        }
    }

    private void CreateCeilingCores()
    {
        if (ceilingCorePrefab == null)
            return;

        Transform parent = ceilingCoreParent != null ? ceilingCoreParent : transform;

        for (int i = 0; i < segments.Count; i++)
        {
            CeilingSegment segment = segments[i];

            if (segment == null)
                continue;

            CeilingCore core = Instantiate(
                ceilingCorePrefab,
                GetCorePosition(segment),
                Quaternion.identity,
                parent
            );

            core.Initialize(i);
            core.SetAliveState(!segment.IsDestroyed);
            ceilingCores.Add(core);
        }
    }

    private void ClearCeilingCores()
    {
        for (int i = ceilingCores.Count - 1; i >= 0; i--)
        {
            CeilingCore core = ceilingCores[i];

            if (core != null)
                Destroy(core.gameObject);
        }

        ceilingCores.Clear();
    }

    private void CreateSegmentRootVisuals()
    {
        if (segmentRootVisualPrefab == null)
            return;

        Transform parent = segmentRootVisualParent != null ? segmentRootVisualParent : transform;

        for (int i = 0; i < segments.Count; i++)
        {
            CeilingSegment segment = segments[i];

            if (segment == null)
                continue;

            CeilingSegmentRootVisual visual = Instantiate(
                segmentRootVisualPrefab,
                GetSegmentRootVisualPosition(segment),
                Quaternion.identity,
                parent
            );

            visual.Initialize(i);
            visual.BuildTiles(GetSegmentRootVisualTilePositions(segment));
            segmentRootVisuals.Add(visual);
        }

        RefreshSegmentRootVisuals();
    }

    private void ClearSegmentRootVisuals()
    {
        for (int i = segmentRootVisuals.Count - 1; i >= 0; i--)
        {
            CeilingSegmentRootVisual visual = segmentRootVisuals[i];

            if (visual != null)
                Destroy(visual.gameObject);
        }

        segmentRootVisuals.Clear();
    }

    public void RefreshSegmentRootVisuals()
    {
        for (int i = 0; i < segmentRootVisuals.Count; i++)
            SetSegmentRootVisualState(i, IsSegmentAliveByIndex(i));
    }

    public void SetSegmentRootVisualState(int segmentIndex, bool active)
    {
        if (segmentIndex < 0 || segmentIndex >= segmentRootVisuals.Count)
            return;

        CeilingSegmentRootVisual visual = segmentRootVisuals[segmentIndex];
        visual?.SetActiveState(active && IsSegmentAliveByIndex(segmentIndex));
    }

    private Vector3 GetSegmentRootVisualPosition(CeilingSegment segment)
    {
        float centerX = startPosition.x + ((segment.StartX + segment.EndX) * 0.5f * brickSize.x);
        float bottomY = startPosition.y - (Mathf.Max(1, rowCount) - 1) * brickSize.y;
        return new Vector3(centerX, bottomY + segmentRootVisualYOffset, transform.position.z);
    }

    private List<Vector3> GetSegmentRootVisualTilePositions(CeilingSegment segment)
    {
        int tileCount = Mathf.Max(0, segment.EndX - segment.StartX + 1);
        List<Vector3> positions = new(tileCount);
        float bottomY = startPosition.y - (Mathf.Max(1, rowCount) - 1) * brickSize.y;

        for (int x = segment.StartX; x <= segment.EndX; x++)
        {
            float edgeOffset = 0f;

            if (tileCount > 1 && x == segment.StartX)
                edgeOffset = segmentRootVisualEdgePadding;
            else if (tileCount > 1 && x == segment.EndX)
                edgeOffset = -segmentRootVisualEdgePadding;

            float columnCenterX = startPosition.x + x * brickSize.x + edgeOffset;
            positions.Add(new Vector3(
                columnCenterX,
                bottomY + segmentRootVisualYOffset,
                transform.position.z
            ));
        }

        return positions;
    }

    private Vector3 GetCorePosition(CeilingSegment segment)
    {
        float centerX = startPosition.x + ((segment.StartX + segment.EndX) * 0.5f * brickSize.x);
        float centerY = startPosition.y - ((Mathf.Max(1, rowCount) - 1) * 0.5f * brickSize.y);
        Vector2 position = new Vector2(centerX, centerY) + ceilingCoreOffset;
        return new Vector3(position.x, position.y, transform.position.z);
    }

    private CeilingCore GetCoreByIndex(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= ceilingCores.Count)
            return null;

        return ceilingCores[segmentIndex];
    }

    private void PlayCoreDamageFlash(int segmentIndex)
    {
        CeilingCore core = GetCoreByIndex(segmentIndex);
        core?.PlayDamageFlash();
    }

    private void SetCoreAlive(int segmentIndex, bool alive)
    {
        CeilingCore core = GetCoreByIndex(segmentIndex);
        core?.SetAliveState(alive);
    }

    private void DisableStemGrowthForSegment(CeilingSegment segment)
    {
        if (segment == null || blockManager == null)
            return;

        int segmentIndex = segments.IndexOf(segment);

        if (segmentIndex >= 0)
            blockManager.DisableStemGrowthByCeilingSegment(segmentIndex);

        blockManager.DisableStemGrowthByStartXRange(segment.StartX, segment.EndX);
    }
    
    private CeilingSegment GetSegmentByIndex(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= segments.Count)
            return null;

        return segments[segmentIndex];
    }

    private CeilingSegment GetSegmentByX(int x)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].ContainsX(x))
                return segments[i];
        }

        return null;
    }

    private int GetSegmentTotalBlockCount(CeilingSegment segment)
    {
        int width = Mathf.Max(0, segment.EndX - segment.StartX + 1);
        return width * Mathf.Max(0, rowCount);
    }

    private int GetAliveBlockCountInSegment(CeilingSegment segment)
    {
        return GetAliveBricksInSegment(segment).Count;
    }

    private List<CeilingBrick> GetAliveBricksInSegment(CeilingSegment segment)
    {
        List<CeilingBrick> result = new();

        for (int i = 0; i < aliveBricks.Count; i++)
        {
            CeilingBrick brick = aliveBricks[i];

            if (brick == null)
                continue;

            if (!brick.gameObject.activeSelf)
                continue;

            if (!segment.ContainsX(brick.Coord.x))
                continue;

            result.Add(brick);
        }

        return result;
    }

    private void UpdateCurrentHpFromSegments()
    {
        currentHp = 0;

        for (int i = 0; i < segments.Count; i++)
        {
            currentHp += segments[i].CurrentHp;
        }
    }

    private void Die()
    {
        if (isStageCleared)
            return;

        isStageCleared = true;
        Debug.Log("Ceiling broken / Stage clear");
        OnStageCleared?.Invoke();
    }
}
