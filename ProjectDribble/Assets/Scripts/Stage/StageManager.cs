using DefaultNamespace;
using UnityEngine;

public class StageManager : MonoBehaviour 
{
    [Header("Stage Data")]
    [SerializeField] private StageDefinition[] stages;
    [SerializeField] private int startStageIndex = 0;
    [SerializeField] private int bossStageIndex = -1;

    [Header("System References")]
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private BallSpawnController ballSpawnController;
    [SerializeField] private BossController bossController;
    [SerializeField] private TutorialManager tutorialManager;

    private int currentStageIndex;

    public int CurrentStageIndex => currentStageIndex;
    public int StageCount => stages == null ? 0 : stages.Length;

    private void OnEnable()
    {
        BindStageEvents();
    }

    private void OnDisable()
    {
        UnbindStageEvents();
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

        StartStage(startStageIndex);
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
        if (bossController == null)
            bossController = FindAnyObjectByType<BossController>();

        if (bossController != null)
            bossController.StopBossPattern();

        GameManager.Instance.RequestStageClear();
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

        if (playerHealth != null)
        {
            playerHealth.OnPlayerDead -= HandlePlayerDead;
        }
    }

    private void ApplyStageData(StageDefinition data)
    {
        if (data == null)
        {
            Debug.LogWarning("StageManager: StageDefinition is null.");
            return;
        }

        if (ceilingManager != null)
        {
            ceilingManager.InitializeCeiling(data.ceilingMaxHpOverride, data.ceilingSegmentMode);
        }

        if (blockManager != null)
        {
            blockManager.InitializeStageBlocks(data.blockData);
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
