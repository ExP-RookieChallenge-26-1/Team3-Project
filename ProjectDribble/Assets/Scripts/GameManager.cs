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
    [SerializeField] private EndingSequenceController endingSequenceController;

    [Header("UI")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject stageClearUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject gameClearUI;
    [SerializeField] private GameObject playTestUI;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject quitConfirmPopup;
    [SerializeField] private GameObject homeConfirmPopup;
    [SerializeField] private GameObject creditPopup;

    private bool isPausedByTutorial;
    private float timeScaleBeforeTutorial = 1f;

    private bool isPaused;
    private float timeScaleBefore = 1f;
    private bool isGameStarted;
    private bool isStageClearInputBlocked;

    public bool IsPausedByTutorial => isPausedByTutorial;
    public bool IsPaused => isPaused;
    public bool IsGameStarted => isGameStarted;
    public bool IsStageClearInputBlocked => isStageClearInputBlocked;
    public bool IsPlayerInputBlocked => isStageClearInputBlocked;
    public bool IsRecallTutorialActive { get; private set; }
    public bool IsEnding => endingSequenceController != null && endingSequenceController.IsEnding;
    public bool ShouldSuppressBallCollisionFeedback => IsEnding;

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
        if (stageManager == null)
        {
            Debug.LogWarning("GameManager: Cannot clear stage because StageManager is missing.");
            return;
        }

        if (stageManager.IsCurrentStageEnding)
        {
            Debug.LogWarning("GameManager: Stage clear was requested during the ending stage and was ignored.");
            return;
        }

        FeedbackManager.Instance?.StopRecallHoldFeedback();
        FeedbackManager.Instance?.StopLaserChargeFeedback();
        int currentStage = stageManager.CurrentStageIndex;

        if (saveManager != null)
        {
            saveManager.MarkStageCleared(currentStage);

            if (laserUnlockState != null)
                saveManager.SetLaserUnlocked(laserUnlockState.IsLaserUnlocked);

            if (stageManager.IsCurrentStageFinalTutorialStage)
                saveManager.SetTutorialCleared(true);

            saveManager.Save();
        }

        isStageClearInputBlocked = true;

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (gameClearUI != null)
            gameClearUI.SetActive(false);

        if (stageManager.IsCurrentStageLastNormalStage())
        {
            if (gameClearUI != null)
                gameClearUI.SetActive(true);

            SoundManager.Instance?.Play(SoundId.StageClear);
        }
        else
        {
            if (stageClearUI != null)
                stageClearUI.SetActive(true);
            SoundManager.Instance?.Play(SoundId.StageClear);
        }
        if (pauseButton != null)
            pauseButton.SetActive(false);
    }

    public void RequestGameOver()
    {
        if (isStageClearInputBlocked)
            return;

        FeedbackManager.Instance?.StopRecallHoldFeedback();
        FeedbackManager.Instance?.StopLaserChargeFeedback();
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        gameOverUI.SetActive(true);
        SoundManager.Instance.Play(SoundId.GameOver);
        pauseButton.SetActive(false);
    }

    public void StartGame()
    {
        if (saveManager == null || stageManager == null)
        {
            Debug.LogWarning("GameManager: Cannot start game because SaveManager or StageManager is missing.");
            return;
        }

        int startStageIndex = saveManager.GetStartStageIndex(stageManager.StageCount);
        stageManager.TryResolvePlayableStartStageIndex(startStageIndex, out startStageIndex);
        StartGameAtStage(startStageIndex);
    }

    public void StartGameAtStage(int startStageIndex)
    {
        if (saveManager != null && saveManager.Current.laserUnlocked && laserUnlockState != null)
            laserUnlockState.UnlockLaser();

        Time.timeScale = timeScaleBefore;
        isPaused = false;
        isGameStarted = true;
        SoundManager.Instance?.ClearBgmMuffles();
        SoundManager.Instance?.PlayGameplayBgm(true);
        playTestUI.SetActive(false);
        titleUI.SetActive(false);
        pauseButton.SetActive(true);
        isStageClearInputBlocked = false;

        stageManager.StartStage(startStageIndex);
    }

    public void StartEndingStage()
    {
        CloseConfirmPopups();

        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (gameClearUI != null)
            gameClearUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        Time.timeScale = 1f;
        timeScaleBefore = 1f;
        isPaused = false;
        isGameStarted = true;
        isStageClearInputBlocked = false;

        if (titleUI != null)
            titleUI.SetActive(false);

        if (playTestUI != null)
            playTestUI.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(true);

        if (stageManager == null || !stageManager.TryStartEndingStage())
        {
            Debug.LogWarning("GameManager: Cannot start ending stage.");
            return;
        }

        if (paddleController != null)
            paddleController.ResetPosition();
    }

    public void RetryStage()
    {
        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (gameClearUI != null)
            gameClearUI.SetActive(false);

        // Restore gameplay before stage initialization so a tutorial popup can
        // capture the correct pre-popup time scale and pause the retried stage.
        ResumeGame();
        stageManager.RestartCurrentStage();
        SoundManager.Instance?.PlayGameplayBgm(true);
        isStageClearInputBlocked = false;

        if (paddleController != null)
            paddleController.ResetPosition();

        if (pauseButton != null)
            pauseButton.SetActive(true);
    }

    public void NextStage()
    {
        // Stage initialization may immediately open a tutorial popup. Resume the
        // stage-clear pause first so that popup remains the final pause owner.
        ResumeGame();
        bool moved = stageManager.TryStartNextStage();

        if (!moved)
        {
            isStageClearInputBlocked = false;
            ToTitle();
            return;
        }

        SoundManager.Instance?.PlayGameplayBgm(true);
        isStageClearInputBlocked = false;

        if (paddleController != null)
            paddleController.ResetPosition();

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(true);
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        FeedbackManager.Instance?.StopRecallHoldFeedback();
        FeedbackManager.Instance?.StopLaserChargeFeedback();
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
        ShowHomeConfirmPopup();
    }

    public void OnClickHomeButton()
    {
        ShowHomeConfirmPopup();
    }

    public void ConfirmGoHome()
    {
        HideHomeConfirmPopup();
        GoHomeInternal();
    }

    public void CancelGoHome()
    {
        HideHomeConfirmPopup();
    }

    public void ShowHomeConfirmPopup()
    {
        HideQuitConfirmPopup();

        if (homeConfirmPopup != null)
            homeConfirmPopup.SetActive(true);
    }

    public void HideHomeConfirmPopup()
    {
        if (homeConfirmPopup != null)
            homeConfirmPopup.SetActive(false);
    }

    public void OnClickQuitButton()
    {
        ShowQuitConfirmPopup();
    }

    public void ShowQuitConfirmPopup()
    {
        HideHomeConfirmPopup();

        if (quitConfirmPopup != null)
            quitConfirmPopup.SetActive(true);
    }

    public void HideQuitConfirmPopup()
    {
        if (quitConfirmPopup != null)
            quitConfirmPopup.SetActive(false);
    }

    public void ShowCreditPopup()
    {
        if (creditPopup != null)
            creditPopup.SetActive(true);
    }

    public void HideCreditPopup()
    {
        if (creditPopup != null)
            creditPopup.SetActive(false);
    }

    public void ConfirmQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CancelQuitGame()
    {
        HideQuitConfirmPopup();
    }

    private void GoHomeInternal()
    {
        Initialize();
    }

    public void GoHomeFromEnding()
    {
        Time.timeScale = 1f;
        endingSequenceController?.EndEndingAndReset();
        GoHomeInternal();
        Time.timeScale = 1f;
    }

    public void Initialize()
    {
        CloseConfirmPopups();
        HideCreditPopup();
        FeedbackManager.Instance?.StopRecallHoldFeedback();
        FeedbackManager.Instance?.StopLaserChargeFeedback();
        Time.timeScale = 0f;
        isPaused = true;
        isGameStarted = false;
        isPausedByTutorial = false;
        isStageClearInputBlocked = false;
        IsRecallTutorialActive = false;
        timeScaleBefore = 1f;
        timeScaleBeforeTutorial = 1f;
        SoundManager.Instance?.ClearBgmMuffles();
        SoundManager.Instance?.PlayTitleBgm();
        endingSequenceController?.EndEndingAndReset();

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

    private void CloseConfirmPopups()
    {
        HideQuitConfirmPopup();
        HideHomeConfirmPopup();
    }

    public void PauseForTutorial()
    {
        if (isPausedByTutorial)
        {
            Debug.Log($"[Tutorial] PauseForTutorial skipped: already paused. Time.timeScale={Time.timeScale}");
            return;
        }

        FeedbackManager.Instance?.StopRecallHoldFeedback();
        FeedbackManager.Instance?.StopLaserChargeFeedback();
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
