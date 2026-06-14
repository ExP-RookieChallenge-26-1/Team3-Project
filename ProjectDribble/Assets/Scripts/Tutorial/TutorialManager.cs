using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private const string Stage1StartMessage =
        "怨듭쓣 ?뺢린怨??쒕━釉뷀빐 蹂댁꽭??\n?쒕━釉???怨듭쓣 諛쒖궗???쇰컲 釉붾줉??紐⑤몢 遺?섏꽭??";
    private const string Stage1CeilingMessage = "泥쒖옣???뚮━?몄슂!";
    private const string Stage2StartMessage =
        "?먮쫫 釉붾줉? ?꾨옒濡??먮엻?덈떎.\n諛붾떏???우쑝硫?移⑥떇?⑸땲??\n以묎컙 ?곌껐???딆쑝硫??깆옣??硫덉땅?덈떎.";
    private const string Stage3StartMessage =
        "泥쒖옣? ?щ윭 媛쒕줈 ?섎돖 ???덉뒿?덈떎.\n泥쒖옣??遺?섎㈃ ?곌껐???먮쫫 釉붾줉???깆옣??硫덉땅?덈떎.";
    private const string Stage3SegmentDestroyedMessage =
        "泥쒖옣怨??곌껐???먮쫫 釉붾줉???깆옣??硫덉톬?듬땲??";
    private const string Stage4StartMessage = "블록을 부수면 게이지가 찹니다.";
    private const string Stage4GaugeFullMessage =
        "게이지가 가득 찼습니다. 공을 잡고 차징한 뒤 레이저를 발사해보세요.";
    private const string Stage4LaserFiredMessage =
        "레이저는 여러 블록을 한 번에 부술 수 있습니다.";
    private const string Stage5StartMessage = "고정 블록은 공으로 부서지지 않습니다.";
    private const string Stage5FixedHitMessage =
        "고정 블록은 공으로는 부술 수 없습니다.";
    private const string Stage5LaserGuideMessage =
        "레이저를 사용하면 고정 블록을 부술 수 있습니다.";
    private const string Stage5FixedDestroyedMessage = "고정 블록을 제거했습니다.";
    private const string Stage6RecallGuideMessage =
        "공이 멀리 있거나 갇혔을 때는 공을 다시 불러올 수 있습니다.\n리스폰 버튼을 눌러 공을 불러오세요.";
    private const string Stage6RecalledMessage = "공을 다시 불러왔습니다.";

    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private CeilingManager ceilingManager;
    [SerializeField] private GaugeManager gaugeManager;
    [SerializeField] private LaserShooter laserShooter;
    [SerializeField] private BallRespawner ballRespawner;

    [Header("Message")]
    [SerializeField] private bool pauseOnMessage;

    private TutorialStageId currentTutorialStageId = TutorialStageId.None;
    private bool isSubscribedToBlockEvents;
    private bool isSubscribedToCeilingEvents;
    private bool isSubscribedToGaugeEvents;
    private bool isSubscribedToLaserEvents;
    private bool isSubscribedToBallRespawnerEvents;
    private bool stage4GaugeFullMessageShown;
    private bool stage4LaserFiredMessageShown;
    private bool stage5FixedHitMessageShown;
    private bool stage5LaserGuideMessageShown;
    private bool stage5FixedDestroyedMessageShown;
    private bool stage6RecallGuideMessageShown;
    private bool stage6RecalledMessageShown;
    private Coroutine pendingMessageRoutine;

    private void Awake()
    {
        if (tutorialUI == null)
            tutorialUI = FindAnyObjectByType<TutorialUI>();

        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();

        if (ceilingManager == null)
            ceilingManager = FindAnyObjectByType<CeilingManager>();

        if (gaugeManager == null)
            gaugeManager = FindAnyObjectByType<GaugeManager>();

        if (laserShooter == null)
            laserShooter = FindAnyObjectByType<LaserShooter>();

        if (ballRespawner == null)
            ballRespawner = FindAnyObjectByType<BallRespawner>();
    }

    private void OnDisable()
    {
        ClearStageSubscriptions();
    }

    public void BeginStage(int stageIndex, StageDefinition stageDefinition)
    {
        ClearStageSubscriptions();

        currentTutorialStageId = ResolveTutorialStageId(stageIndex, stageDefinition);

        switch (currentTutorialStageId)
        {
            case TutorialStageId.Stage1:
                BeginStage1();
                break;
            case TutorialStageId.Stage2:
                ShowMessage(Stage2StartMessage);
                break;
            case TutorialStageId.Stage3:
                BeginStage3();
                break;
            case TutorialStageId.Stage4:
                BeginStage4();
                break;
            case TutorialStageId.Stage5:
                BeginStage5();
                break;
            case TutorialStageId.Stage6:
                BeginStage6();
                break;
            default:
                HideMessage();
                break;
        }
    }

    private TutorialStageId ResolveTutorialStageId(int stageIndex, StageDefinition stageDefinition)
    {
        if (stageDefinition == null || !stageDefinition.isTutorialStage)
            return TutorialStageId.None;

        if (stageDefinition.tutorialStageId != TutorialStageId.None)
            return stageDefinition.tutorialStageId;

        switch (stageIndex)
        {
            case 0:
                return TutorialStageId.Stage1;
            case 1:
                return TutorialStageId.Stage2;
            case 2:
                return TutorialStageId.Stage3;
            default:
                return TutorialStageId.None;
        }
    }

    private void BeginStage1()
    {
        ShowMessage(Stage1StartMessage);

        if (blockManager == null)
            return;

        blockManager.OnNormalBlocksCleared += HandleNormalBlocksCleared;
        isSubscribedToBlockEvents = true;
    }

    private void BeginStage3()
    {
        ShowMessage(Stage3StartMessage);

        if (ceilingManager == null)
            return;

        ceilingManager.OnCeilingSegmentDestroyed += HandleCeilingSegmentDestroyed;
        isSubscribedToCeilingEvents = true;
    }

    private void BeginStage4()
    {
        ShowMessage(Stage4StartMessage);

        if (gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged += HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged += HandleGaugeSegmentChanged;
            isSubscribedToGaugeEvents = true;
        }

        if (laserShooter != null)
        {
            laserShooter.OnLaserFired += HandleLaserFired;
            isSubscribedToLaserEvents = true;
        }

        if (IsGaugeFull())
            ShowMessageAfterCurrentCloses(Stage4GaugeFullMessage, MarkStage4GaugeFullMessageShown);
    }

    private void BeginStage5()
    {
        ShowMessage(Stage5StartMessage);

        if (blockManager == null)
            return;

        blockManager.OnFixedBlockHitByBall += HandleFixedBlockHitByBall;
        blockManager.OnFixedBlockDestroyedByLaser += HandleFixedBlockDestroyedByLaser;
        isSubscribedToBlockEvents = true;
    }

    private void BeginStage6()
    {
        if (ballRespawner == null)
            return;

        ballRespawner.OnBallRecalled += HandleBallRecalled;
        isSubscribedToBallRespawnerEvents = true;
    }

    private void HandleNormalBlocksCleared()
    {
        if (currentTutorialStageId != TutorialStageId.Stage1)
            return;

        ShowMessage(Stage1CeilingMessage);
    }

    private void HandleCeilingSegmentDestroyed(CeilingSegment segment)
    {
        if (currentTutorialStageId != TutorialStageId.Stage3)
            return;

        ShowMessage(Stage3SegmentDestroyedMessage);
    }

    private void HandleGaugeValueChanged(int value)
    {
        if (currentTutorialStageId != TutorialStageId.Stage4)
            return;

        if (IsGaugeFull())
            HandleGaugeFull();
    }

    private void HandleGaugeSegmentChanged(int filledSegments)
    {
        if (currentTutorialStageId != TutorialStageId.Stage4)
            return;

        if (IsGaugeFull())
            HandleGaugeFull();
    }

    private void HandleGaugeFull()
    {
        if (stage4GaugeFullMessageShown)
            return;

        stage4GaugeFullMessageShown = true;
        ShowMessage(Stage4GaugeFullMessage);
    }

    private bool IsGaugeFull()
    {
        if (gaugeManager == null)
            return false;

        if (gaugeManager.MaxGaugeValue > 0)
            return gaugeManager.CurrentGaugeValue >= gaugeManager.MaxGaugeValue;

        return gaugeManager.MaxGaugeSegments > 0 &&
               gaugeManager.FilledGaugeSegments >= gaugeManager.MaxGaugeSegments;
    }

    private void HandleLaserFired()
    {
        if (currentTutorialStageId != TutorialStageId.Stage4)
            return;

        if (stage4LaserFiredMessageShown)
            return;

        stage4LaserFiredMessageShown = true;
        ShowMessage(Stage4LaserFiredMessage);
    }

    private void HandleFixedBlockHitByBall(BlockCell block)
    {
        if (currentTutorialStageId != TutorialStageId.Stage5)
            return;

        if (!stage5FixedHitMessageShown)
        {
            stage5FixedHitMessageShown = true;
            ShowMessage(Stage5FixedHitMessage);
            ShowMessageAfterCurrentCloses(Stage5LaserGuideMessage, MarkStage5LaserGuideMessageShown);
            return;
        }

        if (!stage5LaserGuideMessageShown)
        {
            stage5LaserGuideMessageShown = true;
            ShowMessage(Stage5LaserGuideMessage);
        }
    }

    private void HandleFixedBlockDestroyedByLaser(BlockCell block)
    {
        if (currentTutorialStageId != TutorialStageId.Stage5)
            return;

        if (stage5FixedDestroyedMessageShown)
            return;

        stage5FixedDestroyedMessageShown = true;
        ShowMessage(Stage5FixedDestroyedMessage);
    }

    public void NotifyTriggerEntered(TutorialStageId tutorialStageId, string triggerId)
    {
        if (currentTutorialStageId != TutorialStageId.Stage6)
            return;

        if (tutorialStageId != TutorialStageId.None && tutorialStageId != TutorialStageId.Stage6)
            return;

        if (stage6RecallGuideMessageShown)
            return;

        stage6RecallGuideMessageShown = true;
        ShowMessage(Stage6RecallGuideMessage, false);
    }

    private void HandleBallRecalled()
    {
        if (currentTutorialStageId != TutorialStageId.Stage6)
            return;

        if (stage6RecalledMessageShown)
            return;

        stage6RecalledMessageShown = true;
        ShowMessage(Stage6RecalledMessage);
    }

    private void ClearStageSubscriptions()
    {
        if (isSubscribedToBlockEvents && blockManager != null)
        {
            blockManager.OnNormalBlocksCleared -= HandleNormalBlocksCleared;
            blockManager.OnFixedBlockHitByBall -= HandleFixedBlockHitByBall;
            blockManager.OnFixedBlockDestroyedByLaser -= HandleFixedBlockDestroyedByLaser;
        }

        if (isSubscribedToCeilingEvents && ceilingManager != null)
            ceilingManager.OnCeilingSegmentDestroyed -= HandleCeilingSegmentDestroyed;

        if (isSubscribedToGaugeEvents && gaugeManager != null)
        {
            gaugeManager.OnGaugeValueChanged -= HandleGaugeValueChanged;
            gaugeManager.OnGaugeSegmentChanged -= HandleGaugeSegmentChanged;
        }

        if (isSubscribedToLaserEvents && laserShooter != null)
            laserShooter.OnLaserFired -= HandleLaserFired;

        if (isSubscribedToBallRespawnerEvents && ballRespawner != null)
            ballRespawner.OnBallRecalled -= HandleBallRecalled;

        if (pendingMessageRoutine != null)
        {
            StopCoroutine(pendingMessageRoutine);
            pendingMessageRoutine = null;
        }

        isSubscribedToBlockEvents = false;
        isSubscribedToCeilingEvents = false;
        isSubscribedToGaugeEvents = false;
        isSubscribedToLaserEvents = false;
        isSubscribedToBallRespawnerEvents = false;
        currentTutorialStageId = TutorialStageId.None;
        ResetStageFlags();
    }

    private void ShowMessage(string message)
    {
        ShowMessage(message, pauseOnMessage);
    }

    private void ShowMessage(string message, bool pauseGame)
    {
        if (tutorialUI == null)
            return;

        tutorialUI.ShowMessage(message, pauseGame);
    }

    private void HideMessage()
    {
        if (tutorialUI == null)
            return;

        tutorialUI.Hide();
    }

    private void ResetStageFlags()
    {
        stage4GaugeFullMessageShown = false;
        stage4LaserFiredMessageShown = false;
        stage5FixedHitMessageShown = false;
        stage5LaserGuideMessageShown = false;
        stage5FixedDestroyedMessageShown = false;
        stage6RecallGuideMessageShown = false;
        stage6RecalledMessageShown = false;
    }

    private void ShowMessageAfterCurrentCloses(string message, System.Action markShown)
    {
        if (tutorialUI == null)
            return;

        markShown?.Invoke();

        if (pendingMessageRoutine != null)
            StopCoroutine(pendingMessageRoutine);

        pendingMessageRoutine = StartCoroutine(ShowMessageAfterCurrentClosesRoutine(message, markShown));
    }

    private IEnumerator ShowMessageAfterCurrentClosesRoutine(string message, System.Action markShown)
    {
        while (tutorialUI != null && tutorialUI.IsShowing)
            yield return null;

        ShowMessage(message);
        pendingMessageRoutine = null;
    }

    private void MarkStage4GaugeFullMessageShown()
    {
        stage4GaugeFullMessageShown = true;
    }

    private void MarkStage5LaserGuideMessageShown()
    {
        stage5LaserGuideMessageShown = true;
    }
}
