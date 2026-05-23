using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(
        fileName = "LaserData",
        menuName = "ScriptableObjects/LaserData"
    )]
    public class LaserData : ScriptableObject
    {
        [Header("Gauge")]
        [Min(0)]
        public int startGaugeValue = 0;

        [Min(1)]
        public int maxGaugeSegments = 3;

        [Min(1)]
        public int gaugePerSegment = 10;


        [Header("Charge")]
        [Min(0.01f)]
        public float chargeTime = 3f;

        [Min(1)]
        public int maxChargeCount = 3;


        [Header("Laser Size")]
        [Min(0f)]
        public float baseWidth = 0.5f;

        [Min(0f)]
        public float widthPerCharge = 0.5f;

        [Min(0f)]
        public float range = 10f;

        [Min(0f)]
        public float startOffset = 0f;


        [Header("Ball After Laser")]
        [Min(0f)]
        public float ballSpawnBackOffset = 0.5f;


        [Header("Preview")]
        public Color previewColor = Color.red;

        [Min(0.001f)]
        public float previewLineWidth = 0.05f;
    }
}