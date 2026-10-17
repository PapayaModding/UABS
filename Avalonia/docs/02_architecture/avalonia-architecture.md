# Avalonia Architecture and Current Status

## Project shape

The solution has three `net9.0` projects:

- `UABS`: Avalonia Views, ViewModels, commands, filesystem service, picker adapter, package workspace, and composition.
- `UABS.UnityAssets`: UI-independent AssetsTools.NET integration, neutral session contracts, package persistence, metadata search, and direct dependency resolution services.
- `UABS.UnityAssets.Tests`: xUnit integration, policy, ViewModel, filesystem, package/search, and dependency-audit tests.

The small `MainWindow` composition root creates the picker, directory service, asset-session factory, and `MainWindowViewModel`. Its code-behind only initializes and disposes the ViewModel.

## Implemented component boundaries

```mermaid
flowchart TD
    Window["MainWindow composition"] --> MainVM["MainWindowViewModel"]
    MainVM --> BrowserVM["BrowserViewModel"]
    MainVM --> ListVM["AssetListViewModel"]
    BrowserVM --> Directory["IDirectoryBrowserService"]
    BrowserVM --> Picker["IFilePickerService"]
    BrowserVM -->|open selected file| MainVM
    MainVM --> Factory["IAssetSessionFactory"]
    ListVM --> Session["IAssetSession"]
    Factory --> Infra["UABS.UnityAssets"]
    Package["IPackageService"] --> Infra
    Package --> Indexes["Legacy-compatible package indexes"]
    Search["IPackageSearchService"] --> Indexes
    Search --> Results["Virtual metadata results"]
    Dependency["IPackageDependencyResolver"] --> Indexes
    Dependency --> DependencyResults["Direct dependency results"]
    PackageVM["PackageManagementViewModel"] --> Package
    EditorVM["PackageEditorViewModel"] --> Package
    EditorVM --> Picker
    MainVM --> EditorVM
    PackageVM --> Search
    PackageVM --> Dependency
    PackageView["PackageManagementView"] --> PackageVM
    Infra --> ATN["AssetsTools.NET"]
    Infra --> Files["Read-only Unity files"]

    Future["Preview, export, reverse lookup, memos"] -. planned .-> MainVM
```

Filesystem browsing and asset-session ownership are deliberately independent. Moving between folders never closes or replaces the active asset session. Opening a file delegates to `MainWindowViewModel.LoadFileAsync`; only a successfully opened candidate replaces the old session.

## Presentation state

`BrowserViewModel` owns current-path state, directory rows, selection, errors, cancellation, and back/forward history. Directory reads are transactional: state and history change only after enumeration succeeds. A new navigation after Back clears the forward branch; refresh does not add a history entry.

`AssetListViewModel` owns the cached source rows, visible immutable snapshot, filters, sort choice, selection, and lazy details. Filtering or sorting performs one in-memory rebuild and one visible-list property change; it never reparses the Unity file. Selection is retained by object identity through sorting, cleared if filtering hides it, and cleared when the session changes.

`MainWindowViewModel` owns the active `IAssetSession`, load status/errors, and application-lifetime cancellation. It composes the two child ViewModels but does not mix directory history into the asset-session lifecycle.

`PackageManagementViewModel` owns a separate package workspace. It loads resilient catalog entries through `IPackageService`, flattens loaded package metadata into source/asset presentation rows, delegates matching to `IPackageSearchService`, and delegates selected-package direct resolution to `IPackageDependencyResolver`. Catalog, search, and dependency requests have independent cancellation/version guards. Selecting another package replaces contents and prevents stale dependency publication without touching the active asset session.

`PackageEditorViewModel` owns an isolated deep clone of an existing package or a new empty root index. It validates the directory-backed package name, adds source/asset references through `IPackageService`, places folder-imported sources in service-provided relative indexes, tracks edit generations and dirty state, requests explicit overwrite/discard decisions, saves a snapshot, and reloads the target before publishing a clean result. Equivalent normal/extended Windows source paths share a UI-independent identity without rewriting stored path text. `MainWindowViewModel` transfers only the current neutral `AssetEntry`; no AssetsTools.NET object crosses into presentation.

## Infrastructure boundary

Public neutral contracts include `AssetEntry`, `AssetDetails`, `AssetSessionMetadata`, `UnityFileKind`, load errors, `IAssetSession`, `IAssetSessionFactory`, and the models behind `IPackageService`, `IPackageSearchService`, and `IPackageDependencyResolver`. Avalonia code never exposes AssetsTools.NET objects. `GetDetailsAsync` returns session-backed metadata without eager full-object deserialization.

Only `UABS.UnityAssets` references AssetsTools.NET. An automated audit checks the built application assemblies' references and shipped top-level DLLs for exact `UnityEngine`, `UnityEngine.*`, `UnityEditor`, or `UnityEditor.*` assembly names.

## Resource ownership and concurrency

- Session opening and enumeration run away from the UI thread.
- Candidate session replacement is transactional; failed candidates cannot destroy the last good result.
- `AssetsToolsAssetSession` owns the manager and streams and has idempotent disposal.
- Directory requests cancel superseded requests and ignore stale completions.
- Detail requests cancel on selection/session changes and use a version check before publishing results.
- The browser does not own or dispose the asset session.
- Package scanning, parsing, and staged persistence use task-based APIs with cancellation and progress; no Avalonia dispatcher is used in infrastructure.
- Folder scans return additive source-relative mappings; transactional editor merge retains existing flat/hierarchical/empty indexes and does not automatically migrate package layout.
- Package replacement rejects implicit overwrite and retains the prior target as a rollback backup during the staged directory swap. The UI requests confirmation before enabling overwrite and reopens the committed package before marking it clean.
- Package search scans only loaded neutral models, supports cancellation, and returns read-only identity snapshots without filesystem access or cache copies.
- Direct dependency resolution builds a temporary CAB lookup from a per-call metadata snapshot, supports cancellation, and returns immutable resolved/unresolved/ambiguous results without recursive traversal or source-file I/O.
- Package catalog enumeration returns per-directory success/error entries so one malformed package does not hide valid siblings. The package workspace never opens referenced Unity files merely to browse, search, or resolve metadata.

## Verification status

- `dotnet build Avalonia.sln --no-restore`: clean, zero warnings/errors.
- Milestone 6 final command evidence is recorded in the [Milestone 06 report](../05_audit/milestone-06-report.md).
- Real fixtures: one compressed Unity bundle and its extracted standalone serialized file.
- Covered: file detection/enumeration, provenance/details, disposal/replacement, filesystem enumeration, history/refresh/failure preservation, numeric sorting, combined filters, selection rules, detail races, browser/session independence, and zero Unity runtime assemblies.
- Not yet fully verified: end-to-end graphical interaction on Windows and a bundle containing multiple serialized files.

See the [Milestone 06 report](../05_audit/milestone-06-report.md), [package system](../04_systems/package-search-dependency-memo.md), and [filesystem browser system](../04_systems/filesystem-browser.md).

## Deferred architecture

Texture/sprite decoding, preview, export, reverse-dependency lookup, memos, structured logging, and platform app-data paths remain unimplemented. The old placeholder `FolderViewEntry`, `FolderViewType`, sort panel, and `AssetsViewEntry` are not part of the active workflow.
