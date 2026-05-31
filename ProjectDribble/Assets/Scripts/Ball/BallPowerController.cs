using UnityEngine;

public class BallPowerController : MonoBehaviour
{
    public BallData data;

    [SerializeField] private float currentDamage;

    public float CurrentDamageValue => currentDamage;

    void Start()
    {
        ResetToBaseDamage();
    }
    
    public float CurrentDamage()
    {
        return currentDamage;
    }

    public void ResetToBaseDamage()
    {
        currentDamage = data != null ? data.BaseDamage : 1f;
    }

    public void AddDamage(float amount)
    {
        if (data == null)
        {
            currentDamage = Mathf.Max(0f, currentDamage + amount);
            return;
        }

        currentDamage += amount;
        currentDamage = Mathf.Clamp(currentDamage, data.BaseDamage, data.MaxDamage);
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
}
