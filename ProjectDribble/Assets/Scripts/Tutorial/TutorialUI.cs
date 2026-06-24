using System;
using TMPro;
using UnityEngine;

public enum TutorialPopupCloseMode
{
    ClickToClose,
    ExternalOnly
}

public class UIManager : MonoBehaviour
{
    [Header("Tutorial Popup")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Tutorial Step Popups")]
    [Tooltip("Step 1 through Step 8 popup roots, in order.")]
    [SerializeField] private GameObject[] tutorialStepPopups = new GameObject[8];

    private Action tutorialCloseCallback;
    private bool isTutorialPopupOpen;
    private TutorialPopupCloseMode tutorialPopupCloseMode = TutorialPopupCloseMode.ClickToClose;
    private GameObject currentTutorialPopupRoot;

    public bool IsTutorialPopupOpen => isTutorialPopupOpen;
    public GameObject CurrentTutorialPopupRoot => currentTutorialPopupRoot;

    protected virtual void Awake()
    {
        HideTutorialPopup(false);
    }

    protected virtual void OnDisable()
    {
        if (isTutorialPopupOpen)
        {
            HideTutorialPopup();
            return;
        }

        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Tutorial, false);
    }

    public bool ShowTutorialPopup(
        string message,
        Action onClose = null,
        TutorialPopupCloseMode closeMode = TutorialPopupCloseMode.ClickToClose)
    {
        Debug.Log(
            $"[Tutorial] TutorialUI.ShowTutorialPopup called. " +
            $"rootActiveSelf={gameObject.activeSelf}, rootActiveInHierarchy={gameObject.activeInHierarchy}, " +
            $"panelActiveSelf={(messageRoot != null && messageRoot.activeSelf)}, " +
            $"panelActiveInHierarchy={(messageRoot != null && messageRoot.activeInHierarchy)}");

        if (isTutorialPopupOpen)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: another tutorial popup is already open.");
            return false;
        }

        if (messageRoot == null)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: messageRoot is not assigned.");
            return false;
        }

        if (messageText == null)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: messageText is not assigned.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: message is empty.");
            return false;
        }

        if (!enabled)
            enabled = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: TutorialUI has an inactive parent.");
            return false;
        }

        HideAllTutorialStepPopups();

        if (!messageRoot.activeSelf)
            messageRoot.SetActive(true);

        if (!messageRoot.activeInHierarchy)
        {
            Debug.LogWarning("[Tutorial] Popup was not shown: messageRoot has an inactive parent.");
            return false;
        }

        messageText.text = message;
        tutorialCloseCallback = onClose;
        tutorialPopupCloseMode = closeMode;
        currentTutorialPopupRoot = messageRoot;

        isTutorialPopupOpen = true;
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Tutorial, true);
        Debug.Log(
            $"[Tutorial] Popup shown. rootActive={gameObject.activeInHierarchy}, " +
            $"panelActive={messageRoot.activeInHierarchy}");
        return true;
    }

    public bool ShowTutorialStepPopup(int stepNumber, Action onClose = null)
    {
        if (isTutorialPopupOpen)
        {
            Debug.LogWarning("[Tutorial] Step popup was not shown: another tutorial popup is already open.");
            return false;
        }

        int popupIndex = stepNumber - 1;
        if (tutorialStepPopups == null || popupIndex < 0 || popupIndex >= tutorialStepPopups.Length)
        {
            Debug.LogWarning($"[Tutorial] Step popup was not shown: invalid step number {stepNumber}.");
            return false;
        }

        GameObject popupRoot = tutorialStepPopups[popupIndex];
        if (popupRoot == null)
        {
            Debug.LogWarning($"[Tutorial] Step popup was not shown: Step {stepNumber} root is not assigned.");
            return false;
        }

        if (!enabled)
            enabled = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[Tutorial] Step popup was not shown: TutorialUI has an inactive parent.");
            return false;
        }

        if (messageRoot != null)
            messageRoot.SetActive(false);

        HideAllTutorialStepPopups();
        popupRoot.SetActive(true);

        tutorialCloseCallback = onClose;
        tutorialPopupCloseMode = TutorialPopupCloseMode.ClickToClose;
        currentTutorialPopupRoot = popupRoot;
        isTutorialPopupOpen = true;
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Tutorial, true);
        return true;
    }

    public void HideAllTutorialStepPopups()
    {
        if (tutorialStepPopups == null)
            return;

        for (int i = 0; i < tutorialStepPopups.Length; i++)
        {
            if (tutorialStepPopups[i] != null)
                tutorialStepPopups[i].SetActive(false);
        }

        currentTutorialPopupRoot = null;
    }

    public void HideTutorialPopup()
    {
        HideTutorialPopup(true);
    }

    public void HideTutorialPopupWithoutCallback()
    {
        HideTutorialPopup(false);
    }

    public void Continue()
    {
        if (tutorialPopupCloseMode == TutorialPopupCloseMode.ExternalOnly)
            return;

        HideTutorialPopup();
    }

    public void ShowMessage(string message, bool pauseGame)
    {
        ShowTutorialPopup(message);
    }

    public void Hide()
    {
        HideTutorialPopup();
    }

    private void HideTutorialPopup(bool invokeCallback)
    {
        if (messageRoot != null)
            messageRoot.SetActive(false);

        HideAllTutorialStepPopups();

        if (messageText != null)
            messageText.text = string.Empty;

        bool wasOpen = isTutorialPopupOpen;
        Action callback = tutorialCloseCallback;
        tutorialCloseCallback = null;
        tutorialPopupCloseMode = TutorialPopupCloseMode.ClickToClose;
        currentTutorialPopupRoot = null;
        isTutorialPopupOpen = false;
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Tutorial, false);

        if (invokeCallback && wasOpen)
            callback?.Invoke();
    }

}

public class TutorialUI : UIManager
{
}
