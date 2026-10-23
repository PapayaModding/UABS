# V2 UI and Workflow Recovery Roadmap

**Recorded:** 2026-10-23. **Baseline:** `4bb4bc8` (Gate5G.1 header sorting).
**Purpose:** preserve user-reported requirements across sessions. This is a documentation-only recovery plan, not implementation authorization or evidence that fixes have shipped.

## Evidence and scope

All 17 items below start **Reported / Not yet resolved under this roadmap**. Observations are user reports, not fresh agent desktop observations or independently confirmed root causes. Verify the affected source paths and actual Debug application before implementation. Historical build/test success does not establish manual acceptance of these requirements.

Related sources of truth: [AI policy](../01_core/ai-agent.md), [current UI/workflow design](../03_design/ui-and-workflow-design.md), [approved V2 design](../03_design/package-format-v2-design.md), [Gate5G/5G.1 evidence](package-format-v2-gate-05g-search-paging-performance.md), [migration roadmap](migration-roadmap.md), and [session handoff](codex-session-handoff.md).

[Phase1A bundle identity and memo audit](v2-recovery-phase-01a-bundle-audit.md) traces the legacy bundle-returning workflow and current source/asset contracts for #1/#2/#7/#3. Confirmed: source-level projection is representable without a schema change, but no existing bundle-owned memo storage exists. The five product questions are now decided by [P1-D1–P1-D5](#phase-1-approved-decisions); detailed implementation dependencies remain below. Neither the audit nor this approval record implements a fix or changes any of the17 Reported issue statuses.

The current implementation intentionally includes individual indexed assets, per-asset memos, V1 support, and restricted V2 global sorting. The requirements below propose changes to those boundaries; they do not rewrite historical reports or establish that existing design decisions were implementation defects. In particular, current UI documentation describes Search-to-Assets as deferred; calling for its restoration records the desired legacy workflow, not proof that a previously implemented Avalonia action was removed.

[Phase1B implementation contract audit](v2-recovery-phase-01b-implementation-contract.md) maps actual controls, source eligibility, pre-page aggregation, global sorting, exact navigation/history and workspace/model boundaries. Before implementation, approve B1 source-kind/status inclusion, B2 existing-control and direct-source/child predicate interactions, B3 navigation/history/stale/failed-draft policies, and B4 source-header mappings. Its six dependency-ordered checkpoints are proposals, not implementation authorization; P1-D1–P1-D5 remain binding and all17 issues remain Reported.

## Mandatory compatibility contract

**Existing functionality and UI behavior are protected by default.** For every implementation:

- Inspect affected workflows and identify comparable workspaces before editing.
- Preserve unrequested controls, commands, columns, layouts, defaults and navigation. Never disable an existing feature to simplify a new implementation, or silently add extra columns/actions.
- Cross-workspace consistency does not authorize changing every workspace. Obtain explicit authorization for the implementation slice; do not broaden it automatically.
- Report conflicts requiring a design decision before changing behavior. An issue listed here is not permission to change schema, persistence, identity or previously approved contracts.
- Test existing as well as new behavior, keep changes small/reviewable, and report user-visible behavior added, removed, preserved and intentionally changed.
- Require user-operated **Debug UI acceptance** for visible changes, separately from automated verification. Existing desktop-control restrictions remain in force.

Preserve immutable identities, safe session ownership, cancellation/stale-result protection, bounded memory, exact memo transactions/drafts, zero Unity-runtime dependencies, real packages/source files and existing Git work. Approved D5 explicit Refresh remains in force unless separately changed with approval.

## Status register

Priority is proposed: **P0** core workflow, **P1** interaction/reliability or retirement gate, **P2** usability/polish. These labels are not scheduling or implementation approval. Phase numbers refer to the proposed sequence below.

