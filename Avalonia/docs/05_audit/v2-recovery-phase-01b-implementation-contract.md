# V2 Recovery Phase 1B — Implementation Contract Audit

**Date:** 2026-10-23. **Baseline:** `efe97e0`; index/worktree clean at entry. **Scope:** source inspection and documentation only. No implementation, application execution, package queries/writes, builds or regression runs. All 17 recovery issues remain Reported; #1/#2/#3/#7 are not resolved.

## Authority and outcome

**Current contract:** the [Phase1C approval record](v2-ui-workflow-recovery-roadmap.md#phase-1c-approved-decisions-p1-d6p1-d9) adds P1-D6–P1-D9. These override conflicting proposals below, not source findings. The original P1-D1–P1-D5 remain binding. No implementation is authorized and all17 issues remain unresolved.

[Phase 1 Approved Decisions P1-D1–P1-D5](v2-ui-workflow-recovery-roadmap.md#phase-1-approved-decisions) are binding: one immutable source occurrence; source-name OR contained asset-name/memo matching; separate future genuine bundle-memo design; exact Search → Assets navigation/history; meaningful global source sorting before pagination. They do not amend [V2 D1–D8/schema](../03_design/package-format-v2-design.md), including D5 explicit Refresh. Recommendations below are not additional approvals.

The [Phase1A audit](v2-recovery-phase-01a-bundle-audit.md) supplies legacy call tracing and identity evidence. Source results are representable without a schema change, but current Search does **not** aggregate child asset matches into sources, and indexed Assets does not browse only a selected source. Recovery needs an explicit source projection and source-scoped navigation, not grouping visible asset rows or selecting an arbitrary child.

## Phase 1C Approved Decisions P1-D6–P1-D9

**Recorded 2026-10-23 by explicit user approval.** The authoritative details are in the roadmap linked above. The four product approval areas B1–B4 are decided; technical dependencies remain.

The original approvals remain: **P1-D1** immutable occurrence identity/no label/hash merging; **P1-D2** bundle-name or contained name/memo matching, aggregated before global sort/pages; **P1-D3** separately designed genuine bundle memos, existing asset memos unchanged; **P1-D4** exact Search → Assets navigation/history and supported offline indexed metadata; **P1-D5** deterministic meaningful global sorting, bundle Memo sorting deferred. P1-D6–P1-D9 refine these directions as follows.

| Decision | Approved contract | Remaining implementation detail |
|---|---|---|
| P1-D6 / B1 | Bundle-level Search accepts only sources confirmed as actual Unity bundles by reliable source-kind/indexing evidence, one occurrence under P1-D1; no filename/extension/CAB/name heuristics or silent serialized/resource inclusion | Reliability where classification evidence is insufficient; partial/failed coverage; non-bundle Packages inventory not separately decided |
| P1-D7 / B2 | Different children may collectively satisfy conditions, e.g. separate assets supply `hero` name, `Texture2D` type and `Attack` memo; same-asset-only recommendation **superseded**. Preserve P1-D2 bundle-name matching and aggregation before sort/pages | Include OR groups, candidate excludes/negation scope, Memo-only, exact/substring, type/Class ID/Path ID, bundle-name/filter interaction, empty/incomplete sets, inherently identity-bound predicates |
| P1-D8 / B3 | Go to Assets uses exact indexed occurrence identity; directory Back/Forward/Up stay unchanged; carry only necessary extra source-navigation context | Source/child selection, missing originals, unload/replacement, stale results, return context and draft policy |
| P1-D9 / B4 | Bundle Search shows exactly Name and Package, with meaningful supported global sorting; no Source or asset-level Path ID/Class ID/Kind/Memo columns; technical source path/identity in Inspector and internal keys | Display-name mapping, deterministic ties/cursors and scoped UI implementation; extra visible columns require approval |

P1-D7 is not unconditional OR and does not authorize removal/reinterpretation of controls. P1-D9 supersedes the Source-header alias proposal. Bundle-owned memo storage remains separately deferred by P1-D3; V2 D1–D8/schema/D5 remain unchanged.

## 1. Source eligibility

[Extractor.ExtractAsync](../../UABS.UnityAssets/Metadata/AssetsToolsV2MetadataExtractor.cs), [PackageBuilder.BuildAsync](../../UABS.PackageV2/PackageBuilder.cs) and [canonical DDL](../../UABS.PackageV2/Schema.sql) establish these entities. The table retains entity evidence but reconciles earlier eligibility proposals with P1-D6; Search and Packages policy must not be conflated.

| Evidence/entity | Proposed Search / Packages Indexed Assets treatment | Constraint / approval |
|---|---|---|
| Verified `bundle`, complete tables | One source occurrence; Search applies predicates, Packages lists inventory | Full immutable identity; never filename/hash deduplication |
| Verified standalone `serialized` source | Excluded from bundle-level Search under P1-D6 | Earlier inclusion proposal superseded; non-bundle Packages inventory remains a separate detail |
| Confirmed bundle with partial tables | Eligible kind under P1-D6; preserve incomplete coverage | Exact empty/incomplete-set matching remains unresolved under P1-D7; non-match does not prove absence in original bytes |
| Failed/unsupported/unknown imported source | Preserve source/status evidence; Search requires reliable confirmation it is an actual bundle | Earlier blanket direct-name eligibility proposal superseded; absent reliable evidence, document limitation, never invent classification/children/fingerprint |
| Serialized logical file | Child evidence, not another top-level result | Original directory ordinal, including gaps; standalone ordinal is 0 |
| Raw/nonserialized bundle entry | Not a serialized table or independent source | Content/flags/range evidence, not extension; independently imported raw files follow unsupported-source policy |

The extractor detects source kind by content, checks bundle entry ranges/candidacy and skips raw resources. Failed tables retain diagnostics; failed source verification discards provisional child metadata, not the source issue record. Dependency-budget failures are separate from asset-table coverage and do not alone exclude a source. Partial publication requires acknowledgement; views must retain coverage evidence. Equal names/CABs/hashes never merge distinct source occurrences.

## 2. Every Search control: current behavior and source mapping

Evidence: [SearchWorkspaceView](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml), [SearchWorkspaceViewModel request creation/field mapping](../../UABS/Script/ViewModels/SearchWorkspaceViewModel.cs), [SearchContracts](../../UABS.PackageV2/Search/SearchContracts.cs), [SearchSession](../../UABS.PackageV2/Search/SearchSession.cs) and [SearchQuery](../../UABS.PackageV2/Search/SearchQuery.cs). Preserve V1 and existing asset-query callers; do not silently redefine their result kinds.

| Control | Existing meaning | Source projection / approval needed |
|---|---|---|
| Include / comma OR | Trim English/Chinese comma terms; distinct OrdinalIgnoreCase; any term in selected fields | Direct source-name OR qualifying child-name/memo branch; display source once |
| Exact name | Whole-field versus substring for selected fields, including memo and excludes | Preserve V2 semantics, not legacy case-sensitive/first-term quirks |
| Exclude | Any excluded term in selected values vetoes that candidate | P1-D7 leaves candidate/negation/exclusion scope unresolved; do not silently apply a sibling veto or silently waive exclusions |
| Search memos | Adds Memo to selected fields | Child asset memo can qualify a source; never copy it into source Memo |
| Memo only | Non-whitespace asset-memo gate, independent of keyword memo matching | P1-D7 cross-asset principle applies; detailed quantification and interaction with direct bundle hits unresolved |
| Path ID | Exact signed invariant Int64; permits execution without include | Asset evidence, no source-owned Path ID; P1-D7 detailed Boolean/identity binding and direct bundle-name interaction remain unresolved |
| Class ID / exact type | V2 asset-only AND filters; signed Int32 / whole-field OrdinalIgnoreCase; mixed/non-asset requests currently fail validation | P1-D7 permits different children to contribute; detailed filter/identity rules need specification before changing current validation |
| Fields: Names | PackageName + SourceName + AssetName | P1-D2 source/child branches; **B2:** explicitly retain/define package-name contribution |
| Fields: Paths | PackagePath + IndexDirectory + SourcePath | Package/source metadata; **B2:** approve interaction with source/child-name rule |
| Fields: CAB codes | Logical-file CAB evidence | **B2:** contained logical-file match may qualify source; never identity by CAB |
| Fields: Assets | AssetName only | Child branch; **B2:** define direct source-name participation |
| Fields: Dependencies | Original/virtual dependency paths and raw CAB | **B2:** approve contained dependency qualification; preserve both paths |
| Fields: All | All except Memo unless Search memos enabled | Combine approved branches, no genuine bundle memo search yet |
| Package/Source/Asset result kinds | Independent branches; SourceFile branch does not search child asset names/memos | **B2:** conflict with source-only displayed unit requires approved mapping/eligibility, not removal or silent reinterpretation |
| Package checkboxes, Select All/Clear, name filter | Scope captured on execution; filter changes visible picker entries | Preserve behavior; hidden checkbox entries must not silently alter selection |
| Search / Enter / Cancel | Explicit execution; criteria-changed status and cancellation | Preserve; include-free requests need Path ID, exclude-only/Memo-only do not enumerate all; at least one result kind required today |
| Refresh Search / paging Retry | Explicit stale readmission versus retry valid boundary | Preserve D5; criteria edits, memo saves and navigation do not implicitly Search |

**Historical recommendation — superseded by P1-D7:** Phase1B originally required a single child to satisfy all conditions and rejected combining a `Hero` name from asset A with a Class ID from asset B. That is no longer a mandatory invariant: different contained assets may collectively satisfy conditions. Do not carry the original same-asset-only rule into implementation/tests. Predicates inherently tied to one asset identity still need an explicit contract; cross-asset approval does not mean indiscriminate OR.

**B2 principle decided; detailed semantics unresolved:** the earlier recommendation to require one qualifying child for every active child gate is superseded. Define include OR groups, candidate-level excludes, negation/exclusion scope, Memo-only, exact/substring, type/Class ID/Path ID predicates, direct bundle-name/filter interaction, empty/incomplete sets and identity-bound predicates explicitly. In particular, source `Hero` with no child at requested Path ID needs an approved Boolean rule; neither automatic bypass nor rejection is implied by P1-D7. Do not silently ignore/remove existing controls.

## 3. Aggregation, global sorting and paging

Add a narrow explicit source-projection request/result to the existing V2 search path; preserve old SourceFile/asset callers and V1. No provider framework is necessary. **Historical Phase1B sketch — same-asset predicate superseded by P1-D7; not an implementation contract, implemented SQL or DDL:**

```sql
SELECT source_identity_and_display_evidence
FROM source AS s
WHERE eligible_source(s)
  AND (approved_direct_source_predicate(s)
       OR EXISTS (
         SELECT 1 FROM logical_file AS f
         JOIN asset AS a ON a.file_id = f.file_id
         WHERE f.source_id = s.source_id
           AND all_approved_child_predicates_on_this_asset(a, f)
       ))
  AND after_source_keyset_cursor(s)
ORDER BY approved_source_sort_and_immutable_ties
LIMIT page_size_plus_one;
```

**Current direction:** retain one outer row per confirmed bundle, but replace the sketch's single-child requirement with the separately specified cross-asset Boolean expression. Multiple correlated predicates may express separately quantified conditions; their grouping/negation/identity constraints are not approved SQL yet. Finalize detailed control semantics before writing that expression. Aggregation still precedes per-package limit, global merge and pagination. Never deduplicate after LIMIT or group the retained600 rows. Keep match evidence bounded, with no concatenated child memos or arbitrary child selection. Source Memo remains unavailable and is not a Search column under P1-D9.

[V2SearchService.ReadPageAsync](../../UABS.PackageV2/Search/V2SearchService.cs) already merges ordered per-package candidates into a bounded best-K+1 set with worker limits, cancellation and revision checks. Reuse this architecture (engine page maximum 500; production fetch 100), counting sources rather than assets. Keep [V2SearchWindow](../../UABS/Script/ViewModels/V2SearchWindow.cs)'s 600 retained rows/128 checkpoints and separately retained selection/draft. Exhausted exact counts mean indexed eligible sources, not whole-corpus completeness.

Source key: `(package_uuid, generation_uuid, source_uuid, source_revision_uuid)`, with registration/location epoch and catalog revision as lifecycle evidence. SQLite integer IDs stay internal. Use existing OrdinalIgnoreCase then Ordinal text comparison, explicit NULL-first ascending and deterministic immutable ties. Reverse the approved primary direction consistently; retain the existing ascending tie policy explicitly. Original source names are non-null; optional evidence remains nullable rather than invented strings.

Cursors remain session/projection/filter/sort/scope/revision-vector specific. Sort changes create a new session, invalidate cursors/checkpoints, reset the window and reject late pages using cancellation plus request generation. Retain exact selected source if still a verified match, never row index/name. Unload/generation/location changes invalidate targets; same-generation catalog changes require explicit Refresh under D5. Never retarget to another registration at the same path.

### Existing indexes and approximately 1M assets

Schema includes source-name indexing, `(source_id,directory_ordinal)` logical-file lookup, `(file_id,table_ordinal)` occurrences, asset identity/Class ID/Path ID/type indexes, ASCII-name and memo-nonblank partial indexes, and dependency declaration keys. These support correctness without DDL changes, not guaranteed efficient aggregation. General Unicode substring/memo matching may scan 1M assets; sorting can spill to temporary storage. Managed candidate bounds do not bound SQLite work or temporary-file size.

Gate5G's ASCII/default **asset** fast path has strict eligibility; it does not automatically apply to source aggregation. Existing [Gate5G timings](package-format-v2-gate-05g-search-paging-performance.md) are not benchmarks of this proposal. Before optimization, inspect disposable multi-source 10k/100k/1M query plans, predicate/comparison work, first/deep/backward pages and Unicode/memo searches. Avoid full child rescans per source. Preserve explicit deadline/budget failures, never truncated success. Persistent indexes/derived keys require separate review; no schema edits authorized here.

## 4. Search → Assets and history

[MainWindowViewModel](../../UABS/Script/ViewModels/MainWindowViewModel.cs) switches long-lived workspaces and flushes drafts on departure; `LoadFileAsync` swaps native sessions only after success. [BrowserViewModel](../../UABS/Script/ViewModels/BrowserViewModel.cs)'s Back/Forward is **directory history**, not Search/source/workspace history. File activation does not turn it into a source stack.

[V2BrowseViewModel](../../UABS/Script/ViewModels/V2BrowseViewModel.cs) currently pages all package assets; [PackageReader](../../UABS.PackageV2/PackageReader.cs) lacks source-filtered asset paging. Source metadata is flattened for display. Selecting the package and changing workspace is insufficient: add typed exact-source resolution and source-scoped asset retrieval under [V2BrowseCatalog registration](../../UABS/Script/Services/V2BrowseCatalog.cs), retaining every logical file/class.

**Recommended destination, B3 approval required:** select the exact source/root context and show its indexed assets with **no arbitrary child selected**. This works offline without pretending a child memo belongs to the source. A matched-child witness is evidence, not imposed selection. If native payload opening is chosen instead/as an additional action, reuse the loader with verified locator/bytes, never filename/CAB or SQLite path. Native asset rows lack full V2 ordinal/package provenance; path/hash/logical-name bridging can be ambiguous and must not weaken [InspectorMemoService](../../UABS/Script/Services/InspectorMemoService.cs) validation.

Proposed transition, not existing implementation:

```mermaid
sequenceDiagram
    participant S as Search
    participant N as Existing shell navigation
    participant D as Shared memo drafts
    participant R as V2 registration
    participant A as Indexed Assets
    S->>N: Go to Assets with captured source key and return context
    N->>D: Flush captured work without retargeting
    D-->>N: Outcome; failed drafts remain retained
    N->>R: Revalidate exact identity and lifecycle evidence
    R-->>N: Exact source or explicit rejection
    N->>A: Publish source destination only if still current
```

- Identical names remain distinct by key/context. Unload/replacement during awaits rejects the captured transition, never auto-opens a new package at the old path.
- Missing original bytes permit admitted indexed browsing with metadata-only status. Failed native loading preserves the old valid session.
- **B3:** approve blocking stale-result navigation until Refresh versus separately revalidating exact navigation. D5 prohibits stale paging but does not decide this policy itself.
- Pending saves remain attached to captured immutable asset identities. **B3:** decide blocking navigation on failed/conflicted flush versus allowing departure with retained draft/status. Never discard drafts, invent a modal policy or trigger competing flush paths.
- Capture Search criteria/scope, selected source, window/anchor and return context. Return does not rerun Search; package/editor ownership and dirty clones remain intact.
- **P1-D8 decided:** preserve directory Back/Forward/Up without redefining them or replacing their history. Carry additional source-navigation context only where necessary. Detailed return/context behavior remains unresolved; no database paths as directories or competing navigator.

## 5. Header meanings and global sorts

[Current header wiring](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml.cs) routes eligible V2 headers globally; V1 remains native. Raw asset Name currently requires asset-only results. Source projection needs explicit mappings, not arbitrary child values.

| Existing header | Source meaning / proposed behavior |
|---|---|
| Name | `source.original_name`, global OrdinalIgnoreCase/Ordinal ordering and immutable ties |
| Package | Stored package display name; deterministic package/location/source ties |
| Source | Historical alias-to-Name proposal superseded by P1-D9: not displayed; source path/technical identity in Inspector |
| Path ID | Not displayed under P1-D9; no first/min/matching-child surrogate |
| Kind / Class ID | Not displayed or repurposed as bundle columns under P1-D9 |
| Memo | Not displayed under P1-D9; no borrowed child value or bundle Memo sort under P1-D3/P1-D5 |

**P1-D9 current contract:** exactly Name and Package are visible in bundle-level Search; no additional columns without explicit approval. Name is the bundle display name, Package the owning UABS package; retain meaningful global sorts where supported. Internal source identity remains available for deterministic ordering/deduplication/navigation. The table documents existing headers and their future disposition, not permission to retain extra headers. This documentation edit changes no XAML. Sort arrows reflect published order; rebuild preserves criteria/scope/verified identity/drafts, resets paging and leaves D5 unchanged.

## 6. Workspace and model boundaries

| Workspace | Recovery row unit | Reuse / missing adaptation |
|---|---|---|
| Search | Matching source occurrences | Existing search operation/federation/window/lifecycle/styles; typed source result/evidence adapter |
| Assets | Individual assets of selected source | Existing asset identity/Inspector/memo coordinator/reader; exact source context/filter |
| Packages Indexed Assets | Source inventory | Source records/registration/styles; typed source presentation, no borrowed memo |

Do not turn `V2AssetRow` into a generic row that accidentally becomes a source memo target. Retain immutable context and asset memo revisions. The same browse ViewModel serves Assets and Packages today; source focus must not reset package inventory selection, editor sessions or workspace state. Narrow typed adapters/context are appropriate, not a wholesale generic-grid replacement. Inspectors retain technical details; column changes need their own authorized slice. V1 and editor transactions remain intact.

## 7. Approval gates and small implementation checkpoints

**B1–B4 product directions decided by P1-D6–P1-D9:** confirmed bundles only in Search; cross-asset matching allowed; exact navigation preserves directory Back/Forward/Up; Search headers exactly Name/Package. Still unresolved are reliable classification where evidence is insufficient, detailed Boolean/control/identity/coverage semantics, source selection/offline/lifecycle/stale/draft/context behavior, and exact display/sort/cursor integration. These do not reopen P1-D1–P1-D9. Genuine bundle memo design stays separate with separate schema approval.

A new scoped implementation request is required. Dependency order:

| Checkpoint / code responsibility | Preserve / focused tests | Manual Debug acceptance / risk |
|---|---|---|
| 1. Approved criteria/eligibility and typed source key/projection in contracts/adapters | Asset/V1 callers; duplicate labels/hashes, bundle-only evidence, partial sources, approved cross-asset combinations and eventual Boolean/identity boundaries | Review criteria/unsupported explanations; detailed P1-D7 semantics must be settled |
| 2. Source query/comparer/federation/keyset in SearchQuery/SearchSession/V2SearchService | Schema/D1–D8, budgets; Unicode/NULL, multi-package, >600 sources, late pages, lifecycle invalidation | Results/count/coverage; disposable 1M plans/calibration, no assumed fast-path benefit |
| 3. Source binding and P1-D9 Name/Package-only headers in Search VM/View, reuse window | Unchanged criteria controls/defaults/styles, 600/128, drafts/D5; toggles, eviction/replay, rapid sorts, V1 | Exactly two approved headers, published arrows and Inspector source details; never local-only sort |
| 4. Source-scoped reader/catalog/browse context | All classes/ordinals, exact memos/editor ownership; missing payload, duplicate logical names, partial coverage, paged source filtering, workspace independence | Correct root/only its assets/no arbitrary child; shared browse state must not reset Packages |
| 5. Go to Assets and existing-shell history/return | Browser history/valid sessions/drafts; pending/failed flush, unload during await, rapid navigation, identical names, return state | Exact destination/return/offline/failure paths; B3 and native provenance prerequisites |
| 6. Packages source inventory and separately authorized Assets column slice | Editor guards/dirty state/V1/technical Inspector; row units, memo eligibility, focus/styles | Source versus asset selection and existing package actions; no invented bundle memos/unrequested columns |

Each implementation checkpoint needs recoverable handoff, focused tests and actual Debug startup build before manual acceptance; full regression/audits when production search/navigation risk warrants them. No such runs occur in this audit. Rendering and query correctness are separate evidence. No infinite-scroll redesign, V1 retirement, reindexing or bundle memo implementation is authorized.

## Verification boundary

Check this audit and roadmap/handoff references for local links/anchors, balanced fences, decision consistency, whitespace/conflicts and protected paths. The Mermaid describes a **proposal**, syntax-reviewed, not rendered or desktop-observed. Source references are inspected code evidence, not runtime acceptance. No recovery issue is marked implemented and no new performance/build/UI result is claimed.
