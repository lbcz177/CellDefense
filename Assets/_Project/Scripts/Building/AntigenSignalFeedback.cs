using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class AntigenSignalFeedback : MonoBehaviour
{
    [SerializeField] private TimedSpriteIndicator sourceIndicator;
    [SerializeField, Min(0.01f)] private float visibleDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float lineWidth = 0.06f;
    [SerializeField] private Color lineColor = new Color(0.35f, 0.95f, 1f, 1f);

    private sealed class Link
    {
        public LineRenderer Line;
        public Transform Source;
        public Transform Destination;
        public float Until;
    }
    private readonly List<Link> links = new List<Link>();
    private Material sharedLineMaterial;
    private Material ownedMaterial;
    public int ActiveConnectionCount
    {
        get
        {
            int count = 0;
            foreach (Link link in links)
                if (link.Line.enabled && Time.time < link.Until) count++;
            return count;
        }
    }

    private void Awake()
    {
        EnsureReady();
    }

    private void EnsureReady()
    {
        if (links.Count > 0) return;
        LineRenderer first = GetComponent<LineRenderer>();
        sharedLineMaterial = first.sharedMaterial;
        if (sharedLineMaterial == null)
        {
            ownedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Antigen Signal" };
            sharedLineMaterial = ownedMaterial;
        }
        Configure(first);
        links.Add(new Link { Line = first });
    }

    private void Configure(LineRenderer line)
    {
        line.sharedMaterial = sharedLineMaterial;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = line.endWidth = lineWidth;
        line.startColor = line.endColor = lineColor;
        line.enabled = false;
    }

    private void OnDisable()
    {
        foreach (Link link in links)
        {
            link.Line.enabled = false;
            link.Source = link.Destination = null;
            link.Until = 0f;
        }
        if (sourceIndicator != null) sourceIndicator.Hide();
    }

    public void Show(Transform source, Transform destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        EnsureReady();
        Link selected = null;
        foreach (Link link in links)
            if (link.Destination == destination) { selected = link; break; }
        if (selected == null)
            foreach (Link link in links)
                if (!link.Line.enabled || Time.time >= link.Until) { selected = link; break; }
        if (selected == null)
        {
            GameObject child = new GameObject("Antigen Connection");
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            Configure(line);
            selected = new Link { Line = line };
            links.Add(selected);
        }
        selected.Source = source;
        selected.Destination = destination;
        selected.Until = Time.time + visibleDuration;
        SpriteRenderer body = source.GetComponent<SpriteRenderer>();
        if (body != null)
        {
            selected.Line.sortingLayerID = body.sortingLayerID;
            selected.Line.sortingOrder = body.sortingOrder + 2;
        }
        selected.Line.SetPosition(0, source.position);
        selected.Line.SetPosition(1, destination.position);
        selected.Line.enabled = true;
        if (sourceIndicator != null)
        {
            sourceIndicator.ShowFor(visibleDuration);
        }
    }

    private void Update()
    {
        foreach (Link link in links)
        {
            if (!link.Line.enabled) continue;
            if (Time.time >= link.Until || link.Source == null || link.Destination == null ||
                !link.Source.gameObject.activeInHierarchy || !link.Destination.gameObject.activeInHierarchy)
                link.Line.enabled = false;
            else
            {
                link.Line.SetPosition(0, link.Source.position);
                link.Line.SetPosition(1, link.Destination.position);
            }
        }
    }

    private void OnDestroy()
    {
        if (ownedMaterial == null) return;
        if (Application.isPlaying) Destroy(ownedMaterial);
        else DestroyImmediate(ownedMaterial);
    }
}
