using System;
using UnityEngine;

public class ChargingLaserManager : MonoBehaviour
{
    [SerializeField] private ScriptableObjectScripts.LaserGaugeData laserGaugeData;
    [SerializeField] private BallSound ballSound;

    [SerializeField] private GaugeManager gauge;
    private LaserShooter shooter;
    //private bool charging = false;
    public bool charging = false;
    
    private int bounceCount = 0;
    //private int chargeCount = 0;
    public int chargeCount = 0;
    [SerializeField]private int PerBounceCount = 10;

    private void Start()
    {
        PerBounceCount = laserGaugeData.perBounceCount;
        shooter = GetComponent<LaserShooter>();
    }


    public void CheckBounceCount()
    {
        bounceCount++;
        Debug.Log("팅");
        if (bounceCount == PerBounceCount)
        {
            if(TryCharging())
            {
                bounceCount = 0;
            }

        }
    }

    private bool TryCharging()
    {
        if (gauge.TryReduceGaugeLevel())
        {
            charging = true;
            
            chargeCount++;
            float chargeLevel = (float)chargeCount / 3f;
            ballSound.Play(chargeLevel, BallSound.SoundType.Charging);
            return true;
        }

        return false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            ShootLaser();
        }
        ballSound.SetCharging(charging);
    }

    private void ShootLaser()
    {
        if (chargeCount > 0)
        {
            shooter.Shoot(chargeCount);
            charging = false;
            float chargeLevel = (float)chargeCount / 3f;
            ballSound.Play(chargeLevel, BallSound.SoundType.Fire);
            Reset();
        }
    }

    
    
    public void Reset()
    {
        
        bounceCount = 0;
        chargeCount = 0;
    }
    
    
}
