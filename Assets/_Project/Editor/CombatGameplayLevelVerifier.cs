using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CombatGameplayLevelVerifier
{
    private const string Key="CellDefense.GameplayFeedbackVerification";
    private const string Root="Docs/Art/AnimationIntegration/2026-10-12/gameplay";
    private static OpenFieldPrototypeController level;
    private static CombatHitFeedback feedback;
    private static readonly HashSet<int> movementSprites=new HashSet<int>();
    private static float started,nextCapture;
    private static bool sawFlash,sawSparks,sawAttack,finished;
    private static int captures;
    private static RenderTexture target;
    private static readonly List<float> captureTimes=new List<float>();

    static CombatGameplayLevelVerifier() { EditorApplication.update+=Tick; }

    public static void Run()
    {
        CombatAnimationSampleInstaller.Install();
        Directory.CreateDirectory(Root+"/runtime-frames");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/OpenFieldLevel02.unity",OpenSceneMode.Single);
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || finished) return;
        try
        {
            if (level==null)
            {
                level=UnityEngine.Object.FindObjectOfType<OpenFieldPrototypeController>();
                feedback=UnityEngine.Object.FindObjectOfType<CombatHitFeedback>();
                if (level==null || feedback==null || level.CurrentATP<=0) { level=null;return; }
                Require(SceneManager.GetActiveScene().path=="Assets/_Project/Scenes/OpenFieldLevel02.unity","Wrong gameplay scene.");
                OpenFieldGridModel grid=(OpenFieldGridModel)typeof(OpenFieldPrototypeController)
                    .GetField("grid",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(level);
                var route=new List<Vector2Int>();Require(grid.TryGetPath(grid.GetSpawnCell(0),route),"No spawn route.");
                Vector2Int[] offsets={Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down};
                int built=0;level.SelectTower(0);
                foreach (int index in new[] {2,4})
                {
                    if (index>=route.Count) continue;
                    foreach (Vector2Int offset in offsets)
                    {
                        Vector2Int cell=route[index]+offset;
                        if (route.Contains(cell) || grid.CheckPlacement(cell)!=OpenFieldPlacementResult.Valid) continue;
                        if (level.TryBuildAt(cell)) { built++;break; }
                    }
                }
                Require(built==2,"Could not build two towers through the normal placement API.");
                Require(level.StartAssault(),"Could not start the existing level's assault.");
                started=Time.time;nextCapture=Time.time;
                target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);target.Create();
                Application.logMessageReceived+=OnLog;
            }
            foreach (EnemyController enemy in UnityEngine.Object.FindObjectsOfType<EnemyController>())
            {
                SpriteRenderer renderer=enemy.GetComponent<SpriteRenderer>();
                if (renderer.sprite!=null) movementSprites.Add(renderer.sprite.GetInstanceID());
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                if (block.GetFloat("_FlashAmount")>0.5f) sawFlash=true;
                if (enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack")) sawAttack=true;
            }
            foreach (SpriteRenderer renderer in feedback.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled) { sawSparks=true;break; }
            if (Time.time>=nextCapture && captures<100)
            {
                Capture();nextCapture=Time.time+0.1f;
            }
            if (Time.time-started>9f && feedback.CoreImpactCount>0)
            {
                Require(feedback.ShotCount>0 && feedback.HitCount>=3,"Normal combat did not produce hits.");
                Require(sawFlash && sawSparks,"Hit flash or impact particles did not appear.");
                Require(sawAttack && movementSprites.Count>=4,"Movement/core attack animations did not run.");
                Require(level.CurrentCoreLife<20,"Core attack did not resolve through the level life service.");
                Finish(true,null);
            }
            if (Time.time-started>25f) Finish(false,"Timed out while checking existing-level combat.");
        }
        catch (Exception exception) { Finish(false,exception.ToString()); }
    }

    private static void Capture()
    {
        Camera camera=Camera.main;RenderTexture previous=RenderTexture.active;
        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        Texture2D image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        File.WriteAllBytes(Root+"/runtime-frames/frame_"+captures.ToString("D3")+".png",image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);camera.targetTexture=null;RenderTexture.active=previous;
        captureTimes.Add(Time.time-started);captures++;
    }

    private static void OnLog(string message,string stack,LogType type)
    {
        if (type==LogType.Error || type==LogType.Exception) Finish(false,message+"\n"+stack);
    }

    private static void Finish(bool passed,string error)
    {
        if (finished) return;
        finished=true;SessionState.SetBool(Key,false);Application.logMessageReceived-=OnLog;
        var report=new Report {passed=passed,error=error,scene=SceneManager.GetActiveScene().path,
            shots=feedback!=null ? feedback.ShotCount : 0,hits=feedback!=null ? feedback.HitCount : 0,
            coreImpacts=feedback!=null ? feedback.CoreImpactCount : 0,coreLife=level!=null ? level.CurrentCoreLife : 0,
            hitFlash=sawFlash,hitParticles=sawSparks,coreAttack=sawAttack,movementSpriteCount=movementSprites.Count,
            captureCount=captures,captureTimes=captureTimes.ToArray()};
        File.WriteAllText(Root+"/level-validation.json",JsonUtility.ToJson(report,true));
        if (target!=null) target.Release();
        Debug.Log("Existing gameplay combat verification: "+(passed ? "PASS" : "FAIL")+(error==null ? "" : " "+error));
        EditorApplication.Exit(passed ? 0 : 1);
    }

    private static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
    [Serializable] private class Report
    {
        public bool passed,hitFlash,hitParticles,coreAttack;
        public string scene,error;
        public int shots,hits,coreImpacts,coreLife,movementSpriteCount,captureCount;
        public float[] captureTimes;
    }
}
