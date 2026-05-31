using System;
using UnityEngine;

[Serializable]
public class CeilingSegment
{
    [SerializeField] private string segmentName;
    [SerializeField] private int startX;
    [SerializeField] private int endX;
    [SerializeField] private float currentHp;
    [SerializeField] private float maxHp;
    [SerializeField] private bool isDestroyed;

    public string SegmentName => segmentName;
    public int StartX => startX;
    public int EndX => endX;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDestroyed => isDestroyed;

    public CeilingSegment(string segmentName, int startX, int endX, float maxHp)
    {
        this.segmentName = segmentName;
        this.startX = startX;
        this.endX = endX;

        Reset(maxHp);
    }

    public void Reset(float maxHp)
    {
        this.maxHp = Mathf.Max(1f, maxHp);
        currentHp = this.maxHp;
        isDestroyed = false;
    }

    public bool ContainsX(int x)
    {
        return x >= startX && x <= endX;
    }

    public bool ApplyDamage(float damage)
    {
        if (isDestroyed)
            return false;

        currentHp = Mathf.Clamp(currentHp - Mathf.Max(0, damage), 0, maxHp);

        if (currentHp > 0)
            return false;

        isDestroyed = true;
        return true;
    }

    public float GetHpPercent()
    {
        if (maxHp <= 0)
            return 0f;

        return currentHp / (float)maxHp;
    }
}
