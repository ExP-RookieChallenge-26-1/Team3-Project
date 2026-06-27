using System.Collections;
using DefaultNamespace;
using TMPro;
using UnityEngine;

public class StrangePopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI dotText;
    [SerializeField] private GameObject homeButton;
    [SerializeField] private float dotInterval = 0.35f;
    [SerializeField] private float homeButtonDelay = 3f;
    [SerializeField] private bool pauseWhenHomeButtonAppears = true;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EndingSequenceController endingSequenceController;
    [SerializeField] private EndingMovieController endingMovieController;

    private Coroutine animationRoutine;
    private Coroutine homeButtonRoutine;

    private void Awake()
    {
        HideAndReset();
    }

    public void Show()
    {
        HideAndReset();

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (dotText != null)
            dotText.text = ".";

        SoundManager.Instance?.Play(SoundId.EndingPopupNoise);

        if (homeButton != null)
            homeButton.SetActive(false);

        animationRoutine = StartCoroutine(AnimateDots());
        homeButtonRoutine = StartCoroutine(ShowHomeButtonAfterDelay());
    }

    public void HideAndReset()
    {
        StopRunningCoroutines();

        if (dotText != null)
            dotText.text = string.Empty;

        if (homeButton != null)
            homeButton.SetActive(false);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void OnClickHome()
    {
        Time.timeScale = 1f;

        if (endingMovieController != null)
        {
            endingMovieController.PlayMovie();
            return;
        }

        endingSequenceController?.EndEndingAndReset();

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager != null)
            gameManager.GoHomeFromEnding();
        else
            Debug.LogWarning("StrangePopupController: GameManager is missing; cannot return home.");
    }

    private IEnumerator AnimateDots()
    {
        int dotCount = 1;

        while (true)
        {
            if (dotText != null)
                dotText.text = new string('.', dotCount);

            dotCount = dotCount >= 3 ? 1 : dotCount + 1;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, dotInterval));
        }
    }

    private IEnumerator ShowHomeButtonAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, homeButtonDelay));

        if (homeButton != null)
            homeButton.SetActive(true);

        SoundManager.Instance?.Play(SoundId.EndingQuestionAppear);

        if (pauseWhenHomeButtonAppears)
            Time.timeScale = 0f;
    }

    private void StopRunningCoroutines()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (homeButtonRoutine != null)
        {
            StopCoroutine(homeButtonRoutine);
            homeButtonRoutine = null;
        }
    }
}
