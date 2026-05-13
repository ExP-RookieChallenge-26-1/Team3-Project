using UnityEngine;

namespace ScriptableObjectScripts
{
    [CreateAssetMenu(menuName = "Game/Paddle Data")]
    public class PaddleData : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 50f;
        public float paddleWidth = 0.7f;

        [Header("Collision")]
        public float activeCollisionEnabled = 1f;

        [Header("Visual")]
        public float transparentAlpha = 0.3f;
    }
}