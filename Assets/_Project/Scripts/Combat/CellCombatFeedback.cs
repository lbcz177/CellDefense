using UnityEngine;
using UnityEngine.SceneManagement;

// A bounded, scene-local pool. Contact poses have priority over emissions and travel crumbs.
public sealed class CellCombatFeedback : MonoBehaviour
{
    private const int Capacity = 96;
    private static readonly float[] ContactEnds = { .03f, .065f, .11f, .16f, .22f, .30f };
    private static CellCombatFeedback active;
    private static CellImpactArtwork artwork;
    private readonly Effect[] effects = new Effect[Capacity];
    private readonly int[] contactCounts = new int[8];
    private OpenFieldPrototypeController owner;
    public static CellImpactArtwork Artwork => artwork != null ? artwork : artwork = Resources.Load<CellImpactArtwork>("CellImpactArtwork");
    public static bool IsReady => active != null && Artwork != null;
    public int ActiveEffectCount { get { int count=0;foreach(Effect effect in effects)if(effect.renderer.enabled)count++;return count; } }
    public int ContactCount(CellImpactKind kind) => contactCounts[(int)kind];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= Ensure;SceneManager.sceneLoaded += Ensure;
        Ensure(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    private static void Ensure(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<OpenFieldPrototypeController>(true) != null)
            {
                foreach (GameObject item in scene.GetRootGameObjects()) if (item.GetComponent<CellCombatFeedback>() != null) return;
                var host = new GameObject("Cell contact feedback");SceneManager.MoveGameObjectToScene(host, scene);
                host.AddComponent<CellCombatFeedback>();return;
            }
    }
    private void Awake()
    {
        active = this;owner = FindObjectOfType<OpenFieldPrototypeController>();
        for (int i=0;i<Capacity;i++)
        {
            var obj = new GameObject("Pooled cell contact");obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();renderer.sortingOrder = 28;renderer.enabled = false;
            if (Artwork != null && Artwork.spriteMaterial != null) renderer.sharedMaterial = Artwork.spriteMaterial;
            effects[i] = new Effect { renderer=renderer };
        }
    }
    public static void Release(CellImpactKind kind, Component source, Vector3 destination)
    {
        if (source == null) return;
        TowerImpactRecoil recoil = source.GetComponent<TowerImpactRecoil>();
        if (recoil != null) recoil.Play(destination - source.transform.position, kind == CellImpactKind.Complement ? 1.1f : .7f);
        if (!IsReady) return;
        CellImpactStyle style = Artwork.Find(kind);
        if (style != null) active.Spawn(style.frames, source.transform.position, .37f, .13f, 1, 3, .85f);
        CombatHitFeedback.CellAction(kind, source.transform.position, false);
    }
    public static void Impact(CellImpactKind kind, Vector3 position, bool subtle = false)
    {
        if (!IsReady) return;
        CellImpactStyle style=Artwork.Find(kind);if (style == null) return;
        active.contactCounts[(int)kind]++;
        active.Spawn(style.frames, position, style.worldSize * (subtle ? .45f : 1f), .30f, 2, 6, subtle ? .5f : 1f);
        if (!subtle) CombatHitFeedback.CellAction(kind, position, true);
    }
    public static void PrepareProjectile(Projectile projectile, CellImpactKind kind)
    {
        if (projectile == null || Artwork == null) return;
        CellImpactStyle style=Artwork.Find(kind);
        if (style != null && style.projectile != null)
            projectile.SetVisual(style.projectile, Color.white, kind == CellImpactKind.Acid ? .28f : kind == CellImpactKind.Antibody ? .24f : .19f);
        projectile.SetFeedback(kind);
    }
    public static void Trail(CellImpactKind kind, Vector3 position)
    {
        if (!IsReady) return;
        CellImpactStyle style=Artwork.Find(kind);
        if (style?.Trail != null) active.Spawn(style.Trail, position, kind == CellImpactKind.Acid ? .14f : .10f, .12f, 0, 1, .45f);
    }
    private void Spawn(Sprite[] frames, Vector3 position, float size, float duration, int priority, int count, float opacity)
    {
        if (frames == null || frames.Length < count || frames[0] == null || owner == null || !owner.HasActiveRun) return;
        Effect selected=null;
        foreach(Effect effect in effects)
        {
            if (!effect.renderer.enabled) { selected=effect;break; }
            if (effect.priority <= priority && effect.startedFrame != Time.frameCount &&
                (selected == null || effect.priority < selected.priority || effect.priority == selected.priority && effect.age > selected.age)) selected=effect;
        }
        if (selected == null) return;
        selected.frames=frames;selected.count=count;selected.age=0f;selected.duration=duration;selected.priority=priority;
        selected.startedFrame=Time.frameCount;selected.opacity=opacity;selected.size=size;
        selected.renderer.transform.SetPositionAndRotation(position, Quaternion.identity);
        selected.renderer.sprite=frames[0];selected.renderer.color=new Color(1f,1f,1f,opacity);
        selected.renderer.transform.localScale=Vector3.one*(size/Mathf.Max(.001f,frames[0].bounds.size.x));
        selected.renderer.sortingOrder=priority==0 ? 12 : 28;selected.renderer.enabled=true;
    }
    private void LateUpdate()
    {
        if (owner == null || !owner.HasActiveRun) { foreach(Effect e in effects)e.renderer.enabled=false;return; }
        if (owner.IsPaused || Time.deltaTime<=0f) return;
        foreach(Effect effect in effects)
        {
            if (!effect.renderer.enabled || effect.startedFrame==Time.frameCount) continue;
            effect.age+=Time.deltaTime;
            if (effect.age>=effect.duration) { effect.renderer.enabled=false;continue; }
            int frame=0;
            if (effect.count==6) { while(frame<5 && effect.age>=ContactEnds[frame])frame++; }
            else frame=Mathf.Min(effect.count-1,Mathf.FloorToInt(effect.age/effect.duration*effect.count));
            effect.renderer.sprite=effect.frames[frame];
            if (effect.priority==0)
            {
                float phase=effect.age/effect.duration;effect.renderer.color=new Color(1f,1f,1f,effect.opacity*(1f-phase));
                effect.renderer.transform.localScale=Vector3.one*(effect.size/Mathf.Max(.001f,effect.frames[0].bounds.size.x))*(1f-.4f*phase);
            }
        }
    }
    private void OnDestroy() { if(active==this)active=null; }
    private sealed class Effect
    {
        public SpriteRenderer renderer;public Sprite[] frames;
        public float age,duration,opacity,size;public int priority,count,startedFrame;
    }
}
