using UnityEngine;
using UnityEngine.InputSystem; // 1. 네임스페이스 추가
using UnityEngine.U2D;

public class PaddleMovement : MonoBehaviour
{
    [SerializeField] private float activeCollisionEnabled = 1f;
    private float transparentAlpha = 0.3f;
    private Camera mainCamera;

    //private SpriteShapeRenderer shapeRenderer; 
    //private Color originalColor; 

    //private Collider2D paddleCollider;

    public PaddleData data;
    private Transform tr;
    private bool isCollisionOn = true;

    Transform paddle_up;
    Transform paddle_down;
    Transform roof_paddle;

    SpriteShapeRenderer up_shape;
    Collider2D up_collider;

    float moveSpeed; 
    float paddleWidth; 


    void Start()
    {
        moveSpeed = data.moveSpeed;
        paddleWidth = data.paddleWidth;

        paddle_up = transform.Find("paddle_up");
        paddle_down = transform.Find("paddle_down");
        roof_paddle = transform.Find("roof_paddle");

        up_shape = paddle_up.GetComponent<SpriteShapeRenderer>();
        up_collider = paddle_up.GetComponent<Collider2D>();

        mainCamera = Camera.main;
        tr = GetComponent<Transform>();
        
        
    }

    void Update()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            MovePad();
            SetPaddleAlpha("paddle_up",1.0f);
            SetPaddleAlpha("roof_paddle",1.0f);
            SetPaddleCollider("paddle_up",true);
            SetPaddleCollider("roof_paddle",true);
        }
        else
        {
            SetPaddleAlpha("paddle_up",transparentAlpha);
            SetPaddleAlpha("roof_paddle",transparentAlpha);
            SetPaddleCollider("paddle_up",false);
            SetPaddleCollider("roof_paddle",false);
        }
    }

    void MovePad()
    {
        // 마우스 위치 읽기
        Vector2 mousePixelPos = Mouse.current.position.ReadValue();
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(new Vector3(mousePixelPos.x, mousePixelPos.y, -mainCamera.transform.position.z));
        
        Vector3 targetPos = new Vector3(mousePos.x, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        float clampedX = Mathf.Clamp(transform.position.x, -6.5f, 6.5f);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    public void SetPaddleAlpha(string childName, float alpha)
    {
        // 1. 이름으로 자식 오브젝트의 Transform을 찾음
        Transform childTransform = transform.Find(childName);

        if (childTransform != null)
        {
            // 2. 해당 자식의 SpriteRenderer 컴포넌트를 가져옴
            SpriteShapeRenderer spriteShapeRenderer = childTransform.GetComponent<SpriteShapeRenderer>();

            if (spriteShapeRenderer != null)
            {
                // 3. 기존 색상을 가져와서 알파(Alpha) 값만 변경 후 재대입
                Color currentColor = spriteShapeRenderer.color;
                currentColor.a = alpha; // 투명도 설정 (0.0f ~ 1.0f)
                
                spriteShapeRenderer.color = currentColor;
            }
            else
            {
                Debug.LogWarning($"{childName} 오브젝트에 SpriteRenderer가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"{childName}이라는 이름의 자식 오브젝트를 찾을 수 없습니다.");
        }
    }

    public void SetPaddleCollider(string childName, bool isActive)
    {
        // 1. 이름으로 자식 오브젝트를 찾음
        Transform childTransform = transform.Find(childName);

        if (childTransform != null)
        {
            // 2. 해당 자식의 Collider2D 컴포넌트를 가져옴 (Box, Circle, Capsule 등 모두 호환)
            Collider2D col = childTransform.GetComponent<Collider2D>();

            if (col != null)
            {
                // 3. 콜라이더 활성화 상태 변경 (true = 켜기, false = 끄기)
                col.enabled = isActive;
            }
            else
            {
                Debug.LogWarning($"{childName} 오브젝트에 Collider2D가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"{childName}이라는 이름의 자식 오브젝트를 찾을 수 없습니다.");
        }
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
