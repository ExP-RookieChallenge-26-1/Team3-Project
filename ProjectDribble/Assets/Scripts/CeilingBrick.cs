using UnityEngine;
using Interfaces;

public class CeilingBrick : MonoBehaviour, IDamageable
{
    private CeilingManager manager;

    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Init(CeilingManager manager, Sprite sprite)
    {
        this.manager = manager;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
    }

    public bool TakeDamage(int damage)
    {
        manager.TakeDamage(damage, this);
        return false;
    }

    public void Break()
    {
        gameObject.SetActive(false);
    }
}