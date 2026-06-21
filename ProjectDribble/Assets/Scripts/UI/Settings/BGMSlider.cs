using UnityEngine;
using UnityEngine.UI;

public class BGMSlider : MonoBehaviour
{
    [SerializeField] private SoundManager SoundManager;
    public void OnBGMSliderValueChanged(float value)
    {
        Debug.Log($"bgm 조절: {value}");
        SoundManager.SetBgmVolume(NormalizeSliderValue(value));
    }

    private float NormalizeSliderValue(float value)
    {
        return Mathf.Clamp01(value > 1f ? value / 100f : value);
    }
}
