using UnityEngine;

public static class SharedFlickerSignal
{
    public static float Evaluate(float flickerSpeed, float phase)
    {
        if (flickerSpeed <= 0f)
            return 0f;

        float time = Time.unscaledTime;
        float main = Mathf.Abs(Mathf.Sin(time * flickerSpeed + phase));
        float sub = Mathf.Abs(Mathf.Sin(time * flickerSpeed * 2.37f + phase * 1.71f));
        return Mathf.Clamp01(main * 0.75f + sub * 0.25f);
    }

    public static Color ApplyAlphaFlicker(
        Color baseColor,
        bool canFlicker,
        float flickerAmount,
        float flickerSpeed,
        float phase,
        float minAlpha
    )
    {
        if (!canFlicker || flickerAmount <= 0f || flickerSpeed <= 0f)
            return baseColor;

        float signal = Evaluate(flickerSpeed, phase);
        float alphaOffset = signal * flickerAmount;

        Color color = baseColor;
        color.a = Mathf.Clamp(baseColor.a - alphaOffset, Mathf.Clamp01(minAlpha), 1f);
        return color;
    }
}
