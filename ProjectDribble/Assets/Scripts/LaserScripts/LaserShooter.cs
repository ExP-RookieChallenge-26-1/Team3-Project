using ScriptableObjects;
using UnityEngine;

public class LaserShoot : MonoBehaviour
{
    [SerializeField] private LaserData laserData;
    [SerializeField] private LaserBlockEraser laserBlockEraser; 
    [SerializeField] private Transform ball;
    [SerializeField] private Transform firePoint;
    public void ShootLaser(int chargeCount)
    {
       

        Vector2 origin = firePoint.position;

        float width = laserData.baseWidth +laserData.widthPerCharge  * chargeCount;

        Vector2 laserEndPoint = laserBlockEraser.EraseByLaser(
            origin,
            width,
            laserData.range,
            laserData.startOffset
        );

        Vector2 newBallPosition =
            laserEndPoint - Vector2.up * laserData.ballSpawnBackOffset;

        ball.position = newBallPosition;
        
    }
    
    
}
