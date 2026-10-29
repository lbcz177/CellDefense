using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-30)]
public sealed class ChapterThreeInflammation : MonoBehaviour
{
    [SerializeField] private InflammationRegionState[] regions;
    private OpenFieldPrototypeController owner;
    private SpriteRenderer[] overlays;
    private LineRenderer[] borders;
    private Material lineMaterial;
    private Canvas canvas;
    private PixelImageText status;
    private bool finished;
    private InflammationRegionVisual[] regionVisuals;
    private float visualTime;
    public InflammationRegionState[] Regions => regions;
    private readonly List<TowerCombatState> defenders=new List<TowerCombatState>();
    private void Start()
    {
        owner=GetComponent<OpenFieldPrototypeController>();
        lineMaterial=new Material(Shader.Find("Sprites/Default"));
        overlays=new SpriteRenderer[regions.Length];borders=new LineRenderer[regions.Length];
        GuidanceArtwork art=GuidanceArtwork.Load();regionVisuals=new InflammationRegionVisual[regions.Length];
        for(int i=0;i<regions.Length;i++)
        {
            regions[i].Reset();
            var visual=new GameObject("Inflammation zone "+regions[i].label);visual.transform.SetParent(transform,false);
            var image=visual.AddComponent<SpriteRenderer>();image.sprite=regions[i].overlay;image.sortingOrder=-18;
            if(image.sprite!=null)visual.transform.localScale=new Vector3(15f/image.sprite.bounds.size.x,11f/image.sprite.bounds.size.y,1f);
            overlays[i]=image;
            var border=new GameObject("Zone boundary");border.transform.SetParent(transform,false);
            LineRenderer line=border.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.widthMultiplier=.035f;
            line.sortingOrder=-10;line.useWorldSpace=true;line.loop=true;line.positionCount=4;
            Rect r=regions[i].bounds;
            Vector3 origin=transform.position-new Vector3(7.5f,5.5f);
            line.SetPositions(new[]{origin+new Vector3(r.xMin,r.yMin),origin+new Vector3(r.xMin,r.yMax),origin+new Vector3(r.xMax,r.yMax),origin+new Vector3(r.xMax,r.yMin)});
            borders[i]=line;
            if(art!=null)
            {
                var membrane=new GameObject("Inflammation tissue "+regions[i].label);membrane.transform.SetParent(transform,false);
                regionVisuals[i]=membrane.AddComponent<InflammationRegionVisual>();regionVisuals[i].Initialize(art,r,origin);line.enabled=false;
            }
        }
        var ui=new GameObject("Inflammation Status",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        ui.layer=LayerMask.NameToLayer("UI");ui.transform.SetParent(transform,false);canvas=ui.GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=6;
        var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("Region Status Panel",typeof(RectTransform),typeof(Image));panel.layer=ui.layer;panel.transform.SetParent(ui.transform,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.right;
        rect.anchoredPosition=new Vector2(-16,16);rect.sizeDelta=new Vector2(320,100);
        PromptImageResources.StylePanel(panel.GetComponent<Image>());panel.GetComponent<Image>().raycastTarget=false;
        var text=new GameObject("Region Status Text",typeof(RectTransform),typeof(PixelImageText));text.layer=ui.layer;text.transform.SetParent(panel.transform,false);
        status=text.GetComponent<PixelImageText>();status.raycastTarget=false;status.fontSize=16;status.alignment=TextAlignmentOptions.TopLeft;
        status.color=new Color(1f,.95f,.8f);status.rectTransform.anchorMin=Vector2.zero;status.rectTransform.anchorMax=Vector2.one;
        status.rectTransform.offsetMin=new Vector2(12,10);status.rectTransform.offsetMax=new Vector2(-12,-10);
        MobileSafeFrame.Wrap(canvas,new Vector2(1280,720));RefreshVisuals();
    }
    private void ResetModifiers()
    {
        foreach(TowerCombatState cell in owner.Defenders)
        {
            if(cell==null||cell.Tower==null)continue;
            cell.Tower.EnvironmentalDamageMultiplier=1f;
            var b=cell.GetComponent<AntibodyAttackBehaviour>();if(b!=null)b.SupportMarkDurationBonus=0f;
            var support=cell.GetComponent<ImmuneSupportBehaviour>();
            if(support!=null){support.SupportedAntibody=null;support.SupportedRegion=-1;support.GetComponent<ChapterTwoLinkVisual>().Hide();}
        }
    }
    private void Update()
    {
        if(owner==null)return;
        if(!owner.isActiveAndEnabled||!owner.HasActiveRun)
        {
            if(!finished){ResetModifiers();foreach(var region in regions)region.Reset();finished=true;}
            canvas.enabled=false;foreach(var border in borders)border.enabled=false;foreach(var overlay in overlays)overlay.enabled=false;
            foreach(var region in regionVisuals)if(region!=null)region.gameObject.SetActive(false);return;
        }
        if(owner.IsPaused||Time.deltaTime<=0f)return;
        visualTime+=Time.deltaTime;Tick(Time.deltaTime);RefreshVisuals();
    }
    private void Tick(float deltaTime)
    {
        ResetModifiers();defenders.Clear();defenders.AddRange(owner.Defenders);
        bool[] regulated=new bool[regions.Length];
        foreach(TowerCombatState cell in defenders)
        {
            if(cell==null)continue;
            var support=cell.GetComponent<ImmuneSupportBehaviour>();if(support==null||!cell.IsAlive)continue;
            float range=cell.Tower.Definition.AttackRange;
            if(support.IsRegulatory)
            {
                int best=-1;float distance=float.PositiveInfinity;
                for(int i=0;i<regions.Length;i++)
                {
                    // A regulator chooses one reachable zone by center; it never covers both by overlap.
                    Vector2 center=regions[i].bounds.center;Vector3 point=transform.position+new Vector3(center.x-7.5f,center.y-5.5f);
                    float d=(point-cell.transform.position).sqrMagnitude;
                    if(d<=range*range && d<distance){distance=d;best=i;}
                }
                if(best>=0){regulated[best]=true;support.SupportedRegion=best;
                    Vector2 p=regions[best].bounds.center;
                    support.GetComponent<ChapterTwoLinkVisual>().Show(transform.position+new Vector3(p.x-7.5f,p.y-5.5f),1f,new Color(.4f,1f,.75f));}
            }
            else
            {
                AntibodyAttackBehaviour selected=null;float closest=float.PositiveInfinity;
                foreach(TowerCombatState candidate in defenders)
                {
                    var b=candidate.GetComponent<AntibodyAttackBehaviour>();if(b==null||!candidate.IsAlive)continue;
                    float d=(candidate.transform.position-cell.transform.position).sqrMagnitude;
                    if(d<=range*range && d<closest){closest=d;selected=b;}
                }
                if(selected!=null){selected.SupportMarkDurationBonus=2f;support.SupportedAntibody=selected;
                    support.GetComponent<ChapterTwoLinkVisual>().Show(selected.transform.position,1f,new Color(.55f,.85f,1f));}
            }
        }
        int[] sources=new int[regions.Length];
        foreach(EnemyController enemy in owner.ActiveEnemies)
        {
            if(!enemy.CanBeTargeted||!enemy.isActiveAndEnabled||!enemy.Definition.IsInflammationSource)continue;
            var marker=enemy.GetComponent<ChapterTwoLinkVisual>();if(marker!=null)marker.Hide();
            for(int i=0;i<regions.Length;i++)if(regions[i].bounds.Contains(owner.WorldToLogicalPosition(enemy.transform.position)))
            {
                sources[i]++;if(marker!=null){Vector2 p=regions[i].bounds.center;marker.Show(transform.position+new Vector3(p.x-7.5f,p.y-5.5f),regions[i].Level/100f,new Color(1f,.55f,.3f));}
            }
        }
        for(int i=0;i<regions.Length;i++)regions[i].Tick(deltaTime,sources[i],regulated[i]);
        // Death callbacks remove defenders from the owner's dictionary; iterate the snapshot.
        foreach(TowerCombatState cell in defenders)
        {
            if(cell==null||!cell.IsAlive)continue;
            foreach(var region in regions)if(region.bounds.Contains(new Vector2(cell.Cell.x+.5f,cell.Cell.y+.5f)))
            {
                if(region.IsActivated)cell.Tower.EnvironmentalDamageMultiplier=1.15f;
                if(region.IsDamaging)cell.TakeDamage(2f*deltaTime);
            }
        }
    }
    private void RefreshVisuals()
    {
        var lines=new List<string>();
        for(int i=0;i<regions.Length;i++)
        {
            var r=regions[i];Color color=r.IsOverheated?new Color(1f,.35f,.25f):r.IsActivated?new Color(1f,.85f,.35f):new Color(.45f,.9f,.8f);
            borders[i].startColor=borders[i].endColor=color;
            overlays[i].color=new Color(1f,1f,1f,.04f+r.Level*.0012f);
            if(regionVisuals[i]!=null)regionVisuals[i].Render(r,visualTime);
            string state=r.IsDamaging?"过热·细胞受伤":r.IsOverheated?$"预警 {Mathf.CeilToInt(4f-r.WarningElapsed)}秒":r.IsActivated?"激活·伤害+15%":"稳定";
            lines.Add($"{r.label} {r.Level:0}/100  {state}");
        }
        status.text="局部炎症\n"+string.Join("\n",lines);
    }
    private void OnDisable()
    {
        if(owner!=null)ResetModifiers();if(canvas!=null)canvas.enabled=false;
        if(borders!=null)foreach(var b in borders)if(b!=null)b.enabled=false;
        if(overlays!=null)foreach(var o in overlays)if(o!=null)o.enabled=false;
        if(regionVisuals!=null)foreach(var r in regionVisuals)if(r!=null)r.gameObject.SetActive(false);
    }
    private void OnDestroy(){if(lineMaterial!=null)Destroy(lineMaterial);}
}
