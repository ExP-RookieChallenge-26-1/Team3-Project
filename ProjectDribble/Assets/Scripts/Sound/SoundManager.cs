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

    [Header("Sound Data")]
    [SerializeField] private SoundData[] soundDatas;

    private readonly Dictionary<SoundId, SoundData> soundMap = new();
    private readonly Dictionary<SoundId, float> lastPlayTimes = new();
    private SoundData currentLoopData;
    private SoundPlayOptions currentLoopOptions;
    private SoundId currentLoopId = SoundId.None;

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
        float maxSpeed = ballSpeedController.data.maxSpeed;

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

        float pitch = CalculatePitch(data, options);
        float volume = CalculateVolume(data, options);

        if (data.soundType == SoundType.BGM)
        {
            PlayBGM(clip, pitch, volume, data.mixerGroup);
            return;
        }

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
        loopSource.volume = CalculateVolume(data, options);
        loopSource.Play();
    }

    public void StopLoop()
    {
        currentLoopData = null;
        currentLoopOptions = SoundPlayOptions.Default;
        currentLoopId = SoundId.None;

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

    private void PlayBGM(AudioClip clip, float pitch, float volume, AudioMixerGroup mixerGroup = null)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("SoundManager: BGM AudioSource is not assigned.");
            return;
        }

        if (bgmSource.isPlaying)
            bgmSource.Stop();

        ApplyMixerGroup(bgmSource, mixerGroup);
        bgmSource.pitch = pitch;
        bgmSource.volume = volume;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
    }

    public void SetVolume(SoundType type, float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (type == SoundType.BGM)
        {
            if (bgmSource != null)
                bgmSource.volume = volume;
        }
        else
        {
            if (defaultSfxSource != null)
                defaultSfxSource.volume = volume;

            if (uiSfxSource != null)
                uiSfxSource.volume = volume;

            if (loopSource != null)
                loopSource.volume = volume;
        }
    }
}