| Issue | Requirement | Priority | Phase | Status |
|---|---|---|---|---|
| #1 | Bundle-level Search results | P0 | 1 | Reported |
| #2 | Search → Assets navigation | P0 | 1 | Reported |
| #3 | Correct Assets columns and add Memo | P0 | 1 | Reported |
| #4 | Consistent infinite scrolling | P1 | 2 | Reported |
| #5 | Rapid mouse-wheel stalls | P1 | 2 | Reported |
| #6 | Complete appropriate global Name/Memo sorting | P1 | 2 | Reported |
| #7 | Bundle-level Packages Indexed Assets presentation | P0 | 1 | Reported |
| #8 | Shared scrollable-grid interaction infrastructure | P1 | 2 | Reported |
| #9 | Complete V1 retirement after parity approval | P1 | 4–5 | Reported |
| #10 | Selectable/copyable static text | P2 | 3 | Reported |
| #11 | Investigate redundant Filesystem Assets button | P2 | 3 | Reported |
| #12 | Icon-only Assets navigation actions | P2 | 3 | Reported |
| #13 | Rename Choose... to match picker behavior | P2 | 3 | Reported |
| #14 | Criteria & Search Fields above Package Scope | P2 | 3 | Reported |
| #15 | Native Windows file icon | P2 | 3 | Reported |
| #16 | Consistent subtle header separators | P2 | 3 | Reported |
| #17 | Duplicate Detailed Package View introduction | P2 | 3 | Reported |

## Issue requirements and acceptance

### #1 — Search displays the wrong results

- **Reported:** Search exposes individual asset rows instead of the intended matching asset bundles.
- **Intended/scope:** each displayed result represents the appropriate bundle-level identity. Asset metadata may identify matching bundles internally; do not discard the underlying asset index or substitute an arbitrary matching asset as the bundle's identity.
- **Priority/dependencies:** P0; P1-D1/P1-D2 decide source-occurrence identity, distinct occurrences and match aggregation; specify remaining source-kind/control/query details. Bundle-memo design is separately deferred by P1-D3. Coordinate #2, #6 and #7.
- **Acceptance:** representative queries display the approved bundle units, preserve package/source provenance and matching evidence, and avoid accidental duplicates or omissions. Paging, global ordering and selection use the approved bundle identity, not name/CAB/Path ID alone. Match-membership rules are approved and regression-tested before changing results.

### #2 — Search → Assets navigation is missing

- **Reported:** the expected action to open a selected bundle in Assets is unavailable.
- **Intended/scope:** restore that workflow, preserving relevant selection and navigation context through the existing Assets loader/session system; do not introduce another navigation system.
- **Priority/dependencies:** P0; #1's exact bundle identity, source availability and the existing safe loading/ownership contract. Confirm the action/gesture before adding or repurposing a control.
- **Acceptance:** the correct bundle opens in Assets; duplicate names cannot misroute it. Search criteria, scope, selected result, scroll and drafts survive workspace switching. Missing/unavailable sources and load failures produce clear feedback without destroying a valid session; pending work cannot retarget a newer selection.

### #3 — Incorrect Assets columns

- **Reported:** visible Type, Class ID, Directory Ordinal and Table Ordinal columns are unwanted; Memo is missing.
- **Intended/scope:** remove those four visible columns and add Memo. Keep technical metadata available in the Inspector and preserve underlying identities/data; leave other columns, order and interactions unchanged unless explicitly approved.
- **Priority/dependencies:** P0; audit which Assets grid(s) are affected and the existing exact-asset memo publication path. Depends on approved memo identity for the row unit actually displayed, not an invented bundle memo.
- **Acceptance:** affected authorized grids show Memo and no longer show those four columns; Inspector retains technical fields. Exact verified memo updates, selection, sorting eligibility, Kind sizing, zebra states and resizing remain correct.

### #4 — Inconsistent pagination

- **Reported:** Assets/Packages require manually advancing groups of100 while Search offers infinite scrolling.
- **Intended/scope:** adopt a consistent infinite-scrolling interaction where appropriate, without assuming identical data sources or replacing every paged control.
- **Priority/dependencies:** P1; #8 contracts, #5 wheel behavior, #1/#7 row identity, and workspace-specific cursor/count capabilities.
- **Acceptance:** approved grids load forward/backward near edges with bounded retained data/checkpoints, no missing/duplicate rows or viewport jumps, and explicit loading/error/retry states. Cancellation, stale revisions, selection/drafts and workspace independence remain safe. Internal100-row fetch batches may remain; consistency concerns interaction, not mandatory batch-size changes.

