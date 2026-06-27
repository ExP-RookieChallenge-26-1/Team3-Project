using UnityEngine;
using UnityEngine.UI;

public sealed class SFXSlider : MonoBehaviour
{
    private const string SfxVolumeKey = "Settings.SFXVolume";

    [SerializeField] private Slider slider;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        float savedValue = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        slider.SetValueWithoutNotify(savedValue);
        SoundManager.Instance?.SetSfxVolume(savedValue);
    }

    public void OnValueChanged(float value)
    {
        float normalized = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(SfxVolumeKey, normalized);
        PlayerPrefs.Save();

        SoundManager.Instance?.SetSfxVolume(normalized);
    }
}
