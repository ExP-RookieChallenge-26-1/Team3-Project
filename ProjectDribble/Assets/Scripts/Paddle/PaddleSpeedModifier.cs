using Interfaces;
using UnityEngine;

public class PaddleSpeedModifier : MonoBehaviour, IBallSpeedModifier
{
    public BallData data;

    float speedIncrease;

    void Start()
    {
        if (gameObject.name == "roof_paddle")
            speedIncrease = data.outerPaddleSpeedIncrease;
        else
        {
            speedIncrease = data.innerPaddleSpeedIncrease;
        }
    }

    public void ModifySpeed(BallSpeedController speedController)
    {
        speedController.AddSpeedByPaddle(speedIncrease);
    }
}
