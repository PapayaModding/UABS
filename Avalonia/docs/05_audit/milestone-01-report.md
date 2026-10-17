# Milestone 01 Report — Functional Asset Loading

Date: 2026-10-16

## Objective

Prove the replacement architecture with one real end-to-end slice: select an existing Unity file, detect its format, load it through AssetsTools.NET outside the UI layer, enumerate real assets, and present their identity and provenance in an Avalonia DataGrid without modifying the source file.

## Implementation summary

The Avalonia solution now has a dedicated `UABS.UnityAssets` infrastructure project and `UABS.UnityAssets.Tests` test project. The desktop project composes a file-picker adapter, asset-session factory, and `MainWindowViewModel`. The former hard-coded rows are gone; the active grid is fed only by neutral records returned from a successful AssetsTools.NET session.

Supported detector/loader paths are UnityFS/UnityRaw/UnityWeb bundles and Unity serialized files recognized by their headers. Bundle enumeration preserves each contained serialized file's logical name. Rows expose best-effort name and type plus exact class ID, original path ID, byte size, logical file, and original source path.

Unsupported, missing, truncated, or otherwise invalid files return classified errors rather than crashing the window. Missing classdata is a non-fatal warning. A replacement is opened completely before the old session is disposed, so a failed replacement leaves the last good asset list visible.

## Architecture introduced

```text
Avalonia View/MenuFlyout
  -> MainWindowViewModel + AsyncCommand
  -> IFilePickerService
  -> IAssetSessionFactory
  -> UnityFileDetector
  -> AssetsToolsAssetSessionFactory
  -> AssetsTools.NET
  -> IAssetSession / AssetEntry
  -> ObservableCollection
  -> DataGrid
```

`IAssetSession` is the resource boundary. Its implementation owns `AssetsManager`, source streams, and any in-memory unpacked bundle stream. Both failed-open cleanup and normal disposal are best effort and idempotent. No UnityEngine or Avalonia type crosses the infrastructure public contract.

## Dependency setup

- Vendored only `AssetsTools.NET.dll` version 3.0.0.0 (224,256 bytes) under `Dependencies/AssetsTools.NET`.
- Removed direct, broken UI references to nonexistent `..\Libs` paths.
- Added a project reference from the desktop project to `UABS.UnityAssets`.
- Vendored `classdata.tpk` (286,635 bytes) under `UABS.UnityAssets/Resources` with deterministic output copying.
- Located classdata through `AppContext.BaseDirectory/Resources/classdata.tpk`.
- Deferred Cpp2IL, MonoCecil, ImageSharp, BCn, ASTC, and other Unity plugin binaries because this milestone does not use them.

## Significant files added

- `UABS.UnityAssets/UABS.UnityAssets.csproj`
- `UABS.UnityAssets/AssetEntry.cs`
- `UABS.UnityAssets/AssetSessionMetadata.cs`
- `UABS.UnityAssets/AssetLoadException.cs`
- `UABS.UnityAssets/UnityFileDetector.cs`
- `UABS.UnityAssets/IAssetSession.cs`
- `UABS.UnityAssets/IAssetSessionFactory.cs`
- `UABS.UnityAssets/AssetsToolsAssetSession.cs`
- `UABS.UnityAssets/AssetsToolsAssetSessionFactory.cs`
- `UABS/Script/Services/IFilePickerService.cs`
- `UABS/Script/Services/AvaloniaFilePickerService.cs`
- `UABS/Script/ViewModels/AsyncCommand.cs`
- `UABS/Script/ViewModels/MainWindowViewModel.cs`
- `UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj`
- `UABS.UnityAssets.Tests/UnityFileDetectorTests.cs`
- `UABS.UnityAssets.Tests/AssetSessionTests.cs`
- `UABS.UnityAssets.Tests/MainWindowViewModelTests.cs`
- `UABS.UnityAssets.Tests/Fixtures/aigirl.ab`

## Significant files modified

- `Avalonia.sln`
- `UABS/UABS.csproj`
- `UABS/Script/MainWindow.axaml` and `.cs`
- `UABS/Script/View/Options/Toolbar.axaml` and `.cs`
- `UABS/Script/View/Pages/FolderView.axaml` and `.cs`
- `UABS/Script/Data/AssetTypeIconConvertor.cs`
- migration documentation listed below

The legacy Unity repository was not modified.

## Verification

Environment: .NET SDK 10.0.401 building `net9.0`; .NET runtime 9.0.5 available.

Final build command:

```powershell
dotnet build E:\Projects\UABS\Avalonia\Avalonia.sln
```

Result: succeeded, 0 warnings, 0 errors; restore reported all projects up to date.

Final test command:

```powershell
dotnet test E:\Projects\UABS\Avalonia\UABS.UnityAssets.Tests\UABS.UnityAssets.Tests.csproj --no-build --no-restore --logger "console;verbosity=normal"
```

Result: 14 passed, 0 failed. Covered:

- representative bundle detection and invalid/missing detection;
- valid real-bundle open and asset enumeration;
- nonempty name/type/logical file, stable original path IDs, byte sizes, and source provenance;
- unsupported and nonexistent inputs;
- missing classdata as a non-fatal warning;
- idempotent disposal and exclusive source-file reopen;
- repeated open/close;
- successful ViewModel replacement disposing the prior session;
- failed replacement retaining the prior session and showing an error;
- empty asset lists;
- application/ViewModel close disposing the current session.

The application executable was launched and remained running without an immediate XAML/runtime failure. Native desktop automation was unavailable, so the file picker was not clicked and the rendered DataGrid was not visually inspected. This is a smoke launch, not manual end-to-end UI verification.

During verification, one build failed only because the smoke-launched `UABS.exe` held its own output file open. After stopping that process, the final build and test commands above passed cleanly; this was an environment/process lock, not a code failure.

A separate `dotnet build -t:Rebuild` probe was blocked by the managed sandbox when MSBuild attempted to recreate a solution-root `obj` directory. The standard solution build—the supported verification command recorded above—ran outside that probe and passed repeatedly with zero warnings/errors.

## Remaining issues and coverage gaps

- The included 17,353-byte fixture is a real bundle with one loadable serialized file. No suitable small standalone `.assets` or multi-file bundle fixture was included, so those branches are compiled but not fixture-verified.
- Compressed bundles currently unpack into memory, matching a simple proven AssetsTools path but potentially using substantial memory for large inputs.
- Fast name extraction is intentionally best effort; unfamiliar layouts display `Unnamed asset` rather than triggering expensive general deserialization.
- MonoBehaviour template resolution from managed/IL2CPP game data is deferred.
- Picker/DataGrid interaction still needs one manual Windows pass.
- Folder browsing/history, sort/filter commands, details, preview, texture decode, export, package/search/dependency, memo, and unrelated toolbar actions are deferred.

## Documentation updated

The Milestone 1 session updated the roadmap, feature comparison, Avalonia architecture, dependency analysis, migration risks, handoff, index, and this report. Those documents were subsequently organized under the current `docs/01_core`, `docs/02_architecture`, and `docs/05_audit` structure; use the root [documentation index](../README.md) for their current paths.

## Recommended next milestone

Complete Phase 2 before texture work: implement filesystem browsing and back history, sort/filter/selection behavior, and a cancellable lazy metadata-details panel. Add standalone serialized-file and multi-file bundle fixtures so both loader paths and provenance across multiple contained files are proven. Texture decoding and export should remain a later, independently testable milestone.
