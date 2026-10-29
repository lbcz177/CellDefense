using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuFocusGraphic : MaskableGraphic
{
    private bool pressed;
    public void SetPressed(bool value) { if (pressed == value) return;pressed = value;SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();Rect r = GetPixelAdjustedRect();
        Color gold = pressed ? new Color(1f,.82f,.4f,1f) : new Color(1f,.94f,.68f,.95f);
        Color fill = gold;fill.a = pressed ? .22f : .07f;
        Quad(mesh,r.xMin,r.yMin,r.width,r.height,fill);
        float thickness = pressed ? 4f : 3f;
        float corner = Mathf.Min(20f,r.height*.25f);
        foreach(float x in new[]{r.xMin,r.xMax-corner})
            foreach(float y in new[]{r.yMin,r.yMax-thickness})Quad(mesh,x,y,corner,thickness,gold);
        foreach(float x in new[]{r.xMin,r.xMax-thickness})
            foreach(float y in new[]{r.yMin,r.yMax-corner})Quad(mesh,x,y,thickness,corner,gold);
    }
    private static void Quad(VertexHelper mesh,float x,float y,float w,float h,Color color)
    {
        int index=mesh.currentVertCount;
        mesh.AddVert(new Vector3(x,y),color,Vector2.zero);mesh.AddVert(new Vector3(x,y+h),color,Vector2.zero);
        mesh.AddVert(new Vector3(x+w,y+h),color,Vector2.zero);mesh.AddVert(new Vector3(x+w,y),color,Vector2.zero);
        mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
    }
}
