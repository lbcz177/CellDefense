using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-mode integration check: real player-loop animation events, pooling and pause.
[InitializeOnLoad]
public static class CombatAnimationSampleSmokeTest
{
    private const string ActiveKey="CellDefense.AnimationSampleSmoke.Active";
    private static CombatAnimationSamplePreview preview;
    private static readonly HashSet<int> observedSprites=new HashSet<int>();
    private static bool sawCharge,sawRelease,sawFlash,sawCoreAttack,sawReusedMove,pausePassed,finished;
    private static int pauseStage,captureIndex;
    private static float pauseRealStart,nextCapture,gameStart;
    private static Vector3 pausedPosition;
    private static float pausedAnimation;
    private static int pausedHits;
    private static RenderTexture renderTarget;
    private static readonly List<float> captureTimes=new List<float>();

    static CombatAnimationSampleSmokeTest()
    {
        EditorApplication.update+=Tick;
    }

    public static void Run()
    {
        CombatAnimationSampleInstaller.Install();
        Directory.CreateDirectory(CombatAnimationSampleInstaller.ReportRoot+"/runtime-frames");
        SessionState.SetBool(ActiveKey,true);
        EditorApplication.isPlaying=true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey,false) || !EditorApplication.isPlaying || finished) return;
        try
        {
            if (preview == null)
            {
                preview=UnityEngine.Object.FindObjectOfType<CombatAnimationSamplePreview>();
                if (preview == null || preview.Enemy.Definition == null) { preview=null;return; }
                Application.logMessageReceived+=OnLog;
                gameStart=Time.time;nextCapture=Time.time;
                renderTarget=new RenderTexture(640,480,24,RenderTextureFormat.ARGB32);
                renderTarget.Create();
            }
            EnemyController enemy=preview.Enemy;
            float elapsed=Time.time-gameStart;
            if (enemy.gameObject.activeSelf)
            {
                SpriteRenderer renderer=enemy.GetComponent<SpriteRenderer>();
                if (renderer.sprite != null) observedSprites.Add(renderer.sprite.GetInstanceID());
                MaterialPropertyBlock block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                if (block.GetFloat("_FlashAmount")>0.5f) sawFlash=true;
                AnimatorStateInfo state=enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Attack")) sawCoreAttack=true;
                if (enemy.SpawnVersion>1 && state.IsName("Move")) sawReusedMove=true;
            }
            AnimatorStateInfo towerState=preview.Tower.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            if (!sawRelease && towerState.IsName("Attack") && towerState.normalizedTime<0.22f)
            {
                Require(preview.Pool.ActiveCount==0,"A projectile appeared before the release pose.");
                sawCharge=true;
            }
            if (!sawRelease && preview.Pool.ActiveCount>0)
            {
                Require(sawCharge,"No visible anticipation preceded the projectile.");
                foreach (Projectile projectile in preview.Pool.GetComponentsInChildren<Projectile>())
                {
                    SpriteRenderer renderer=projectile.GetComponent<SpriteRenderer>();
                    Require(renderer.sprite.name=="单体颗粒","Sample still uses a placeholder projectile.");
                    Require(renderer.bounds.size.x<0.26f && renderer.bounds.size.y<0.26f,"Sample projectile is too large.");
                }
                sawRelease=true;
            }
            if (pauseStage==0 && elapsed>0.35f)
            {
                Time.timeScale=0f;pauseRealStart=Time.realtimeSinceStartup;pauseStage=1;
            }
            else if (pauseStage==1 && Time.realtimeSinceStartup-pauseRealStart>0.05f)
            {
                pausedPosition=enemy.transform.position;
                pausedAnimation=enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime;
                pausedHits=preview.ProjectileHits;pauseStage=2;
            }
            else if (pauseStage==2 && Time.realtimeSinceStartup-pauseRealStart>0.2f)
            {
                Require(Vector3.Distance(pausedPosition,enemy.transform.position)<0.0001f,"Enemy moved while paused.");
                Require(Mathf.Abs(pausedAnimation-enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime)<0.001f,"Animation advanced while paused.");
                Require(pausedHits==preview.ProjectileHits,"Damage advanced while paused.");
                Time.timeScale=1f;pauseStage=3;pausePassed=true;
            }
            if (Time.time>=nextCapture && Time.timeScale>0f)
            {
                Capture();nextCapture=Time.time+0.05f;
            }
            if (elapsed>3.6f)
            {
                Require(observedSprites.Count>=4,"Movement sprites did not advance.");
                Require(sawCharge && sawRelease,"Release event was not exercised.");
                Require(sawFlash && preview.ProjectileHits>=2,"Repeated hits did not show hit flashes.");
                Require(sawCoreAttack && preview.CoreHits>=2,"Core contact did not play and resolve.");
                Require(sawReusedMove && enemy.SpawnVersion>=3,"Reused enemy did not reset to movement.");
                Require(preview.CoreHits<enemy.SpawnVersion,"A spawn resolved more than once.");
                Require(pausePassed,"Pause check did not finish.");
                Finish(true,null);
            }
        }
        catch (Exception exception) { Finish(false,exception.ToString()); }
    }

    private static void Capture()
    {
        Camera camera=Camera.main;
        RenderTexture previous=RenderTexture.active;
        camera.targetTexture=renderTarget;camera.Render();RenderTexture.active=renderTarget;
        Texture2D image=new Texture2D(renderTarget.width,renderTarget.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,renderTarget.width,renderTarget.height),0,0);image.Apply();
        string file=CombatAnimationSampleInstaller.ReportRoot+"/runtime-frames/frame_"+captureIndex.ToString("D3")+".png";
        File.WriteAllBytes(file,image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);camera.targetTexture=null;RenderTexture.active=previous;
        captureTimes.Add(Time.time-gameStart);captureIndex++;
    }

    private static void OnLog(string message,string stack,LogType type)
    {
        if (type==LogType.Exception || type==LogType.Error) Finish(false,message+"\n"+stack);
    }

    private static void Finish(bool passed,string error)
    {
        if (finished) return;
        finished=true;SessionState.SetBool(ActiveKey,false);Application.logMessageReceived-=OnLog;Time.timeScale=1f;
        var report=new Report {passed=passed,error=error,anticipationBeforeRelease=sawCharge,releaseEvent=sawRelease,
            hitFlash=sawFlash,coreAttack=sawCoreAttack,pooledEnemyReset=sawReusedMove,pause=pausePassed,
            projectileHits=preview!=null ? preview.ProjectileHits : 0,coreHits=preview!=null ? preview.CoreHits : 0,
            captureCount=captureIndex,captureTimes=captureTimes.ToArray()};
        File.WriteAllText(CombatAnimationSampleInstaller.ReportRoot+"/runtime-validation.json",JsonUtility.ToJson(report,true));
        if (renderTarget!=null) renderTarget.Release();
        Debug.Log("Animation runtime validation: " + (passed ? "PASS" : "FAIL") + (error==null ? "" : " "+error));
        EditorApplication.Exit(passed ? 0 : 1);
    }

    private static void Require(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [Serializable] private class Report
    {
        public bool passed,anticipationBeforeRelease,releaseEvent,hitFlash,coreAttack,pooledEnemyReset,pause;
        public int projectileHits,coreHits,captureCount;
        public float[] captureTimes;
        public string error;
    }
}
