using TMPro;
using UnityEngine;

public class PlayTestUI : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private BallMovement ballMovement;
    [SerializeField] private BallPowerController ballPowerController;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text damageText;

    
    
    [Header("Display")]
    [SerializeField] private string prefix = "Speed: ";
    [SerializeField] private string damagePrefix = "Damage: ";
    [SerializeField] private int decimalPlaces = 1;

    private void Awake()
    {
        if (ballPowerController == null && ballMovement != null)
            ballPowerController = ballMovement.GetComponent<BallPowerController>();
    }

    private void Update()
    {
        if (ballMovement == null || speedText == null)
            return;

        float speed = ballMovement.speed;
        float damage = ballPowerController != null ? ballPowerController.CurrentDamage() : 0f;

        string speedDisplay = prefix + speed.ToString($"F{decimalPlaces}");
        string damageDisplay = damagePrefix + damage.ToString($"F{decimalPlaces}");

        speedText.text = damageText == null
            ? speedDisplay + "\n" + damageDisplay
            : speedDisplay;

        if (damageText != null)
            damageText.text = damageDisplay;
    }
}
