using UnityEngine;
using UnityEngine.UI;

public class BGMSlider : MonoBehaviour
{
    [SerializeField] private SoundManager SoundManager;
    public void OnBGMSliderValueChanged(float value)
    {
        
        SoundManager.SetVolume(SoundType.BGM,value);
    }
}
