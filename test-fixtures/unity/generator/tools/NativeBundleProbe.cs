using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UabsFixturesProbe.Editor
{
    public static class NativeBundleProbe
    {
        private const string BundleName = "uabs-native-probe.bundle";

        public static void Run()
        {
            string output = RequiredArgument("-probeOutput");
            Directory.CreateDirectory(output);
            string reportPath = Path.Combine(output, "probe-result.txt");
            bool manifestCreated = false;
            bool bundleExists = false;
            bool reopened = false;
            string error = string.Empty;

            try
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
                const string assetDirectory = "Assets/NativeBundleProbe";
                if (!AssetDatabase.IsValidFolder(assetDirectory))
                    AssetDatabase.CreateFolder("Assets", "NativeBundleProbe");
                const string assetPath = assetDirectory + "/minimal.txt";
                File.WriteAllText(
                    Path.Combine(Application.dataPath, "NativeBundleProbe", "minimal.txt"),
                    "UABS isolated native AssetBundle probe\n");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                string bundleDirectory = Path.Combine(output, "bundles");
                Directory.CreateDirectory(bundleDirectory);
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                    bundleDirectory,
                    new[] { new AssetBundleBuild { assetBundleName = BundleName, assetNames = new[] { assetPath } } },
                    BuildAssetBundleOptions.DeterministicAssetBundle
                    | BuildAssetBundleOptions.ChunkBasedCompression
                    | BuildAssetBundleOptions.StrictMode
                    | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                    EditorUserBuildSettings.activeBuildTarget);
                manifestCreated = manifest != null;
                string bundlePath = Path.Combine(bundleDirectory, BundleName);
                bundleExists = File.Exists(bundlePath);
                if (bundleExists)
                {
                    AssetBundle loaded = AssetBundle.LoadFromFile(bundlePath);
                    reopened = loaded != null;
                    if (loaded != null) loaded.Unload(true);
                }
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }
            finally
            {
                File.WriteAllLines(reportPath, new[]
                {
                    "unityVersion=" + Application.unityVersion,
                    "activeBuildTarget=" + EditorUserBuildSettings.activeBuildTarget,
                    "standaloneSubtarget=" + EditorUserBuildSettings.standaloneBuildSubtarget,
                    "graphicsDevice=" + SystemInfo.graphicsDeviceType,
                    "manifestCreated=" + manifestCreated,
                    "bundleExists=" + bundleExists,
                    "reopened=" + reopened,
                    "errorBase64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(error))
                });
                Debug.Log(
                    $"UABS_NATIVE_BUNDLE_PROBE manifest={manifestCreated} bundle={bundleExists} " +
                    $"reopened={reopened} report={reportPath}");
            }

            if (!manifestCreated || !bundleExists || !reopened || !string.IsNullOrEmpty(error))
                throw new InvalidOperationException("Native AssetBundle probe failed; see " + reportPath);
        }

        private static string RequiredArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, name);
            if (index < 0 || index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]))
                throw new InvalidOperationException("Missing required argument " + name);
            return Path.GetFullPath(arguments[index + 1]);
        }
    }
}
