using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TimedSpriteIndicator : MonoBehaviour
{
    private SpriteRenderer indicatorRenderer;
    private float visibleUntil;

    private void Awake()
    {
        indicatorRenderer = GetComponent<SpriteRenderer>();
        indicatorRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (indicatorRenderer != null)
        {
            indicatorRenderer.enabled = false;
        }
        visibleUntil = 0f;
    }

    private void OnDisable()
    {
        if (indicatorRenderer != null)
        {
            indicatorRenderer.enabled = false;
        }
        visibleUntil = 0f;
    }

    public void ShowFor(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        visibleUntil = Mathf.Max(visibleUntil, Time.time + duration);
        indicatorRenderer.enabled = true;
    }

    private void Update()
    {
        if (indicatorRenderer.enabled && Time.time >= visibleUntil)
        {
            indicatorRenderer.enabled = false;
        }
    }
}
