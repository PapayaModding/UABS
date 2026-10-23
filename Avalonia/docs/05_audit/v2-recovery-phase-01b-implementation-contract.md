# V2 Recovery Phase 1B — Implementation Contract Audit

**Date:** 2026-10-23. **Baseline:** `efe97e0`; index/worktree clean at entry. **Scope:** source inspection and documentation only. No implementation, application execution, package queries/writes, builds or regression runs. All 17 recovery issues remain Reported; #1/#2/#3/#7 are not resolved.

## Authority and outcome

[Phase 1 Approved Decisions P1-D1–P1-D5](v2-ui-workflow-recovery-roadmap.md#phase-1-approved-decisions) are binding: one immutable source occurrence; source-name OR contained asset-name/memo matching; separate future genuine bundle-memo design; exact Search → Assets navigation/history; meaningful global source sorting before pagination. They do not amend [V2 D1–D8/schema](../03_design/package-format-v2-design.md), including D5 explicit Refresh. Recommendations below are not additional approvals.

The [Phase1A audit](v2-recovery-phase-01a-bundle-audit.md) supplies legacy call tracing and identity evidence. Source results are representable without a schema change, but current Search does **not** aggregate child asset matches into sources, and indexed Assets does not browse only a selected source. Recovery needs an explicit source projection and source-scoped navigation, not grouping visible asset rows or selecting an arbitrary child.

## 1. Source eligibility

[Extractor.ExtractAsync](../../UABS.UnityAssets/Metadata/AssetsToolsV2MetadataExtractor.cs), [PackageBuilder.BuildAsync](../../UABS.PackageV2/PackageBuilder.cs) and [canonical DDL](../../UABS.PackageV2/Schema.sql) establish these entities. Proposed inclusion still requires the approvals indicated.

| Evidence/entity | Proposed Search / Packages Indexed Assets treatment | Constraint / approval |
|---|---|---|
| Verified `bundle`, complete tables | One source occurrence; Search applies predicates, Packages lists inventory | Full immutable identity; never filename/hash deduplication |
| Verified standalone `serialized` source | Include as a clearly identified serialized source | **B1:** approve inclusion under bundle-oriented presentation |
| Bundle/serialized source with partial tables | Include with incomplete-coverage evidence; match captured data/source name | **B1:** approve presentation; a non-match does not prove absence in original bytes |
| Failed/unsupported/unknown imported source | Preserve source/status evidence in Packages; propose direct-name-only Search eligibility | **B1:** approve Search inclusion; no invented children or verified fingerprint |
| Serialized logical file | Child evidence, not another top-level result | Original directory ordinal, including gaps; standalone ordinal is 0 |
| Raw/nonserialized bundle entry | Not a serialized table or independent source | Content/flags/range evidence, not extension; independently imported raw files follow unsupported-source policy |

The extractor detects source kind by content, checks bundle entry ranges/candidacy and skips raw resources. Failed tables retain diagnostics; failed source verification discards provisional child metadata, not the source issue record. Dependency-budget failures are separate from asset-table coverage and do not alone exclude a source. Partial publication requires acknowledgement; views must retain coverage evidence. Equal names/CABs/hashes never merge distinct source occurrences.

## 2. Every Search control: current behavior and source mapping

Evidence: [SearchWorkspaceView](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml), [SearchWorkspaceViewModel request creation/field mapping](../../UABS/Script/ViewModels/SearchWorkspaceViewModel.cs), [SearchContracts](../../UABS.PackageV2/Search/SearchContracts.cs), [SearchSession](../../UABS.PackageV2/Search/SearchSession.cs) and [SearchQuery](../../UABS.PackageV2/Search/SearchQuery.cs). Preserve V1 and existing asset-query callers; do not silently redefine their result kinds.

| Control | Existing meaning | Source projection / approval needed |
|---|---|---|
| Include / comma OR | Trim English/Chinese comma terms; distinct OrdinalIgnoreCase; any term in selected fields | Direct source-name OR qualifying child-name/memo branch; display source once |
| Exact name | Whole-field versus substring for selected fields, including memo and excludes | Preserve V2 semantics, not legacy case-sensitive/first-term quirks |
| Exclude | Any excluded term in selected values vetoes that candidate | Keep child candidate scope; **B2:** define direct-source versus child exclusion composition; excluded sibling must not silently veto every child |
| Search memos | Adds Memo to selected fields | Child asset memo can qualify a source; never copy it into source Memo |
| Memo only | Non-whitespace asset-memo gate, independent of keyword memo matching | Same-child gate; **B2:** whether direct source hit also requires a qualifying child |
| Path ID | Exact signed invariant Int64; permits execution without include | Child-only AND gate; **B2:** interaction with direct source hits; no source Path ID |
| Class ID / exact type | V2 asset-only AND filters; signed Int32 / whole-field OrdinalIgnoreCase; mixed/non-asset requests currently fail validation | Same-child predicates possible, but enabling source projection is **B2**, not permission to ignore current validation |
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

**Grouping invariant:** a single child must satisfy all active child conditions. Asset A named `Hero` plus unrelated asset B having requested Class ID must not qualify the source. Use one correlated child predicate with text, excludes, Path ID, class/type and memo-only gates; never separate `EXISTS(name)` and `EXISTS(class)` that combine siblings.

**B2 remains open:** source named `Hero` with no child at requested Path ID cannot accidentally bypass that filter. Recommend requiring a qualifying child whenever child-only gates are active, with direct-name matching contributing the text part. This is a proposed clarification, not an approved override of P1-D2's OR. Also approve exclusion scope for direct source hits versus excluded children. Until decided, explicit validation is safer than silently ignoring controls.

## 3. Aggregation, global sorting and paging

Add a narrow explicit source-projection request/result to the existing V2 search path; preserve old SourceFile/asset callers and V1. No provider framework is necessary. Proposed query shape, **not implemented SQL or DDL**:

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

Finalize B2 before composing branches. One outer row per source establishes aggregation **before** per-package limit, global merge and pagination. Never join all asset hits then deduplicate after LIMIT, or group the retained 600 rows. Match-category evidence may be bounded; no unbounded witness lists, concatenated memos or arbitrary child selection. Source Memo is unavailable, not an invented asset memo.

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
- Preserve directory Back/Forward. **B3:** define source/workspace return integration in existing shell history before implementation; no database paths as directories or second competing navigator. Workspace switching alone does not fulfill source history.

## 5. Header meanings and global sorts

[Current header wiring](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml.cs) routes eligible V2 headers globally; V1 remains native. Raw asset Name currently requires asset-only results. Source projection needs explicit mappings, not arbitrary child values.

| Existing header | Source meaning / proposed behavior |
|---|---|
| Name | `source.original_name`, global OrdinalIgnoreCase/Ordinal ordering and immutable ties |
| Package | Stored package display name; deterministic package/location/source ties |
| Source | Current source label also original name; **B4:** approve alias-to-Name behavior, no invented field |
| Path ID | Not source-owned; **B4:** explicitly unsupported in source mode, no first/min/matching-child surrogate |
| Kind | No equivalent meaningful approved global Kind contract; do not invent one |
| Memo | Genuine source memo absent; no borrowed child values or sorting under P1-D3/P1-D5 |

No columns are added/removed/reordered or resized by this audit. Other existing meaningful source fields require explicitly scoped header mappings, not silent repurposing. Arrows must reflect published global order; unsupported headers explain limitations. Sort rebuild preserves criteria/scope/verified identity/drafts, resets paging and leaves D5 unchanged.

## 6. Workspace and model boundaries

| Workspace | Recovery row unit | Reuse / missing adaptation |
|---|---|---|
| Search | Matching source occurrences | Existing search operation/federation/window/lifecycle/styles; typed source result/evidence adapter |
| Assets | Individual assets of selected source | Existing asset identity/Inspector/memo coordinator/reader; exact source context/filter |
| Packages Indexed Assets | Source inventory | Source records/registration/styles; typed source presentation, no borrowed memo |

Do not turn `V2AssetRow` into a generic row that accidentally becomes a source memo target. Retain immutable context and asset memo revisions. The same browse ViewModel serves Assets and Packages today; source focus must not reset package inventory selection, editor sessions or workspace state. Narrow typed adapters/context are appropriate, not a wholesale generic-grid replacement. Inspectors retain technical details; column changes need their own authorized slice. V1 and editor transactions remain intact.

## 7. Approval gates and small implementation checkpoints

**Still undecided:** B1 kind/status inclusion; B2 all field/result-kind and direct-source/child-gate/exclusion interactions; B3 destination/history/stale/failed-draft navigation; B4 alias/meaningless-header behavior. These refine contracts without reopening P1-D1–P1-D5. Genuine bundle memo identity/storage/transactions/conflicts/UI stays in its separate milestone with separate schema approval.

A new scoped implementation request is required. Dependency order:

| Checkpoint / code responsibility | Preserve / focused tests | Manual Debug acceptance / risk |
|---|---|---|
| 1. Approved criteria/eligibility and typed source key/projection in contracts/adapters | Asset/V1 callers; duplicate labels/hashes, partial sources, same-child AND false positives | Review criteria/unsupported explanations; B1/B2 must be settled |
| 2. Source query/comparer/federation/keyset in SearchQuery/SearchSession/V2SearchService | Schema/D1–D8, budgets; Unicode/NULL, multi-package, >600 sources, late pages, lifecycle invalidation | Results/count/coverage; disposable 1M plans/calibration, no assumed fast-path benefit |
| 3. Source binding/header eligibility in Search VM/View, reuse window | Controls/defaults/styles, 600/128, drafts/D5; toggles, eviction/replay, rapid sorts, V1 | Published arrows, unsupported headers, scrolling/zebra/resize; never local-only sort |
| 4. Source-scoped reader/catalog/browse context | All classes/ordinals, exact memos/editor ownership; missing payload, duplicate logical names, partial coverage, paged source filtering, workspace independence | Correct root/only its assets/no arbitrary child; shared browse state must not reset Packages |
| 5. Go to Assets and existing-shell history/return | Browser history/valid sessions/drafts; pending/failed flush, unload during await, rapid navigation, identical names, return state | Exact destination/return/offline/failure paths; B3 and native provenance prerequisites |
| 6. Packages source inventory and separately authorized Assets column slice | Editor guards/dirty state/V1/technical Inspector; row units, memo eligibility, focus/styles | Source versus asset selection and existing package actions; no invented bundle memos/unrequested columns |

Each implementation checkpoint needs recoverable handoff, focused tests and actual Debug startup build before manual acceptance; full regression/audits when production search/navigation risk warrants them. No such runs occur in this audit. Rendering and query correctness are separate evidence. No infinite-scroll redesign, V1 retirement, reindexing or bundle memo implementation is authorized.

## Verification boundary

Check this audit and roadmap/handoff references for local links/anchors, balanced fences, decision consistency, whitespace/conflicts and protected paths. The Mermaid describes a **proposal**, syntax-reviewed, not rendered or desktop-observed. Source references are inspected code evidence, not runtime acceptance. No recovery issue is marked implemented and no new performance/build/UI result is claimed.
