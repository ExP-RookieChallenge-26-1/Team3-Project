using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    
    void Awake()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
    }

    public void StartGame()
    {
    }

    public void RequestStageClear()
    {
    }

    public void RequestGameOver()
    {
    }

    public void RetryStage()
    {
    }

    public void NextStage()
    {
    }

    public void PauseGame()
    {
    }

    public void ResumeGame()
    {
    }
}
