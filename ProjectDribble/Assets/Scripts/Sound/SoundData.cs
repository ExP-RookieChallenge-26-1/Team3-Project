using DefaultNamespace;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

[System.Serializable]
public class SoundData : ISerializationCallbackReceiver
{
    public SoundId id;

    public AudioClip[] clips;

    [Header("BGM")]
    [Tooltip("Optional loop clip played after clips[0]. Leave empty to loop clips[0] as before.")]
    public AudioClip loopClip;

    [SerializeField, FormerlySerializedAs("clip")]
    private AudioClip legacyClip;

    [Header("Category")]
    public SoundType soundType = SoundType.SFX;

    [Header("Volume")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("volume")]
    public float baseVolume = 1f;

    public float volumeRandomRange = 0f;

    [Header("Pitch")]
    public float basePitch = 1f;
    public float maxPitch = 1.2f;
    public float pitchRandomRange = 0.03f;

    [Header("Mixer")]
    public AudioMixerGroup mixerGroup;

    [Header("Limit")]
    public float minInterval = 0f;

    [HideInInspector]
    public bool usePitchByRatio;

    [HideInInspector]
    public float minPitch = 0.9f;

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if ((clips == null || clips.Length == 0) && legacyClip != null)
        {
            clips = new[] { legacyClip };
        }
    }
}
