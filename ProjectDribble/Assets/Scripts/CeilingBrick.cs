using UnityEngine;
using Interfaces;

public class CeilingBrick : MonoBehaviour, IDamageable
{
    private CeilingManager manager;
    private Vector2Int coord;

    [SerializeField] private SpriteRenderer spriteRenderer;

    public Vector2Int Coord => coord;

    public void Init(CeilingManager manager, Sprite sprite)
    {
        Init(manager, Vector2Int.zero, sprite);
    }

    public void Init(CeilingManager manager, Vector2Int coord, Sprite sprite)
    {
        this.manager = manager;
        this.coord = coord;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
    }

    public bool TakeDamage(float damage)
    {
        if (manager == null)
            return false;

        manager.TakeDamage(damage, this);
        return manager.IsSegmentDestroyedByX(coord.x);
    }

    public void Break()
    {
        gameObject.SetActive(false);
    }
}
