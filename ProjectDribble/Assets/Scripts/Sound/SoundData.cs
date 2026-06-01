using DefaultNamespace;
using UnityEngine;

[System.Serializable]
public class SoundData
{
    public SoundId soundId;
    public SoundType soundType;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    public float basePitch = 1f;
    public bool usePitchByRatio;
    public float minPitch = 0.9f;
    public float maxPitch = 1.2f;
}