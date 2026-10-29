using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteHitFlash : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.065f;
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    private static readonly int Squash = Shader.PropertyToID("_ImpactSquash");
    private static readonly int Direction = Shader.PropertyToID("_ImpactDirection");
    private SpriteRenderer body;
    private MaterialPropertyBlock properties;
    private float flashUntil;
    private bool flashing;

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        flashing = false;
        Apply(0f);
    }

    public void Play()
    {
        flashing = true;
        flashUntil = Time.time + duration;
        Apply(1f);
    }

    private void LateUpdate()
    {
        if (!flashing || Time.time < flashUntil)
        {
            return;
        }
        flashing = false;
        Apply(0f);
    }

    private void OnDisable()
    {
        flashing = false;
        Apply(0f);
    }

    private void Apply(float amount)
    {
        if (body == null || properties == null)
        {
            return;
        }
        body.GetPropertyBlock(properties);
        properties.SetFloat(FlashAmount, amount);
        properties.SetFloat(Squash, amount * .075f);
        properties.SetVector(Direction, new Vector4(1f, 0f, 0f, 0f));
        body.SetPropertyBlock(properties);
    }
}
