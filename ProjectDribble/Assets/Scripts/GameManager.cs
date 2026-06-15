using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private bool isPausedByTutorial;
    private float timeScaleBeforeTutorial = 1f;

    public bool IsPausedByTutorial => isPausedByTutorial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameManager: more than one GameManager exists. Keeping the first instance.");
            return;
        }

        Instance = this;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
    }

    public void StartGame()
    {
    }

    public void RequestStageClear()
    {
    }

    public void RequestGameOver()
    {
    }

    public void RetryStage()
    {
    }

    public void NextStage()
    {
    }

    public void PauseGame()
    {
    }

    public void ResumeGame()
    {
    }

    public void PauseForTutorial()
    {
        if (isPausedByTutorial)
            return;

        timeScaleBeforeTutorial = Time.timeScale;
        Time.timeScale = 0f;
        isPausedByTutorial = true;
    }

    public void ResumeFromTutorial()
    {
        if (!isPausedByTutorial)
            return;

        Time.timeScale = timeScaleBeforeTutorial;
        isPausedByTutorial = false;
    }
}
