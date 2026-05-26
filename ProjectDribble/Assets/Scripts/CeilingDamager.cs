using UnityEngine;
using Interfaces;
using System.Collections;
public class CeilingDamager : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 1;

    public bool TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Die();
            return false;
        }
        
        return true;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
    
}
