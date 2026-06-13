using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    private bool pausedByTutorial;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        Hide();
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
    }

    public void ShowMessage(string message, bool pauseGame)
    {
        if (messageText != null)
            messageText.text = message;

        if (messageRoot != null)
            messageRoot.SetActive(true);

        if (pauseGame && !pausedByTutorial)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedByTutorial = true;
        }
    }

    public void Hide()
    {
        if (messageRoot != null)
            messageRoot.SetActive(false);

        RestoreTimeScaleIfNeeded();
    }

    public void Continue()
    {
        Hide();
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!pausedByTutorial)
            return;

        Time.timeScale = previousTimeScale;
        pausedByTutorial = false;
    }
}
