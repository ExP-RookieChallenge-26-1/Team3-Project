using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private BossProjectile bossProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = -1f;

    [Header("Target")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private bool autoFindTarget = true;

    [Header("Pattern")]
    [SerializeField] private bool startPatternOnStart = true;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private bool fireImmediately = true;

    private Coroutine fireRoutine;
    private bool isBossPatternActive;

    public bool IsBossPatternActive => isBossPatternActive;

    private void Start()
    {
        if (targetTransform == null && autoFindTarget)
            targetTransform = FindDefaultTarget();

        if (startPatternOnStart)
            StartBossPattern();
    }

    public void StartBossPattern()
    {
        if (isBossPatternActive)
            return;

        isBossPatternActive = true;
        fireRoutine = StartCoroutine(FireRoutine());
    }

    public void StopBossPattern()
    {
        isBossPatternActive = false;

        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }
    }

    public void FireProjectile()
    {
        if (bossProjectilePrefab == null)
            return;

        if (targetTransform == null && autoFindTarget)
            targetTransform = FindDefaultTarget();

        Transform spawnPoint = projectileSpawnPoint != null
            ? projectileSpawnPoint
            : transform;

        BossProjectile projectile = Instantiate(
            bossProjectilePrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Vector2 direction = GetFireDirection(spawnPoint.position);
        projectile.Launch(direction, projectileSpeed);
    }

    private IEnumerator FireRoutine()
    {
        if (fireImmediately)
            FireProjectile();

        while (isBossPatternActive)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, fireInterval));

            if (isBossPatternActive)
                FireProjectile();
        }

        fireRoutine = null;
    }

    private Vector2 GetFireDirection(Vector3 spawnPosition)
    {
        if (targetTransform == null)
            return Vector2.down;

        Vector2 direction = targetTransform.position - spawnPosition;

        return direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.down;
    }

    private Transform FindDefaultTarget()
    {
        PaddleController paddle = FindAnyObjectByType<PaddleController>();

        if (paddle != null)
            return paddle.transform;

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();

        return playerHealth != null ? playerHealth.transform : null;
    }
}
