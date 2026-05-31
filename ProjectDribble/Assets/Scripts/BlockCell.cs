using Interfaces;
using UnityEngine;

public class BlockCell : MonoBehaviour,
    IBallHitReceiver,
    ILaserHittable,
    IDamageable,
    IBallSpeedModifier
{
    private BlockManager manager;
    private Vector2Int coord;

    [SerializeField] private GaugeManager _gaugeManager;
    [SerializeField] private float ballSpeedDecrease = 2f;

    [Header("Visual")]
    [SerializeField] private float minAlpha = 0.2f;

    private SpriteRenderer sr;

    private float hp;
    private float maxHp;

    private bool isFixed;

    public Vector2Int Coord => coord;
    public bool IsFixed => isFixed;
    public bool IsAlive => gameObject.activeSelf;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(BlockManager manager, Vector2Int coord)
    {
        this.manager = manager;
        this.coord = coord;
    }

    public void Activate(Vector2Int coord, float hp, bool isFixed)
    {
        this.coord = coord;

        this.hp = hp;
        this.maxHp = hp;

        this.isFixed = isFixed;

        gameObject.SetActive(true);

        UpdateVisual();
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void ModifySpeed(BallSpeedController speedController)
    {
        speedController.ApplyBlockSlow(ballSpeedDecrease);
    }
    
    public void OnBallHit(BallController ball)
    {
        
        // 일단 비워둬도 됨.
        // 반사는 BallCollisionHandler에서 기본 반사 처리함.
    }

    public bool OnLaserHit()
    {
        manager.RemoveBlock(coord, true);
        return isFixed;
    }

    public bool TakeDamage(float damage)
    {
        if (isFixed)
            return false;

        hp -= damage;

        UpdateVisual();

        if (hp <= 0)
        {

            manager.AddGauge();
            manager.RemoveBlock(coord);
            
            if (hp>= - 0.3f)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private void UpdateVisual()
    {
        if (sr == null)
            return;

        float hpPercent = (float)hp / maxHp;

        float alpha = Mathf.Lerp(minAlpha, 1f, hpPercent);

        Color color = sr.color;
        color.a = alpha;

        sr.color = color;
    }
}
