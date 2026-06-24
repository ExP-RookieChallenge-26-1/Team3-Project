using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private Button vibrationToggleButton;
    [SerializeField] private Sprite vibrationEnabledSpriteRenderer;
    [SerializeField] private Sprite vibrationDisabledSpriteRenderer;

    void Start()
    {
        if (vibrationToggleButton == null && settingsUI != null)
        {
            Transform toggleTransform = settingsUI.transform.Find("VibrationToggle");
            vibrationToggleButton = toggleTransform != null
                ? toggleTransform.GetComponent<Button>()
                : null;
        }

        if (vibrationToggleButton != null)
        {
            vibrationToggleButton.onClick.AddListener(ToggleVibration);
            RefreshVibrationToggleVisual();
        }
    }

    private void OnDestroy()
    {
        if (vibrationToggleButton != null)
            vibrationToggleButton.onClick.RemoveListener(ToggleVibration);
    }

    public void ToggleVibration()
    {
        if (VibrationManager.Instance == null)
            return;

        VibrationManager.Instance.SetVibrationEnabled(
            !VibrationManager.Instance.VibrationEnabled);
        RefreshVibrationToggleVisual();
        FeedbackManager.Instance?.PlayUIButtonFeedback();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrationManager.Instance?.SetVibrationEnabled(enabled);
        RefreshVibrationToggleVisual();
    }

    private void RefreshVibrationToggleVisual()
    {
        if (vibrationToggleButton == null || VibrationManager.Instance == null)
            return;

        Image image = vibrationToggleButton.targetGraphic as Image;
        if (image != null)
        {
            image.sprite = VibrationManager.Instance.VibrationEnabled
                ? vibrationEnabledSpriteRenderer
                : vibrationDisabledSpriteRenderer;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenSettings()
    {
        settingsUI.SetActive(true);
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Settings, true);
    }

    public void CloseSettings()
    {
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Settings, false);
        settingsUI.SetActive(false);
    }

    private void OnDisable()
    {
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Settings, false);
    }
}
