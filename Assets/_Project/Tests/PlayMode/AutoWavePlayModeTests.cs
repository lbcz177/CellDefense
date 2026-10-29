using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class AutoWavePlayModeTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly List<Object> owned=new List<Object>();
    int? previousScore;
    const string Key="CellDefense.LevelProgress.v1.OpenFieldPrototype.BestScore";
    static Type Runtime(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    static Component Owner()=>(Component)Object.FindObjectOfType(Runtime("OpenFieldPrototypeController"));
    static object Get(object o,string name)=>o.GetType().GetProperty(name,Flags).GetValue(o);
    static object Field(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethods(Flags).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);
    static Component[] Enemies(Component owner)=>((IEnumerable)Field(owner,"activeEnemies")).Cast<Component>().ToArray();
    static void Kill(Component enemy)=>enemy.GetType().GetMethod("TakeDamage",new[]{typeof(float)}).Invoke(enemy,new object[]{1000000f});
    [SetUp] public void Preserve()=>previousScore=PlayerPrefs.HasKey(Key)?(int?)PlayerPrefs.GetInt(Key):null;
    void FastWaves(Component owner)
    {
        Assert.That(Field(owner,"autoWaveDelay"),Is.EqualTo(10f));
        object original=Field(owner,"waveSequence");object definition=Call(Call(original,"GetWave",0),"GetGroupEnemy",0);
        Array waves=Array.CreateInstance(Runtime("WaveDefinition"),3);
        for(int i=0;i<3;i++)
        {
            ScriptableObject wave=ScriptableObject.CreateInstance(Runtime("WaveDefinition"));owned.Add(wave);
            Set(wave,"enemy",definition);Set(wave,"enemyCount",1);Set(wave,"spawnInterval",0f);Set(wave,"burstInterval",0f);
            waves.SetValue(wave,i);
        }
        ScriptableObject sequence=ScriptableObject.CreateInstance(Runtime("WaveSequenceDefinition"));owned.Add(sequence);
        Set(sequence,"waves",waves);Set(owner,"waveSequence",sequence);Set(owner,"scheduledEnemyCount",3);Set(owner,"autoWaveDelay",.4f);
    }
    [UnityTest] public IEnumerator FirstWaveIsManualAndSurvivorsDoNotPreventAutomaticSecondWave()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");Component owner=Owner();FastWaves(owner);
        yield return new WaitForSeconds(.5f);
        Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(1));Assert.That(Enemies(owner),Is.Empty);
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
        Call(owner,"StartAssault");
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.True);
        Component survivor=Enemies(owner).Single();((Behaviour)survivor.GetComponent(Runtime("PathFollower"))).enabled=false;
        float remaining=(float)Get(owner,"AutoWaveSecondsRemaining");Call(owner,"Pause");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(Get(owner,"AutoWaveSecondsRemaining"),Is.EqualTo(remaining).Within(.001f));
        Call(owner,"Resume");Call(owner,"SetPlaybackSpeed",2);
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(2),"Countdown uses scaled game time.");
        Assert.That(Enemies(owner).Contains(survivor),Is.True);
        Assert.That(Get(owner,"CurrentATP"),Is.EqualTo(140));
        Call(owner,"StartAssault");Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(3));
        Assert.That(Get(owner,"CurrentATP"),Is.EqualTo(180));
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
        yield return new WaitForSeconds(.5f);Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(3));
    }
    [UnityTest] public IEnumerator CountdownStartsOnlyAfterLastSpawnAndCaptionRendersWithoutMissingGlyphs()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");Component owner=Owner();FastWaves(owner);
        object first=Call(Field(owner,"waveSequence"),"GetWave",0);
        Set(first,"enemyCount",2);Set(first,"spawnInterval",.2f);Set(owner,"autoWaveDelay",10f);
        Call(owner,"StartAssault");
        yield return new WaitForSeconds(.1f);
        Assert.That(Enemies(owner).Length,Is.EqualTo(1));
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
        Assert.That(Call(owner,"StartAssault"),Is.False,"Spawning waves cannot overlap.");
        yield return new WaitForSeconds(.15f);
        Assert.That(Enemies(owner).Length,Is.EqualTo(2));
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.True);
        object buttons=Field(owner,"entranceWaveButtons");Call(buttons,"Refresh");
        Canvas.ForceUpdateCanvases();
        foreach(Component label in (Array)Field(buttons,"labels"))
        {
            Assert.That(Get(label,"text"),Is.EqualTo("第2波\n10秒后开始"));
            Assert.That(Get(label,"MissingGlyphCount"),Is.EqualTo(0));
            Assert.That(Get(label,"RenderedLineCount"),Is.EqualTo(2));
            Assert.That(Get(label,"RenderedGlyphCount"),Is.GreaterThan(0));
        }
        string destination=Environment.GetEnvironmentVariable("CELLDEFENSE_CHAPTER_CAPTURE_DIR");
        if(!string.IsNullOrEmpty(destination))
        {
            OpenFieldPlaybackPlayModeTests.CaptureUi(Camera.main,"auto-wave-countdown.png");
            System.IO.Directory.CreateDirectory(destination);
            System.IO.File.Copy(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                "../Logs/PauseArtwork/auto-wave-countdown.png")),System.IO.Path.Combine(destination,"auto-wave-countdown.png"),true);
        }
        Set(owner,"autoWaveRemaining",.2f);
        yield return new WaitForSeconds(.25f);
        Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator ClearingBeforeCountdownExpiresDoesNotGrantRoundRewardTwice()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");Component owner=Owner();FastWaves(owner);
        Call(owner,"StartAssault");Kill(Enemies(owner).Single());
        Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Planning"));
        Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(2));int atp=(int)Get(owner,"CurrentATP");
        yield return new WaitForSeconds(.45f);
        Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Assault"));
        Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(2));Assert.That(Get(owner,"CurrentATP"),Is.EqualTo(atp));
        Call(owner,"StartAssault");Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(3));
        Assert.That(Get(owner,"CurrentATP"),Is.EqualTo(atp+40));
        Assert.That(Call(owner,"StartAssault"),Is.False);Assert.That(Get(owner,"CurrentATP"),Is.EqualTo(atp+40));
        foreach(Component enemy in Enemies(owner))Kill(enemy);
        Assert.That(Get(owner,"Phase").ToString(),Is.EqualTo("Victory"));
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
    }
    [UnityTest] public IEnumerator DisabledOrDefeatedRunsDoNotAutoStartAndRestartWaitsForFirstInput()
    {
        yield return SceneManager.LoadSceneAsync("OpenFieldPrototype");Component owner=Owner();FastWaves(owner);
        Call(owner,"StartAssault");((Behaviour)owner).enabled=false;
        Assert.That(Call(owner,"TickAutoWave",1f),Is.False);Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(1));
        ((Behaviour)owner).enabled=true;
        Call(owner,"FinishRun",Enum.Parse(Runtime("OpenFieldPhase"),"Defeat"));
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);
        yield return new WaitForSeconds(.5f);Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(1));
        Call(owner,"RestartPrototype");yield return null;
        owner=Owner();Assert.That(Get(owner,"CurrentRound"),Is.EqualTo(1));
        Assert.That(Get(owner,"HasAutoWaveCountdown"),Is.False);Assert.That(Enemies(owner),Is.Empty);
    }
    [UnityTearDown] public IEnumerator Restore()
    {
        Time.timeScale=1f;yield return SceneManager.LoadSceneAsync("MainMenu");
        foreach(Object item in owned)Object.Destroy(item);owned.Clear();
        if(previousScore.HasValue)PlayerPrefs.SetInt(Key,previousScore.Value);else PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
