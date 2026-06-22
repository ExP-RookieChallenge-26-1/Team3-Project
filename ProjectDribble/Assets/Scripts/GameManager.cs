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
    [SerializeField] private GameObject playTestUI;
    [SerializeField] private GameObject pauseButton;

    private bool isPausedByTutorial;
    private float timeScaleBeforeTutorial = 1f;

    private bool isPaused;
    private float timeScaleBefore = 1f;
    private bool isGameStarted;

    public bool IsPausedByTutorial => isPausedByTutorial;
    public bool IsPaused => isPaused;
    public bool IsGameStarted => isGameStarted;
    public bool IsRecallTutorialActive { get; private set; }

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

    private void Start()
    {
        if (!isGameStarted)
            SoundManager.Instance?.PlayTitleBgm();
    }

    public void RequestStageClear()
    {
        int currentStage = stageManager.CurrentStageIndex;

        saveManager.MarkStageCleared(currentStage);
        saveManager.SetLaserUnlocked(laserUnlockState.IsLaserUnlocked);

        if (stageManager.IsCurrentStageTutorial)
            saveManager.SetTutorialCleared(true);

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
            SoundManager.Instance.Play(SoundId.StageClear); // 아직 게임클리어 사운드 없음
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
        StartGameAtStage(startStageIndex);
    }

    public void StartGameAtStage(int startStageIndex)
    {
        if (saveManager.Current.laserUnlocked)
            laserUnlockState.UnlockLaser();

        Time.timeScale = timeScaleBefore;
        isPaused = false;
        isGameStarted = true;
        SoundManager.Instance?.ClearBgmMuffles();
        SoundManager.Instance?.PlayGameplayBgm();
        playTestUI.SetActive(false);
        titleUI.SetActive(false);
        pauseButton.SetActive(true);

        stageManager.StartStage(startStageIndex);
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
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Pause, true);
        pauseUI.SetActive(true);
        pauseButton.SetActive(false);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = timeScaleBefore;
        isPaused = false;
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Pause, false);
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
        isGameStarted = false;
        isPausedByTutorial = false;
        IsRecallTutorialActive = false;
        timeScaleBefore = 1f;
        timeScaleBeforeTutorial = 1f;
        SoundManager.Instance?.ClearBgmMuffles();
        SoundManager.Instance?.PlayTitleBgm();

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

        if (playTestUI != null)
            playTestUI.SetActive(false);

        if (titleUI != null)
            titleUI.SetActive(true);

        if (pauseButton != null)
            pauseButton.SetActive(false);
    }

    public void PauseForTutorial()
    {
        if (isPausedByTutorial)
        {
            Debug.Log($"[Tutorial] PauseForTutorial skipped: already paused. Time.timeScale={Time.timeScale}");
            return;
        }

        timeScaleBeforeTutorial = Time.timeScale;
        Time.timeScale = 0f;
        isPausedByTutorial = true;
        Debug.Log(
            $"[Tutorial] PauseForTutorial called. " +
            $"previousTimeScale={timeScaleBeforeTutorial}, Time.timeScale={Time.timeScale}");
    }

    public void BeginRecallTutorial()
    {
        if (IsRecallTutorialActive)
            return;

        IsRecallTutorialActive = true;
        PauseForTutorial();
    }

    public void EndRecallTutorial()
    {
        if (!IsRecallTutorialActive)
            return;

        IsRecallTutorialActive = false;
        ResumeFromTutorial();
    }

    public void ResumeFromTutorial()
    {
        if (!isPausedByTutorial)
        {
            Debug.Log($"[Tutorial] ResumeFromTutorial skipped: tutorial is not paused. Time.timeScale={Time.timeScale}");
            return;
        }

        Time.timeScale = timeScaleBeforeTutorial;
        isPausedByTutorial = false;
        Debug.Log($"[Tutorial] ResumeFromTutorial called. Time.timeScale={Time.timeScale}");
    }
}
