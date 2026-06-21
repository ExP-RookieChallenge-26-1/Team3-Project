using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public SaveData Current { get; private set; }

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Current = new SaveData();
            Save();
            return;
        }

        string json = File.ReadAllText(SavePath);
        Current = JsonUtility.FromJson<SaveData>(json);

        if (Current == null)
            Current = new SaveData();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Current, true);
        File.WriteAllText(SavePath, json);
    }

    public void MarkStageCleared(int stageIndex)
    {
        if (stageIndex > Current.highestClearedStageIndex)
            Current.highestClearedStageIndex = stageIndex;
    }

    public void SetLaserUnlocked(bool value)
    {
        Current.laserUnlocked = value;
    }

    public void SetTutorialCleared(bool value)
    {
        Current.tutorialCleared = value;
    }

    public void SetRecallTutorialSeen(bool value)
    {
        Current.recallTutorialSeen = value;
    }

    public int GetStartStageIndex(int stageCount)
    {
        int nextStage = Current.highestClearedStageIndex + 1;
        return Mathf.Clamp(nextStage, 0, stageCount - 1);
    }

    public void ResetSave()
    {
        Current = new SaveData();
        Save();
    }
}
