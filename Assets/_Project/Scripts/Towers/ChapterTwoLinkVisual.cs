using UnityEngine;

// World-space feedback keeps support actions readable independently of sprite scale.
public sealed class ChapterTwoLinkVisual : MonoBehaviour
{
    private LineRenderer link, progress;
    private Material material;
    private void Awake()
    {
        material = new Material(Shader.Find("Sprites/Default"));
        link = Create("Support link", .035f);
        progress = Create("Assembly progress", .06f);
        Hide();
    }
    private LineRenderer Create(string label, float width)
    {
        var obj = new GameObject(label);obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>();line.sharedMaterial = material;
        line.useWorldSpace = true;line.widthMultiplier = width;line.sortingOrder = 25;
        line.numCapVertices = 2;return line;
    }
    public void Show(Vector3 destination, float fraction, Color color)
    {
        link.enabled = progress.enabled = true;
        link.startColor = link.endColor = color;
        link.positionCount = 2;link.SetPosition(0, transform.position);link.SetPosition(1, destination);
        progress.startColor = progress.endColor = color;
        int points = Mathf.Max(2, Mathf.CeilToInt(Mathf.Clamp01(fraction) * 32) + 1);
        progress.positionCount = points;
        for (int i = 0; i < points; i++)
        {
            float angle = Mathf.PI * 2f * Mathf.Clamp01(fraction) * i / (points - 1);
            progress.SetPosition(i, destination + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * .45f);
        }
    }
    public void Hide() { if (link != null) link.enabled = false;if (progress != null) progress.enabled = false; }
    private void OnDisable() => Hide();
    private void OnDestroy() { if (material != null) Destroy(material); }
}
