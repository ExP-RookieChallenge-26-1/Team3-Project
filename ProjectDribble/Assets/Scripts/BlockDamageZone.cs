using System.Collections.Generic;
using UnityEngine;

public class BlockDamageZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private BoxCollider2D zoneCollider;

    [Header("Damage")]
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageInterval = 1f;

    [Header("Detect")]
    [SerializeField] private LayerMask blockLayerMask;

    private readonly HashSet<BlockCell> detectedBlocks = new();

    private float damageTimer;

    private void Awake()
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        DetectBlocksInZone();

        if (detectedBlocks.Count <= 0)
        {
            damageTimer = 0f;
            return;
        }

        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;

            playerHealth.TakeDamage(damagePerTick);
            
        }
    }

    private void DetectBlocksInZone()
    {
        detectedBlocks.Clear();

        Vector2 center = zoneCollider.bounds.center;
        Vector2 size = zoneCollider.bounds.size;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            blockLayerMask
        );

        foreach (Collider2D hit in hits)
        {
            BlockCell block = hit.GetComponentInParent<BlockCell>();

            if (block == null)
                continue;

            if (!block.IsAlive)
                continue;

            detectedBlocks.Add(block);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider2D>();

        if (zoneCollider == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
    }
}