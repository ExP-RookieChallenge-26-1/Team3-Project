using ScriptableObjects;
using System;
using UnityEngine;

public class LaserShooter : MonoBehaviour
{
    [SerializeField] private LaserData laserData;
    [SerializeField] private LaserBlockEraser laserBlockEraser; 
    [SerializeField] private Transform ball;
    [SerializeField] private BallSpeedController ballSpeedController;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LaserUnlockState laserUnlockState;

    public event Action OnLaserFired;

    private void Awake()
    {
        if (laserUnlockState == null)
            laserUnlockState = FindAnyObjectByType<LaserUnlockState>();
    }

    public void ShootLaser(int chargeCount)
    {
        if (laserUnlockState == null || !laserUnlockState.IsLaserUnlocked)
            return;

        if (laserData == null || laserBlockEraser == null || ball == null || firePoint == null || chargeCount <= 0)
            return;

        Vector2 origin = firePoint.position;

        float width = laserData.GetWidthForCharge(chargeCount);

        Vector2 laserEndPoint = laserBlockEraser.EraseByLaser(
            origin,
            width,
            laserData.range,
            laserData.startOffset,
            laserData.laserAffectsBelowPaddle,
            laserData.laserBottomOffset
        );

        Vector2 newBallPosition =
            laserEndPoint - Vector2.up * laserData.ballSpawnBackOffset;

        ball.position = newBallPosition;
        GetBallSpeedController()?.ApplyLaserBoost();
        OnLaserFired?.Invoke();
        
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
