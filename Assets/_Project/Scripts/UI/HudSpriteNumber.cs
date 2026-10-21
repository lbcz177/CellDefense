using System;
using UnityEngine;
using UnityEngine.UI;

// All slots are authored in the prefab, including overflow slots.
public sealed class HudSpriteNumber : MonoBehaviour
{
    [SerializeField] private Image[] slots;
    private OpenFieldUiResources artwork;
    private RectTransform row;
    private int value = -1;

    public void Initialize(OpenFieldUiResources resources)
    {
        if (resources == null || slots == null || slots.Length != 10 || Array.Exists(slots, slot => slot == null))
            throw new InvalidOperationException("Sprite number requires artwork and ten prefab Image slots.");
        artwork = resources;
        row = GetComponent<RectTransform>();
        value = -1;
    }

    public void SetValue(int number)
    {
        number = Mathf.Max(0, number);
        if (value == number) return;
        value = number;
        int count = 1;
        for (int remaining = number; remaining >= 10; remaining /= 10) count++;
        float width = Mathf.Min(32f, row.rect.width / count);
        float height = Mathf.Min(row.rect.height, width * 1.25f);
        int digits = number;
        for (int i = slots.Length - 1; i >= 0; i--)
        {
            bool visible = i < count;
            if (slots[i].gameObject.activeSelf != visible) slots[i].gameObject.SetActive(visible);
            if (!visible) continue;
            slots[i].sprite = artwork.GetDigit(digits % 10);
            digits /= 10;
            slots[i].rectTransform.anchoredPosition = new Vector2(i * width, 0f);
            slots[i].rectTransform.sizeDelta = new Vector2(width, height);
        }
    }
}
