# Migration Risks and Technical Debt

## Critical/early risks

### Legacy package creation layout — mitigated for practical interoperability

A read-only comparison of the real Avalonia `OWL` and Unity `OWL_Unity` packages found identical source, asset, and dependency populations but different persistence layouts. Milestone 7.5 now carries source-relative directories through recursive editor imports, preserves all existing indexes and serialized strings, and treats safe normal/extended Windows path spellings as one internal identity. New empty directories are not materialized, flat packages are not migrated, full CABs remain canonical, and exact Unity writer reproduction remains out of scope. A disposable save/fresh-load of the genuine 422-index package found zero record differences. See [Legacy package format compatibility](legacy-package-format-compatibility.md) and [Milestone 7.5 report](milestone-07-5-compatibility-report.md).

### Dependency resolution was broken — mitigated for Milestone 1

The missing `..\Libs` references were removed. Core `AssetsTools.NET.dll` and `classdata.tpk` now live in declared repository locations and are copied to output. Cpp2IL, MonoCecil, texture codecs, and native ASTC remain deliberately deferred and are still risks for their future milestones.

### Resource ownership and memory pressure

`NextInstance` opens streams without a clear disposal boundary and can decompress an entire bundle into memory. `AssetsManager.UnloadAll()` is the legacy coarse cleanup mechanism. Avalonia needs disposable sessions, bounded caches, cancellation, and progress. Large parsing/decoding must never run on the UI thread.

### Texture support is partly provided by Unity

BCn has a reusable decoder and ASTC has a native function, but ETC/ETC2/RGBA4444 currently pass through Unity `Texture2D`. A non-Unity replacement is required. Sprite atlas pointer/index assumptions also need fixture coverage.

### Class database and MonoBehaviour templates

`classdata.tpk`, MonoCecil, and Cpp2IL must be packaged and located reliably. `NextInstance._hasSetMonoTempGenerators` is static: one failed or completed initialization can incorrectly affect later files/sessions from different games.

## Correctness risks found in source

