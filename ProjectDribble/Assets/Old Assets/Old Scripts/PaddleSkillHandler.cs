using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaddleSkillHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BallController ball;
    [SerializeField] private BrickManager brickManager;
    [SerializeField] private LayerMask targetLayer;

    [Header("Gauge & Charge")]
    public float currentGauge = 3f; // 테스트를 위해 3으로 설정
    private int chargeLevel = 0;
    private bool isCharging = false;
    private float chargeHoldTimer = 0f;

    [Header("Dribble Logic")]
    private int dribbleCount = 0;
    private float dribbleWindowTimer = 0f;
    private const float DRIBBLE_TIME_LIMIT = 3f;

    private Collider2D paddleCollider;

    void Awake()
    {
        paddleCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        HandleDribbleTimer();
        HandleChargingInput();
    }

    // BallController에서 패들에 충돌할 때 이 함수를 호출해야 함
    public void OnBallHitPaddle()
    {
        if (isCharging) return;

        if (dribbleCount == 0) dribbleWindowTimer = DRIBBLE_TIME_LIMIT;
        dribbleCount++;

        // 3초 내 2번 왕복 시 차징 시작 조건
        if (dribbleCount >= 2 && currentGauge >= 1f)
        {
            StartCharging();
        }
    }

    private void HandleDribbleTimer()
    {
        if (dribbleWindowTimer > 0)
        {
            dribbleWindowTimer -= Time.deltaTime;
            if (dribbleWindowTimer <= 0) dribbleCount = 0;
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeLevel = 1;
        currentGauge -= 1f;
        chargeHoldTimer = 0f;
        dribbleCount = 0;
        Debug.Log("차징 시작: 1단계");
    }

    private void HandleChargingInput()
    {
        if (!isCharging) return;

        // 차징 유지 및 단계 상승
        if (Input.GetKey(KeyCode.Space))
        {
            chargeHoldTimer += Time.deltaTime;
            if (chargeHoldTimer >= 2f && chargeLevel < 3 && currentGauge >= 1f)
            {
                chargeLevel++;
                currentGauge -= 1f;
                chargeHoldTimer = 0f;
                Debug.Log($"차징 단계 상승: {chargeLevel}단계");
            }
        }

        // 스페이스바를 떼면 패들 비활성화 및 공 방출 대기
        if (Input.GetKeyUp(KeyCode.Space))
        {
            StartCoroutine(ReleaseSequence());
        }
    }

    private IEnumerator ReleaseSequence()
    {
        // 1. 패들 비활성화 (공이 통과하게 함)
        paddleCollider.enabled = false;
        Debug.Log("패들 비활성화 - 공 방출 대기");

        // 2. 공이 패들 라인(Y축) 아래로 완전히 빠져나갈 때까지 대기
        // 패들보다 아래로 내려가는 순간이 '발사' 타이밍
        while (ball.transform.position.y > transform.position.y - 0.5f)
        {
            yield return null;
        }

        // 3. 발사 실행
        ExecuteSkill();

        // 4. 패들 복구
        paddleCollider.enabled = true;
        isCharging = false;
    }

    private void ExecuteSkill()
    {
        Debug.Log($"{chargeLevel}단계 레이저 발사!");

        // 레이저 범위 및 데미지 설정
        float laserWidth = 2f + (chargeLevel - 1) * 1.5f;
        float damage = chargeLevel * 30f;

        // 레이저 히트박스 생성 (수직 위 방향)
        Vector2 boxSize = new Vector2(laserWidth, 20f);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * 10f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, targetLayer);

        foreach (var hit in hits)
        {
            // 인터페이스를 통한 벽돌 파괴 (부스러기X, 매니저 데이터 동기화O)
            var receiver = hit.GetComponentInParent<IBallHitReceiver>();
            if (receiver != null)
            {
                receiver.OnBallHit();
            }

            // 적 데미지 처리 (Enemy 스크립트에 TakeDamage가 있다고 가정)
            // hit.GetComponentInParent<Enemy>()?.TakeDamage(damage);
        }

        // 공 연출 실행
        //ball.ExecutePowerShot(transform.position.x);
    }
}