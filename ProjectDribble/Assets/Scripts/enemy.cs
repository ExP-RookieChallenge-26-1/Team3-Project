using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IBallHitReceiver
{
    [Header("HP")]
    [SerializeField] private int maxHp = 3;
    private int currentHp;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 2f;

    private float fireTimer;

    private void Awake()
    {
        currentHp = maxHp;
    }

    private void Update()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            Fire();
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
            return;

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position;

        Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
    }
    public void OnBallHit()
    {
        currentHp--;

        Debug.Log($"Enemy Hit: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }

    public enum BlockedMoveAction
    {
        Wait,
        SkipToNextCommand
    }

    [System.Serializable]
    public class MoveCommand
    {
        [Header("Move")]
        public Vector2Int direction = Vector2Int.right;

        [Header("Repeat")]
        [Min(1)] public int repeatCount = 1;

        [Header("Section Loop")]
        public bool sectionLoopEnd = false;

        [Tooltip("�ǵ��ư� Ŀ�ǵ� ��ȣ")]
        [Min(0)] public int loopBackToIndex = 0;

        [Tooltip("���� �ݺ� Ƚ��")]
        [Min(1)] public int sectionLoopCount = 1;

        [Header("Y Limit")]
        public bool stopYMoveAtRow = false;
        public int stopYRow = 6;

        [Header("Blocked")]
        public BlockedMoveAction blockedMoveAction =
            BlockedMoveAction.SkipToNextCommand;
    }

    private BrickManager brickManager;

    private Vector2Int currentCell;
    private List<MoveCommand> commands;

    private float moveInterval;
    private float moveSpeed;
    private bool loopAllCommands;

    private int commandIndex = 0;
    private int commandRepeatCounter = 0;

    private Dictionary<int, int> sectionLoopCounters = new();

    private bool isMoving = false;
    private Coroutine moveRoutine;

    public void Init(
        BrickManager brickManager,
        Vector2Int startCell,
        List<MoveCommand> commands,
        float moveInterval,
        float moveSpeed,
        bool loopAllCommands
    )
    {
        this.brickManager = brickManager;
        this.currentCell = startCell;
        this.commands = commands;
        this.moveInterval = moveInterval;
        this.moveSpeed = moveSpeed;
        this.loopAllCommands = loopAllCommands;

        commandIndex = 0;
        commandRepeatCounter = 0;
        sectionLoopCounters.Clear();

        transform.position = brickManager.GetCellWorldPosition(currentCell);

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine());
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            if (!isMoving)
                TryMoveByCommand();

            yield return new WaitForSeconds(moveInterval);
        }
    }

    private void TryMoveByCommand()
    {
        if (brickManager == null)
            return;

        if (commands == null || commands.Count == 0)
            return;

        int safety = 0;

        while (safety < 20)
        {
            safety++;

            if (commandIndex < 0 || commandIndex >= commands.Count)
                return;

            MoveCommand command = commands[commandIndex];
            Vector2Int direction = command.direction;

            if (direction == Vector2Int.zero)
            {
                CompleteCurrentCommand(command);
                continue;
            }

            if (command.stopYMoveAtRow && direction.y != 0)
            {
                bool reachedLimit =
                    (direction.y > 0 && currentCell.y >= command.stopYRow) ||
                    (direction.y < 0 && currentCell.y <= command.stopYRow);

                if (reachedLimit)
                {
                    CompleteCurrentCommand(command);
                    continue;
                }
            }

            Vector2Int nextCell = currentCell + direction;

            if (brickManager.IsCellOccupied(nextCell))
            {
                if (command.blockedMoveAction == BlockedMoveAction.SkipToNextCommand)
                {
                    CompleteCurrentCommand(command);
                    continue;
                }

                return;
            }

            StartCoroutine(MoveToCell(nextCell));
            AdvanceCommand(command);
            return;
        }
    }

    private void AdvanceCommand(MoveCommand command)
    {
        commandRepeatCounter++;

        if (commandRepeatCounter < command.repeatCount)
            return;

        commandRepeatCounter = 0;

        // ���� �ݺ� ó��
        if (command.sectionLoopEnd)
        {
            int endIndex = commandIndex;

            if (!sectionLoopCounters.ContainsKey(endIndex))
                sectionLoopCounters[endIndex] = 0;

            sectionLoopCounters[endIndex]++;

            // ���� ���� �ݺ� Ƚ���� �������� �ǵ��ư�
            if (sectionLoopCounters[endIndex] < command.sectionLoopCount)
            {
                commandIndex = Mathf.Clamp(
                    command.loopBackToIndex,
                    0,
                    commands.Count - 1
                );
                return;
            }

            // ���� �ݺ��� �������� ī���� ���� �� ���� Ŀ�ǵ�� ����
            sectionLoopCounters.Remove(endIndex);
        }

        // ���� Ŀ�ǵ� ����
        commandIndex++;

        // ��¥ ���������� ���� ��쿡�� ����
        if (commandIndex >= commands.Count)
        {
            if (loopAllCommands)
            {
                commandIndex = 0;
                commandRepeatCounter = 0;
                sectionLoopCounters.Clear();
            }
            else
            {
                StopMoving();
            }
        }
    }

    private void CompleteCurrentCommand(MoveCommand command)
    {
        commandRepeatCounter = command.repeatCount - 1;
        AdvanceCommand(command);
    }

    private IEnumerator MoveToCell(Vector2Int targetCell)
    {
        isMoving = true;

        Vector3 targetPos = brickManager.GetCellWorldPosition(targetCell);

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;
        currentCell = targetCell;

        isMoving = false;
    }

    private void StopMoving()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isMoving = false;
    }

    //unity 물리 사용 시 (circlecast 방식엔 필요 x)
    /*private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball"))
            return;

        BallController ball = collision.gameObject.GetComponent<BallController>();

        if (ball != null)
            //ball.DecreaseByBlockHit();

        Destroy(gameObject);
    }*/
}