using System;
using UnityEngine;

public class LaserUnlockState : MonoBehaviour
{
    [SerializeField] private bool isLaserUnlocked;

    public bool IsLaserUnlocked => isLaserUnlocked;

    public event Action OnLaserUnlocked;
    public event Action OnLaserLocked;

    public void UnlockLaser()
    {
        if (isLaserUnlocked)
            return;

        isLaserUnlocked = true;
        OnLaserUnlocked?.Invoke();
    }

    public void LockLaser()
    {
        if (!isLaserUnlocked)
            return;

        isLaserUnlocked = false;
        OnLaserLocked?.Invoke();
    }

    public void ResetProgressForNewGame()
    {
        // TODO: Call this from the real new-game/progress-reset flow when that system is added.
        LockLaser();
    }
}
