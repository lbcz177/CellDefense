using System;
using UnityEngine;

[Serializable]
public sealed class InflammationRegionState
{
    public string label;
    public Rect bounds;
    public Sprite overlay;
    public float Level { get; private set; }
    public float WarningElapsed { get; private set; }
    public bool IsOverheated { get; private set; }
    public bool IsDamaging => IsOverheated && WarningElapsed >= 4f;
    public bool IsActivated => !IsOverheated && Level >= 20f;
    public void Tick(float deltaTime,int sources,bool regulated)
    {
        if(deltaTime<=0f||float.IsNaN(deltaTime)||float.IsInfinity(deltaTime))return;
        float rate=sources>0 ? Mathf.Min(sources,2)*8f : -8f;
        if(regulated)rate-=6f;
        Level=Mathf.Clamp(Level+rate*deltaTime,0f,100f);
        bool wasHot=IsOverheated;
        if(Level>=70f)IsOverheated=true;
        else if(Level<=60f)IsOverheated=false;
        if(IsOverheated && wasHot)WarningElapsed+=deltaTime;
        else WarningElapsed=0f;
    }
    public void Reset() { Level=WarningElapsed=0f;IsOverheated=false; }
}
