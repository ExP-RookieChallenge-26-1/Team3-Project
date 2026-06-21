using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum TutorialId
{
    None,
    Stage1Start,
    Stage1Ceiling,
    Stage2Start,
    Stage3Start,
    Stage3SegmentDestroyed,
    Stage4Start,
    Stage4GaugeFull,
    Stage4LaserFired,
    Stage5Start,
    Stage5FixedHit,
    Stage5LaserGuide,
    Stage5FixedDestroyed,
    Stage6RecallGuide,
    Stage6Recalled
}

public class TutorialManager : MonoBehaviour
{
    private enum TutorialPhase
    {
        None,
        Intro,
        BreakNormalBlocks,
        RevealCeiling,
        AttackCeiling,
        Completed
    }

    private const string IntroMessage = "공을 튕겨 블록을 모두 부수세요.";
    private const string DribbleGuideMessage =
        "공이 패들 사이에 들어오면 잠시 붙잡을 수 있습니다.\n" +
        "붙잡은 동안 조준하고 레이저를 차징할 수 있습니다.";
    private const string RevealCeilingMessage =
        "위에 나타난 천장을 노리세요.\n" +
        "초록 흐름 블록은 아래로 자라나며, 바닥에 닿으면 위험합니다.";
    private const string CeilingSegmentGuideMessage =
        "천장을 부수면 연결된 흐름 블록의 성장이 멈춥니다.";
    private const float DribbleGuideDetectionDelay = 3f;
    private const float DribbleGuideForceDelay = 10f;

    private const string Stage1StartMessage =
        "기본 조작 설명 \n블록을 모두 부수세요!";
    private const string Stage1CeilingMessage = 
        "천장을 부수세요!";
    private const string Stage2StartMessage =
        "흐름 블록 설명\n 천장을 부수세요!";
    private const string Stage3StartMessage =
        "천장이 2개로 나눠졌음";
    private const string Stage3SegmentDestroyedMessage =
        "천장을 부수면 줄기가 멈춘다는 설명";
    private const string Stage4StartMessage = "블록을 부수면 게이지가 찹니다.";
    private const string Stage4GaugeFullMessage =
        "게이지가 가득 찼습니다. 공을 잡고 차징한 뒤 레이저를 발사해보세요.";
    private const string Stage4LaserFiredMessage =
        "레이저는 여러 블록을 한 번에 부술 수 있습니다.";
    private const string Stage5StartMessage = "고정 블록은 공으로 부서지지 않습니다.";
    private const string Stage5FixedHitMessage =
        "고정 블록은 공으로는 부술 수 없습니다.";
    private const string Stage5LaserGuideMessage =
        "레이저를 사용하면 고정 블록을 부술 수 있습니다.";
    private const string Stage5FixedDestroyedMessage = "고정 블록을 제거했습니다.";
    private const string Stage6RecallGuideMessage =
        "공이 멀리 있거나 갇혔을 때는 공을 다시 불러올 수 있습니다.\n리스폰 버튼을 눌러 공을 불러오세요.";
    private const string Stage6RecalledMessage = 
        "공을 다시 불러왔습니다.";

