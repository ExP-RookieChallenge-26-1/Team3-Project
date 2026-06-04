using DefaultNamespace;
using UnityEngine;

public class BallMovement : MonoBehaviour
{
    public float speed = 10f;

    [SerializeField] public float skinWidth = 0.03f;
    [SerializeField] private float _outsideMaxBounceAngle = 50f;
    [SerializeField] private float _insideMaxBounceAngle = 50f;
    [SerializeField] private int maxCollisionIterations = 4;
    [SerializeField] private bool debugCollision;
    [SerializeField] private bool warnWhenOutOfPlayArea = true;
    [SerializeField] private float outOfPlayAreaY = -30f;

    private const float ImmediateRehitDistance = 0.01f;

    public BallData data;
    public float moveDistance = 0;

    private Transform tr;
    private CircleCollider2D cc;
    private Rigidbody2D rb;

    private BallController ballController;
    private BallCollisionHandler ballCollisionHandler;
    private readonly RaycastHit2D[] collisionHits = new RaycastHit2D[16];

    private float baseSpeed;
    private float maxSpeed;
    private float ballDamage;
    private float initialSpeed;
    private float initialBaseSpeed;
    private float initialMaxSpeed;
    private Collider2D lastResolvedCollider;
    private Vector2 lastResolvedDirection;

    private void Start()
    {
        tr = transform;
        cc = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        ballController = GetComponent<BallController>();
        ballCollisionHandler = GetComponent<BallCollisionHandler>();

        ConfigureRigidbody();

        if (data == null && ballController != null)
            data = ballController.data;

        baseSpeed = data != null ? data.baseSpeed : speed;
        maxSpeed = data != null ? data.maxSpeed : speed;
        ballDamage = data != null ? data.ballDamage : 1f;

        initialSpeed = speed;
        initialBaseSpeed = baseSpeed;
        initialMaxSpeed = maxSpeed;
    }

    public Vector2 MoveBall(Vector2 direction, float actualRadius, LayerMask collisionMask)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.down;

        direction.Normalize();
        moveDistance = speed * Time.deltaTime;

        float remainingDistance = moveDistance;
        int iterationCount = Mathf.Max(1, maxCollisionIterations);
        Collider2D lastHitCollider = null;

        if (debugCollision)
        {
            Debug.Log(
                $"[BallMoveBegin] frame={Time.frameCount}, pos={GetPosition()}, dir={direction}, speed={speed:0.000}, moveDistance={moveDistance:0.0000}, skinWidth={skinWidth:0.0000}, radius={actualRadius:0.0000}, iterations={iterationCount}"
            );
        }

        for (int i = 0; i < iterationCount && remainingDistance > 0.0001f; i++)
        {
            Vector2 startPosition = GetPosition();
            RaycastHit2D hit = CastForCollision(
                startPosition,
                actualRadius,
                direction,
                remainingDistance + skinWidth,
                collisionMask
            );

            DebugMove(startPosition, startPosition + direction * remainingDistance, hit);

            if (hit.collider == null)
            {
                if (debugCollision)
                {
                    Debug.Log(
                        $"[BallCollisionNone] frame={Time.frameCount}, iter={i}, start={startPosition}, dir={direction}, move={remainingDistance:0.0000}"
                    );
                }

                MoveBy(direction * remainingDistance);
                remainingDistance = 0f;
                break;
            }

            if (IsImmediateRepeatHit(hit, direction, lastHitCollider))
            {
                if (debugCollision)
                {
                    Debug.Log(
                        $"[BallCollisionRepeatBlocked] frame={Time.frameCount}, iter={i}, collider={hit.collider.name}, distance={hit.distance:0.0000}, dir={direction}, lastResolvedDir={lastResolvedDirection}, reason=sameColliderImmediateRehit"
                    );
                }

                remainingDistance = 0f;
                break;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
            Vector2 positionBeforeSafeMove = GetPosition();
            MoveBy(direction * safeDistance);

            Vector2 stableNormal = GetStableNormal(hit, direction, actualRadius, collisionMask);
            Vector2 positionBeforeSkinMove = GetPosition();
            MoveBy(stableNormal * skinWidth);
            Vector2 positionAfterSkinMove = GetPosition();

            remainingDistance -= safeDistance;

            if (debugCollision)
            {
                Debug.Log(
                    $"[BallCollisionHit] frame={Time.frameCount}, iter={i}, collider={hit.collider.name}, start={startPosition}, hitPoint={hit.point}, hitCentroid={hit.centroid}, hitDistance={hit.distance:0.0000}, hitNormal={hit.normal}, stableNormal={stableNormal}, dotStableAgainstIncoming={Vector2.Dot(stableNormal, -direction):0.0000}, safeDistance={safeDistance:0.0000}, posBeforeSafe={positionBeforeSafeMove}, posBeforeSkin={positionBeforeSkinMove}, posAfterSkin={positionAfterSkinMove}, remainingAfterSafe={remainingDistance:0.0000}"
                );
            }

            Vector2 incomingDirection = direction;
            Vector2 rawReflectDirection = Vector2.Reflect(incomingDirection, stableNormal).normalized;
            BallCollisionResult result = ballCollisionHandler.HandleCollision(hit, direction, stableNormal);
            direction = result.nextDirection;
            lastHitCollider = hit.collider;
            lastResolvedCollider = hit.collider;
            lastResolvedDirection = direction;

            if (debugCollision)
            {
                Debug.Log(
                    $"[BallCollisionResult] frame={Time.frameCount}, iter={i}, collider={hit.collider.name}, inDir={incomingDirection}, hitNormal={hit.normal}, stableNormal={stableNormal}, rawReflect={rawReflectDirection}, outDir={direction}, outDotNormal={Vector2.Dot(direction, stableNormal):0.0000}, yBefore={incomingDirection.y:0.0000}, yAfter={direction.y:0.0000}, moveRemaining={result.shouldMoveRemainingDistance}"
                );

                if (Mathf.Sign(incomingDirection.y) == Mathf.Sign(direction.y) && Mathf.Abs(incomingDirection.y) > 0.0001f)
                {
                    Debug.LogWarning(
                        $"[BallCollisionSuspiciousY] frame={Time.frameCount}, iter={i}, collider={hit.collider.name}, y sign did not change. inDir={incomingDirection}, outDir={direction}, hitNormal={hit.normal}, stableNormal={stableNormal}, rawReflect={rawReflectDirection}"
                    );
                }
            }

            if (!result.shouldMoveRemainingDistance)
                break;

            remainingDistance = Mathf.Max(0f, remainingDistance - skinWidth);
            ResolveOverlap(actualRadius, collisionMask, direction);
        }

        ResolveOverlap(actualRadius, collisionMask, direction);
        WarnIfOutOfPlayArea();

        return direction;
    }

