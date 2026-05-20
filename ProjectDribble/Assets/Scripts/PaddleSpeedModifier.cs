using Interfaces;
using UnityEngine;

public class PaddleSpeedModifier : MonoBehaviour, IBallSpeedModifier
{
    [SerializeField] private float speedIncrease = 5f;

    public void ModifySpeed(BallSpeedController speedController)
    {
        speedController.AddSpeed(speedIncrease);
    }
}