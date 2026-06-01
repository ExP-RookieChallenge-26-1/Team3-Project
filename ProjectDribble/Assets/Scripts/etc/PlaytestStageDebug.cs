using UnityEngine;

public class PlaytestStageDebug : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;

    public void ForceNextStage()
    {
        if (stageManager == null)
            return;

        stageManager.TryStartNextStage();
    }

    public void ForceRestartStage()
    {
        if (stageManager == null)
            return;

        stageManager.RestartCurrentStage();
    }

    public void ForceLoadStage(int stageIndex)
    {
        if (stageManager == null)
            return;

        stageManager.StartStage(stageIndex);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            ForceRestartStage();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            ForceNextStage();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ForceLoadStage(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ForceLoadStage(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ForceLoadStage(2);
        }
    }
#endif
}
