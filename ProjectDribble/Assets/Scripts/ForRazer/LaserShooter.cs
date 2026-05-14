using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class LaserShooter : MonoBehaviour
{
    [SerializeField] ChargingLaserManager chargingManager;
    
    
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
        
        ball.position = newBallPosition;
    }

    private Vector2 FireSegment(Vector2 origin, Vector2 dir, float distance, float width)
    {
        // startOffset을 통해 공보다 살짝 앞에 있도록
        Vector2 start = origin + dir.normalized * startOffset;
        Vector2 endPoint = start + dir.normalized * distance;

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            start,
            new Vector2(width, 0.1f), // 감지 박스 하나의 크기
            Vector2.SignedAngle(Vector2.up, dir), // 박스 회전 각도
            dir, // 감지 진행 방향
            distance, // 감지 진행 거리
            stopLayer
        );

        hits = hits
            .Where(hit => hit.collider != null)
            .OrderBy(hit => hit.distance)
            .ToArray();

        foreach (RaycastHit2D hit in hits)
        {
            BrickCell brick = hit.collider.GetComponentInParent<BrickCell>();
            if (brick != null)
            {
                Debug.Log("레이저가 브릭 감지: " + brick.name);
                brick.DestroybyLaser();
                if (brick.IsFixedBrick())
                {
                    endPoint = hit.point;
                    break;
                }
                continue;
            }
            
            Debug.Log("레이저가 벽/천장 감지: " + hit.collider.name);
            endPoint = hit.point;
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
