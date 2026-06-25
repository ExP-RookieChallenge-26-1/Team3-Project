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
    [SerializeField] private GlitchOverlayVisual glitchOverlay;
    [SerializeField] private DamageFlashVisual damageFlashVisual;
    [SerializeField] private SpriteRenderer laserTargetOverlayRenderer;
    [SerializeField, Range(0f, 1f)] private float damagedDarkenAmount = 0.2f;

    private SpriteRenderer sr;
    private Sprite defaultSprite;
    private Color connectedStemColor = Color.white;

    private float hp;
    private float maxHp;

    private BlockType blockType = BlockType.Flow;
    private bool isDisconnectedStem;
    private float danger01;
    private int glitchStage = 1;
    private CeilingSegmentVisualProfile stemVisualProfile;

    public Vector2Int Coord => coord;
    public BlockType BlockType => blockType;
    public bool IsFixed => blockType == BlockType.Fixed;
    public bool IsNormal => blockType == BlockType.Normal;
    public bool IsAlive => gameObject.activeSelf;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            defaultSprite = sr.sprite;
            connectedStemColor = sr.color;
        }

        EnsureLaserTargetOverlay();
    }

    public void Init(BlockManager manager, Vector2Int coord)
    {
        this.manager = manager;
        this.coord = coord;
    }

    public void SetSpriteOverride(Sprite spriteOverride)
    {
        if (sr == null)
            return;

        sr.sprite = spriteOverride != null ? spriteOverride : defaultSprite;

        if (laserTargetOverlayRenderer != null)
            laserTargetOverlayRenderer.sprite = sr.sprite;
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
        glitchStage = 1;
        stemVisualProfile = null;

        gameObject.SetActive(true);

        ResetTransientVisuals();
        UpdateVisual();
    }

    public void Deactivate()
    {
        SetLaserTargetPreview(false, 0f);
        blockType = BlockType.Empty;
        isDisconnectedStem = false;
        danger01 = 0f;
        glitchStage = 1;
        stemVisualProfile = null;
        ResetTransientVisuals();
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
        SetStemVisual(isConnected, danger01, 1);
    }

    public void SetStemVisual(bool isConnected, float danger01, int glitchStage)
    {
        SetStemVisual(isConnected, danger01, glitchStage, null);
    }

    public void SetStemVisual(
        bool isConnected,
        float danger01,
        int glitchStage,
        CeilingSegmentVisualProfile profile
    )
    {
        isDisconnectedStem = !isConnected;
        this.danger01 = Mathf.Clamp01(danger01);
        this.glitchStage = Mathf.Clamp(glitchStage, 1, 3);
        stemVisualProfile = profile;
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
        damageFlashVisual?.PlayFlash();

        if (hp <= 0)
        {

            if (addGauge)
                manager.AddGauge();

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayBlockBreakFeedback();
            else
                SoundManager.Instance?.Play(SoundId.BlockBreak);
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

        Color color = GetUndamagedStemColor();
        color = Color.Lerp(color, dangerStemColor, danger01);

        sr.color = color;
        RefreshDamageTint();

        bool showGlitch = blockType == BlockType.Flow;
        glitchOverlay?.SetState(
            showGlitch,
            !isDisconnectedStem,
            danger01,
            glitchStage,
            stemVisualProfile
        );
    }

    public void SetLaserTargetPreview(bool active, float alpha)
    {
        EnsureLaserTargetOverlay();
        if (laserTargetOverlayRenderer == null)
            return;

        laserTargetOverlayRenderer.sprite = sr != null ? sr.sprite : laserTargetOverlayRenderer.sprite;
        laserTargetOverlayRenderer.enabled = active && IsAlive;
        Color color = laserTargetOverlayRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        laserTargetOverlayRenderer.color = color;
    }

    private void EnsureLaserTargetOverlay()
    {
        if (laserTargetOverlayRenderer != null || sr == null)
            return;

        GameObject overlayObject = new GameObject("LaserTargetOverlay");
        overlayObject.transform.SetParent(transform, false);
        laserTargetOverlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        laserTargetOverlayRenderer.sprite = sr.sprite;
        laserTargetOverlayRenderer.material = sr.sharedMaterial;
        laserTargetOverlayRenderer.sortingLayerID = sr.sortingLayerID;
        laserTargetOverlayRenderer.sortingOrder = sr.sortingOrder + 1;
        laserTargetOverlayRenderer.color = new Color(1f, 0.08f, 0.04f, 0f);
        laserTargetOverlayRenderer.enabled = false;
    }

    private void UpdateHpVisual()
    {
        if (crackOverlayRenderer == null)
            return;

        crackOverlayRenderer.gameObject.SetActive(false);
    }

    private Color GetUndamagedStemColor()
    {
        return isDisconnectedStem ? disconnectedStemColor : connectedStemColor;
    }

    private void RefreshDamageTint()
    {
        if (sr == null)
            return;

        float healthRatio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 1f;
        float darken = Mathf.Lerp(damagedDarkenAmount, 0f, healthRatio);
        float multiplier = 1f - darken;

        Color color = sr.color;
        color.r *= multiplier;
        color.g *= multiplier;
        color.b *= multiplier;
        sr.color = color;
    }

    private void ResetDamageTint()
    {
        if (sr == null)
            return;

        Color color = GetUndamagedStemColor();
        color.a = connectedStemColor.a;
        sr.color = color;
    }

    private void ResetTransientVisuals()
    {
        glitchOverlay?.ResetVisual();
        damageFlashVisual?.ResetVisual();
        ResetDamageTint();
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
