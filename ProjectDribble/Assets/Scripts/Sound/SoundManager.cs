using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public enum SoundType
{
    BGM,
    SFX
}

public enum BgmMuffleReason
{
    Pause,
    Settings,
    Tutorial
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private BallSpeedController ballSpeedController;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;

    [FormerlySerializedAs("sfxSource")]
    [SerializeField] private AudioSource defaultSfxSource;

    [SerializeField] private AudioSource loopSource;
    [SerializeField] private AudioSource uiSfxSource;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private float normalLowPassCutoff = 22000f;
    [SerializeField] private float muffledLowPassCutoff = 1000f;

    [Header("Sound Data")]
    [SerializeField] private SoundData[] soundDatas;

    private readonly Dictionary<SoundId, SoundData> soundMap = new();
    private readonly Dictionary<SoundId, float> lastPlayTimes = new();
    private SoundData currentLoopData;
    private SoundPlayOptions currentLoopOptions;
    private SoundId currentLoopId = SoundId.None;
    private float currentLoopBaseVolume = 1f;
    private SoundId currentBgmId = SoundId.None;
    private readonly HashSet<BgmMuffleReason> bgmMuffleReasons = new();
    private float userBgmVolume = 1f;
    private float userSfxVolume = 1f;

    private const string BgmVolumeParameter = "BGMVolume";
    private const string SfxVolumeParameter = "SFXVolume";
    private const string BgmLowPassCutoffParameter = "BGMLowPassCutoff";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (bgmSource != null)
            bgmSource.loop = true;

        if (loopSource != null)
            loopSource.loop = true;

        ApplyBgmMuffleState();

        soundMap.Clear();
        lastPlayTimes.Clear();

