using UnityEngine;

// One reusable ground display. AcidAttackBehaviour owns timing, damage and pool lifetime.
internal sealed class AcidFieldVisual
{
    private const int Segments = 64;
    private static readonly int TintProperty = Shader.PropertyToID("_Color");
    private readonly GameObject root;
    private readonly MeshRenderer fill;
    private readonly LineRenderer edge;
    private readonly SpriteRenderer bubbles;
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private Color fillColor;
    private Color edgeColor;
    private float startedAt;
    private float expiresAt;

    public AcidFieldVisual(Transform parent, Mesh mesh, Material material)
    {
        root = new GameObject("Acid Area");
        root.transform.SetParent(parent, false);

        GameObject ground = new GameObject("Acid Fill", typeof(MeshFilter), typeof(MeshRenderer));
        ground.transform.SetParent(root.transform, false);
        ground.GetComponent<MeshFilter>().sharedMesh = mesh;
        fill = ground.GetComponent<MeshRenderer>();
        fill.sharedMaterial = material;
        fill.sortingLayerName = "BackGround";
        fill.sortingOrder = 0;

        GameObject rim = new GameObject("Acid Boundary", typeof(LineRenderer));
        rim.transform.SetParent(root.transform, false);
        edge = rim.GetComponent<LineRenderer>();
        edge.sharedMaterial = material;
        edge.sortingLayerName = "BackGround";
        edge.sortingOrder = 1;
        edge.useWorldSpace = false;
        edge.loop = true;
        edge.positionCount = Segments;
        edge.widthMultiplier = 0.025f;
        GameObject foam = new GameObject("Acid vesicle bubbles");foam.transform.SetParent(root.transform,false);
        bubbles=foam.AddComponent<SpriteRenderer>();bubbles.sortingLayerName="BackGround";bubbles.sortingOrder=2;
        bubbles.transform.localScale=Vector3.one*1.8f;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            edge.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
        }
        Hide();
    }

    public void Show(Vector2 center, float radius, float duration, Color acidColor, Color boundaryColor)
    {
        root.transform.position = new Vector3(center.x, center.y, 0f);
        // The parent is a unit-scale scene root, independent of the tower's sprite scale.
        root.transform.localScale = Vector3.one * radius;
        fillColor = acidColor;
        edgeColor = boundaryColor;
        startedAt = Time.time;
        expiresAt = startedAt + duration;
        root.SetActive(true);
        Refresh(startedAt);
    }

    public void Refresh(float now)
    {
        if (root == null) return;
        float remaining = expiresAt - now;
        if (remaining <= 0f)
        {
            Hide();
            return;
        }

        float fadeDuration = Mathf.Min(0.35f, (expiresAt - startedAt) * 0.25f);
        float fade = Mathf.Clamp01(remaining / fadeDuration);
        float pulse = 0.9f + 0.1f * Mathf.Cos((now - startedAt) * 7f);
        Color tint = fillColor;
        // Several real fields can overlap; keep their accumulated fill below the units.
        tint.a *= fade * pulse * .40f;
        properties.SetColor(TintProperty, tint);
        fill.SetPropertyBlock(properties);
        Color border = edgeColor;
        border.a *= fade * .60f;
        edge.startColor = border;
        edge.endColor = border;
        CellImpactStyle style=CellCombatFeedback.Artwork!=null?CellCombatFeedback.Artwork.Find(CellImpactKind.Acid):null;
        if(style!=null&&style.frames!=null&&style.frames.Length==6)
        {
            bubbles.sprite=style.frames[3+Mathf.FloorToInt((now-startedAt)*8f)%3];
            bubbles.color=new Color(1f,1f,1f,.22f*fade);
        }
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    public static Mesh CreateUnitMesh()
    {
        // Solid inner disc and a transparent outer edge share the actual unit radius.
        Vector3[] vertices = new Vector3[1 + Segments * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[Segments * 9];
        colors[0] = Color.white;
        uv[0] = Vector2.one * 0.5f;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            int inner = 1 + i;
            int outer = 1 + Segments + i;
            vertices[inner] = direction * 0.78f;
            vertices[outer] = direction;
            uv[inner] = new Vector2(vertices[inner].x, vertices[inner].y) * 0.5f + Vector2.one * 0.5f;
            uv[outer] = new Vector2(direction.x, direction.y) * 0.5f + Vector2.one * 0.5f;
            colors[inner] = Color.white;
            colors[outer] = new Color(1f, 1f, 1f, 0f);
            int nextInner = 1 + (i + 1) % Segments;
            int nextOuter = nextInner + Segments;
            int index = i * 9;
            triangles[index] = 0;
            triangles[index + 1] = inner;
            triangles[index + 2] = nextInner;
            triangles[index + 3] = inner;
            triangles[index + 4] = outer;
            triangles[index + 5] = nextOuter;
            triangles[index + 6] = inner;
            triangles[index + 7] = nextOuter;
            triangles[index + 8] = nextInner;
        }
        Mesh mesh = new Mesh { name = "Acid Unit Disc" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
