using UnityEngine;


public class BallMovement : MonoBehaviour
{
    public float speed = 10f; // 공의 초기 속도

    [SerializeField] public float skinWidth = 0.1f; // 벽과 거리 유지 정도
    [SerializeField] private float _outsideMaxBounceAngle = 50f;
    [SerializeField] private float _insideMaxBounceAngle = 50f;

    
    public BallData data;
    public float moveDistance = 0;
    private Transform tr;
    private CircleCollider2D cc;

    private BallController BallController;
    private BallCollisionHandler BallCollisionHandler;
    private BallSpeedController BallSpeedController;

    float baseSpeed;
    float maxSpeed;
    float ballDamage;
    float initialSpeed;
    float initialBaseSpeed;
    float initialMaxSpeed;

    void Start()
    {
        tr = GetComponent<Transform>();
        cc = GetComponent<CircleCollider2D>();
        BallCollisionHandler = GetComponent<BallCollisionHandler>();
        BallSpeedController = GetComponent<BallSpeedController>();

        baseSpeed = data.baseSpeed;
        maxSpeed = data.maxSpeed;
        ballDamage = data.ballDamage;

        initialSpeed = speed;
        initialBaseSpeed = baseSpeed;
        initialMaxSpeed = maxSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        
        //MoveBall(BallController.direction, BallController.actualRadius, BallController.collisionMask);
    }
    public Vector2 MoveBall(Vector2 direction, float actualRadius, LayerMask collisionMask)
    {
        moveDistance = speed * Time.deltaTime;
        // 1. CircleCast로 이동 경로에 장애물이 있는지 확인
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            actualRadius,
            direction,
            moveDistance,
            collisionMask
        );

        if (hit.collider != null)
        {   
            // 2. 충돌 지점까지 우선 이동 (충돌 지점에서 아주 살짝 띄움)
            float distanceToHit = hit.distance;
            transform.Translate(direction * distanceToHit, Space.World);
            
            // 2.5. 충돌면 바깥쪽으로 확실히 밀어냄
            transform.position += (Vector3)(hit.normal * skinWidth);
            
            // 3. 충돌 대상에 따른 반사 방향 계산
            float remainingDistance = moveDistance - distanceToHit;

            BallCollisionResult result =
                BallCollisionHandler.HandleCollision(hit, direction);

            direction = result.nextDirection;
            
            // 4. 남은 거리가 있다면 새로운 방향으로 다시 이동 (재귀 호출 방지를 위해 단순화)
            if (result.shouldMoveRemainingDistance && remainingDistance > 0)
            {   
                transform.Translate(direction * remainingDistance, Space.World);
            }

            return direction;
        }
        // 충돌이 없다면 지정된 거리만큼 직선 이동
        transform.Translate(direction * moveDistance, Space.World);
        ResolveOverlap(actualRadius, collisionMask);
        return direction;
    }
   /*
    public Vector2 UpdateDirection(RaycastHit2D hit, float outsideMaxAngle, float insideMaxAngle, Vector2 direction)
    {
        Debug.Log("충돌함: " + hit.collider.name);
        GameObject obj = hit.collider.gameObject;

        //패들 외 물체와 충돌 시 작용 인터페이스(i ball hit receiver)로 위임 
        var hitObj = hit.collider.GetComponentInParent<IBallHitReceiver>();

        if (hitObj != null)
        {
            hitObj.OnBallHit();
        }

        // 패들 충돌 로직
        if (obj.name.Contains("paddle_up") || obj.name.Contains("paddle_down") || obj.name.Contains("roof_paddle"))
        {
            //razerManager.CheckBounceCount();
            // 1. 비율 계산 ( -1 ~ 1 사이가 될 것임)
            float xOffset = (transform.position.x - obj.transform.position.x) / (3f / 2f);
            xOffset = Mathf.Clamp(xOffset, -1f, 1f);
        
            // 2. 튕겨나갈 기본 방향 결정 (위패들은 아래로, 아래패들은 위로)
            Vector2 baseDir = obj.name.Contains("paddle_down") || obj.name.Contains("roof_paddle") ? Vector2.up : Vector2.down;

            // 3. 각도 보정 (Lerp)
            float targetAngle;
            if (obj.name.Contains("paddle_up") || obj.name.Contains("paddle_down"))
            {
                targetAngle = Mathf.Lerp(0, insideMaxAngle, Mathf.Abs(xOffset));
            }
            else
            {
                targetAngle = Mathf.Lerp(0, outsideMaxAngle, Mathf.Abs(xOffset));
            }
            
            Quaternion rotation = Quaternion.Euler(0, 0, -xOffset * targetAngle);

            return (rotation * baseDir).normalized;
        }
        else
        {
            // 벽이나 기타 오브젝트: 일반적인 물리 반사 법칙 적용
            return Vector2.Reflect(direction, hit.normal).normalized;
           // razerManager.Reset();
        }
    }
*/
    void ResolveOverlap(float actualRadius, LayerMask collisionMask)
    {
        Collider2D overlap = Physics2D.OverlapCircle(tr.position, actualRadius, collisionMask);

        if (overlap == null)
            return;

        if (overlap.CompareTag("need_correction")) // 해당 태그 보유한 모든 오브젝트를 예외 처리함
            return;

        Vector2 closest = overlap.ClosestPoint(tr.position);
        Vector2 pushDir = (tr.position - (Vector3)closest);

        if (pushDir.sqrMagnitude < 0.0001f)
        {
            // 완전히 겹쳤을 때 (중심이 동일)
            pushDir = Random.insideUnitCircle.normalized;
        }

        tr.position += (Vector3)(pushDir.normalized * (skinWidth));
        
    }


    // 공 현재 속도
    public void SetBallSpeed(float amount)
    {
        speed = amount;
    }
    public void AddBallSpeed(float amount)
    {
        speed += amount;
    }

// 공 기본 속도
    public void SetBallBaseSpeed(float amount)
    {
        baseSpeed = amount;
    }
    public void AddBallBaseSpeed(float amount)
    {
        baseSpeed += amount;
    }

// 공 최대 속도
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