    private RaycastHit2D CastForCollision(
        Vector2 startPosition,
        float actualRadius,
        Vector2 direction,
        float distance,
        LayerMask collisionMask
    )
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(collisionMask);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        int hitCount = Physics2D.CircleCast(
            startPosition,
            actualRadius,
            direction,
            filter,
            collisionHits,
            distance
        );

        RaycastHit2D closestHit = default;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = collisionHits[i];

            if (hit.collider == null || hit.collider == cc || hit.collider.isTrigger)
                continue;

            if (hit.distance < closestDistance)
            {
                closestHit = hit;
                closestDistance = hit.distance;
            }
        }

        return closestHit;
    }

    private bool IsImmediateRepeatHit(
        RaycastHit2D hit,
        Vector2 direction,
        Collider2D currentMoveLastHitCollider
    )
    {
        if (hit.collider == null)
            return false;

        if (hit.distance > ImmediateRehitDistance)
            return false;

        if (hit.collider == currentMoveLastHitCollider)
            return true;

        if (hit.collider != lastResolvedCollider)
            return false;

        return Vector2.Dot(direction.normalized, lastResolvedDirection.normalized) > 0.8f;
    }

    public Vector2 UpdateDirection(RaycastHit2D hit, float outsideMaxAngle, float insideMaxAngle, Vector2 direction)
    {
        return Vector2.Reflect(direction, hit.normal).normalized;
    }

    private void ResolveOverlap(float actualRadius, LayerMask collisionMask, Vector2 fallbackDirection)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(GetPosition(), actualRadius, collisionMask);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D overlap = overlaps[i];

            if (overlap == null || overlap == cc)
                continue;

            if (overlap.isTrigger)
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
            float penetration = actualRadius - distanceToSurface;

            if (penetration > 0f)
            {
                if (debugCollision)
                {
                    Debug.Log(
                        $"[BallOverlapResolve] frame={Time.frameCount}, collider={overlap.name}, pos={position}, closest={closest}, pushDir={pushDirection.normalized}, penetration={penetration:0.0000}, skinWidth={skinWidth:0.0000}, fallbackDir={fallbackDirection}"
                    );
                }

                MoveBy(pushDirection.normalized * (penetration + skinWidth));
            }
        }
    }

    private Vector2 GetStableNormal(
        RaycastHit2D hit,
        Vector2 incomingDirection,
        float actualRadius,
        LayerMask collisionMask
    )
    {
        if (TryGetAxisAlignedWallNormal(hit.collider, out Vector2 wallNormal))
        {
            if (debugCollision)
            {
                Debug.Log(
                    $"[BallStableNormalWallOverride] frame={Time.frameCount}, collider={hit.collider.name}, hitNormal={hit.normal}, incoming={incomingDirection}, wallNormal={wallNormal}"
                );
            }

            return wallNormal;
        }

        Vector2 sampleCenter = GetPosition() + incomingDirection.normalized * Mathf.Max(0f, hit.distance);
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(
            sampleCenter,
            actualRadius + skinWidth * 2f,
            collisionMask
        );

        Vector2 normalSum = hit.normal.sqrMagnitude > 0.0001f
            ? hit.normal.normalized
            : -incomingDirection.normalized;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            Collider2D nearby = nearbyColliders[i];

            if (nearby == null || nearby == cc)
                continue;

            if (nearby.isTrigger)
                continue;

            if (!IsBlockOrWall(nearby))
                continue;

            Vector2 closest = nearby.ClosestPoint(sampleCenter);
            Vector2 away = sampleCenter - closest;

            if (away.sqrMagnitude < 0.0001f)
                away = sampleCenter - (Vector2)nearby.bounds.center;

            if (away.sqrMagnitude > 0.0001f)
            {
                normalSum += away.normalized;

                if (debugCollision)
                {
                    Debug.Log(
                        $"[BallStableNormalSample] frame={Time.frameCount}, source={nearby.name}, sampleCenter={sampleCenter}, closest={closest}, away={away.normalized}, normalSum={normalSum}"
                    );
                }
            }
        }

        if (normalSum.sqrMagnitude < 0.0001f)
            return -incomingDirection.normalized;

        Vector2 stableNormal = normalSum.normalized;
        Vector2 stableNormalBeforeFlip = stableNormal;

        if (Vector2.Dot(stableNormal, -incomingDirection.normalized) < 0f)
            stableNormal = -stableNormal;

        if (debugCollision)
        {
            Debug.Log(
                $"[BallStableNormalResult] frame={Time.frameCount}, collider={hit.collider.name}, hitNormal={hit.normal}, incoming={incomingDirection}, sampleCenter={sampleCenter}, beforeFlip={stableNormalBeforeFlip}, final={stableNormal}, dotFinalAgainstIncoming={Vector2.Dot(stableNormal, -incomingDirection.normalized):0.0000}"
            );
        }

        return stableNormal;
    }

    private bool TryGetAxisAlignedWallNormal(Collider2D collider, out Vector2 normal)
    {
        normal = Vector2.zero;

        if (collider == null)
            return false;

        string objectName = collider.name.ToLowerInvariant();
        string parentName = collider.transform.parent != null
            ? collider.transform.parent.name.ToLowerInvariant()
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

    private bool IsBlockOrWall(Collider2D collider)
    {
        if (collider.GetComponentInParent<BlockCell>() != null)
            return true;

        if (collider.GetComponentInParent<WallBallHitReceiver>() != null)
            return true;

        return collider.name.ToLowerInvariant().Contains("wall");
    }

    private Vector2 GetPosition()
    {
        return rb != null ? rb.position : (Vector2)tr.position;
    }

    private void MoveBy(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0f)
            return;

        Vector2 nextPosition = GetPosition() + delta;

        if (rb != null)
            rb.position = nextPosition;
        else
            tr.position = nextPosition;
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

    private void DebugMove(Vector2 startPosition, Vector2 endPosition, RaycastHit2D hit)
    {
        if (!debugCollision)
            return;

        Debug.DrawLine(startPosition, endPosition, hit.collider != null ? Color.red : Color.cyan);
        Debug.DrawRay(startPosition, (endPosition - startPosition).normalized, Color.yellow);

        if (hit.collider != null)
            Debug.DrawRay(hit.point, hit.normal, Color.green);
    }

    private void WarnIfOutOfPlayArea()
    {
        if (!warnWhenOutOfPlayArea)
            return;

        Vector2 position = GetPosition();

        if (position.y < outOfPlayAreaY)
            Debug.LogWarning($"[BallOutOfPlayArea] Position: {position}, Speed: {speed}, MoveDistance: {moveDistance}");
    }

    public void SetBallSpeed(float amount = -1)
    {
        if (amount < 0)
            speed = baseSpeed;
        else
            speed = amount;
    }

    public void AddBallSpeed(float amount)
    {
        speed += amount;
    }

    public void SetBallBaseSpeed(float amount)
    {
        baseSpeed = amount;
    }

    public void AddBallBaseSpeed(float amount)
    {
        baseSpeed += amount;
    }

    public void SetBallMaxSpeed(float amount)
    {
        maxSpeed = amount;
    }

    public void AddBallMaxSpeed(float amount)
    {
        maxSpeed += amount;
    }

    public void ResetMovementState()
    {
        speed = initialSpeed;
        baseSpeed = initialBaseSpeed;
        maxSpeed = initialMaxSpeed;
    }
}
