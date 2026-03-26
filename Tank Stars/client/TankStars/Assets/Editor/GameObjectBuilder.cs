using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class SceneSpec
{
    public string sceneName;
    public string uxmlPath;
    public string managerScriptPath;
    public bool createGameManager;
}

public class GameObjectBuilder : EditorWindow
{
    private PanelSettings panelSettings;

    private static readonly SceneSpec[] SceneCatalog = new[]
    {
        new SceneSpec
        {
            sceneName         = "LoginScene",
            uxmlPath          = "Assets/UI/UXML/LoginScreen.uxml",
            managerScriptPath = "Assets/Scripts/AuthManager.cs",
            createGameManager = true
        },
        new SceneSpec
        {
            sceneName         = "MenuScene",
            uxmlPath          = "Assets/UI/UXML/MenuScreen.uxml",
            managerScriptPath = "Assets/Scripts/MenuManager.cs",
            createGameManager = false
        },
        new SceneSpec
        {
            sceneName         = "WaitingScene",
            uxmlPath          = "Assets/UI/UXML/WaitingScreen.uxml",
            managerScriptPath = "Assets/Scripts/WaitingManager.cs",
            createGameManager = false
        },
    };

    [MenuItem("Tools/GameObject Builder")]
    public static void ShowWindow()
    {
        GetWindow<GameObjectBuilder>("GameObject Builder");
    }

    void OnEnable()
    {
        panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/New Panel Settings.asset");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Tank Stars Scene Builder", EditorStyles.boldLabel);

