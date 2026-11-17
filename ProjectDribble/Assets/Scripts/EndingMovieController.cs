using DefaultNamespace;
using UnityEngine;
using UnityEngine.Video;

public class EndingMovieController : MonoBehaviour
{
    [SerializeField] private GameObject movieRoot;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioClip movieAudioClip;
    [SerializeField] private GameObject tapToReturnText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EndingSequenceController endingSequenceController;
    [SerializeField] private StrangePopupController strangePopup;
    [SerializeField] private bool hideTapTextUntilFinished = true;
    [SerializeField, Min(0f)] private float inputEnableDelayAfterMovie = 0.2f;

    private bool isPlaying;
    private bool movieAudioPending;
    private bool waitingForTap;
    private float inputEnabledRealtime = float.PositiveInfinity;
    private AudioSource movieAudioSource;

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

        StopMovieAudio();
    }

    private void Update()
    {
        if (isPlaying)
        {
            if (movieAudioPending && videoPlayer != null && videoPlayer.isPlaying && videoPlayer.frame >= 0)
                PlayMovieAudio();

            return;
        }

        if (!waitingForTap)
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
        StopMovieAudio();
        SoundManager.Instance?.StopAllAudioForEnding();
        SoundManager.Instance?.Play(SoundId.EndingErrorVideoStart);
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
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.isLooping = false;
        movieAudioPending = movieAudioClip != null;
        isPlaying = true;
        videoPlayer.Play();
    }

    public void StopAndReset()
    {
        StopMovieAudio();

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
        if (!isPlaying)
            return;

        StopMovieAudio();
        isPlaying = false;
        waitingForTap = true;
        inputEnabledRealtime = Time.realtimeSinceStartup + inputEnableDelayAfterMovie;

        SaveManager saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetEndingCleared(true);
            saveManager.Save();
        }

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

    private void PlayMovieAudio()
    {
        movieAudioPending = false;

        float offset = Mathf.Max(0f, (float)videoPlayer.time);
        float volume = SoundManager.Instance != null ? SoundManager.Instance.SfxVolume : 1f;
        if (offset >= movieAudioClip.length || volume <= 0f)
            return;

        if (movieAudioSource == null)
        {
            movieAudioSource = gameObject.AddComponent<AudioSource>();
            movieAudioSource.playOnAwake = false;
            movieAudioSource.spatialBlend = 0f;
        }

        movieAudioSource.clip = movieAudioClip;
        movieAudioSource.volume = volume;
        movieAudioSource.time = offset;
        movieAudioSource.Play();
    }

    private void StopMovieAudio()
    {
        movieAudioPending = false;
        if (movieAudioSource != null)
            movieAudioSource.Stop();
    }
}
