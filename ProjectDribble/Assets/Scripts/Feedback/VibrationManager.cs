using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    public const string VibrationEnabledPrefsKey = "VibrationEnabled";

    [Header("Amplitude")]
    [SerializeField, Range(1, 255)] private int minAmplitude = 30;
    [SerializeField, Range(1, 255)] private int maxAmplitude = 220;

    [Header("Progressive Feedback")]
    [SerializeField, Min(0.05f)] private float progressivePulseInterval = 0.12f;
    [SerializeField, Min(1)] private long progressivePulseDurationMs = 18;
    [SerializeField, Range(0f, 1f)] private float progressiveMinimumIntensity = 0.15f;

    [Header("Laser Charge Pulse")]
    [SerializeField] private bool chargePulseEnabled = true;
    [SerializeField, Min(0.05f)] private float chargePulseInterval = 0.15f;
    [SerializeField, Min(1)] private long chargePulseDurationMs = 90;
    [SerializeField, Range(0f, 1f)] private float chargePulseIntensity = 0.25f;
    [SerializeField, Min(0f)] private float chargePulseForegroundGap = 0.04f;

    private readonly Dictionary<string, float> lastEventTimes = new();
    private float nextProgressivePulseTime;
    private bool progressiveFeedbackActive;
    private bool chargePulseActive;
    private bool chargePulseOwnsActiveVibration;
    private float nextChargePulseTime;
    private float nextBackgroundPulseAllowedTime;

    public bool VibrationEnabled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        VibrationManager existing = FindAnyObjectByType<VibrationManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        new GameObject(nameof(VibrationManager)).AddComponent<VibrationManager>();
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
        VibrationEnabled = PlayerPrefs.GetInt(VibrationEnabledPrefsKey, 1) != 0;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            CancelVibration();
    }

    private void OnApplicationQuit()
    {
        CancelVibration();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrationEnabled = enabled;
        PlayerPrefs.SetInt(VibrationEnabledPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!enabled)
            CancelVibration();
    }

    public bool TryPlayOneShot(
        string eventId,
        float cooldownSeconds,
        long milliseconds,
        float intensity)
    {
        if (!CanPlay(eventId, cooldownSeconds, intensity))
            return false;

        chargePulseOwnsActiveVibration = false;
        DelayBackgroundPulse(milliseconds / 1000f);
        PlayOneShot(milliseconds, NormalizeIntensityToAmplitude(intensity));
        return true;
    }

    public bool TryPlayWaveform(
        string eventId,
        float cooldownSeconds,
        long[] timings,
        float[] intensities)
    {
        if (timings == null || intensities == null ||
            timings.Length == 0 || timings.Length != intensities.Length)
            return false;

        float peakIntensity = 0f;
        for (int i = 0; i < intensities.Length; i++)
            peakIntensity = Mathf.Max(peakIntensity, intensities[i]);

        if (!CanPlay(eventId, cooldownSeconds, peakIntensity))
            return false;

        int[] amplitudes = new int[intensities.Length];
        for (int i = 0; i < amplitudes.Length; i++)
            amplitudes[i] = intensities[i] <= 0f ? 0 : NormalizeIntensityToAmplitude(intensities[i]);

        long totalDurationMs = 0L;
        for (int i = 0; i < timings.Length; i++)
            totalDurationMs += Math.Max(0L, timings[i]);

        chargePulseOwnsActiveVibration = false;
        DelayBackgroundPulse(totalDurationMs / 1000f);
        PlayWaveform(timings, amplitudes);
        return true;
    }

    public void UpdateProgressiveVibration(float progress01)
    {
        if (!VibrationEnabled)
            return;

        if (chargePulseActive)
            StopLaserChargePulse();

        float progress = Mathf.Clamp01(progress01);
        if (progress <= 0f)
            return;

        progressiveFeedbackActive = true;
        if (Time.unscaledTime < nextProgressivePulseTime)
            return;

        nextProgressivePulseTime = Time.unscaledTime + progressivePulseInterval;
        float intensity = Mathf.Lerp(progressiveMinimumIntensity, 1f, progress);
        long gapMs = Math.Max(1L, (long)(progressivePulseInterval * 1000f) - progressivePulseDurationMs);
        chargePulseOwnsActiveVibration = false;
        PlayWaveform(
            new[] { 0L, progressivePulseDurationMs, gapMs },
            new[] { 0, NormalizeIntensityToAmplitude(intensity), 0 });
    }

    public void StopProgressiveVibration()
    {
        if (!progressiveFeedbackActive)
            return;

        progressiveFeedbackActive = false;
        nextProgressivePulseTime = 0f;
        CancelVibration();
    }

    public void StartLaserChargePulse(float intensity = 1f)
    {
        if (!VibrationEnabled || !chargePulseEnabled || intensity <= 0f)
            return;

        if (progressiveFeedbackActive)
            StopProgressiveVibration();

        chargePulseActive = true;
        nextChargePulseTime = 0f;
        UpdateLaserChargePulse(intensity);
    }

    public void UpdateLaserChargePulse(float intensity = 1f)
    {
        if (!VibrationEnabled || !chargePulseEnabled || !chargePulseActive || intensity <= 0f)
            return;

        float now = Time.unscaledTime;
        if (now < nextChargePulseTime || now < nextBackgroundPulseAllowedTime)
            return;

        nextChargePulseTime = now + chargePulseInterval;
        chargePulseOwnsActiveVibration = true;
        PlayOneShot(
            chargePulseDurationMs,
            NormalizeIntensityToAmplitude(chargePulseIntensity * Mathf.Clamp01(intensity)));
    }

    public void StopLaserChargePulse()
    {
        if (!chargePulseActive)
            return;

        chargePulseActive = false;
        nextChargePulseTime = 0f;

        if (chargePulseOwnsActiveVibration)
            CancelHardwareVibration();

        chargePulseOwnsActiveVibration = false;
    }

    public int NormalizeIntensityToAmplitude(float intensity)
    {
        int low = Mathf.Clamp(minAmplitude, 1, 255);
        int high = Mathf.Clamp(maxAmplitude, low, 255);
        return Mathf.RoundToInt(Mathf.Lerp(low, high, Mathf.Clamp01(intensity)));
    }

    public void PlayOneShot(long milliseconds, int amplitude)
    {
        if (!VibrationEnabled || milliseconds <= 0)
            return;

#if UNITY_ANDROID
        if (Application.isEditor)
            return;

        try
        {
            using AndroidJavaObject vibrator = GetVibrator();
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                return;

            if (GetAndroidSdkInt() >= 26)
            {
                using AndroidJavaClass effectClass = new("android.os.VibrationEffect");
                using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255));
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"VibrationManager: Android vibration failed. {exception.Message}");
            Handheld.Vibrate();
        }