- `AssetsReadEvent` and `AssetsRead4DeriveEvent` assume `assetsInsts` is non-null; `AssetsReader` can return `(null, null)` for unknown files.
- `TextureDisplayForBundle` uses a default `AssetImageInfo` after decode failure, then dereferences `textureWithMeta.rect`; behavior is misleading and can cascade.
- `ImageReader.GetExternalAsset()` reads `targetFile.file` before checking whether `targetFile` is null.
- `ImageReader`'s fallback `imageBytes.Length == width * height * format` treats enum numeric value as bytes per pixel, which is not generally valid.
- `ImageReader.IsAstcFormat()` repeats non-HDR 10x10/12x12 at the end, apparently omitting HDR 10x10/12x12 in the predicate even though `GetBlock()` handles them.
- export uses asset name directly as a filename: invalid characters, duplicates, unnamed assets, and overwrite behavior are not handled.
- The replacement package reader tolerates sources with no `AssetBundle` object; broad version/game fixture coverage is still missing.
- `ReadDependencyInfo` makes the same first-item assumption.
- CAB scanning reads the entire bundle into memory and returns a substring with `end - start - 1`, potentially truncating the CAB token.
- exact search indexes `sKeys[0]` without validating that any keyword exists.
- Legacy `InheritMemoWriter.GetIdentifyNameFromItemName()` slices at the last underscore without handling names that have none. Milestone 8 preserves the heuristic but safely uses the complete stem when no underscore exists.
- Legacy memo identity is bundle path + asset name and silently takes first matches. Milestone 8 direct editing adds package/index/Path-ID context in memory and refuses duplicate complete identities; moved paths and schema-level logical-file ambiguity remain limitations.
- path containment uses string `StartsWith`, which can confuse sibling prefixes (for example `Cache` and `Cache2`).
- Windows `GetLongPathName` P/Invoke and `\\?\` prefixing make the legacy browser adapter platform-specific.
- UE-007 is replaced with an awaitable task/cancellation/progress workflow. Large scans are sequential and not resumable.
- package directories are staged and swapped with a rollback backup, but multi-file directory replacement is not one filesystem-level atomic operation. In-process writers now serialize by normalized package path; external processes remain outside that lock, and stale-document conflicts still require an explicit policy.
- the replacement search service aggregates selected packages deterministically, but no large-catalog benchmark or UI paging strategy exists yet.
- direct dependency resolution deliberately accepts only exact CAB matches or the verified one-character legacy truncation variant; malformed identifiers remain unresolved instead of using the legacy broad prefix match.
- duplicate package records with the same normalized physical source path collapse to one candidate, but records with missing paths cannot be safely deduplicated and may produce ambiguity.
- package catalog enumeration now isolates malformed directories, but very large catalogs are still loaded sequentially and have no paging or benchmark.
- package creation/save now uses distinct source/destination selection, explicit overwrite confirmation, staged persistence, and verified reopening; package removal remains absent because the legacy recursive delete is unsafe.
- package rediscovery now accepts a catalog root or an individual package directory, and source import uses verified file headers rather than extensions. Folder import is transactionally published and can explicitly recurse without following reparse points, but large-tree sequential scanning has no performance benchmark and native access-denied behavior remains platform-dependent.
- real-world Milestone 7.3 validation loaded 15 nested bundles and 1,173 eligible references through the production service/ViewModel path without source copies or package writes. Current-folder scope and the editor-only-before-Save boundary are now explicit, but the exact native picker/click/Save workflow still needs a desktop-capable observation.
- Milestone 7.4 prevents transient package-grid deselection from sharing the explicit abandon path, observes UI async-command and package-selection failures, clears cancellation owners safely, and suppresses stale asset-load publication. Historical intermittent crashes lack a crash dump, so these fixes do not prove that every possible crash is eliminated.

## Avalonia prototype risks

- the new session/ViewModel boundary covers only the Open File vertical slice; future features must continue using it instead of accumulating in code-behind.
- the prior code-generated dropdown and visual-tree lookup were removed; standard `MenuFlyout` command binding is now used.
- toolbar buttons imply features that are not wired.
- `Printer` previously leaked the new-file stream before append; Milestone 7.4 replaces it with serialized, handle-safe writes and stack-trace exception logging. Logs still reset each launch and do not rotate.
- image converter caches never dispose bitmaps and assume every generated icon URI exists.
- relative paths depend on working directory.
- errors are surfaced in the browser/asset pages and 30 automated tests cover the current scope, but complete interactive picker/navigation/DataGrid behavior still needs manual verification.

## Milestone 2 residual risks

- The real fixtures cover one bundle with one loadable serialized file and the corresponding standalone `.assets`; multi-file bundles, encrypted bundles, and very large/compressed bundles are not yet in the automated matrix.
- Compressed bundles are unpacked into a `MemoryStream`; this is correct for the narrow slice but can create high peak memory on large files.
- Fast-name extraction intentionally recognizes common `m_Name`, GameObject, and MonoBehaviour layouts and falls back to `Unnamed asset`; it is not full deserialization.
- Class database selection is best effort. Unknown Unity versions remain enumerable where type trees exist but may have reduced type/name quality.
- The application smoke-launched without an immediate runtime/XAML failure, but the file-picker-to-grid path was not manually clicked because native desktop automation was unavailable.
- Directory enumeration materializes one snapshot and asset criteria rebuild one array; performance and cancellation should be measured with unusually large directories and asset lists.
- Browser history is process-local and not persisted; symlink/junction cycles are navigable but not recursively traversed.

## Product/data migration risks

- Existing package JSON schema has no explicit version and stores absolute source paths.
- “image search” behavior may not match user expectations.
- Search semantics are OR-based with exclusions taking precedence; changing this silently would be a regression.
- Memo Safe/Overwrite/Force semantics and the bundle-name heuristic are characterized and retained. Duplicate heuristic matches are now visible conflicts rather than filesystem-order choices, which is a deliberate safety difference from Unity.
- The package schema has no package/directory/source memo field or copy provenance; adding either requires a versioned format decision rather than silent legacy JSON changes.
- It is unclear which Unity texture formats/games/platforms are required, so decoder scope cannot yet be finalized.
- Copying search/dependency result bundles may have accidentally provided isolation from source files; direct-reference replacement should remain read-only.
- Package models are mutable; search and dependency services snapshot relevant values per call, but concurrent mutation during snapshot creation is not coordinated.
- Package workspace operations cancel stale requests, but native file/folder picker behavior, keyboard traversal, narrow-window layout, and high-DPI presentation require manual platform verification.
- Avalonia writer/reader round trips do not constitute a Unity UI acceptance test. Source-relative editor placement and genuine legacy save/reload preservation are automated and data-verified, but the unchanged Unity application has not been executed against new Avalonia output.

## Mitigations

1. Freeze representative fixture files and existing package JSON examples before implementation.
2. Add characterization tests around legacy-schema algorithms and explicitly approved compatibility behavior.
3. Introduce disposable asset sessions and neutral DTOs first.
4. Ship decoding format-by-format with capability reporting; do not silently show incorrect images.
5. Make all source game data read-only and all writes atomic and limited to explicit app-data/package/export locations.
6. Add structured operation results/logging and surface failures in the UI.
