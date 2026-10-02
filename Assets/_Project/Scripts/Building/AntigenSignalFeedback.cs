using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AntigenSignalFeedback : MonoBehaviour
{
    [SerializeField] private TimedSpriteIndicator sourceIndicator;
    [SerializeField, Min(0.01f)] private float visibleDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float lineWidth = 0.06f;
    [SerializeField] private Color lineColor = new Color(0.35f, 0.95f, 1f, 1f);

    private LineRenderer signalLine;
    private float visibleUntil;

    private void Awake()
    {
        signalLine = GetComponent<LineRenderer>();
        signalLine.useWorldSpace = true;
        signalLine.positionCount = 2;
        signalLine.startWidth = lineWidth;
        signalLine.endWidth = lineWidth;
        signalLine.startColor = lineColor;
        signalLine.endColor = lineColor;
        signalLine.enabled = false;
    }

    private void OnDisable()
    {
        if (signalLine != null)
        {
            signalLine.enabled = false;
        }
        visibleUntil = 0f;
    }

    public void Show(Transform source, Transform destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        signalLine.SetPosition(0, source.position);
        signalLine.SetPosition(1, destination.position);
        visibleUntil = Time.time + visibleDuration;
        signalLine.enabled = true;
        if (sourceIndicator != null)
        {
            sourceIndicator.ShowFor(visibleDuration);
        }
    }

    private void Update()
    {
        if (signalLine.enabled && Time.time >= visibleUntil)
        {
            signalLine.enabled = false;
        }
    }
}
