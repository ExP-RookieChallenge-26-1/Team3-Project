using UnityEngine;
using UnityEngine.UI;
public class Gauge : MonoBehaviour
{
    public Slider slider;
    public void SetGuage(float value)
    {
        slider.value = value;
    }
}
