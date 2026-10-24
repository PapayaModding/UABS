# Recovery Checkpoint 1.1 — Search Compatibility Resolution

**Date:** 2026-10-23. **Baseline:** `9094328`, clean index/worktree at entry. Backend only; no Search/Assets/Packages UI, navigation, schema/version, V1 or memo-writing changes authorized. P1-D1–P1-D15 remain binding; all17 recovery issues remain unresolved.

## Pre-implementation compatibility matrix

This matrix records investigation **before code changes**, using [Checkpoint1](v2-recovery-checkpoint-01-bundle-search.md), [Phase1B](v2-recovery-phase-01b-implementation-contract.md), [Phase1D approvals](v2-ui-workflow-recovery-roadmap.md#phase-1d-approved-decisions-p1-d10p1-d15), actual [UI field mapping](../../UABS/Script/ViewModels/SearchWorkspaceViewModel.cs), [V1 MatchFields](../../UABS.UnityAssets/Packages/Search/PackageSearchService.cs) and [V2 independent candidate branches](../../UABS.PackageV2/Search/SearchQuery.cs).

| Control/query mapping | Existing behavior | Bundle interpretation / authority / feasibility | Ambiguity and action |
|---|---|---|---|
| AssetName/Memo positive fields | Match selected child values with comma OR and exact/substring OrdinalIgnoreCase | Existing CP1 behavior under P1-D2/P1-D11/P1-D12; preserve | None; no bundle-owned values |
| SourceName/IndexDirectory/SourcePath | SourceFile branch compares source's original name/index directory/original path | Same fields on the exact source row; source predicates, not asset properties. P1-D12 preserves meaning; straightforward SQL | Implement additional source path/index-directory positive fields |
| CAB/dependencies | SourceFile branch matches contained logical CAB or raw original/virtual dependency paths/raw CAB | Reuse exact source-linked evidence via EXISTS; existing values/Unicode semantics, not guessed identity | Implement positive CAB/dependency fields; no dependency resolution added |
| PackageName/PackagePath; Names/Paths/All UI groups containing them | Package branch matches owning package and emits a Package row; SourceFile/Asset branches do not inherit those matches | Expanding package match into every bundle or ignoring package fields would change result-kind behavior | Remain explicit InvalidRequest; approval needed for package-to-bundle membership. No automatic UI group remap |
| SourceFile result kind | One source candidate | Compatible with verified bundle-only projection under P1-D6 | Keep supported |
| Package/Asset/combined/None result kinds | Choose independent emitted entities; Class/type currently assets-only | Not equivalent to one verified source occurrence | Remain explicit unsupported; no evidence-toggle redesign or dropped kind |
| Exclusion with AssetName/Memo-only field selection | Same selected fields govern include/exclude; any selected name/memo match rejects current asset | Derive the identical field mask; P1-D10 widens rejection to entire bundle, not to new fields | Implement automatic scope for asset-only selections, retain explicit CP1 asset masks |
| Exclusion with source/metadata/mixed fields | Source candidate checks metadata; child candidate checks selected asset fields independently | New projected bundle could plausibly veto on source-only evidence, or only on children under P1-D10 | Keep explicit asset mask required or return unsupported. Example: source `debug.bundle`, child `hero`, include `hero`, exclude `debug`: source-only exclusion policy is not decided |
| Signed Path ID/Class ID/type/Memo-only | Asset predicates, mandatory and independent from include OR | Keep CP1 cross-asset mandatory witnesses and validation; never source properties | No additional UI semantics or identity relaxation |
| Package scope/Unicode/exact/comma OR | Captured registrations and existing .NET matcher | Unchanged | No new approval question |
| Partial package with complete eligible bundles, unrelated incomplete serialized/unverified sources | Package partial flag covers all sources, not just query-eligible bundles | Evaluate source-level complete evidence; non-bundles/unverified sources remain ineligible under P1-D6 | Replace blanket package rejection with admission-captured eligible-source coverage checks |
| Eligible partial bundle / unavailable requested asset-name evidence | Absence of indexed witness does not establish negative over missing evidence | Cannot safely assert no excluded child | Keep explicit indeterminate/unsupported response for whole requested scope; do not silently skip uncertain bundles or return complete counts. Whether to expose incomplete results needs separate policy |

The implementation must preserve the separate callable backend, pre-page aggregation, immutable keys, global Name/Package keysets and bounded cancellation/revision checks. The matrix is not approval of blocked mappings. Progress and verified results will be recorded below and in the handoff.

## Implemented mappings

[BundleSearchContracts](../../UABS.PackageV2/Search/BundleSearchContracts.cs) now accepts positive SourceName, SourcePath, IndexDirectory, CabCode, Dependency, AssetName and Memo fields. Defaults remain unchanged. [BundleSearchQuery](../../UABS.PackageV2/Search/BundleSearchQuery.cs) reads original source values, CAB evidence through logical files and all three existing dependency strings independently (`raw_path`, `raw_virtual_path`, `raw_cab`). Matching uses the existing .NET UDFs; dependency evidence is not a resolved target or identity. Multiple matching declarations still emit one source candidate before global keysets/LIMIT/federation.

When exclusions are active and `Fields` contains **only** AssetName/Memo, a missing `ExclusionFields` is inferred from that same mask, matching existing field-selection behavior. A record copy captures this normalized contract; the caller's request is not mutated. Existing explicit AssetName/Memo masks remain honored even when positive source fields are present. Automatic mixed/source exclusion scope is not inferred; no new syntax, source-only veto or implicit search of unselected metadata is introduced. P1-D10 rejection scope remains entire bundle if any selected child name/memo matches, regardless of positive/gate witnesses.

PackageName/PackagePath and non-SourceFile kinds still return `InvalidRequest` explaining that a bundle membership mapping is required. This includes the current UI Names/Paths/All groups when they contain package fields. No fields are stripped from those groups. Invalid metadata/mixed automatic exclusion requests explain the need for an explicit supported asset mask or a separate scope decision; existing UI remains untouched. Type/Class ID/Path ID/Memo-only keep their CP1 child predicates and cross-asset mandatory conjunction.

## Source completeness and indeterminate exclusions

[PackageBuilder](../../UABS.PackageV2/PackageBuilder.cs) marks source complete only after committed source verification, final extractor status and logical-file summaries; failed verification discards provisional children. Package partial status aggregates **all** sources. [StorageSchema validation](../../UABS.PackageV2/StorageSql.cs) enforces complete logical-table declared/captured counts, complete source evidence/logical coverage, and complete package consistency. Dependency-enrichment status is separate from asset coverage and does not automatically mean missing assets.

[V2SearchService registration](../../UABS.PackageV2/Search/V2SearchService.cs) captures a small field mask in [Registration](../../UABS.PackageV2/Search/SearchSession.cs), under the same read transaction/package revision as admission and sort constants:

- Any CP1-eligible verified bundle with partial source status makes both asset-name and memo exclusions indeterminate for that selected registration.
- A complete eligible bundle containing unavailable names (`failed`, `budgeted`, `unattempted`) makes **name** exclusions indeterminate, even when all asset rows are captured. Known `absent` names are not unavailable evidence. Known nullable memo values remain independently evaluable.
- Incomplete serialized sources and failed/unverified sources excluded by unchanged CP1 eligibility do not invalidate complete eligible bundle coverage. No eligible source is silently reclassified or omitted by the exclusion check.

At bundle-session creation, the requested exclusion mask is checked against that captured coverage mask, replacing the blanket package-partial rejection. Thus a partial package containing only complete eligible bundles plus unrelated incomplete/noneligible sources can be evaluated safely with existing PartialIndex warnings. A source-level incomplete eligible bundle still causes an explicit **InvalidRequest / indeterminate** for the entire selected scope, not a silently skipped source or successful exact count. The caller may explicitly select only other safe registrations; no automatic scope reduction occurs. Even a query apparently targeting another source is conservatively blocked in that uncertain registration; source-predicate pruning/partial-result policy is not invented.

The admitted revision vector, short read transactions and pre-publication checks remain unchanged: external/cooperating revision changes reject stale pages instead of trusting old coverage. This is registration-scoped immutable evidence, not a new validation cache or schema field. No publication opt-in, source files or packages are rewritten. Incomplete positives without exclusion retain CP1 behavior/coverage warnings; absence of observed matches is not original-corpus proof.

## Tests, files and verification checkpoint

Changed backend files: BundleSearchContracts, BundleSearchQuery, SearchSession and V2SearchService; expanded [V2BundleSearchTests](../../UABS.UnityAssets.Tests/V2BundleSearchTests.cs). No existing test cases removed. Added29 SQL-boundary cases for source fields compared directly against existing SourceFile queries, field-specific exclusion inference across siblings, unsupported package/kind/metadata-exclusion mappings, partial-package versus source coverage, unavailable-name versus known-memo evidence, independent dependency warnings and combined numeric/type/CAB/exclusion keysets.

Final verification:

| Check | Actual result |
|---|---|
| Focused bundle SQL tests | **49/49 passed**,2.83s, including original20 and29 added cases |
| Existing Search regression | **54/54 passed**,18.52s on recovery; also passed18.04s before interruption |
| Full in-process xUnit | **774 total:773 passed,0 failed,1 existing skip**,67.98s; interrupted session's completion recovered and runner library/test hashes verified against current Debug outputs |
| Debug solution including actual startup UABS | **0 warnings/errors**,1.34s on recovery; also passed1.55s before interruption |
| External runner/test build; storage library build | **0 warnings/errors**,16.10s and4.38s respectively |
| Format/safety/integrity | Embedded canonical DDL equals unchanged schema; protected UI/navigation/V1/Unity/genuine fixture paths unchanged; zero-Unity project/output audit, Git whitespace/conflicts, documentation links/anchors/fences and17 Reported issue rows passed |

These are standard **in-process xUnit** results, not VSTest transport verification or manual UI acceptance. The existing skip is the physical CreateHardLink test explicitly disabled for Win32 access-denied restrictions. No source changed during recovery, so the verified completed full suite was not rerun. A sandboxed recovery build failed on repository/output access; the normally authorized Debug build passed without source fixes. The usage-limit-interrupted safety command had not executed; its checks were completed on recovery without approval bypasses. Removed only the diagnostic log generated by these regression tests (`Avalonia/uabs_log.txt`), not pre-existing user data. Current code is verified buildable; remaining errors below are deliberate contract boundaries.

No Search/Assets/Packages UI, grid columns, navigation, schema/version, V1 behavior, memo writer, real Unity/OWL data or packages modified. No desktop automation, staging or commits. This checkpoint does not integrate the backend or resolve issue #1.

## Remaining user-visible decisions

1. **Package fields/result kinds:** should a package match produce its eligible bundles, or should package/asset result-kind controls be handled separately? Current independent result meanings cannot be mapped by merely relabeling rows. Explicit approval required; no evidence-toggle redesign selected.
2. **Source/mixed exclusion fields:** bundle `debug.bundle` with child `hero`, include `hero`, exclude `debug`: should a source-name-only hit veto the source, while P1-D10 always vetoes any excluded child? Backend does not choose. Asset-only selection or an explicit asset exclusion mask is supported now.
3. **Indeterminate coverage:** how should a future UI present incomplete eligible bundles that cannot establish a negative exclusion? Whole-scope error remains the safe current boundary; no skipped-source success or fabricated complete count. Known complete source evidence is already used where sufficient.

Future integration requires resolving these cases, preserving Name/Package-only presentation, exact identities, D5 Refresh and shared drafts; none is permission to begin UI work or other recovery issues. P1-D1–P1-D15 are not reopened.
