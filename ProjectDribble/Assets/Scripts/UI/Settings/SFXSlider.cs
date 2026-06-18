using UnityEngine;

public class SFXSlider : MonoBehaviour
{
    [SerializeField] private SoundManager SoundManager;
    public void OnSFXSliderValueChanged(float value)
    {
        Debug.Log($"sfx 조절: {value}");
        SoundManager.SetVolume(SoundType.SFX,value);
    }
}
