using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Stage Data")]
    [SerializeField] private StageDefinition[] stages;
    [SerializeField] private int startStageIndex = 0;

    [Header("System References")]
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private BallSpawnController ballSpawnController;

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
        bool moved = TryStartNextStage();

        if (!moved)
        {
            RestartCurrentStage();
        }
    }

    private void HandlePlayerDead()
    {
        RestartCurrentStage();
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

        if (blockManager != null)
        {
            blockManager.InitializeStageBlocks(data.blockData);
        }

        if (ceilingManager != null)
        {
            ceilingManager.InitializeCeiling(data.ceilingMaxHpOverride);
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
    }

    private bool IsValidStageIndex(int index)
    {
        return stages != null && index >= 0 && index < stages.Length;
    }
}
