using System;
using UnityEngine;

public class PhagocyteAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(0f)] private float baseEngulfHealth = 20f;
    [SerializeField, Min(0f)] private float markedEngulfHealth = 50f;
    [SerializeField, Min(0.01f)] private float digestionDuration = 3f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color attackFlashColor = new Color(0.55f, 0.95f, 1f, 1f);
    [SerializeField] private Color engulfFlashColor = new Color(1f, 0.7f, 0.25f, 1f);
    [SerializeField, Min(0.01f)] private float feedbackDuration = 0.2f;

    private float digestionUntil;
    private float feedbackUntil;
    private Color normalColor;
    private bool feedbackActive;

    public override bool CanAttack => Time.time >= digestionUntil;

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

    private void Update()
    {
        if (feedbackActive && Time.time >= feedbackUntil)
        {
            ResetFeedback();
        }
    }

    private void OnDisable()
    {
        ResetFeedback();
    }

    public override int GetTargetPriority(EnemyController target)
    {
        return ShouldEngulf(target) ? 1 : 0;
    }

    public override void Attack(EnemyController target, float damage, ProjectilePool projectilePool)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (digestionDuration <= 0f)
        {
            throw new InvalidOperationException("Digestion duration must be greater than zero.");
        }
        if (baseEngulfHealth < 0f || markedEngulfHealth < baseEngulfHealth)
        {
            throw new InvalidOperationException("Marked engulf health must be at least the base engulf health.");
        }

        if (ShouldEngulf(target))
        {
            Health health = target.GetComponent<Health>();
            if (health == null)
            {
                throw new InvalidOperationException("Engulf target needs a Health component.");
            }

            ShowFeedback(engulfFlashColor);
            digestionUntil = Time.time + digestionDuration;
            target.TakeDamage(health.CurrentHealth);
            return;
        }

        ShowFeedback(attackFlashColor);
        target.TakeDamage(damage);
    }

    private void ShowFeedback(Color color)
    {
        if (bodyRenderer == null)
        {
            return;
        }

        bodyRenderer.color = color;
        feedbackUntil = Time.time + Mathf.Max(feedbackDuration, 0.01f);
        feedbackActive = true;
    }

    private void ResetFeedback()
    {
        if (!feedbackActive)
        {
            return;
        }

        feedbackActive = false;
        if (bodyRenderer != null)
        {
            bodyRenderer.color = normalColor;
        }
    }

    private bool ShouldEngulf(EnemyController target)
    {
        if (target == null)
        {
            return false;
        }
        if (target.CanBeTargeted == false)
        {
            return false;
        }
        if (target.Definition == null)
        {
            return false;
        }
        if (target.Definition.CanBeEngulfed == false)
        {
            return false;
        }

        if (baseEngulfHealth < 0f || markedEngulfHealth < baseEngulfHealth)
        {
            throw new InvalidOperationException("Marked engulf health must be at least the base engulf health.");
        }
        Health health = target.GetComponent<Health>();
        if (health == null)
        {
            return false;
        }
        EnemyImmuneState enemy = target.GetComponent<EnemyImmuneState>();
        bool isMarked = enemy != null && enemy.IsMarked;
        float threshold = isMarked? markedEngulfHealth : baseEngulfHealth;

        return health.CurrentHealth <= threshold;
    }
}