### #5 — Rapid mouse-wheel scrolling stalls

- **Reported:** fast Search wheel scrolling stops advancement while dragging the scrollbar continues to work.
- **Intended/scope:** reproduce and trace viewport/prefetch/cancellation behavior before fixing the confirmed cause; do not mask it with unrestricted loading or a new scrolling system.
- **Priority/dependencies:** P1; established Search window/anchor logic, #8, and deterministic wheel-input reproduction. No corruption/root-cause claim yet.
- **Acceptance:** repeated fast wheel input continues loading as needed in both directions; stopping/reversing remains responsive. Thumb dragging still works; no skipped results, scroll jumps, duplicate overlapping loads, unbounded cache or stale publication. Cover the actual event route and require manual Debug acceptance.

### #6 — Incomplete Name and Memo sorting

- **Reported:** intended Name/Memo sorting is incomplete across appropriate grids.
- **Intended/scope:** global ordering over the full matching result set, never only materialized rows. Preserve header toggling/indicators and supported existing sorts.
- **Priority/dependencies:** P1; #1/#7 row units, #8, approved Name/Memo contracts and storage support. Gate5G.1 already supports raw asset Name only in Assets-only V2 queries; Memo and mixed-result Name remain unsupported. That partial delivery does not close this broader issue.
- **Acceptance:** follow P1-D5's approved global Name/existing meaningful source-field direction and deferred bundle Memo sorting; specify exact mappings, NULL/empty, Unicode, direction and immutable ties for each affected row unit. Other asset-grid sort proposals are not automatically approved. Implement only supported approved global sorts; unsupported headers explain the limitation. Verify multi-package/multi-window order, cursor invalidation, cancellation, selection/drafts and D5. Do not silently invent semantics or weaken validation.

### #7 — Packages Indexed Assets view is too detailed

- **Reported:** Packages Indexed Assets displays individual assets and unwanted technical columns.
- **Intended/scope:** display bundles; remove visible Type, Class ID, Directory Ordinal and Table Ordinal, add Memo, and retain technical evidence through the Inspector/details. Do not treat bundle rows as selected asset rows for persistence.
- **Priority/dependencies:** P0; #1's row identity/aggregation, #3 presentation consistency and confirmed **bundle-level memo identity/storage**. Existing per-asset memo support does not prove bundle memo support.
- **Acceptance:** rows represent approved bundle units with stable identity/provenance; approved columns and existing selection/loading behavior work. Confirm storage/read/write/synchronization contracts before enabling bundle memo editing. Where storage is unsupported, record the gap and obtain a separate design decision rather than fabricate a memo target or implicitly amend schema.

### #8 — Shared scrollable-grid infrastructure

- **Reported:** Search, Assets and Packages have divergent scrolling/paging interactions.
- **Intended/scope:** share suitable scrolling, paging, cancellation, sorting, selection-retention and presentation rules. Keep workspace-specific providers, immutable identities, Inspector behavior and columns separate.
- **Priority/dependencies:** P1; audit all comparable grids and #1/#7 identity contracts first. Proposed Phase2 foundation for #5/#4/#6, not authorization for a speculative provider framework or simultaneous redesign.
- **Acceptance:** a narrow approved reuse boundary preserves each workspace's capabilities/defaults while common contracts cover bounded loading, stale completion, navigation and visual states. Regression-test all affected consumers; no automatic column unification or hidden behavior removal.

### #9 — Complete V1 retirement

- **Reported/intended:** V2 is to become the only supported package format; obsolete V1 creation, readers, search, persistence and compatibility UI should eventually be removed. **No legacy migration is required.** This records the product direction, not immediate deletion authority.
- **Scope:** first audit useful V1 functionality, especially Package Editor operations, then classify each as retain, replace or explicitly retire with user approval.
- **Priority/dependencies:** P1 gated work; Phase4 parity decisions before Phase5 deletion, including unresolved bundle workflows/memos and V2 structural editing gaps. Do not delete a feature merely because its implementation is V1.
- **Acceptance:** approved capability inventory and replacements/retirements are verified; only then authorize removal. Remaining V2 creation, browsing, search, dependencies, memos, dirty-state protection and navigation pass regression/manual acceptance. Remove obsolete format UI/code without modifying real V1 packages or adding an unrequested migration path.

