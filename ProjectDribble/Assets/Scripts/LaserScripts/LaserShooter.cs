using ScriptableObjects;
using UnityEngine;

public class LaserShooter : MonoBehaviour
{
    [SerializeField] private LaserData laserData;
    [SerializeField] private LaserBlockEraser laserBlockEraser; 
    [SerializeField] private Transform ball;
    [SerializeField] private BallSpeedController ballSpeedController;
    [SerializeField] private Transform firePoint;

    public void ShootLaser(int chargeCount)
    {
        if (laserData == null || chargeCount <= 0)
            return;

        Vector2 origin = firePoint.position;

        float width = laserData.GetWidthForCharge(chargeCount);

        Vector2 laserEndPoint = laserBlockEraser.EraseByLaser(
            origin,
            width,
            laserData.range,
            laserData.startOffset
        );

        Vector2 newBallPosition =
            laserEndPoint - Vector2.up * laserData.ballSpawnBackOffset;

        ball.position = newBallPosition;
        GetBallSpeedController()?.ApplyLaserBoost();
        
    }

    private BallSpeedController GetBallSpeedController()
    {
        if (ballSpeedController != null)
            return ballSpeedController;

        if (ball == null)
            return null;

        ballSpeedController = ball.GetComponent<BallSpeedController>();
        return ballSpeedController;
    }
    
    
}
