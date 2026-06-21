using UnityEngine;

public class BallPowerController : MonoBehaviour
{
    public BallData data;

    [SerializeField] private float currentDamage;

    private bool hasStageMaxDamageOverride;
    private float stageMaxDamageOverride;
    private float powerGainMultiplier = 1f;

    public float CurrentDamageValue => currentDamage;

    void Start()
    {
        ResetToBaseDamage();
    }
    
    public float CurrentDamage()
    {
        ClampCurrentDamage();
        return currentDamage;
    }

    public void ResetToBaseDamage()
    {
        float baseDamage = data != null ? data.BaseDamage : 1f;
        currentDamage = hasStageMaxDamageOverride
            ? Mathf.Min(baseDamage, stageMaxDamageOverride)
            : baseDamage;
    }

    public void AddDamage(float amount)
    {
        if (amount > 0f)
            amount *= powerGainMultiplier;

        currentDamage += amount;
        ClampCurrentDamage();
    }

    public void AddPaddleDamage()
    {
        AddDamage(data != null ? data.PaddleDamageBonus : 0f);
    }

    public void AddDribbleDamage()
    {
        AddDamage(data != null ? data.DribbleDamageBonus : 0f);
    }

    public void ApplyBlockDamageLoss()
    {
        float beforeDamage = currentDamage;
        float lossAmount = data != null ? data.BlockDamageLoss : 0f;

        AddDamage(-lossAmount);

        Debug.Log($"[BallDamage] Damage Loss: {lossAmount}, CurrentDamage Before Loss: {beforeDamage}, CurrentDamage After Loss: {currentDamage}");
    }

    public void ApplyStageTuning(float maxDamageOverride, float gainMultiplier)
    {
        hasStageMaxDamageOverride = true;
        stageMaxDamageOverride = Mathf.Max(0f, maxDamageOverride);
        powerGainMultiplier = Mathf.Max(0f, gainMultiplier);
        ClampCurrentDamage();
    }

    public void ClearStageTuning()
    {
        hasStageMaxDamageOverride = false;
        powerGainMultiplier = 1f;
        ClampCurrentDamage();
    }

    private void ClampCurrentDamage()
    {
        float baseDamage = data != null ? data.BaseDamage : 0f;
        float maxDamage = hasStageMaxDamageOverride
            ? stageMaxDamageOverride
            : data != null ? data.MaxDamage : float.PositiveInfinity;

        float minDamage = Mathf.Min(baseDamage, maxDamage);
        currentDamage = Mathf.Clamp(currentDamage, minDamage, maxDamage);
    }
}
