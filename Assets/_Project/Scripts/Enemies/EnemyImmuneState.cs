using System;
using UnityEngine;

public class EnemyImmuneState : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color markedColor = new Color(1f, 0.85f, 0.25f, 1f);

    private Color normalColor;
    private float markedUntil;
    private bool wasMarked;

    public bool IsMarked => Time.time < markedUntil;

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

    public void ResetState()
    {
        markedUntil = 0f;
        wasMarked = false;
        if (bodyRenderer != null)
        {
            bodyRenderer.color = normalColor;
        }
    }

    public void ApplyMark(float duration)
    {
        if (duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        // TODO(USER): Refresh markedUntil using scaled game time. Do not stack durations.
        throw new NotImplementedException("Complete EnemyImmuneState.ApplyMark before enabling antibody attacks.");
    }

    private void Update()
    {
        bool isMarked = IsMarked;
        if (isMarked == wasMarked)
        {
            return;
        }

        wasMarked = isMarked;
        if (bodyRenderer != null)
        {
            bodyRenderer.color = isMarked ? markedColor : normalColor;
        }
    }
}
