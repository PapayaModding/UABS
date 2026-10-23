# Recovery Checkpoint 1 — Bundle Search Backend Foundation

**Date:** 2026-10-23. **Baseline:** `551e1de`, clean worktree/index at entry. **Status:** bounded backend implemented; remaining field/result-kind and incomplete-exclusion contracts deliberately blocked, not invented. Search UI integration and manual acceptance are not implemented; issue #1 and all17 recovery issues remain unresolved.

## Separate callable contract

[BundleSearchContracts](../../UABS.PackageV2/Search/BundleSearchContracts.cs) adds `CreateBundleSession`, `ReadBundlePageAsync` and `CountBundlesAsync` on the existing `V2SearchService`. Registration/unload APIs are shared, not duplicated. The typed session/cursor wraps existing lifetime/revision/keyset infrastructure; no second registration system or UI provider framework.

`BundleSearchResult` contains immutable package/generation/source/revision identity, Name, Package, original source path, package path/index directory, index status and bounded match-category evidence. It has no asset Path ID/Class ID/type/memo properties or invented bundle memo. Registration ID/location epoch/catalog revision remain in the session/page revision vector; a package UUID has only one live registration in the service. No live SQLite handles are retained in session/cursor.

Supported matching:

- Positive fields: explicit SourceName, AssetName and Memo masks. English/Chinese comma terms retain OR, literal OrdinalIgnoreCase exact/substring matching remains authoritative, nullable names do not fabricate text.
- Mandatory Memo-only, signed Int64 Path ID, Class ID and exact type-name gates are conjunctive and independently witnessed by potentially different contained assets. Text remains a separate required OR group when Path ID plus include terms are supplied. Include-free execution still needs Path ID.
- Any contained asset matching the caller's **explicit** AssetName/Memo exclusion mask vetoes its entire bundle, including direct source-name matches. The exclusion query is independent of positive/gate witnesses. No automatic exclusion-field scope is inferred from UI controls.
- Package scope is the captured selected registration list. Only verified indexed source evidence qualifies: `kind=bundle`, complete/partial source status, non-null strong revision fingerprint and byte size. A misleading `.bundle` filename does not admit standalone serialized files; failed/unverified sources do not qualify. This trusts admitted index evidence, not original-file existence or current source-byte verification.

## Explicit contract blockers

These return `InvalidRequest` with explanation and no session, rather than silently changing semantics:

| Input | Reason / exact next decision |
|---|---|
| Positive fields other than SourceName/AssetName/Memo | Existing PackageName/paths/CAB/dependency modes and their independent result-kind meanings need a compatible bundle projection contract; do not ignore them or redesign controls |
| ResultKinds other than SourceFile | Current Package/Asset result kinds emit those entities. P1-D12 forbids an unapproved evidence-toggle reinterpretation; confirm exact compatibility handling before integration |
| Exclusion terms without explicit nonempty AssetName/Memo scope, or with source/package/other field scope | P1-D10 settles bundle-wide veto, not field selection. Backend accepts explicit known asset fields, but does not decide a UI scope or source-metadata exclusion rule |
| Exclusion terms against any selected partial package | Unreadable/unindexed children cannot establish absence of a bundle-wide exclusion. Obtain explicit incomplete/unknown-membership policy; conservative whole-package rejection is an unsupported-case guard, not a new product membership rule |

Partial positives without exclusion retain existing PartialIndex/UnavailableMetadata diagnostics and per-source status. Exact count is indexed eligible matches, not original-corpus completeness. Empty bundles can satisfy observed source-name criteria without child gates; they cannot satisfy mandatory child existence. No unrelated native navigation/metadata fallback policy is implemented.

## Query and paging boundary

[BundleSearchQuery](../../UABS.PackageV2/Search/BundleSearchQuery.cs) emits one outer `source` candidate, with correlated child `EXISTS` predicates for match categories and each mandatory gate and an independent full-source exclusion `EXISTS`. Aggregation is therefore before outer exclusion/category filtering, keyset boundary, global ordering and LIMIT. Never group fetched asset hits or the visible600-row window.

[SearchQuery](../../UABS.PackageV2/Search/SearchQuery.cs) dispatches only bundle sessions to that branch; existing asset/package/source branches are unchanged for existing callers. [SearchSession](../../UABS.PackageV2/Search/SearchSession.cs) captures an optional immutable bundle request; [V2SearchService](../../UABS.PackageV2/Search/V2SearchService.cs) reuses bounded K+1 federation, workers, text/predicate budgets, deadlines, cancellation, short read transactions and pre-publication registration/revision-vector validation. No memo hydration runs for source rows. Name maps to source original name; Package maps to package display name. Existing OrdinalIgnoreCase/Ordinal comparison, direction and ascending immutable tie policy remain; duplicate labels/hashes do not merge occurrences.

Opaque cursors are owner/session scoped; changed scope/criteria/sort requires a new session. Late catalog changes reject a whole page, not partially publish it. The separate path does not attach to or change the600-row/128-checkpoint UI. No schema/index/format changes, no Memo sort. Correlated Unicode/name/memo scans may be expensive for a large many-source corpus; only bounded synthetic correctness tests are performed here, not a1M/OWL performance claim.

## Verification and preservation

New [V2BundleSearchTests](../../UABS.UnityAssets.Tests/V2BundleSearchTests.cs) exercise real SQL over disposable synthetic builder-created SQLite packages: bundle/asset/memo names, comma OR/Unicode/exact text, separate mandatory witnesses, source-name non-bypass, full-source veto, duplicate names/hashes, non-bundle/unverified rejection, empty/partial coverage, deterministic multi-package Name/Package asc/desc keysets,605 sources/1210 matching assets across pages, scope/count/cursor isolation, unchanged asset query behavior, cancellation/unload, late catalog publication and budget failure. No tests write real sources/packages.

Focused20/20 passed(1.82s) using the existing **in-process xUnit runner**, not VSTest; existing V2 Search tests54/54 passed(18.09s). Full regression:745 total,744 passed,0 failed,1 existing permission-related skip(66.05s). First sandboxed fixture attempt failed on temporary-file permissions; authorized normal fixture execution passed. Initial test compilation found target-typed params errors, corrected before successful0-warning/error test/runner build(10.91s). Baseline Debug solution/startup build passed0 warnings/errors(37.21s); final Debug solution/startup build passed0 warnings/errors(1.56s). Embedded canonical DDL equality, unchanged protected paths, zero-Unity-runtime project/output audit, Git whitespace/conflicts and documentation checks passed. Only session-created regression diagnostic `Avalonia/uabs_log.txt` was removed after testing; no pre-existing data removed. Final state and exact continuation dependencies are in the [handoff](codex-session-handoff.md).

No UI/XAML, navigation, Packages, V1, memo writer, schema, original Unity/OWL data or real SQLite package edits. No desktop execution, staging or commits. Future UI integration requires explicit resolution of the blocked control mappings, current Debug build and user-operated acceptance. Bundle-owned memos, Go to Assets and V1 retirement remain out of scope.
