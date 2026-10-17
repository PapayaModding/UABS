# Unity vs. Avalonia Feature Comparison

Legend: **Complete** means implemented in the target for the stated scope and verified. “Partial” means a usable vertical slice exists but legacy breadth is intentionally deferred.

| Subsystem | Unity | Avalonia | Migration status | Recommendation |
|---|---|---|---|---|
| App startup/composition | `App` singleton + scene discovery | `MainWindow` composes picker, session factory, and ViewModel | Partial | Move composition to `App` only if more long-lived services justify it |
| Event/state coordination | synchronous `EventDispatcher`, `AppStateTracker`, UnityEvents | none | Not Migrated | ViewModel state + commands; small typed notifications only where useful |
| File/folder picker | StandaloneFileBrowser wrapper | `IFilePickerService` + Avalonia `StorageProvider` file/folder pickers | Implemented | Retain cancellation-safe adapter boundary |
| Folder model/list | `FolderViewInfo`, `ReadFolderContent`, prefab list | `BrowserEntry`, `DirectorySnapshot`, `BrowserView` | Implemented | Measure very large-directory behavior |
| Folder navigation/history | three folder managers | `BrowserViewModel` with back/forward/up/refresh | Implemented | Add platform permission matrix |
| File detection | `FileTypeDetector` | `UnityFileDetector` in `UABS.UnityAssets` | Implemented; bundle and serialized tested | Broaden version/malformed fixture matrix |
| Bundle/assets loading | `NextInstance`, `AssetsReader`, `BundleReader` | disposable `AssetsToolsAssetSessionFactory` | Implemented for bundle/serialized files | Broaden fixture matrix and large-file testing |
| Asset list row | `AssetEntryInfo`, `EntryInfoView` | neutral `AssetEntry` bound directly to DataGrid | Milestone 1 Complete | Add row VM only when row-specific behavior warrants it |
| Icon resources | ScriptableObjects + Unity sprites | copied PNG resources + converters | Partially Migrated | Keep Avalonia resource approach, add fallback/existence tests |
| Sorting/filtering | managers and toolbar controls | cached filters and selectable text/numeric sort in `AssetListViewModel` | Implemented | Profile very large lists before adding complexity |
| Selection/navigation | selection manager + Unity input | DataGrid selection with instance-based retention/clearing rules | Implemented core | Multi-select and keyboard range semantics intentionally deferred |
| Asset metadata | `AssetReader`, `DisplayInfoPanel` | lazy `AssetDetails` panel with entry and session metadata | Implemented core | Extend only when later features need deeper fields |
| Texture/sprite decoding | `ImageReader`, BCn, ASTC, Unity codecs | decoder DLLs copied, no code | Needs Redesign | Neutral RGBA/image pipeline; verify each format |
| Preview pan/zoom | `RawImage`, `PanZoomRawImage` | none | Not Migrated | Avalonia control/behaviors after decoder milestone |
| PNG export | Unity `EncodeToPNG` | Export button only | Needs Redesign | ImageSharp encoding + safe naming/collision policy |
| Package index build | package reader/writer + raw Thread | `IPackageService` with neutral models, Task/cancellation/progress, real fixture scanning | Implemented backend | Add broader production fixtures and performance measurements |
| Package management | package list/remove controls | resilient catalog plus first-package creation, isolated editing, source/asset add/remove, explicit overwrite/discard, staged save verification, and reopen | Implemented basic workflow | Broaden production fixtures and retain safe confirmation boundaries; removal remains deferred |
| Keyword/memo search | recursive JSON scan + controls + cache copying | in-memory `IPackageSearchService` with package/source/asset UI and provenance | Implemented | Measure large catalogs before paging/indexing |
| Image search | exact filename match | exact asset-name metadata search available; no image-content search | Behavior clarified | Present as filename/asset-name search, never image similarity |
| Dependency lookup | AssetsTools + recursive package JSON scan | in-memory direct resolver plus selected-package/source inspection UI showing all candidates | Implemented direct workflow | Decide reverse lookup separately; keep original files read-only |
| Reverse dependents | reader exists, UI unclear | none | Uncertain | Confirm product need before porting |
| Memo editing | JSON read/write + coroutine debounce | Memo button only | Not Migrated | Stable identity, async save/debounce, atomic writes |
| Memo inheritance | three-mode writer + controls | none | Reusable with fixes | Port into package service with validation/tests |
| Temporary result cache | copies whole bundles | none | Should Not Be Migrated | Virtual result references; optional explicit cache only |
| Logging/errors | `Debug.Log` | static text-file `Printer` | Needs Redesign | `Microsoft.Extensions.Logging` and user-facing operation state |
| Settings | relative constants | relative log/icon constants | Not Migrated | options + platform app-data paths |
| Automated tests | custom scene runner | asset/browser/package/search/direct-dependency/editor xUnit tests, real Unity fixtures, legacy-format package fixture, Unity dependency audit | Milestone 7 automated scope verified | Add production package, multi-file bundle, large-catalog performance, and manual UI breadth |

## Name/behavior correspondences

| Unity concept | Avalonia artifact | Assessment |
|---|---|---|
| `FolderViewInfo` / `FolderViewType` | `FolderViewEntry` / `FolderViewType` | Same presentation concept, but Avalonia lacks `Path`, `Size`, bundle distinction, and derived/original path metadata |
| `EntryInfoView` + `AssetEntryInfo` | `AssetsViewEntry` | Same row concept: name, `AssetClassID`, type label |
| `AssetType2IconData` | `AssetTypeIconConvertor` | Avalonia correctly replaces ScriptableObject mapping with resource convention, though missing-icon fallback is needed |
| `FolderViewType2IconData` | `FolderViewIconConvertor` | Same purpose; current file mapping is incomplete |
| Unity top option buttons | `Toolbar` button labels | Visual names copied, behavior absent |
| Unity `Segue`/page GameObjects | `MainContent` | A suitable host exists, but no navigation state or views beyond folder prototype |
| `LoadEntries2ScrollView` | Avalonia `DataGrid` | Native binding/virtualization can replace manual pooling |

Bundle/serialized-file detection, loading, enumeration, session ownership, package infrastructure, metadata search, direct dependency resolution, read-only package inspection, and basic package creation/edit/save UI are migrated. Reverse-dependency lookup, memo persistence, texture, preview, and export remain deferred by design.
