using UnityEngine;


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
        public float chargeTime = 3f;
        public int maxChargeCount = 3;
        
        [Header("Gauge")]
        public int startGaugeValue = 30;
        public int gaugePerSegment = 10;
        public int maxGaugeSegments = 3;
        
    }
}