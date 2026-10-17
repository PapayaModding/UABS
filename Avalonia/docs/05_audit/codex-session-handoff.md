# Codex Session Handoff

**Updated:** 2026-10-17

**Current milestone:** Milestone 7.5 complete for approved practical legacy interoperability

**Next task:** Resume paused Milestone 7.4 — application crashes and package editor state loss

**Repository state:** Verified buildable; uncommitted Milestone 7.5 work is preserved; no commit was created

## Priority status

- **Critical A — package format compatibility:** resolved for practical interoperability. Exact Unity writer reproduction is not claimed or required.
- **Critical B — crashes and package editor state loss:** paused and unresolved. Resume this before starting another feature milestone.
- Milestone 7.4 entered its pause with no stability source changes. Source inspection established that `MainWindow` creates one `MainWindowViewModel`, which creates one `PackageManagementViewModel`; tab rendering alone does not prove ViewModel recreation.
- Next exact Milestone 7.4 step: add composition-level reproduction tests for clean, dirty, and active-import editor state across tab/view lifecycle changes, then fix only reproduced crash/state-loss faults.

## Completed Milestone 7.5 phases

1. Audited the genuine Avalonia `OWL` and legacy Unity `OWL_Unity` packages and traced the Unity writer/readers.
2. Chose and implemented practical interoperability: relative index placement for imported supported sources, preservation of existing indexes/strings, no automatic migration, full CAB output, and bounded legacy matching.
3. Added a shared non-I/O package path identity for safe normal/extended Windows drive and UNC variants.
4. Added focused hierarchy, mixed-layout, empty-index, duplicate-index, path-identity, dependency-deduplication, CAB-collision, and save/reopen coverage.
5. Completed a disposable production-service save/fresh-load comparison of the genuine 422-index legacy package.
6. Updated the audit, architecture/system guidance, roadmap, risks, Milestone 3/7 history, open questions, and Milestone 7.5 report.

## Implementation decisions

- `PackageFolderImportResult` retains its original flat `References` and adds optional `PackageFolderImportReference` mappings. Old callers/test doubles continue to map to the root index.
- Recursive/current-folder scans map every successfully imported source to its directory relative to the selected root.
- A new source enters its mapped index. An equivalent existing source remains in its current index and is not moved.
- Existing flat, hierarchical, and empty indexes are preserved. Newly scanned empty directories do not create indexes.
- `PackagePathComparer` handles only safe drive/UNC extended-prefix equivalence plus lexical case/slash/trailing-separator normalization. It performs no filesystem access or link resolution and leaves serialized paths unchanged.
- New CAB values remain complete. Stored truncated values remain unchanged. Dependency resolution accepts exact or a one-character trailing difference and reports multiple physical candidates as ambiguous.

## Files added or modified

Implementation and tests:

- Added `UABS.UnityAssets/Packages/PackagePathComparer.cs`.
- Added `UABS.UnityAssets.Tests/PackagePathComparerTests.cs`.
- Modified package models, folder import service, dependency resolver, package editor ViewModel, and their focused test files.

Documentation:

- Added `docs/05_audit/legacy-package-format-compatibility.md`.
- Added `docs/05_audit/milestone-07-5-compatibility-report.md`.
- Updated AI-agent guidance, project overview, package-system documentation, roadmap, risks, Milestone 3/7 reports, open questions, documentation index, and this handoff.

## Verification

- Baseline: build passed with 0 warnings/errors; tests passed 153/153.
- Focused rebuilt package service/editor/dependency/path tests: 101/101 passed.
- Final `dotnet build Avalonia.sln --no-restore`: passed with 0 warnings/errors.
- Final `dotnet test Avalonia.sln --no-build`: 166/166 passed, 0 failed, 0 skipped.
- `git diff --check`: passed; only line-ending notices were emitted.
- Documentation: 30 Markdown files, 32 Mermaid blocks, 0 missing relative links, and 0 unclosed fences.
- Zero-Unity-runtime dependency audit passed as part of the full test suite.
- Genuine legacy disposable round trip: 422 indexes, 99 empty indexes, 6,671 sources, 175,132 assets, and 15,771 dependencies before and after; 0 missing, extra, or changed identity-keyed records.

## Real-data safety

- Real Avalonia package `E:\Media\UABS_Packages\OWL`: 2 files; final fingerprint `81E67CD6280DCF08754798E90101ACD43FAC4438D8CD8845FD09268F3DC7CC4C`, matching the audit baseline.
- Genuine legacy package `E:\Projects\UABS\UABS\UABS_Cache\OWL_Unity`: 423 files; final fingerprint `CF9BEACF7991B2DD88F5A3C83479B459347070DD6DBC39165770615A9C60631A`, matching the audit baseline.
- The disposable output under `Avalonia\.codex-validation` was removed. Neither real package was a save target.
- The legacy Unity source was not modified by this milestone.

## Working-tree warnings

- Preserve all uncommitted Avalonia changes listed above.
- Four pre-existing legacy Unity changes remain out of scope and must not be touched or staged with this milestone:
  - `UABS/Packages/manifest.json`
  - `UABS/Packages/packages-lock.json`
  - `UABS/ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json`
  - `UABS/ProjectSettings/ProjectVersion.txt`
- No reset, clean, revert, checkout, branch switch, commit, package migration, or write to the real package directories is authorized.

## Remaining limitations and decisions

- The Unity desktop application was not run against newly generated Avalonia output; automated/source-based interoperability evidence is not a Unity UI acceptance test.
- New empty directories are not emitted. Exact `\\?\` path output, legacy CAB truncation, and extension-only filtering are intentionally not reproduced.
- Absolute package paths remain installation-specific; path remapping is not implemented.
- UE-008 memo identity/save/recovery policy remains an explicit product decision.
- Exact legacy export and a separate versioned modern format remain optional future decisions.

## Safe continuation

Read `docs/01_core/ai-agent.md`, this handoff, and `docs/05_audit/milestone-07-5-compatibility-report.md`; inspect the working tree; rerun the build/tests if the tree changed; then resume only Milestone 7.4 using the exact composition-level tests above. Do not restart Milestone 7.5 or begin Milestone 8.
