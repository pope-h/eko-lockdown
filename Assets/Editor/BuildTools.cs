using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Eko > Create Scene, Build WebGL, Build Mac. Also callable headless with -executeMethod.
public static class BuildTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Eko/Create Scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Resources");
        // One lit material asset, loaded at runtime and cloned per colour. Keeping it as an
        // asset is what stops the build from stripping the Standard shader variants we use.
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/EkoLit.mat") == null)
        {
            var m = new Material(Shader.Find("Standard")) { enableInstancing = true };
            AssetDatabase.CreateAsset(m, "Assets/Resources/EkoLit.mat");
        }
        // The game builds itself at startup, so the scene only carries render settings:
        // fog on (or its shader variants are stripped) and the default sky.
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        EditorSceneManager.SaveScene(sc, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "Eko Lockdown";
        PlayerSettings.companyName = "EkoLockdown";
        Debug.Log("Eko: scene created");
    }

    [MenuItem("Eko/Build WebGL")]
    public static void BuildWebGL()
    {
        CreateScene();
        // Gzip with the fallback decoder: about a third of the download, and it works on any static
        // host (itch.io, GitHub Pages) without special Content-Encoding headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.template = "PROJECT:Eko"; // Assets/WebGLTemplates/Eko: phone-friendly page
        Run("Builds/WebGL", BuildTarget.WebGL);
    }

    [MenuItem("Eko/Build Mac")]
    public static void BuildMac()
    {
        CreateScene();
        Run("Builds/Mac/EkoLockdown.app", BuildTarget.StandaloneOSX);
    }

    static void Run(string path, BuildTarget target)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        });
        Debug.Log("Eko build " + target + ": " + report.summary.result + " -> " + path);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
