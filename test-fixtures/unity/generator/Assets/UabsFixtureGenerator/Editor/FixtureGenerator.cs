using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace UabsFixtures.Editor
{
    public static class FixtureGenerator
    {
        private const string GeneratorVersion = "1.0.0";
        private const string GeneratedAssetRoot = "Assets/GeneratedFixtures";
        private static readonly HashSet<string> SupportedTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Texture2D", "Sprite", "Material", "Prefab", "TextAsset",
            "ScriptableObject", "AnimationClip", "SpriteAtlas"
        };

        [Serializable]
        private sealed class GeneratorConfig
        {
            public string profile;
            public string buildTarget;
            public int seed;
            public int bundleCount;
            public int assetsPerBundle;
            public string compression;
            public float dependencyDensity;
            public float duplicateNameFrequency;
            public bool includeUnicodeNames;
            public string[] assetTypes;
            public string outputDirectory;
        }

        [Serializable]
        private sealed class SourceAssignment
        {
            public string assetPath;
            public string assetName;
            public string assetType;
        }

        [Serializable]
        private sealed class BundleRecord
        {
            public string name;
            public string file;
            public long sizeBytes;
            public string sha256;
            public List<SourceAssignment> sourceAssignments = new List<SourceAssignment>();
            public string[] unityDependencies;
            public string[] unityReportedContents;
        }

        [Serializable]
        private sealed class FixtureManifest
        {
            public int schemaVersion = 1;
            public string generatorVersion = GeneratorVersion;
            public string unityVersion;
            public string buildTarget;
            public string profile;
            public int seed;
            public GeneratorConfig configuration;
            public List<BundleRecord> bundles = new List<BundleRecord>();
            public string[] knownCharacteristics;
            public string scopeNotice;
        }

        private sealed class PlannedBundle
        {
            public string Name;
            public readonly List<string> Paths = new List<string>();
            public readonly List<SourceAssignment> Assignments = new List<SourceAssignment>();
        }

        [Serializable]
        private sealed class ModuleDiagnosticReport
        {
            public string unityVersion;
            public string buildTarget;
            public string standaloneSubtarget;
            public string graphicsDevice;
            public bool originalStripEngineCode;
            public bool requestedStripEngineCode;
            public string managedStrippingLevel;
            public bool manifestCreated;
            public bool bundleExists;
            public long sizeBytes;
            public string header;
            public bool reopened;
            public string error;
        }

        public static void Generate()
        {
            string configArgument = RequiredArgument("-fixtureConfig");
            string outputArgument = RequiredArgument("-fixtureOutput");
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string generatedRoot = Path.GetFullPath(Path.Combine(projectRoot, "Generated", "working"));
            string outputRoot = RequireContained(Path.GetFullPath(outputArgument), generatedRoot, "output");
            string configRoot = Path.GetFullPath(Path.Combine(projectRoot, "config"));
            string configPath = RequireContained(Path.GetFullPath(configArgument), configRoot, "configuration");

            GeneratorConfig config = JsonUtility.FromJson<GeneratorConfig>(File.ReadAllText(configPath));
            Validate(config);
            if (!Enum.TryParse(config.buildTarget, out BuildTarget target)
                || target != BuildTarget.StandaloneWindows64
                   && target != BuildTarget.StandaloneLinux64
                   && target != BuildTarget.StandaloneOSX)
            {
                throw new InvalidOperationException($"Unsupported standalone build target: {config.buildTarget}");
            }

            ConfigureStandalonePlayerSubtarget();
            ValidateEditorContext(target);

            PrepareOutput(outputRoot);
            PrepareGeneratedAssets(config.profile);
            List<PlannedBundle> plans = CreateAssets(config, new System.Random(config.seed));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string bundleDirectory = Path.Combine(outputRoot, "bundles");
            Directory.CreateDirectory(bundleDirectory);
            AssetBundleBuild[] builds = plans.Select(plan => new AssetBundleBuild
            {
                assetBundleName = plan.Name,
                assetNames = plan.Paths.OrderBy(path => path, StringComparer.Ordinal).ToArray()
            }).ToArray();
            AssetBundleManifest built = BuildPipeline.BuildAssetBundles(
                bundleDirectory,
                builds,
                BuildOptions(config.compression) | BuildAssetBundleOptions.StrictMode,
                target);
            if (built == null)
                throw new InvalidOperationException("Unity returned no AssetBundleManifest.");

            FixtureManifest manifest = new FixtureManifest
            {
                unityVersion = Application.unityVersion,
                buildTarget = target.ToString(),
                profile = config.profile,
                seed = config.seed,
                configuration = config,
                knownCharacteristics = new[]
                {
                    "Original programmatically generated assets only; no game-extracted content.",
                    "Includes duplicate names, Unicode names, and deliberate cross-bundle references when configured.",
                    "Stress output remains local unless separately allowlisted and explicitly approved."
                },
                scopeNotice = "sourceAssignments lists generator-known inputs only. Unity may create additional internal serialized objects; UABS-extracted records require separate validation."
            };

            foreach (PlannedBundle plan in plans.OrderBy(plan => plan.Name, StringComparer.Ordinal))
            {
                string file = Path.Combine(bundleDirectory, plan.Name);
                if (!File.Exists(file))
                    throw new FileNotFoundException("Unity did not emit an expected bundle.", file);
                AssetBundle loadedBundle = AssetBundle.LoadFromFile(file);
                if (loadedBundle == null)
                {
                    throw new InvalidOperationException(
                        $"Unity {Application.unityVersion} built but could not reopen '{file}' " +
                        $"({new FileInfo(file).Length} bytes, header '{ReadHeader(file)}'). " +
                        $"Configured/active target: {target}/{EditorUserBuildSettings.activeBuildTarget}; " +
                        $"standalone subtarget: {EditorUserBuildSettings.standaloneBuildSubtarget}; " +
                        $"graphics device: {SystemInfo.graphicsDeviceType}. Check the preceding Unity module or " +
                        "bundle compatibility errors; generation is not successful until every bundle reopens.");
                }
                string[] reportedContents = loadedBundle.GetAllAssetNames()
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                loadedBundle.Unload(true);
                manifest.bundles.Add(new BundleRecord
                {
                    name = plan.Name,
                    file = "bundles/" + plan.Name,
                    sizeBytes = new FileInfo(file).Length,
                    sha256 = Sha256(file),
                    sourceAssignments = plan.Assignments,
                    unityDependencies = built.GetAllDependencies(plan.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    unityReportedContents = reportedContents
                });
            }

            File.WriteAllText(Path.Combine(outputRoot, "fixture-manifest.json"), JsonUtility.ToJson(manifest, true) + Environment.NewLine);
            Debug.Log($"Generated {manifest.bundles.Count} UABS fixture bundles at {outputRoot}");
        }

        public static void DiagnoseModuleStripping()
        {
            string configArgument = RequiredArgument("-fixtureConfig");
            string outputArgument = RequiredArgument("-fixtureOutput");
            bool requestedStripping = ParseBooleanArgument("-fixtureStripEngineCode");
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string generatedRoot = Path.GetFullPath(Path.Combine(projectRoot, "Generated", "working"));
            string outputRoot = RequireContained(Path.GetFullPath(outputArgument), generatedRoot, "output");
            string configRoot = Path.GetFullPath(Path.Combine(projectRoot, "config"));
            string configPath = RequireContained(Path.GetFullPath(configArgument), configRoot, "configuration");
            GeneratorConfig config = JsonUtility.FromJson<GeneratorConfig>(File.ReadAllText(configPath));
            Validate(config);
            if (!Enum.TryParse(config.buildTarget, out BuildTarget target)
                || target != BuildTarget.StandaloneWindows64
                   && target != BuildTarget.StandaloneLinux64
                   && target != BuildTarget.StandaloneOSX)
                throw new InvalidOperationException($"Unsupported standalone build target: {config.buildTarget}");

            ConfigureStandalonePlayerSubtarget();
            bool originalStripping = PlayerSettings.stripEngineCode;
            ModuleDiagnosticReport report = new ModuleDiagnosticReport
            {
                unityVersion = Application.unityVersion,
                buildTarget = target.ToString(),
                standaloneSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget.ToString(),
                graphicsDevice = SystemInfo.graphicsDeviceType.ToString(),
                originalStripEngineCode = originalStripping,
                requestedStripEngineCode = requestedStripping,
                managedStrippingLevel = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone).ToString(),
                error = string.Empty
            };
            bool outputPrepared = false;
            string reportPath = Path.Combine(outputRoot, "module-diagnostic.json");

            try
            {
                PlayerSettings.stripEngineCode = requestedStripping;
                ValidateEditorContext(target);
                PrepareOutput(outputRoot);
                outputPrepared = true;
                string assetDirectory = $"{GeneratedAssetRoot}/module-diagnostic";
                EnsureAssetFolder(assetDirectory);
                string assetPath = $"{assetDirectory}/minimal.txt";
                File.WriteAllText(AbsoluteAssetPath(assetPath), "UABS minimal AssetBundle module diagnostic\n");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                string bundleDirectory = Path.Combine(outputRoot, "bundles");
                Directory.CreateDirectory(bundleDirectory);
                const string bundleName = "uabs-module-diagnostic.bundle";
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                    bundleDirectory,
                    new[] { new AssetBundleBuild { assetBundleName = bundleName, assetNames = new[] { assetPath } } },
                    BuildOptions(config.compression) | BuildAssetBundleOptions.StrictMode
                                                     | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                    target);
                report.manifestCreated = manifest != null;
                string bundlePath = Path.Combine(bundleDirectory, bundleName);
                report.bundleExists = File.Exists(bundlePath);
                if (report.bundleExists)
                {
                    report.sizeBytes = new FileInfo(bundlePath).Length;
                    report.header = ReadHeader(bundlePath);
                    AssetBundle loaded = AssetBundle.LoadFromFile(bundlePath);
                    report.reopened = loaded != null;
                    if (loaded != null) loaded.Unload(true);
                }
            }
            catch (Exception exception)
            {
                report.error = exception.ToString();
            }
            finally
            {
                PlayerSettings.stripEngineCode = originalStripping;
                AssetDatabase.SaveAssets();
                if (outputPrepared)
                {
                    File.WriteAllText(
                        reportPath,
                        JsonUtility.ToJson(report, true) + Environment.NewLine);
                }
                Debug.Log(
                    $"UABS_MODULE_DIAGNOSTIC stripping={requestedStripping} manifest={report.manifestCreated} " +
                    $"bundle={report.bundleExists} reopened={report.reopened} error={report.error}");
            }

            if (!report.manifestCreated || !report.bundleExists || !report.reopened || !string.IsNullOrEmpty(report.error))
                throw new InvalidOperationException(
                    $"Module diagnostic failed with stripEngineCode={requestedStripping}; " +
                    (outputPrepared ? $"see {reportPath}" : "the output directory was not prepared; see the Unity log."));
        }

        private static List<PlannedBundle> CreateAssets(GeneratorConfig config, System.Random random)
        {
            List<PlannedBundle> plans = new List<PlannedBundle>();
            List<string> priorReferenceCandidates = new List<string>();
            for (int bundleIndex = 0; bundleIndex < config.bundleCount; bundleIndex++)
            {
                PlannedBundle plan = new PlannedBundle
                {
                    Name = $"uabs-{config.profile}-{bundleIndex:D4}.bundle"
                };
                string directory = $"{GeneratedAssetRoot}/{config.profile}/bundle-{bundleIndex:D4}";
                EnsureAssetFolder(directory);
                for (int assetIndex = 0; assetIndex < config.assetsPerBundle; assetIndex++)
                {
                    string assetDirectory = $"{directory}/slot-{assetIndex:D5}";
                    EnsureAssetFolder(assetDirectory);
                    string type = config.assetTypes[assetIndex % config.assetTypes.Length];
                    string stem = StableStem(config, bundleIndex, assetIndex);
                    string referencePath = priorReferenceCandidates.Count > 0
                                           && random.NextDouble() < config.dependencyDensity
                        ? priorReferenceCandidates[(bundleIndex + assetIndex) % priorReferenceCandidates.Count]
                        : null;
                    string path = CreateAsset(type, assetDirectory, stem, bundleIndex, assetIndex, referencePath);
                    plan.Paths.Add(path);
                    plan.Assignments.Add(new SourceAssignment
                    {
                        assetPath = path,
                        assetName = Path.GetFileNameWithoutExtension(path),
                        assetType = type
                    });
                    if (type != "SpriteAtlas")
                        priorReferenceCandidates.Add(path);
                }
                plans.Add(plan);
            }
            return plans;
        }

        private static string CreateAsset(
            string type, string directory, string stem, int bundleIndex, int assetIndex, string referencePath)
        {
            switch (type)
            {
                case "Texture2D":
                    return CreateTexture(directory, stem, false, bundleIndex, assetIndex);
                case "Sprite":
                    return CreateTexture(directory, stem, true, bundleIndex, assetIndex);
                case "Material":
                {
                    string path = $"{directory}/{stem}.mat";
                    Shader shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                    if (shader == null) throw new InvalidOperationException("No built-in material shader is available.");
                    Material material = new Material(shader) { name = stem, color = StableColor(bundleIndex, assetIndex) };
                    material.mainTexture = LoadReference<Texture>(referencePath);
                    AssetDatabase.CreateAsset(material, path);
                    return path;
                }
                case "Prefab":
                {
                    string path = $"{directory}/{stem}.prefab";
                    GameObject gameObject = new GameObject(stem);
                    FixtureMarker marker = gameObject.AddComponent<FixtureMarker>();
                    marker.Sequence = bundleIndex * 100000 + assetIndex;
                    marker.StableName = stem;
                    marker.ReferencedAsset = LoadReference<Object>(referencePath);
                    PrefabUtility.SaveAsPrefabAsset(gameObject, path);
                    Object.DestroyImmediate(gameObject);
                    return path;
                }
                case "TextAsset":
                {
                    string path = $"{directory}/{stem}.txt";
                    File.WriteAllText(AbsoluteAssetPath(path), $"UABS fixture {bundleIndex}/{assetIndex}\nUnicode=测试-テスト-😀\n");
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    return path;
                }
                case "ScriptableObject":
                {
                    string path = $"{directory}/{stem}.asset";
                    FixtureData data = ScriptableObject.CreateInstance<FixtureData>();
                    data.name = stem;
                    data.Sequence = bundleIndex * 100000 + assetIndex;
                    data.StableName = stem;
                    data.UnicodeText = "测试-テスト-😀";
                    data.ReferencedAsset = LoadReference<Object>(referencePath);
                    AssetDatabase.CreateAsset(data, path);
                    return path;
                }
                case "AnimationClip":
                {
                    string path = $"{directory}/{stem}.anim";
                    AnimationClip clip = new AnimationClip { name = stem, frameRate = 30f };
                    AnimationCurve curve = AnimationCurve.Linear(0f, assetIndex, 1f, assetIndex + 1f);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), curve);
                    AssetDatabase.CreateAsset(clip, path);
                    return path;
                }
                case "SpriteAtlas":
                {
                    string path = $"{directory}/{stem}.spriteatlas";
                    SpriteAtlas atlas = new SpriteAtlas { name = stem };
                    Object packable = LoadReference<Texture2D>(referencePath);
                    if (packable == null) packable = LoadReference<Sprite>(referencePath);
                    if (packable != null) atlas.Add(new[] { packable });
                    AssetDatabase.CreateAsset(atlas, path);
                    return path;
                }
                default:
                    throw new InvalidOperationException($"Unsupported requested asset type: {type}");
            }
        }

        private static string CreateTexture(string directory, string stem, bool sprite, int bundleIndex, int assetIndex)
        {
            string path = $"{directory}/{stem}.png";
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color color = StableColor(bundleIndex, assetIndex);
            texture.SetPixels(Enumerable.Repeat(color, 16 * 16).ToArray());
            texture.Apply(false, false);
            File.WriteAllBytes(AbsoluteAssetPath(path), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return path;
        }

        private static T LoadReference<T>(string path) where T : Object =>
            string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);

        private static string StableStem(GeneratorConfig config, int bundle, int asset)
        {
            double selector = ((bundle * 73856093L + asset * 19349663L + config.seed) & 0xffff) / 65535.0;
            if (selector < config.duplicateNameFrequency)
                return $"duplicate-{asset % 3:D2}";
            if (config.includeUnicodeNames && (bundle + asset) % 11 == 0)
                return $"资源-テスト-{bundle:D4}-{asset:D4}";
            return $"asset-{bundle:D4}-{asset:D4}";
        }

        private static Color StableColor(int bundle, int asset) => new Color(
            ((bundle * 37 + asset * 17) % 255) / 255f,
            ((bundle * 13 + asset * 53 + 71) % 255) / 255f,
            ((bundle * 97 + asset * 7 + 19) % 255) / 255f,
            1f);

        private static BuildAssetBundleOptions BuildOptions(string compression)
        {
            BuildAssetBundleOptions options = BuildAssetBundleOptions.DeterministicAssetBundle;
            switch (compression)
            {
                case "lz4": return options | BuildAssetBundleOptions.ChunkBasedCompression;
                case "lzma": return options;
                case "uncompressed": return options | BuildAssetBundleOptions.UncompressedAssetBundle;
                default: throw new InvalidOperationException($"Unsupported compression: {compression}");
            }
        }

        private static void ConfigureStandalonePlayerSubtarget()
        {
            StandaloneBuildSubtarget previous = EditorUserBuildSettings.standaloneBuildSubtarget;
            if (previous != StandaloneBuildSubtarget.Player)
            {
                Debug.LogWarning(
                    $"Changing standalone build subtarget from {previous} to Player. Unity 2021.3's public " +
                    "BuildAssetBundles overload inherits this setting; the Server subtarget strips runtime " +
                    "modules required by the generated AnimationClip and AssetBundle content.");
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            }
            if (EditorUserBuildSettings.standaloneBuildSubtarget != StandaloneBuildSubtarget.Player)
                throw new InvalidOperationException("Could not select the standalone Player build subtarget.");
        }

        private static void ValidateEditorContext(BuildTarget target)
        {
            if (!Application.unityVersion.StartsWith("2021.3.", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"The fixture project requires Unity 2021.3 LTS; running {Application.unityVersion}.");
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new InvalidOperationException(
                    $"Unity {Application.unityVersion} does not have the {target} build support module installed.");
            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException(
                    $"Configured target {target} does not match the active Editor target " +
                    $"{EditorUserBuildSettings.activeBuildTarget}. Launch through fixturectl so -buildTarget is set.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException(
                    "The fixture smoke test requires a graphics-enabled batch Editor for post-build AssetBundle " +
                    "load verification. Remove -nographics and run in a Windows desktop session.");
            Debug.Log(
                $"UABS fixture context: Unity {Application.unityVersion}; target {target}; " +
                $"subtarget {EditorUserBuildSettings.standaloneBuildSubtarget}; " +
                $"graphics {SystemInfo.graphicsDeviceType}; batchMode={Application.isBatchMode}; " +
                $"stripEngineCode={PlayerSettings.stripEngineCode}; " +
                $"managedStripping={PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone)}");
        }

        private static string ReadHeader(string path)
        {
            byte[] header = new byte[Math.Min(16, new FileInfo(path).Length)];
            using (FileStream stream = File.OpenRead(path))
                stream.Read(header, 0, header.Length);
            return string.Concat(header.Select(value => value >= 32 && value <= 126
                ? ((char)value).ToString()
                : $"\\x{value:x2}"));
        }

        private static void Validate(GeneratorConfig config)
        {
            if (config == null) throw new InvalidOperationException("Configuration is empty or invalid JSON.");
            if (config.profile != "smoke" && config.profile != "regression" && config.profile != "stress")
                throw new InvalidOperationException("profile must be smoke, regression, or stress.");
            if (config.bundleCount < 1 || config.bundleCount > 5000)
                throw new InvalidOperationException("bundleCount must be between 1 and 5000.");
            if (config.assetsPerBundle < 1 || config.assetsPerBundle > 10000)
                throw new InvalidOperationException("assetsPerBundle must be between 1 and 10000.");
            if (config.dependencyDensity < 0 || config.dependencyDensity > 1
                || config.duplicateNameFrequency < 0 || config.duplicateNameFrequency > 1)
                throw new InvalidOperationException("Density/frequency values must be between zero and one.");
            if (config.assetTypes == null || config.assetTypes.Length == 0 || config.assetTypes.Any(type => !SupportedTypes.Contains(type)))
                throw new InvalidOperationException("assetTypes contains an unsupported or empty selection.");
        }

        private static void PrepareGeneratedAssets(string profile)
        {
            string path = $"{GeneratedAssetRoot}/{profile}";
            if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
            EnsureAssetFolder(path);
        }

        private static void PrepareOutput(string output)
        {
            string marker = Path.Combine(output, ".uabs-fixture-workspace");
            if (Directory.Exists(output) && !File.Exists(marker) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new InvalidOperationException($"Refusing to overwrite an unmarked output directory: {output}");
            Directory.CreateDirectory(output);
            File.WriteAllText(marker, GeneratorVersion + Environment.NewLine);
            foreach (string path in new[] { Path.Combine(output, "bundles"), Path.Combine(output, "fixture-manifest.json") })
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
                else if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static string AbsoluteAssetPath(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static string RequiredArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, name);
            if (index < 0 || index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]))
                throw new InvalidOperationException($"Missing required command-line argument {name}.");
            return arguments[index + 1];
        }

        private static bool ParseBooleanArgument(string name)
        {
            string value = RequiredArgument(name);
            if (!bool.TryParse(value, out bool result))
                throw new InvalidOperationException($"{name} must be true or false, got '{value}'.");
            return result;
        }

        private static string RequireContained(string candidate, string root, string label)
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedCandidate = Path.GetFullPath(candidate);
            if (!normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{label} must stay inside {normalizedRoot}");
            return normalizedCandidate;
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
