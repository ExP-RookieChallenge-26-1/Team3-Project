using DefaultNamespace;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("System References")]
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private LaserUnlockState laserUnlockState;
    [SerializeField] private PaddleController paddleController;

    [Header("UI")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject stageClearUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject gameClearUI;
    [SerializeField] private GameObject pauseButton;

    private bool isPausedByTutorial;
    private float timeScaleBeforeTutorial = 1f;

    private bool isPaused;
    private float timeScaleBefore = 1f;

    public bool IsPausedByTutorial => isPausedByTutorial;
    public bool IsPaused => isPaused;

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

        Initialize();
    }

    public void RequestStageClear()
    {
        int currentStage = stageManager.CurrentStageIndex;

        saveManager.MarkStageCleared(currentStage);
        saveManager.SetLaserUnlocked(laserUnlockState.IsLaserUnlocked);

        // 튜토리얼 여부는 나중 (현재 클리어 여부 변수 없음)
        saveManager.SetTutorialCleared(false);

        saveManager.Save();

        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        if (stageManager.IsValidStageIndex(currentStage + 1))
        {
            stageClearUI.SetActive(true);
            SoundManager.Instance.Play(SoundId.StageClear);
        }
        else
        {
            gameClearUI.SetActive(true);
            SoundManager.Instance.Play(SoundId.StageClear); //아직 게임클리어 사운드 없음 
        }
        pauseButton.SetActive(false);
    }

    public void RequestGameOver()
    {
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        gameOverUI.SetActive(true);
        SoundManager.Instance.Play(SoundId.GameOver);
        pauseButton.SetActive(false);
    }

    public void StartGame()
    {
        int startStageIndex = saveManager.GetStartStageIndex(stageManager.StageCount);

        if (saveManager.Current.laserUnlocked)
            laserUnlockState.UnlockLaser();

        stageManager.StartStage(startStageIndex);

        Time.timeScale = timeScaleBefore;
        isPaused = false;
        titleUI.SetActive(false);
        pauseButton.SetActive(true);
    }

    public void RetryStage()
    {
        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        stageManager.RestartCurrentStage();

        if (paddleController != null)
            paddleController.ResetPosition();

        ResumeGame();
    }

    public void NextStage()
    {
        bool moved = stageManager.TryStartNextStage();

        if (!moved)
        {
            ToTitle();
            return;
        }

        if (paddleController != null)
            paddleController.ResetPosition();

        stageClearUI.SetActive(false);

        ResumeGame();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        pauseUI.SetActive(true);
        pauseButton.SetActive(false);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = timeScaleBefore;
        isPaused = false;
        pauseUI.SetActive(false);
        pauseButton.SetActive(true);
    }

    public void ToTitle()
    {
        Initialize();
    }

    public void Initialize()
    {
        Time.timeScale = 0f;
        isPaused = true;
        isPausedByTutorial = false;
        timeScaleBefore = 1f;
        timeScaleBeforeTutorial = 1f;

        if (stageManager != null)
            stageManager.StartStage(0);

        if (paddleController != null)
            paddleController.ResetPosition();

        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (gameClearUI != null)
            gameClearUI.SetActive(false);

        if (titleUI != null)
            titleUI.SetActive(true);

        if (pauseButton != null)
            pauseButton.SetActive(false);
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
