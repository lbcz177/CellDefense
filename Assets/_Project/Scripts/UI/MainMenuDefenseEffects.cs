using UnityEngine;
using UnityEngine.UI;

// Small pixel-shaped UI particles add actions to the baked defenders without stretching them.
public sealed class MainMenuDefenseEffects : MaskableGraphic
{
    private float elapsed;
    private static readonly Color Cyan = new Color(.32f,.91f,1f,1f);
    private static readonly Color Gold = new Color(1f,.85f,.21f,1f);
    private static readonly Color Purple = new Color(.94f,.18f,.76f,1f);

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    private void Update() => RenderAt(elapsed + Time.unscaledDeltaTime);

    public void RenderAt(float seconds)
    {
        elapsed = seconds;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        // Cyan pulses travel out from the dendritic cell and neutrophil toward the channel.
        Pellets(mesh,new Vector2(.591f,.663f),new Vector2(.645f,.603f),3,0f);
        Pellets(mesh,new Vector2(.487f,.431f),new Vector2(.555f,.438f),5,.6f);
        Antibodies(mesh,new Vector2(.823f,.580f),new Vector2(.733f,.535f),0f);
        Antibodies(mesh,new Vector2(.817f,.507f),new Vector2(.725f,.478f),1.4f);
        // Debris contracts toward the macrophage mouth, suggesting ingestion rather than a whole-body squash.
        for(int i=0;i<3;i++)
        {
            float p=Mathf.Repeat(elapsed/3.4f+i*.33f,1f);
            if(p>.55f)continue;
            float t=p/.55f;
            Vector2 a=new Vector2(.709f+i*.008f,.278f-i*.017f);
            Vector2 b=new Vector2(.762f,.275f);
            Pixel(mesh,Vector2.Lerp(a,b,Mathf.SmoothStep(0f,1f,t)),
                Mathf.Lerp(.0045f,.002f,t),Purple,Mathf.Sin(t*Mathf.PI)*.85f);
        }
    }

    private void Pellets(VertexHelper mesh,Vector2 from,Vector2 to,int count,float offset)
    {
        for(int i=0;i<count;i++)
        {
            float p=Mathf.Repeat((elapsed-offset)/3.2f-i*.13f,1f);
            if(p>.4f)continue;
            float t=p/.4f;
            Vector2 position=Vector2.Lerp(from,to,t);
            position.y+=(i%2==0?1f:-1f)*.003f*Mathf.Sin(t*Mathf.PI);
            Pixel(mesh,position,.0045f,Cyan,Mathf.Sin(t*Mathf.PI)*.9f);
        }
    }

    private void Antibodies(VertexHelper mesh,Vector2 from,Vector2 to,float offset)
    {
        for(int i=0;i<2;i++)
        {
            float p=Mathf.Repeat((elapsed-offset)/3.6f-i*.5f,1f);
            if(p>.48f)continue;
            float t=p/.48f;
            Vector2 uv=Vector2.Lerp(from,to,t);
            float alpha=Mathf.Sin(Mathf.PI*t)*.95f;
            Vector2 center=Local(uv);
            float size=rectTransform.rect.width*.013f;
            float angle=Mathf.Sin(elapsed+i)*.16f;
            Color edge=new Color(.44f,.20f,.16f,alpha);
            Color fill=new Color(Gold.r,Gold.g,Gold.b,alpha);
            for(int layer=0;layer<2;layer++)
            {
                Color tint=layer==0?edge:fill;
                float width=size*(layer==0?.27f:.15f);
                Vector2 basePoint=Rotate(new Vector2(0,-.5f)*size,angle)+center;
                Vector2 fork=Rotate(new Vector2(0,.05f)*size,angle)+center;
                Stroke(mesh,basePoint,fork,width,tint);
                Stroke(mesh,fork,Rotate(new Vector2(-.42f,.5f)*size,angle)+center,width,tint);
                Stroke(mesh,fork,Rotate(new Vector2(.42f,.5f)*size,angle)+center,width,tint);
            }
        }
    }

    private Vector2 Local(Vector2 uv)
    {
        Rect r=rectTransform.rect;
        return new Vector2(r.xMin+uv.x*r.width,r.yMin+uv.y*r.height);
    }

    private static Vector2 Rotate(Vector2 point,float angle)
    {
        float s=Mathf.Sin(angle),c=Mathf.Cos(angle);
        return new Vector2(point.x*c-point.y*s,point.x*s+point.y*c);
    }

    private void Pixel(VertexHelper mesh,Vector2 uv,float width,Color tint,float alpha)
    {
        if(alpha<.01f)return;
        Vector2 p=Local(uv);
        float size=rectTransform.rect.width*width;
        Color edge=new Color(tint.r*.25f,tint.g*.35f,tint.b*.45f,alpha*.7f);
        Box(mesh,p,size,edge);
        tint.a=alpha;Box(mesh,p,size*.68f,tint);
        Box(mesh,p+new Vector2(-size*.12f,size*.12f),size*.25f,new Color(1f,1f,1f,alpha));
    }

    private static void Box(VertexHelper mesh,Vector2 center,float size,Color color)
    {
        Vector2 half=Vector2.one*(size*.5f);
        Quad(mesh,center-half,center+new Vector2(-half.x,half.y),center+half,
            center+new Vector2(half.x,-half.y),color);
    }

    private static void Stroke(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color)
    {
        Vector2 delta=(b-a).normalized;
        Vector2 side=new Vector2(-delta.y,delta.x)*(width*.5f);
        Quad(mesh,a-side,a+side,b+side,b-side,color);
    }

    private static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {
        int n=mesh.currentVertCount;
        Vector2 uv=new Vector2(.5f,.5f);
        mesh.AddVert(a,color,uv);mesh.AddVert(b,color,uv);mesh.AddVert(c,color,uv);mesh.AddVert(d,color,uv);
        mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
    }
}
