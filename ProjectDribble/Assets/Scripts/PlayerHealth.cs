using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public event Action OnPlayerDead;

    [Header("Data")]
    [SerializeField] private HealthData healthData;

    private int currentHp;
    private int runtimeMaxHp;
    private bool isDead;

    [Header("Ground Visual")]
    [SerializeField] private SpriteRenderer leftGroundRenderer;
    [SerializeField] private SpriteRenderer rightGroundRenderer;
    [SerializeField] private SpriteRenderer UpPaddleRenderer;
    [SerializeField] private SpriteRenderer DownPaddleRenderer;
    
    public int CurrentHp => currentHp;
    public int MaxHp => runtimeMaxHp;

    private void Awake()
    {
        InitializePlayerHealth(healthData.playerMaxHp);
    }

    public void InitializePlayerHealth(int maxHp)
    {
        runtimeMaxHp = Mathf.Max(1, maxHp);
        ResetPlayerHealth();
    }

    public void ResetPlayerHealth()
    {
        isDead = false;
        currentHp = runtimeMaxHp > 0 ? runtimeMaxHp : Mathf.Max(1, healthData.playerMaxHp);
        UpdateGroundColor();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, runtimeMaxHp);

        Debug.Log($"Player HP: {currentHp}");
        UpdateGroundColor();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateGroundColor()
    {
        float hpRatio = currentHp / (float)runtimeMaxHp;

        Color currentColor = Color.Lerp(
            healthData.lowHpColor,
            healthData.fullHpColor,
            hpRatio
        );

        if (leftGroundRenderer != null)
            leftGroundRenderer.color = currentColor;

        if (rightGroundRenderer != null)
            rightGroundRenderer.color = currentColor;
        if (DownPaddleRenderer != null)
            DownPaddleRenderer.color = currentColor;

        if (UpPaddleRenderer != null)
            UpPaddleRenderer.color = currentColor;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("Game Over");
        OnPlayerDead?.Invoke();
    }
}
