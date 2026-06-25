using UnityEngine;

public class BallSpeedColorController : MonoBehaviour
{
    [SerializeField] private BallData data;
    [SerializeField] private BallSpeedController speedController;
    [SerializeField] private BallMovement ballMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite midSpeedSprite;
    [SerializeField] private Sprite highSpeedSprite;

    private Sprite defaultSprite;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        AutoAssignReferences();
        CacheDefaultSprite();
    }

    private void Update()
    {
        if (data == null || !data.useSpeedColorChange || spriteRenderer == null)
            return;

        float currentSpeed = GetCurrentSpeed();
        Sprite speedSprite = GetSpriteForSpeed(currentSpeed);

        if (speedSprite != null && spriteRenderer.sprite != speedSprite)
            spriteRenderer.sprite = speedSprite;

        Color currentColor = spriteRenderer.color;
        spriteRenderer.color = new Color(Color.white.r, Color.white.g, Color.white.b, currentColor.a);
    }

    private void AutoAssignReferences()
    {
        if (speedController == null)
            speedController = GetComponent<BallSpeedController>();

        if (ballMovement == null)
            ballMovement = GetComponent<BallMovement>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (data == null && speedController != null)
            data = speedController.data;

        if (data == null && ballMovement != null)
            data = ballMovement.data;

        if (data == null)
        {
            BallController ballController = GetComponent<BallController>();
            if (ballController != null)
                data = ballController.data;
        }
    }

    private void CacheDefaultSprite()
    {
        if (spriteRenderer == null)
            return;

        defaultSprite = spriteRenderer.sprite;

        if (normalSprite == null)
            normalSprite = defaultSprite;
    }

    private float GetCurrentSpeed()
    {
        if (speedController != null)
            return speedController.CurrentSpeed;

        if (ballMovement != null)
            return ballMovement.speed;

        return 0f;
    }

    private Sprite GetSpriteForSpeed(float currentSpeed)
    {
        float startSpeed = data.speedColorStartSpeed;
        float endSpeed = data.speedColorEndSpeed;

        if (endSpeed <= startSpeed)
            return currentSpeed <= startSpeed ? GetNormalSprite() : GetHighSpeedSprite();

        if (currentSpeed <= startSpeed)
            return GetNormalSprite();

        if (currentSpeed >= endSpeed)
            return GetHighSpeedSprite();

        return GetMidSpeedSprite();
    }

    private Sprite GetNormalSprite()
    {
        return normalSprite != null ? normalSprite : defaultSprite;
    }

    private Sprite GetMidSpeedSprite()
    {
        if (midSpeedSprite != null)
            return midSpeedSprite;

        return GetNormalSprite();
    }

    private Sprite GetHighSpeedSprite()
    {
        if (highSpeedSprite != null)
            return highSpeedSprite;

        return GetMidSpeedSprite();
    }
}
