using UnityEngine;
using UnityEngine.UI;
public class Gauge : MonoBehavior
{
    public Slider slider;
    public void SetGuage(float value)
    {
        slider.value = value;
    }
}
