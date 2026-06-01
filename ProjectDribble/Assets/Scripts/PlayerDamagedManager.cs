using UnityEngine;
using System.Collections;

public class PlayerDamagedManager : MonoBehaviour
{
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private HealthData data;

    private bool isConnected;
    private Coroutine damageCoroutine;

    private void Update()
    {
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

    private void CheckConnected()
    {
        bool[,] connected = blockManager.GetStartConnectedCells();

        isConnected = false;

        for (int i = 0; i < 7; i++)
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
            playerHealth.TakeDamage(data.damagePerTick);
            Debug.Log("데미지 받기");

            yield return new WaitForSeconds(data.damageInterval);
        }

        damageCoroutine = null;
    }
}