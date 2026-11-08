#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// GitHub Actions (GameCI) derleme girişi. Önce TMP kaynaklarını içe aktarır ve "Her Şeyi Kur" kurulumunu çalıştırır,
/// sonra APK veya Xcode projesi üretir. Komut satırı: -executeMethod PocketBaristaCI.Build -customBuildPath &lt;yol&gt; -buildTarget Android|iOS
/// </summary>
public static class PocketBaristaCI
{
    private const string BundleId = "com.ates.pocketbarista";

    public static void Build()
    {
        try
        {
            ImportTmpEssentials();
            PocketBaristaSetup.SetupAll();

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string outPath = Arg("-customBuildPath") ?? (target == BuildTarget.iOS ? "build/iOS" : "build/Android/PocketBarista.apk");
            ConfigurePlayer(target);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outPath,
                target = target,
                options = BuildOptions.None
            };
            if (options.scenes.Length == 0) throw new Exception("Build Settings'te sahne yok (kurulum başarısız mı?)");

            Debug.Log($"[CI] Derleniyor: {target} → {outPath} ({options.scenes.Length} sahne)");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[CI] Sonuç: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} hata");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 101);
        }
        catch (Exception e)
        {
            Debug.LogError("[CI] Derleme hatası: " + e);
            EditorApplication.Exit(102);
        }
    }

    private static void ConfigurePlayer(BuildTarget target)
    {
        PlayerSettings.SetApplicationIdentifier(target == BuildTarget.iOS ? UnityEditor.Build.NamedBuildTarget.iOS : UnityEditor.Build.NamedBuildTarget.Android, BundleId);
        PlayerSettings.bundleVersion = "0.1." + (Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "0");
        if (target == BuildTarget.Android)
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            int.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "1", out int code);
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, code);
            EditorUserBuildSettings.buildAppBundle = false; // APK
        }
        else if (target == BuildTarget.iOS)
        {
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        }
    }

    /// <summary>TextMeshPro temel kaynakları (font) yoksa paketten içe aktarır.</summary>
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
        string[] candidates =
        {
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
            "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
        };
        foreach (string c in candidates)
        {
            string full = Path.GetFullPath(c);
            if (!File.Exists(full)) continue;
            Debug.Log("[CI] TMP Essential Resources içe aktarılıyor: " + full);
            AssetDatabase.ImportPackage(full, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return;
        }
        Debug.LogWarning("[CI] TMP Essential Resources paketi bulunamadı.");
    }

    private static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
#endif
