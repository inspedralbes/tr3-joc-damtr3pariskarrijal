// ProjectSetupTool — Eina d'editor per configurar totes les escenes del projecte Tank Stars
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
        if (Application.isPlaying)
        {
            Debug.LogError("Atura el mode Play abans d'executar el Setup!");
            return;
        }

        // Configurar Input System a "Both" (legacy + new)
        SerializedObject settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var prop = settings.FindProperty("activeInputHandler");
        if (prop != null)
        {
            prop.intValue = 2;
            settings.ApplyModifiedProperties();
        }

        SetupTags();
        CreateTerrainMaterial();
        CreateScenes();
        SetupBuildSettings();
        SetupLoginScene();
        SetupMenuScene();
        SetupWaitingScene();
        SetupCombatScene();
        SetupVsAIScene();
        SetupTrainingScene();
        DeepProjectClean();
        Debug.Log("Configuració del projecte completa! Obre LoginScene i prem Play.");
    }

    private static void DeepProjectClean()
    {
        string[] obsolete = {
            "Assets/Editor/VsAISetupTool.cs",
            "Assets/Editor/AgentSetupTool.cs",
            "Assets/Editor/GameObjectBuilder.cs",
            "Assets/Editor/PropertyDebugger.cs"
        };

        foreach (var path in obsolete)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Neteja: Eliminat script obsolet -> {path}");
            }
        }
        AssetDatabase.Refresh();
    }

    private static void CreateScenes()
    {
        string[] scenes = { "LoginScene", "MenuScene", "WaitingScene", "CombatScene", "VsAIScene", "TrainingScene" };
        foreach (var sceneName in scenes)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            if (!File.Exists(path))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }
        }

        // Eliminar SampleScene si existeix
        string samplePath = "Assets/Scenes/SampleScene.unity";
        if (File.Exists(samplePath))
            AssetDatabase.DeleteAsset(samplePath);

        AssetDatabase.Refresh();
    }

    private static void SetupBuildSettings()
    {
        string[] scenesToOrder = {
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/MenuScene.unity",
            "Assets/Scenes/WaitingScene.unity",
            "Assets/Scenes/CombatScene.unity",
            "Assets/Scenes/VsAIScene.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (var path in scenesToOrder)
        {
            if (File.Exists(path))
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
    }

    private static void CreateTerrainMaterial()
    {
        string matPath = "Assets/Resources/TerrainMaterial.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null) return;

        // Crear directori si no existeix
        if (!Directory.Exists("Assets/Resources"))
            Directory.CreateDirectory("Assets/Resources");

        // Utilitzar URP/Unlit per respectar vertex colors
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.color = Color.white;
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.Refresh();
        Debug.Log("TerrainMaterial creat a " + matPath);
    }

    private static Material LoadTerrainMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/TerrainMaterial.mat");
        if (mat != null) return mat;

        // Fallback: crear un material temporal amb URP/Unlit
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return new Material(shader);
    }

    // ─── LoginScene ──────────────────────────────────────────────────────

    private static void SetupLoginScene()
    {
        string path = "Assets/Scenes/LoginScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        foreach (var rootGo in scene.GetRootGameObjects())
            GameObject.DestroyImmediate(rootGo);

        // GameManager (singleton DontDestroyOnLoad)
        var gmGo = new GameObject("GameManager");
        gmGo.AddComponent<GameManager>();

        // UI Root amb AuthManager
        var uiGo = new GameObject("UI Root");
        var uiDoc = uiGo.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/LoginScreen.uxml");
        var panelSettings = LoadPanelSettings();
        if (panelSettings != null) uiDoc.panelSettings = panelSettings;
        uiGo.AddComponent<AuthManager>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ─── MenuScene ───────────────────────────────────────────────────────

    private static void SetupMenuScene()
    {
        string path = "Assets/Scenes/MenuScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        foreach (var rootGo in scene.GetRootGameObjects())
            GameObject.DestroyImmediate(rootGo);

        // Càmera principal
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";

        // UI Root amb MenuManager
        var uiGo = new GameObject("UI Root");
        var uiDoc = uiGo.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/MenuScreen.uxml");
        var panelSettings = LoadPanelSettings();
        if (panelSettings != null) uiDoc.panelSettings = panelSettings;
        uiGo.AddComponent<MenuManager>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ─── WaitingScene ────────────────────────────────────────────────────

    private static void SetupWaitingScene()
    {
        string path = "Assets/Scenes/WaitingScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        foreach (var rootGo in scene.GetRootGameObjects())
            GameObject.DestroyImmediate(rootGo);

        // Càmera principal
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";

        // UI Root amb WaitingManager
        var uiGo = new GameObject("UI Root");
        var uiDoc = uiGo.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/WaitingScreen.uxml");
        var panelSettings = LoadPanelSettings();
        if (panelSettings != null) uiDoc.panelSettings = panelSettings;
        uiGo.AddComponent<WaitingManager>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ─── CombatScene ─────────────────────────────────────────────────────

    private static void SetupCombatScene()
    {
        string path = "Assets/Scenes/CombatScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        foreach (var rootGo in scene.GetRootGameObjects())
            GameObject.DestroyImmediate(rootGo);

        // 1. Càmera (posició 0, 0.5, -10 per visualitzar el terreny millor)
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        var cam = camGo.GetComponent<Camera>();
        camGo.transform.position = new Vector3(0, 0.5f, -10);
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
        cam.orthographic = true;
        cam.orthographicSize = 6;
        camGo.tag = "MainCamera";

        // 2. Terreny amb material URP/Unlit
        var terrainGo = new GameObject("Terrain");
        var tg = terrainGo.AddComponent<TerrainGenerator>();
        terrainGo.GetComponent<MeshRenderer>().sharedMaterial = LoadTerrainMaterial();

        // 3. Tancs (Player1 = blau, Player2 = vermell)
        GameObject p1 = BuildTank("Player1Tank", true);
        GameObject p2 = BuildTank("Player2Tank", false);

        var tc1 = p1.GetComponent<TankController>();
        var tc2 = p2.GetComponent<TankController>();
        tc1.terrain = tg;
        tc2.terrain = tg;
        tc1.playerId = 1;
        tc2.playerId = 2;

        // 4. Input humà per al jugador local
        var hInput1 = p1.AddComponent<CombatInput>();

        // 5. Manager i UI
        var mgrGo = new GameObject("CombatManager");

        var uiDoc = mgrGo.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/CombatScreen.uxml");

        var panelSettings = LoadPanelSettings();
        if (panelSettings != null) uiDoc.panelSettings = panelSettings;

        var combatMgr = mgrGo.AddComponent<CombatManager>();
        combatMgr.terrain = tg;
        combatMgr.player1Tank = tc1;
        combatMgr.player2Tank = tc2;
        combatMgr.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        combatMgr.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Explosion.prefab");
        combatMgr.mainCamera = cam;

        hInput1.manager = combatMgr;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ─── VsAIScene ───────────────────────────────────────────────────────

    private static void SetupVsAIScene()
    {
        string path = "Assets/Scenes/VsAIScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        foreach (var rootGo in scene.GetRootGameObjects())
            GameObject.DestroyImmediate(rootGo);

        // 1. Càmera
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        var cam = camGo.GetComponent<Camera>();
        camGo.transform.position = new Vector3(0, 0.5f, -10);
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
        cam.orthographic = true;
        cam.orthographicSize = 6;
        camGo.tag = "MainCamera";

        // 2. Terreny amb material URP/Unlit
        var terrainGo = new GameObject("Terrain");
        var tg = terrainGo.AddComponent<TerrainGenerator>();
        terrainGo.GetComponent<MeshRenderer>().sharedMaterial = LoadTerrainMaterial();

        // 3. Tancs
        GameObject p1 = BuildTank("PlayerHuman", true);
        GameObject p2 = BuildTank("AI_Agent", false);

        var tc1 = p1.GetComponent<TankController>();
        var tc2 = p2.GetComponent<TankController>();
        tc1.terrain = tg;
        tc2.terrain = tg;

        // 4. Lògica ML-Agent
        var agent = p2.AddComponent<TankAgent>();
        agent.localTank = tc2;
        agent.enemyTank = tc1;
        agent.terrain = tg;
        agent.isVsAIMode = true;
        agent.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        agent.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Explosion.prefab");

        var bp = p2.AddComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        bp.BehaviorName = "TankBehavior";
        bp.BehaviorType = Unity.MLAgents.Policies.BehaviorType.InferenceOnly;

        // Carregar model ONNX
        var model = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Models/TankBehavior.onnx");
        if (model != null)
        {
            var so = new SerializedObject(bp);
            so.FindProperty("m_Model").objectReferenceValue = model;
            so.ApplyModifiedProperties();
        }

        // DecisionRequester per executar el motor d'inferència
        var dr = p2.AddComponent<Unity.MLAgents.DecisionRequester>();
        dr.DecisionPeriod = 1;
        dr.TakeActionsBetweenDecisions = false;

        // 5. Input humà
        var hInput = p1.AddComponent<HumanTankInput>();
        hInput.tank = tc1;

        // 6. Manager i UI
        var mgrGo = new GameObject("VsAIManager");

        var uiDoc = mgrGo.AddComponent<UIDocument>();
        uiDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UXML/CombatScreen.uxml");

        var panelSettings = LoadPanelSettings();
        if (panelSettings != null) uiDoc.panelSettings = panelSettings;

        var vsMgr = mgrGo.AddComponent<VsAIManager>();
        vsMgr.terrain = tg;
        vsMgr.playerTank = tc1;
        vsMgr.aiTank = tc2;
        vsMgr.aiAgent = agent;
        hInput.manager = vsMgr;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ─── TrainingScene ───────────────────────────────────────────────────

    private static void SetupTrainingScene()
    {
        string path = "Assets/Scenes/TrainingScene.unity";
        var scene = EditorSceneManager.OpenScene(path);

        foreach (var rootGo in scene.GetRootGameObjects())
            GameObject.DestroyImmediate(rootGo);

        // 1. Càmera i terreny
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.GetComponent<Camera>().orthographic = true;
        camGo.GetComponent<Camera>().orthographicSize = 6.5f;

        var terrainGo = new GameObject("Terrain");
        var tg = terrainGo.AddComponent<TerrainGenerator>();
        terrainGo.GetComponent<MeshRenderer>().sharedMaterial = LoadTerrainMaterial();

        // 2. Tancs
        GameObject agentObj = BuildTank("AI_Agent_Trainer", false);
        GameObject targetObj = BuildTank("Training_Target", true);

        var tc1 = agentObj.GetComponent<TankController>();
        var tc2 = targetObj.GetComponent<TankController>();
        tc1.terrain = tg;
        tc2.terrain = tg;

        // 3. Configuració de l'agent
        var agent = agentObj.AddComponent<TankAgent>();
        agent.localTank = tc1;
        agent.enemyTank = tc2;
        agent.terrain = tg;
        agent.isVsAIMode = false;
        agent.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        agent.explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Explosion.prefab");

        var bp = agentObj.AddComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        bp.BehaviorName = "TankBehavior";
        bp.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;

        agentObj.AddComponent<Unity.MLAgents.DecisionRequester>().DecisionPeriod = 1;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ─── Utilitats ───────────────────────────────────────────────────────

    private static GameObject BuildTank(string name, bool isPlayer)
    {
        var tank = new GameObject(name);
        tank.tag = "Tank";

        var body = new GameObject("Body");
        body.transform.SetParent(tank.transform);
        var bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sprite = LoadTankSprite(isPlayer, "body");
        bodySr.sortingOrder = 1;

        var barrel = new GameObject("Barrel");
        barrel.transform.SetParent(tank.transform);
        barrel.transform.localPosition = new Vector3(isPlayer ? 0.3f : -0.3f, 0.2f, 0);
        var barrelSr = barrel.AddComponent<SpriteRenderer>();
        barrelSr.sprite = LoadTankSprite(isPlayer, "barrel");
        barrelSr.sortingOrder = 2;

        var tc = tank.AddComponent<TankController>();
        tc.barrel = barrel.transform;

        var rb = tank.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        tank.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 0.6f);

        return tank;
    }

    /// <summary>Carrega l'sprite del tanc provant múltiples rutes.</summary>
    private static Sprite LoadTankSprite(bool isPlayer, string part)
    {
        string color = isPlayer ? "blue" : "red";
        string fileName = $"tank_{color}_{part}.png";

        // Provar primer a Assets/Sprites/Tanks/
        string path1 = $"Assets/Sprites/Tanks/{fileName}";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path1);
        if (sprite != null) return sprite;

        // Provar a Assets/Resources/Images/tanks/
        string path2 = $"Assets/Resources/Images/tanks/{fileName}";
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path2);
        if (sprite != null) return sprite;

        Debug.LogWarning($"Sprite no trobat: {fileName} (provat {path1} i {path2})");
        return null;
    }

    private static PanelSettings LoadPanelSettings()
    {
        // Provar la ruta esperada
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/New Panel Settings.asset");
        if (panelSettings != null) return panelSettings;

        // Buscar qualsevol PanelSettings al projecte
        string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
        if (guids.Length == 0) return null;

        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
    }

    private static void SetupTags()
    {
        var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets.Length == 0) return;

        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
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
