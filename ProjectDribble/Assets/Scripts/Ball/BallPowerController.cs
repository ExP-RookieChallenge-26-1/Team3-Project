using UnityEngine;

public class BallPowerController : MonoBehaviour
{
    [SerializeField] private float damageMultiplier = 0.2f;
    private BallMovement ballMovement;

    void Start()
    {
        ballMovement = GetComponent<BallMovement>();
    }
    
    public int CurrentDamage()
    {
        return Mathf.Max(1,
            Mathf.RoundToInt(ballMovement.speed * damageMultiplier));
    }
}
