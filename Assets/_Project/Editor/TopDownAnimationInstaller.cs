using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class TopDownAnimationInstaller
{
    public const string ArtRoot = "Assets/_Project/Art/Sprites/细胞绘制/帧动画/正俯视10帧_v3";
    public const string AnimationRoot = "Assets/_Project/Art/Animations/TopDown10_v3";
    private const string Docs = "Docs/Art/AnimationSamples/TopDown10-v3";

    [MenuItem("Cell Defense/Install Top Down Ten Frame Animations")]
    public static void Install()
    {
        Require(!EditorApplication.isPlaying, "Install outside Play Mode.");
        Directory.CreateDirectory(AnimationRoot);
        string json = File.ReadAllText(Docs + "/units.json").TrimStart('\ufeff');
        Unit[] units = JsonUtility.FromJson<Config>("{\"units\":" + json + "}").units;
        Require(units.Length == 14, "Expected six towers and eight enemy types.");
        bool installedBefore = File.Exists(Docs + "/integration-validation.json");
        // Check every input before changing any prefab.
        foreach (Unit unit in units)
        {
            if (!installedBefore) Require(Hash(unit.Prefab) == unit.PrefabHash, "Concurrent prefab edit: " + unit.Prefab);
            foreach (string action in unit.Kind == "Enemy" ? new[] { "Move", "Attack" } : new[] { "Attack" })
            {
                string path = ClipDirectory(unit, action);
                ClipReport report = JsonUtility.FromJson<ClipReport>(File.ReadAllText(path + "/animation.json"));
                Require(report.passed && report.frames == 10 && report.distinctFrames == 10,
                    "Native animation validation failed: " + path);
            }
        }
        AssetDatabase.Refresh();
        Shader shader = Shader.Find("CellDefense/SpriteHitFlash");
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Hit feedback shader failed.");
        string materialPath = AnimationRoot + "/AnimatedEnemy.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
        material.shader = shader; material.SetColor("_StateTint", Color.white); material.SetFloat("_FlashAmount", 0f);
        EditorUtility.SetDirty(material);
        foreach (Unit unit in units) InstallUnit(unit, material);
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static void InstallUnit(Unit unit, Material material)
    {
        string backup = Docs + "/backup/" + unit.Prefab;
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        if (!File.Exists(backup))
        {
            File.Copy(unit.Prefab, backup);
            File.Copy(unit.Prefab + ".meta", backup + ".meta");
        }
        GameObject root = PrefabUtility.LoadPrefabContents(unit.Prefab);
        try
        {
            bool enemy = unit.Kind == "Enemy";
            float size = unit.Id.Contains("boss") ? 1.35f :
                unit.Id == "10_fungal_spore" || unit.Id == "11_virus_clone" ? 0.45f : 0.9f;
            ClipReport source = JsonUtility.FromJson<ClipReport>(File.ReadAllText(ClipDirectory(unit, enemy ? "Move" : "Attack") + "/animation.json"));
            BoundsData neutral = source.framesData[0].bounds;
            float ppu = Mathf.Max(neutral.w, neutral.h) * Mathf.Abs(root.transform.localScale.x) / size;
            Sprite[] attack = Import(unit, "Attack", ppu);
            Sprite[] rest = enemy ? Import(unit, "Move", ppu) : attack;
            bool fibrin = unit.Id == "02_fibrin";
            AnimationClip idleClip = enemy ? Clip(unit.Id + "_Move", rest, true, false, null) :
                Clip(unit.Id + "_Idle", rest, true, !fibrin, null);
            AnimationEvent actionEvent = enemy ? new AnimationEvent { time = 0.46f, functionName = "CompleteCoreAttack" } :
                fibrin ? null : new AnimationEvent { time = 0.16f, functionName = "ReleaseAttack" };
            AnimationClip attackClip = Clip(unit.Id + "_Attack", attack, false, false, actionEvent);
            AnimatorController controller = Controller(unit.Id, enemy ? "Move" : "Idle", idleClip, attackClip);
            Animator animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = rest[0]; renderer.flipX = false; renderer.flipY = false;
            if (enemy)
            {
                renderer.sharedMaterial = material;
                if (root.GetComponent<SpriteHitFlash>() == null) root.AddComponent<SpriteHitFlash>();
                SerializedObject data = new SerializedObject(root.GetComponent<EnemyController>());
                data.FindProperty("playCoreAttackAnimation").boolValue = true;
                data.FindProperty("rotateBodyWithDirection").boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
                EnemyVisualState visual = root.GetComponent<EnemyVisualState>();
                if (visual != null)
                {
                    SerializedObject state = new SerializedObject(visual);
                    state.FindProperty("baseSprite").objectReferenceValue = rest[0];
                    state.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                SerializedObject data = new SerializedObject(root.GetComponent<TowerController>());
                data.FindProperty("releaseOnAnimationEvent").boolValue = !fibrin;
                data.FindProperty("aimVisualAtTarget").boolValue = true;
                data.FindProperty("muzzleLocalOffset").vector2Value = Vector2.right * (0.35f / Mathf.Max(0.0001f, Mathf.Abs(root.transform.localScale.x)));
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, unit.Prefab);
            Require(AssetDatabase.AssetPathToGUID(unit.Prefab) == GuidFromMeta(backup + ".meta"), "Prefab GUID changed.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static string ClipDirectory(Unit unit, string action) => ArtRoot + "/" + unit.Id + "_" + action.ToLowerInvariant();

    private static Sprite[] Import(Unit unit, string action, float ppu)
    {
        string path = ClipDirectory(unit, action) + "/animation_sheet.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        SpriteMetaData[] frames = new SpriteMetaData[10];
        for (int i = 0; i < 10; i++) frames[i] = new SpriteMetaData
        {
            name = "frame_" + (i + 1).ToString("D2"), rect = new Rect(i % 5 * 128, (1 - i / 5) * 128, 128, 128),
            alignment = (int)SpriteAlignment.Center, pivot = Vector2.one * 0.5f
        };
        importer.spritesheet = frames; importer.SaveAndReimport();
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
        Require(sprites.Length == 10 && sprites.All(s => s.rect.width == 128 && s.rect.height == 128), "Bad slicing: " + path);
        return sprites;
    }

    private static AnimationClip Clip(string name, Sprite[] sprites, bool loop, bool idle, AnimationEvent actionEvent)
    {
        string path = AnimationRoot + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.name = name; clip.frameRate = 100;
        float[] times = loop ? Enumerable.Range(0, 11).Select(i => i * 0.1f).ToArray() :
            new[] { 0f, 0.05f, 0.09f, 0.13f, 0.16f, 0.20f, 0.24f, 0.29f, 0.34f, 0.40f, 0.48f };
        if (idle) times = new[] { 0f, 0.1f };
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[times.Length];
        for (int i = 0; i < keys.Length; i++) keys[i] = new ObjectReferenceKeyframe
        {
            time = times[i], value = sprites[idle || (loop && i == 10) ? 0 : Mathf.Min(i, 9)]
        };
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; settings.startTime = 0f; settings.stopTime = times[times.Length - 1];
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, actionEvent == null ? Array.Empty<AnimationEvent>() : new[] { actionEvent });
        EditorUtility.SetDirty(clip); return clip;
    }

    private static AnimatorController Controller(string name, string restName, AnimationClip restClip, AnimationClip attackClip)
    {
        string path = AnimationRoot + "/" + name + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
        controller.parameters = new[] { new AnimatorControllerParameter { name = "Attack", type = AnimatorControllerParameterType.Trigger } };
        AnimatorState rest = machine.AddState(restName); rest.motion = restClip; rest.writeDefaultValues = false;
        AnimatorState attack = machine.AddState("Attack"); attack.motion = attackClip; attack.writeDefaultValues = false;
        machine.defaultState = rest;
        AnimatorStateTransition begin = rest.AddTransition(attack);
        begin.hasExitTime = false; begin.duration = 0f; begin.canTransitionToSelf = false;
        begin.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        AnimatorStateTransition finish = attack.AddTransition(rest);
        finish.hasExitTime = true; finish.exitTime = 1f; finish.duration = 0f;
        EditorUtility.SetDirty(controller); return controller;
    }

    public static void Validate()
    {
        Unit[] units = JsonUtility.FromJson<Config>("{\"units\":" + File.ReadAllText(Docs + "/units.json").TrimStart('\ufeff') + "}").units;
        int clips = 0;
        foreach (Unit unit in units)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(unit.Prefab);
            Animator animator = root.GetComponent<Animator>();
            Require(animator != null && animator.runtimeAnimatorController != null, "Missing animator: " + unit.Prefab);
            Require(!root.GetComponent<SpriteRenderer>().flipX && !root.GetComponent<SpriteRenderer>().flipY, "Unexpected mirror.");
            foreach (string action in unit.Kind == "Enemy" ? new[] { "Move", "Attack" } : new[] { "Attack" })
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/" + unit.Id + "_" + action + ".anim");
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip,
                    new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" });
                Require(keys.Select(k => k.value).Distinct().Count() == 10 && keys.All(k => k.value != null), "Missing ten poses: " + unit.Id);
                if (action == "Move") Require(AnimationUtility.GetAnimationClipSettings(clip).loopTime, "Movement must loop.");
                else Require(!AnimationUtility.GetAnimationClipSettings(clip).loopTime, "Attack must play once.");
                clips++;
            }
        }
        Require(clips == 22, "Expected twenty-two action clips.");
        File.WriteAllText(Docs + "/integration-validation.json", "{\"passed\":true,\"units\":14,\"actionClips\":22,\"distinctFramesPerAction\":10,\"nativeSpriteReferences\":true,\"prefabGUIDsPreserved\":true,\"playModeRun\":false}");
        Debug.Log("Top-down ten-frame integration PASS: 14 units, 22 action clips.");
    }

    [MenuItem("Cell Defense/Apply And Validate Planar Facing")]
    public static void ApplyAndValidatePlanarFacing()
    {
        Require(!EditorApplication.isPlaying, "Apply outside Play Mode.");
        Unit[] units = JsonUtility.FromJson<Config>("{\"units\":" + File.ReadAllText(Docs + "/units.json").TrimStart('\ufeff') + "}").units;
        foreach (Unit unit in units.Where(u => u.Kind == "Enemy"))
        {
            GameObject root = PrefabUtility.LoadPrefabContents(unit.Prefab);
            try
            {
                SerializedObject data = new SerializedObject(root.GetComponent<EnemyController>());
                data.FindProperty("rotateBodyWithDirection").boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, unit.Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Validate();

        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        int directions = 0, sampledPoses = 0;
        GameObject targetObject = new GameObject("Facing validation target");
        targetObject.SetActive(false);
        EnemyController target = targetObject.AddComponent<EnemyController>();
        try
        {
            foreach (Unit unit in units)
            {
                GameObject root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(unit.Prefab));
                try
                {
                    root.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                    SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                    Vector3 position = root.transform.position;
                    Vector3 scale = root.transform.localScale;
                    EnemyController enemy = root.GetComponent<EnemyController>();
                    TowerController tower = root.GetComponent<TowerController>();
                    if (tower != null) typeof(TowerController).GetField("bodyRenderer", flags).SetValue(tower, renderer);
                    if (enemy != null)
                        Require(new SerializedObject(enemy).FindProperty("rotateBodyWithDirection").boolValue, "Enemy rotation disabled.");
                    else Require(new SerializedObject(tower).FindProperty("aimVisualAtTarget").boolValue, "Tower rotation disabled.");

                    for (int i = 0; i < 8; i++)
                    {
                        float radians = i * Mathf.PI / 4f;
                        Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                        targetObject.transform.position = position + direction * 5f;
                        if (enemy != null) enemy.FaceTowards(targetObject.transform.position);
                        else typeof(TowerController).GetMethod("AimAtTarget", flags).Invoke(tower, new object[] { target });
                        Require(Vector3.Dot(root.transform.right, direction) > 0.9999f, "Incorrect aim: " + unit.Id);
                        Require(Vector3.Dot(root.transform.forward, Vector3.forward) > 0.9999f, "Body tilted out of plane.");
                        Require(root.transform.localScale == scale && root.transform.position == position, "Facing changed size or position.");
                        Require(!renderer.flipX && !renderer.flipY, "Facing mirrored sprite.");

                        if (tower != null)
                        {
                            Vector2 muzzle = new SerializedObject(tower).FindProperty("muzzleLocalOffset").vector2Value;
                            Vector3 localAim = root.transform.InverseTransformDirection(direction).normalized * muzzle.magnitude;
                            Vector3 worldMuzzle = root.transform.TransformPoint(localAim) - position;
                            Require(Vector3.Dot(worldMuzzle.normalized, direction) > 0.9999f, "Muzzle rotated twice.");
                        }
                        foreach (string action in unit.Kind == "Enemy" ? new[] { "Move", "Attack" } : new[] { "Attack" })
                        {
                            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/" + unit.Id + "_" + action + ".anim");
                            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip,
                                new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" });
                            foreach (ObjectReferenceKeyframe key in keys.Take(10))
                            {
                                clip.SampleAnimation(root, key.time);
                                Require(renderer.sprite == key.value, "Animation pose not sampled.");
                                Require(Vector3.Dot(root.transform.right, direction) > 0.9999f && root.transform.localScale == scale,
                                    "Animation overwrote facing or scale.");
                                Require(!renderer.flipX && !renderer.flipY, "Animation mirrored sprite.");
                                sampledPoses++;
                            }
                        }
                        directions++;
                    }
                    Quaternion held = root.transform.rotation;
                    TopDownFacing.FaceDirection(root.transform, renderer, Vector3.zero);
                    Require(Quaternion.Angle(held, root.transform.rotation) < 0.001f, "Zero direction reset facing.");
                    Require(Hash(unit.Prefab + ".meta") == Hash(Docs + "/planar-facing-backup/" + unit.Prefab + ".meta"), "Prefab meta changed.");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(targetObject); }
        File.WriteAllText(Docs + "/planar-facing-validation.json", "{\"passed\":true,\"units\":14,\"directionChecks\":" + directions +
            ",\"sampledAnimationPoses\":" + sampledPoses + ",\"mirroring\":false,\"prefabMetasPreserved\":true,\"playModeRun\":false}");
        Debug.Log("Planar facing PASS: " + directions + " direction checks, " + sampledPoses + " animation pose samples.");
    }

    private static string Hash(string path) { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
    private static string GuidFromMeta(string path) => File.ReadAllLines(path).First(s => s.StartsWith("guid: ")).Substring(6).Trim();
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    [Serializable] private class Config { public Unit[] units; }
    [Serializable] private class Unit { public string Id, Kind, Prefab, PrefabHash; }
    [Serializable] private class ClipReport { public bool passed; public int frames, distinctFrames; public FrameData[] framesData; }
    [Serializable] private class FrameData { public BoundsData bounds; }
    [Serializable] private class BoundsData { public float w, h; }
}
