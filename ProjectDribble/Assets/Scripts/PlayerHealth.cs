using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public event Action OnPlayerDead;

    [Header("Data")]
    [SerializeField] private HealthData healthData;

    private int currentHp;
    private int runtimeMaxHp;
    private bool isDead;
    private bool gameOverSoundStarted;

    [Header("Ground Visual")]
    [SerializeField] private SpriteRenderer leftGroundRenderer;
    [SerializeField] private SpriteRenderer rightGroundRenderer;
    [SerializeField] private SpriteRenderer UpPaddleRenderer;
    [SerializeField] private SpriteRenderer DownPaddleRenderer;

    [Header("Paddle Visual")]
    [SerializeField] private bool enablePaddleBaseTint;
    [SerializeField] private PaddleCorruptionVisual[] paddleCorruptionVisuals;
    
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
        gameOverSoundStarted = false;
        currentHp = runtimeMaxHp > 0 ? runtimeMaxHp : Mathf.Max(1, healthData.playerMaxHp);
        UpdateHealthVisuals();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        int previousHp = currentHp;
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, runtimeMaxHp);

        Debug.Log($"Player HP: {currentHp}");
        UpdateHealthVisuals();

        if (currentHp < previousHp)
        {
            float dangerRatio = 1f - currentHp / (float)runtimeMaxHp;
            SoundManager.Instance.Play(SoundId.PlayerHit, Mathf.Clamp01(dangerRatio));
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthVisuals()
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

        if (enablePaddleBaseTint)
        {
            if (DownPaddleRenderer != null)
                ApplyRendererRgb(DownPaddleRenderer, currentColor);

            if (UpPaddleRenderer != null)
                ApplyRendererRgb(UpPaddleRenderer, currentColor);
        }

        UpdatePaddleCorruptionVisuals(hpRatio);
    }

    private void ApplyRendererRgb(SpriteRenderer targetRenderer, Color rgbSource)
    {
        Color color = targetRenderer.color;
        color.r = rgbSource.r;
        color.g = rgbSource.g;
        color.b = rgbSource.b;
        targetRenderer.color = color;
    }

    private void UpdatePaddleCorruptionVisuals(float hpRatio)
    {
        if (paddleCorruptionVisuals == null)
            return;

        for (int i = 0; i < paddleCorruptionVisuals.Length; i++)
            paddleCorruptionVisuals[i]?.ApplyHealthRatio(hpRatio);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("Game Over");
        StartGameOverSoundDelay();
        OnPlayerDead?.Invoke();
    }

    private void StartGameOverSoundDelay()
    {
        if (gameOverSoundStarted)
            return;

        gameOverSoundStarted = true;
        StartCoroutine(PlayGameOverSoundDelayed());
    }

    private IEnumerator PlayGameOverSoundDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        SoundManager.Instance.Play(SoundId.GameOver);
    }
}
