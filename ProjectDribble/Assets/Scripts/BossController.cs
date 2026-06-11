using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private BossProjectile bossProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = -1f;

    [Header("Block Absorb")]
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private Transform gatherPoint;
    [SerializeField] private int absorbedBlockCountPerShot = 3;
    [SerializeField] private float absorbSearchRadiusWorld = 6f;
    [SerializeField] private float gatherDuration = 0.35f;
    [SerializeField] private float gatherStagger = 0.05f;

    [Header("Target")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private bool autoFindTarget = true;

    [Header("Pattern")]
    [SerializeField] private bool startPatternOnStart = true;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private bool fireImmediately = true;

    private Coroutine fireRoutine;
    private bool isBossPatternActive;
    private bool isGathering;

    public bool IsBossPatternActive => isBossPatternActive;

    private void Start()
    {
        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();

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

        if (isGathering)
            return;

        if (targetTransform == null && autoFindTarget)
            targetTransform = FindDefaultTarget();

        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();

        Vector3 gatherPosition = GetGatherPosition();
        List<Vector2Int> absorbableCoords = blockManager != null
            ? blockManager.GetAbsorbableFlowBlockCoords(
                gatherPosition,
                absorbSearchRadiusWorld,
                Mathf.Max(0, absorbedBlockCountPerShot)
            )
            : null;

        if (absorbableCoords == null || absorbableCoords.Count == 0)
        {
            LaunchProjectileAt(GetProjectileSpawnPosition());
            return;
        }

        StartCoroutine(AbsorbBlocksAndFire(absorbableCoords));
    }

    private IEnumerator AbsorbBlocksAndFire(List<Vector2Int> coords)
    {
        isGathering = true;

        List<BlockVisualClone> clones = new();

        for (int i = 0; i < coords.Count; i++)
        {
            BlockVisualSnapshot snapshot = CreateBlockVisualSnapshot(coords[i]);

            if (!blockManager.TryRemoveAbsorbableFlowBlock(coords[i]))
                continue;

            GameObject clone = CreateVisualClone(snapshot);

            if (clone == null)
                continue;

            clones.Add(new BlockVisualClone(clone, snapshot.position));
        }

        if (clones.Count > 0)
            yield return MoveClonesToGatherPoint(clones);

        for (int i = 0; i < clones.Count; i++)
        {
            if (clones[i].gameObject != null)
                Destroy(clones[i].gameObject);
        }

        LaunchProjectileAt(GetGatherPosition());
        isGathering = false;
    }

    private IEnumerator MoveClonesToGatherPoint(List<BlockVisualClone> clones)
    {
        float duration = Mathf.Max(0.01f, gatherDuration);
        float stagger = Mathf.Max(0f, gatherStagger);
        float totalDuration = duration + stagger * Mathf.Max(0, clones.Count - 1);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            Vector3 targetPosition = GetGatherPosition();

            for (int i = 0; i < clones.Count; i++)
            {
                GameObject clone = clones[i].gameObject;

                if (clone == null)
                    continue;

                float localTime = Mathf.Clamp01((elapsed - i * stagger) / duration);
                float easedTime = localTime * localTime * (3f - 2f * localTime);
                clone.transform.position = Vector3.Lerp(
                    clones[i].startPosition,
                    targetPosition,
                    easedTime
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 finalPosition = GetGatherPosition();

        for (int i = 0; i < clones.Count; i++)
        {
            if (clones[i].gameObject != null)
                clones[i].gameObject.transform.position = finalPosition;
        }
    }

    private void LaunchProjectileAt(Vector3 spawnPosition)
    {
        Quaternion spawnRotation = projectileSpawnPoint != null
            ? projectileSpawnPoint.rotation
            : transform.rotation;

        BossProjectile projectile = Instantiate(
            bossProjectilePrefab,
            spawnPosition,
            spawnRotation
        );

        Vector2 direction = GetFireDirection(spawnPosition);
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

    private Vector3 GetProjectileSpawnPosition()
    {
        return projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
    }

    private Vector3 GetGatherPosition()
    {
        if (gatherPoint != null)
            return gatherPoint.position;

        return projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
    }

    private BlockVisualSnapshot CreateBlockVisualSnapshot(Vector2Int coord)
    {
        Vector3 fallbackPosition = blockManager != null
            ? blockManager.GridToWorld(coord)
            : transform.position;

        BlockVisualSnapshot snapshot = new BlockVisualSnapshot
        {
            position = fallbackPosition,
            rotation = Quaternion.identity,
            scale = Vector3.one,
            color = Color.white,
            sortingLayerName = "Default",
            sortingOrder = 0
        };

        BlockCell block = blockManager != null
            ? blockManager.GetBlockCell(coord)
            : null;

        if (block == null)
            return snapshot;

        snapshot.position = block.transform.position;
        snapshot.rotation = block.transform.rotation;
        snapshot.scale = block.transform.lossyScale;

        SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();

        if (renderer == null)
            renderer = block.GetComponentInChildren<SpriteRenderer>();

        if (renderer == null)
            return snapshot;

        snapshot.position = renderer.transform.position;
        snapshot.rotation = renderer.transform.rotation;
        snapshot.scale = renderer.transform.lossyScale;
        snapshot.sprite = renderer.sprite;
        snapshot.color = renderer.color;
        snapshot.sharedMaterial = renderer.sharedMaterial;
        snapshot.sortingLayerName = renderer.sortingLayerName;
        snapshot.sortingOrder = renderer.sortingOrder;

        return snapshot;
    }

    private GameObject CreateVisualClone(BlockVisualSnapshot snapshot)
    {
        GameObject clone = new GameObject("BossAbsorbedBlockVisual");
        clone.transform.position = snapshot.position;
        clone.transform.rotation = snapshot.rotation;
        clone.transform.localScale = snapshot.scale;

        SpriteRenderer renderer = clone.AddComponent<SpriteRenderer>();
        renderer.sprite = snapshot.sprite;
        renderer.color = snapshot.color;
        renderer.sortingLayerName = snapshot.sortingLayerName;
        renderer.sortingOrder = snapshot.sortingOrder;

        if (snapshot.sharedMaterial != null)
            renderer.sharedMaterial = snapshot.sharedMaterial;

        return clone;
    }

    private struct BlockVisualSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Sprite sprite;
        public Color color;
        public Material sharedMaterial;
        public string sortingLayerName;
        public int sortingOrder;
    }

    private struct BlockVisualClone
    {
        public GameObject gameObject;
        public Vector3 startPosition;

        public BlockVisualClone(GameObject gameObject, Vector3 startPosition)
        {
            this.gameObject = gameObject;
            this.startPosition = startPosition;
        }
    }
}
