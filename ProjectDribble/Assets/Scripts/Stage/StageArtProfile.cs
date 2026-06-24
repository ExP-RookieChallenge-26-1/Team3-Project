using UnityEngine;

[CreateAssetMenu(fileName = "StageArtProfile", menuName = "ScriptableObjects/StageArtProfile")]
public class StageArtProfile : ScriptableObject
{
    [Header("Blocks")]
    [SerializeField] private Sprite fixedBlockSprite;

    [Header("Screen Glitch Overlay")]
    [SerializeField] private bool useScreenGlitchOverlay;
    [SerializeField] private Sprite screenGlitchOverlaySprite;
    [SerializeField] private Color screenGlitchOverlayColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float screenGlitchOverlayAlpha = 1f;
    [SerializeField] private bool animateOverlay;
    [SerializeField, Min(0f)] private float overlayPulseSpeed = 1f;
    [SerializeField] private bool useUnscaledTime = true;

    public Sprite FixedBlockSprite => fixedBlockSprite;
    public bool UseScreenGlitchOverlay => useScreenGlitchOverlay;
    public Sprite ScreenGlitchOverlaySprite => screenGlitchOverlaySprite;
    public Color ScreenGlitchOverlayColor => screenGlitchOverlayColor;
    public float ScreenGlitchOverlayAlpha => screenGlitchOverlayAlpha;
    public bool AnimateOverlay => animateOverlay;
    public float OverlayPulseSpeed => overlayPulseSpeed;
    public bool UseUnscaledTime => useUnscaledTime;

    private void OnValidate()
    {
        screenGlitchOverlayAlpha = Mathf.Clamp01(screenGlitchOverlayAlpha);
        overlayPulseSpeed = Mathf.Max(0f, overlayPulseSpeed);
    }
}
