# V2 Recovery Phase 1D — Search Semantics and Navigation Lifecycle

**Date:** 2026-10-23. **Baseline:** `f6183cf`, clean index/worktree at entry. **Mode:** focused source audit/documentation only; no code changes, package access, builds, tests or desktop execution. All17 recovery issues remain Reported/unresolved.

## Authority and findings

[P1-D1–P1-D5](v2-ui-workflow-recovery-roadmap.md#phase-1-approved-decisions) and [P1-D6–P1-D9](v2-ui-workflow-recovery-roadmap.md#phase-1c-approved-decisions-p1-d6p1-d9) remain binding. Search returns one confirmed bundle occurrence; different children may satisfy different conditions; aggregate before global sorting/paging. Exact Go to Assets preserves directory Back/Forward/Up. Bundle Search shows exactly Name and Package; technical identity stays in Inspector/internal context. Bundle-owned memos remain separate. V2 D1–D8/schema and D5 explicit Refresh are unchanged.

This report narrows the [Phase1A evidence](v2-recovery-phase-01a-bundle-audit.md) and [Phase1B contract, reconciled with Phase1C](v2-recovery-phase-01b-implementation-contract.md). Its original recommendations are now reconciled with explicit [P1-D10–P1-D15 approvals](v2-ui-workflow-recovery-roadmap.md#phase-1d-approved-decisions-p1-d10p1-d15). Historical findings remain unchanged. The UI has one include box and field selector, not separate name/memo keyword groups: `hero,Attack` remains OR. Technical translation must preserve controls without inventing a hidden AND or a new UI.

## Phase 1D Approved Decisions P1-D10–P1-D15

**Recorded 2026-10-23 by explicit user approval.** All six approval questions are decided. Together with P1-D1 immutable occurrence identity, P1-D2 bundle-name/child-name/memo matching before pages, P1-D3 separate bundle memos, P1-D4 exact navigation, P1-D5 global sorting, P1-D6 actual-bundle eligibility, P1-D7 cross-asset matching, P1-D8 directory-history preservation and P1-D9 Name/Package-only headers, these complete the current design approval set. No implementation is authorized or complete.

| Approval | Current contract | Technical dependencies, not replacement product choices |
|---|---|---|
| P1-D10 / S1 | Any contained asset matching active exclusions vetoes the entire bundle, even if its name or another asset matches positively. Full source scope, before emission/sorting/pages; preserve parsing/comparison. Candidate-only recommendation **superseded** | Exact active exclusion-field scope; source/package metadata exclusions; incomplete evidence cannot prove unseen negatives |
| P1-D11 / S2 | Separate children may witness separate mandatory filters. AND mandatory requirements, keep include terms OR; no same-asset-only rule or unconditional OR. Exclusions independently follow P1-D10 | Precise identity-bound lookup and unspecified edge cases, never ambiguous synthesized identity |
| P1-D12 / S3 | Preserve supported fields, keywords/exact/substring/OR, Memo-only/type/Class ID/signed Path ID, result kinds and package scope; asset fields become child predicates, not bundle values | Explicit compatibility resolution if a result-kind cannot yield eligible bundles; no automatic redesign, evidence-toggle remap or discarded control |
| P1-D13 / N1 | Go to Assets opens original Unity source in filesystem browsing, exact immutable/live occurrence context, directory Back/Forward/Up intact. No indexed-metadata fallback; missing/invalid/ambiguous original fails clearly with state retained. Indexed-destination recommendation **superseded** | Exact locator/byte verification, safe original-file loader/directory context, native child provenance, no arbitrary unrelated asset selection |
| P1-D14 / N2 | Revalidate exact source/package/generation/revision even when Search stale; no blanket stale-navigation block, similar-name substitution or implicit Search rerun | Check captured owner/file before publication; reject unloaded/replaced/invalid target and preserve Assets |
| P1-D15 / N3 | Nonblocking navigation preserves pending/failed drafts and errors; no successful-save prerequisite, discard, false Saved state, modal save gate or memo UI redesign | Verify each transition retains shared draft ownership/error and existing retry contracts; document gaps rather than claim support |

The roadmap is authoritative. Earlier Phase1A/1B proposals inconsistent with these approvals are historical, not active requirements; their files are preserved without unrelated rewriting. Independent indexed browsing can still exist under P1-D4, but cannot serve as this action's fallback under P1-D13.

## 1. Current source evidence and complete control mapping

References used below:

- **V1:** [PackageSearchService.SearchAsync/MatchFields/TextMatches](../../UABS.UnityAssets/Packages/Search/PackageSearchService.cs) and [PackageSearchModels](../../UABS.UnityAssets/Packages/Search/PackageSearchModels.cs). Here V1 means the current Avalonia V1 backend, not the original Unity Search. The [Phase1A legacy comparison](v2-recovery-phase-01a-bundle-audit.md#2-historical-unity-workflow--code-evidence-not-runtime-acceptance) records Unity's different field-local excludes/exact-mode quirks; those are not restored.
- **V2:** [SearchQuery.Command](../../UABS.PackageV2/Search/SearchQuery.cs), [V2SearchService registration/UDFs](../../UABS.PackageV2/Search/V2SearchService.cs), [SearchSession](../../UABS.PackageV2/Search/SearchSession.cs) and [SearchContracts](../../UABS.PackageV2/Search/SearchContracts.cs).
- **Shared UI:** [SearchWorkspaceView controls](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml), [SearchWorkspaceViewModel.TryCreateRequest/MapFields/RunV2SearchAsync](../../UABS/Script/ViewModels/SearchWorkspaceViewModel.cs), [Enter/header handlers](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml.cs).

| Control | Confirmed current V1 / V2 behavior | Bundle proposal, scope and cross-asset interpretation |
|---|---|---|
| Include text / include OR | Both split English/Chinese commas, trim/remove empty terms, match any term in selected fields; case-insensitive literal matching, no query-language AND/NOT. Both normalize duplicate terms | Preserve one OR group across enabled source/child values. Different mandatory groups may use different children; do not reinterpret comma as AND |
| Exact name | Both use whole-field OrdinalIgnoreCase instead of substring for include/exclude selected text, not just names; no Unicode normalization, natural-sort or wildcard semantics implied | Preserve matching primitive/grouping. Exact type remains independently whole-field. No new approval question |
| Exclude | V1 MatchFields vetoes a candidate if any selected value matches; V2 `excluded=0` equivalent. Asset scope is selected name/memo values. Source scope includes selected source metadata/CAB/dependency evidence. Package scope is package fields | **P1-D10:** bundle-wide veto from any matching contained asset, independent of positive witnesses; original candidate-only recommendation superseded. Exact field scope remains a technical ambiguity |
| Search memos | Both add Memo to fields; does not make memo text mandatory or remove name fields | Preserve optional child-memo text evidence. Name OR memo in the same include group, not name AND memo. No genuine bundle memo reads/writes |
| Memo-only | Both reject whitespace/null/empty asset memos independently of keyword searching. V2 uses compatible memo_nonblank or authoritative fallback. Does not gate existing package/source branches | **P1-D11/P1-D12:** mandatory nonblank child-memo predicate, separate witness permitted; not a positive keyword or bundle-owned memo |
| Signed Path ID | Both exact Int64; no text include needed. Suppresses existing Package/SourceFile branches; when include exists, asset must also text-match | **P1-D11/P1-D12:** preserve signed asset predicate and independent witnesses; never source Path ID or proof of unique memo-writable target |
| Class ID / type name | V1 UI rejects these V2-only fields. V2 requires assets-only kinds; Class ID signed Int32 and type whole-field OrdinalIgnoreCase; AND gates on current asset. Filters alone do not bypass include-or-Path-ID validation | **P1-D11/P1-D12:** separate children may satisfy separate mandatory filters; preserve supported meanings and document validation/result-kind conflict. No filename type inference or same-asset requirement |
| Field selection: Names | Both PackageName + SourceName + AssetName; current branches do not inherit another branch's text matches | **P1-D12:** preserve supported field meanings; child-name predicates target contained assets; document any incompatible package-result mapping, not silently force branches |
| Paths | Both PackagePath + IndexDirectory + SourcePath | **P1-D12:** preserve supported metadata fields; technical paths remain in Inspector, not extra table columns |
| CAB codes | V1 bundle CAB; V2 contained logical-file CAB evidence | **P1-D12:** preserve supported evidence, never CAB identity or invented asset value |
| Assets | Both AssetName only | **P1-D12:** child-name predicates retain meaning; do not force incompatible field/result-kind mapping to simplify source output |
| Dependencies | V1 listed dependency strings; V2 raw original/virtual paths and raw CAB under logical files | **P1-D12:** preserve supported field meaning and both paths; source/dependency exclusion-field policy remains unspecified, not automatically inferred from the asset veto |
| All | All searchable fields except Memo unless Search memos enabled | Preserve field union; not unconditional positive match. No hidden bundle-owned memo |
| Package scope / name filter / Select All / Clear | Both use captured selected packages. Package name filter only changes picker visibility; Select All/Clear iterate **all PackageScopes**, including hidden entries. V2 scope change invalidates active operation; V1/V2 mixed execution is explicitly rejected | Preserve exactly; scope is a mandatory restriction, not text-match evidence. Hidden entries remain selected unless explicitly changed; no new decision |
| Packages / Sources / Assets result-kind boxes | Both currently choose independent emitted candidates; V2 source candidates do not test child asset names/memos; no separate logical-file result kind | **P1-D12:** preserve supported meanings; document conflict between independent row kinds and eligible bundle-only output for explicit resolution. Earlier evidence-branch toggle remap is not approved |
| Search / Enter / Cancel / criteria-changed indicator | Shared explicit execution; Enter in criteria text boxes; criteria edits retain last results with changed indicator. Requires include or Path ID and at least one result kind. V2 invalid class/type/mixed-format validation is explicit | Preserve validation floor, explicit execution and errors. Type-only/Memo-only/exclude-only empty query remains invalid; no invented enumerate-all default |
| Header sorting / Refresh / paging Retry | V1 native full-result sorting; V2 supported global sorting, bounded600/128 window/checkpoints. Catalog changes stale session; explicit Refresh, no stale cursor append; Retry is current valid page boundary | Future bundle Name/Package globally sorted, no extra headers under P1-D9. Preserve D5, cancellation, selection/drafts; no local grouping/sorting |

No source-specific input beyond the field modes above is present. There are no independent name/memo include boxes, source UUID text filter or image-stem search control. Programmatic V2 `AssetIdentity` is a precise occurrence lookup, not a free user filter; keep all its components conjunctively bound to the same asset. Cross-asset approval never permits combining halves of two identities or writing a memo to a witness chosen by name.

## 2. Approved conceptual predicate — pending technical translation

Preserve existing normalization and text matcher. Let `I` be the include OR group, `E` the exclude OR group, and `match(v,t)` the selected exact/substring OrdinalIgnoreCase comparison. NULL/empty text provides no keyword match; Memo-only uses non-whitespace semantics independently. Package scope and confirmed bundle kind are always gates, never positive evidence.

```text
For each source s in captured selected live package registrations:
  require reliable existing evidence that s is an actual bundle (P1-D6)
  retain full package/generation/source/revision + registration context

  -- Resolve exact field/result-kind compatibility explicitly under P1-D12.
  sourceWitness = enabled supported source evidence text-matches I
  assetWitness = any indexed child text-matches I in enabled child fields
  textOK = sourceWitness OR assetWitness
           (or true for validated Path-ID-only execution, not general empty search)

  gates = AND over every active mandatory condition:
    Path ID: some indexed child has requested signed ID
    Class ID: some indexed child has requested class
    Type name: some indexed child has requested exact type
    Memo-only: some indexed child has a non-whitespace memo
  -- Each gate may have a DIFFERENT child witness; gates are not OR.
  -- Identity-bound internal lookup remains one exact occurrence, not split gates.

  excluded = ANY contained asset matches an active exclusion
             -- full source, not only text/gate witnesses; field scope to specify
  emit one s iff textOK AND gates AND NOT excluded
  apply deterministic Name/Package ordering with immutable ties, then paginate
```

This model is **approved conceptually** under P1-D10/P1-D11/P1-D12, not implemented SQL or approval for unspecified mappings. Mandatory filters remain requirements distinct from positive OR; witnesses can differ. A triggered asset exclusion vetoes even a direct bundle-name match. Keep existing exclusion term parsing/comparison, but explicitly resolve active field scope and source/package/CAB/dependency exclusions rather than inventing rules. Result-kind meaning conflicts require explicit resolution; no automatic branch-toggle remapping. Package scope alone is never a positive match. Exact identity lookup remains indivisible and does not create bundle-owned asset values.

With existing input, `hero,Attack` remains a single OR group even with Search memos. P1-D7/P1-D11 do not authorize new controls, comma-as-AND or hidden keyword groups. P1-D12 preserves result-kind meanings; incompatible cases must be reported without emitting asset-valued bundle columns or silently redesigning controls.

**Incomplete data:** unknown/unreadable children are not invented witnesses. A detected asset exclusion still vetoes under P1-D10; no observed exclusion in a partial index is not proof of its absence in original bytes. Coverage/unknown-membership treatment is a technical dependency, not authority to weaken the veto or mark indexing complete. Empty-set/filter edge cases retain evidence limitations. Classification uncertainty remains ineligible under P1-D6 until reliable evidence exists; never infer kind from a name.

## 3. Compact truth table

Assume selected confirmed bundles, supported field/control mapping and complete evidence unless noted. Apply P1-D10 veto to every otherwise positive row, and P1-D11 mandatory gates independently. Match means one source row, never asset rows. Unspecified field/incomplete-set edge cases are not new approvals.

| Query / observed evidence | Outcome and authority |
|---|---|
| Include `hero`; bundle name `hero_bundle`, no matching child | **Approved:** bundle-name match under P1-D2; no additional child gates assumed |
| Include `hero`; unrelated bundle name, child name `hero` | **Approved:** child-name match, one bundle |
| Include `Attack`; Search memos on; only child memo contains it | **Approved:** child-memo match; no bundle-owned memo value |
| Independently required name `hero` and memo `Attack` occur on different children | **Approved principle:** allowed by P1-D7; expression of those separate required groups is not present/approved in current UI |
| Include `hero`, type `Texture2D`; different children supply name/type | **Approved P1-D11:** cross-asset mandatory filter witnesses allowed; any triggered P1-D10 exclusion still vetoes |
| Include `hero,Attack`, only `Attack` present in an enabled field | **Confirmed existing / proposed preserved:** match by OR; do not require both terms |
| Matching child A; unrelated child B matches active exclusion `debug` | **Approved P1-D10:** entire bundle excluded; A/bundle name cannot bypass veto |
| Memo-only checked; no stored nonblank memos; bundle name matches | **Approved conceptual P1-D11:** mandatory filter not satisfied; current source branch behavior is not proof of future implementation |
| Two source occurrences share identical displayed name, including across packages | **Approved:** two rows when each qualifies; never merge by name/hash |
| Empty indexed child set; bundle name matches; no mandatory child gate | **Proposed preserved behavior:** source-name evidence can match; empty/incomplete-set handling is not separately approved by P1-D7; coverage remains visible |
| Partial index, no observed positive witness | **Proposed evidence policy:** no emitted match; retain coverage warning, no claim of original-source absence |
| Include/exclude `hero`; contained name `hero` matches active exclusion field | **Approved P1-D10:** no match; exclusion veto, not another positive |
| Only source name matches exclude; child positive, no observed asset exclusion | **Technical ambiguity:** P1-D10 asset veto is settled, source-only exclusion-field policy is not invented here |
| Bundle name matches, requested Path ID absent | **Approved conceptual P1-D11:** mandatory condition fails; bundle-name positive is not a replacement for a filter |
| No include/Path ID; only type/Memo-only/excludes | **Confirmed existing / proposed preserved:** validation, not enumerate-all |

## 4. Navigation lifecycle: existing guarantees versus missing integration

Evidence: [MainWindowViewModel.SelectedWorkspaceIndex/LoadFileAsync/flush observers](../../UABS/Script/ViewModels/MainWindowViewModel.cs), [BrowserViewModel navigation/history](../../UABS/Script/ViewModels/BrowserViewModel.cs), [V2BrowseViewModel selection/loading/removal](../../UABS/Script/ViewModels/V2BrowseViewModel.cs), [V2BrowseCatalog registration checks](../../UABS/Script/Services/V2BrowseCatalog.cs), [PackageReader](../../UABS.PackageV2/PackageReader.cs), [EditableMemoInspectorViewModel](../../UABS/Script/ViewModels/EditableMemoInspectorViewModel.cs), [MemoDraftCoordinator](../../UABS.UnityAssets/Packages/Memos/MemoDraftCoordinator.cs).

Current Search XAML explicitly defers Show in Assets; no Go to Assets integration exists. Indexed browse is package-wide, not source-filtered. Directory history commits successful directory snapshots only, retains Refresh semantics and has independent Up. Existing mode commands select filesystem versus indexed view; opening a Unity source swaps native sessions only after successful/current loading, preserving the previous valid session on error. V2 verification failures can clear invalid metadata rows; this is not a transactional source-navigation implementation.

| Case | Existing guarantee | Proposed minimal outcome / missing work |
|---|---|---|
| Valid Go to Assets | Immutable source row key/registration evidence exist; action absent | **P1-D13:** resolve exact original Unity file, verify live context and source revision, use filesystem browsing/native loader; publish only if still current, no unrelated child selection |
| Directory Back/Forward/Up | Existing independent Browser directory stack | **Approved P1-D8:** leave it unchanged; retain extra source/return context only as needed, not a replacement history or new history UI |
| Repeated navigation / rapid selection | Load/page cancellation and generation guards exist | Reuse latest-request-wins guard; same current source can remain selected without duplicate directory history; no child auto-selection or retargeted draft |
| Filesystem/indexed switching | Long-lived state and mode commands exist | **P1-D13:** Go to Assets targets original-file filesystem mode; preserve retained independent indexed state and directory commands, not silent metadata routing |
| Original bundle missing | SQLite metadata independent of payload; native loader needs bytes | **P1-D13:** error and preserve current workspace/Assets; no indexed metadata fallback. Independently choosing existing indexed browse remains separate |
| Registration unloaded/replaced | Registration lifetime/epoch, package/generation and source revision evidence permit rejection; reader verifies current evidence | Reject original target, retain prior workspace where still valid; explain reload/reselect required. Never silently reopen same path or find same filename |
| Same-generation catalog change / stale Search | Exact verified memo deltas update asset rows; stale session blocks paging/sorting until explicit Refresh; ActivateAsync checks without rerun | **P1-D14:** exact live revalidation, not blanket block; keep Search stale and D5 explicit Refresh, reject invalid owner/file without retargeting |
| Package generation/source revision change | Identity contract invalidates old targets; pending drafts retain original identity | Reject, no fallback by label/path/hash. Fresh Search may establish new identity only via explicit execution |
| Pending/failed memo drafts | Shared coordinator stores per-identity drafts, serialized saves,1.5s debounce. Navigation flush attempts unsaved work; **failed drafts are not auto-retried** (`saveFailed:false`). Main shell switches before observed async flush finishes and does not inspect returned failed state | **P1-D15:** nonblocking navigation with retained pending/failed drafts/errors and current explicit Retry contract; no save-success prerequisite/modal gate. Verify the new transition retains drafts before claiming support |
| Identical names | Full source/package/generation/registration identities available | Always exact source scope; Inspector shows provenance; name/path alone never chooses target |
| Selection after explicit Refresh | Asset sort retention exists; source projection/refresh retention not implemented | Retain source selection by full immutable key only after current query proves membership; if unresolved/off-window do not substitute first/name match. Clear explicitly if absent; drafts survive outside selection |

**Pending implementation steps under P1-D13–P1-D15:** capture immutable key/registration and current Search/Assets context; resolve the exact original locator; check file availability, ambiguity and source-revision evidence; prepare normal filesystem-directory/file loading using existing cancellation/session ownership; revalidate captured owner/package generation/source revision after awaits; publish only a current successful destination. Any parent-directory selection/history integration must use the existing Browser path and preserve Back/Forward/Up, with rollback/nonpublication on failure; exact integration is an engineering dependency, not a new navigation policy. Do not commit a partial directory/workspace change before an unavailable file is detected. Do not require memo persistence to finish: retain captured drafts/errors and observe flush completion without retargeting or undoing a newer navigation. Return through existing workspace navigation preserves Search state and checks staleness without executing Search.

Do not attach a child memo editor to a source root. Explicit asset selection reuses the shared coordinator/editor-exclusion/transaction path. The native model lacks full original V2 ordinal/package association; path/hash/logical-name bridging can remain ambiguous. **P1-D13 requires native filesystem navigation despite that limitation:** preserve exact source context, document child memo-provenance work, never weaken memo identity validation or substitute indexed navigation. No bundle memo, payload preview or shutdown/crash-recovery feature is added.

### Failure feedback

Use existing status/error regions, no new dialog framework. Suggested wording, not new UI implementation: invalid/unloaded/replaced identity — “The selected bundle is no longer available in this loaded package.” Missing/invalid/ambiguous original — “The original Unity source could not be opened; the current Assets view was retained.” Stale but revalidated — “Original Unity source opened; Search remains stale. Refresh Search explicitly to continue results.” Failed memo retains existing Failed/Conflict/Retry and exact draft identity. No metadata fallback or automatic Search rerun. Cancellation/late completion publishes no destination; independently invalidated prior registrations may still show their existing unavailable state.

## 5. Approval questions resolved by P1-D10–P1-D15

All six questions are **decided**; the original recommendations are not current authority. In particular, candidate-scoped exclusions and indexed-metadata navigation are explicitly superseded. No settled decision is reopened. Historical code evidence above still describes the unmodified application.

| Audit question | Approved disposition | Superseded proposal / remaining technical constraint | Checkpoint |
|---|---|---|---|
| S1 | **P1-D10:** entire bundle veto from any matching contained asset | Candidate-only exclusion superseded; exact active field scope/source-only exclusions need technical clarification | CP1–3 |
| S2 | **P1-D11:** mandatory conjunction, separate child witnesses allowed | No direct-name replacement for mandatory filters or same-asset-only condition; identity/empty/incomplete evidence details remain | CP1–3 |
| S3 | **P1-D12:** preserve supported field/result-kind meanings and child predicates | Evidence-toggle redesign not approved; independent Package/Asset row-kind conflict must be reported explicitly | CP1/3 |
| N1 | **P1-D13:** original Unity filesystem destination, missing/invalid/ambiguous fails with state retained | Indexed primary/fallback recommendation superseded; exact loader/directory/provenance integration pending | CP4/5 |
| N2 | **P1-D14:** exact revalidation even if Search stale; no implicit refresh | Blanket stale-result block not chosen; registration/source invalidation still fails | CP5 |
| N3 | **P1-D15:** nonblocking navigation preserves drafts/errors | Blocking save gate not chosen; verify draft retention on each new transition | CP5 |

Classification, Name/Package headers, Unicode/comma-OR/exact identity, directory commands and D5 remain determined. Full bundle veto is now independently approved; partial-index warnings must not claim unseen negatives. Remaining compatibility/field/evidence dependencies are explicit, not grounds to replace the chosen behavior.

## 6. Six-checkpoint readiness and tests

No implementation checkpoint has started or completed. P1-D1–P1-D15 complete current design approvals; implementation still needs a bounded request and treatment of technical compatibility gaps. CP1→CP2→CP3 applies approved Search semantics; CP4→CP5 must use original Unity filesystem navigation, not the historical indexed destination. CP6 remains separately scoped, including non-bundle inventory policy where affected; bundle memos remain separate.

| Phase1B checkpoint | Exact remaining prerequisite / focused tests for future implementation |
|---|---|
| CP1 typed source/criteria contract | Encode P1-D10–P1-D12; resolve exclusion-field/result-kind compatibility explicitly; test scope/OR/exact/signed IDs, separate mandatory witnesses, unrelated excluded-child veto, no positive bypass, identity-bound targeting |
| CP2 source query/federation/keyset | CP1; truth table, duplicates across packages, >600 source matches, partial/empty bundles, Name/Package ties, cancellation/revisions, budget failures; disposable multi-source performance plans, no assumption asset fast path applies |
| CP3 Search binding/headers/window | CP2; actual header clicks/arrows, exact two columns, controls retained per S3,600/128 eviction/replay, stale pages, source selection/drafts, V1 native regression |
| CP4 original-source filesystem Assets integration | P1-D13; exact original locator/revision/native loader provenance, same-name sources/packages, missing/invalid/ambiguous original preserves prior state with no metadata fallback, no unrelated child selection, retained indexed state |
| CP5 exact navigation/lifecycle | CP3/4 and P1-D14/P1-D15; repeated/rapid navigation, unload/generation/revision replacement during await, stale-valid revalidation, pending/failed draft retention without save gate, directory Back/Forward/Up unchanged, explicit Refresh/return anchor |
| CP6 Packages inventory/authorized Assets columns | Typed source browse and explicit UI slice; workspace row-unit separation, exact memo publication/dirty-editor guards, technical Inspector, unchanged shared styles; non-bundle inventory decision if affected |

Future implementation requires normal risk-based tests, Debug startup build and user-operated visual acceptance, not desktop automation. This documentation audit neither runs them nor claims their results. Genuine bundle memo schema/storage/conflict/UI remains a separate milestone.

## Audit validation and limitations

Source inspection confirms current behavior, not rendered UI acceptance. Check this file and roadmap/handoff references for local links/anchors, Markdown fences, conflicts/whitespace and three-document-only scope. No application source, XAML, tests, SQLite schema, real packages, original bundles or Unity files changed. No builds/tests/desktop actions or Git staging/commit. Preserve all17 Reported issues and approved decisions.

**Unity-Exclusive Features Requiring Decisions:** no new Unity-runtime replacement requirement found; the proposed metadata/query/navigation behavior stays standalone. Existing preview and legacy runtime replacements remain outside this audit.
