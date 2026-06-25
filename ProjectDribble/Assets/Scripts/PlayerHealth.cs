using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public event Action OnPlayerDead;

    [Header("Data")]
    [SerializeField] private HealthData healthData;

    [Header("Auto Recovery")]
    [SerializeField] private bool enableAutoRecovery = true;
    [SerializeField, Min(0f)] private float recoveryDelay = 3f;
    [SerializeField, Min(0f)] private float recoveryPerSecond = 1f;

    private int currentHp;
    private int runtimeMaxHp;
    private bool isDead;
    private bool gameOverSoundStarted;
    private float lastDamageTime;
    private float pendingRecovery;

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

    private void Update()
    {
        UpdateAutoRecovery();
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
        lastDamageTime = Time.time;
        pendingRecovery = 0f;
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
            lastDamageTime = Time.time;
            pendingRecovery = 0f;

            float dangerRatio = 1f - currentHp / (float)runtimeMaxHp;
            SoundManager.Instance.Play(SoundId.PlayerHit, Mathf.Clamp01(dangerRatio));
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateAutoRecovery()
    {
        if (!enableAutoRecovery ||
            isDead ||
            currentHp >= runtimeMaxHp ||
            Time.time - lastDamageTime < recoveryDelay)
        {
            return;
        }

        pendingRecovery += recoveryPerSecond * Time.deltaTime;
        int recoveredHp = Mathf.FloorToInt(pendingRecovery);

        if (recoveredHp <= 0)
            return;

        pendingRecovery -= recoveredHp;
        currentHp = Mathf.Min(runtimeMaxHp, currentHp + recoveredHp);
        UpdateHealthVisuals();
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
