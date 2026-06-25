using UnityEngine;
using UnityEngine.UI;

public class StageArtManager : MonoBehaviour
{
    [Header("Screen Glitch Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Image overlayImage;
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Header("Screen Decor Prefab")]
    [SerializeField] private Transform screenDecorRoot;

    private StageArtProfile currentProfile;
    private float baseOverlayAlpha;
    private GameObject currentScreenDecorInstance;

    private void Awake()
    {
        ConfigureOverlayRaycastBlocking();
        ResetToDefault();
    }

    private void Update()
    {
        if (currentProfile == null ||
            !currentProfile.AnimateOverlay ||
            currentProfile.OverlayPulseSpeed <= 0f ||
            overlayCanvasGroup == null ||
            overlayRoot == null ||
            !overlayRoot.activeSelf)
        {
            return;
        }

        float time = currentProfile.UseUnscaledTime ? Time.unscaledTime : Time.time;
        float pulse01 = 0.5f + 0.5f * Mathf.Sin(
            time * currentProfile.OverlayPulseSpeed * Mathf.PI * 2f
        );

        overlayCanvasGroup.alpha = baseOverlayAlpha * Mathf.Lerp(0.65f, 1f, pulse01);
    }

    private void OnDisable()
    {
        ResetToDefault();
    }

    public void Apply(StageArtProfile profile)
    {
        currentProfile = profile;
        ApplyScreenDecor(profile);

        if (profile == null)
        {
            ResetOverlayToDefault();
            return;
        }

        if (!profile.UseScreenGlitchOverlay || profile.ScreenGlitchOverlaySprite == null)
        {
            ResetOverlayToDefault();
            return;
        }

        baseOverlayAlpha = profile.ScreenGlitchOverlayAlpha;

        if (overlayImage != null)
        {
            overlayImage.sprite = profile.ScreenGlitchOverlaySprite;
            overlayImage.color = profile.ScreenGlitchOverlayColor;
            overlayImage.raycastTarget = false;
        }

        ConfigureOverlayRaycastBlocking();

        if (overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = baseOverlayAlpha;

        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    public void ResetToDefault()
    {
        currentProfile = null;
        ClearScreenDecor();
        ResetOverlayToDefault();
    }

    private void ResetOverlayToDefault()
    {
        baseOverlayAlpha = 0f;

        if (overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = 0f;

        if (overlayImage != null)
        {
            overlayImage.sprite = null;
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;
        }

        ConfigureOverlayRaycastBlocking();

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void ApplyScreenDecor(StageArtProfile profile)
    {
        ClearScreenDecor();

        if (profile == null || profile.ScreenDecorPrefab == null)
            return;

        if (screenDecorRoot == null)
        {
            Debug.LogWarning(
                $"StageArtManager: ScreenDecorRoot is not assigned for art profile '{profile.name}'.",
                this
            );
            return;
        }

        currentScreenDecorInstance = Instantiate(
            profile.ScreenDecorPrefab,
            screenDecorRoot,
            false
        );
    }

    private void ClearScreenDecor()
    {
        if (currentScreenDecorInstance == null)
            return;

        currentScreenDecorInstance.SetActive(false);
        Destroy(currentScreenDecorInstance);
        currentScreenDecorInstance = null;
    }

    private void ConfigureOverlayRaycastBlocking()
    {
        if (overlayImage != null)
            overlayImage.raycastTarget = false;

        if (overlayCanvasGroup == null)
            return;

        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;
    }
}
