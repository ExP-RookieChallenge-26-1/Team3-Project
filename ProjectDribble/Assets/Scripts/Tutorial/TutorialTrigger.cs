using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private TutorialStageId tutorialStageId = TutorialStageId.Stage6;
    [SerializeField] private string triggerId;
    [SerializeField] private TutorialManager tutorialManager;

    private void Awake()
    {
        if (tutorialManager == null)
            tutorialManager = FindAnyObjectByType<TutorialManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        if (other.GetComponentInParent<BallController>() == null)
            return;

        tutorialManager?.NotifyTriggerEntered(tutorialStageId, triggerId);
    }
}
