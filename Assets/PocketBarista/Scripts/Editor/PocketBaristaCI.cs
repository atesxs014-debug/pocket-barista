#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
            Debug.Log("[CI] TMP Essential Resources açılıyor: " + full);
            // AssetDatabase.ImportPackage batch modda asenkron çalışır ve kurulumdan önce bitmez.
            // Bu yüzden .unitypackage (tar.gz) dosyasını kendimiz açıp dosyaları Assets'e yazıyoruz.
            int count = ExtractUnityPackage(full);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[CI] TMP Essential Resources: {count} dosya yazıldı.");
            return;
        }
        Debug.LogWarning("[CI] TMP Essential Resources paketi bulunamadı.");
    }

    /// <summary>.unitypackage = tar.gz; her GUID klasöründe pathname, asset ve asset.meta bulunur.</summary>
    private static int ExtractUnityPackage(string packagePath)
    {
        var entries = new Dictionary<string, Dictionary<string, byte[]>>();
        using (FileStream fs = File.OpenRead(packagePath))
        using (var gz = new GZipStream(fs, CompressionMode.Decompress))
        {
            var header = new byte[512];
            string longName = null;
            while (ReadFull(gz, header, 512))
            {
                if (header[0] == 0) break; // Arşiv sonu
                string name = longName ?? CString(header, 0, 100);
                longName = null;
                string prefix = CString(header, 345, 155);
                if (!string.IsNullOrEmpty(prefix)) name = prefix + "/" + name;
                string octal = CString(header, 124, 12).Trim();
                long size = octal.Length > 0 ? Convert.ToInt64(octal, 8) : 0;
                char type = (char)header[156];
                var data = new byte[size];
                if (size > 0 && !ReadFull(gz, data, (int)size)) break;
                long pad = (512 - size % 512) % 512;
                if (pad > 0) ReadFull(gz, new byte[pad], (int)pad);

                if (type == 'L') { longName = Encoding.UTF8.GetString(data).TrimEnd('\0'); continue; }
                if (type != '0' && type != '\0') continue; // Sadece normal dosyalar

                name = name.TrimStart('.', '/');
                int slash = name.IndexOf('/');
                if (slash <= 0) continue;
                string guid = name.Substring(0, slash);
                string file = name.Substring(slash + 1);
                if (!entries.TryGetValue(guid, out var files)) entries[guid] = files = new Dictionary<string, byte[]>();
                files[file] = data;
            }
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        int written = 0;
        foreach (var kv in entries)
        {
            if (!kv.Value.TryGetValue("pathname", out byte[] pn)) continue;
            string rel = Encoding.UTF8.GetString(pn).Split('\n')[0].Trim();
            if (!rel.StartsWith("Assets/")) continue;
            string dest = Path.Combine(projectRoot, rel);
            if (kv.Value.TryGetValue("asset", out byte[] asset))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.WriteAllBytes(dest, asset);
                written++;
            }
            else Directory.CreateDirectory(dest);
            if (kv.Value.TryGetValue("asset.meta", out byte[] meta)) File.WriteAllBytes(dest + ".meta", meta);
        }
        return written;
    }

    private static bool ReadFull(Stream s, byte[] buf, int count)
    {
        int off = 0;
        while (off < count)
        {
            int n = s.Read(buf, off, count - off);
            if (n <= 0) return false;
            off += n;
        }
        return true;
    }

    private static string CString(byte[] b, int start, int len)
    {
        int end = start;
        while (end < start + len && b[end] != 0) end++;
        return Encoding.ASCII.GetString(b, start, end - start);
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
