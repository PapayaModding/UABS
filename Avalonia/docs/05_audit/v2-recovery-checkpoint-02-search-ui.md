# V2 Recovery Checkpoint 2 — Bundle Search UI

**Date:** 2026-10-23. **Entry baseline:** `2b345c7` (clean index/worktree).
**Scope:** Search-only integration of the verified Checkpoint 1/1.1 backend. Automated verification complete; user-operated Debug acceptance pending. Recovery issue #1 is not resolved.

## Implemented boundary

- The production `MainWindowViewModel` explicitly selects bundle projection in its long-lived Search ViewModel. The existing optional-constructor default and raw asset-search operation remain available for established programmatic callers; there is no user-facing mode switch. V1 queries retain their existing presentation and native sorting.
- `V2SearchOperation.OpenBundleAsync` validates the unmodified requested fields/kinds, admits exact registrations, then creates the verified bundle session. Its engine-native source-session adapter reuses existing paging, revision checks, cancellation, count and bounded-window ownership; no grouping of visible asset rows occurs.
- V2 Search displays exactly **Name / Package**. Other existing columns are hidden only in this format-aware projection, not deleted or restyled. Both headers toggle the approved global ordering before keyset pagination; immutable tie-breakers remain backend-owned.
- Selection retains the exact package/generation/source/revision key and registration vector, including identical display names across packages. Sorting the same frozen query retains the selected source even outside the current window. Pending criteria are not executed by a sort.
- The selectable Inspector shows source name, package/location, original indexed path, index directory, source identity/revision, registration/location/catalog evidence, index status and match categories. It explicitly describes source metadata, not a contained asset. Asset-only fields and memo editing are hidden for bundle rows: no child memo is presented as bundle-owned.
- The existing 600-row retention, 128-checkpoint implementation, backward replay, late-result rejection, navigation draft flushing and explicit D5 Refresh workflow are reused. Catalog/memo changes do not rerun Search or silently retarget selections. No Go to Assets action was added.

## Supported controls and explicit limitations

Controls, layout, defaults and include/exclude parsing are unchanged. P1-D1–P1-D15 and the [Checkpoint 1.1 compatibility matrix](v2-recovery-checkpoint-01-1-search-compatibility.md) remain authoritative.

| Existing configuration | Bundle behavior |
| --- | --- |
| Sources only; Assets text fields | Asset-name evidence aggregates to one source occurrence; Search memos adds asset-memo evidence. |
| Sources only; CAB codes or Dependencies | Positive source-field matching is supported. |
| Path ID, Class ID, type and Memo only | Mandatory contained-asset witnesses; different children may satisfy separate gates. No inferred asset identity. |
| Asset-only selected text fields plus exclusions | Any child matching the selected exclusion fields vetoes the bundle. |
| Names, Paths or All field group | Explicit error: these groups include package fields without an approved bundle-membership mapping. |
| Packages/Assets kinds, alone or mixed with Sources | Explicit error; the controls are not silently converted to Sources. |
| Source/mixed positive fields with automatic exclusion scope | Explicit error; no invented scope or dropped exclusion. |
| Uncertain eligible asset/name coverage | Backend returns the existing explicit coverage error, not false completeness. |

The unchanged default mixed kinds/Names configuration is consequently unsupported for V2 bundles. This is a visible compatibility limitation, not a successful empty search. A simple supported manual recipe is **Sources only → Assets fields → include a known contained asset name**. Searching bundle names through the combined Names group still requires a separate approved control/mapping decision; this checkpoint does not broaden semantics to conceal that blocker.

Genuine bundle memos, Search-to-Assets navigation, indexed-source revalidation for navigation, shared-grid redesign and wheel-stall remediation remain deferred. Existing asset drafts/writers are untouched; the source row has no editable memo target. No new Unity-exclusive dependency was found.

## Changed files

