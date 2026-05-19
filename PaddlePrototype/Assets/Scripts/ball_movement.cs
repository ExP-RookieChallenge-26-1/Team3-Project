using DefaultNamespace;
using ScriptableObjectScripts;
using UnityEngine;

public class BallController : MonoBehaviour
{
    
    [SerializeField] private BallBalanceData balanceData;
    [SerializeField] private BallSound ballSound;
    
    [SerializeField]private float speed = 10f; // 공의 초기 속도
    [SerializeField] private float baseSpeed = 10f; // 공의 초기 속도
    [SerializeField] private float maxSpeed = 30f; // 최대 속도

    private float power = 10f;
    [SerializeField] private float basePower = 10f;

    [SerializeField] private float paddleSpeedIncrease = 5f;
    [SerializeField] private float paddlePowerIncrease = 10f;

    [SerializeField] private float blockSpeedDecrease = 5f;
    [SerializeField] private float blockPowerDecrease = 10f;

    [SerializeField] private float centerPullStrength = 1.0f; // 중앙으로 끌리는 정도
    [SerializeField] private float centerZone = 0.2f; // 중앙에서 수직으로 튕기는 범위

    [SerializeField] private float ballRadius = 0.7f;

    [SerializeField] private float skinWidth = 0.1f; // 벽과 거리 유지 정도

    [SerializeField] private float _outsideMaxBounceAngle = 50f;
    [SerializeField] private float _insideMaxBounceAngle = 50f;

    private float actualRadius;

    private SpriteRenderer spriteRenderer;
    
    [SerializeField] private float colliderCooldown = 0.001f;
    private float lastHitTime = -999f;
    [SerializeField] private float damagedCooldown = 0.2f;
    private float lastDamagedTime = -999f;
    
    public LayerMask collisionMask; // 벽과 패들 레이어를 선택하세요
    [SerializeField] private int maxCollisionIterations = 5;
    
    private Transform tr;
    private Vector2 direction;
    private CircleCollider2D cc;
    private bool isGameStarted = false;
    [SerializeField] private ChargingLaserManager razerManager;

    
    Vector2 SnapNormal(Vector2 normal)
    {
        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
        {
            return new Vector2(Mathf.Sign(normal.x), 0f);
        }
        else
        {
            return new Vector2(0f, Mathf.Sign(normal.y));
        }
    }
    
    void Start()
    {
        speed = balanceData.baseSpeed;
        power = balanceData.basePower;

        // 밸런싱 값 복사
        baseSpeed = balanceData.baseSpeed;
        maxSpeed = balanceData.maxSpeed;

        basePower = balanceData.basePower;

        paddleSpeedIncrease = balanceData.paddleSpeedIncrease;
        paddlePowerIncrease = balanceData.paddlePowerIncrease;

        blockSpeedDecrease = balanceData.blockSpeedDecrease;
        blockPowerDecrease = balanceData.blockPowerDecrease;

        centerPullStrength = balanceData.centerPullStrength;
        centerZone = balanceData.centerZone;

        _outsideMaxBounceAngle = balanceData.outsideMaxBounceAngle;
        _insideMaxBounceAngle = balanceData.insideMaxBounceAngle;

        spriteRenderer = GetComponent<SpriteRenderer>();
        direction = new Vector2(0.3f, -1f).normalized;
        //LaunchBall();
        isGameStarted = true;
        speed = baseSpeed;
        power = basePower;
        tr = GetComponent<Transform>();
        tr.localScale = new Vector3(ballRadius,ballRadius,ballRadius);
        cc = GetComponent<CircleCollider2D>();
        actualRadius = cc.radius * transform.lossyScale.x;
        
    }

    void Update()
    {
        MoveBall(speed * Time.deltaTime);
        
        if (speed < baseSpeed)
        {
            speed = baseSpeed;
        }
        if (speed >= maxSpeed)
        {
            speed = maxSpeed;
        }

        
        
        power = speed / 5;
        spriteRenderer.color = new Color(1f, 1f, 1f- (speed-baseSpeed) / (maxSpeed-baseSpeed), 1f);

    }

    void MoveBall(float distance)
    {
        float remainingDistance = distance;

        for (int i = 0; i < maxCollisionIterations; i++)
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position,
                actualRadius,
                direction,
                remainingDistance,
                collisionMask
            );

