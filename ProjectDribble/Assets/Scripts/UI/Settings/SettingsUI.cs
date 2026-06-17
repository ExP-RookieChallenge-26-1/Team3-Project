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
    }

    public void CloseSettings()
    {
        settingsUI.SetActive(false);
    }
}
