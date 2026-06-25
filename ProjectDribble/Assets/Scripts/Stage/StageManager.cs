using System;
using DefaultNamespace;
using UnityEngine;

public class StageManager : MonoBehaviour 
{
    [Header("Stage Data")]
    [SerializeField] private StageDefinition[] stages;
    [SerializeField] private int titleStageIndex = 0;
    [SerializeField] private int startStageIndex = 1;
    [SerializeField] private int bossStageIndex = -1;

    [Header("System References")]
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private BallSpawnController ballSpawnController;
    [SerializeField] private BallSpeedController ballSpeedController;
    [SerializeField] private BallPowerController ballPowerController;
    [SerializeField] private BossController bossController;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private StageArtManager stageArtManager;
    [SerializeField] private EndingSequenceController endingSequenceController;

    [Header("Top Decoration")]
    [SerializeField] private Transform topDecorationParent;

    private int currentStageIndex;
    private GameObject currentTopDecoration;
    private bool isCompletingStage;

    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages == null ? 0 : stages.Length;
    public bool IsCurrentStageTutorial =>
        IsValidStageIndex(currentStageIndex) &&
        stages[currentStageIndex] != null &&
        stages[currentStageIndex].isTutorialStage;
    public bool IsCurrentStageEnding =>
        CurrentStageDefinition != null &&
        CurrentStageDefinition.StageType == StageType.Ending;
    public bool IsCurrentStageNormal =>
        CurrentStageDefinition != null &&
        CurrentStageDefinition.StageType == StageType.Normal &&
        !CurrentStageDefinition.isTutorialStage &&
        currentStageIndex != titleStageIndex;
    public bool IsCurrentStageFinalTutorialStage =>
        IsCurrentStageTutorial &&
        stages[currentStageIndex].tutorialStageId == TutorialStageId.Stage3;
    private StageDefinition CurrentStageDefinition =>
        IsValidStageIndex(currentStageIndex) ? stages[currentStageIndex] : null;

    private void OnEnable()
    {
        BindStageEvents();
    }

    private void OnDisable()
    {
        UnbindStageEvents();
    }

    private void OnDestroy()
    {
        ClearTopDecoration();
    }

    private void Start()
    {
        if (bossController == null)
            bossController = FindAnyObjectByType<BossController>();

        if (StageCount <= 0)
        {
            Debug.LogWarning("StageManager: stages is empty.");
            return;
        }

        StartStage(titleStageIndex);
    }

    public void StartStage(int stageIndex)
    {
        if (!IsValidStageIndex(stageIndex))
        {
            Debug.LogWarning($"StageManager: invalid stage index {stageIndex}.");
            return;
        }

        isCompletingStage = false;
        currentStageIndex = stageIndex;
        ApplyStageData(stages[currentStageIndex]);
    }

    public void RestartCurrentStage()
    {
        StartStage(currentStageIndex);
    }

    public bool TryStartNextStage()
    {
        int nextIndex = currentStageIndex + 1;

        if (!IsValidStageIndex(nextIndex))
        {
            return false;
        }

        StartStage(nextIndex);
        return true;
    }

    public bool IsCurrentStageLastNormalStage()
    {
        return IsLastNormalStage(currentStageIndex);
    }

    public bool IsLastNormalStage(int index)
    {
        if (!IsValidStageIndex(index) || !IsNormalStageIndex(index))
            return false;

        for (int i = index + 1; i < StageCount; i++)
        {
            if (IsNormalStageIndex(i))
                return false;
        }

        return true;
    }

    public bool TryStartEndingStage()
    {
        int endingStageIndex = FindFirstStageIndex(StageType.Ending);

        if (!IsValidStageIndex(endingStageIndex))
        {
            Debug.LogWarning("StageManager: Ending stage is missing. Add a StageDefinition with StageType.Ending.");
            return false;
        }

        StartStage(endingStageIndex);
        return true;
    }

