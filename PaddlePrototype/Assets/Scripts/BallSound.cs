using UnityEngine;

public class BallSound : MonoBehaviour
{
    public enum SoundType
    {
        Bounce,
        PaddleBounce,
        Charging,
        Fire
    }

    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip paddleBounceClip;
    [SerializeField] private AudioClip bounceClip;
    [SerializeField] private AudioClip chargingClip;
    [SerializeField] private AudioClip fireClip;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.4f;

    [Header("Option")]
    [SerializeField] private bool isCharging = false;

    public void SetCharging(bool charging)
    {
        isCharging = charging;

        if (isCharging)
        {
            StartChargingSound(0f);
        }
        else
        {
            StopChargingSound();
        }
    }

    public void Play(float level, SoundType soundType)
    {
        level = Mathf.Clamp01(level);

        if (isCharging && soundType == SoundType.Bounce)
            return;

        switch (soundType)
        {
            case SoundType.Bounce:
                PlayBounce(level);
                break;

            case SoundType.Charging:
                StartChargingSound(level);
                break;

            case SoundType.Fire:
                StopChargingSound();
                PlayFire(level);
                break;
            case SoundType.PaddleBounce:
                PlayPaddleBounce(level);
                break;
        }
    }

    private void PlayPaddleBounce(float level)
    {
        if (paddleBounceClip == null)
            return;

        audioSource.pitch = Mathf.Lerp(minPitch-0.2f, maxPitch-0.2f, level-0.2f);
        audioSource.PlayOneShot(paddleBounceClip);

        Debug.Log("Sound Play: PaddleBounce");
    }
    private void PlayBounce(float level)
    {
        if (bounceClip == null)
            return;

        audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, level);
        audioSource.PlayOneShot(bounceClip);

        Debug.Log("Sound Play: Bounce");
    }

    private void StartChargingSound(float level)
    {
        if (chargingClip == null)
            return;

        audioSource.pitch = minPitch;
            //Mathf.Lerp(minPitch, maxPitch, level);

        if (audioSource.clip == chargingClip && audioSource.isPlaying)
            return;

        audioSource.clip = chargingClip;
        audioSource.loop = true;
        audioSource.Play();

        Debug.Log("Sound Play: Charging");
    }

    private void StopChargingSound()
    {
        if (audioSource.clip != chargingClip)
            return;

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;

        Debug.Log("Sound Stop: Charging");
    }

    private void PlayFire(float level)
    {
        if (fireClip == null)
            return;

        audioSource.pitch = minPitch;
        //Mathf.Lerp(minPitch, maxPitch, level);
        audioSource.PlayOneShot(fireClip);

        Debug.Log("Sound Play: Fire");
    }
}