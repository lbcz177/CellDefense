using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Distance-based dashes retain their spacing and direction through grid corners.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class RouteFlowRenderer : MonoBehaviour
{
    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<float> distances = new List<float>();
    private readonly List<Vector3> vertices = new List<Vector3>(1024);
    private readonly List<Color32> colors = new List<Color32>(1024);
    private readonly List<int> triangles = new List<int>(1536);
    private Mesh mesh;
    private float cellSize, length, elapsed;
    private bool preview;
    private static readonly Color32 Edge = new Color32(128, 82, 34, 185);
    private static readonly Color32 Yellow = new Color32(255, 216, 68, 235);
    private static readonly Color32 PreviewYellow = new Color32(255, 235, 120, 245);

    public void Initialize(Material material)
    {
        mesh = new Mesh { name = "Animated yellow route" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = -12;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    public void SetPath(List<Vector3> worldPoints, float size, bool isPreview)
    {
        bool changed = points.Count != worldPoints.Count || !Mathf.Approximately(size, cellSize);
        for (int i = 0; !changed && i < points.Count; i++)
            changed = (points[i] - transform.InverseTransformPoint(worldPoints[i])).sqrMagnitude > .000001f;
        preview = isPreview;
        if (changed)
        {
            cellSize = Mathf.Max(.01f, size);
            points.Clear();
            distances.Clear();
            length = 0f;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                Vector3 point = transform.InverseTransformPoint(worldPoints[i]);
                if (i > 0) length += Vector3.Distance(points[i - 1], point);
                points.Add(point);
                distances.Add(length);
            }
            elapsed = 0f;
        }
        Render(elapsed);
    }

    private void Update()
    {
        // Scaled time follows the game's pause and playback speed controls.
        if (Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        Render(elapsed);
    }

    private void Render(float time)
    {
        if (mesh == null) return;
        vertices.Clear(); colors.Clear(); triangles.Clear();
        float end = Mathf.Min(length, time * cellSize * 12f);
        float period = cellSize * .65f;
        float dash = cellSize * .37f;
        float phase = Mathf.Repeat(time * cellSize * .8f, period);
        int segment = 1;
        for (float start = phase - period; start < end; start += period)
        {
            float from = Mathf.Max(0f, start);
            float to = Mathf.Min(end, start + dash);
            if (to <= from) continue;
            while (segment < points.Count && distances[segment] <= from) segment++;
            for (int i = segment; i < points.Count && distances[i - 1] < to; i++)
            {
                float span = distances[i] - distances[i - 1];
                if (span <= .00001f) continue;
                float a = Mathf.Max(from, distances[i - 1]);
                float b = Mathf.Min(to, distances[i]);
                if (b <= a) continue;
                Vector3 p = Vector3.Lerp(points[i - 1], points[i], (a - distances[i - 1]) / span);
                Vector3 q = Vector3.Lerp(points[i - 1], points[i], (b - distances[i - 1]) / span);
                Quad(p, q, cellSize * .15f, Edge);
                Quad(p, q, cellSize * .09f, preview ? PreviewYellow : Yellow);
            }
        }
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    private void Quad(Vector3 a, Vector3 b, float width, Color32 color)
    {
        Vector3 direction = (b - a).normalized;
        Vector3 side = new Vector3(-direction.y, direction.x, 0f) * (width * .5f);
        int n = vertices.Count;
        vertices.Add(a - side); vertices.Add(a + side);
        vertices.Add(b + side); vertices.Add(b - side);
        for (int i = 0; i < 4; i++) colors.Add(color);
        triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
        triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 3);
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