    public bool TryResolvePlayableStartStageIndex(int requestedIndex, out int resolvedIndex)
    {
        if (IsPlayableStartStageIndex(requestedIndex))
        {
            resolvedIndex = requestedIndex;
            return true;
        }

        if (IsPlayableStartStageIndex(startStageIndex))
        {
            resolvedIndex = startStageIndex;
            return true;
        }

        for (int i = 0; i < StageCount; i++)
        {
            if (!IsPlayableStartStageIndex(i))
                continue;

            resolvedIndex = i;
            return true;
        }

        resolvedIndex = IsValidStageIndex(titleStageIndex) ? titleStageIndex : 0;
        Debug.LogWarning("StageManager: No playable start stage was found. Falling back to title stage.");
        return false;
    }

    private void HandleStageCleared()
    {
        StageDefinition data = CurrentStageDefinition;

        if (data == null || data.clearCondition != StageClearCondition.DestroyCeiling)
            return;

        CompleteCurrentStage();
    }

    private void HandleNormalBlocksCleared()
    {
        StageDefinition data = CurrentStageDefinition;

        if (data == null || data.clearCondition != StageClearCondition.DestroyAllNormalBlocks)
            return;

        CompleteCurrentStage();
    }

    private void CompleteCurrentStage()
    {
        if (isCompletingStage)
            return;

        isCompletingStage = true;

        if (bossController == null)
            bossController = FindAnyObjectByType<BossController>();

        if (bossController != null)
            bossController.StopBossPattern();

        Action continueStageClear = ContinueStageClear;
        if (tutorialManager != null &&
            tutorialManager.TryInterceptStageClear(CurrentStageDefinition, continueStageClear))
        {
            return;
        }

        continueStageClear();
    }

    private void ContinueStageClear()
    {
        if (!isCompletingStage)
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestStageClear();
            return;
        }

