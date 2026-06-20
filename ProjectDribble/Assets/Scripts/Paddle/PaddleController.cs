using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : MonoBehaviour
{
    [SerializeField] private PointerInputReader inputReader;
    [SerializeField] private SpriteRenderer upPaddleSpriteRenderer;
    [SerializeField] private bool debugPaddleActiveState;

    private const float TransparentAlpha = 0.3f;

    public PaddleData data;

    private Camera mainCamera;
    private float moveSpeed;
    private float paddleWidth;
    private float velocityX;
    private bool lastLoggedPaddleActive;
    private bool hasAttemptedUpPaddleSpriteRendererResolve;
    private bool hasLoggedMissingUpPaddleSpriteRenderer;

    public bool IsPaddleActive => inputReader != null && inputReader.IsPressed;
    public float VelocityX => velocityX;
    public Vector2 Velocity => new Vector2(velocityX, 0f);

    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.position;
        ResolveUpPaddleSpriteRenderer();
    }

    private void Start()
    {
        moveSpeed = data.moveSpeed;
        paddleWidth = data.paddleWidth;
        mainCamera = Camera.main;

        SetReflectColliderEnabled("paddle_up", IsPaddleActive);
        SetReflectColliderEnabled("roof_paddle", IsPaddleActive);
        EnsureCaptureTriggersEnabled();

        lastLoggedPaddleActive = !IsPaddleActive;
        LogPaddleStateIfChanged();
    }

    private void Update()
    {
        float xBeforeMove = transform.position.x;

        if (IsPaddleActive)
        {
            MovePad(inputReader.ScreenPosition);
            SetUpPaddleAlpha(1f);
        }
        else
        {
            SetUpPaddleAlpha(TransparentAlpha);
        }

        SetReflectColliderEnabled("paddle_up", IsPaddleActive);
        SetReflectColliderEnabled("roof_paddle", IsPaddleActive);
        EnsureCaptureTriggersEnabled();

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        velocityX = (transform.position.x - xBeforeMove) / deltaTime;

        LogPaddleStateIfChanged();
    }

    public void ResetPosition()
    {
        transform.position = initialPosition;
    }

    private void MovePad(Vector2 screenPosition)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -mainCamera.transform.position.z
            )
        );

        Vector3 targetPos = new Vector3(
            worldPos.x,
            transform.position.y,
            transform.position.z
        );

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        float clampedX = Mathf.Clamp(transform.position.x, -6.55f, 6.55f);

        transform.position = new Vector3(
            clampedX,
            transform.position.y,
            transform.position.z
        );
    }

    private void ResolveUpPaddleSpriteRenderer()
    {
        if (upPaddleSpriteRenderer != null || hasAttemptedUpPaddleSpriteRendererResolve)
            return;

        hasAttemptedUpPaddleSpriteRendererResolve = true;
        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i].name == "UpPaddleSprite")
            {
                upPaddleSpriteRenderer = childRenderers[i];
                return;
            }
        }
    }

    private void SetUpPaddleAlpha(float targetAlpha)
    {
        if (upPaddleSpriteRenderer == null)
            ResolveUpPaddleSpriteRenderer();

        if (upPaddleSpriteRenderer == null)
        {
            if (!hasLoggedMissingUpPaddleSpriteRenderer)
            {
                Debug.LogWarning("UpPaddleSprite SpriteRenderer was not assigned or found.", this);
                hasLoggedMissingUpPaddleSpriteRenderer = true;
            }

            return;
        }

        Color color = upPaddleSpriteRenderer.color;
        upPaddleSpriteRenderer.color = new Color(color.r, color.g, color.b, targetAlpha);
    }

    public void SetPaddleCollider(string childName, bool isActive)
    {
        SetReflectColliderEnabled(childName, isActive);
    }

    private void SetReflectColliderEnabled(string childName, bool isActive)
    {
        Transform childTransform = transform.Find(childName);

        if (childTransform == null)
        {
            Debug.LogWarning($"{childName} child object was not found.");
            return;
        }
        // 콜라이더 여러개 적용을 위해
        Collider2D[] colliders = childTransform.GetComponents<Collider2D>();

        foreach (Collider2D col in colliders)
        {
            if (col == null)
            {
                Debug.LogWarning($"{childName} has no Collider2D.");
                return;
            }
            
            col.enabled = isActive;
        }
        
    }

    private void EnsureCaptureTriggersEnabled()
    {
        PaddleCaptureZone[] entrances = GetComponentsInChildren<PaddleCaptureZone>(true);

        for (int i = 0; i < entrances.Length; i++)
            EnsureTriggerEnabled(entrances[i].GetComponent<Collider2D>());

        PaddleInactiveCaptureTrigger[] inactiveTriggers =
            GetComponentsInChildren<PaddleInactiveCaptureTrigger>(true);

        for (int i = 0; i < inactiveTriggers.Length; i++)
            EnsureTriggerEnabled(inactiveTriggers[i].GetComponent<Collider2D>());
    }

    private void EnsureTriggerEnabled(Collider2D trigger)
    {
        if (trigger == null)
            return;

        trigger.enabled = true;
        trigger.isTrigger = true;
    }

    private void LogPaddleStateIfChanged()
    {
        if (!debugPaddleActiveState)
            return;

        if (lastLoggedPaddleActive == IsPaddleActive)
            return;

        lastLoggedPaddleActive = IsPaddleActive;

        bool reflectColliderEnabled =
            IsColliderEnabled("paddle_up") || IsColliderEnabled("roof_paddle");
        bool dribbleTriggerEnabled = IsAnyCaptureTriggerEnabled();

        Debug.Log(
            $"[PaddleState] active={IsPaddleActive}, reflectCollider={reflectColliderEnabled}, dribbleTrigger={dribbleTriggerEnabled}"
        );
    }

    private bool IsColliderEnabled(string childName)
    {
        Transform childTransform = transform.Find(childName);

        if (childTransform == null)
            return false;

        Collider2D col = childTransform.GetComponent<Collider2D>();
        return col != null && col.enabled;
    }

    private bool IsAnyCaptureTriggerEnabled()
    {
        PaddleCaptureZone[] entrances = GetComponentsInChildren<PaddleCaptureZone>(true);

        for (int i = 0; i < entrances.Length; i++)
        {
            Collider2D trigger = entrances[i].GetComponent<Collider2D>();

            if (trigger != null && trigger.enabled && trigger.isTrigger)
                return true;
        }

        PaddleInactiveCaptureTrigger[] inactiveTriggers =
            GetComponentsInChildren<PaddleInactiveCaptureTrigger>(true);

        for (int i = 0; i < inactiveTriggers.Length; i++)
        {
            Collider2D trigger = inactiveTriggers[i].GetComponent<Collider2D>();

            if (trigger != null && trigger.enabled && trigger.isTrigger)
                return true;
        }

        return false;
    }

    public void SetPaddleSpeed(float amount)
    {
        moveSpeed = amount;
    }

    public void AddPaddleSpeed(float amount)
    {
        moveSpeed += amount;
    }
}
