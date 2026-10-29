using UnityEngine;
using UnityEngine.UI;

// Local UV motion on the existing artwork keeps baked text and hit areas aligned.
[RequireComponent(typeof(Image))]
public sealed class MainMenuCellFlow : MonoBehaviour
{
    private static readonly int FlowTime = Shader.PropertyToID("_FlowTime");
    private static readonly int UvRect = Shader.PropertyToID("_UvRect");
    private Image image;
    private Material instance, previous;
    private float elapsed;

    public static void Attach(Canvas canvas)
    {
        foreach (Image candidate in canvas.GetComponentsInChildren<Image>(true))
        {
            if (candidate.sprite == null || candidate.sprite.name != "胞卫战_主界面_v1") continue;
            if (candidate.GetComponent<MainMenuCellFlow>() == null)
                candidate.gameObject.AddComponent<MainMenuCellFlow>();
            if (candidate.GetComponent<MainMenuBacteriaFlow>() == null)
                candidate.gameObject.AddComponent<MainMenuBacteriaFlow>();
            if (candidate.GetComponentInChildren<MainMenuDefenseEffects>() == null)
            {
                GameObject effects = new GameObject("Blue Defender Actions",typeof(RectTransform),typeof(CanvasRenderer));
                effects.layer = candidate.gameObject.layer;
                effects.transform.SetParent(candidate.transform,false);
                RectTransform rect = effects.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                effects.AddComponent<MainMenuDefenseEffects>();
            }
            return;
        }
    }

    private void Awake()
    {
        image = GetComponent<Image>();
        Material source = Resources.Load<Material>("MainMenuCellFlow");
        if (source == null || image.sprite == null) { enabled = false; return; }
        previous = image.material;
        instance = new Material(source) { name = "Main menu cell flow (instance)" };
        Rect rect = image.sprite.textureRect;
        Texture texture = image.sprite.texture;
        instance.SetVector(UvRect, new Vector4(rect.x / texture.width, rect.y / texture.height,
            rect.width / texture.width, rect.height / texture.height));
        image.material = instance;
    }

    private void Update()
    {
        if (instance == null || image.canvasRenderer.cull) return;
        elapsed += Time.unscaledDeltaTime;
        instance.SetFloat(FlowTime, elapsed);
    }

    private void OnDestroy()
    {
        if (image != null && image.material == instance) image.material = previous;
        if (instance != null) Destroy(instance);
    }
}
