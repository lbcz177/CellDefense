using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            "Assets/_Project/Data/Waves/WaveSequenceDefinition.asset");
        TowerDefinition guard = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/TowerDefinition.asset");
        TowerDefinition antibody = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/InterceptorTowerDefinition.asset");
        TowerDefinition phagocyte = AssetDatabase.LoadAssetAtPath<TowerDefinition>(
            "Assets/_Project/Data/Towers/TowerDefinition 1.asset");
        GameObject projectileObject = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Projectiles/Projectile.prefab");
        Projectile projectile = projectileObject != null
            ? projectileObject.GetComponent<Projectile>()
            : null;
        if (waves == null || waves.WaveCount != 3 || guard == null || antibody == null ||
            phagocyte == null || projectile == null)
        {
            EditorUtility.DisplayDialog("Open Field Prototype",
                "A three-wave sequence, one of the tower definitions, or the projectile prefab is missing.", "OK");
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
        camera.orthographicSize = 9f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.08f, 0.11f, 1f);
        cameraObject.AddComponent<AudioListener>();

        GameObject root = new GameObject("Open Field Prototype");
        OpenFieldPrototypeController controller = root.AddComponent<OpenFieldPrototypeController>();
        GameObject poolObject = new GameObject("Projectile Pool");
        poolObject.transform.SetParent(root.transform);
        ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();

        SerializedObject poolSerialized = new SerializedObject(pool);
        poolSerialized.FindProperty("prefab").objectReferenceValue = projectile;
        poolSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSerialized = new SerializedObject(controller);
        controllerSerialized.FindProperty("waveSequence").objectReferenceValue = waves;
        controllerSerialized.FindProperty("projectilePool").objectReferenceValue = pool;
        controllerSerialized.FindProperty("gameplayCamera").objectReferenceValue = camera;
        SerializedProperty towers = controllerSerialized.FindProperty("towerDefinitions");
        towers.arraySize = 3;
        towers.GetArrayElementAtIndex(0).objectReferenceValue = guard;
        towers.GetArrayElementAtIndex(1).objectReferenceValue = antibody;
        towers.GetArrayElementAtIndex(2).objectReferenceValue = phagocyte;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Selection.activeGameObject = root;
        Debug.Log($"Created {ScenePath}. Press Play to try the three-round prototype.");
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
