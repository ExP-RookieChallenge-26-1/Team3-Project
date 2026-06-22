using TMPro;
using UnityEngine;

public class PlayTestUI : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private BallMovement ballMovement;
    [SerializeField] private BallPowerController ballPowerController;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private StageManager stageManager;

    [Header("UI")]
    [SerializeField] private GameObject playTestUI;
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject statTextOnButton;
    [SerializeField] private GameObject statTextOffButton;
    [SerializeField] private TMP_InputField stageInput;
    [SerializeField] private GameObject pauseButton;

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

    public void OpenPlayTest()
    {
        if (playTestUI != null)
            playTestUI.SetActive(true);
    }
    public void StatTextsOn()
    {
        speedText.gameObject.SetActive(true);
        damageText.gameObject.SetActive(true);
        statTextOnButton.SetActive(false);
        statTextOffButton.SetActive(true);
    }
    public void StatTextsOff()
    {
        speedText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        statTextOnButton.SetActive(true);
        statTextOffButton.SetActive(false);
    }
    public void SelectStage()
    {
        if (stageInput == null || stageManager == null)
            return;

        if (!int.TryParse(stageInput.text, out int stageIndex))
        {
            Debug.LogWarning("스테이지 번호가 숫자가 아님");
            return;
        }

        if (!stageManager.IsValidStageIndex(stageIndex))
        {
            Debug.LogWarning($"없는 스테이지 인덱스: {stageIndex}");
            return;
        }

        playTestUI.SetActive(false);
        titleUI.SetActive(false);
        pauseButton.SetActive(true);
        Time.timeScale = 1f;
        stageManager.StartStage(stageIndex);
    }
}
