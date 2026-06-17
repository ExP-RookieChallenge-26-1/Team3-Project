using UnityEngine;

public class SFXSlider : MonoBehaviour
{
    [SerializeField] private SoundManager SoundManager;
    public void OnSFXSliderValueChanged(float value)
    {
        
        SoundManager.SetVolume(SoundType.SFX,value);
    }
}
