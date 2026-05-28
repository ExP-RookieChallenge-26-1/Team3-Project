using System;
using DefaultNamespace;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SoundDatabase",
    menuName = "Sound/Sound Database"
)]
public class SoundDatabase : ScriptableObject
{
    public SoundData[] sounds;
}

[Serializable]
public class SoundData
{
    public SoundId soundId;
    public SoundType soundType = SoundType.SFX;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    public bool usePitchByRatio = false;
    public float basePitch = 1f;
    public float minPitch = 0.9f;
    public float maxPitch = 1.2f;
}