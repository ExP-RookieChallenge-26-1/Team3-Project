using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;

        [Header("Spawn")]
        public Vector2Int startCell = new Vector2Int(0, 0);

        [Header("Move Commands")]
        public List<Enemy.MoveCommand> moveCommands = new();

        [Header("Move Setting")]
        public float moveInterval = 1.0f;
        public float moveSpeed = 3.0f;
        public bool loopAllCommands = false;
    }

    [Header("Enemy")]
    [SerializeField] private BrickManager brickManager;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private List<EnemySpawnData> enemies = new();

    [Header("Enemy Spawn Option")]
    [SerializeField] private bool spawnEnemiesOnStart = true;

    private readonly List<Enemy> spawnedEnemies = new();

    private bool isRestarting = false;

    private IEnumerator Start()
    {
        if (brickManager == null)
            brickManager = FindAnyObjectByType<BrickManager>();

        while (brickManager == null || !brickManager.IsInitialized)
            yield return null;

        if (spawnEnemiesOnStart)
        {
            SpawnAllEnemies();
        }
    }

    public void SpawnAllEnemies()
    {
        if (brickManager == null)
            brickManager = FindAnyObjectByType<BrickManager>();

        ClearEnemies();

        foreach (EnemySpawnData data in enemies)
        {
            SpawnEnemy(data);
        }
    }

    private void SpawnEnemy(EnemySpawnData data)
    {
        if (data.enemyPrefab == null)
            return;

        if (brickManager == null)
            return;

        Vector3 spawnPos = brickManager.GetCellWorldPosition(data.startCell);

        GameObject enemyObj = Instantiate(
            data.enemyPrefab,
            spawnPos,
            Quaternion.identity,
            enemyParent
        );

        Enemy mover = enemyObj.GetComponent<Enemy>();

        if (mover == null)
            mover = enemyObj.AddComponent<Enemy>();

        mover.Init(
           brickManager,
            data.startCell,
            data.moveCommands,
            data.moveInterval,
            data.moveSpeed,
            data.loopAllCommands
        );

        spawnedEnemies.Add(mover);
    }

    public void ClearEnemies()
    {
        foreach (Enemy enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        spawnedEnemies.Clear();

        if (enemyParent != null)
        {
            for (int i = enemyParent.childCount - 1; i >= 0; i--)
            {
                Destroy(enemyParent.GetChild(i).gameObject);
            }
        }
    }

    public void RespawnEnemiesIfNoBrick()
    {
        foreach (EnemySpawnData enemyData in enemies)
        {
            Vector2Int cell = enemyData.startCell;

            // 해당 위치에 벽돌이 없으면
            if (!brickManager.IsCellOccupied(cell))
            {
                SpawnEnemy(enemyData);
            }
        }
    }

    public void RestartGame()
    {
        if (isRestarting)
            return;

        isRestarting = true;
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        // 현재 프레임 UI/이벤트 끝까지 기다림
        yield return null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}