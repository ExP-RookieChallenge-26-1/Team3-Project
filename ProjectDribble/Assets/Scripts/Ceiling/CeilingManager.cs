using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class CeilingManager : MonoBehaviour
{
    public event Action OnStageCleared;

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
    private bool isStageCleared;
    private bool isInitialized;

    [Header("Brick Spawn")]
    [SerializeField] private CeilingBrick ceilingBrickPrefab;

    [SerializeField] private int columnCount = 7;
    [SerializeField] private int rowCount = 2;

    [SerializeField] private Vector2 brickSize = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 startPosition;

    [Header("Sprites")]
    [SerializeField] private Sprite[] ceilingSprites;

    [Header("Segments")]
    [SerializeField] private int leftSegmentMaxHp;
    [SerializeField] private int centerSegmentMaxHp;
    [SerializeField] private int rightSegmentMaxHp;

    [Header("Ball Control")]
    [SerializeField] private Transform ball;
    [SerializeField] private float forceDownSpeed = 10f;
    [SerializeField] private bool forceBallDownOnSegmentDestroyed = true;

    private readonly List<CeilingBrick> aliveBricks = new();
    private readonly List<CeilingSegment> segments = new();

    private void Awake()
    {
        runtimeMaxHp = Mathf.Max(1, healthData.ceilingMaxHp);
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
        runtimeMaxHp = Mathf.Max(1, maxHp);
        isInitialized = true;
        ResetCeilingState();
    }

    public void ResetCeilingState()
    {
        isStageCleared = false;
        currentHp = 0;

        ClearCeilingBricks();
        ResetSegments();
        CreateCeilingBricks();
    }

    private void ResetSegments()
    {
        segments.Clear();

        float maxHp = runtimeMaxHp > 0 ? runtimeMaxHp : Mathf.Max(1, healthData.ceilingMaxHp);
        float leftHp = leftSegmentMaxHp > 0 ? leftSegmentMaxHp : CalculateDefaultSegmentHp(maxHp, 2);
        float centerHp = centerSegmentMaxHp > 0 ? centerSegmentMaxHp : CalculateDefaultSegmentHp(maxHp, 3);
        float rightHp = rightSegmentMaxHp > 0 ? rightSegmentMaxHp : CalculateDefaultSegmentHp(maxHp, 2);

        segments.Add(new CeilingSegment("Left", LeftSegmentStartX, LeftSegmentEndX, leftHp));
        segments.Add(new CeilingSegment("Center", CenterSegmentStartX, CenterSegmentEndX, centerHp));
        segments.Add(new CeilingSegment("Right", RightSegmentStartX, RightSegmentEndX, rightHp));

        UpdateCurrentHpFromSegments();
    }

    private int CalculateDefaultSegmentHp(float totalHp, int segmentWidth)
    {
        float widthRatio = segmentWidth / (float)Mathf.Max(1, columnCount);
        return Mathf.Max(1, Mathf.RoundToInt(totalHp * widthRatio));
    }

    private void CreateCeilingBricks()
    {
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
            Destroy(transform.GetChild(i).gameObject);
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
        UpdateCurrentHpFromSegments();

        Debug.Log(
            $"Ceiling segment {segment.SegmentName} damaged by {damage}. HP {segment.CurrentHp}/{segment.MaxHp}"
        );

        UpdateSegmentBlockVisuals(segment);

        if (destroyed)
        {
            SoundManager.Instance.Play(SoundId.CeilingBreak);
            Debug.Log($"Ceiling segment {segment.SegmentName} destroyed.");
            BreakSegmentBricks(segment);
            ForceBallDown();
        }
        else
        {
            float ratio = 1f - segment.GetHpPercent();
            SoundManager.Instance.Play(SoundId.CeilingHit, Mathf.Clamp01(ratio));
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

    public float GetSegmentHpPercentByX(int x)
    {
        CeilingSegment segment = GetSegmentByX(x);
        return segment == null ? 0f : segment.GetHpPercent();
    }

    public bool AreAllSegmentsDestroyed()
    {
        return segments.Count > 0 && segments.TrueForAll(segment => segment.IsDestroyed);
    }

    public void UpdateSegmentBlockVisuals(CeilingSegment segment)
    {
        if (segment == null)
            return;

        float hpRatio = Mathf.Clamp01(segment.GetHpPercent());
        int totalBlockCount = GetSegmentTotalBlockCount(segment);
        int targetAliveCount = Mathf.CeilToInt(totalBlockCount * hpRatio);
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

    public void DestroyRandomBlocksInSegment(CeilingSegment segment, int count)
    {
        if (segment == null || count <= 0)
            return;

        List<CeilingBrick> segmentAliveBricks = GetAliveBricksInSegment(segment);
        int destroyCount = Mathf.Clamp(count, 0, segmentAliveBricks.Count);

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

    public void ForceBallDown()
    {
        if (!forceBallDownOnSegmentDestroyed)
            return;

        if (ball == null)
        {
            Debug.LogWarning("CeilingManager: ball reference is missing. Cannot force ball down.");
            return;
        }

        BallController ballController = ball.GetComponent<BallController>();
        BallMovement ballMovement = ball.GetComponent<BallMovement>();

        if (ballController != null)
        {
            ballController.SetBallDirection(Vector2.down.x, Vector2.down.y);
        }

        if (ballMovement != null)
        {
            float targetSpeed = Mathf.Max(ballMovement.speed, forceDownSpeed);
            ballMovement.SetBallSpeed(targetSpeed);
            Debug.Log($"CeilingManager: forced ball down. Speed {targetSpeed}.");
            return;
        }

        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            float targetSpeed = Mathf.Max(rb.linearVelocity.magnitude, forceDownSpeed);
            rb.linearVelocity = Vector2.down * targetSpeed;
            Debug.Log($"CeilingManager: forced Rigidbody2D ball down. Speed {targetSpeed}.");
            return;
        }

        Debug.LogWarning("CeilingManager: ball has no BallMovement or Rigidbody2D component.");
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