        foreach (SoundData data in soundDatas)
        {
            if (data == null)
                continue;

            if (soundMap.ContainsKey(data.id))
            {
                Debug.LogWarning($"SoundManager: {data.id} is registered more than once. The first entry will be used.");
                continue;
            }

            soundMap.Add(data.id, data);
        }
    }

    private float GetBallSpeedPitchRatio()
    {
        if (ballSpeedController == null || ballSpeedController.data == null)
            return 0f;

        float baseSpeed = ballSpeedController.data.baseSpeed;
        float maxSpeed = ballSpeedController.data.NormalMaxSpeed;

        if (Mathf.Approximately(baseSpeed, maxSpeed))
            return 0f;

        return Mathf.InverseLerp(baseSpeed, maxSpeed, ballSpeedController.CurrentSpeed);
    }

    public void Play(SoundId id)
    {
        Play(id, SoundPlayOptions.Default);
    }

    public void Play(SoundId id, float ratio)
    {
        SoundPlayOptions options = SoundPlayOptions.Default;
        options.ratio = ratio;
        Play(id, options);
    }

    public void Play(SoundId id, float ratio, float volumeScale)
    {
        SoundPlayOptions options = SoundPlayOptions.Default;
        options.ratio = ratio;
        options.volumeScale = volumeScale;
        Play(id, options);
    }

    public void Play(SoundId id, SoundPlayOptions options)
    {
        if (!TryGetSoundData(id, out SoundData data))
            return;

        if (!CanPlayByInterval(data))
            return;

        AudioClip clip = GetRandomClip(data);
        if (clip == null)
            return;

        if (data.soundType == SoundType.BGM)
        {
            PlayBgmInternal(id, data, clip);
            return;
        }

        float pitch = CalculatePitch(data, options);
        float volume = CalculateVolume(data, options);

        AudioSource source = GetSourceFor(id);
        if (source == null)
        {
            Debug.LogWarning($"SoundManager: AudioSource for {id} is not assigned.");
            return;
        }

        ApplyMixerGroup(source, data.mixerGroup);
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
        lastPlayTimes[id] = Time.time;
    }
    

    public void PlayLoop(SoundId id)
    {
        PlayLoop(id, SoundPlayOptions.Default);
    }

    public void PlayLoop(SoundId id, float ratio)
    {
        SoundPlayOptions options = SoundPlayOptions.Default;
        options.ratio = ratio;
        PlayLoop(id, options);
    }

    public void PlayLoop(SoundId id, SoundPlayOptions options)
    {
        if (loopSource == null)
        {
            Debug.LogWarning("SoundManager: loop AudioSource is not assigned.");
            return;
        }

        if (!TryGetSoundData(id, out SoundData data))
            return;

        if (loopSource.isPlaying && currentLoopId == id)
        {
            SetLoopRatio(options.ratio);
            return;
        }

        AudioClip clip = GetRandomClip(data);
        if (clip == null)
            return;

        currentLoopId = id;
        currentLoopData = data;
        currentLoopOptions = options;

        ApplyMixerGroup(loopSource, data.mixerGroup);
        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.pitch = CalculatePitch(data, options);
        float loopVolume = CalculateVolume(data, options);
        currentLoopBaseVolume = loopVolume;
        loopSource.volume = audioMixer == null
            ? loopVolume * userSfxVolume
            : loopVolume;
        loopSource.Play();
    }

    public void StopLoop()
    {
        currentLoopData = null;
        currentLoopOptions = SoundPlayOptions.Default;
        currentLoopId = SoundId.None;
        currentLoopBaseVolume = 1f;

        if (loopSource == null)
            return;

        loopSource.Stop();
        loopSource.clip = null;
    }

    public void SetLoopRatio(float ratio)
    {
        if (loopSource == null || !loopSource.isPlaying || currentLoopData == null)
            return;

        currentLoopOptions.ratio = ratio;
        loopSource.pitch = CalculatePitch(currentLoopData, currentLoopOptions);
    }

    private bool TryGetSoundData(SoundId id, out SoundData data)
    {
        if (soundMap.TryGetValue(id, out data))
            return true;

        Debug.LogWarning($"SoundManager: SoundData for {id} is missing.");
        return false;
    }

    private AudioClip GetRandomClip(SoundData data)
    {
        if (data.clips == null || data.clips.Length == 0)
        {
            Debug.LogWarning($"SoundManager: AudioClip for {data.id} is empty.");
            return null;
        }

        return data.clips[Random.Range(0, data.clips.Length)];
    }

    private float CalculatePitch(SoundData data, SoundPlayOptions options)
    {
        float ratio = Mathf.Clamp01(options.ratio);
        float pitch = Mathf.Lerp(data.basePitch, data.maxPitch, ratio);
        pitch += Random.Range(-data.pitchRandomRange, data.pitchRandomRange);
        pitch *= options.pitchScale;
        return Mathf.Clamp(pitch, 0.1f, 3f);
    }

    private float CalculateVolume(SoundData data, SoundPlayOptions options)
    {
        float volume = data.baseVolume;
        volume += Random.Range(-data.volumeRandomRange, data.volumeRandomRange);
        volume *= options.volumeScale;
        return Mathf.Clamp01(volume);
    }

    private bool CanPlayByInterval(SoundData data)
    {
        if (data.minInterval <= 0f)
            return true;

        if (lastPlayTimes.TryGetValue(data.id, out float lastPlayTime) &&
            Time.time - lastPlayTime < data.minInterval)
        {
            return false;
        }

        return true;
    }

    private AudioSource GetSourceFor(SoundId id)
    {
        string idName = id.ToString();
        if (idName == "UIClick" || idName == "GaugeSegmentFilled")
            return uiSfxSource != null ? uiSfxSource : defaultSfxSource;

        return defaultSfxSource;
    }

    private void ApplyMixerGroup(AudioSource source, AudioMixerGroup mixerGroup)
    {
        if (mixerGroup != null)
            source.outputAudioMixerGroup = mixerGroup;
    }

    public void PlayTitleBgm()
    {
        PlayBgm(SoundId.TitleBGM);
    }

    public void PlayGameplayBgm()
    {
        PlayBgm(SoundId.GameplayBGM);
    }

    public void PlayBgm(SoundId id)
    {
        if (currentBgmId == id && bgmSource != null && bgmSource.isPlaying)
            return;

        if (!TryGetSoundData(id, out SoundData data))
            return;

        if (data.soundType != SoundType.BGM)
        {
            Debug.LogWarning($"SoundManager: {id} is not configured as BGM.");
            return;
        }

        AudioClip clip = GetRandomClip(data);
        if (clip == null)
            return;

        PlayBgmInternal(id, data, clip);
    }

    private void PlayBgmInternal(SoundId id, SoundData data, AudioClip clip)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("SoundManager: BGM AudioSource is not assigned.");
            return;
        }

        if (currentBgmId == id && bgmSource.isPlaying)
            return;

        if (bgmSource.isPlaying)
            bgmSource.Stop();

        ApplyMixerGroup(bgmSource, data.mixerGroup);
        bgmSource.loop = true;
        bgmSource.pitch = Mathf.Clamp(data.basePitch, 0.1f, 3f);
        float baseVolume = Mathf.Clamp01(data.baseVolume);
        bgmSource.volume = audioMixer == null
            ? baseVolume * userBgmVolume
            : baseVolume;
        bgmSource.clip = clip;
        bgmSource.Play();
        currentBgmId = id;
        ApplyBgmMuffleState();
    }

    public void StopBgm()
    {
        currentBgmId = SoundId.None;

        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void StopBGM()
    {
        StopBgm();
    }

    public void SetVolume(SoundType type, float volume)
    {
        if (type == SoundType.BGM)
            SetBgmVolume(volume);
        else
            SetSfxVolume(volume);
    }

    public void SetBgmVolume(float normalizedVolume)
    {
        userBgmVolume = Mathf.Clamp01(normalizedVolume);

        if (audioMixer != null)
        {
            audioMixer.SetFloat(BgmVolumeParameter, LinearToDb(userBgmVolume));
            return;
        }

        if (bgmSource != null)
        {
            float baseVolume = currentBgmId != SoundId.None &&
                               soundMap.TryGetValue(currentBgmId, out SoundData data)
                ? Mathf.Clamp01(data.baseVolume)
                : 1f;
            bgmSource.volume = baseVolume * userBgmVolume;
        }
    }

    public void SetSfxVolume(float normalizedVolume)
    {
        userSfxVolume = Mathf.Clamp01(normalizedVolume);

        if (audioMixer != null)
        {
            audioMixer.SetFloat(SfxVolumeParameter, LinearToDb(userSfxVolume));
            return;
        }

        if (defaultSfxSource != null)
            defaultSfxSource.volume = userSfxVolume;

        if (uiSfxSource != null)
            uiSfxSource.volume = userSfxVolume;

        if (loopSource != null)
        {
            loopSource.volume = currentLoopData != null && loopSource.isPlaying
                ? currentLoopBaseVolume * userSfxVolume
                : userSfxVolume;
        }
    }

    public void SetBgmMuffled(BgmMuffleReason reason, bool enabled)
    {
        if (enabled)
            bgmMuffleReasons.Add(reason);
        else
            bgmMuffleReasons.Remove(reason);

        ApplyBgmMuffleState();
    }

    public void ClearBgmMuffles()
    {
        bgmMuffleReasons.Clear();
        ApplyBgmMuffleState();
    }

    private void ApplyBgmMuffleState()
    {
        if (audioMixer == null)
            return;

        float cutoff = bgmMuffleReasons.Count > 0
            ? muffledLowPassCutoff
            : normalLowPassCutoff;
        audioMixer.SetFloat(BgmLowPassCutoffParameter, cutoff);
    }

    private float LinearToDb(float value)
    {
        return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
    }
}
