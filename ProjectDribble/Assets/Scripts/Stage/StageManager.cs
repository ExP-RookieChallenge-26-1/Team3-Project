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

    [Header("Top Decoration")]
    [SerializeField] private Transform topDecorationParent;

    private int currentStageIndex;
    private GameObject currentTopDecoration;

    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages == null ? 0 : stages.Length;
    public bool IsCurrentStageTutorial =>
        IsValidStageIndex(currentStageIndex) &&
        stages[currentStageIndex] != null &&
        stages[currentStageIndex].isTutorialStage;
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
        if (bossController == null)
            bossController = FindAnyObjectByType<BossController>();

        if (bossController != null)
            bossController.StopBossPattern();

        if (GameManager.Instance != null)
            GameManager.Instance.RequestStageClear();
        else
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

        if (data.useCeiling && ceilingManager != null)
        {
            ceilingManager.InitializeCeiling(data.ceilingMaxHpOverride, data.ceilingSegmentMode);
        }
        else if (data.useCeiling)
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
            Sprite fixedBlockSpriteOverride = data.ArtProfile != null
                ? data.ArtProfile.FixedBlockSprite
                : null;

            blockManager.InitializeStageBlocks(
                data.blockData,
                data.useCeiling,
                fixedBlockSpriteOverride
            );
        }
        else
        {
            Debug.LogWarning("StageManager: BlockManager is missing.");
        }

        if (playerHealth != null)
        {
            playerHealth.InitializePlayerHealth(data.playerMaxHpOverride);
        }

        if (gaugeManager != null)
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

        if (tutorialManager != null)
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

    private void ApplyBossPatternStateForCurrentStage()
    {
        if (bossController == null)
            bossController = FindAnyObjectByType<BossController>();

        if (bossController == null)
            return;

        if (currentStageIndex == bossStageIndex)
            bossController.StartBossPattern();
        else
            bossController.StopBossPattern();
    }
}
