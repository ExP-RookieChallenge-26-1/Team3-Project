using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class LaserShooter : MonoBehaviour
{
    [SerializeField] private BallSound ballSound;
    
    [SerializeField] private ScriptableObjectScripts.LaserGaugeData laserGaugeData;
    [SerializeField] ChargingLaserManager chargingManager;
    [SerializeField] private float sameRowYTolerance = 0.05f;
    
    [SerializeField] private Transform ball;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask stopLayer; // Brick + Wall/Ceiling 포함
    [SerializeField] private Transform paddle;

    [Header("Laser")]
    [SerializeField] private float baseWidth = 1.0f;
    [SerializeField] private float widthPerCharge = 0.5f;
    [SerializeField] private float range = 20f;
    [SerializeField] private float startOffset = 0.5f;
    
    [SerializeField] private float ballSpawnBackOffset = 0.3f;
    /*
     * 위치 기준으로 chargeUsing에 따른 오프셋만큼 넓이를 잡고
     * 그 넓이내의 블록과 적을 포착한 후 없앤다.
     *
     * 공을 위로 올리고
     * 공의 속도를 일시적으로 Max보다 높은 속도로 이동하게 한다
     * 이동방향은 Up
     *
     */

    private void Start()
    {
        baseWidth = laserGaugeData.baseWidth;
        widthPerCharge = laserGaugeData.widthPerCharge;
        range = laserGaugeData.range;
        startOffset = laserGaugeData.startOffset;
        ballSpawnBackOffset = laserGaugeData.ballSpawnBackOffset;
    }

    public void Shoot(int chargeCount)
    {
        Debug.Log("차징 "+chargeCount+"슛");

        Vector2 origin = paddle.position;
        Vector2 direction = Vector2.up; // 일단은 위로만 발사되도록

        float width = baseWidth + widthPerCharge * chargeCount;

        Vector2 laserEndBlock = FireSegment(origin, direction, range, width);
        Vector2 laserEndPoint = new Vector2(origin.x, laserEndBlock.y);
        Vector2 newBallPosition =
            laserEndPoint - direction.normalized * ballSpawnBackOffset;
        chargingManager.charging = false;
        
        
        
        
        ball.position = newBallPosition;
    }

    private Vector2 FireSegment(Vector2 origin, Vector2 dir, float distance, float width)
{
    Vector2 start = origin + dir.normalized * startOffset;
    Vector2 endPoint = start + dir.normalized * distance;

    RaycastHit2D[] hits = Physics2D.BoxCastAll(
        start,
        new Vector2(width, 0.1f),
        Vector2.SignedAngle(Vector2.up, dir),
        dir,
        distance,
        stopLayer
    );

    hits = hits
        .Where(hit => hit.collider != null)
        .OrderBy(hit => hit.distance)
        .ToArray();

    bool foundFixedRow = false;
    float fixedRowY = 0f;

    HashSet<BrickCell> destroyedBricks = new HashSet<BrickCell>();

    foreach (RaycastHit2D hit in hits)
    {
        BrickCell brick = hit.collider.GetComponentInParent<BrickCell>();

        if (brick != null)
        {
            if (destroyedBricks.Contains(brick))
                continue;

            // 아직 고정 브릭 줄을 만나기 전
            if (!foundFixedRow)
            {
                Debug.Log("레이저가 브릭 감지: " + brick.name);

                bool isFixed = brick.IsFixedBrick();

                brick.DestroybyLaser();
                destroyedBricks.Add(brick);

                if (isFixed)
                {
                    foundFixedRow = true;
                    fixedRowY = brick.transform.position.y;
                    endPoint = hit.point;
                }

                continue;
            }

            // 이미 고정 브릭 줄을 만난 이후:
            // 같은 y줄의 브릭은 모두 파괴
            if (Mathf.Abs(brick.transform.position.y - fixedRowY) <= sameRowYTolerance)
            {
                Debug.Log("레이저가 같은 y줄 브릭 감지: " + brick.name);

                brick.DestroybyLaser();
                destroyedBricks.Add(brick);

                continue;
            }

            // 다른 y줄 브릭이면 레이저 종료
            break;
        }

        // 벽/천장 처리
        if (!foundFixedRow)
        {
            Debug.Log("레이저가 벽/천장 감지: " + hit.collider.name);
            endPoint = hit.point;
        }

        break;
    }

    return endPoint;
}
    
    private void OnDrawGizmos()
    {
        if (!chargingManager.charging)
            return;

        Vector2 dir = Vector2.up;

        float width = baseWidth + widthPerCharge * chargingManager.chargeCount;

        float angle = Vector2.SignedAngle(Vector2.up, dir);

        Vector2 center =
            (Vector2)paddle.position
            + dir.normalized * (range * 0.5f);

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                center,
                Quaternion.Euler(0, 0, angle),
                Vector3.one
            );

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(width, range, 1f)
        );

        Gizmos.matrix = oldMatrix;
    }
}
