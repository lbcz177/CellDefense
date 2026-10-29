using System;
using System.Reflection;
using NUnit.Framework;

public sealed class InflammationStateTests
{
    static Type Type=>System.Type.GetType("InflammationRegionState, Assembly-CSharp",true);
    static object Get(object o,string p)=>Type.GetProperty(p).GetValue(o);
    static void Tick(object o,float delta,int sources,bool regulated=false)=>Type.GetMethod("Tick").Invoke(o,new object[]{delta,sources,regulated});
    [Test] public void SourceActivationWarningDamageAndRecoveryHaveSeparateStages()
    {
        object state=Activator.CreateInstance(Type);Tick(state,4,0);Assert.That(Get(state,"Level"),Is.EqualTo(0f));
        Tick(state,4,1);Assert.That(Get(state,"IsActivated"),Is.True);
        Tick(state,5,1);Assert.That(Get(state,"IsOverheated"),Is.True);Assert.That(Get(state,"IsDamaging"),Is.False);
        Tick(state,3,1);Assert.That(Get(state,"IsDamaging"),Is.False);
        Tick(state,1,1);Assert.That(Get(state,"IsDamaging"),Is.True);Assert.That(Get(state,"Level"),Is.EqualTo(100f));
        Tick(state,5,0);Assert.That(Get(state,"IsOverheated"),Is.False);
        Tick(state,8,0);Assert.That(Get(state,"Level"),Is.EqualTo(0f));Assert.That(Get(state,"IsActivated"),Is.False);
    }
    [Test] public void SourcesAreCappedAndRegulationDoesNotAffectOtherRegions()
    {
        object left=Activator.CreateInstance(Type),right=Activator.CreateInstance(Type);
        Tick(left,1,8,true);Tick(right,1,8,false);
        Assert.That(Get(left,"Level"),Is.EqualTo(10f));Assert.That(Get(right,"Level"),Is.EqualTo(16f));
        Tick(left,0,8);Assert.That(Get(left,"Level"),Is.EqualTo(10f));
        Tick(left,1,0,true);Assert.That(Get(left,"Level"),Is.EqualTo(0f));Assert.That(Get(right,"Level"),Is.EqualTo(16f));
    }
    [Test] public void WarningHysteresisAvoidsRepeatedThresholdFlicker()
    {
        object state=Activator.CreateInstance(Type);Tick(state,9,1);Tick(state,.5f,0);
        Assert.That(Get(state,"IsOverheated"),Is.True);Tick(state,1,0);Assert.That(Get(state,"IsOverheated"),Is.False);
    }
}
