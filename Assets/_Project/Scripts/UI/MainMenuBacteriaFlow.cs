using UnityEngine;
using UnityEngine.UI;

// Independent sprites follow the channel. Bodies translate and rotate without UV distortion.
[RequireComponent(typeof(Image))]
public sealed class MainMenuBacteriaFlow : MonoBehaviour
{
    private const int Count = 10;
    private readonly Image[] bacteria = new Image[Count];
    private readonly RectTransform[] bodies = new RectTransform[Count];
    private RectTransform board;
    private float elapsed;
    private static readonly Vector2[] Channel =
    {
        new Vector2(.916f,.928f), new Vector2(.873f,.837f),
        new Vector2(.793f,.797f), new Vector2(.740f,.724f),
        new Vector2(.699f,.635f), new Vector2(.629f,.568f),
        new Vector2(.573f,.492f), new Vector2(.573f,.408f),
        new Vector2(.577f,.319f), new Vector2(.503f,.232f),
        new Vector2(.431f,.157f), new Vector2(.362f,.063f)
    };

    private void Awake()
    {
        MainMenuBacteriaArtwork art = Resources.Load<MainMenuBacteriaArtwork>("MainMenuBacteriaArtwork");
        if (art == null || art.background == null || art.bacteria == null || art.bacteria.Length == 0)
        { enabled = false; return; }
        Image background = GetComponent<Image>();
        background.sprite = art.background;
        board = background.rectTransform;
        for (int i = 0; i < Count; i++)
        {
            GameObject go = new GameObject("Flowing Purple Bacteria " + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            bodies[i] = go.GetComponent<RectTransform>();
            bodies[i].anchorMin = bodies[i].anchorMax = bodies[i].pivot = Vector2.one * .5f;
            bacteria[i] = go.GetComponent<Image>();
            bacteria[i].sprite = art.bacteria[i % art.bacteria.Length];
            bacteria[i].raycastTarget = false;
            bacteria[i].preserveAspect = true;
        }
        RenderAt(0f);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        RenderAt(elapsed);
    }

    public void RenderAt(float seconds)
    {
        if (board == null) return;
        Rect rect = board.rect;
        for (int i = 0; i < Count; i++)
        {
            // Slight speed differences make a stream rather than a rigid procession.
            float progress = Mathf.Repeat(i / (float)Count + seconds * (.033f + (i % 3) * .002f), 1f);
            Vector2 uv = Position(progress);
            Vector2 direction = Position(Mathf.Min(1f, progress + .002f)) - Position(Mathf.Max(0f, progress - .002f));
            Vector2 tangent = new Vector2(direction.x * rect.width, direction.y * rect.height).normalized;
            Vector2 lane = new Vector2(-tangent.y, tangent.x) * rect.height * ((i % 3 - 1) * .007f);
            bodies[i].anchoredPosition = new Vector2((uv.x-.5f)*rect.width,(uv.y-.5f)*rect.height) + lane;
            float width = rect.width * (.046f + (i % 3) * .006f);
            Sprite sprite = bacteria[i].sprite;
            bodies[i].sizeDelta = new Vector2(width,width*sprite.rect.height/sprite.rect.width);
            float angle = i % 3 == 1 ? Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg-45f : i*37f+seconds*4f;
            bodies[i].localRotation = Quaternion.Euler(0f,0f,angle);
            float alpha = Mathf.SmoothStep(0f,1f,Mathf.Min(progress/.045f,(.94f-progress)/.06f));
            bacteria[i].color = new Color(1f,1f,1f,alpha);
        }
    }

    private static Vector2 Position(float progress)
    {
        float scaled = Mathf.Clamp01(progress) * (Channel.Length-1);
        int index = Mathf.Min(Mathf.FloorToInt(scaled),Channel.Length-2);
        float t=scaled-index;
        Vector2 a=Channel[Mathf.Max(0,index-1)], b=Channel[index];
        Vector2 c=Channel[index+1], d=Channel[Mathf.Min(Channel.Length-1,index+2)];
        return .5f*((2f*b)+(-a+c)*t+(2f*a-5f*b+4f*c-d)*t*t+(-a+3f*b-3f*c+d)*t*t*t);
    }
}
