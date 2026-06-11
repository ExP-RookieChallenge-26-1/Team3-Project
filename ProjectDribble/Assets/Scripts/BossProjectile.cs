using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
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

    [Header("Paddle Reflect")]
    [SerializeField] private float fallbackPaddleWidth = 4f;
    [SerializeField] private float fallbackMaxBounceAngle = 50f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    private Vector2 direction;
    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();

        ConfigureRigidbody();

        direction = initialDirection.sqrMagnitude > 0.0001f
            ? initialDirection.normalized
            : Vector2.down;
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
            speed = launchSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDamageCollider(other))
            ApplyDamageAndDestroy();
    }

    private void MoveProjectile()
    {
        float remainingDistance = speed * Time.deltaTime;
        int iterationCount = Mathf.Max(1, maxCollisionIterations);

        for (int i = 0; i < iterationCount && remainingDistance > 0.0001f; i++)
        {
            RaycastHit2D hit = CastForCollision(direction, remainingDistance + skinWidth);

            if (hit.collider == null)
            {
                MoveBy(direction * remainingDistance);
                break;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
            MoveBy(direction * safeDistance);

            if (IsDamageCollider(hit.collider))
            {
                ApplyDamageAndDestroy();
                return;
            }

            Vector2 normal = hit.normal.sqrMagnitude > 0.0001f
                ? hit.normal.normalized
                : -direction;

            MoveBy(normal * skinWidth);
            remainingDistance = Mathf.Max(0f, remainingDistance - safeDistance - skinWidth);

            PaddleBallReflector paddleReflector = hit.collider.GetComponentInParent<PaddleBallReflector>();
            direction = paddleReflector != null
                ? GetPaddleReflectDirection(paddleReflector)
                : Vector2.Reflect(direction, normal).normalized;
        }
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

            if (hit.distance < closestDistance)
            {
                closestHit = hit;
                closestDistance = hit.distance;
            }
        }

        return closestHit;
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
