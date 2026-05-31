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
    public float PaddleSpeedIncrease = 5f;
    public float BlockSpeedDecrease = -5f;
    public float DamageMultiplier = 0.2f;

    [Header("Speed")]
    [SerializeField] private float dribbleSpeedBonus = 5f;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float maxDamage = 3f;
    [SerializeField] private float paddleDamageBonus = 0.3f;
    [SerializeField] private float dribbleDamageBonus = 0.7f;
    [SerializeField] private float blockDamageLoss = 0.4f;

    [Header("Block Speed Slow")]
    [SerializeField] private float blockSpeedSlowCooldown = 0.25f;

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
    }
}
