using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyVisualState : EnemyBehaviour
{
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite slowedSprite;
    [SerializeField] private Sprite shieldedSprite;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private Sprite engulfSprite;
    private SpriteRenderer body;
    private EnemySlowState slow;
    private EnemyImmuneState immune;
    private Animator animator;
    private Sprite phaseSprite;
    private LineRenderer pulse;
    private Material pulseMaterial;
    private float pulseStarted;
    private float pulseUntil;
    private float pulseRadius;
    private Color pulseColor;

    public override void Initialize(EnemyDefinition definition)
    {
        body = GetComponent<SpriteRenderer>();
        slow = GetComponent<EnemySlowState>();
        immune = GetComponent<EnemyImmuneState>();
        animator = GetComponent<Animator>();
        if (baseSprite == null && body != null) baseSprite = body.sprite;
        base.Initialize(definition);
        if (body != null) body.sprite = baseSprite;
    }
    public override void ResetState()
    {
        phaseSprite = null;
        pulseUntil = 0f;
        if (pulse != null) pulse.enabled = false;
    }
    public void SetPhaseSprite(Sprite sprite) => phaseSprite = sprite;

    public void ShowPulse(Color color, float radius, float duration)
    {
        if (!IsCurrent) return;
        if (pulse == null)
        {
            GameObject obj = new GameObject("Enemy Ability Pulse");
            obj.transform.SetParent(transform, false);
            Vector3 scale = transform.lossyScale;
            obj.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            pulse = obj.AddComponent<LineRenderer>();
            pulseMaterial = new Material(Shader.Find("Sprites/Default"));
            pulse.sharedMaterial = pulseMaterial;
            pulse.useWorldSpace = true;
            pulse.loop = true;
            pulse.positionCount = 64;
            pulse.widthMultiplier = 0.035f;
            pulse.sortingLayerName = "Enemy";
            pulse.sortingOrder = -1;
        }
        pulseStarted = Time.time;
        pulseUntil = Time.time + duration;
        pulseRadius = radius;
        pulseColor = color;
        pulse.enabled = true;
        RefreshPulse(Time.time);
    }

    private void LateUpdate()
    {
        if (!IsCurrent)
        {
            if (pulse != null) pulse.enabled = false;
            return;
        }
        if (body != null)
        {
            Sprite next = phaseSprite != null ? phaseSprite : baseSprite;
            bool overrideSprite = phaseSprite != null;
            if ((Enemy.IsArmorBroken || (Enemy.Definition.InitialShield > 0f && !Enemy.HasShield)) &&
                brokenSprite != null) { next = brokenSprite; overrideSprite = true; }
            else if (engulfSprite != null && (Enemy.CanEngulf(20f) ||
                (immune != null && immune.IsMarked && Enemy.CanEngulf(50f))))
                { next = engulfSprite; overrideSprite = true; }
            else if (slow != null && slow.SpeedMultiplier < 1f && slowedSprite != null)
                { next = slowedSprite; overrideSprite = true; }
            else if (Enemy.HasShield && shieldedSprite != null) { next = shieldedSprite; overrideSprite = true; }
            bool animated = animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
            if ((!animated || overrideSprite) && next != null && body.sprite != next) body.sprite = next;
        }
        if (pulse != null && Time.deltaTime > 0f) RefreshPulse(Time.time);
    }

    private void RefreshPulse(float now)
    {
        if (pulse == null) return;
        if (now >= pulseUntil)
        {
            pulse.enabled = false;
            return;
        }
        float progress = Mathf.Clamp01((now - pulseStarted) / (pulseUntil - pulseStarted));
        Color color = pulseColor;
        color.a *= 1f - progress;
        pulse.startColor = pulse.endColor = color;
        float radius = pulseRadius * (0.75f + 0.25f * progress);
        Vector3 center = transform.position;
        for (int i = 0; i < 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            pulse.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }
    private void OnDestroy()
    {
        if (pulseMaterial != null) Destroy(pulseMaterial);
    }
}
