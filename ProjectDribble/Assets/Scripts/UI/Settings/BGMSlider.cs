using UnityEngine;
using UnityEngine.UI;

public sealed class BGMSlider : MonoBehaviour
{
    private const string BgmVolumeKey = "Settings.BGMVolume";

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

        float savedValue = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));

        slider.SetValueWithoutNotify(savedValue);
        SoundManager.Instance?.SetBgmVolume(savedValue);
    }

    public void OnValueChanged(float value)
    {
        float normalized = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(BgmVolumeKey, normalized);
        PlayerPrefs.Save();

        SoundManager.Instance?.SetBgmVolume(normalized);
    }
}
