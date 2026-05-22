using UnityEngine;

public class BallPowerController : MonoBehaviour
{

    private BallMovement ballMovement;

    public BallData data;

    float damageMultiplier;

    void Start()
    {
        ballMovement = GetComponent<BallMovement>();
        damageMultiplier = data.DamageMultiplier;
    }
    
    public int CurrentDamage()
    {
        return Mathf.Max(1,
            Mathf.RoundToInt(ballMovement.speed * damageMultiplier));
    }
}
