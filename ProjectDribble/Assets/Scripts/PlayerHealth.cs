using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHp = 10;

    private int currentHp;
    private bool isDead = false;

    [Header("Ground Visual")]
    [SerializeField] private SpriteRenderer leftGroundRenderer;
    [SerializeField] private SpriteRenderer rightGroundRenderer;

    [SerializeField] private Color fullHpColor = Color.white;
    [SerializeField] private Color lowHpColor = Color.green;

    private void Awake()
    {
        currentHp = maxHp;
        UpdateGroundColor();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        Debug.Log($"Player HP: {currentHp}");

        UpdateGroundColor();

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void UpdateGroundColor()
    {
        float hpRatio = currentHp / (float)maxHp;

        Color currentColor = Color.Lerp(
            lowHpColor,
            fullHpColor,
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