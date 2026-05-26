using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private HealthData healthData;

    private int currentHp;
    private bool isDead = false;

    [Header("Ground Visual")]
    [SerializeField] private SpriteRenderer leftGroundRenderer;
    [SerializeField] private SpriteRenderer rightGroundRenderer;

    private void Awake()
    {
        currentHp = healthData.playerMaxHp;
        UpdateGroundColor();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, healthData.playerMaxHp);

        Debug.Log($"Player HP: {currentHp}");

        UpdateGroundColor();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateGroundColor()
    {
        float hpRatio = currentHp / (float)healthData.playerMaxHp;

        Color currentColor = Color.Lerp(
            healthData.lowHpColor,
            healthData.fullHpColor,
            hpRatio
        );

        if (leftGroundRenderer != null)
            leftGroundRenderer.color = currentColor;

        if (rightGroundRenderer != null)
            rightGroundRenderer.color = currentColor;
    }

    private void Die()
    {
        isDead = true;

        Debug.Log("Game Over - Restart Scene");

        RestartScene();
    }

    private void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}