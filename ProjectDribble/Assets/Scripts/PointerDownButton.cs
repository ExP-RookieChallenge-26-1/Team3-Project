using UnityEngine;
using UnityEngine.EventSystems;

public class PointerDownButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ButtonAction action;

    private enum ButtonAction
    {
        StartGame,
        ResumeGame,
        NextStage,
        RetryStage
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (gameManager == null)
            return;

        switch (action)
        {
            case ButtonAction.StartGame:
                gameManager.StartGame();
                break;

            case ButtonAction.ResumeGame:
                gameManager.ResumeGame();
                break;

            case ButtonAction.NextStage:
                gameManager.NextStage();
                break;

            case ButtonAction.RetryStage:
                gameManager.RetryStage();
                break;
        }
    }
}