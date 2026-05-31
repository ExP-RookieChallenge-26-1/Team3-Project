using UnityEngine;
using System.Threading;

public class GroundHp : MonoBehaviour
{
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float HP = 100f;
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageInterval = 2f; // 데미지 받는 간격

    bool IsConnected = false;
    bool game = true;

    Thread thread = null;
    
    bool[,] connected = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        connected = blockManager.GetStartConnectedCells();
        thread = new Thread(new ThreadStart(Run));
        thread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        connected = blockManager.GetStartConnectedCells();
        for (int i = 0; i < 7; i++)
        {
            if (connected[i, 17])
            {
                IsConnected = true;
                break;
            }
            else
                IsConnected = false;
        }

        if (HP > maxHP)
            HP = maxHP;

        if (HP <= 0)
        {
            gameManager.RequestGameOver();
            Debug.Log("게임 오버");
        }


    } 

    void Run()
    {
        while (true) { 
            while (IsConnected)
            {
                Thread.Sleep((int)(damageInterval * 1000f));
                HP -= damage;
                Debug.Log($"데미지 받음 / 남은 HP: {HP}");
            }
        }
    }

    void OnApplicationQuit()
    {
        thread.Abort();
    }


}
