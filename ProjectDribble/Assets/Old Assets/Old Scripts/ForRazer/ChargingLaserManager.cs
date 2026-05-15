using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class ChargingLaserManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

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
        shooter = GetComponent<LaserShooter>();
    }


    public void CheckBounceCount()
    {
        bounceCount++;
        Debug.Log("팅");
        if (bounceCount % PerBounceCount == 0 && gauge.filledGaugeSegments!=0)
        {
            ChargingbyBounce(bounceCount/PerBounceCount);
            
        }
    }

    
    private void ChargingbyBounce(int bounceUnit)
    {
        charging = true;
        gauge.ChangeGaugeLevel(-1);

        chargeCount = bounceUnit;
        
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ShootLaser();
        }
    }

    private void ShootLaser()
    {
        if (chargeCount > 0)
        {
            shooter.Shoot(chargeCount);
            Reset();
        }
    }
    
    
    public void Reset()
    {
        
        bounceCount = 0;
        chargeCount = 0;
    }
    
    
}
