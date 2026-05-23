using UnityEngine;

public class BallSound : MonoBehaviour
{
    public enum SoundType
    {
        Bounce,
        Charging,
        Fire
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip bounceClip;
    [SerializeField] private AudioClip chargingClip;
    [SerializeField] private AudioClip fireClip;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.4f;
    [SerializeField] private float randomPitchRange = 0.03f;

    [Header("State")]
    [SerializeField] private bool isCharging = false;

    [Header("Bounce Option")]
    [SerializeField] private float bounceSoundCooldown = 0.03f;

    private float lastBounceSoundTime = -999f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void SetCharging(bool charging)
    {
        isCharging = charging;
    }

    public void Play(float level, SoundType soundType)
    {
        level = Mathf.Clamp01(level);

        if (isCharging && soundType == SoundType.Bounce)
            return;

        AudioClip clipToPlay = GetClip(soundType);

        if (clipToPlay == null)
        {
            Debug.LogWarning($"[BallSound] Clip is missing: {soundType}");
            return;
        }

        if (soundType == SoundType.Bounce)
        {
            if (Time.time - lastBounceSoundTime < bounceSoundCooldown)
                return;

            lastBounceSoundTime = Time.time;
        }

        float pitch = Mathf.Lerp(minPitch, maxPitch, level);

        if (soundType == SoundType.Bounce)
            pitch += Random.Range(-randomPitchRange, randomPitchRange);

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clipToPlay);
    }

    public void PlayBounceBySpeed(float currentSpeed, float baseSpeed, float maxSpeed)
    {
        float level = Mathf.InverseLerp(baseSpeed, maxSpeed, currentSpeed);
        Play(level, SoundType.Bounce);
    }

    public void PlayCharging(float chargeLevel)
    {
        SetCharging(true);
        Play(chargeLevel, SoundType.Charging);
    }

    public void PlayFire(float chargeLevel)
    {
        SetCharging(false);
        Play(chargeLevel, SoundType.Fire);
    }

    private AudioClip GetClip(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.Bounce:
                return bounceClip;

            case SoundType.Charging:
                return chargingClip;

            case SoundType.Fire:
                return fireClip;

            default:
                return null;
        }
    }
}