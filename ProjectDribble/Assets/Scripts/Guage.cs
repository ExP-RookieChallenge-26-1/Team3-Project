using UnityEngine;
using UnityEngine.UI;
public class Gauge : MonoBehaviour
{
    [SerializeField]
    private Slider slider;
    public void SetGuage(float value)
    {
        if slider.value != null
        {
            slider.value = value;
        }
    }
}
