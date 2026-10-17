# Reusable Code Map

Paths below are relative to `E:\Projects\UABS\UABS` unless otherwise stated.

## Direct or near-direct reuse

| Source | Namespace/type | Reuse notes |
|---|---|---|
| `Assets/Script/UnityFile/FileTypeDetector.cs` | `UABS.Assets.Script.DataStruct._New.FileTypeDetector` | Move to infrastructure; AssetsTools.NET only |
| `Assets/Script/DataStruct/_New/FileInstanceLike.cs` | `FileInstanceLike` | Prefer a discriminated result/record, but code is framework-neutral |
| `Assets/Script/Misc/Streams/FormatFileSize.cs` | `FileSize` | Pure formatting utility |
| `Assets/Script/Wrapper/TextureDecoder/*` | `ITextureDecoder`, `BcDecoderWrapper`, enums/wrappers | BCn decoder layer is already abstracted from Unity |
| `Assets/Script/Wrapper/Json/*` | `IJsonSerializer`, `IJsonObject`, Newtonsoft implementations | Useful for package schema compatibility; clean nullability |
| `Assets/Script/Reader/DumpProcessor/JsonDumper.cs` | `JsonDumper` | AssetsTools.NET + Newtonsoft only |
| `Assets/Script/Reader/BundlesRead/AssetParser.cs` | `AssetParser` | Thin AssetsTools.NET mapper |
| `Assets/Script/DataStruct/*` except `AssetImageInfo` | records/enums | Convert mutable public fields to immutable/nullable-aware records where practical |

## Reuse algorithms after light adaptation

| Source | Reusable behavior | Required changes |
|---|---|---|
| `UnityFile/NextInstance.cs` | detect/load bundle/assets, class DB, fast names, Mono/IL2CPP templates | logger, disposal, session-scoped state, error/cancellation contract |
| `Reader/BundlesRead/AssetsReader.cs` | aggregate asset entries | logger, null-safe results, ownership |
| `Reader/BundlesRead/AssetReader.cs` | names and extra metadata | clarify class DB state/thread safety |
| `Reader/BundlesRead/ReadFolderContent.cs` | directory/readable-file enumeration | async option, extension policy, avoid parsing bundle merely for size in list path |
| `DataSource/Manager/AssetsDataSortManager.cs` | natural/type/path sorting | extract pure comparer/service; define null-name behavior |
| `DataSource/Manager/AssetsDataFilterManager.cs` | type filtering | pure predicate/ViewModel collection view |
| `DataSource/Manager/AssetsDataSelectionManager.cs` | selection behavior | remove dispatcher/Mathf/Debug and test range semantics |
| `Reader/Search/ReadSearchInfo.cs` | English/Chinese comma splitting | quote/escape policy for commas remains open |
| `Reader/Search/FindDeriveInfoInPackage.cs` | search and CAB lookup | load index once, validate exact-search empty input, structured failures |
| `Reader/Search/ReadDependencyInfo.cs` | dependency extraction | session service and unresolved-dependency result |
| `Reader/BundlesRead/DependentReader.cs` | reverse dependency query | confirm product integration |
| `Reader/UserPackage/UserPackageBuildReader.cs` | package schema/build rules | async/cancellation/progress, safe bundle handling, schema/version model |
| `Reader/UserPackage/MemoReader.cs` | memo lookup | repository abstraction, stable key strategy |
| `Writer/UserPackage/MemoWriter.cs` | memo update | atomic write, concurrency, stable key |
| `Writer/UserPackage/InheritMemoWriter.cs` | three inheritance modes | validate bundle-name heuristic and missing underscore |

## Reuse only as specification

- `ImageReader.cs`: reuse AssetsTools.NET field/pointer traversal and format mapping as behavioral evidence, not its Unity texture implementation.
- `AtlasDumpProcessor.cs`: mapping logic is valuable, but replace `UnityEngine.Rect` with a neutral rectangle and test malformed/missing atlas links.
- `ExportTextureAsImage2Path.cs`: preserve export selection/folder intent; redesign encoding, sanitization, collisions, progress, and cancellation.
- `DataSource` managers and event classes: preserve workflow semantics, not the global event-driven structure.
- Unity UI/controllers/views: use them as screen/interaction requirements only.

## Existing Avalonia assets worth keeping

Paths below are relative to `E:\Projects\UABS\Avalonia\UABS`.

- `UI/Icons/*.png` and fonts: reusable presentation resources.
- `Script/Data/AssetTypeIconConvertor.cs`: useful convention-based mapping after adding safe fallback/disposal policy.
- `Script/Data/FolderViewIconConvertor.cs`: useful with correct file/bundle mapping.
- `Script/Data/DataStruct/AssetsViewEntry.cs` and `FolderViewEntry.cs`: prototypes for item ViewModels, but expand/recast rather than treating them as domain models.
- `Script/MainWindow.axaml`, `FolderView.axaml`, and styles: reusable visual starting points after binding to real ViewModels.

## Suggested destination mapping

| Legacy class | Suggested destination |
|---|---|
| `FileTypeDetector`, `NextInstance`, `AssetsReader`, `AssetReader` | `UABS.UnityAssets` infrastructure library |
| `ParsedAssetAndEntry` | split into public `AssetDescriptor` and internal session handle |
| search/package/memo classes | `UABS.Application` services with persistence adapters in infrastructure |
| sort/filter/selection rules | `UABS.Core` pure functions/models or ViewModel collection behavior |
| `ImageReader` | `UnityTextureDataReader` + `TextureDecoder` + `SpriteExtractor` |
| `ExportTextureAsImage2Path` | `AssetExportService` |
| `AppEnvironment` | explicit service registration/composition in Avalonia `App` |
