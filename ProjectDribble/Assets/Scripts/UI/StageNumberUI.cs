using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageNumberUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNumberText;

    public static StageNumberUI CreateUnder(Canvas canvas)
    {
        if (canvas == null)
            return null;

        GameObject root = new("StageNumberUI", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(28f, -24f);
        rootRect.sizeDelta = new Vector2(220f, 48f);

        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.text = string.Empty;

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        outline.effectDistance = new Vector2(2f, -2f);

        StageNumberUI ui = root.AddComponent<StageNumberUI>();
        ui.stageNumberText = text;
        ui.Hide();
        return ui;
    }

    private void Awake()
    {
        if (stageNumberText == null)
            stageNumberText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void SetStageNumber(int normalStageNumber)
    {
        if (normalStageNumber <= 0)
        {
            Hide();
            return;
        }

        if (stageNumberText != null)
            stageNumberText.text = $"STAGE {normalStageNumber}";

        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
