using System;

[Serializable]
public class SaveData
{
    public int highestClearedStageIndex = -1;
    public bool laserUnlocked = false;
    public bool tutorialCleared = false;
    public bool recallTutorialSeen = false;
}