    [Header("References")]
    [FormerlySerializedAs("tutorialUI")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private LaserShooter laserShooter;
    [SerializeField] private BallRespawner ballRespawner;
    [SerializeField] private LaserUnlockState laserUnlockState;
    [SerializeField] private BallController ballController;
    [SerializeField] private GameObject tutorialTopBoundary;

    [Header("Message")]
    [SerializeField] private bool pauseOnMessage;

    private readonly HashSet<TutorialId> shownTutorials = new();
    private TutorialStageId currentTutorialStageId = TutorialStageId.None;
    private bool isSubscribedToBlockEvents;
    private bool isSubscribedToCeilingEvents;
    private bool isSubscribedToGaugeEvents;
    private bool isSubscribedToLaserEvents;
    private bool isSubscribedToBallRespawnerEvents;
    private bool isSubscribedToBallEvents;
    private TutorialPhase currentPhase = TutorialPhase.None;
    private bool hasShownDribbleGuide;
    private bool hasShownCeilingSegmentGuide;
    private float breakNormalBlocksElapsed;
    private bool stage4GaugeFullMessageShown;
    private bool stage4LaserFiredMessageShown;
    private bool stage5FixedHitMessageShown;
    private bool stage5LaserGuideMessageShown;
    private bool stage5FixedDestroyedMessageShown;
    private bool stage6RecallGuideMessageShown;
    private bool stage6RecalledMessageShown;
    private Coroutine pendingMessageRoutine;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();

        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();

        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();

        if (ceilingManager == null)
            ceilingManager = FindAnyObjectByType<CeilingManager>();

        if (gaugeManager == null)
            gaugeManager = FindAnyObjectByType<GaugeManager>();

        if (laserShooter == null)
            laserShooter = FindAnyObjectByType<LaserShooter>();

        if (ballRespawner == null)
            ballRespawner = FindAnyObjectByType<BallRespawner>();

        if (laserUnlockState == null)
            laserUnlockState = FindAnyObjectByType<LaserUnlockState>();

        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();
    }

