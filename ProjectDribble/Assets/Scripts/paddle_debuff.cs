using UnityEngine;

public class PaddleDebuff : MonoBehaviour
{
    private bool isStunned;
    private float stunTimer;

    public bool IsStunned => isStunned;

    private void Update()
    {
        if (!isStunned)
            return;

        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
            isStunned = false;
    }

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
    }
}