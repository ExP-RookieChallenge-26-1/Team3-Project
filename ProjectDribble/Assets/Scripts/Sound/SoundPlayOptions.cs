public struct SoundPlayOptions
{
    public float ratio;
    public float volumeScale;
    public float pitchScale;

    public static SoundPlayOptions Default => new SoundPlayOptions
    {
        ratio = 0f,
        volumeScale = 1f,
        pitchScale = 1f
    };
}
