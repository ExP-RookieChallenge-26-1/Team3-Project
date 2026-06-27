using System;
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
        Stage1Intro,
        Stage1BreakNormalBlocks,
        Stage1RevealCeiling,
        Stage1AttackCeiling,
        Stage2Intro,
        Stage2Playing,
        Stage3Intro,
        Stage3Playing
    }

    private const float AimGuideDetectionDelay = 3f;
    private const float AimGuideForceDelay = 10f;

    [Header("References")]
    [FormerlySerializedAs("tutorialUI")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private LaserUnlockState laserUnlockState;
    [SerializeField] private LaserChargeController laserChargeController;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private BallController ballController;
    [SerializeField] private BallRespawner ballRespawner;
    [SerializeField] private GameObject tutorialTopBoundary;

    [Header("Recall Tutorial")]
    [SerializeField] private bool enableRecallTutorial = true;
    [SerializeField] private float recallWatchMinY = 0f;
    [SerializeField] private float recallWatchMaxY = 18f;
    [Min(0f)]
    [SerializeField] private float recallObserveDuration = 4f;
    [Min(0f)]
    [SerializeField] private float recallMaxYDeviation = 10f;
    [Min(0f)]
    [SerializeField] private float stage3RecallDelay = 5.5f;
    [Min(0f)]
    [SerializeField] private float generalStageRecallDelay = 8f;
    [Min(0f)]
    [SerializeField] private float generalStageRecallObserveDuration = 2.5f;
    [Min(0f)]
    [SerializeField] private float generalStageRecallMaxYDeviation = 14f;
    [SerializeField] private bool saveRecallTutorialSeen = true;

    [Header("Legacy Dynamic Message")]
    [Tooltip("Kept for callers that still use TryShowTutorial. Step 1-8 popups always pause.")]
    [SerializeField] private bool pauseOnMessage;

    private readonly HashSet<TutorialId> shownTutorials = new();

    private StageDefinition currentStageDefinition;
    private TutorialStageId currentTutorialStageId = TutorialStageId.None;
    private TutorialPhase currentPhase = TutorialPhase.None;
    private float stageElapsedTime;
    private float stage3PlayingElapsedTime;
    private float stage1PlayElapsed;
    private bool hasShownStep2;
    private bool hasShownStep3;
    private bool hasShownStep5;
    private bool hasShownStep7;
    private bool hasShownStep8;
    private bool isRunningPendingStageClear;
    private Action pendingStageClearCallback;
    private bool hasRecallObservation;
    private bool recallTutorialSeenThisSession;
    private float recallObservedTime;
    private float recallObservedMinY;
    private float recallObservedMaxY;

    private bool IsRecallTutorialActive =>
        gameManager != null && gameManager.IsRecallTutorialActive;

    private void Awake()
    {
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);

        if (gameManager == null)
            gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindAnyObjectByType<GameManager>();

        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();

        if (ceilingManager == null)
            ceilingManager = FindAnyObjectByType<CeilingManager>();

        if (gaugeManager == null)
            gaugeManager = FindAnyObjectByType<GaugeManager>();

        if (laserUnlockState == null)
            laserUnlockState = FindAnyObjectByType<LaserUnlockState>();

        if (laserChargeController == null)
            laserChargeController = FindAnyObjectByType<LaserChargeController>();

        if (saveManager == null)
            saveManager = SaveManager.Instance != null
                ? SaveManager.Instance
                : FindAnyObjectByType<SaveManager>();

        if (ballController == null)
            ballController = FindAnyObjectByType<BallController>();

        if (ballRespawner == null)
            ballRespawner = FindAnyObjectByType<BallRespawner>();
    }

    private void OnEnable()
    {
        if (ballRespawner == null)
            ballRespawner = FindAnyObjectByType<BallRespawner>();

        if (ballRespawner != null)
            ballRespawner.OnBallRecalled += HandleRecallTutorialBallRecalled;
    }

    private void Update()
    {
        stageElapsedTime += Time.deltaTime;

        if (currentTutorialStageId == TutorialStageId.Stage1 &&
            currentPhase == TutorialPhase.Stage1BreakNormalBlocks &&
            !hasShownStep2)
        {
            stage1PlayElapsed += Time.deltaTime;

            if (stage1PlayElapsed >= AimGuideForceDelay)
                ShowStage1AimGuide();
        }

        UpdateRecallTutorial();
    }

    private void OnDisable()
    {
        if (ballRespawner != null)
            ballRespawner.OnBallRecalled -= HandleRecallTutorialBallRecalled;

        ClearStageRuntimeState();
    }

    private void OnDestroy()
    {
        pendingStageClearCallback = null;
        isRunningPendingStageClear = false;
        UnsubscribeStageEvents();
    }

    public void BeginStage(int stageIndex, StageDefinition stageDefinition)
    {
        ClearStageRuntimeState();
        currentStageDefinition = stageDefinition;
        currentTutorialStageId = ResolveTutorialStageId(stageDefinition);

        if (gameManager != null && !gameManager.IsGameStarted)
            return;

        RestoreDefaultCeilingState();

        switch (currentTutorialStageId)
        {
            case TutorialStageId.Stage1:
                BeginStage1();
                break;
            case TutorialStageId.Stage2:
                BeginStage2(stageDefinition);
                break;
            case TutorialStageId.Stage3:
                BeginStage3();
                break;
        }
    }

    private static TutorialStageId ResolveTutorialStageId(StageDefinition stageDefinition)
    {
        if (stageDefinition == null || !stageDefinition.isTutorialStage)
            return TutorialStageId.None;

        switch (stageDefinition.tutorialStageId)
        {
            case TutorialStageId.Stage1:
            case TutorialStageId.Stage2:
            case TutorialStageId.Stage3:
                return stageDefinition.tutorialStageId;
            default:
                return TutorialStageId.None;
        }
    }

    private void BeginStage1()
    {
        currentPhase = TutorialPhase.Stage1Intro;
        SetTutorialTopBoundaryActive(true);

        laserUnlockState?.LockLaser();
        gaugeManager?.SetGaugeGainWhileLaserLockedEnabled(false);
        blockManager?.StopGrowth();

        if (ceilingManager != null)
        {
            ceilingManager.SetCorePulseUseUnscaledTime(false);
            ceilingManager.SetCeilingVisible(false);
            ceilingManager.SetCeilingCollisionEnabled(false);
            ceilingManager.SetDamageEnabled(false);
        }

        if (blockManager != null)
            blockManager.OnNormalBlocksCleared += HandleStage1NormalBlocksCleared;

        if (ballController != null)
            ballController.OnCaptured += HandleStage1BallCaptured;

        ShowStepPopup(1, BeginStage1BlockPlay);
    }

    private void BeginStage1BlockPlay()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1)
            return;

        currentPhase = TutorialPhase.Stage1BreakNormalBlocks;
        stage1PlayElapsed = 0f;
    }

    private void HandleStage1BallCaptured()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1 ||
            currentPhase != TutorialPhase.Stage1BreakNormalBlocks ||
            hasShownStep2 ||
            stage1PlayElapsed < AimGuideDetectionDelay)
        {
            return;
        }

        ShowStage1AimGuide();
    }

    private void ShowStage1AimGuide()
    {
        if (hasShownStep2)
            return;

        hasShownStep2 = true;
        ShowStepPopup(2);
    }

    private void HandleStage1NormalBlocksCleared()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1 ||
            currentPhase != TutorialPhase.Stage1BreakNormalBlocks ||
            hasShownStep3)
        {
            return;
        }

        hasShownStep3 = true;
        currentPhase = TutorialPhase.Stage1RevealCeiling;
        SetTutorialTopBoundaryActive(false);

        if (ceilingManager != null)
        {
            ceilingManager.SetCeilingVisible(true);
            ceilingManager.SetCeilingCollisionEnabled(true);
            ceilingManager.SetDamageEnabled(false);
            ceilingManager.SetAllAliveCoreVisualsConnected(true);
            ceilingManager.SetCorePulseUseUnscaledTime(true);
        }

        ShowStepPopup(3, BeginStage1CeilingAttack);
    }

    private void BeginStage1CeilingAttack()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1)
            return;

        if (ceilingManager != null)
        {
            ceilingManager.SetCorePulseUseUnscaledTime(false);
            ceilingManager.SetDamageEnabled(true);
        }

        currentPhase = TutorialPhase.Stage1AttackCeiling;
    }

    private void BeginStage2(StageDefinition stageDefinition)
    {
        currentPhase = TutorialPhase.Stage2Intro;
        laserUnlockState?.LockLaser();

        if (gaugeManager != null)
        {
            gaugeManager.SetGaugeGainWhileLaserLockedEnabled(true);
            gaugeManager.OnGaugeValueChanged += HandleStage2GaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged += HandleStage2GaugeSegmentChanged;
            gaugeManager.InitializeGauge(
                stageDefinition != null ? stageDefinition.startGaugeValue : 0,
                true);
        }

        blockManager?.StopGrowth();

        ShowStepPopup(4, BeginStage2Play);
    }

    private void BeginStage2Play()
    {
        if (currentTutorialStageId != TutorialStageId.Stage2)
            return;

        blockManager?.StartGrowth();
        currentPhase = TutorialPhase.Stage2Playing;
        TryShowStage2LaserUnlock();
    }

    private void HandleStage2GaugeValueChanged(int value)
    {
        TryShowStage2LaserUnlock();
    }

    private void HandleStage2GaugeSegmentChanged(int filledSegments)
    {
        TryShowStage2LaserUnlock();
    }

    private void TryShowStage2LaserUnlock()
    {
        if (currentTutorialStageId != TutorialStageId.Stage2 ||
            currentPhase != TutorialPhase.Stage2Playing ||
            hasShownStep5 ||
            !IsGaugeFull())
        {
            return;
        }

        hasShownStep5 = true;
        ShowStepPopup(5, CompleteStage2LaserUnlock);
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

    private void CompleteStage2LaserUnlock()
    {
        if (currentTutorialStageId != TutorialStageId.Stage2)
            return;

        laserUnlockState?.UnlockLaser();
        gaugeManager?.SetGaugeGainWhileLaserLockedEnabled(false);

        if (saveManager != null)
        {
            saveManager.SetLaserUnlocked(true);
            saveManager.Save();
        }
    }

    private void BeginStage3()
    {
        currentPhase = TutorialPhase.Stage3Intro;
        blockManager?.StopGrowth();

        if (ceilingManager != null)
            ceilingManager.OnCeilingSegmentDestroyed += HandleStage3CeilingSegmentDestroyed;

        ShowStepPopup(6, BeginStage3Play);
    }

    private void BeginStage3Play()
    {
        if (currentTutorialStageId != TutorialStageId.Stage3)
            return;

        blockManager?.StartGrowth();
        currentPhase = TutorialPhase.Stage3Playing;
        stage3PlayingElapsedTime = 0f;
    }

    private void HandleStage3CeilingSegmentDestroyed(CeilingSegment segment)
    {
        if (currentTutorialStageId != TutorialStageId.Stage3 || hasShownStep7)
            return;

        // CeilingManager disables the destroyed segment's stem before raising this event.
        hasShownStep7 = true;
        ShowStepPopup(7);
    }

    public bool TryInterceptStageClear(
        StageDefinition stageDefinition,
        Action continueStageClear)
    {
        if (ResolveTutorialStageId(stageDefinition) != TutorialStageId.Stage3 ||
            currentTutorialStageId != TutorialStageId.Stage3)
        {
            return false;
        }

        if (pendingStageClearCallback != null || hasShownStep8 || isRunningPendingStageClear)
            return true;

        if (continueStageClear == null)
        {
            Debug.LogWarning("[Tutorial] Stage 3 clear interception was skipped: callback is missing.");
            return false;
        }

        pendingStageClearCallback = continueStageClear;
        hasShownStep8 = true;

        if (uiManager != null && uiManager.IsTutorialPopupOpen)
            uiManager.HideTutorialPopupWithoutCallback();

        ShowStepPopup(8, CompletePendingStageClear, true);
        return true;
    }

    private void CompletePendingStageClear()
    {
        if (isRunningPendingStageClear || pendingStageClearCallback == null)
            return;

        Action callback = pendingStageClearCallback;
        pendingStageClearCallback = null;
        isRunningPendingStageClear = true;

        try
        {
            callback.Invoke();
        }
        finally
        {
            isRunningPendingStageClear = false;
        }
    }

    private bool ShowStepPopup(
        int stepNumber,
        Action onConfirmed = null,
        bool resumeBeforeCallback = false)
    {
        if (uiManager == null)
        {
            Debug.LogWarning($"[Tutorial] Step {stepNumber} was skipped: UIManager is missing.");
            if (resumeBeforeCallback)
                gameManager?.ResumeFromTutorial();
            onConfirmed?.Invoke();
            return false;
        }

        if (uiManager.IsTutorialPopupOpen)
        {
            Debug.LogWarning($"[Tutorial] Step {stepNumber} was skipped: another tutorial popup is open.");
            return false;
        }

        laserChargeController?.CancelChargeAndRefund();

        bool shown = uiManager.ShowTutorialStepPopup(stepNumber, () =>
        {
            if (resumeBeforeCallback)
            {
                gameManager?.ResumeFromTutorial();
                onConfirmed?.Invoke();
                return;
            }

            try
            {
                onConfirmed?.Invoke();
            }
            finally
            {
                gameManager?.ResumeFromTutorial();
            }
        });

        if (!shown)
        {
            if (resumeBeforeCallback)
                gameManager?.ResumeFromTutorial();
            onConfirmed?.Invoke();
            return false;
        }

        gameManager?.PauseForTutorial();
        return true;
    }

    private void ClearStageRuntimeState()
    {
        UnsubscribeStageEvents();
        ResetRecallObservation();

        if (uiManager != null)
            uiManager.HideTutorialPopupWithoutCallback();

        if (IsRecallTutorialActive)
            gameManager.EndRecallTutorial();
        else if (gameManager != null && gameManager.IsPausedByTutorial)
            gameManager.ResumeFromTutorial();

        gaugeManager?.SetGaugeGainWhileLaserLockedEnabled(false);
        ceilingManager?.SetCorePulseUseUnscaledTime(false);
        SetTutorialTopBoundaryActive(false);

        shownTutorials.Clear();
        currentStageDefinition = null;
        currentTutorialStageId = TutorialStageId.None;
        currentPhase = TutorialPhase.None;
        stageElapsedTime = 0f;
        stage3PlayingElapsedTime = 0f;
        stage1PlayElapsed = 0f;
        hasShownStep2 = false;
        hasShownStep3 = false;
        hasShownStep5 = false;
        hasShownStep7 = false;
        hasShownStep8 = false;
        isRunningPendingStageClear = false;
        pendingStageClearCallback = null;
    }

    private void UnsubscribeStageEvents()
    {
        if (blockManager != null)
            blockManager.OnNormalBlocksCleared -= HandleStage1NormalBlocksCleared;

        if (ballController != null)
            ballController.OnCaptured -= HandleStage1BallCaptured;

        if (ceilingManager != null)
            ceilingManager.OnCeilingSegmentDestroyed -= HandleStage3CeilingSegmentDestroyed;

        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged -= HandleStage2GaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged -= HandleStage2GaugeSegmentChanged;
        }
    }

    private void RestoreDefaultCeilingState()
    {
        if (ceilingManager == null)
            return;

        ceilingManager.SetCeilingVisible(true);
        ceilingManager.SetCeilingCollisionEnabled(true);
        ceilingManager.SetDamageEnabled(true);
        ceilingManager.SetCorePulseUseUnscaledTime(false);
    }

    private void SetTutorialTopBoundaryActive(bool active)
    {
        if (tutorialTopBoundary != null)
            tutorialTopBoundary.SetActive(active);
    }

    private void UpdateRecallTutorial()
    {
        if (!CanShowRecallTutorialNow())
        {
            ResetRecallObservation();
            return;
        }

        if (TryShowStage3RecallTutorial())
            return;

        if (!CanObserveGeneralStageRecallTutorial())
        {
            ResetRecallObservation();
            return;
        }

        float currentY = ballController.transform.position.y;

        if (currentY < recallWatchMinY || currentY > recallWatchMaxY)
        {
            ResetRecallObservation();
            return;
        }

        float observeDuration = generalStageRecallObserveDuration > 0f
            ? generalStageRecallObserveDuration
            : recallObserveDuration;
        float maxYDeviation = Mathf.Max(recallMaxYDeviation, generalStageRecallMaxYDeviation);

        ObserveRecallTutorialY(currentY, observeDuration, maxYDeviation);
    }

    private bool CanShowRecallTutorialNow()
    {
        if (!enableRecallTutorial ||
            HasSeenRecallTutorial() ||
            IsRecallTutorialActive)
        {
            return false;
        }

        if (ballController == null || ballController.IsCaptured)
            return false;

        if (gameManager == null ||
            !gameManager.IsGameStarted ||
            gameManager.IsPaused ||
            gameManager.IsPausedByTutorial ||
            gameManager.IsStageClearInputBlocked ||
            gameManager.IsEnding)
        {
            return false;
        }

        if (!Mathf.Approximately(Time.timeScale, 1f))
            return false;

        return uiManager == null || !uiManager.IsTutorialPopupOpen;
    }

    private bool TryShowStage3RecallTutorial()
    {
        if (currentTutorialStageId != TutorialStageId.Stage3 ||
            currentPhase != TutorialPhase.Stage3Playing)
        {
            return false;
        }

        stage3PlayingElapsedTime += Time.deltaTime;

        if (stage3PlayingElapsedTime < stage3RecallDelay)
            return false;

        TryBeginRecallTutorial();
        return true;
    }

    private bool CanObserveGeneralStageRecallTutorial()
    {
        if (currentStageDefinition == null ||
            currentStageDefinition.StageType != StageType.Normal ||
            currentStageDefinition.isTutorialStage)
        {
            return false;
        }

        return stageElapsedTime >= generalStageRecallDelay;
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

    private void ObserveRecallTutorialY(
        float currentY,
        float requiredDuration,
        float maxYDeviation)
    {
        if (!hasRecallObservation)
        {
            StartRecallObservation(currentY);
            return;
        }

        recallObservedMinY = Mathf.Min(recallObservedMinY, currentY);
        recallObservedMaxY = Mathf.Max(recallObservedMaxY, currentY);

        if (recallObservedMaxY - recallObservedMinY > maxYDeviation)
        {
            StartRecallObservation(currentY);
            return;
        }

        recallObservedTime += Time.deltaTime;

        if (recallObservedTime >= requiredDuration)
            TryBeginRecallTutorial();
    }

    private void TryBeginRecallTutorial()
    {
        ResetRecallObservation();

        if (uiManager == null || gameManager == null)
        {
            Debug.LogWarning("[Tutorial] Recall popup was skipped: UIManager or GameManager is missing.");
            return;
        }

        bool shown = uiManager.ShowRecallTutorialPopup(CompleteRecallTutorial);

        if (!shown)
            return;

        gameManager.BeginRecallTutorial();
    }

    private void HandleRecallTutorialBallRecalled()
    {
        if (!IsRecallTutorialActive)
            return;

        uiManager?.HideTutorialPopup();
    }

    private void CompleteRecallTutorial()
    {
        recallTutorialSeenThisSession = true;

        if (saveRecallTutorialSeen && saveManager != null)
        {
            saveManager.SetRecallTutorialSeen(true);
            saveManager.Save();
        }

        if (IsRecallTutorialActive)
            gameManager.EndRecallTutorial();

        ResetRecallObservation();
    }

    private void StartRecallObservation(float y)
    {
        hasRecallObservation = true;
        recallObservedTime = 0f;
        recallObservedMinY = y;
        recallObservedMaxY = y;
    }

    private void ResetRecallObservation()
    {
        hasRecallObservation = false;
        recallObservedTime = 0f;
        recallObservedMinY = 0f;
        recallObservedMaxY = 0f;
    }

    // Kept for TutorialTrigger and older scenes. Stage 4-6 flows are inactive in MainScene.
    public void NotifyTriggerEntered(TutorialStageId tutorialStageId, string triggerId)
    {
    }

    public bool TryShowTutorial(TutorialId id)
    {
        if (id == TutorialId.None || HasShown(id))
            return false;

        string message = GetLegacyTutorialMessage(id);
        if (string.IsNullOrWhiteSpace(message) || uiManager == null)
            return false;

        Action onClose = pauseOnMessage ? ResumeLegacyTutorial : null;
        bool shown = uiManager.ShowTutorialPopup(message, onClose);

        if (!shown)
            return false;

        MarkShown(id);

        if (pauseOnMessage)
            gameManager?.PauseForTutorial();

        return true;
    }

    public void MarkShown(TutorialId id)
    {
        if (id != TutorialId.None)
            shownTutorials.Add(id);
    }

    public bool HasShown(TutorialId id)
    {
        return shownTutorials.Contains(id);
    }

    private void ResumeLegacyTutorial()
    {
        gameManager?.ResumeFromTutorial();
    }

    private static string GetLegacyTutorialMessage(TutorialId id)
    {
        switch (id)
        {
            case TutorialId.Stage1Start:
                return "기본 조작 설명\n블록을 모두 부수세요!";
            case TutorialId.Stage1Ceiling:
                return "천장을 부수세요!";
            case TutorialId.Stage2Start:
                return "흐름 블록 설명\n천장을 부수세요!";
            case TutorialId.Stage3Start:
                return "천장이 2개로 나뉘었습니다.";
            case TutorialId.Stage3SegmentDestroyed:
                return "천장을 부수면 연결된 줄기의 성장이 멈춥니다.";
            default:
                return string.Empty;
        }
    }
}