- `UABS.PackageV2/Search/BundleSearchContracts.cs`: narrow engine-native session adapter only.
- `UABS/Script/Services/V2SearchOperation.cs`: bundle session admission route, shared lifecycle retained.
- `UABS/Script/ViewModels/SearchWorkspaceViewModel.cs`: projection dispatch, source Inspector and supported sort mapping.
- `UABS/Script/ViewModels/MainWindowViewModel.cs`: Search constructor opts in; Assets/Packages composition unchanged.
- `UABS/Script/View/Pages/SearchWorkspaceView.axaml` and `.axaml.cs`: format-aware visible columns, header mapping and Inspector visibility.
- `UABS.UnityAssets.Tests/V2BundleSearchWorkspaceTests.cs`: disposable fixtures and production/headless wiring coverage.
- `UABS.UnityAssets.Tests/V2MemoIntegrationTests.cs` and `V2BrowseIntegrationTests.cs`: adapt two historical production-asset-Search assumptions to the approved bundle projection, retaining asset-memo assertions through the existing programmatic API/real asset Inspectors and adding bundle/draft isolation checks.
- This report, [roadmap](v2-ui-workflow-recovery-roadmap.md) and [handoff](codex-session-handoff.md).

## Verification

Fourteen new in-process xUnit cases pass: actual app composition, two visible headers, copyable source Inspector, unsupported controls preserving confirmed rows, cross-asset gates/exclusions, package scope and duplicate names, header direction indicators, 805-source global ordering with backward paging, selection retention, explicit Refresh, rapid-sort/cancel late completion and V1 presentation.

- Focused existing suites passed: Search integration18, headers15, infinite scroll7, bundle SQL49, raw Search54; after adapting the historical assumptions, memo integration23 and browse integration26 passed.
- Final **in-process xUnit: 788 total, 787 passed, 0 failed, 1 existing hard-link permission skip (66.54s)**. This is not VSTest transport verification. Initial full run found only the two superseded asset-Search presentation assumptions; all original memo targeting/writing/draft assertions remain covered. A test-only nonexistent diagnostic-property reference was corrected before builds passed; no runtime parser/persistence defect was introduced or fixed.
- Final `dotnet build Avalonia.sln -c Debug --no-restore -m:1 -nr:false --verbosity quiet`: zero warnings/errors (1.16s), includes actual startup app. Initial integration build14.50s and final verification-runner build2.35s also passed with zero warnings/errors. Tests/builds ran sequentially.
- Approved design DDL equals canonical `Schema.sql` and the Debug embedded schema. Schema/version and persistence paths are unchanged. Zero Unity-runtime project/output dependency scan passed; seven tracked fixture hashes equal baseline. Protected legacy Unity, Assets/Packages views, memo services, real package/source and schema paths were not edited.
- Changed-document links/anchors (49), balanced fences, existing16-document/209-reference check, staged/unstaged whitespace and conflict checks passed. No new Mermaid diagrams; existing20 blocks were preserved, not rendered. All17 issue rows remain Reported; index empty. Removed only session-created `uabs_log.txt` from intentional regression failure paths.

The worktree is buildable and ready for review/commit as a bounded checkpoint, **not** proof of visual acceptance or whole-corpus performance. No desktop was launched or visually observed. No benchmark rerun, real-package mutation, process termination, automatic staging or commit occurred.

## Manual Debug acceptance

1. Launch `E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe`; load disposable V2 packages. Select Sources only, Assets fields and a known contained asset-name keyword.
2. Verify one row per source occurrence, only Name/Package headers, readable zebra/selection states and selectable source-level Inspector text with no asset memo editor.
3. Toggle both headers, scroll beyond 600 matches and back, switch workspaces, and check exact selected-source retention and package scope.
4. Try unsupported Names/mixed-kind/source-exclusion configurations: errors must be explicit, controls unchanged and prior confirmed rows retained.
5. Make a controlled catalog change in a disposable fixture: confirm stale state requires explicit Refresh. Confirm V1 Search still has its existing columns and native sorting.

No real OWL source/package was used or modified. Manual acceptance remains user-owned; all 17 recovery issues remain Reported. Do not start the navigation checkpoint from this report.
