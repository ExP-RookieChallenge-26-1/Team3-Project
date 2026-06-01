using UnityEngine;

[CreateAssetMenu(
    fileName = "HealthData",
    menuName = "ScriptableObjects/HealthData"
)]
public class HealthData : ScriptableObject
{
    [Header("Player HP")]
    public int playerMaxHp = 10;

    [Header("Player Ground Visual")]
    public Color fullHpColor = Color.white;
    public Color lowHpColor = Color.green;

    [Header("Block Damage Zone")]
    public int damagePerTick = 1;
    public float damageInterval = 1f;
    
    [Header("Ceiling HP")]
    public int ceilingMaxHp = 100;
}