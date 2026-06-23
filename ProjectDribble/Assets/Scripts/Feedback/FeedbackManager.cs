using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum BallFeedbackSurface
{
    Paddle,
    Wall,
    Block,
    Ceiling,
    Other
}

public sealed class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("Event Vibration")]
    [SerializeField] private bool uiVibrationEnabled = true;
    [SerializeField] private bool blockVibrationEnabled = true;
    [SerializeField, Range(0f, 1f)] private float uiIntensity = 0.2f;
    [SerializeField, Range(0f, 1f)] private float gaugeReadyIntensity = 0.35f;
    [SerializeField, Range(0f, 1f)] private float paddleIntensityScale = 0.65f;
    [SerializeField, Range(0f, 1f)] private float wallIntensityScale = 0.45f;
    [SerializeField, Range(0f, 1f)] private float blockIntensityScale = 0.55f;
    [SerializeField, Range(0f, 1f)] private float ceilingIntensityScale = 0.8f;

    [Header("Durations (ms)")]
    [SerializeField, Min(1)] private long uiDurationMs = 15;
    [SerializeField, Min(1)] private long paddleDurationMs = 22;
    [SerializeField, Min(1)] private long wallDurationMs = 15;
    [SerializeField, Min(1)] private long blockDurationMs = 20;
    [SerializeField, Min(1)] private long ceilingDurationMs = 30;
    [SerializeField, Min(1)] private long gaugeReadyDurationMs = 18;

    [Header("Cooldowns")]
    [SerializeField, Min(0f)] private float uiCooldown = 0.03f;
    [SerializeField, Min(0f)] private float paddleCooldown = 0.08f;
    [SerializeField, Min(0f)] private float wallCooldown = 0.12f;
    [SerializeField, Min(0f)] private float blockCooldown = 0.1f;
    [SerializeField, Min(0f)] private float ceilingCooldown = 0.12f;
    [SerializeField, Min(0f)] private float laserCooldown = 0.1f;

    [Header("Laser Weak-Strong-Weak Pattern")]
    [SerializeField] private long[] laserTimingsMs = { 0, 20, 25, 40, 25, 25 };
    [SerializeField] private float[] laserIntensityPattern = { 0f, 0.35f, 0f, 1f, 0f, 0.4f };

    private readonly HashSet<Button> registeredButtons = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        FeedbackManager existing = FindAnyObjectByType<FeedbackManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        new GameObject(nameof(FeedbackManager)).AddComponent<FeedbackManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        RegisterSceneButtons();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StopRecallHoldFeedback();
        StopLaserChargeFeedback();
    }

    public void PlayUIButtonFeedback(float eventVolume = 1f)
    {
        SoundManager.Instance?.Play(SoundId.UIClick, eventVolume);
        if (uiVibrationEnabled)
            VibrateOneShot("ui", uiCooldown, uiDurationMs, uiIntensity * eventVolume);
    }

    public void PlayBallBounceFeedback(
        BallFeedbackSurface surface,
        SoundId soundId,
        SoundPlayOptions options)
    {
        SoundManager.Instance?.Play(soundId, options);

        float sourceIntensity = Mathf.Clamp01(options.volumeScale);
        switch (surface)
        {
            case BallFeedbackSurface.Paddle:
                VibrateOneShot("ball-paddle", paddleCooldown, paddleDurationMs,
                    sourceIntensity * paddleIntensityScale);
                break;
            case BallFeedbackSurface.Wall:
                VibrateOneShot("ball-wall", wallCooldown, wallDurationMs,
                    sourceIntensity * wallIntensityScale);
                break;
            case BallFeedbackSurface.Block:
                if (blockVibrationEnabled)
                    VibrateOneShot("ball-block", blockCooldown, blockDurationMs,
                        sourceIntensity * blockIntensityScale);
                break;
            case BallFeedbackSurface.Ceiling:
                VibrateOneShot("ceiling", ceilingCooldown, ceilingDurationMs,
                    sourceIntensity * ceilingIntensityScale);
                break;
        }
    }

    public void PlayCeilingHitFeedback(float eventVolume)
    {
        SoundManager.Instance?.Play(SoundId.CeilingHit, eventVolume);
        VibrateOneShot("ceiling", ceilingCooldown, ceilingDurationMs,
            Mathf.Clamp01(eventVolume) * ceilingIntensityScale);
    }

    public void PlayBlockBreakFeedback(float eventVolume = 1f)
    {
        SoundManager.Instance?.Play(SoundId.BlockBreak, eventVolume);
        if (blockVibrationEnabled)
            VibrateOneShot("ball-block", blockCooldown, blockDurationMs,
                Mathf.Clamp01(eventVolume) * blockIntensityScale);
    }

    public void PlayLaserFireFeedback(float eventVolume)
    {
        SoundManager.Instance?.Play(SoundId.LaserFire, eventVolume);

        if (VibrationManager.Instance == null)
            return;

        float scale = Mathf.Clamp01(eventVolume);
        float[] intensities = new float[laserIntensityPattern.Length];
        for (int i = 0; i < intensities.Length; i++)
            intensities[i] = laserIntensityPattern[i] * scale;

        VibrationManager.Instance.TryPlayWaveform(
            "laser", laserCooldown, laserTimingsMs, intensities);
    }

    public void PlayGaugeSegmentFilledFeedback(float eventVolume, bool becameReady)
    {
        SoundManager.Instance?.Play(SoundId.GaugeSegmentFilled, eventVolume);
        if (becameReady)
            VibrateOneShot("gauge-ready", 0f, gaugeReadyDurationMs,
                gaugeReadyIntensity * Mathf.Clamp01(eventVolume));
    }

    public void StartRecallHoldFeedback(float progress01)
    {
        VibrationManager.Instance?.UpdateProgressiveVibration(progress01);
    }

    public void StopRecallHoldFeedback()
    {
        VibrationManager.Instance?.StopProgressiveVibration();
    }

    public void StartLaserChargeFeedback(float intensity = 1f)
    {
        VibrationManager.Instance?.StartLaserChargePulse(intensity);
    }

    public void UpdateLaserChargeFeedback(float intensity = 1f)
    {
        VibrationManager.Instance?.UpdateLaserChargePulse(intensity);
    }

    public void StopLaserChargeFeedback()
    {
        VibrationManager.Instance?.StopLaserChargePulse();
    }

    private void VibrateOneShot(string eventId, float cooldown, long durationMs, float intensity)
    {
        VibrationManager.Instance?.TryPlayOneShot(eventId, cooldown, durationMs, intensity);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        registeredButtons.Clear();
        StopRecallHoldFeedback();
        StopLaserChargeFeedback();
        RegisterSceneButtons();
    }

    private void RegisterSceneButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);

        foreach (Button button in buttons)
        {
            if (button == null || button.name == "VibrationToggle" || !registeredButtons.Add(button))
                continue;

            button.onClick.AddListener(HandleUIButtonClicked);
        }
    }

    private void HandleUIButtonClicked()
    {
        PlayUIButtonFeedback();
    }
}
