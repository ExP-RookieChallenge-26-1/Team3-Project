using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lifeTime = 5f;

    [SerializeField] private float stunDuration = 0.5f;

    [Header("Size")]
    [SerializeField] private Vector2 size = new Vector2(0.3f, 0.3f);

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.down * moveSpeed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PaddleDebuff debuff = other.GetComponentInParent<PaddleDebuff>();

        if (debuff != null)
        {
            debuff.ApplyStun(stunDuration);
            Destroy(gameObject);
            return;
        }
    }
}