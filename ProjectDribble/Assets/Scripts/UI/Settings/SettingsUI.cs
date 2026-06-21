using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsUI;

    void Start()
    {
        
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
