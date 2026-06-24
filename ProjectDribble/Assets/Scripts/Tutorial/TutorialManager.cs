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
    [SerializeField] private GameObject tutorialTopBoundary;

    [Header("Legacy Dynamic Message")]
    [Tooltip("Kept for callers that still use TryShowTutorial. Step 1-8 popups always pause.")]
    [SerializeField] private bool pauseOnMessage;

    private readonly HashSet<TutorialId> shownTutorials = new();

    private TutorialStageId currentTutorialStageId = TutorialStageId.None;
    private TutorialPhase currentPhase = TutorialPhase.None;
    private float stage1PlayElapsed;
    private bool hasShownStep2;
    private bool hasShownStep3;
    private bool hasShownStep5;
    private bool hasShownStep7;
    private bool hasShownStep8;
    private bool isRunningPendingStageClear;
    private Action pendingStageClearCallback;

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
    }

    private void Update()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1 ||
            currentPhase != TutorialPhase.Stage1BreakNormalBlocks ||
            hasShownStep2)
        {
            return;
        }

        stage1PlayElapsed += Time.deltaTime;

        if (stage1PlayElapsed >= AimGuideForceDelay)
            ShowStage1AimGuide();
    }

    private void OnDisable()
    {
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

        if (uiManager != null)
            uiManager.HideTutorialPopupWithoutCallback();

        if (gameManager != null && gameManager.IsPausedByTutorial)
            gameManager.ResumeFromTutorial();

        gaugeManager?.SetGaugeGainWhileLaserLockedEnabled(false);
        ceilingManager?.SetCorePulseUseUnscaledTime(false);
        SetTutorialTopBoundaryActive(false);

        shownTutorials.Clear();
        currentTutorialStageId = TutorialStageId.None;
        currentPhase = TutorialPhase.None;
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
