using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

public class PaddleMovement : MonoBehaviour
{
    [SerializeField] private PointerInputReader inputReader;
    [SerializeField] private bool debugPaddleActiveState;

    private const float TransparentAlpha = 0.3f;

    public PaddleData data;

    private Camera mainCamera;
    private float moveSpeed;
    private float paddleWidth;
    private float velocityX;
    private bool lastLoggedPaddleActive;

    public bool IsPaddleActive => inputReader != null && inputReader.IsPressed;
    public float VelocityX => velocityX;

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
            SetPaddleAlpha("paddle_up", 1f);
            SetPaddleAlpha("roof_paddle", 1f);
        }
        else
        {
            SetPaddleAlpha("paddle_up", TransparentAlpha);
            SetPaddleAlpha("roof_paddle", TransparentAlpha);
        }

        SetReflectColliderEnabled("paddle_up", IsPaddleActive);
        SetReflectColliderEnabled("roof_paddle", IsPaddleActive);
        EnsureCaptureTriggersEnabled();

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        velocityX = (transform.position.x - xBeforeMove) / deltaTime;

        LogPaddleStateIfChanged();
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

        float clampedX = Mathf.Clamp(transform.position.x, -6.5f, 6.5f);

        transform.position = new Vector3(
            clampedX,
            transform.position.y,
            transform.position.z
        );
    }

    public void SetPaddleAlpha(string childName, float alpha)
    {
        Transform childTransform = transform.Find(childName);

        if (childTransform == null)
        {
            Debug.LogWarning($"{childName} child object was not found.");
            return;
        }

        SpriteShapeRenderer spriteShapeRenderer = childTransform.GetComponent<SpriteShapeRenderer>();

        if (spriteShapeRenderer == null)
        {
            Debug.LogWarning($"{childName} has no SpriteShapeRenderer.");
            return;
        }

        Color currentColor = spriteShapeRenderer.color;
        currentColor.a = alpha;
        spriteShapeRenderer.color = currentColor;
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

        Collider2D col = childTransform.GetComponent<Collider2D>();

        if (col == null)
        {
            Debug.LogWarning($"{childName} has no Collider2D.");
            return;
        }

        col.enabled = isActive;
    }

    private void EnsureCaptureTriggersEnabled()
    {
        PaddleCaptureEntrance[] entrances = GetComponentsInChildren<PaddleCaptureEntrance>(true);

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
        PaddleCaptureEntrance[] entrances = GetComponentsInChildren<PaddleCaptureEntrance>(true);

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
