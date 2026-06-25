using UnityEngine;

public class EndingSequenceController : MonoBehaviour
{
    [SerializeField] private GameObject normalGaugeRoot;
    [SerializeField] private EndingGaugeController endingGauge;
    [SerializeField] private StrangePopupController strangePopup;
    [SerializeField] private EndingMovieController endingMovieController;
    [SerializeField] private LaserChargeController laserChargeController;
    [SerializeField] private PlayerDamagedManager playerDamagedManager;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private bool disableLaserDuringEnding = true;
    [SerializeField] private bool disableDamageDuringEnding = true;
    [SerializeField] private bool hidePauseButtonDuringEnding;

    private bool isEnding;
    private bool laserWasEnabled;
    private bool damageWasEnabled;
    private bool pauseButtonWasActive;

    public bool IsEnding => isEnding;

    public void BeginEnding()
    {
        if (isEnding)
            EndEndingAndReset();

        isEnding = true;
        Time.timeScale = 1f;

        if (normalGaugeRoot != null)
            normalGaugeRoot.SetActive(false);

        if (strangePopup != null)
            strangePopup.HideAndReset();

        if (laserChargeController != null)
        {
            laserWasEnabled = laserChargeController.enabled;

            if (disableLaserDuringEnding)
                laserChargeController.enabled = false;
        }

        if (playerDamagedManager != null)
        {
            damageWasEnabled = playerDamagedManager.enabled;

            if (disableDamageDuringEnding)
                playerDamagedManager.enabled = false;
        }

        if (pauseButton != null)
        {
            pauseButtonWasActive = pauseButton.activeSelf;

            if (hidePauseButtonDuringEnding)
                pauseButton.SetActive(false);
        }

        if (endingGauge != null)
            endingGauge.BeginFill();
    }

    public void EndEndingAndReset()
    {
        bool wasEnding = isEnding;
        isEnding = false;

        if (wasEnding)
            Time.timeScale = 1f;

        if (endingGauge != null)
            endingGauge.ResetGauge();

        if (strangePopup != null)
            strangePopup.HideAndReset();

        if (endingMovieController != null)
            endingMovieController.StopAndReset();

        if (!wasEnding)
            return;

        if (normalGaugeRoot != null)
            normalGaugeRoot.SetActive(true);

        if (laserChargeController != null && disableLaserDuringEnding)
            laserChargeController.enabled = laserWasEnabled;

        if (playerDamagedManager != null && disableDamageDuringEnding)
            playerDamagedManager.enabled = damageWasEnabled;

        if (pauseButton != null && hidePauseButtonDuringEnding)
            pauseButton.SetActive(pauseButtonWasActive);
    }
}
