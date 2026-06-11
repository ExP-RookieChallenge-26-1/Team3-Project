using DefaultNamespace;
using Interfaces;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
    [Header("Ball Data")]
    [SerializeField] private BallData ballData;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float blockDamageMultiplier = 1f;
    [SerializeField] private float ceilingDamageMultiplier = 1f;

    [Header("Movement")]
    [SerializeField] private Vector2 initialDirection = Vector2.down;
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 8f;
    [SerializeField] private float radius = 0.35f;
    [SerializeField] private float skinWidth = 0.03f;
    [SerializeField] private int maxCollisionIterations = 4;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private LayerMask damageMask;
    [SerializeField] private string dangerAreaTag = "PlayerDanger";

    [Header("Damage")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int damage = 1;
    [SerializeField] private float blockDamage = 1f;
    [SerializeField] private float ceilingDamage = 1f;

    [Header("Paddle Reflect")]
    [SerializeField] private float fallbackPaddleWidth = 4f;
    [SerializeField] private float fallbackMaxBounceAngle = 50f;

    private const float ImmediateRehitDistance = 0.01f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private Vector2 direction;
    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private float spawnTime;
    private float effectiveSpeed;
    private Collider2D lastResolvedCollider;
    private Vector2 lastResolvedDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();

        ConfigureRigidbody();

        direction = initialDirection.sqrMagnitude > 0.0001f
            ? initialDirection.normalized
            : Vector2.down;

        effectiveSpeed = GetBallDataSpeed();
    }

    private void Start()
    {
        spawnTime = Time.time;

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        MoveProjectile();
    }

    public void Launch(Vector2 launchDirection, float launchSpeed)
    {
        if (launchDirection.sqrMagnitude > 0.0001f)
            direction = launchDirection.normalized;

        if (launchSpeed > 0f)
            effectiveSpeed = launchSpeed;
        else
            effectiveSpeed = GetBallDataSpeed();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDamageCollider(other))
            ApplyDamageAndDestroy();
    }

    private void MoveProjectile()
    {
        float remainingDistance = effectiveSpeed * Time.deltaTime;
        int iterationCount = Mathf.Max(1, maxCollisionIterations);
        Collider2D lastHitCollider = null;

        for (int i = 0; i < iterationCount && remainingDistance > 0.0001f; i++)
        {
            RaycastHit2D hit = CastForCollision(direction, remainingDistance + skinWidth);

            if (hit.collider == null)
            {
                MoveBy(direction * remainingDistance);
                break;
            }

            if (IsImmediateRepeatHit(hit, direction, lastHitCollider))
            {
                remainingDistance = 0f;
                break;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
            MoveBy(direction * safeDistance);

            Vector2 normal = GetStableNormal(hit, direction);
            Vector2 incomingDirection = direction;

            MoveBy(normal * skinWidth);
            remainingDistance -= safeDistance;

            PaddleBallReflector paddleReflector = hit.collider.GetComponentInParent<PaddleBallReflector>();
            BlockCell hitBlock = hit.collider.GetComponentInParent<BlockCell>();
            IDamageable ceilingDamageTarget = GetCeilingDamageTarget(hit.collider);

            if (hitBlock != null)
            {
                bool destroyed = DamageBlock(hitBlock);
                bool blockWasBroken = !hitBlock.IsAlive;

                if (blockWasBroken || destroyed)
                {
                    direction = incomingDirection.normalized;
                }
                else
                {
                    direction = Vector2.Reflect(incomingDirection, normal).normalized;
                }
            }
            else if (ceilingDamageTarget != null)
            {
                bool destroyed = DamageCeiling(ceilingDamageTarget);

                direction = destroyed
                    ? incomingDirection.normalized
                    : Vector2.Reflect(incomingDirection, normal).normalized;
            }
            else if (paddleReflector != null)
            {
                direction = GetPaddleReflectDirection(paddleReflector);
            }
            else if (IsDamageCollider(hit.collider))
            {
                ApplyDamageAndDestroy();
                return;
            }
            else
            {
                direction = Vector2.Reflect(incomingDirection, normal).normalized;
            }

            lastHitCollider = hit.collider;
            lastResolvedCollider = hit.collider;
            lastResolvedDirection = direction;
            remainingDistance = Mathf.Max(0f, remainingDistance - skinWidth);
            ResolveOverlap(direction);
        }

        ResolveOverlap(direction);
    }

    private RaycastHit2D CastForCollision(Vector2 moveDirection, float distance)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(collisionMask | damageMask);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int hitCount = Physics2D.CircleCast(
            GetPosition(),
            radius,
            moveDirection,
            filter,
            hits,
            distance
        );

        RaycastHit2D closestHit = default;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];

            if (hit.collider == null || hit.collider == ownCollider)
                continue;

            if (hit.collider.isTrigger && !IsDamageCollider(hit.collider))
                continue;

            if (hit.distance < closestDistance)
            {
                closestHit = hit;
                closestDistance = hit.distance;
            }
        }

        return closestHit;
    }

    private bool DamageBlock(BlockCell block)
    {
        if (block == null)
            return false;

        return block.TakeDamage(GetBlockDamage(), false);
    }

    private bool DamageCeiling(IDamageable damageTarget)
    {
        if (damageTarget == null)
            return false;

        return damageTarget.TakeDamage(GetCeilingDamage());
    }

    private float GetBallDataSpeed()
    {
        if (ballData != null)
            return Mathf.Max(0f, ballData.baseSpeed * speedMultiplier);

        return Mathf.Max(0f, speed);
    }

    private float GetBlockDamage()
    {
        if (ballData != null)
            return Mathf.Max(0f, ballData.BaseDamage * blockDamageMultiplier);

        return Mathf.Max(0f, blockDamage);
    }

    private float GetCeilingDamage()
    {
        if (ballData != null)
            return Mathf.Max(0f, ballData.BaseDamage * ceilingDamageMultiplier);

        return Mathf.Max(0f, ceilingDamage);
    }

    private IDamageable GetCeilingDamageTarget(Collider2D hitCollider)
    {
        if (hitCollider == null)
            return null;

        if (hitCollider.GetComponentInParent<BlockCell>() != null)
            return null;

        return hitCollider.GetComponentInParent<IDamageable>();
    }

    private bool IsImmediateRepeatHit(
        RaycastHit2D hit,
        Vector2 moveDirection,
        Collider2D currentMoveLastHitCollider
    )
    {
        if (hit.collider == null)
            return false;

        if (hit.distance > ImmediateRehitDistance)
            return false;

        if (currentMoveLastHitCollider != null)
            return true;

        if (hit.collider != lastResolvedCollider)
            return false;

        return Vector2.Dot(moveDirection.normalized, lastResolvedDirection.normalized) > 0.8f;
    }

    private Vector2 GetStableNormal(RaycastHit2D hit, Vector2 incomingDirection)
    {
        if (TryGetAxisAlignedWallNormal(hit.collider, out Vector2 wallNormal))
            return wallNormal;

        if (TryGetDirectDamageTargetHitNormal(hit, incomingDirection, out Vector2 directNormal))
            return directNormal;

        Vector2 sampleCenter = GetPosition() + incomingDirection.normalized * Mathf.Max(0f, hit.distance);
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(
            sampleCenter,
            radius + skinWidth * 2f,
            collisionMask
        );

        Vector2 normalSum = hit.normal.sqrMagnitude > 0.0001f
            ? hit.normal.normalized
            : -incomingDirection.normalized;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            Collider2D nearby = nearbyColliders[i];

            if (nearby == null || nearby == ownCollider || nearby.isTrigger)
                continue;

            if (!IsBlockOrWall(nearby))
                continue;

            Vector2 closest = nearby.ClosestPoint(sampleCenter);
            Vector2 away = sampleCenter - closest;

            if (away.sqrMagnitude < 0.0001f)
                away = sampleCenter - (Vector2)nearby.bounds.center;

            if (away.sqrMagnitude > 0.0001f)
                normalSum += away.normalized;
        }

        if (normalSum.sqrMagnitude < 0.0001f)
            return -incomingDirection.normalized;

        Vector2 stableNormal = normalSum.normalized;

        if (Vector2.Dot(stableNormal, -incomingDirection.normalized) < 0f)
            stableNormal = -stableNormal;

        return stableNormal;
    }

    private bool TryGetDirectDamageTargetHitNormal(
        RaycastHit2D hit,
        Vector2 incomingDirection,
        out Vector2 normal
    )
    {
        normal = Vector2.zero;

        if (hit.collider == null)
            return false;

        bool isDirectDamageTarget =
            hit.collider.GetComponentInParent<BlockCell>() != null ||
            hit.collider.GetComponentInParent<CeilingBrick>() != null;

        if (!isDirectDamageTarget)
            return false;

        if (hit.normal.sqrMagnitude < 0.0001f)
            return false;

        normal = hit.normal.normalized;

        if (Vector2.Dot(normal, -incomingDirection.normalized) < 0f)
            normal = -normal;

        return true;
    }

    private bool TryGetAxisAlignedWallNormal(Collider2D hitCollider, out Vector2 normal)
    {
        normal = Vector2.zero;

        if (hitCollider == null)
            return false;

        string objectName = hitCollider.name.ToLowerInvariant();
        string parentName = hitCollider.transform.parent != null
            ? hitCollider.transform.parent.name.ToLowerInvariant()
            : string.Empty;

        if (objectName.Contains("wall_left") || parentName.Contains("wall_left"))
        {
            normal = Vector2.right;
            return true;
        }

        if (objectName.Contains("wall_right") || parentName.Contains("wall_right"))
        {
            normal = Vector2.left;
            return true;
        }

        return false;
    }

    private bool IsBlockOrWall(Collider2D hitCollider)
    {
        if (hitCollider.GetComponentInParent<BlockCell>() != null)
            return true;

        if (hitCollider.GetComponentInParent<CeilingBrick>() != null)
            return true;

        if (hitCollider.GetComponentInParent<WallBallHitReceiver>() != null)
            return true;

        return hitCollider.name.ToLowerInvariant().Contains("wall");
    }

    private void ResolveOverlap(Vector2 fallbackDirection)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(GetPosition(), radius, collisionMask);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];

            if (overlap == null || overlap == ownCollider || overlap.isTrigger)
                continue;

            if (overlap.CompareTag("need_correction"))
                continue;

            Vector2 position = GetPosition();
            Vector2 closest = overlap.ClosestPoint(position);
            Vector2 pushDirection = position - closest;

            if (pushDirection.sqrMagnitude < 0.0001f)
                pushDirection = position - (Vector2)overlap.bounds.center;

            if (pushDirection.sqrMagnitude < 0.0001f)
                pushDirection = -fallbackDirection;

            float distanceToSurface = Vector2.Distance(position, closest);
            float penetration = radius - distanceToSurface;

            if (penetration > 0f)
                MoveBy(pushDirection.normalized * (penetration + skinWidth));
        }
    }

    private Vector2 GetPaddleReflectDirection(PaddleBallReflector paddleReflector)
    {
        float paddleWidth = GetPaddleWidth(paddleReflector);
        float xOffset = (transform.position.x - paddleReflector.transform.position.x) / (paddleWidth * 0.5f);
        xOffset = Mathf.Clamp(xOffset, -1f, 1f);

        Vector2 baseDirection = paddleReflector.ReflectUp ? Vector2.up : Vector2.down;
        float angleRatio = Mathf.Abs(xOffset) * Mathf.Abs(xOffset);
        float targetAngle = Mathf.Lerp(0f, fallbackMaxBounceAngle, angleRatio);
        Quaternion rotation = Quaternion.Euler(0f, 0f, -xOffset * targetAngle);

        return ((Vector2)(rotation * baseDirection)).normalized;
    }

    private float GetPaddleWidth(PaddleBallReflector paddleReflector)
    {
        Collider2D paddleCollider = paddleReflector.GetComponentInChildren<Collider2D>();

        if (paddleCollider != null && paddleCollider.bounds.size.x > 0.001f)
            return paddleCollider.bounds.size.x;

        return Mathf.Max(0.001f, fallbackPaddleWidth);
    }

    private bool IsDamageCollider(Collider2D hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (hitCollider.GetComponentInParent<BlockCell>() != null)
            return false;

        if (GetCeilingDamageTarget(hitCollider) != null)
            return false;

        if (IsInLayerMask(hitCollider.gameObject.layer, damageMask))
            return true;

        if (!string.IsNullOrEmpty(dangerAreaTag) && hitCollider.gameObject.tag == dangerAreaTag)
            return true;

        string objectName = hitCollider.name.ToLowerInvariant();
        string parentName = hitCollider.transform.parent != null
            ? hitCollider.transform.parent.name.ToLowerInvariant()
            : string.Empty;

        return IsFloorName(objectName) || IsFloorName(parentName);
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private bool IsFloorName(string objectName)
    {
        return objectName.Contains("ground") ||
               objectName.Contains("floor") ||
               objectName.Contains("bottom");
    }

    private void ApplyDamageAndDestroy()
    {
        if (playerHealth != null && damage > 0)
            playerHealth.TakeDamage(damage);

        Destroy(gameObject);
    }

    private Vector2 GetPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    private void MoveBy(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f)
            return;

        Vector2 nextPosition = GetPosition() + delta;

        if (rb != null)
            rb.position = nextPosition;
        else
            transform.position = nextPosition;
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
            return;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
}
