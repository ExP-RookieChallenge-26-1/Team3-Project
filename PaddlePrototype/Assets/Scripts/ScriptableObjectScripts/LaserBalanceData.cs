using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjectScripts
{
    [CreateAssetMenu(menuName = "Game/Laser Gauge Data")]
    public class LaserGaugeData : ScriptableObject
    {
        [Header("Laser")]
        public float baseWidth = 1.0f;
        public float widthPerCharge = 0.5f;
        public float range = 20f;
        public float startOffset = 0.5f;
        public float ballSpawnBackOffset = 0.3f;

        [Header("Charging")]
        public int perBounceCount = 10;

        [FormerlySerializedAs("startGaugeValue")] [Header("Gauge")]
        public int startGaugeValue = 30;
        public int gaugePerSegment = 10;
        public int maxGaugeSegments = 3;
        
    }
}