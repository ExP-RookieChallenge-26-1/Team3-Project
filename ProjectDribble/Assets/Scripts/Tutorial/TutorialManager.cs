using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private const string Stage1StartMessage =
        "공을 튕기고 드리블해 보세요.\n드리블 후 공을 발사해 일반 블록을 모두 부수세요.";
    private const string Stage1CeilingMessage = "천장을 때리세요!";
    private const string Stage2StartMessage =
        "흐름 블록은 아래로 자랍니다.\n바닥에 닿으면 침식됩니다.\n중간 연결을 끊으면 성장이 멈춥니다.";
    private const string Stage3StartMessage =
        "천장은 여러 개로 나뉠 수 있습니다.\n천장을 부수면 연결된 흐름 블록의 성장이 멈춥니다.";
    private const string Stage3SegmentDestroyedMessage =
        "천장과 연결된 흐름 블록의 성장이 멈췄습니다.";

    [Header("References")]
    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private CeilingManager ceilingManager;

    [Header("Message")]
    [SerializeField] private bool pauseOnMessage;

    private TutorialStageId currentTutorialStageId = TutorialStageId.None;
    private bool isSubscribedToBlockEvents;
    private bool isSubscribedToCeilingEvents;

    private void Awake()
    {
        if (tutorialUI == null)
            tutorialUI = FindAnyObjectByType<TutorialUI>();

        if (blockManager == null)
            blockManager = FindAnyObjectByType<BlockManager>();

        if (ceilingManager == null)
            ceilingManager = FindAnyObjectByType<CeilingManager>();
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

    private void ClearStageSubscriptions()
    {
        if (isSubscribedToBlockEvents && blockManager != null)
            blockManager.OnNormalBlocksCleared -= HandleNormalBlocksCleared;

        if (isSubscribedToCeilingEvents && ceilingManager != null)
            ceilingManager.OnCeilingSegmentDestroyed -= HandleCeilingSegmentDestroyed;

        isSubscribedToBlockEvents = false;
        isSubscribedToCeilingEvents = false;
        currentTutorialStageId = TutorialStageId.None;
    }

    private void ShowMessage(string message)
    {
        if (tutorialUI == null)
            return;

        tutorialUI.ShowMessage(message, pauseOnMessage);
    }

    private void HideMessage()
    {
        if (tutorialUI == null)
            return;

        tutorialUI.Hide();
    }
}
