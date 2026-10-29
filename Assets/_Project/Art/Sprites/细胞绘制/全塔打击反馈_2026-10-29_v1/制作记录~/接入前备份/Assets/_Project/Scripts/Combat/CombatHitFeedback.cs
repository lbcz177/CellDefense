using UnityEngine;
using UnityEngine.SceneManagement;

// Local impact feedback in the existing gameplay levels. Uses a fixed pool of pixel sparks.
public sealed class CombatHitFeedback : MonoBehaviour
{
    [SerializeField, Range(1,12)] private int hitSparkCount=6;
    [SerializeField, Min(0.05f)] private float hitDuration=0.18f;
    [SerializeField, Min(0.01f)] private float pixelSize=0.06f;
    [SerializeField] private bool soundEnabled=true;
    [SerializeField, Range(0f,1f)] private float soundVolume=0.14f;
    private const int Capacity=96;
    private static CombatHitFeedback active;
    private readonly System.Random random=new System.Random(52137);
    private Spark[] sparks;
    private AudioSource[] voices;
    private Sprite pixelSprite;
    private AudioClip shotSound,hitSound,coreSound;
    private bool audioPaused;
    private int cursor;
    public int ShotCount { get; private set; }
    public int HitCount { get; private set; }
    public int CoreImpactCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnableForGameplay()
    {
        SceneManager.sceneLoaded-=EnsureForScene;
        SceneManager.sceneLoaded+=EnsureForScene;
        EnsureForScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);
    }

    private static void EnsureForScene(Scene scene,LoadSceneMode mode)
    {
        if (scene.name!="OpenFieldPrototype" && scene.name!="OpenFieldLevel02" &&
            scene.name!="Prototype" && scene.name!="Level02") return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<CombatHitFeedback>(true)!=null) return;
        GameObject host=new GameObject("Combat Feedback");
        SceneManager.MoveGameObjectToScene(host,scene);
        host.AddComponent<CombatHitFeedback>();
    }

    private void Awake()
    {
        active=this;
        pixelSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*0.5f,1f);
        sparks=new Spark[Capacity];
        for (int i=0;i<Capacity;i++)
        {
            GameObject particle=new GameObject("Pixel spark");particle.transform.SetParent(transform,false);
            SpriteRenderer renderer=particle.AddComponent<SpriteRenderer>();renderer.sprite=pixelSprite;
            renderer.sortingLayerName="Tower";renderer.sortingOrder=100;renderer.enabled=false;
            sparks[i]=new Spark {renderer=renderer};
        }
        voices=new AudioSource[6];
        for (int i=0;i<voices.Length;i++)
        {
            AudioSource voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;
            voice.spatialBlend=0f;voice.loop=false;voices[i]=voice;
        }
        shotSound=MakeSound("Cell emission",0.045f,1050f,430f,0.08f);
        hitSound=MakeSound("Cell impact",0.075f,650f,130f,0.24f);
        coreSound=MakeSound("Core impact",0.12f,230f,70f,0.18f);
    }

    public static void Shot(Vector3 position)
    {
        if (active==null) return;
        active.ShotCount++;active.PlaySound(active.shotSound,position,0.7f);
    }

    public static void Hit(Vector3 position)
    {
        if (active==null) return;
        active.HitCount++;
        active.Burst(position,active.hitSparkCount,active.hitDuration,active.pixelSize,new Color(0.3f,0.95f,1f));
        active.PlaySound(active.hitSound,position,1f);
    }

    public static void CoreImpact(Vector3 position)
    {
        if (active==null) return;
        active.CoreImpactCount++;
        active.Burst(position,10,0.25f,active.pixelSize*1.5f,new Color(1f,0.45f,0.35f));
        active.PlaySound(active.coreSound,position,1f);
    }

    private void Burst(Vector3 position,int count,float duration,float size,Color color)
    {
        for (int i=0;i<count;i++)
        {
            Spark spark=sparks[cursor];cursor=(cursor+1)%Capacity;
            float angle=Range(0f,Mathf.PI*2f);
            spark.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0f)*Range(0.6f,1.3f);
            spark.age=0f;spark.duration=duration;spark.size=size*Range(0.7f,1.2f);
            spark.color=i<2 ? Color.white : color;
            spark.renderer.transform.position=position;
            spark.renderer.transform.localScale=Vector3.one*spark.size;
            spark.renderer.color=spark.color;spark.renderer.enabled=true;
        }
    }

    private void Update()
    {
        bool paused=Time.timeScale<=0f;
        if (paused!=audioPaused)
        {
            foreach (AudioSource voice in voices) { if (paused) voice.Pause();else voice.UnPause(); }
            audioPaused=paused;
        }
        if (Time.deltaTime<=0f) return;
        foreach (Spark spark in sparks)
        {
            if (!spark.renderer.enabled) continue;
            spark.age+=Time.deltaTime;
            if (spark.age>=spark.duration) { spark.renderer.enabled=false;continue; }
            float progress=spark.age/spark.duration;
            spark.renderer.transform.position+=spark.velocity*Time.deltaTime;
            spark.velocity*=Mathf.Max(0f,1f-5f*Time.deltaTime);
            spark.renderer.transform.localScale=Vector3.one*spark.size*(1f-0.65f*progress);
            Color color=spark.color;color.a=1f-progress;spark.renderer.color=color;
        }
    }

    private void PlaySound(AudioClip clip,Vector3 position,float volume)
    {
        if (!soundEnabled || Time.timeScale<=0f) return;
        foreach (AudioSource voice in voices)
        {
            if (voice.isPlaying) continue;
            voice.clip=clip;voice.volume=soundVolume*volume;voice.pitch=Range(0.96f,1.04f);
            Camera camera=Camera.main;
            voice.panStereo=camera==null ? 0f : Mathf.Clamp((position.x-camera.transform.position.x)/12f,-0.6f,0.6f);
            voice.Play();return;
        }
    }

    private AudioClip MakeSound(string name,float length,float startFrequency,float endFrequency,float noise)
    {
        const int rate=22050;
        float[] samples=new float[Mathf.CeilToInt(length*rate)];
        float phase=0f;
        for (int i=0;i<samples.Length;i++)
        {
            float progress=(float)i/samples.Length;
            phase+=Mathf.Lerp(startFrequency,endFrequency,progress)*Mathf.PI*2f/rate;
            float wave=Mathf.Sin(phase)*0.75f+Range(-1f,1f)*noise;
            samples[i]=wave*Mathf.Exp(-progress*7f)*Mathf.Min(1f,progress*30f);
        }
        AudioClip clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }

    private float Range(float minimum,float maximum) => minimum+(maximum-minimum)*(float)random.NextDouble();

    private void OnDestroy()
    {
        if (active==this) active=null;
        if (pixelSprite!=null) Destroy(pixelSprite);
        if (shotSound!=null) Destroy(shotSound);
        if (hitSound!=null) Destroy(hitSound);
        if (coreSound!=null) Destroy(coreSound);
    }

    private sealed class Spark
    {
        public SpriteRenderer renderer;
        public Vector3 velocity;
        public Color color;
        public float age,duration,size;
    }
}