            if (hit.collider != null)
            {
                
                
                float distanceToHit = Mathf.Max(hit.distance, 0f);

                // 1. 충돌 지점까지 이동
                transform.Translate(direction * distanceToHit, Space.World);

                // 2. 남은 거리 감소
                remainingDistance -= distanceToHit;

                // 3. 반사 방향 계산
                UpdateDirection(hit);

                // 4. 충돌면에서 살짝 떼기
                transform.position += (Vector3)(hit.normal * skinWidth);

                // 5. 같은 프레임 재충돌 방지용으로 남은 거리 약간 감소
                remainingDistance = Mathf.Max(remainingDistance - skinWidth, 0f);

                if (remainingDistance <= 0.001f)
                {
                    break;
                }
            }
            else
            {
                transform.Translate(direction * remainingDistance, Space.World);
                break;
            }
        }

        ResolveOverlap();
    }

    void UpdateDirection(RaycastHit2D hit)
    {
        GameObject obj = hit.collider.gameObject;

        //패들 외 물체와 충돌 시 작용 인터페이스(i ball hit receiver)로 위임 
        var hitObj = hit.collider.GetComponentInParent<IBallHitReceiver>();

        if (hitObj != null)
        {
            hitObj.OnBallHit(); 
        }

        var damageObj = hit.collider.GetComponentInParent<IBallDamagable>();

        if (damageObj != null)
        {
            damageObj.GetDamaged(power);
        }

        // 패들 충돌 로직
        if (obj.name.Contains("paddle_up") || obj.name.Contains("paddle_down") || obj.name.Contains("roof_paddle"))
        {
            if (Time.time < lastHitTime + colliderCooldown)
                return;
            float t = Mathf.InverseLerp(baseSpeed, maxSpeed, speed);
            ballSound.Play(t, BallSound.SoundType.PaddleBounce);
            lastHitTime = Time.time;
            razerManager.CheckBounceCount();
            // 1. 비율 계산 (이미 3으로 잘 나온다면 이 값은 -1 ~ 1 사이가 될 것임)
            float xOffset = (transform.position.x - obj.transform.position.x) / (3f / 2f);
            xOffset = Mathf.Clamp(xOffset, -1f, 1f);
        
            // 2. 튕겨나갈 기본 방향 결정 (위패들은 아래로, 아래패들은 위로)
            Vector2 baseDir = obj.name.Contains("paddle_down") || obj.name.Contains("roof_paddle") ? Vector2.up : Vector2.down;

            // 3. 각도 보정 (Lerp)
            float targetAngle;
            if (obj.name.Contains("paddle_up") || obj.name.Contains("paddle_down"))
            {
                targetAngle = Mathf.Lerp(0, _insideMaxBounceAngle, Mathf.Abs(xOffset));
            }
            else
            {
                targetAngle = Mathf.Lerp(0, _outsideMaxBounceAngle, Mathf.Abs(xOffset));
            }
            
            Quaternion rotation = Quaternion.Euler(0, 0, -xOffset * targetAngle);

            direction = (rotation * baseDir).normalized;

            // 5. 가속 로직 (아래쪽 패들에 닿았을 때만 가속하고 싶다면 조건 유지)
            if (obj.name.Contains("paddle_down"))
            {
                speed += 5; 
            }
        }
        else
        {
            
            float t = Mathf.InverseLerp(baseSpeed, maxSpeed, speed);
                
            ballSound.Play(t, BallSound.SoundType.Bounce);
            GetSlower();
            // 벽이나 기타 오브젝트: 일반적인 물리 반사 법칙 적용
            Vector2 normal = SnapNormal(hit.normal);
            direction = Vector2.Reflect(direction, normal).normalized;
            razerManager.Reset();
        }
    }

    void ResolveOverlap()
    {
        Collider2D overlap = Physics2D.OverlapCircle(tr.position, actualRadius, collisionMask);

        if (overlap == null)
            return;

        BrickCell brick = overlap.GetComponentInParent<BrickCell>();

        //일반 벽돌만 겹침 보정 제외
        //고정 벽돌은 보정해야 끼임 방지됨
        if (brick != null && !brick.IsFixedBrick())
            return;

        //적은 겹침 보정 제외
        Enemy enemy = overlap.GetComponentInParent<Enemy>();
        if (enemy != null)
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

    // 에디터 씬 뷰에서 공의 충돌 범위를 확인하기 위한 기즈모
   
    private void GetSlower()
    { 
        if (Time.time < lastDamagedTime + damagedCooldown)
            return;

        lastDamagedTime = Time.time;
        
        speed -= blockSpeedDecrease;
    }


    //void LaunchBall()
    //{
    //    // 위쪽 방향 중 랜덤한 각도로 발사
    //    float xRandom = Random.Range(-1f, 1f);
    //    Vector2 direction = new Vector2(xRandom, 1f).normalized;
    //    
    //    rb.linearVelocity = direction * speed;
    //}

    //private void OnCollisionStay2D(Collision2D collision)
    //{   
    //    speed += 5;
    //    power += 10;
    //    if (collision.gameObject.name.Contains("paddle_up") || collision.gameObject.name.Contains("paddle_down"))
    //    {
    //        float xOffset = transform.position.x - collision.transform.position.x;
//
    //        // 데드존 안이라면 X축 속도를 즉시 0으로 소멸시킴
    //        if (Mathf.Abs(xOffset) < centerZone)
    //        {
    //            // 현재 속도에서 X만 0으로 만들고 Y 방향은 유지하며 속도 재설정
    //            float yDirection = (collision.gameObject.name.Contains("paddle_down")) ? 0.5f : -0.5f;
    //            rb.linearVelocity = new Vector2(0, yDirection).normalized * speed;
    //            
    //            // 디버깅용 선 그리기 (Game 뷰에서 확인 가능)
    //            Debug.DrawRay(transform.position, rb.linearVelocity, Color.red, 1f);
    //        }
    //    }
    //}

    //void FixedUpdate()
    //{
    //    if (rb.linearVelocity.magnitude != speed && rb.linearVelocity != Vector2.zero)
    //    {
    //        rb.linearVelocity = rb.linearVelocity.normalized * speed;
    //    }
//
    //    if (speed > maxSpeed)
    //    {
    //        speed = maxSpeed;
    //    }
//
    //    if (speed >= baseSpeed)
    //    {
    //        speed -= 0.1f; // 속도가 줄어들 수단이 아직 없어서 임시로 작성
    //    }
    //   
    //}
    
}