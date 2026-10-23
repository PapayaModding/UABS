# V2 Recovery Phase 1D — Search Semantics and Navigation Lifecycle

**Date:** 2026-10-23. **Baseline:** `f6183cf`, clean index/worktree at entry. **Mode:** focused source audit/documentation only; no code changes, package access, builds, tests or desktop execution. All17 recovery issues remain Reported/unresolved.

## Authority and findings

[P1-D1–P1-D5](v2-ui-workflow-recovery-roadmap.md#phase-1-approved-decisions) and [P1-D6–P1-D9](v2-ui-workflow-recovery-roadmap.md#phase-1c-approved-decisions-p1-d6p1-d9) remain binding. Search returns one confirmed bundle occurrence; different children may satisfy different conditions; aggregate before global sorting/paging. Exact Go to Assets preserves directory Back/Forward/Up. Bundle Search shows exactly Name and Package; technical identity stays in Inspector/internal context. Bundle-owned memos remain separate. V2 D1–D8/schema and D5 explicit Refresh are unchanged.

This report narrows the [Phase1A evidence](v2-recovery-phase-01a-bundle-audit.md) and [Phase1B contract, reconciled with Phase1C](v2-recovery-phase-01b-implementation-contract.md). It proposes detailed semantics, **not new approvals**. The current UI has one include box and a field selector, not independent name/memo keyword groups. Thus `hero,Attack` means OR today; P1-D7's name AND memo example cannot be inferred from that input. Cross-asset permission is settled; how existing controls express conditions still needs decisions.

## 1. Current source evidence and complete control mapping

References used below:

- **V1:** [PackageSearchService.SearchAsync/MatchFields/TextMatches](../../UABS.UnityAssets/Packages/Search/PackageSearchService.cs) and [PackageSearchModels](../../UABS.UnityAssets/Packages/Search/PackageSearchModels.cs). Here V1 means the current Avalonia V1 backend, not the original Unity Search. The [Phase1A legacy comparison](v2-recovery-phase-01a-bundle-audit.md#2-historical-unity-workflow--code-evidence-not-runtime-acceptance) records Unity's different field-local excludes/exact-mode quirks; those are not restored.
- **V2:** [SearchQuery.Command](../../UABS.PackageV2/Search/SearchQuery.cs), [V2SearchService registration/UDFs](../../UABS.PackageV2/Search/V2SearchService.cs), [SearchSession](../../UABS.PackageV2/Search/SearchSession.cs) and [SearchContracts](../../UABS.PackageV2/Search/SearchContracts.cs).
- **Shared UI:** [SearchWorkspaceView controls](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml), [SearchWorkspaceViewModel.TryCreateRequest/MapFields/RunV2SearchAsync](../../UABS/Script/ViewModels/SearchWorkspaceViewModel.cs), [Enter/header handlers](../../UABS/Script/View/Pages/SearchWorkspaceView.axaml.cs).

| Control | Confirmed current V1 / V2 behavior | Bundle proposal, scope and cross-asset interpretation |
|---|---|---|
| Include text / include OR | Both split English/Chinese commas, trim/remove empty terms, match any term in selected fields; case-insensitive literal matching, no query-language AND/NOT. Both normalize duplicate terms | Preserve one OR group across enabled source/child values. Different mandatory groups may use different children; do not reinterpret comma as AND |
| Exact name | Both use whole-field OrdinalIgnoreCase instead of substring for include/exclude selected text, not just names; no Unicode normalization, natural-sort or wildcard semantics implied | Preserve matching primitive/grouping. Exact type remains independently whole-field. No new approval question |
| Exclude | V1 MatchFields vetoes a candidate if any selected value matches; V2 `excluded=0` equivalent. Asset scope is selected name/memo values. Source scope includes selected source metadata/CAB/dependency evidence. Package scope is package fields | **S1:** recommend candidate rejection, not bundle-wide NOT EXISTS over all assets. A rejected child cannot witness a positive/gate; unrelated rejected siblings do not veto others. Direct source candidate rejection does not automatically reject a surviving child branch |
| Search memos | Both add Memo to fields; does not make memo text mandatory or remove name fields | Preserve optional child-memo text evidence. Name OR memo in the same include group, not name AND memo. No genuine bundle memo reads/writes |
| Memo-only | Both reject whitespace/null/empty asset memos independently of keyword searching. V2 uses compatible memo_nonblank or authoritative fallback. Does not gate existing package/source branches | **S2:** propose a mandatory existential nonblank child memo gate, separate from text witness. Can be a different child; does not itself become a positive keyword. No memos means false even for bundle-name hit under this proposal |
| Signed Path ID | Both exact Int64; no text include needed. Suppresses existing Package/SourceFile branches; when include exists, asset must also text-match | **S2:** propose a mandatory child-existence gate, independently witnessed from text/type. Never source Path ID. Multiple children with same scalar ID can witness existence, but this does not make them memo-writable |
| Class ID / type name | V1 UI rejects these V2-only fields. V2 requires assets-only kinds; Class ID signed Int32 and type whole-field OrdinalIgnoreCase; AND gates on current asset. Filters alone do not bypass include-or-Path-ID validation | **S2/S3:** source projection requires deliberate validation adaptation. Propose independent existential gates; paired Class/type consistency versus separate witnesses needs explicit approval. Do not silently ignore filters or replace unknown type with filename inference |
| Field selection: Names | Both PackageName + SourceName + AssetName; current branches do not inherit another branch's text matches | **S3:** recommend selected package/source metadata may directly qualify each eligible source; child names qualify through witnesses. Preserve field toggles rather than always forcing every name field |
| Paths | Both PackagePath + IndexDirectory + SourcePath | **S3:** direct metadata branch, no child-name matching unless added by other enabled fields; technical paths stay out of table columns |
| CAB codes | V1 bundle CAB; V2 contained logical-file CAB evidence | **S3:** source-level evidence branch; keep V2 original logical relationships, never CAB identity |
| Assets | Both AssetName only | **S3:** child-name text only under recommended field-preserving mapping; explicitly decide interaction with P1-D2 bundle-name branch, do not silently override selector |
| Dependencies | V1 listed dependency strings; V2 raw original/virtual paths and raw CAB under logical files | **S3:** source-level evidence, retaining both V2 path strings. Existing V2 any-excluded dependency veto of source candidate is preserved under S1, not extended to every child |
| All | All searchable fields except Memo unless Search memos enabled | Preserve field union; not unconditional positive match. No hidden bundle-owned memo |
| Package scope / name filter / Select All / Clear | Both use captured selected packages. Package name filter only changes picker visibility; Select All/Clear iterate **all PackageScopes**, including hidden entries. V2 scope change invalidates active operation; V1/V2 mixed execution is explicitly rejected | Preserve exactly; scope is a mandatory restriction, not text-match evidence. Hidden entries remain selected unless explicitly changed; no new decision |
| Packages / Sources / Assets result-kind boxes | Both currently choose independent emitted candidates; V2 source candidates do not test child asset names/memos; no separate logical-file result kind | **S3:** recommend these select evidence branches for bundle projection (package/source/child), never extra displayed row kinds. This is a user-visible change requiring approval; preserve V1 old behavior and do not remove boxes silently |
| Search / Enter / Cancel / criteria-changed indicator | Shared explicit execution; Enter in criteria text boxes; criteria edits retain last results with changed indicator. Requires include or Path ID and at least one result kind. V2 invalid class/type/mixed-format validation is explicit | Preserve validation floor, explicit execution and errors. Type-only/Memo-only/exclude-only empty query remains invalid; no invented enumerate-all default |
| Header sorting / Refresh / paging Retry | V1 native full-result sorting; V2 supported global sorting, bounded600/128 window/checkpoints. Catalog changes stale session; explicit Refresh, no stale cursor append; Retry is current valid page boundary | Future bundle Name/Package globally sorted, no extra headers under P1-D9. Preserve D5, cancellation, selection/drafts; no local grouping/sorting |

No source-specific input beyond the field modes above is present. There are no independent name/memo include boxes, source UUID text filter or image-stem search control. Programmatic V2 `AssetIdentity` is a precise occurrence lookup, not a free user filter; keep all its components conjunctively bound to the same asset. Cross-asset approval never permits combining halves of two identities or writing a memo to a witness chosen by name.

## 2. Formal proposal — pending S1–S3 approval

Preserve existing normalization and text matcher. Let `I` be the include OR group, `E` the exclude OR group, and `match(v,t)` the selected exact/substring OrdinalIgnoreCase comparison. NULL/empty text provides no keyword match; Memo-only uses non-whitespace semantics independently. Package scope and confirmed bundle kind are always gates, never positive evidence.

```text
For each source s in captured selected live package registrations:
  require reliable existing evidence that s is an actual bundle (P1-D6)
  retain full package/generation/source/revision + registration context

  sourceWitness = enabled source/package/CAB/dependency evidence text-matches I
                  and its candidate is not excluded in selected source fields
  usableChildren = indexed children not excluded in their selected name/memo fields
  assetWitness = any usable child text-matches I in enabled child fields
  textOK = sourceWitness OR assetWitness
           (or true for validated Path-ID-only execution, not general empty search)

  gates = AND over every active mandatory condition:
    Path ID: some usable child has requested signed ID
    Class ID: some usable child has requested class
    Type name: some usable child has requested exact type
    Memo-only: some usable child has a non-whitespace memo
  -- Each gate may have a DIFFERENT child witness; gates are not OR.
  -- Identity-bound internal lookup remains one exact occurrence, not split gates.

  emit one s iff textOK AND gates
  apply deterministic Name/Package ordering with immutable ties, then paginate
```

This is a **recommended model**, not approved executable pseudocode. S2 decides bundle-name hits versus gates and Class/type/Path-ID witness grouping; S3 decides field/result-kind branch eligibility. Under S1, excluded children are ineligible witnesses, not positive evidence or an automatic bundle veto. Keep existing source-candidate exclusion behavior for selected CAB/dependency values; do not silently broaden that into an exclusion of all child branches. A selected package's matching name is text evidence only if the approved field/branch mapping permits it; being in scope alone never matches.

With existing input, `hero,Attack` remains a single OR group, even when Search memos is enabled. P1-D7 allows independently required name and memo groups to use different children **if such groups are separately specified**, but does not authorize new controls, reinterpretation of comma, or an invisible AND. S3 must resolve existing result-kind semantics without exposing asset results contrary to P1-D9.

**Incomplete data:** evaluate only positively observed metadata; unknown/unreadable children are not invented witnesses. A direct-name hit can survive an empty child set if no mandatory child gate applies. Display coverage warnings; a failed existential test says no indexed witness was found, not that the original bundle lacks one. Do not treat incomplete data as complete, or use missing evidence as proof of a bundle-wide negative. No new bundle-wide exclusion policy is implied. Classification uncertainty is ineligible under P1-D6 until reliable evidence exists; do not infer kind from a name.

## 3. Compact truth table

Assume selected confirmed bundles, Names mode with source/asset evidence enabled unless noted. **Approved** rows follow P1-D1–P1-D9 directly; **Proposed** rows depend on S1–S3. Match means one source row, never asset rows.

| Query / observed evidence | Outcome and authority |
|---|---|
| Include `hero`; bundle name `hero_bundle`, no matching child | **Approved:** bundle-name match under P1-D2; no additional child gates assumed |
| Include `hero`; unrelated bundle name, child name `hero` | **Approved:** child-name match, one bundle |
| Include `Attack`; Search memos on; only child memo contains it | **Approved:** child-memo match; no bundle-owned memo value |
| Independently required name `hero` and memo `Attack` occur on different children | **Approved principle:** allowed by P1-D7; expression of those separate required groups is not present/approved in current UI |
| Include `hero`, type `Texture2D`; different children supply name/type | **Approved principle:** cross-asset qualification allowed; proposed S2 provides precise gating/exclusion rules |
| Include `hero,Attack`, only `Attack` present in an enabled field | **Confirmed existing / proposed preserved:** match by OR; do not require both terms |
| Matching child A; unrelated child B has excluded `debug` | **Proposed S1:** A still witnesses match; B is rejected, no bundle-wide veto |
| Memo-only checked; no stored nonblank memos; bundle name matches | **Proposed S2:** no match because mandatory gate fails; current source candidates are not gated this way |
| Two source occurrences share identical displayed name, including across packages | **Approved:** two rows when each qualifies; never merge by name/hash |
| Empty indexed child set; bundle name matches; no mandatory child gate | **Proposed preserved behavior:** source-name evidence can match; empty/incomplete-set handling is not separately approved by P1-D7; coverage remains visible |
| Partial index, no observed positive witness | **Proposed evidence policy:** no emitted match; retain coverage warning, no claim of original-source absence |
| Include/exclude `hero`; only matching child has name `hero`, no source hit | **Proposed S1:** no match; exclusion rejects witness, it is not another positive |
| Source name matches include and exclude; nonexcluded child matches include | **Proposed S1:** child branch can qualify; source branch veto is not bundle-wide NOT |
| Bundle name matches, requested Path ID absent | **Proposed S2:** no match; bundle-name OR does not bypass mandatory restrictions |
| No include/Path ID; only type/Memo-only/excludes | **Confirmed existing / proposed preserved:** validation, not enumerate-all |

## 4. Navigation lifecycle: existing guarantees versus missing integration

Evidence: [MainWindowViewModel.SelectedWorkspaceIndex/LoadFileAsync/flush observers](../../UABS/Script/ViewModels/MainWindowViewModel.cs), [BrowserViewModel navigation/history](../../UABS/Script/ViewModels/BrowserViewModel.cs), [V2BrowseViewModel selection/loading/removal](../../UABS/Script/ViewModels/V2BrowseViewModel.cs), [V2BrowseCatalog registration checks](../../UABS/Script/Services/V2BrowseCatalog.cs), [PackageReader](../../UABS.PackageV2/PackageReader.cs), [EditableMemoInspectorViewModel](../../UABS/Script/ViewModels/EditableMemoInspectorViewModel.cs), [MemoDraftCoordinator](../../UABS.UnityAssets/Packages/Memos/MemoDraftCoordinator.cs).

Current Search XAML explicitly defers Show in Assets; no Go to Assets integration exists. Indexed browse is package-wide, not source-filtered. Directory history commits successful directory snapshots only, retains Refresh semantics and has independent Up. Existing mode commands select filesystem versus indexed view; opening a Unity source swaps native sessions only after successful/current loading, preserving the previous valid session on error. V2 verification failures can clear invalid metadata rows; this is not a transactional source-navigation implementation.

| Case | Existing guarantee | Proposed minimal outcome / missing work |
|---|---|---|
| Valid Go to Assets | Immutable source row key and registration/revision evidence exist, but action/source filter absent | **N1:** capture exact source key/context, resolve/load source-scoped indexed list, publish only if still current; select source root, no arbitrary asset |
| Directory Back/Forward/Up | Existing independent Browser directory stack | **Approved P1-D8:** leave it unchanged; retain extra source/return context only as needed, not a replacement history or new history UI |
| Repeated navigation / rapid selection | Load/page cancellation and generation guards exist | Reuse latest-request-wins guard; same current source can remain selected without duplicate directory history; no child auto-selection or retargeted draft |
| Filesystem/indexed switching | Long-lived state and mode commands exist | Preserve filesystem path/history/native session; Go to Assets changes indexed source context only under N1, normal mode switch still restores retained mode state |
| Original bundle missing | SQLite metadata independent of payload; native loader needs bytes | **N1:** show exact indexed source assets with metadata-only/missing-payload feedback; no inferred source remap or fake successful native load |
| Registration unloaded/replaced | Registration lifetime/epoch, package/generation and source revision evidence permit rejection; reader verifies current evidence | Reject original target, retain prior workspace where still valid; explain reload/reselect required. Never silently reopen same path or find same filename |
| Same-generation catalog change / stale Search | Exact verified memo deltas update asset rows; stale session blocks paging/sorting until explicit Refresh; ActivateAsync checks without rerun | **N2:** distinguish stale membership snapshot from invalid identity; recommend exact revalidation may navigate but stale Search remains stale. Never automatically refresh/paginate |
| Package generation/source revision change | Identity contract invalidates old targets; pending drafts retain original identity | Reject, no fallback by label/path/hash. Fresh Search may establish new identity only via explicit execution |
| Pending/failed memo drafts | Shared coordinator stores per-identity drafts, serialized saves,1.5s debounce. Navigation flush attempts unsaved work; **failed drafts are not auto-retried** (`saveFailed:false`). Main shell switches before observed async flush finishes and does not inspect returned failed state | **N3:** preserve current nonblocking navigation style with retained draft/error, explicit Retry; never discard or silently retry. If blocking instead is chosen, await/result handling must be explicitly implemented, not assumed existing |
| Identical names | Full source/package/generation/registration identities available | Always exact source scope; Inspector shows provenance; name/path alone never chooses target |
| Selection after explicit Refresh | Asset sort retention exists; source projection/refresh retention not implemented | Retain source selection by full immutable key only after current query proves membership; if unresolved/off-window do not substitute first/name match. Clear explicitly if absent; drafts survive outside selection |

**Proposed transition:** capture immutable key + Search return/window state → resolve live owner → prepare source-scoped page → revalidate owner/source after awaits → publish destination atomically if still current. Coordinate captured outgoing memo identities without changing their ownership. If N3 allows nonblocking flush, don't interpret later failure as successful persistence or undo a newer navigation. Directory history is never modified as a side effect. Return through existing workspace navigation keeps criteria, package scope, source selection, window anchor and draft; ActivateAsync may mark stale but does not execute Search.

Do not attach a child memo editor to a source root. When user explicitly selects an exact asset, reuse the existing shared coordinator/editor-exclusion/transaction path. The native model still lacks full original V2 ordinal/package association; path/hash/logical-name bridging can remain ambiguous. N1's indexed default avoids pretending that native load alone proves memo provenance. No bundle memo, source payload preview or shutdown/crash-recovery feature is added.

### Failure feedback

Use existing status/error regions; no new dialog framework. Invalid/unloaded/replaced identity: “The selected bundle is no longer available in this loaded package. Reload/reselect, then Search explicitly.” Stale but still valid: “Search results are stale; exact indexed source opened. Refresh Search before continuing results.” Missing payload: “Indexed metadata only; original bundle unavailable.” Failed memo: retain existing Failed/Conflict and Retry with the captured asset draft. Never promise persistence or corpus completeness. Cancellation/late completion publishes no destination and preserves prior valid state; an independently invalidated prior registration may still show its existing unavailable state.

## 5. Only remaining user-visible decisions

These are recommendations, **not P1-D10+ approvals**. No settled P1-D1–P1-D9 question is reopened.

| ID / specific question | Recommended default | Meaningful alternative | Evidence / compatibility / checkpoint |
|---|---|---|---|
| S1: what does exclusion reject after aggregation? | Reject the individual source/asset candidate in its selected fields; excluded children cannot witness gates; unrelated sibling doesn't veto bundle | Any excluded child vetoes whole bundle, or exclusion applies only to text witnesses rather than all gate witnesses | Existing candidate-wide behavior supports recommendation; whole-bundle negation changes membership. CP1–3 |
| S2: do mandatory child gates apply to direct-name hits, and can each gate use different children? | AND all active gates after name/source OR; independent existential witnesses for Path ID/class/type/Memo-only; exact identity lookup stays indivisible | Direct bundle-name bypass for child gates; or explicitly paired Class/type/Path-ID groups while permitting separate name/memo witnesses | Existing asset gates are mandatory, but source branches were ungated. P1-D7 permits crossing, not this full Boolean contract. CP1–3 |
| S3: how do existing fields/result-kind boxes map to one bundle result? | Preserve selected fields; map Package/Source/Asset boxes to package/source/child evidence branches, never additional displayed row kinds; keep comma OR and include-or-Path-ID validation | Separately approved bundle mode replacing/locking obsolete kind controls; alternatively always-on bundle-name branch despite selected field mode | No independent name/memo groups exist. Mapping changes emitted-unit behavior and Class/type validation; cannot silently remove controls or force field participation. CP1/3 |
| N1: what is the primary Assets destination? | Source-filtered indexed metadata, exact source selected, no child selected; works with missing payload | Verified native source load when available, indexed fallback when absent, with explicit provenance gap handling | Reader currently package-wide; native model lacks full V2 association. No new directory-history meaning. CP4/5 |
| N2: may stale-but-still-valid Search identities navigate? | Yes after exact live revalidation; preserve stale banner and require explicit Refresh for results paging/sort | Block Go to Assets until explicit Refresh | D5 mandates stale paging restriction, not navigation ban. Unload/replacement always rejects either way. CP5 |
| N3: must pending/failed memo save block Go to Assets? | Nonblocking captured flush, retain failed/conflicted draft/error and explicit Retry; no auto-retry of already-failed draft | Await flush and block navigation on unresolved failure, preserving current selection and offering existing Save Now/Retry | Current shell already switches then observes flush; coordinator retains draft and skips failed retry. Blocking needs new result-aware transition. CP5 |

Classification by reliable kind, Name/Package-only columns, Unicode matching, comma OR, exact identity safety, directory command meanings and no implicit Search refresh are already determined; do not manufacture additional approval questions for them. Partial-index results must report evidence limitations under either exclusion choice, never claim unseen negatives.

## 6. Six-checkpoint readiness and tests

No implementation checkpoint has started or completed in this task. After S1–S3 approval, CP1→CP2→CP3 can proceed in separately authorized slices; after N1–N3 approval, CP4→CP5 can follow. CP6 remains separately scoped, including any non-bundle Packages inventory policy and requested Assets columns; its readiness does not authorize bundle memo storage.

| Phase1B checkpoint | Exact remaining prerequisite / focused tests for future implementation |
|---|---|
| CP1 typed source/criteria contract | Freeze S1–S3; test field/kind combinations, include OR, exact Unicode, scope, signed IDs, rejected invalid inputs, cross-asset witnesses and indivisible identity targeting |
| CP2 source query/federation/keyset | CP1; truth table, duplicates across packages, >600 source matches, partial/empty bundles, Name/Package ties, cancellation/revisions, budget failures; disposable multi-source performance plans, no assumption asset fast path applies |
| CP3 Search binding/headers/window | CP2; actual header clicks/arrows, exact two columns, controls retained per S3,600/128 eviction/replay, stale pages, source selection/drafts, V1 native regression |
| CP4 source-scoped Assets | N1 plus exact source contract; multi-logical-file/all-class filtering, missing payload, duplicate labels, page boundaries, no child auto-selection, retained filesystem and Packages state |
| CP5 exact navigation/lifecycle | CP3/4 and N2/N3; repeated/rapid navigation, unload/replacement during await, stale-valid versus invalid, pending/failed drafts, directory Back/Forward/Up unchanged, refresh membership/return anchor retention |
| CP6 Packages inventory/authorized Assets columns | Typed source browse and explicit UI slice; workspace row-unit separation, exact memo publication/dirty-editor guards, technical Inspector, unchanged shared styles; non-bundle inventory decision if affected |

Future implementation requires normal risk-based tests, Debug startup build and user-operated visual acceptance, not desktop automation. This documentation audit neither runs them nor claims their results. Genuine bundle memo schema/storage/conflict/UI remains a separate milestone.

## Audit validation and limitations

Source inspection confirms current behavior, not rendered UI acceptance. Check this file and roadmap/handoff references for local links/anchors, Markdown fences, conflicts/whitespace and three-document-only scope. No application source, XAML, tests, SQLite schema, real packages, original bundles or Unity files changed. No builds/tests/desktop actions or Git staging/commit. Preserve all17 Reported issues and approved decisions.

**Unity-Exclusive Features Requiring Decisions:** no new Unity-runtime replacement requirement found; the proposed metadata/query/navigation behavior stays standalone. Existing preview and legacy runtime replacements remain outside this audit.