### #10 — Selectable and copyable static UI text

- **Reported:** appropriate static text is not consistently selectable/copyable.
- **Intended/scope:** enable text selection and Ctrl+C for useful non-interactive text across authorized areas; first distinguish labels, instructions, details and interactive controls.
- **Priority/dependencies:** P2; audit text-control templates and focus/key routing, including comparable Inspector/details views.
- **Acceptance:** approved text can be selected/copied accurately, with accessible focus behavior. Buttons, editors, DataGrid row/cell selection, shortcuts and navigation retain their existing behavior; no global key interception that steals editing commands.

### #11 — Filesystem Assets button

- **Reported:** clicking Filesystem Assets appears to do nothing; Choose... already provides browsing.
- **Intended/scope:** trace binding/command and any distinct mode or state before deciding whether the button is redundant. Remove only if redundancy is confirmed and authorized; otherwise report its unique behavior or actual defect.
- **Priority/dependencies:** P2; navigation audit and #13 picker behavior; preserve indexed/filesystem workspace distinctions if meaningful.
- **Acceptance:** document the action's real purpose and decision. Any approved removal loses no unique workflow, shortcut, state or accessible action; a nonredundant action is not silently deleted.

### #12 — Icon-only Assets navigation actions

- **Reported/intended:** replace visible Back, Forward, Up and Refresh text with recognizable icons.
- **Scope:** presentation only; use existing icon/style conventions, keep comfortable hit targets, centered alignment, hover tooltips and accessible names with the original action labels.
- **Priority/dependencies:** P2; existing navigation commands and enabled/history states.
- **Acceptance:** the same commands, ordering, keyboard access and enabled states remain; tooltips/accessibility expose Back/Forward/Up/Refresh; icons remain readable under supported themes/scaling. No navigation logic changes.

### #13 — Rename Choose...

- **Reported:** Choose... does not clearly describe its picker.
- **Intended/scope:** inspect actual picker behavior, then choose an accurate label such as Browse Files... or Browse Folder.... Do not change the picker or its selection/filtering behavior.
- **Priority/dependencies:** P2; verified picker type/multiplicity and #11 overlap.
- **Acceptance:** visible/accessibility text accurately describes the existing action; cancellation, path handling and selected inputs remain unchanged. Record the final label rather than assume a folder or file picker.

### #14 — Reorder Search controls

- **Reported/intended:** place Criteria & Search Fields above Package Scope.
- **Scope:** reorder those existing sections only, preserving all fields, commands, expansion state, retained criteria/package selections, scrolling and established scrollbar gutter/auto-hide behavior.
- **Priority/dependencies:** P2; confirm actual section boundaries and current state ownership.
- **Acceptance:** requested vertical order is visible; Search/Enter, cancellation, filters and package selection are unchanged; expansion/focus/tab order and workspace-retained state remain usable without losing controls.

### #15 — Native Windows file icon

- **Reported/intended:** replace generic `Icons\file.png` usage with an appropriate Windows system file icon.
- **Scope:** identify exact consumers and clarify generic-system-document versus file-type-specific shell icons before implementation. Preserve unrelated icons, accessibility and selection contrast.
- **Priority/dependencies:** P2; approve icon mode, missing-file behavior and non-Windows fallback; audit reusable icon/cache ownership. Windows shell integration must not introduce Unity-runtime dependencies.
- **Acceptance:** approved icons are DPI-aware and consistent, with cached/bounded retrieval rather than expensive per-row calls; missing/unknown types and themes work safely. Dispose native resources appropriately and preserve virtualization/scroll responsiveness.

### #16 — Header boundaries

