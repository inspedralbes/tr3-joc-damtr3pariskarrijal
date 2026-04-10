using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

public class ProjectSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Project")]
    public static void RunSetup()
    {
        CreateScenes();
        SetupBuildSettings();
        SetupCombatScene();
        SetupTags();
        Debug.Log("Project Setup Complete!");
    }

    private static void CreateScenes()
    {
        string[] scenes = { "CombatScene", "LoginScene", "MenuScene", "WaitingScene", "TrainingScene" };
        foreach (var sceneName in scenes)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            if (!File.Exists(path))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }
        }

        // Delete SampleScene
        string samplePath = "Assets/Scenes/SampleScene.unity";
        if (File.Exists(samplePath))
        {
            AssetDatabase.DeleteAsset(samplePath);
        }
        AssetDatabase.Refresh();
    }

    private static void SetupBuildSettings()
    {
        string[] scenesToOrder = {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/MenuScene.unity",
            "Assets/Scenes/WaitingScene.unity",
            "Assets/Scenes/CombatScene.unity",
            "Assets/Scenes/TrainingScene.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (var path in scenesToOrder)
        {
            if (File.Exists(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    private static void SetupCombatScene()
    {
        string path = "Assets/Scenes/CombatScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        // Cleanup existing objects if re-running
        string[] toClear = { "Terrain", "Player1Tank", "Player2Tank", "CombatManager", "CombatUI" };
        foreach (var name in toClear)
        {
            var go = GameObject.Find(name);
            if (go != null) GameObject.DestroyImmediate(go);
        }

        // 8A - Camera Setup
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null) camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        var cam = camGo.GetComponent<Camera>();
        camGo.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.15f);
        cam.orthographic = true;
        cam.orthographicSize = 5;

        // 8B - Terrain
        var terrainGo = new GameObject("Terrain");
        terrainGo.transform.position = new Vector3(0, -3, 0);
        var tg = terrainGo.AddComponent<TerrainGenerator>();
        var meshRenderer = terrainGo.GetComponent<MeshRenderer>();
        
        // Create Material
        string matPath = "Assets/Resources/TerrainMaterial.mat";
        Material terrainMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (terrainMat == null)
        {
            terrainMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            terrainMat.color = new Color(0.2f, 0.5f, 0.2f); // Greenish
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(terrainMat, matPath);
        }
        meshRenderer.material = terrainMat;

        // 8C - Player 1 Tank
        var p1Tank = CreateTank("Player1Tank", new Vector3(-5, 0, 0));
        var p1Controller = p1Tank.AddComponent<TankController>();
        p1Controller.barrel = p1Tank.transform.Find("Barrel");
        p1Controller.terrain = tg;
        p1Controller.isLocalPlayer = true;

        // 8D - Player 2 Tank
        var p2Tank = CreateTank("Player2Tank", new Vector3(5, 0, 0));
        var p2Controller = p2Tank.AddComponent<TankController>();
        p2Controller.barrel = p2Tank.transform.Find("Barrel");
        p2Controller.terrain = tg;

        // 8E - Projectile Prefab
        var projGo = new GameObject("ProjectileTemp");
        projGo.AddComponent<SpriteRenderer>();
        var rbProj = projGo.AddComponent<Rigidbody2D>();
        rbProj.gravityScale = 1;
        projGo.AddComponent<CircleCollider2D>().radius = 0.15f;
        projGo.AddComponent<ProjectileController>();
        
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        GameObject projPrefab = PrefabUtility.SaveAsPrefabAsset(projGo, "Assets/Prefabs/Projectile.prefab");
        GameObject.DestroyImmediate(projGo);

        // 8F - Explosion Prefab
        var expGo = new GameObject("ExplosionTemp");
        expGo.AddComponent<SpriteRenderer>();
        expGo.AddComponent<Animator>();
        GameObject expPrefab = PrefabUtility.SaveAsPrefabAsset(expGo, "Assets/Prefabs/Explosion.prefab");
        GameObject.DestroyImmediate(expGo);

        // 8G & 8H - CombatManager & UI
        var mgrGo = new GameObject("CombatManager");
        var uiDoc = mgrGo.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/CombatScreen.uxml");
        
        var network = mgrGo.AddComponent<CombatNetworkManager>();
        var uiManager = mgrGo.AddComponent<CombatUIManager>();

        // Wire references
        uiManager.terrain = tg;
        uiManager.localTank = p1Controller;
        uiManager.enemyTank = p2Controller;
        uiManager.projectilePrefab = projPrefab;
        uiManager.explosionPrefab = expPrefab;
        uiManager.mainCamera = cam;
        uiManager.network = network;

        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject CreateTank(string name, Vector3 pos)
    {
        var tank = new GameObject(name);
        tank.transform.position = pos;
        tank.tag = "Tank";

        var body = new GameObject("Body");
        body.transform.SetParent(tank.transform);
        body.AddComponent<SpriteRenderer>();

        var barrel = new GameObject("Barrel");
        barrel.transform.SetParent(tank.transform);
        barrel.transform.localPosition = new Vector3(0.3f, 0.2f, 0);
        barrel.AddComponent<SpriteRenderer>();

        var rb = tank.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        tank.AddComponent<BoxCollider2D>();

        return tank;
    }

    private static void SetupTags()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals("Tank"))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = "Tank";
            tagManager.ApplyModifiedProperties();
        }
    }
}
