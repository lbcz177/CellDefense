using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local file commands let the already-open editor apply and inspect this task's changes.
[InitializeOnLoad]
public static class CombatGameplayEditorBridge
{
    private const string Root="Docs/Art/AnimationIntegration/2026-10-12/gameplay";
    private static double nextCheck;

    static CombatGameplayEditorBridge() { EditorApplication.update+=Check; }

    private static void Check()
    {
        if (EditorApplication.timeSinceStartup<nextCheck || EditorApplication.isCompiling) return;
        nextCheck=EditorApplication.timeSinceStartup+0.5;
        string requestPath=Root+"/request.json";
        if (!File.Exists(requestPath)) return;
        Request request=JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
        File.Delete(requestPath);
        try
        {
            if (request.action=="inspect") WriteStatus(request.id,null);
            else WriteStatus(request.id,"Unsupported command: "+request.action);
        }
        catch (Exception exception) { WriteStatus(request.id,exception.ToString()); }
    }

    private static void WriteStatus(string id,string error)
    {
        Directory.CreateDirectory(Root);
        Scene scene=SceneManager.GetActiveScene();
        var result=new Status {id=id,error=error,activeScene=scene.path,isPlaying=EditorApplication.isPlaying,
            isPaused=EditorApplication.isPaused,isDirty=scene.isDirty,timeScale=Time.timeScale};
        File.WriteAllText(Root+"/status.json",JsonUtility.ToJson(result,true));
    }

    [Serializable] private class Request { public string id,action; }
    [Serializable] private class Status
    {
        public string id,error,activeScene;
        public bool isPlaying,isPaused,isDirty;
        public float timeScale;
    }
}
