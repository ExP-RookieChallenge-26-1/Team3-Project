using UnityEngine;

public class SFXSlider : MonoBehaviour
{
    [SerializeField] private SoundManager SoundManager;
    public void OnSFXSliderValueChanged(float value)
    {
        Debug.Log($"sfx 조절: {value}");
        SoundManager.SetSfxVolume(NormalizeSliderValue(value));
    }

    private float NormalizeSliderValue(float value)
    {
        return Mathf.Clamp01(value > 1f ? value / 100f : value);
    }
}
