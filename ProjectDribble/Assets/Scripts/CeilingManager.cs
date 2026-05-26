using System.Collections.Generic;
using UnityEngine;

public class CeilingManager : MonoBehaviour
{
    [Header("Ceiling HP")]
    [SerializeField] private int maxHp = 100;
    private int currentHp;

    [Header("Brick Spawn")]
    [SerializeField] private CeilingBrick ceilingBrickPrefab;

    [SerializeField] private int columnCount = 7;
    [SerializeField] private int rowCount = 2;

    [SerializeField] private Vector2 brickSize = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 startPosition;

    [Header("Sprites")]
    [SerializeField] private Sprite[] ceilingSprites;

    private List<CeilingBrick> aliveBricks = new();
    private int totalBrickCount;

    private void Start()
    {
        currentHp = maxHp;
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

    public void TakeDamage(int damage, CeilingBrick hitBrick)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        
        UpdateBrokenBricksByHpRatio();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateBrokenBricksByHpRatio()
    {
        float hpRatio = currentHp / (float)maxHp;
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

        int index = Random.Range(0, aliveBricks.Count);

        CeilingBrick brick = aliveBricks[index];
        aliveBricks.RemoveAt(index);

        brick.Break();
    }

    private void Die()
    {
        Debug.Log("천장 파괴 / 스테이지 클리어");
        gameObject.SetActive(false);
    }
}