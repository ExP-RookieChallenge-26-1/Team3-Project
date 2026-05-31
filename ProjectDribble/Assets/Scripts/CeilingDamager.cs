using UnityEngine;
using Interfaces;
using System.Collections;
public class CeilingDamager : MonoBehaviour, IDamageable
{
    [SerializeField] private float hp = 1;

    public bool TakeDamage(float damage)
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
