using DefaultNamespace;
using TMPro;
using UnityEngine;

public class Ceiling : MonoBehaviour,IBallDamagable
{
    [SerializeField] private int ceilingHp = 100;
    [SerializeField] private TextMeshProUGUI ceilingHealthText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float damageCooldown = 0.2f;
    private float lastDamagedTime = -999f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateGaugeUI();
    }

    // Update is called once per frame
    public void GetDamaged(float damage)
    {
        if (Time.time < lastDamagedTime + damageCooldown)
            return;

        lastDamagedTime = Time.time;
        
        
        ceilingHp -= (int)damage;
        UpdateGaugeUI();
        if (ceilingHp <= 0)
        {
            gameManager.RestartGame();
        }
    }
    private void UpdateGaugeUI()
    {
        if (ceilingHealthText != null)
        {
            ceilingHealthText.text = $"{ceilingHp}";
        }

    }
    
    
}
