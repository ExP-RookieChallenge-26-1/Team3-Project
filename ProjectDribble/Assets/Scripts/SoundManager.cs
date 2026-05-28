using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    BGM,
    SFX
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; } 
    //싱글톤
    // 다른 소리 트리거하는 쪽에서 Instance에 get은 할 수 있지만 set은 못하도록

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Preload Clip Names")]
    [SerializeField] private string[] audioClipNames;

    private readonly Dictionary<string, AudioClip> _clipDict = new();

    private void Awake() //싱글톤
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
        // bgm은 기본이 루프니까

        foreach (string clipName in audioClipNames)
        {
            GetClip(clipName);
        }
    }

    public void Play2D(
        string clipName,
        SoundType type = SoundType.SFX,
        float pitch = 1f
    )
    {
        AudioClip clip = GetClip(clipName);

        if (clip == null)
        {
            Debug.LogWarning($"SoundManager: {clipName} 클립을 찾을 수 없습니다.");
            return;
        }

        if (type == SoundType.BGM)
        {
            PlayBGM(clip, pitch);
        }
        else
        {
            PlaySFX(clip, pitch);
        }
    }

    private void PlayBGM(AudioClip clip, float pitch)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("SoundManager: BGM AudioSource가 없습니다.");
            return;
        }

        if (bgmSource.isPlaying)
            bgmSource.Stop();

        bgmSource.pitch = pitch;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private void PlaySFX(AudioClip clip, float pitch) 
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("SoundManager: SFX AudioSource가 없습니다.");
            return;
        }

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    public void StopBGM() // 나중에 UI에 연결
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
    }

    public void SetVolume(SoundType type, float volume) // 나중에 UI에 연결
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

    public AudioClip GetClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return null;

        if (_clipDict.TryGetValue(clipName, out AudioClip clip))
            return clip;

        clip = Resources.Load<AudioClip>($"Sounds/{clipName}");

        if (clip != null)
        {
            _clipDict.Add(clipName, clip);
        }

        return clip;
    }

    public void ClearCache()
    {
        _clipDict.Clear();
    }
}