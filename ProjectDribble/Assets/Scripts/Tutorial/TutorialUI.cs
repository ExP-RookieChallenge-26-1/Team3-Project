using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Tutorial Popup")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    private Action tutorialCloseCallback;
    private bool isTutorialPopupOpen;

    public bool IsTutorialPopupOpen => isTutorialPopupOpen;

    protected virtual void Awake()
    {
        HideTutorialPopup(false);
    }

    protected virtual void Update()
    {
        if (!isTutorialPopupOpen)
            return;

        if (IsTutorialCloseInput())
            HideTutorialPopup();
    }

    protected virtual void OnDisable()
    {
        if (isTutorialPopupOpen)
            HideTutorialPopup();
    }

    public void ShowTutorialPopup(string message, Action onClose = null)
    {
        tutorialCloseCallback = onClose;

        if (messageText != null)
            messageText.text = message;

        if (messageRoot != null)
            messageRoot.SetActive(true);

        isTutorialPopupOpen = true;
    }

    public void HideTutorialPopup()
    {
        HideTutorialPopup(true);
    }

    public void Continue()
    {
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
        isTutorialPopupOpen = false;

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