- **Reported/intended:** comparable DataGrid headers need subtle, consistent separators.
- **Scope:** reuse shared theme resources/templates where suitable; do not introduce a competing grid style or change columns.
- **Priority/dependencies:** P2; audit actual header templates and all proposed consumers, and coordinate #8 only where authorized.
- **Acceptance:** light/dark separators are subtle and consistent; sorting arrows/hit targets, blank Kind header, width bounds, resize edges, scrolling, zebra and selection behavior remain unchanged.

### #17 — Duplicate Packages introduction

- **Reported:** Packages → Detailed Package View shows its opening paragraph twice.
- **Intended/scope:** trace actual content/templates and remove only the duplicate rendering or duplicate text.
- **Priority/dependencies:** P2; reproduce the actual detailed-view state before editing.
- **Acceptance:** exactly one intended introduction remains in that view; no other content, bindings, visibility states or layout behavior is removed or redesigned.

## Proposed implementation order — not permission to execute

1. **Phase1: restore core workflow — #1, #2, #7, #3.** Apply P1-D1–P1-D5's approved direction after remaining technical contracts and a bounded implementation slice are authorized; recover bundle presentation, Search → Assets and the explicitly approved columns. Genuine bundle-memo design stays in a separate milestone.
2. **Phase2: shared grids/navigation — #8, #5, #4, #6.** Establish an evidence-based reuse boundary, fix wheel stalls, align appropriate infinite scrolling and complete approved global sorts. Diagnose independently when useful; do not make a broad refactor a prerequisite for every bug fix.
3. **Phase3: usability/visual consistency — #10–#17.** Separate small, independently reviewable checkpoints; cross-workspace consistency still needs scoped authorization.
4. **Phase4: V1 feature-parity audit.** Inventory V1-only capabilities, especially Package Editor operations, and obtain retain/replace/retire decisions. No deletion during the audit.
5. **Phase5: V1 retirement — #9.** Remove obsolete format paths only after Phase4 approvals and regression verification. No legacy migration.

Do not start Phase1, a V1 audit, schema changes or any implementation automatically because this document exists. Future tasks must name the authorized slice and prerequisites.

## Phase 1 Approved Decisions

**Authority:** explicit user approval, recorded 2026-10-23. **Status:** P1-D1–P1-D5 are decided design direction, not implementation authorization. No roadmap phase, schema/trigger amendment, bundle-memo storage or behavior change is authorized by this documentation task. All 17 issues, including #1/#2/#3/#7, remain Reported, unimplemented/unresolved under this roadmap. These IDs are separate from the existing V2 D1–D8, including D5 Refresh policy, which remain unchanged.

### P1-D1 — Bundle identity and deduplication

**Approved / decided:** represent one result per indexed source/bundle occurrence, using existing immutable source UUID and source revision together with required package/generation and live registration context. Never merge separate occurrences solely by filenames, CAB labels, asset names or content hashes. Identical display names remain distinct occurrences. A label is not an identity.

### P1-D2 — Search matching and aggregation

**Approved / decided:** a bundle/source occurrence matches when its source name matches the criteria **or** a contained asset name or memo matches. Present that occurrence once; an asset-level match is evidence, not itself a displayed Search result in the bundle projection. Retain approved V2 Unicode and filtering semantics for asset matching. Aggregate at bundle level **before deterministic global sorting and pagination**, never over the visible scrolling window. This does not approve reproducing legacy field-local exclusion or exact-mode quirks.

### P1-D3 — Genuine bundle-owned memos

**Approved / decided:** design genuine bundle-owned memos in a **separate milestone**. Existing V2 asset memos remain asset-owned and unchanged; never assign an arbitrary contained asset memo or a merged asset-memo string to a bundle. The independent bundle-memo identity, storage, transaction/conflict model and UI behavior still require a separate approved design. No bundle memo storage, SQLite schema/trigger changes or migration work is authorized now. Before genuine support exists, a future bundle Memo column must not display invented or misleading values; its exact unavailable-state presentation remains to be specified.

### P1-D4 — Search to Assets navigation

