using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    private bool isShowing;
    private bool pausedByTutorial;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (!isShowing)
            return;

        bool clicked = Input.GetMouseButtonDown(0);
        bool touched = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;

        if (clicked || touched)
            Hide();
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
        isShowing = false;
    }

    public void ShowMessage(string message, bool pauseGame)
    {
        if (!pauseGame)
            RestoreTimeScaleIfNeeded();

        if (messageText != null)
            messageText.text = message;

        if (messageRoot != null)
            messageRoot.SetActive(true);

        isShowing = true;

        if (pauseGame && !pausedByTutorial)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedByTutorial = true;
        }
    }

    public void Hide()
    {
        if (messageRoot != null)
            messageRoot.SetActive(false);

        if (messageText != null)
            messageText.text = string.Empty;

        isShowing = false;
        RestoreTimeScaleIfNeeded();
    }

    public void Continue()
    {
        Hide();
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!pausedByTutorial)
            return;

        Time.timeScale = previousTimeScale;
        pausedByTutorial = false;
    }
}
