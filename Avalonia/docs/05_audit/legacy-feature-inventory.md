# Legacy Feature Inventory

Status describes source completeness, not fresh runtime verification.

| Feature | Legacy implementation | Observed status | Notes |
|---|---|---|---|
| Open Unity file | `DropdownOptions/Opener/OpenFile.cs`, `BundleReader`, `AssetsReader` | Mostly implemented | Accepts arbitrary file selection, then detects bundle/assets format |
| Open folder | `OpenFolder.cs`, `FolderDataPathManager`, `ReadFolderContent` | Mostly implemented | Shows folders and `.bundle`/`.ab`/`.assets` only |
| File-type detection | `UnityFile/FileTypeDetector.cs` | Implemented | Header/metadata heuristic, reusable |
| Bundle decompression/loading | `UnityFile/NextInstance.cs` | Implemented with risks | Whole-bundle memory decompression; missing error/disposal policy |
| Multi-assets-file enumeration | `AssetsReader.cs` | Implemented | Aggregates entries across loaded assets files |
| Asset display names/types | `NextInstance.GetDisplayNameFast` | Implemented with fallbacks | Type-tree/class-database and MonoBehaviour paths |
| Folder navigation/back | `FolderDataSource` managers | Implemented | Path stack intertwined with derived cache folders |
| Asset table/virtual scrolling | `LoadEntries2ScrollView`, `EntryInfoView` | Implemented in Unity UI | Manual fixed-size pool |
| Sorting | `AssetsDataSortManager` | Implemented | Natural name, type, path ID |
| Type filtering | `AssetsDataFilterManager`, filter controls | Implemented | `AssetClassID` dictionary state |
| Selection | `AssetsDataSelectionManager` | Implemented | Single/Ctrl/Shift plus next/previous |
| Metadata panel | `DisplayInfoPanel`, `AssetReader.ReadExtraInfoFromAsset` | Implemented | Name/path ID/file ID/path/size |
| Texture2D preview | `ImageReader`, `TextureDisplayForBundle` | Mostly implemented | Format-dependent; exceptions and unsupported cases remain |
| Sprite/atlas preview | `DumpReader`, `AtlasDumpProcessor`, `ImageReader` | Mostly implemented | Complex pointer/index assumptions |
| Pan/zoom preview | `PanZoomRawImage` | Implemented in Unity UI | Rewrite for Avalonia interaction |
| PNG export | `ExportTextureAsImage2Path` | Partially implemented | Export kind `Dump` exists in model but has no handler; filename collisions unsafeguarded |
| Build package index | `UserPackageBuildReader`, `WriteUserPackage` | Mostly implemented | Indexes only Sprite/Texture2D assets; threading contract is flawed |
| Select/remove packages | `ReadUserPackage`, user-package controls | Implemented | Uses relative `External/UABS_UserPackages` |
| Keyword/exclusion search | `ReadSearchInfo`, `FindDeriveInfoInPackage` | Implemented | OR include terms; exclusions win |
| Memo search | same plus `SearchBundleByMemosButton` | Implemented | Searches asset names and memo text |
| “Image search” | `SearchBundleByImageButton` | Misleading/limited | Exact filename only, not image content |
| Dependency lookup | `ReadDependencyInfo`, `DependencyDataSource` | Implemented | Requires a compatible package index |
| Reverse dependent lookup | `DependentReader` | Library path present | Production UI integration not established |
| Search/dependency result browsing | `CopyDeriveToSysFolder`, derive events | Implemented with redesign need | Copies full bundles into temporary folders |
| Per-asset memo read/write | `MemoReader`, `MemoWriter`, `MemoField` | Implemented | Sprite/Texture2D only; bundle path + asset name identity |
| Memo inheritance | `InheritMemoWriter` | Implemented | Safe/Overwrite/Force, heuristic bundle identity |
| Cache cleanup | `ClearSystemCache`, `CacheCleaner` | Implemented | Recursive delete of relative cache directories |
| Logging/status | Unity `Debug`, `AppStateTracker` | Basic | No durable structured error model |
| Tests | `Assets/Script/__Test__` | Ad hoc | Scene/coroutine tests, external fixtures, not standard automated suite |

## Primary user workflows

### Inspect a file

Open file → detect/load/decompress → enumerate assets → switch to bundle view → sort/filter/select → lazily show metadata → decode preview when supported → optionally export PNG.

### Browse a folder

Open folder → list child folders/readable Unity files → open nested directory or file → record path history → back unloads all AssetsTools.NET state and restores prior location.

### Build and use a package

Choose game data folder and package destination → scan directory tree → parse each Unity file → write JSON indexes → select package(s) for search/dependency resolution → browse copied result bundles.

### Annotate and carry memos forward

Select one memo package → select a Sprite/Texture2D → load memo → debounce writes to package JSON → optionally inherit memos from an older package to a new one under one of three overwrite modes.

## Known incomplete or misleading legacy areas

- `ExportKind.Dump` is modeled but `AssetsDataExportManager` only handles `ExportKind.Asset`.
- “Search by image” uses only the selected image filename.
- `AssetContainer` has an empty constructor and no operational use.
- `App.StartCoreSystem()` is empty.
- `DependentReader` exists but its production workflow is unclear.
- `ReadFolderContent` ignores extensionless bundles and most Unity data layouts.
- Tests are not CI-ready and runtime state has not been freshly verified.