**Approved / decided:** restore navigation from a selected Search bundle result to its corresponding source/bundle in Assets using exact identity, never filenames or CAB labels. Preserve existing navigation history and Back/Forward behavior. Support indexed metadata browsing when original source bundles are unavailable wherever existing V2 contracts permit it; metadata availability does not imply source payload loading/preview is possible. Action wiring, missing-source presentation and exact child-selection provenance remain technical dependencies, not a reason to retarget identity or replace existing history semantics.

### P1-D5 — Global sorting

**Approved / decided:** support Name and existing meaningful source/bundle fields with deterministic global ordering **before pagination**, never sorting only materialized rows. Do not invent additional fields. Defer **bundle Memo sorting** until bundle-memo architecture and its sorting semantics are separately approved. Detailed eligible field mappings, immutable tie-breakers and source-level cursor integration remain implementation dependencies; this is not approval for a local-window substitute.

## Decisions and new findings register

The five decisions above resolve product direction only. Preserve the remaining technical questions and obtain separate authorization for any implementation slice. Record later decisions with user/date, source evidence and acceptance impact.

| Decision/finding | Affected issues | Current disposition |
|---|---|---|
| Bundle row unit and deduplication | #1, #2, #7 | **Decided: P1-D1.** One indexed source occurrence; preserve separate identities across identical labels/hashes. Standalone/failed/unsupported-source inclusion and typed projection still need specification |
| Search matching and aggregation | #1, #6, #7 | **Decided: P1-D2.** Source-name OR qualifying contained asset-name/memo match, one source row before global order/paging; approved asset filtering retained. Precise source/child predicate composition, coverage evidence and current mixed-result/Path ID/Class/type/Memo-only controls still need mapping |
| Genuine bundle memo direction | #3, #6, #7, #9 | **Decided: P1-D3; detailed design deferred.** Independent bundle identity/storage/transactions/conflicts/UI/synchronization remain unapproved; preserve asset memos, no fake values or schema/trigger changes |
| Global source/bundle sorting | #6 | **Decided: P1-D5.** Name/existing meaningful fields globally before pages; bundle Memo sorting deferred. Exact mappings, NULL/empty/Unicode/ties and cursor contracts remain to specify without changing established asset matching |
| Search → Assets workflow | #2 | **Decided: P1-D4.** Exact source navigation, existing history/Back/Forward preserved, indexed metadata available without source where supported. Gesture, native provenance, parent-directory/return-context integration and missing-source UX remain to specify |
| Reuse boundary and which paged grids should become infinite-scroll consumers | #4, #8 | Open; preserve workspace-specific providers/columns/Inspectors |
| V1-only capability inventory and V2 replacement/retirement approvals | #9 | Open; future V2-only/no-migration direction recorded, immediate removal not authorized |
| Filesystem Assets unique behavior and accurate Choose... replacement label | #11, #13 | Inspect before deciding; no assumed redundancy/picker type |
| Generic versus file-type-specific Windows system icon, fallbacks/cache/lifetime | #15 | Open; no icon-provider implementation selected |

Further root causes, reproduction evidence, risks and user decisions belong here or in linked scoped audit reports. Do not mark a user report independently verified solely because it is listed.

## Progress and verification rules

Use **Reported → Investigating → Approved for implementation → In progress → Automated verification complete → Manual acceptance complete → Closed** as applicable. Record approval separately from proposal, and include evidence/date/task references for status changes. If interrupted, preserve work and use the session handoff; do not skip acceptance states to declare completion.

Implementation evidence must cover the issue's acceptance criteria plus preserved comparable workflows. Apply the repository's risk-based verification: focused checks for micro-fixes, broader regression/audits for shared concurrency, identity or persistence changes; never weaken existing tests. Visible changes require current Debug startup build and user-operated manual acceptance. Only close after the relevant automated checks, manual acceptance and unresolved decisions are addressed.

**This task:** documentation only; all17 statuses remain Reported. No application builds, regression runs, desktop control, source/package changes or implementation work are needed. Validate Markdown, local references, issue coverage, policy consistency and Git whitespace/scope. The previous Gate5G.1 verification remains historical evidence, not fresh verification of this plan.
