using UnityEngine;
using System.Collections;

public class PlayerDamagedManager : MonoBehaviour
{
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HealthData data;

    private bool isConnected;
    private Coroutine damageCoroutine;

    private void OnDisable()
    {
        StopDamageRoutine();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameplayDamageAllowed)
        {
            StopDamageRoutine();
            return;
        }

        CheckConnected();

        if (isConnected)
        {
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DamageRoutine());
            }
        }
        else
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    public void StopDamageRoutine()
    {
        isConnected = false;

        if (damageCoroutine == null)
            return;

        StopCoroutine(damageCoroutine);
        damageCoroutine = null;
    }

    private void CheckConnected()
    {
        if (blockManager == null)
        {
            isConnected = false;
            return;
        }

        bool[,] connected = blockManager.GetStartConnectedCells();

        isConnected = false;

        int width = connected.GetLength(0);
        int height = connected.GetLength(1);

        if (width <= 0 || height <= 19)
            return;

        int maxX = Mathf.Min(7, width);

        for (int i = 0; i < maxX; i++)
        {
            if (connected[i, 19])
            {
                isConnected = true;
                break;
            }
        }
    }

    private IEnumerator DamageRoutine()
    {
        while (isConnected)
        {
            if (playerHealth != null && data != null)
                playerHealth.TakeDamage(data.damagePerTick);
            Debug.Log("데미지 받기");

            float interval = data != null ? data.damageInterval : 1f;
            yield return new WaitForSeconds(interval);
        }

        damageCoroutine = null;
    }
}
