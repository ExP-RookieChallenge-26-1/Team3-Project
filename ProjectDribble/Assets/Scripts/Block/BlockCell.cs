using DefaultNamespace;
using Interfaces;
using UnityEngine;

public enum BlockType
{
    Empty,
    Flow,
    Normal,
    Fixed
}

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
    [SerializeField] private SpriteRenderer crackOverlayRenderer;
    [SerializeField] private float minCrackAlpha = 0.25f;
    [SerializeField] private float maxCrackAlpha = 0.85f;
    [SerializeField] private Color disconnectedStemColor = new Color(0.45f, 0.5f, 0.42f, 1f);
    [SerializeField] private Color dangerStemColor = Color.red;

    private SpriteRenderer sr;
    private Color connectedStemColor = Color.white;

    private float hp;
    private float maxHp;

    private BlockType blockType = BlockType.Flow;
    private bool isDisconnectedStem;
    private float danger01;

    public Vector2Int Coord => coord;
    public BlockType BlockType => blockType;
    public bool IsFixed => blockType == BlockType.Fixed;
    public bool IsNormal => blockType == BlockType.Normal;
    public bool IsAlive => gameObject.activeSelf;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            connectedStemColor = sr.color;
    }

    public void Init(BlockManager manager, Vector2Int coord)
    {
        this.manager = manager;
        this.coord = coord;
    }

    public void Activate(Vector2Int coord, float hp, bool isFixed)
    {
        Activate(coord, hp, isFixed ? BlockType.Fixed : BlockType.Flow);
    }

    public void Activate(Vector2Int coord, float hp, BlockType blockType)
    {
        this.coord = coord;

        this.hp = hp;
        this.maxHp = hp;

        this.blockType = blockType;
        isDisconnectedStem = false;
        danger01 = 0f;

        gameObject.SetActive(true);

        UpdateVisual();
    }

    public void Deactivate()
    {
        blockType = BlockType.Empty;
        isDisconnectedStem = false;
        danger01 = 0f;
        UpdateVisual();
        gameObject.SetActive(false);
    }

    public void SetStemConnection(bool isConnected)
    {
        isDisconnectedStem = !isConnected;
        UpdateStemConnectionVisual();
    }

    public void SetDangerVisual(float danger01)
    {
        this.danger01 = Mathf.Clamp01(danger01);
        UpdateStemConnectionVisual();
    }

    public void SetStemVisual(bool isConnected, float danger01)
    {
        isDisconnectedStem = !isConnected;
        this.danger01 = Mathf.Clamp01(danger01);
        UpdateStemConnectionVisual();
    }

    public void ModifySpeed(BallSpeedController speedController)
    {
        speedController.ApplyBlockSlow(ballSpeedDecrease);
    }
    
    public void OnBallHit(BallController ball)
    {
        if (IsFixed)
            manager?.NotifyFixedBlockHitByBall(this);
        
        // 일단 비워둬도 됨.
        // 반사는 BallCollisionHandler에서 기본 반사 처리함.
    }

    public bool OnLaserHit()
    {
        bool wasFixed = IsFixed;

        SoundManager.Instance.Play(
            SoundId.BlockBreak,
            new SoundPlayOptions
            {
                ratio = 0f,
                volumeScale = 0.45f,
                pitchScale = 1.05f
            }
        );
        manager.RemoveBlock(coord, true);

        if (wasFixed)
            manager?.NotifyFixedBlockDestroyedByLaser(this);

        return wasFixed;
    }

    public bool TakeDamage(float damage)
    {
        return TakeDamage(damage, true);
    }

    public bool TakeDamage(float damage, bool addGauge)
    {
        if (IsFixed)
        {
            manager?.NotifyFixedBlockHitByBall(this);
            return false;
        }

        hp -= damage;

        UpdateVisual();

        if (hp <= 0)
        {

            if (addGauge)
                manager.AddGauge();

            SoundManager.Instance.Play(SoundId.BlockBreak);
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
        UpdateStemConnectionVisual();
        UpdateHpVisual();
    }

    private void UpdateStemConnectionVisual()
    {
        if (sr == null)
            return;

        Color color = isDisconnectedStem ? disconnectedStemColor : connectedStemColor;
        color = Color.Lerp(color, dangerStemColor, danger01);

        sr.color = color;
    }

    private void UpdateHpVisual()
    {
        if (crackOverlayRenderer == null)
            return;

        float hpRatio = maxHp <= 0f ? 1f : Mathf.Clamp01(hp / maxHp);
        bool damaged = hpRatio < 1f;

        crackOverlayRenderer.gameObject.SetActive(damaged);

        if (!damaged)
            return;

        Color crackColor = crackOverlayRenderer.color;
        crackColor.a = Mathf.Lerp(minCrackAlpha, maxCrackAlpha, 1f - hpRatio);
        crackOverlayRenderer.color = crackColor;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        Gizmos.color = Color.yellow;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D blockCollider = colliders[i];

            if (blockCollider == null)
                continue;

            Bounds bounds = blockCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
