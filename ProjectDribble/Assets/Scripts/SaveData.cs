using System;

[Serializable]
public class SaveData
{
    public int highestClearedStageIndex = 0;
    public bool laserUnlocked = false;
    public bool tutorialCleared = false;
    public bool recallTutorialSeen = false;
    public bool endingCleared = false;
}
