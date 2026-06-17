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

    [Header("UI")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject stageClearUI;
    [SerializeField] private GameObject gameOverUI;

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

        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        titleUI.SetActive(true);
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
        stageClearUI.SetActive(true);
        SoundManager.Instance.Play(SoundId.StageClear);
        //ui
    }

    public void RequestGameOver()
    {
        timeScaleBefore = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        gameOverUI.SetActive(true);
        SoundManager.Instance.Play(SoundId.GameOver);
    }

    public void StartGame()
    {
        Time.timeScale = timeScaleBefore;
        isPaused = false;
        titleUI.SetActive(false);
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

        ResumeGame();
    }

    public void NextStage()
    {
        // 나중에 UI 생기면 여기서 클리어 팝업 닫기
        // uiManager.Hide(UIType.StageClearPopup);

        bool moved = stageManager.TryStartNextStage();

        if (!moved)
        {
            // 마지막 스테이지를 깬 경우
            // 엔딩 UI or 메인 메뉴 이동
            Debug.Log("All stages cleared.");
            return;
        }

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
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = timeScaleBefore;
        isPaused = false;
        pauseUI.SetActive(false);
    }

    public void ToTitle()
    {
        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (stageClearUI != null)
            stageClearUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        titleUI.SetActive(true);
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
