using UnityEngine;

public class BossPaddleMover : MonoBehaviour
{
    [SerializeField] private Transform moveTarget;
    [SerializeField] private float moveRange = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool resetPositionOnStop;

    private Vector3 startPosition;
    private bool isMoving;
    private float moveElapsed;

    private Transform Target => moveTarget != null ? moveTarget : transform;

    private void Awake()
    {
        startPosition = Target.position;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        moveElapsed += Time.deltaTime;

        Vector3 position = Target.position;
        float offsetX = Mathf.Sin(moveElapsed * moveSpeed) * moveRange;
        position.x = startPosition.x + offsetX;
        position.y = startPosition.y;
        position.z = startPosition.z;
        Target.position = position;
    }

    public void StartMoving()
    {
        startPosition = Target.position;
        moveElapsed = 0f;
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;

        if (resetPositionOnStop)
            Target.position = startPosition;
    }
}
