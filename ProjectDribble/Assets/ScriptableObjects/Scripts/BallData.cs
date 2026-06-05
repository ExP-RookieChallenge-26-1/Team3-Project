using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BallData", menuName = "Scriptable Objects/BallData")]
public class BallData : ScriptableObject
{
    [System.Serializable]
    public class SpeedSlowRule
    {
        [Tooltip("Apply this rule when the current speed is at least this value.")]
        public float minSpeed;

        [Tooltip("Speed decrease applied on block hit.")]
        public float slowAmount;
    }

    public float baseSpeed = 30f;
    public float maxSpeed = 55f;
    public float ballDamage = 1f;
    public float outerPaddleSpeedIncrease = 5f;
    public float innerPaddleSpeedIncrease = 5f;
    public float BlockSpeedDecrease = -5f;
    public float DamageMultiplier = 0.2f;

    [Header("Speed")]
    [SerializeField] private float dribbleSpeedBonus = 5f;

    [Header("Speed State")]
    [SerializeField] private float normalMaxSpeed = 70f;
    [SerializeField] private float laserMinBoostSpeed = 80f;
    [SerializeField] private float laserMaxSpeed = 100f;
    [SerializeField] private float laserBoostAmount = 20f;
    [SerializeField] private float laserReturnThreshold = 70f;
    [SerializeField] private float weakenedSpeed = 25f;
    [SerializeField] private float weakenedDuration = 0.75f;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float maxDamage = 3f;
    [SerializeField] private float paddleDamageBonus = 0.3f;
    [SerializeField] private float dribbleDamageBonus = 0.7f;
    [SerializeField] private float blockDamageLoss = 0.4f;

    [Header("Block Speed Slow")]
    [SerializeField] private float blockSpeedSlowCooldown = 0.25f;

    [Header("Capture")]
    [SerializeField] private float captureCooldown = 0.12f;
    [SerializeField] private float releaseRecaptureDelay = 0.15f;
    [SerializeField] private float minEntranceDirectionX = 0.1f;
    [SerializeField] private bool debugCaptureLog = true;

    [Header("Captured Dribble")]
    [SerializeField] private float capturedDribbleSpeedFallback = 20f;
    [SerializeField] private float capturedTopOffset = 0.5f;
    [SerializeField] private float capturedBottomOffset = -0.5f;
    [SerializeField] private float capturedXFollowSpeed = 30f;
    [SerializeField] private float capturedPaddleHitCooldown = 0.05f;
    [SerializeField] private bool debugCapturedDribbleLog = true;

    [SerializeField] private List<SpeedSlowRule> blockSlowRules = new()
    {
        new SpeedSlowRule { minSpeed = 0f, slowAmount = 4f },
        new SpeedSlowRule { minSpeed = 40f, slowAmount = 3f },
        new SpeedSlowRule { minSpeed = 55f, slowAmount = 2f },
        new SpeedSlowRule { minSpeed = 65f, slowAmount = 1f },
    };

    [SerializeField] private float minDirectionX = 0.25f;
    [SerializeField] private float minDirectionY = 0.1f;

    public float MinDirectionX => minDirectionX;
    public float MinDirectionY => minDirectionY;
    public float BaseDamage => baseDamage;
    public float MaxDamage => maxDamage;
    public float PaddleDamageBonus => paddleDamageBonus;
    public float DribbleDamageBonus => dribbleDamageBonus;
    public float BlockDamageLoss => blockDamageLoss;
    public float BlockSpeedSlowCooldown => blockSpeedSlowCooldown;
    public float DribbleSpeedBonus => dribbleSpeedBonus;
    public float NormalMaxSpeed => normalMaxSpeed;
    public float LaserMinBoostSpeed => laserMinBoostSpeed;
    public float LaserMaxSpeed => laserMaxSpeed;
    public float LaserBoostAmount => laserBoostAmount;
    public float LaserReturnThreshold => laserReturnThreshold;
    public float WeakenedSpeed => weakenedSpeed;
    public float WeakenedDuration => weakenedDuration;
    public float CaptureCooldown => captureCooldown;
    public float ReleaseRecaptureDelay => releaseRecaptureDelay;
    public float MinEntranceDirectionX => minEntranceDirectionX;
    public bool DebugCaptureLog => debugCaptureLog;
    public float CapturedDribbleSpeedFallback => capturedDribbleSpeedFallback;
    public float CapturedTopOffset => capturedTopOffset;
    public float CapturedBottomOffset => capturedBottomOffset;
    public float CapturedXFollowSpeed => capturedXFollowSpeed;
    public float CapturedPaddleHitCooldown => capturedPaddleHitCooldown;
    public bool DebugCapturedDribbleLog => debugCapturedDribbleLog;

    public float GetBlockSlowAmount(float currentSpeed)
    {
        if (blockSlowRules == null || blockSlowRules.Count == 0)
            return Mathf.Max(0f, -BlockSpeedDecrease);

        float result = blockSlowRules[0].slowAmount;

        foreach (SpeedSlowRule rule in blockSlowRules)
        {
            if (currentSpeed >= rule.minSpeed)
                result = rule.slowAmount;
        }

        return Mathf.Max(0f, result);
    }

    private void OnValidate()
    {
        if (blockSlowRules != null)
            blockSlowRules.Sort((a, b) => a.minSpeed.CompareTo(b.minSpeed));

        minDirectionX = Mathf.Clamp01(minDirectionX);
        minDirectionY = Mathf.Clamp01(minDirectionY);
        baseDamage = Mathf.Max(0f, baseDamage);
        maxDamage = Mathf.Max(baseDamage, maxDamage);
        paddleDamageBonus = Mathf.Max(0f, paddleDamageBonus);
        dribbleDamageBonus = Mathf.Max(0f, dribbleDamageBonus);
        blockDamageLoss = Mathf.Max(0f, blockDamageLoss);
        blockSpeedSlowCooldown = Mathf.Max(0f, blockSpeedSlowCooldown);
        dribbleSpeedBonus = Mathf.Max(0f, dribbleSpeedBonus);
        normalMaxSpeed = Mathf.Max(baseSpeed, normalMaxSpeed);
        laserReturnThreshold = Mathf.Max(0f, laserReturnThreshold);
        laserMinBoostSpeed = Mathf.Max(laserReturnThreshold, laserMinBoostSpeed);
        laserMaxSpeed = Mathf.Max(laserMinBoostSpeed, laserMaxSpeed);
        laserBoostAmount = Mathf.Max(0f, laserBoostAmount);
        weakenedSpeed = Mathf.Max(0f, weakenedSpeed);
        weakenedDuration = Mathf.Max(0f, weakenedDuration);
        captureCooldown = Mathf.Max(0f, captureCooldown);
        releaseRecaptureDelay = Mathf.Max(0f, releaseRecaptureDelay);
        minEntranceDirectionX = Mathf.Max(0f, minEntranceDirectionX);
        capturedDribbleSpeedFallback = Mathf.Max(0f, capturedDribbleSpeedFallback);
        capturedXFollowSpeed = Mathf.Max(0f, capturedXFollowSpeed);
        capturedPaddleHitCooldown = Mathf.Max(0f, capturedPaddleHitCooldown);
    }
}
