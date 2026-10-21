using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public static class OpenFieldPrototypeSceneCreator
{
    private const string ScenePath = "Assets/_Project/Scenes/OpenFieldPrototype.unity";

    [MenuItem("Cell Defense/Create Open Field Prototype Scene")]
    public static void CreateScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorUtility.DisplayDialog("Open Field Prototype",
                "The prototype scene already exists. Open it from Assets/_Project/Scenes.", "OK");
            return;
        }

        WaveSequenceDefinition waves = AssetDatabase.LoadAssetAtPath<WaveSequenceDefinition>(
            "Assets/_Project/Data/Waves/EnemyRoster01_WaveSequence.asset");
        if (waves == null) waves = AssetDatabase.LoadAssetAtPath<WaveSequenceDefinition>(
            "Assets/_Project/Data/Waves/WaveSequenceDefinition.asset");
        TowerDefinition guard = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/TowerDefinition.asset");
        TowerDefinition antibody = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/InterceptorTowerDefinition.asset");
        TowerDefinition phagocyte = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/TowerDefinition 1.asset");
        TowerDefinition fibrin = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/FibrinTowerDefinition.asset");
        GameObject projectileObject = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Projectiles/Projectile.prefab");
        Projectile projectile = projectileObject != null
            ? projectileObject.GetComponent<Projectile>()
            : null;
        Sprite mapArtwork = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Art/Sprites/细胞绘制/前五关地图_2026-10-15_v1/01_守住伤口.png");
        Sprite waveButtonArtwork = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Art/Sprites/细胞绘制/战斗UI/下一波按钮_v1/下一波_圆形细胞按钮.png");
        TMP_FontAsset buildMenuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Project/Fonts/FZFS_GBK SDF.asset");
        GameObject victoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Menu/Victory.prefab");
        GameObject defeatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Menu/Defeat.prefab");
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Menu/OpenFieldBattleHud.prefab");
        if (waves == null || waves.WaveCount != 3 || guard == null || antibody == null ||
            phagocyte == null || fibrin == null || projectile == null || victoryPrefab == null || defeatPrefab == null ||
            buildMenuFont == null || mapArtwork == null || waveButtonArtwork == null || hudPrefab == null)
        {
            EditorUtility.DisplayDialog("Open Field Prototype",
                "A wave sequence, tower, projectile, map, result UI prefab, or build menu font is missing.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.08f, 0.11f, 1f);
        cameraObject.AddComponent<AudioListener>();

        GameObject root = new GameObject("Open Field Prototype");
        OpenFieldPrototypeController controller = root.AddComponent<OpenFieldPrototypeController>();
        GameObject poolObject = new GameObject("Projectile Pool");
        poolObject.transform.SetParent(root.transform);
        ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();

        GameObject victoryPanel = (GameObject)PrefabUtility.InstantiatePrefab(victoryPrefab, scene);
        GameObject defeatPanel = (GameObject)PrefabUtility.InstantiatePrefab(defeatPrefab, scene);
        GameObject battleHud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        SerializedObject poolSerialized = new SerializedObject(pool);
        poolSerialized.FindProperty("prefab").objectReferenceValue = projectile;
        poolSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSerialized = new SerializedObject(controller);
        ConfigureFirstMap(controllerSerialized);
        controllerSerialized.FindProperty("waveSequence").objectReferenceValue = waves;
        controllerSerialized.FindProperty("projectilePool").objectReferenceValue = pool;
        controllerSerialized.FindProperty("gameplayCamera").objectReferenceValue = camera;
        controllerSerialized.FindProperty("mapArtwork").objectReferenceValue = mapArtwork;
        controllerSerialized.FindProperty("waveButtonArtwork").objectReferenceValue = waveButtonArtwork;
        controllerSerialized.FindProperty("buildMenuFont").objectReferenceValue = buildMenuFont;
        controllerSerialized.FindProperty("sellRefundPercent").intValue = TowerEconomyRules.DefaultSellRefundPercent;
        controllerSerialized.FindProperty("battleHud").objectReferenceValue = battleHud.GetComponent<OpenFieldBattleHud>();
        controllerSerialized.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
        controllerSerialized.FindProperty("defeatPanel").objectReferenceValue = defeatPanel;
        controllerSerialized.FindProperty("nextSceneName").stringValue = "OpenFieldLevel02";
        SerializedProperty towers = controllerSerialized.FindProperty("towerDefinitions");
        towers.arraySize = 4;
        towers.GetArrayElementAtIndex(0).objectReferenceValue = guard;
        towers.GetArrayElementAtIndex(1).objectReferenceValue = antibody;
        towers.GetArrayElementAtIndex(2).objectReferenceValue = phagocyte;
        towers.GetArrayElementAtIndex(3).objectReferenceValue = fibrin;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Selection.activeGameObject = root;
        Debug.Log($"Created {ScenePath}. Press Play to try the three-round prototype.");
    }

    private static void ConfigureFirstMap(SerializedObject controller)
    {
        const int width = 15;
        const int height = 11;
        controller.FindProperty("width").intValue = width;
        controller.FindProperty("height").intValue = height;
        controller.FindProperty("coreCell").vector2IntValue = new Vector2Int(10, 5);
        SerializedProperty spawns = controller.FindProperty("spawnCells");
        spawns.arraySize = 1;
        spawns.GetArrayElementAtIndex(0).vector2IntValue = new Vector2Int(0, 5);

        // The membrane rim is solid, except for the wound on the left edge.
        List<Vector2Int> blocked = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x != 0 && x != width - 1 && y != 0 && y != height - 1) continue;
                if (x == 0 && y >= 4 && y <= 6) continue;
                blocked.Add(new Vector2Int(x, y));
            }
        }
        blocked.AddRange(new[]
        {
            new Vector2Int(1, 1), new Vector2Int(1, 9),
            new Vector2Int(13, 1), new Vector2Int(13, 9),
            new Vector2Int(5, 5), new Vector2Int(6, 4),
            new Vector2Int(6, 5), new Vector2Int(6, 6)
        });
        SerializedProperty cells = controller.FindProperty("blockedCells");
        cells.arraySize = blocked.Count;
        for (int i = 0; i < blocked.Count; i++)
            cells.GetArrayElementAtIndex(i).vector2IntValue = blocked[i];
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene entry in scenes)
        {
            if (entry.path == ScenePath)
            {
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