    private void Update()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1 ||
            currentPhase != TutorialPhase.BreakNormalBlocks ||
            hasShownDribbleGuide)
        {
            return;
        }

        breakNormalBlocksElapsed += Time.deltaTime;

        if (breakNormalBlocksElapsed >= DribbleGuideForceDelay)
            ShowDribbleGuide();
    }

    private void OnDisable()
    {
        ClearStageSubscriptions();
    }

    public void BeginStage(int stageIndex, StageDefinition stageDefinition)
    {
        ClearStageSubscriptions();

        if (ceilingManager != null)
            ceilingManager.SetDamageEnabled(true);

        currentTutorialStageId = ResolveTutorialStageId(stageIndex, stageDefinition);

        if (gameManager != null && !gameManager.IsGameStarted)
        {
            HideMessage();
            return;
        }

        if (ceilingManager != null)
        {
            ceilingManager.SetCeilingVisible(true);
            ceilingManager.SetCeilingCollisionEnabled(true);
            ceilingManager.SetDamageEnabled(true);
        }

        switch (currentTutorialStageId)
        {
            case TutorialStageId.Stage1:
                BeginStage1();
                break;
            case TutorialStageId.Stage2:
                TryShowTutorial(TutorialId.Stage2Start);
                break;
            case TutorialStageId.Stage3:
                BeginStage3();
                break;
            case TutorialStageId.Stage4:
                BeginStage4(stageDefinition);
                break;
            case TutorialStageId.Stage5:
                BeginStage5();
                break;
            case TutorialStageId.Stage6:
                BeginStage6();
                break;
            default:
                HideMessage();
                break;
        }
    }

    private TutorialStageId ResolveTutorialStageId(int stageIndex, StageDefinition stageDefinition)
    {
        if (stageDefinition == null || !stageDefinition.isTutorialStage)
            return TutorialStageId.None;

        if (stageDefinition.tutorialStageId != TutorialStageId.None)
            return stageDefinition.tutorialStageId;

        switch (stageIndex)
        {
            case 0:
                return TutorialStageId.Stage1;
            case 1:
                return TutorialStageId.Stage2;
            case 2:
                return TutorialStageId.Stage3;
            default:
                return TutorialStageId.None;
        }
    }

    private void BeginStage1()
    {
        currentPhase = TutorialPhase.Intro;
        SetTutorialTopBoundaryActive(true);
        hasShownDribbleGuide = false;
        hasShownCeilingSegmentGuide = false;
        breakNormalBlocksElapsed = 0f;

        if (ceilingManager != null)
        {
            ceilingManager.SetCeilingVisible(false);
            ceilingManager.SetCeilingCollisionEnabled(false);
            ceilingManager.SetDamageEnabled(false);
            ceilingManager.OnCeilingSegmentDestroyed += HandleCeilingSegmentDestroyed;
            ceilingManager.OnStageCleared += HandleTutorialStageCleared;
            isSubscribedToCeilingEvents = true;
        }

        if (blockManager != null)
        {
            blockManager.StopGrowth();
            blockManager.OnNormalBlocksCleared += HandleNormalBlocksCleared;
            isSubscribedToBlockEvents = true;
        }

        if (ballController != null)
        {
            ballController.OnCaptured += HandleBallCaptured;
            isSubscribedToBallEvents = true;
        }

        ShowPausedTutorialMessage(IntroMessage, BeginBreakNormalBlocksPhase);
    }

    private void BeginBreakNormalBlocksPhase()
    {
        currentPhase = TutorialPhase.BreakNormalBlocks;
        breakNormalBlocksElapsed = 0f;
    }

    private void HandleBallCaptured()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1 ||
            currentPhase != TutorialPhase.BreakNormalBlocks ||
            hasShownDribbleGuide ||
            breakNormalBlocksElapsed < DribbleGuideDetectionDelay ||
            breakNormalBlocksElapsed > DribbleGuideForceDelay)
        {
            return;
        }

        ShowDribbleGuide();
    }

    private void ShowDribbleGuide()
    {
        if (hasShownDribbleGuide)
            return;

        hasShownDribbleGuide = true;
        ShowPausedTutorialMessage(DribbleGuideMessage);
    }

    private void BeginStage3()
    {
        TryShowTutorial(TutorialId.Stage3Start);

        if (ceilingManager == null)
            return;

        ceilingManager.OnCeilingSegmentDestroyed += HandleCeilingSegmentDestroyed;
        isSubscribedToCeilingEvents = true;
    }

    private void BeginStage4(StageDefinition stageDefinition)
    {
        if (laserUnlockState != null)
            laserUnlockState.UnlockLaser();

        if (gaugeManager != null && stageDefinition != null)
            gaugeManager.InitializeGauge(stageDefinition.startGaugeValue);

        TryShowTutorial(TutorialId.Stage4Start);

        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged += HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged += HandleGaugeSegmentChanged;
            isSubscribedToGaugeEvents = true;
        }

        if (laserShooter != null)
        {
            laserShooter.OnLaserFired += HandleLaserFired;
            isSubscribedToLaserEvents = true;
        }

        if (IsGaugeFull())
            ShowTutorialAfterCurrentCloses(TutorialId.Stage4GaugeFull, MarkStage4GaugeFullMessageShown);
    }

    private void BeginStage5()
    {
        TryShowTutorial(TutorialId.Stage5Start);

        if (blockManager == null)
            return;

        blockManager.OnFixedBlockHitByBall += HandleFixedBlockHitByBall;
        blockManager.OnFixedBlockDestroyedByLaser += HandleFixedBlockDestroyedByLaser;
        isSubscribedToBlockEvents = true;
    }

    private void BeginStage6()
    {
        if (ballRespawner == null)
            return;

        ballRespawner.OnBallRecalled += HandleBallRecalled;
        isSubscribedToBallRespawnerEvents = true;
    }

    private void HandleNormalBlocksCleared()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1 ||
            currentPhase != TutorialPhase.BreakNormalBlocks)
            return;

        currentPhase = TutorialPhase.RevealCeiling;
        SetTutorialTopBoundaryActive(false);

        if (ceilingManager != null)
        {
            ceilingManager.SetCeilingVisible(true);
            ceilingManager.SetCeilingCollisionEnabled(true);
            ceilingManager.SetDamageEnabled(false);
        }

        ShowPausedTutorialMessage(RevealCeilingMessage, BeginAttackCeilingPhase);
    }

    private void BeginAttackCeilingPhase()
    {
        SetTutorialTopBoundaryActive(false);

        if (ceilingManager != null)
            ceilingManager.SetDamageEnabled(true);

        if (blockManager != null)
            blockManager.StartGrowth();

        currentPhase = TutorialPhase.AttackCeiling;
    }

    private void HandleCeilingSegmentDestroyed(CeilingSegment segment)
    {
        if (currentTutorialStageId == TutorialStageId.Stage1)
        {
            if (currentPhase != TutorialPhase.AttackCeiling || hasShownCeilingSegmentGuide)
                return;

            hasShownCeilingSegmentGuide = true;
            ShowPausedTutorialMessage(CeilingSegmentGuideMessage);
            return;
        }

        if (currentTutorialStageId != TutorialStageId.Stage3)
            return;

        TryShowTutorial(TutorialId.Stage3SegmentDestroyed);
    }

    private void HandleTutorialStageCleared()
    {
        if (currentTutorialStageId == TutorialStageId.Stage1)
        {
            currentPhase = TutorialPhase.Completed;
            SetTutorialTopBoundaryActive(false);
        }
    }

    private void HandleGaugeValueChanged(int value)
    {
        if (currentTutorialStageId != TutorialStageId.Stage4)
            return;

        if (IsGaugeFull())
            HandleGaugeFull();
    }

    private void HandleGaugeSegmentChanged(int filledSegments)
    {
        if (currentTutorialStageId != TutorialStageId.Stage4)
            return;

        if (IsGaugeFull())
            HandleGaugeFull();
    }

    private void HandleGaugeFull()
    {
        if (stage4GaugeFullMessageShown)
            return;

        stage4GaugeFullMessageShown = true;
        TryShowTutorial(TutorialId.Stage4GaugeFull);
    }

    private bool IsGaugeFull()
    {
        if (gaugeManager == null)
            return false;

        if (gaugeManager.MaxGaugeValue > 0)
            return gaugeManager.CurrentGaugeValue >= gaugeManager.MaxGaugeValue;

        return gaugeManager.MaxGaugeSegments > 0 &&
               gaugeManager.FilledGaugeSegments >= gaugeManager.MaxGaugeSegments;
    }

    private void HandleLaserFired()
    {
        if (currentTutorialStageId != TutorialStageId.Stage4)
            return;

        if (stage4LaserFiredMessageShown)
            return;

        stage4LaserFiredMessageShown = true;
        TryShowTutorial(TutorialId.Stage4LaserFired);
    }

    private void HandleFixedBlockHitByBall(BlockCell block)
    {
        if (currentTutorialStageId != TutorialStageId.Stage5)
            return;

        if (!stage5FixedHitMessageShown)
        {
            stage5FixedHitMessageShown = true;
            TryShowTutorial(TutorialId.Stage5FixedHit);
            ShowTutorialAfterCurrentCloses(TutorialId.Stage5LaserGuide, MarkStage5LaserGuideMessageShown);
            return;
        }

        if (!stage5LaserGuideMessageShown)
        {
            stage5LaserGuideMessageShown = true;
            TryShowTutorial(TutorialId.Stage5LaserGuide);
        }
    }

    private void HandleFixedBlockDestroyedByLaser(BlockCell block)
    {
        if (currentTutorialStageId != TutorialStageId.Stage5)
            return;

        if (stage5FixedDestroyedMessageShown)
            return;

        stage5FixedDestroyedMessageShown = true;
        TryShowTutorial(TutorialId.Stage5FixedDestroyed);
    }

    public void NotifyTriggerEntered(TutorialStageId tutorialStageId, string triggerId)
    {
        if (currentTutorialStageId != TutorialStageId.Stage6)
            return;

        if (tutorialStageId != TutorialStageId.None && tutorialStageId != TutorialStageId.Stage6)
            return;

        if (stage6RecallGuideMessageShown)
            return;

        stage6RecallGuideMessageShown = true;
        TryShowTutorial(TutorialId.Stage6RecallGuide, false);
    }

    private void HandleBallRecalled()
    {
        if (currentTutorialStageId != TutorialStageId.Stage6)
            return;

        if (stage6RecalledMessageShown)
            return;

        stage6RecalledMessageShown = true;
        TryShowTutorial(TutorialId.Stage6Recalled);
    }

    private void ClearStageSubscriptions()
    {
        SetTutorialTopBoundaryActive(false);

        if (isSubscribedToBlockEvents && blockManager != null)
        {
            blockManager.OnNormalBlocksCleared -= HandleNormalBlocksCleared;
            blockManager.OnFixedBlockHitByBall -= HandleFixedBlockHitByBall;
            blockManager.OnFixedBlockDestroyedByLaser -= HandleFixedBlockDestroyedByLaser;
        }

        if (isSubscribedToCeilingEvents && ceilingManager != null)
        {
            ceilingManager.OnCeilingSegmentDestroyed -= HandleCeilingSegmentDestroyed;
            ceilingManager.OnStageCleared -= HandleTutorialStageCleared;
        }

        if (isSubscribedToGaugeEvents && gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged -= HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged -= HandleGaugeSegmentChanged;
        }

        if (isSubscribedToLaserEvents && laserShooter != null)
            laserShooter.OnLaserFired -= HandleLaserFired;

        if (isSubscribedToBallRespawnerEvents && ballRespawner != null)
            ballRespawner.OnBallRecalled -= HandleBallRecalled;

        if (isSubscribedToBallEvents && ballController != null)
            ballController.OnCaptured -= HandleBallCaptured;

        if (ceilingManager != null)
            ceilingManager.SetDamageEnabled(true);

        if (pendingMessageRoutine != null)
        {
            StopCoroutine(pendingMessageRoutine);
            pendingMessageRoutine = null;
        }

        isSubscribedToBlockEvents = false;
        isSubscribedToCeilingEvents = false;
        isSubscribedToGaugeEvents = false;
        isSubscribedToLaserEvents = false;
        isSubscribedToBallRespawnerEvents = false;
        isSubscribedToBallEvents = false;
        currentTutorialStageId = TutorialStageId.None;
        currentPhase = TutorialPhase.None;
        ResetStageFlags();
    }

    private void SetTutorialTopBoundaryActive(bool active)
    {
        if (tutorialTopBoundary == null)
            return;

        tutorialTopBoundary.SetActive(active);
    }

    private void ShowPausedTutorialMessage(string message, System.Action onClose = null)
    {
        Debug.Log($"[Tutorial] Popup requested. phase={currentPhase}, message={message}");

        if (uiManager == null)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: UIManager reference is null. Tutorial pause skipped.");
            onClose?.Invoke();
            return;
        }

        bool shown = uiManager.ShowTutorialPopup(message, () =>
        {
            try
            {
                onClose?.Invoke();
            }
            finally
            {
                if (gameManager != null)
                    gameManager.ResumeFromTutorial();
            }
        });

        if (!shown)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown. Tutorial pause skipped.");
            onClose?.Invoke();
            return;
        }

        if (gameManager != null)
            gameManager.PauseForTutorial();
    }

    public bool TryShowTutorial(TutorialId id)
    {
        return TryShowTutorial(id, pauseOnMessage);
    }

    public void MarkShown(TutorialId id)
    {
        if (id == TutorialId.None)
            return;

        shownTutorials.Add(id);
    }

    public bool HasShown(TutorialId id)
    {
        return shownTutorials.Contains(id);
    }

    private bool TryShowTutorial(TutorialId id, bool pauseGame)
    {
        if (id == TutorialId.None)
        {
            Debug.LogWarning("[Tutorial] Popup was not requested: TutorialId is None.");
            return false;
        }

        if (HasShown(id))
        {
            Debug.Log($"[Tutorial] Popup skipped: {id} was already shown.");
            return false;
        }

        string message = GetTutorialMessage(id);
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning($"[Tutorial] Popup was not shown: message is empty for {id}.");
            return false;
        }

        bool shown = ShowMessage(message, pauseGame);
        if (shown)
            MarkShown(id);

        return shown;
    }

    private bool ShowMessage(string message, bool pauseGame)
    {
        Debug.Log($"[Tutorial] Popup requested. phase={currentPhase}, message={message}");

        if (uiManager == null)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: UIManager reference is null. Tutorial pause skipped.");
            return false;
        }

        bool shown = uiManager.ShowTutorialPopup(message, pauseGame ? ResumeTutorial : null);
        if (!shown)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown. Tutorial pause skipped.");
            return false;
        }

        if (pauseGame && gameManager != null)
            gameManager.PauseForTutorial();

        return true;
    }

    private void HideMessage()
    {
        if (uiManager == null)
            return;

        uiManager.HideTutorialPopup();
    }

    private void ResetStageFlags()
    {
        stage4GaugeFullMessageShown = false;
        stage4LaserFiredMessageShown = false;
        stage5FixedHitMessageShown = false;
        stage5LaserGuideMessageShown = false;
        stage5FixedDestroyedMessageShown = false;
        stage6RecallGuideMessageShown = false;
        stage6RecalledMessageShown = false;
    }

    private void ShowTutorialAfterCurrentCloses(TutorialId id, System.Action markShown)
    {
        if (uiManager == null || id == TutorialId.None || HasShown(id))
            return;

        markShown?.Invoke();
        MarkShown(id);

        if (pendingMessageRoutine != null)
            StopCoroutine(pendingMessageRoutine);

        pendingMessageRoutine = StartCoroutine(ShowTutorialAfterCurrentClosesRoutine(id));
    }

    private IEnumerator ShowTutorialAfterCurrentClosesRoutine(TutorialId id)
    {
        while (uiManager != null && uiManager.IsTutorialPopupOpen)
            yield return null;

        ShowMessage(GetTutorialMessage(id), pauseOnMessage);
        pendingMessageRoutine = null;
    }

    private void ResumeTutorial()
    {
        if (gameManager == null)
            return;

        gameManager.ResumeFromTutorial();
    }

    private string GetTutorialMessage(TutorialId id)
    {
        switch (id)
        {
            case TutorialId.Stage1Start:
                return Stage1StartMessage;
            case TutorialId.Stage1Ceiling:
                return Stage1CeilingMessage;
            case TutorialId.Stage2Start:
                return Stage2StartMessage;
            case TutorialId.Stage3Start:
                return Stage3StartMessage;
            case TutorialId.Stage3SegmentDestroyed:
                return Stage3SegmentDestroyedMessage;
            case TutorialId.Stage4Start:
                return Stage4StartMessage;
            case TutorialId.Stage4GaugeFull:
                return Stage4GaugeFullMessage;
            case TutorialId.Stage4LaserFired:
                return Stage4LaserFiredMessage;
            case TutorialId.Stage5Start:
                return Stage5StartMessage;
            case TutorialId.Stage5FixedHit:
                return Stage5FixedHitMessage;
            case TutorialId.Stage5LaserGuide:
                return Stage5LaserGuideMessage;
            case TutorialId.Stage5FixedDestroyed:
                return Stage5FixedDestroyedMessage;
            case TutorialId.Stage6RecallGuide:
                return Stage6RecallGuideMessage;
            case TutorialId.Stage6Recalled:
                return Stage6RecalledMessage;
            default:
                return string.Empty;
        }
    }

    private void MarkStage4GaugeFullMessageShown()
    {
        stage4GaugeFullMessageShown = true;
    }

    private void MarkStage5LaserGuideMessageShown()
    {
        stage5LaserGuideMessageShown = true;
    }
}
