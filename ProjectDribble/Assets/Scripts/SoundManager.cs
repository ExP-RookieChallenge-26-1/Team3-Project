using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public enum SoundType
{
    BGM,
    SFX
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Data")]
    [SerializeField] private SoundData[] soundDatas;

    private readonly Dictionary<SoundId, SoundData> soundDataDict = new();

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

        soundDataDict.Clear();

        foreach (SoundData data in soundDatas)
        {
            if (data == null)
                continue;

            if (soundDataDict.ContainsKey(data.soundId))
            {
                Debug.LogWarning($"SoundManager: {data.soundId}가 중복 등록되었습니다.");
                continue;
            }

            soundDataDict.Add(data.soundId, data);
        }
    }

    public void Play2D(SoundId id, float pitchRatio = -1f)
    {
        if (!soundDataDict.TryGetValue(id, out SoundData data))
        {
            Debug.LogWarning($"SoundManager: {id} 사운드 데이터가 없습니다.");
            return;
        }

        if (data.clip == null)
        {
            Debug.LogWarning($"SoundManager: {id} AudioClip이 비어 있습니다.");
            return;
        }

        float pitch = GetPitch(data, pitchRatio);

        if (data.soundType == SoundType.BGM)
        {
            PlayBGM(data.clip, pitch, data.volume);
        }
        else
        {
            PlaySFX(data.clip, pitch, data.volume);
        }
    }

    private float GetPitch(SoundData data, float pitchRatio)
    {
        if (data.usePitchByRatio && pitchRatio >= 0f)
        {
            pitchRatio = Mathf.Clamp01(pitchRatio);
            return Mathf.Lerp(data.minPitch, data.maxPitch, pitchRatio);
        }

        return data.basePitch;
    }

    private void PlayBGM(AudioClip clip, float pitch, float volume)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("SoundManager: BGM AudioSource가 없습니다.");
            return;
        }

        if (bgmSource.isPlaying)
            bgmSource.Stop();

        bgmSource.pitch = pitch;
        bgmSource.volume = volume;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private void PlaySFX(AudioClip clip, float pitch, float volume)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("SoundManager: SFX AudioSource가 없습니다.");
            return;
        }

        sfxSource.pitch = pitch;
        sfxSource.volume = volume;
        sfxSource.PlayOneShot(clip);
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
            if (sfxSource != null)
                sfxSource.volume = volume;
        }
    }
}