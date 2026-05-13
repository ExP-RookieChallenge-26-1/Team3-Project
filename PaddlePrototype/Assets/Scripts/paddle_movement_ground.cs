using ScriptableObjectScripts;
using UnityEngine;
using UnityEngine.InputSystem; // 1. 네임스페이스 추가

public class GroundPadController : MonoBehaviour
{
    
    [SerializeField] private PaddleData paddleData;
    
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float paddleWidth = 0.7f;
    [SerializeField] private float activeCollisionEnabled = 1f;
    private Camera mainCamera;

    private Collider2D paddleCollider;
    private Transform tr;
    private bool isCollisionOn = true;

    //디버프
    private PaddleDebuff paddleDebuff;


    void Start()
    {
        moveSpeed = paddleData.moveSpeed;
        paddleWidth = paddleData.paddleWidth;
        activeCollisionEnabled = paddleData.activeCollisionEnabled;

        
        
        
        mainCamera = Camera.main;
        paddleCollider = GetComponent<Collider2D>();
        tr = GetComponent<Transform>();
        if(gameObject.name.Contains("paddle_down"))
        {
           tr.localScale = new Vector3(paddleWidth,0.5f,1f);
        }
        if(activeCollisionEnabled != 1)
        {
            paddleCollider.enabled = false;
        }

        //디버프
        paddleDebuff = GetComponentInParent<PaddleDebuff>();
    }

    void Update()
    {
        //디버프 적용
        if (paddleDebuff != null && paddleDebuff.IsStunned)
            return;

        // 2. Mouse.current를 사용하여 클릭 체크
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            MovePad(Touchscreen.current.primaryTouch.position.ReadValue());
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            MovePad(Mouse.current.position.ReadValue());
        }
    }

    void MovePad(Vector2 screenPos)
    {
        // 3. 마우스 위치 읽기
        Vector2 mousePixelPos = screenPos;
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(new Vector3(mousePixelPos.x, mousePixelPos.y, -mainCamera.transform.position.z));
        
        Vector3 targetPos = new Vector3(mousePos.x, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        float clampedX = Mathf.Clamp(transform.position.x, -5.5f, 5.5f);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }
}