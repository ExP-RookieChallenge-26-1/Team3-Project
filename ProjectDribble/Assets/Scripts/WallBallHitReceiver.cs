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

        private void OnDrawGizmosSelected()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

            Gizmos.color = Color.cyan;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D wallCollider = colliders[i];

                if (wallCollider == null)
                    continue;

                Bounds bounds = wallCollider.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
