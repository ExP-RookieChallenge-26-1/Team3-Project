using UnityEngine;

public class RecallTutorialManager : MonoBehaviour
{
    private const string RecallGuideMessage =
        "공이 막혔을 때는 아래로 길게 드래그해 공을 끌어올 수 있습니다.";

    [Header("Settings")]
    [SerializeField] private bool enableRecallTutorial = true;
    [SerializeField] private float watchMinY = 0f;
    [SerializeField] private float watchMaxY = 16f;
    [Min(0f)]
    [SerializeField] private float observeDuration = 5f;
    [Min(0f)]
    [SerializeField] private float maxYDeviation = 8f;
    [SerializeField] private bool saveRecallTutorialSeen = true;

    [Header("References")]
    [SerializeField] private BallController ballController;
    [SerializeField] private BallRespawner ballRespawner;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SaveManager saveManager;

    private bool hasObservation;
    private bool recallTutorialSeenThisSession;
    private float observedTime;
    private float observedMinY;
    private float observedMaxY;

    private bool IsRecallTutorialActive =>
        gameManager != null && gameManager.IsRecallTutorialActive;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (ballRespawner != null)
            ballRespawner.OnBallRecalled += HandleBallRecalled;
    }

    private void OnDisable()
    {
        if (ballRespawner != null)
            ballRespawner.OnBallRecalled -= HandleBallRecalled;

        if (IsRecallTutorialActive)
        {
            uiManager?.HideTutorialPopup();
            gameManager.EndRecallTutorial();
        }

        ResetObservation();
    }

    private void Update()
    {
        if (!CanObserveBall())
        {
            ResetObservation();
            return;
        }

        float currentY = ballController.transform.position.y;

        if (currentY < watchMinY || currentY > watchMaxY)
        {
            ResetObservation();
            return;
        }

        ObserveY(currentY);
    }

    private void ObserveY(float currentY)
    {
        if (!hasObservation)
        {
            StartObservation(currentY);
            return;
        }

        observedMinY = Mathf.Min(observedMinY, currentY);
        observedMaxY = Mathf.Max(observedMaxY, currentY);

        if (observedMaxY - observedMinY > maxYDeviation)
        {
            StartObservation(currentY);
            return;
        }

        observedTime += Time.deltaTime;

        if (observedTime >= observeDuration)
            TryBeginRecallTutorial();
    }

    private bool CanObserveBall()
    {
        if (!enableRecallTutorial || HasSeenRecallTutorial() || IsRecallTutorialActive)
            return false;

        if (ballController == null || ballController.IsCaptured)
            return false;

        if (gameManager == null ||
            !gameManager.IsGameStarted ||
            gameManager.IsPaused ||
            gameManager.IsPausedByTutorial)
        {
            return false;
        }

        if (!Mathf.Approximately(Time.timeScale, 1f))
            return false;

        return uiManager == null || !uiManager.IsTutorialPopupOpen;
    }

    private bool HasSeenRecallTutorial()
    {
        if (recallTutorialSeenThisSession)
            return true;

        return saveRecallTutorialSeen &&
               saveManager != null &&
               saveManager.Current != null &&
               saveManager.Current.recallTutorialSeen;
    }

    private void TryBeginRecallTutorial()
    {
        ResetObservation();

        if (uiManager == null || gameManager == null)
        {
            Debug.LogWarning("[RecallTutorial] Required UIManager or GameManager reference is missing.");
            return;
        }

        bool shown = uiManager.ShowTutorialPopup(
            RecallGuideMessage,
            null,
            TutorialPopupCloseMode.ExternalOnly);

        if (!shown)
            return;

        gameManager.BeginRecallTutorial();
    }

    private void HandleBallRecalled()
    {
        if (!IsRecallTutorialActive)
            return;

        uiManager?.HideTutorialPopup();
        recallTutorialSeenThisSession = true;

        if (saveRecallTutorialSeen && saveManager != null)
        {
            saveManager.SetRecallTutorialSeen(true);
            saveManager.Save();
        }

        gameManager.EndRecallTutorial();
        ResetObservation();
    }

    private void StartObservation(float y)
    {
        hasObservation = true;
        observedTime = 0f;
        observedMinY = y;
        observedMaxY = y;
    }

    private void ResetObservation()
    {
        hasObservation = false;
        observedTime = 0f;
        observedMinY = 0f;
        observedMaxY = 0f;
    }

    private void ResolveReferences()
    {
        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();

        if (ballRespawner == null)
            ballRespawner = FindAnyObjectByType<BallRespawner>();

        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();

        if (gameManager == null)
            gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindAnyObjectByType<GameManager>();

        if (saveManager == null)
            saveManager = SaveManager.Instance != null
                ? SaveManager.Instance
                : FindAnyObjectByType<SaveManager>();
    }
}
