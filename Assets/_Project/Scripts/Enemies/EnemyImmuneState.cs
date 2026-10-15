using System;
using UnityEngine;

public class EnemyImmuneState : MonoBehaviour
{
    public const int MarkDamageBonusPercent = 15;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer markRingRenderer;
    [SerializeField] private Color markedColor = new Color(1f, 0.85f, 0.25f, 1f);

    private Color normalColor;
    private float markedUntil;
    private bool wasMarked;

    public bool IsMarked => isActiveAndEnabled && Time.time < markedUntil;
    public float DamageTakenMultiplier => IsMarked ? 1f + MarkDamageBonusPercent / 100f : 1f;

    private void Awake()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
        }
        if (bodyRenderer != null)
        {
            normalColor = bodyRenderer.color;
        }
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void OnDisable()
    {
        ResetState();
    }

    public void ResetState()
    {
        markedUntil = 0f;
        wasMarked = false;
        SetMarkedVisual(false);
    }

    public void ApplyMark(float duration)
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        if (Time.time + duration > markedUntil)
        {
            markedUntil = Time.time + duration;
        }

        if (!wasMarked && IsMarked)
        {
            wasMarked = true;
            SetMarkedVisual(true);
        }
    }

    private void Update()
    {
        bool isMarked = IsMarked;
        if (isMarked == wasMarked)
        {
            return;
        }

        wasMarked = isMarked;
        SetMarkedVisual(isMarked);
    }

    private void SetMarkedVisual(bool isMarked)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.color = isMarked ? markedColor : normalColor;
        }
        if (markRingRenderer != null)
        {
            markRingRenderer.enabled = isMarked;
        }
    }
}
