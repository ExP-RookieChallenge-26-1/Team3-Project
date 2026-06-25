using UnityEngine;
using UnityEngine.Video;

public class EndingMovieController : MonoBehaviour
{
    [SerializeField] private GameObject movieRoot;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject tapToReturnText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EndingSequenceController endingSequenceController;
    [SerializeField] private StrangePopupController strangePopup;
    [SerializeField] private bool hideTapTextUntilFinished = true;
    [SerializeField, Min(0f)] private float inputEnableDelayAfterMovie = 0.2f;

    private bool isPlaying;
    private bool waitingForTap;
    private float inputEnabledRealtime = float.PositiveInfinity;

    private void Awake()
    {
        StopAndReset();
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleMovieFinished;
            videoPlayer.loopPointReached += HandleMovieFinished;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleMovieFinished;
    }

    private void Update()
    {
        if (isPlaying || !waitingForTap)
            return;

        if (Time.realtimeSinceStartup < inputEnabledRealtime)
            return;

        if (!HasReturnInput())
            return;

        ReturnHomeAfterMovie();
    }

    public void PlayMovie()
    {
        Time.timeScale = 1f;
        isPlaying = false;
        waitingForTap = false;
        inputEnabledRealtime = float.PositiveInfinity;

        if (movieRoot != null)
            movieRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (tapToReturnText != null)
            tapToReturnText.SetActive(!hideTapTextUntilFinished);

        if (strangePopup != null)
            strangePopup.HideAndReset();

        if (videoPlayer == null)
        {
            Debug.LogWarning("[EndingMovieController] VideoPlayer is missing.");
            StopAndReset();
            return;
        }

        videoPlayer.loopPointReached -= HandleMovieFinished;
        videoPlayer.loopPointReached += HandleMovieFinished;

        if (videoPlayer.clip == null)
        {
            Debug.LogWarning("[EndingMovieController] Video clip is missing.");
            StopAndReset();
            return;
        }

        if (videoPlayer.clip.length <= 0d)
            Debug.LogWarning("[EndingMovieController] Video clip length is 0 or not ready yet. Check import/settings.");

        videoPlayer.Stop();
        videoPlayer.isLooping = false;
        videoPlayer.time = 0d;
        videoPlayer.Play();

        isPlaying = true;
        waitingForTap = false;
        inputEnabledRealtime = float.PositiveInfinity;
    }

    public void StopAndReset()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (movieRoot != null)
            movieRoot.SetActive(false);

        if (tapToReturnText != null)
            tapToReturnText.SetActive(false);

        isPlaying = false;
        waitingForTap = false;
        inputEnabledRealtime = float.PositiveInfinity;
    }

    private void HandleMovieFinished(VideoPlayer finishedPlayer)
    {
        isPlaying = false;
        waitingForTap = true;
        inputEnabledRealtime = Time.realtimeSinceStartup + inputEnableDelayAfterMovie;

        if (tapToReturnText != null)
            tapToReturnText.SetActive(true);
    }

    private bool HasReturnInput()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.touchCount <= 0)
            return false;

        Touch touch = Input.GetTouch(0);
        return touch.phase == TouchPhase.Began;
    }

    private void ReturnHomeAfterMovie()
    {
        Time.timeScale = 1f;
        StopAndReset();

        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (endingSequenceController != null)
            endingSequenceController.EndEndingAndReset();

        if (gameManager != null)
            gameManager.GoHomeFromEnding();
        else
            Debug.LogWarning("[EndingMovieController] GameManager is missing; cannot return home.");
    }
}
