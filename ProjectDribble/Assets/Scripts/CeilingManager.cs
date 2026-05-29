using System;
using System.Collections.Generic;
using UnityEngine;

public class CeilingManager : MonoBehaviour
{
    public event Action OnStageCleared;

    [Header("Data")]
    [SerializeField] private HealthData healthData;

    private int currentHp;
    private int runtimeMaxHp;
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

    private readonly List<CeilingBrick> aliveBricks = new();
    private int totalBrickCount;

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

    public void InitializeCeiling(int maxHp)
    {
        runtimeMaxHp = Mathf.Max(1, maxHp);
        isInitialized = true;
        ResetCeilingState();
    }

    public void ResetCeilingState()
    {
        isStageCleared = false;
        currentHp = runtimeMaxHp > 0 ? runtimeMaxHp : Mathf.Max(1, healthData.ceilingMaxHp);

        ClearCeilingBricks();
        CreateCeilingBricks();
        totalBrickCount = aliveBricks.Count;
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

                brick.Init(this, sprite);
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

    public void TakeDamage(int damage, CeilingBrick hitBrick)
    {
        if (isStageCleared)
            return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, runtimeMaxHp);

        UpdateBrokenBricksByHpRatio();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateBrokenBricksByHpRatio()
    {
        float hpRatio = currentHp / (float)runtimeMaxHp;
        int targetAliveCount = Mathf.CeilToInt(totalBrickCount * hpRatio);

        while (aliveBricks.Count > targetAliveCount)
        {
            BreakRandomBrick();
        }
    }

    private void BreakRandomBrick()
    {
        if (aliveBricks.Count <= 0)
            return;

        int index = UnityEngine.Random.Range(0, aliveBricks.Count);

        CeilingBrick brick = aliveBricks[index];
        aliveBricks.RemoveAt(index);

        brick.Break();
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
