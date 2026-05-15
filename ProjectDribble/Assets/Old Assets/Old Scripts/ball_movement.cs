using UnityEngine;

public class BallController : MonoBehaviour
{
    private float speed = 10f; // 공의 초기 속도
    [SerializeField] private float baseSpeed = 10f; // 공의 초기 속도
    [SerializeField] private float maxSpeed = 30f; // 최대 속도

    private float power = 10f;
    [SerializeField] private float basePower = 10f;

    [SerializeField] private float paddleSpeedIncrease = 5f;
    [SerializeField] private float paddlePowerIncrease = 10f;

    [SerializeField] private float blockSpeedDecrease = 5f;
    [SerializeField] private float blockPowerDecrease = 10f;

    [SerializeField] private float ballRadius = 0.7f;

    [SerializeField] private float skinWidth = 0.1f; // 벽과 거리 유지 정도

    [SerializeField] private float _outsideMaxBounceAngle = 50f;
    [SerializeField] private float _insideMaxBounceAngle = 50f;

    private float actualRadius;

    public LayerMask collisionMask; // 벽과 패들 레이어를 선택하세요
    
    private Transform tr;
    private Vector2 direction;
    private CircleCollider2D cc;
    private bool isGameStarted = false;
    [SerializeField] private ChargingLaserManager razerManager;

    void Start()
    {
        direction = new Vector2(0.5f, 1f).normalized;
        //LaunchBall();
        isGameStarted = true;
        speed = baseSpeed;
        power = basePower;
        tr = GetComponent<Transform>();
        tr.localScale = new Vector3(ballRadius,ballRadius,ballRadius);
        cc = GetComponent<CircleCollider2D>();
        actualRadius = cc.radius * ballRadius*1.5f;
        
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
        speed -= 0.025f;
    }

    void MoveBall(float distance)
    {
        // 1. CircleCast로 이동 경로에 장애물이 있는지 확인
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, actualRadius, direction, distance, collisionMask);

        if (hit.collider != null)
        {
            // 2. 충돌 지점까지 우선 이동 (충돌 지점에서 아주 살짝 띄움)
            float distanceToHit = hit.distance;
            transform.Translate(direction * distanceToHit, Space.World);

            // 3. 충돌 대상에 따른 반사 방향 계산
            float remainingDistance = distance - distanceToHit;
            UpdateDirection(hit);

            // 4. 남은 거리가 있다면 새로운 방향으로 다시 이동 (재귀 호출 방지를 위해 단순화)
            if (remainingDistance > 0)
            {
                transform.Translate(direction * remainingDistance, Space.World);
            }
        }
        else
        {
            // 충돌이 없다면 지정된 거리만큼 직선 이동
            transform.Translate(direction * distance, Space.World);
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

        // 패들 충돌 로직
        if (obj.name.Contains("paddle_up") || obj.name.Contains("paddle_down") || obj.name.Contains("roof_paddle"))
        {
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
            // 벽이나 기타 오브젝트: 일반적인 물리 반사 법칙 적용
            direction = Vector2.Reflect(direction, hit.normal).normalized;
            razerManager.Reset();
        }
    }

    void ResolveOverlap()
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

    // 에디터 씬 뷰에서 공의 충돌 범위를 확인하기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, actualRadius);
    }


// 공 속도(현재 속도)
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

// 패들 충돌시 속도 증가량
    public void SetPaddleSpeedIncrease(float amount)
    {
        paddleSpeedIncrease = amount;
    }
    public void AddPaddleSpeedIncrease(float amount)
    {
        paddleSpeedIncrease += amount;
    }

// 패들 충돌시 파워 증가량
    public void SetPaddlePowerIncrease(float amount)
    {
        paddlePowerIncrease = amount;
    }
    public void AddPaddlePowerIncrease(float amount)
    {
        paddlePowerIncrease += amount;
    }

// 벽돌 충돌시 속도 감소량
    public void SetBlockSpeedDecrease(float amount)
    {
        blockSpeedDecrease = amount;
    }
    public void AddBlockSpeedDecrease(float amount)
    {
        blockSpeedDecrease += amount;
    }

// 벽돌 충돌시 파워 감소량
    public void SetBlockPowerDecrease(float amount)
    {
        blockPowerDecrease = amount;
    }
    public void AddBlockPowerDecrease(float amount)
    {
        blockPowerDecrease += amount;
    }

// 공 반지름 설정
    public void SetBallRadius(float amount)
    {
        ballRadius = amount;
    }
    public void AddBallRadius(float amount)
    {
        ballRadius += amount;
    }


// 패들 내부 각도 보정 최대치
    public void SetInsideMaxBounceAngle(float amount)
    {
        _insideMaxBounceAngle = amount;
    }
    public void AddInsideMaxBounceAngle(float amount)
    {
        _insideMaxBounceAngle += amount;
    }

// 패들 외부 각도 보정 최대치
    public void SetOutsideMaxBounceAngle(float amount)
    {
        _outsideMaxBounceAngle = amount;
    }
    public void AddOutsideMaxBounceAngle(float amount)
    {
        _outsideMaxBounceAngle += amount;
    }
}