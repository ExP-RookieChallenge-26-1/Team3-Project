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

    private Action tutorialCloseCallback;
    private bool isTutorialPopupOpen;
    private TutorialPopupCloseMode tutorialPopupCloseMode = TutorialPopupCloseMode.ClickToClose;

    public bool IsTutorialPopupOpen => isTutorialPopupOpen;

    protected virtual void Awake()
    {
        HideTutorialPopup(false);
    }

    protected virtual void Update()
    {
        if (!isTutorialPopupOpen)
            return;

        if (tutorialPopupCloseMode == TutorialPopupCloseMode.ExternalOnly)
            return;

        if (IsTutorialCloseInput())
            HideTutorialPopup();
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

        isTutorialPopupOpen = true;
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Tutorial, true);
        Debug.Log(
            $"[Tutorial] Popup shown. rootActive={gameObject.activeInHierarchy}, " +
            $"panelActive={messageRoot.activeInHierarchy}");
        return true;
    }

    public void HideTutorialPopup()
    {
        HideTutorialPopup(true);
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

        if (messageText != null)
            messageText.text = string.Empty;

        bool wasOpen = isTutorialPopupOpen;
        Action callback = tutorialCloseCallback;
        tutorialCloseCallback = null;
        tutorialPopupCloseMode = TutorialPopupCloseMode.ClickToClose;
        isTutorialPopupOpen = false;
        SoundManager.Instance?.SetBgmMuffled(BgmMuffleReason.Tutorial, false);

        if (invokeCallback && wasOpen)
            callback?.Invoke();
    }

    private bool IsTutorialCloseInput()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
            return true;

        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }
}

public class TutorialUI : UIManager
{
}
