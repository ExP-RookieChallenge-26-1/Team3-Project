using Interfaces;
using UnityEngine;

namespace DefaultNamespace
{
    public class WallBallHitReceiver : MonoBehaviour, IBallHitReceiver,IBallSpeedModifier
    {
        [SerializeField] private float ballSpeedDecrease = 5f;
        public void OnBallHit(BallController ball)
        {
            // 일단 비워둬도 됨.
            // 기본 반사는 BallCollisionHandler가 처리함.
        }
        public void ModifySpeed(BallSpeedController speedController)
        {
            speedController.AddSpeed(-ballSpeedDecrease);
        }
    }
}