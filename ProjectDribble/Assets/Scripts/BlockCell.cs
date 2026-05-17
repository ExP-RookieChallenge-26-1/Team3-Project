using UnityEngine;

public class BlockCell : MonoBehaviour , IBallHitReceiver , ILaserHittable , IDamageable
{
    private BlockManager manager;
    private Vector2Int coord;

    private int hp;
    private bool isFixed;

    public Vector2Int Coord => coord;
    public bool IsFixed => isFixed;
    public bool IsAlive => gameObject.activeSelf;

    public void Init(BlockManager manager, Vector2Int coord)
    {
        this.manager = manager;
        this.coord = coord;
    }

    public void Activate(Vector2Int coord, int hp, bool isFixed)
    {
        this.coord = coord;
        this.hp = hp;
        this.isFixed = isFixed;

        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void OnBallHit(/* Ball ball */)
    {
        //ReflectDefault
    }

    public void OnLaserHit()
    {
        manager.RemoveBlock(coord);
    }

    public bool TakeDamage(int damage)
    {
        if (isFixed)
            return false;

        hp -= damage;

        if (hp <= 0)
        {
            manager.RemoveBlock(coord);
            return true;
        }
        return false;
    }
}