#endif
    }

    public void PlayWaveform(long[] timings, int[] amplitudes)
    {
        if (!VibrationEnabled || timings == null || amplitudes == null ||
            timings.Length == 0 || timings.Length != amplitudes.Length)
            return;

#if UNITY_ANDROID
        if (Application.isEditor)
            return;

        try
        {
            using AndroidJavaObject vibrator = GetVibrator();
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                return;

            if (GetAndroidSdkInt() >= 26)
            {
                using AndroidJavaClass effectClass = new("android.os.VibrationEffect");
                using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform", timings, amplitudes, -1);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", timings, -1);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"VibrationManager: Android waveform failed. {exception.Message}");
            Handheld.Vibrate();
        }
#endif
    }

    public void CancelVibration()
    {
        progressiveFeedbackActive = false;
        nextProgressivePulseTime = 0f;
        chargePulseActive = false;
        chargePulseOwnsActiveVibration = false;
        nextChargePulseTime = 0f;
        nextBackgroundPulseAllowedTime = 0f;

        CancelHardwareVibration();
    }

    private void DelayBackgroundPulse(float durationSeconds)
    {
        nextBackgroundPulseAllowedTime = Mathf.Max(
            nextBackgroundPulseAllowedTime,
            Time.unscaledTime + Mathf.Max(0f, durationSeconds) + chargePulseForegroundGap);
    }

    private void CancelHardwareVibration()
    {

#if UNITY_ANDROID
        if (Application.isEditor)
            return;

        try
        {
            using AndroidJavaObject vibrator = GetVibrator();
            vibrator?.Call("cancel");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"VibrationManager: Android vibration cancellation failed. {exception.Message}");
        }
#endif
    }

    private bool CanPlay(string eventId, float cooldownSeconds, float intensity)
    {
        if (!VibrationEnabled || intensity <= 0f)
            return false;

        if (!string.IsNullOrEmpty(eventId) &&
            lastEventTimes.TryGetValue(eventId, out float lastTime) &&
            Time.unscaledTime < lastTime + Mathf.Max(0f, cooldownSeconds))
            return false;

        if (!string.IsNullOrEmpty(eventId))
            lastEventTimes[eventId] = Time.unscaledTime;

        return true;
    }

#if UNITY_ANDROID
    private static AndroidJavaObject GetVibrator()
    {
        using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        return activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
    }

    private static int GetAndroidSdkInt()
    {
        using AndroidJavaClass versionClass = new("android.os.Build$VERSION");
        return versionClass.GetStatic<int>("SDK_INT");
    }
#endif
}
