using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EndingGaugeController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private float fillDuration = 8f;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image[] gaugeSlots;
    [SerializeField] private Color filledColor = new Color(0.2f, 1f, 0.25f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.2f, 1f, 0.25f, 0.12f);
    [SerializeField] private float flickerAmount = 0.2f;
    [SerializeField] private float flickerSpeed = 18f;
    [SerializeField] private float flickerPhase;
    [SerializeField, Range(0f, 1f)] private float minFlickerAlpha;
    [SerializeField] private UnityEvent onFilled;

    private float elapsed;
    private bool filling;
    private bool filled;

    public float Fill01 => fillDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fillDuration);

    private void Awake()
    {
        ResetGauge();
    }

    private void Update()
    {
        if (!filling || filled)
            return;

        elapsed += Time.deltaTime;
        float fill = Fill01;
        ApplyFill(fill);

        if (fill < 1f)
            return;

        filled = true;
        filling = false;
        onFilled?.Invoke();
    }

    public void BeginFill()
    {
        elapsed = 0f;
        filled = false;
        filling = true;

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        ApplyFill(0f);
    }

    public void ResetGauge()
    {
        elapsed = 0f;
        filled = false;
        filling = false;
        ApplyFill(0f);

        if (root != null)
            root.SetActive(false);
    }

    private void ApplyFill(float fill)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = fill;
            fillImage.color = CreateFlickerColor(filledColor, fill > 0f);
        }

        if (gaugeSlots == null || gaugeSlots.Length == 0)
            return;

        float scaledFill = fill * gaugeSlots.Length;

        for (int i = 0; i < gaugeSlots.Length; i++)
        {
            Image slot = gaugeSlots[i];

            if (slot == null)
                continue;

            bool isFilled = i < scaledFill;
            slot.enabled = true;
            slot.color = isFilled
                ? CreateFlickerColor(filledColor, true)
                : emptyColor;
        }
    }

    private Color CreateFlickerColor(Color baseColor, bool canFlicker)
    {
        return SharedFlickerSignal.ApplyAlphaFlicker(
            baseColor,
            canFlicker,
            flickerAmount,
            flickerSpeed,
            flickerPhase,
            minFlickerAlpha
        );
    }
}
