using UnityEngine;

// Only presents the existing region; its geometry and clock are supplied by the rules owner.
public sealed class InflammationRegionVisual : MonoBehaviour
{
    private SpriteRenderer frame,tissue,warning;
    private readonly SpriteRenderer[] motes=new SpriteRenderer[10];
    private Vector2 size;
    public void Initialize(GuidanceArtwork art,Rect bounds,Vector3 origin)
    {
        transform.position=origin+(Vector3)bounds.center;size=bounds.size;
        frame=Make("Tissue membrane boundary",art.regionFrame,-10);frame.drawMode=SpriteDrawMode.Sliced;frame.size=size;
        tissue=Make("Local tissue texture",art.tissue,-17);tissue.drawMode=SpriteDrawMode.Tiled;tissue.size=Vector2.Max(Vector2.one*.1f,size-Vector2.one*.5f);
        warning=Make("Overheat warning vesicle",art.warning,26);warning.transform.localPosition=new Vector3(-size.x*.5f+.3f,size.y*.5f-.3f);warning.transform.localScale=Vector3.one*.30f;
        for(int i=0;i<motes.Length;i++){motes[i]=Make("Cytokine vesicle",art.particle,-9);motes[i].transform.localScale=Vector3.one*(.06f+(i%3)*.025f);}
    }
    private SpriteRenderer Make(string name,Sprite sprite,int order)
    {
        var obj=new GameObject(name);obj.transform.SetParent(transform,false);var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;return renderer;
    }
    public void Render(InflammationRegionState region,float seconds)
    {
        float intensity=region.Level/100f;
        Color tint=region.IsDamaging?new Color(1f,.38f,.29f):region.IsOverheated?new Color(1f,.65f,.32f):region.IsActivated?new Color(1f,.86f,.48f):new Color(.62f,.95f,.82f);
        float pulse=region.IsOverheated?.84f+.16f*Mathf.Sin(seconds*6f):.95f+.05f*Mathf.Sin(seconds*2f);
        frame.color=new Color(tint.r,tint.g,tint.b,pulse);
        tissue.color=new Color(tint.r,tint.g,tint.b,.07f+.16f*intensity);
        warning.enabled=region.IsOverheated;warning.color=new Color(1f,1f,1f,pulse);warning.transform.localScale=Vector3.one*(.30f+.025f*Mathf.Sin(seconds*6f));
        for(int i=0;i<motes.Length;i++)
        {
            motes[i].enabled=i<2+Mathf.FloorToInt(intensity*8f);
            float x=Mathf.Repeat(i*.618f+Mathf.Sin(seconds*.4f+i)*.035f,1f);
            float y=Mathf.Repeat(i*.371f+seconds*(.025f+intensity*.035f),1f);
            motes[i].transform.localPosition=new Vector3((x-.5f)*Mathf.Max(0f,size.x-.7f),(y-.5f)*Mathf.Max(0f,size.y-.7f));
            motes[i].color=new Color(tint.r,tint.g,tint.b,.30f+intensity*.35f);
        }
    }
}