        isCompletingStage = false;
        Debug.LogWarning("StageManager: Cannot complete the stage because GameManager is missing.");
    }

    private void HandlePlayerDead()
    {
        GameManager.Instance.RequestGameOver();
    }

    private void BindStageEvents()
    {
        if (ceilingManager != null)
        {
            ceilingManager.OnStageCleared += HandleStageCleared;
        }

        if (blockManager != null)
        {
            blockManager.OnNormalBlocksCleared += HandleNormalBlocksCleared;
        }

        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead += HandlePlayerDead;
        }
    }

    private void UnbindStageEvents()
    {
        if (ceilingManager != null)
        {
            ceilingManager.OnStageCleared -= HandleStageCleared;
        }

        if (blockManager != null)
        {
            blockManager.OnNormalBlocksCleared -= HandleNormalBlocksCleared;
        }

        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead -= HandlePlayerDead;
        }
    }

    private void ApplyStageData(StageDefinition data)
    {
        ApplyTopDecoration(data);

        if (stageArtManager != null)
        {
            if (data == null)
                stageArtManager.ResetToDefault();
            else
                stageArtManager.Apply(data.ArtProfile);
        }

        if (data == null)
        {
            Debug.LogWarning("StageManager: StageDefinition is null.");
            return;
        }

        ApplyBallTuning(data);
        bool isEndingStage = data.StageType == StageType.Ending;

        if (!isEndingStage && data.useCeiling && ceilingManager != null)
        {
            ceilingManager.InitializeCeiling(data.ceilingMaxHpOverride, data.ceilingSegmentMode);
        }
        else if (!isEndingStage && data.useCeiling)
        {
            Debug.LogWarning(
                $"StageManager: Stage '{data.name}' uses a ceiling, but CeilingManager is missing. " +
                "Ceiling clear and ceiling-based stems will be unavailable."
            );
        }
        else if (ceilingManager != null)
        {
            ceilingManager.DisableCeiling();
        }

        if (!data.useCeiling && data.clearCondition == StageClearCondition.DestroyCeiling)
        {
            Debug.LogWarning(
                $"StageManager: Stage '{data.name}' has useCeiling disabled but still uses DestroyCeiling. " +
                "The stage will not clear automatically. Choose DestroyAllNormalBlocks or None."
            );
        }

        if (blockManager != null)
        {
            if (isEndingStage)
            {
                blockManager.ClearStageBlocks();
            }
            else
            {
                Sprite fixedBlockSpriteOverride = data.ArtProfile != null
                    ? data.ArtProfile.FixedBlockSprite
                    : null;

                blockManager.InitializeStageBlocks(
                    data.blockData,
                    data.useCeiling,
                    fixedBlockSpriteOverride
                );
            }
        }
        else
        {
            Debug.LogWarning("StageManager: BlockManager is missing.");
        }

        if (playerHealth != null)
        {
            playerHealth.InitializePlayerHealth(data.playerMaxHpOverride);
        }

        if (!isEndingStage && gaugeManager != null)
        {
            gaugeManager.InitializeGauge(data.startGaugeValue);
        }

        if (ballSpawnController != null)
        {
            ballSpawnController.InitializeBall(
                data.ballStartPosition,
                data.ballStartDirection
            );
        }

        ApplyBossPatternStateForCurrentStage();

        if (isEndingStage)
            endingSequenceController?.BeginEnding();
        else
            endingSequenceController?.EndEndingAndReset();

        if (!isEndingStage && tutorialManager != null)
        {
            tutorialManager.BeginStage(currentStageIndex, data);
        }
    }

    private void ApplyTopDecoration(StageDefinition data)
    {
        ClearTopDecoration();

        if (data == null || !data.UseTopDecoration)
            return;

        GameObject prefab = data.TopDecorationPrefab;

        if (prefab == null)
        {
            Debug.LogWarning(
                $"StageManager: Stage '{data.name}' enables top decoration, but its prefab is missing."
            );
            return;
        }

        Transform parent = topDecorationParent != null ? topDecorationParent : transform;
        currentTopDecoration = Instantiate(prefab, parent);
    }

    private void ClearTopDecoration()
    {
        if (currentTopDecoration == null)
            return;

        currentTopDecoration.SetActive(false);
        Destroy(currentTopDecoration);
        currentTopDecoration = null;
    }

    private void ApplyBallTuning(StageDefinition data)
    {
        if (ballSpeedController == null)
            ballSpeedController = FindAnyObjectByType<BallSpeedController>();

        if (ballPowerController == null)
            ballPowerController = FindAnyObjectByType<BallPowerController>();

        ballSpeedController?.ClearStageTuning();
        ballPowerController?.ClearStageTuning();

        if (!data.overrideBallTuning)
            return;

        ballSpeedController?.ApplyStageTuning(
            data.maxSpeedOverride,
            data.speedGainMultiplierOverride
        );
        ballPowerController?.ApplyStageTuning(
            data.maxDamageOverride,
            data.powerGainMultiplierOverride
        );
    }

    public bool IsValidStageIndex(int index)
    {
        return stages != null && index >= 0 && index < stages.Length;
    }

    private bool IsNormalStageIndex(int index)
    {
        if (!IsValidStageIndex(index))
            return false;

        StageDefinition stage = stages[index];
        return stage != null &&
               stage.StageType == StageType.Normal &&
               !stage.isTutorialStage &&
               index != titleStageIndex;
    }

    private bool IsPlayableStartStageIndex(int index)
    {
        if (!IsValidStageIndex(index))
            return false;

        StageDefinition stage = stages[index];

        if (stage == null)
            return false;

        if (index == titleStageIndex)
            return false;

        return stage.StageType == StageType.Normal ||
               stage.StageType == StageType.Tutorial ||
               stage.isTutorialStage;
    }

    private int FindFirstStageIndex(StageType stageType)
    {
        for (int i = 0; i < StageCount; i++)
        {
            if (stages[i] != null && stages[i].StageType == stageType)
                return i;
        }

        return -1;
    }

    private void ApplyBossPatternStateForCurrentStage()
    {
        if (bossController == null)
            bossController = FindAnyObjectByType<BossController>();

        if (bossController == null)
            return;

        if (!IsCurrentStageEnding && currentStageIndex == bossStageIndex)
            bossController.StartBossPattern();
        else
            bossController.StopBossPattern();
    }
}