        panelSettings = (PanelSettings)EditorGUILayout.ObjectField(
            "Panel Settings", panelSettings, typeof(PanelSettings), false);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click the button below to auto-create all UI scenes.\n" +
            "Make sure all UXML files and Scripts exist first.",
            MessageType.Info);

        EditorGUILayout.Space();

        GUI.enabled = panelSettings != null;
        if (GUILayout.Button("Build All UI Scenes", GUILayout.Height(40)))
        {
            if (!ValidateDependencies()) return;

            int created = 0;
            foreach (var spec in SceneCatalog)
            {
                if (CreateScene(spec)) created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateBuildSettings();
            OpenLoginScene();

            Debug.Log($"[GameObjectBuilder] Done. {created}/{SceneCatalog.Length} scenes created. " +
                      "Scenes were added to Build Settings and LoginScene was opened.");

            EditorUtility.DisplayDialog(
                "Scenes Created",
                $"{created} scene(s) created in Assets/Scenes/.\n\n" +
                "Build Settings were updated and LoginScene is now open in the Hierarchy.",
                "OK");
        }
        GUI.enabled = true;

        if (panelSettings == null)
        {
            EditorGUILayout.HelpBox(
                "No PanelSettings found at 'Assets/New Panel Settings.asset'.\n" +
                "Create one via: Assets > Create > UI Toolkit > Panel Settings",
                MessageType.Warning);
        }
    }

    // ---------------------------------------------------------------
    // Validation
    // ---------------------------------------------------------------
    bool ValidateDependencies()
    {
        if (panelSettings == null)
        {
            EditorUtility.DisplayDialog("Missing PanelSettings",
                "Assign a PanelSettings asset before generating scenes.", "OK");
            return false;
        }

        foreach (var spec in SceneCatalog)
        {
            if (!File.Exists(spec.uxmlPath))
            {
                Debug.LogError($"[GameObjectBuilder] UXML not found: {spec.uxmlPath}");
                EditorUtility.DisplayDialog("Missing File",
                    $"UXML not found:\n{spec.uxmlPath}\n\nCreate the file first.", "OK");
                return false;
            }

            if (!File.Exists(spec.managerScriptPath))
            {
                Debug.LogError($"[GameObjectBuilder] Script not found: {spec.managerScriptPath}");
                EditorUtility.DisplayDialog("Missing File",
                    $"Script not found:\n{spec.managerScriptPath}\n\nCreate the script first.", "OK");
                return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------
    // Scene creation
    // ---------------------------------------------------------------
    bool CreateScene(SceneSpec spec)
    {
        string sceneFolder = "Assets/Scenes";
        if (!Directory.Exists(sceneFolder))
            Directory.CreateDirectory(sceneFolder);

        string scenePath = $"{sceneFolder}/{spec.sceneName}.unity";

        // Create a brand-new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera(scene);

        // --- GameManager (only in LoginScene so it persists via DontDestroyOnLoad) ---
        if (spec.createGameManager)
        {
            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            EditorSceneManager.MoveGameObjectToScene(gmGO, scene);
        }

        // --- UI Root ---
        var uiRoot = new GameObject("UI Root");

        // UIDocument
        var document = uiRoot.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;

        var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(spec.uxmlPath);
        if (vta == null)
        {
            Debug.LogError($"[GameObjectBuilder] Failed to load VisualTreeAsset: {spec.uxmlPath}");
            DestroyImmediate(uiRoot);
            return false;
        }
        document.visualTreeAsset = vta;

        // Manager script
        var mgr = AddManagerComponent(spec.managerScriptPath, uiRoot);
        if (mgr == null)
        {
            Debug.LogError($"[GameObjectBuilder] Could not add manager script for {spec.sceneName}.");
            DestroyImmediate(uiRoot);
            return false;
        }

        EditorSceneManager.MoveGameObjectToScene(uiRoot, scene);

        // Save
        bool saved = EditorSceneManager.SaveScene(scene, scenePath);
        if (!saved)
        {
            Debug.LogError($"[GameObjectBuilder] Failed to save scene: {scenePath}");
            return false;
        }

        Debug.Log($"[GameObjectBuilder] Created: {scenePath}");
        return true;
    }

    void CreateMainCamera(UnityEngine.SceneManagement.Scene scene)
    {
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";

        var cameraComponent = cameraGO.AddComponent<Camera>();
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = new Color(0.06f, 0.08f, 0.14f, 1f);
        cameraGO.AddComponent<AudioListener>();

        EditorSceneManager.MoveGameObjectToScene(cameraGO, scene);
    }

    void UpdateBuildSettings()
    {
        var generatedScenePaths = SceneCatalog
            .Select(spec => $"Assets/Scenes/{spec.sceneName}.unity")
            .ToArray();

        var existingScenes = EditorBuildSettings.scenes
            .Where(scene => !generatedScenePaths.Contains(scene.path))
            .ToList();

        var orderedGeneratedScenes = generatedScenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToList();

        orderedGeneratedScenes.AddRange(existingScenes);
        EditorBuildSettings.scenes = orderedGeneratedScenes.ToArray();
    }

    void OpenLoginScene()
    {
        string loginScenePath = $"Assets/Scenes/{SceneCatalog[0].sceneName}.unity";
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(loginScenePath, OpenSceneMode.Single);
    }

    // ---------------------------------------------------------------
    // Attach a MonoBehaviour by script path
    // ---------------------------------------------------------------
    Component AddManagerComponent(string scriptPath, GameObject target)
    {
        // Force a compile/import so GetClass() works reliably
        AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);

        var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (monoScript == null)
        {
            Debug.LogError($"[GameObjectBuilder] MonoScript not loaded: {scriptPath}");
            return null;
        }

        var cls = monoScript.GetClass();
        if (cls == null)
        {
            Debug.LogError($"[GameObjectBuilder] GetClass() returned null for {scriptPath}. " +
                           "Make sure the script has compiled without errors.");
            return null;
        }

        if (!typeof(MonoBehaviour).IsAssignableFrom(cls))
        {
            Debug.LogError($"[GameObjectBuilder] {cls.Name} does not extend MonoBehaviour.");
            return null;
        }

        return target.AddComponent(cls);
    }
}
