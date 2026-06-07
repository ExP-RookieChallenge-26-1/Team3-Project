using DefaultNamespace;
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
    [SerializeField] private SpriteRenderer crackOverlayRenderer;
    [SerializeField] private float minCrackAlpha = 0.25f;
    [SerializeField] private float maxCrackAlpha = 0.85f;
    [SerializeField] private Color disconnectedStemColor = new Color(0.45f, 0.5f, 0.42f, 1f);

    private SpriteRenderer sr;
    private Color connectedStemColor = Color.white;

    private float hp;
    private float maxHp;

    private bool isFixed;
    private bool isDisconnectedStem;

    public Vector2Int Coord => coord;
    public bool IsFixed => isFixed;
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
        this.coord = coord;

        this.hp = hp;
        this.maxHp = hp;

        this.isFixed = isFixed;
        isDisconnectedStem = false;

        gameObject.SetActive(true);

        UpdateVisual();
    }

    public void Deactivate()
    {
        isDisconnectedStem = false;
        UpdateVisual();
        gameObject.SetActive(false);
    }

    public void SetStemConnection(bool isConnected)
    {
        isDisconnectedStem = !isConnected;
        UpdateStemConnectionVisual();
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
        color.a = 1f;

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
