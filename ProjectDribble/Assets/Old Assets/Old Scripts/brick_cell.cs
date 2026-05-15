using UnityEngine;

public class BrickCell : MonoBehaviour, IBallHitReceiver
{
    private BrickManager manager;
    private Vector2Int cell;
    private bool isFixed;
    private int version;
    
    private GaugeManager gaugeManager;
    
    private void Awake()
    {
        if (gaugeManager == null)
        {
            gaugeManager = FindFirstObjectByType<GaugeManager>();
        }
    }
    public void Init(
        BrickManager manager,
        Vector2Int cell,
        bool isFixed,
        int version
    )
    {
        this.manager = manager;
        this.cell = cell;
        this.isFixed = isFixed;
        this.version = version;
    }

    public void OnBallHit()
    {
        if (isFixed)
            return;
        gaugeManager.AddGauge();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.NotifyBrickDestroyed(cell, isFixed, version);
           
        }
        
    }
    public void DestroybyLaser()
    {
        Destroy(gameObject);
        
    }
    
    
    public bool IsFixedBrick()
    {
        return isFixed;
    }

    //unity ���� ��� �� (circlecast ��Ŀ� �ʿ� x)
    /*private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball"))
            return;

        BallController ball = collision.gameObject.GetComponent<BallController>();

        if (ball != null)
           //ball.DecreaseByBlockHit();

        if (isFixed)
            return;

        Destroy(gameObject);
    }*/
}