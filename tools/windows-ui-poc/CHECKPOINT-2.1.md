# Checkpoint 2.1 UI acceptance coverage

Source of truth: [manual checklist](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-search-defaults.md#limits-and-manual-acceptance),
[handoff](../../Avalonia/docs/05_audit/codex-session-handoff.md), and
[roadmap](../../Avalonia/docs/05_audit/v2-ui-workflow-recovery-roadmap.md).
No recovery issue or checkpoint status is changed by this tool document.

The user confirmed [run 40246911317](https://github.com/PapayaModding/UABS/actions/runs/40246911317)
at `6991888bd7f8fe7704290f4b3c777b3ec45feedb` passed the original Smoke
scenario. This establishes hosted desktop feasibility and that scenario's
observed defaults/query/results only. The current session could not independently
download that run through the GitHub API (Forbidden).

The user subsequently confirmed [run 40249915733](https://github.com/PapayaModding/UABS/actions/runs/40249915733):
Smoke and fresh V1 passed; the implemented V2 cases passed except CP21-08 and
CP21-09, which were BLOCKED by missing Invoke and SelectionItem respectively.
These are user-confirmed previous-run outcomes; this session's API access is
still Forbidden. Subsequent outcomes are recorded below; source tests/builds
do not establish UI acceptance for a newer commit.

Latest user-confirmed baseline: [run 40259657455](https://github.com/PapayaModding/UABS/actions/runs/40259657455)
at `26f38e68511cd6eaf0d0c23b7ce9fac9228aab70` passed all three jobs:
Smoke, checkpoint21-v2 and checkpoint21-v1. Implemented CP21-01–CP21-11 checks,
including Name sorting, now have real Windows evidence. This investigation's
read-only run API request returned Forbidden; artifacts were not independently
downloaded or rerun. The [final acceptance gap audit](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md)
classifies five remaining behavioral coverage gaps, the literal tooltip evidence/
equivalence decision, regression disposition, deferred work and the next slice.
No checkpoint or unrelated coverage gap closes merely because these jobs pass.

**Latest user-confirmed execution:** [run 40338427998](https://github.com/PapayaModding/UABS/actions/runs/40338427998)
at `7343b139c20795a4dbc66928663db10987dc97df` passed all four jobs, including
the complete G2/G3/G4 slice. These are user-confirmed Windows results, not a
new artifact inspection. The historical gap investigation below is superseded
for those executed cases, not for remaining G1/G5/G6/Linux obligations.

G1 Package sorting still has no supported coordinate-free action: the Package
header lacks Invoke/keyboard sort and the Name Button targets Name only. The
[Package accessibility/fixture proposal](PACKAGE-SORT-ACCESSIBILITY-PROPOSAL.md)
documents the smallest new Button approval scope, a three-row/two-package fixture
and an independent oracle whose descending order retains ascending tie-breakers.
No Package action, fixture, workflow or production change is implemented here.
Separate production approval is required before the automated G1 test can proceed.

## Next test-only G2/G3/G4 slice — implemented, Windows pending

The additive `checkpoint21-gaps` job and [slice/evidence guide](ACCEPTANCE-GAP-SLICE.md)
implement five phased cases for G3 pre-admission input/defaulting/known-query,
G2 non-default criteria retention across every documented scope/workspace/catalog
transition, and G4 metadata-exclusion rejection/retained state/recovery. The
existing three jobs and CP21 assertions remain unchanged. Source/headless checks
do not accept these gaps; actual Windows evidence at the new SHA is required.
G1 Package order, G5 indeterminate feedback, G6 tooltip equivalence/hover and
Linux regression disposition remain explicitly open. No checkpoint closure.

User-confirmed [run 40326632909](https://github.com/PapayaModding/UABS/actions/runs/40326632909)
at `aaadcd0cb26af075a9087de108f475b1e0768bf7` passed the original three jobs
but blocked G3 before typing: six empty writable Edits, no watermark matches.
The separately approved correction adds public Name/ID metadata to the five
criteria controls and replaces watermark discovery with strict paired ID/Name
plus retained RuntimeId verification. See the slice guide for identities/tests.
This correction is not a Windows PASS: G3 and the unexecuted G2/G4 still need
the updated hosted run. No production Search semantics or other gaps changed.

Subsequently, the user confirms [run 40330005576](https://github.com/PapayaModding/UABS/actions/runs/40330005576)
at `e12238943933ae817f1953655d6c0b1757f90b86`: original jobs PASS; all three
G3 phases and G4 PASS; only G2 BLOCKED on the fresh-reader requirement despite
correct non-default values and an intentional `1 displayed · stale count`.
The test-only G2 correction separately verifies retained stale rows/count/state,
last-executed criteria and no query/error activity at every required transition.
Fresh readers and passing cases stay unchanged. See the slice guide; updated
Windows G2 execution remains pending. G1/G5/G6 and Linux disposition remain open.

| Documented requirement | Scenario/case | User-confirmed Windows result before this change | Evidence / remaining validation |
| --- | --- | --- | --- |
| Untouched V2 Sources-only / Assets defaults | unchanged `smoke`; CP21-01 | PASS, user-confirmed baseline | Linked baseline run; fresh SHA gets its own artifact |
| Known Unicode keyword returns exact bundle Name/Package identities | unchanged `smoke`; CP21-01 | PASS, user-confirmed baseline | `preparation.json`, `final.png`, observed rows in `results.json` |
| Explicit Names choice, corrective error, controls and confirmed rows retained | CP21-02-Names (also Paths/All feedback) | PASS | Per-case screenshot/tree, field-selection evidence, error text and exact rows |
| Mixed result kinds produce corrective feedback and retain choices/rows | CP21-03 | PASS | Toggle readback, corrective text, exact rows |
| Original backend diagnostic available through tooltip | CP21-04 | PASS | Original `InvalidRequest` through UIA HelpText in the previous run |
| Explicit selectors/text/other criteria survive scope and workspace transitions | CP21-05 | PASS | Toggle/ComboBox readback and all readable Edit/criterion-flag snapshots before/after |
| Explicit choices survive catalog reload | CP21-06 | PASS | Exact disposable catalog path, native folder Edit Value readback, Refresh Invoke, retained criteria |
| Explicit Refresh unchanged; editing criteria alone does not rerun | CP21-07 | PASS | Prior row retained, Refresh publishes exact empty result, second Refresh restores Unicode result |
| Name sorting through the accessible action | CP21-08 | PASS, user-confirmed run 40259657455 | Button Invoke/focused Enter, both exact visual directions and independently indexed three-bundle oracle. Nontrivial Package sorting remains mandatory G1 |
| Selection retained including after rejection | CP21-09 | PASS, user-confirmed run 40259657455 | Strict positional bounds, UIA focus and Ctrl+Home/End, exact Inspector identity/retention |
| Mixed V1/V2 scope rejects rather than guesses | CP21-10 | PASS, user-confirmed run 40259657455 | Fresh bounded UIA reads and exact dialog/catalog identity; actual rejection exercised per reported outcome |
| Fresh V1-only original mixed/Names defaults and Search behavior | separate `checkpoint21-v1`, CP21-11 | PASS | SHA-checked LegacyPackage copy, six-column exact portrait/LegacyPackage/character.ab/1001/memo row and exact count |

The metadata/mixed-exclusion and indeterminate-coverage guidance described in
the report has no real-desktop case here. Numeric/type filters are
captured for retention but non-default numeric/type values and text entered
before initial package admission are not newly exercised. These remain gaps;
do not infer acceptance from headless tests. See G1–G6 in the final audit for
mandatory evidence and explicit classification. Extra post-reload sort/selection
cross-products and stress variants are optional additional coverage, not silently
passed cases or newly invented checkpoint gates.

## Execution and evidence

The workflow's independent matrix jobs are `smoke`, `checkpoint21-v2`, and
`checkpoint21-v1`. Each job builds the same exact attached recovery commit,
prepares its own package and independently verified expectations, launches a
fresh UABS process, and uploads its own named artifact even on failure. The
original Smoke code path does not opt into expanded interactions.

New scenarios are selected only by optional `acceptance` configuration values;
omission retains Smoke. V2 expanded cases run after baseline success; V1 starts
with a fresh process before any V2 package is registered. Each case records
PASS/FAIL/BLOCKED in `acceptance.json`, timestamped actions in `results.json`,
and a named screenshot/UIA tree. A blocked subcase does not stop later cases.
Aggregate FAIL takes precedence over BLOCKED, which takes precedence over PASS.
The external watchdog bounds an expanded run to 600 seconds; timed-out runs
are BLOCKED even if some earlier cases passed. Missing `acceptance.json` means
the expanded cases were not reached, not that they passed.

Legacy metadata is copied from the tracked disposable fixture (three explicit
files), SHA-checked, and opened only from the run workspace. Embedded historic
game source paths are metadata; no original Unity source is opened or edited.
Catalog/mixed-format scenarios use that workspace and the already SHA-verified
Smoke SQLite copy. Native folder controls, ComboBox selection, Name-sort Button Invoke,
and either row SelectionItem or verified keyboard focus require supported UIA capabilities. A missing pattern or
ambiguous control blocks; there is no coordinate/OCR substitute. Original
diagnostic tooltips are not assumed to be accessible because the binding exists.

Final acceptance requires inspection of every applicable actual case result
and resolution of remaining gaps. Checkpoint 2.1 and issue #1 remain open.

## Two UIA blockers: source investigation and isolated runner change

The sections below preserve the historical investigation. Pending-run statements
describe those revisions; the latest user-confirmed outcome is recorded above.

Avalonia DataGrid 11.3.3's
[header peer](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/Automation/Peers/DataGridColumnHeaderAutomationPeer.cs)
does not implement Invoke. The
[header control](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/DataGridColumnHeader.cs)
routes pointer release to InvokeProcessSort but has no key handler, and the
[grid key dispatcher](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/DataGrid.cs)
has no sorting shortcut. UABS's header HelpText exposes sort state, not an
action. The runner records header/child capabilities, access/accelerator keys,
and legacy-pattern availability in `name-sort-capabilities.json`. It does not
invent an Enter/F3 shortcut, call the ViewModel, use a generic child action,
or synthesize pointer coordinates. CP21-08 stays BLOCKED without Invoke.
A separate production/upstream accessibility proposal would expose a header
Invoke provider routed through the existing user-sort behavior; it is not
implemented or authorized by these tool changes.

For selection, DataGrid is keyboard-focusable. Its ProcessHomeKey and
ProcessEndKey explicitly call UpdateSelectionAndCurrency with SelectCurrent
when Control is held. CP21-09 keeps its existing SelectionItem route when
supported. Otherwise it verifies the independent three-row `asset` oracle,
uses UIA SetFocus on the owned grid, checks HasKeyboardFocus and that the
foreground HWND is the launched UABS window, then sends only Ctrl+Home/End
through normal Windows keyboard input (no pointer action). Unsupported focus
or unverified ownership blocks before any keys. Control is released in finally.

The first and last rows must produce different, readable V2SearchDetails
Inspectors. Each must match the expected Name/Package, exact SHA-verified
disposable DB path, original source filename, and nonempty package/generation/
source/revision UUIDs. After Names rejection, the full Inspector text must
remain identical and the complete row multiset and package SHA must remain
unchanged. Missing Inspector exposure or unverifiable navigation is BLOCKED;
an observed identity change after rejection is FAIL. Capability JSON, first/
last selection screenshots, and all three Inspector strings are retained.
This is an evidence-backed input strategy, not a Windows execution result.

## CP21-09 timeout investigation

The user reports [run 40251315631](https://github.com/PapayaModding/UABS/actions/runs/40251315631)
at `dc2518e93f5d8a8ee731147938bac43b5616ddee` still times out waiting
for the first Inspector identity, although the captured UIA tree contains
V2SearchDetails for `uabs-smoke-0000.bundle`. The current session cannot
download the artifact: GitHub's artifact API returns Forbidden, including on
retry. A populated Inspector alone cannot establish which strict condition
failed, whether the boundary row differs from UIA descendant order, or whether
the Inspector was excluded by its visibility/readability gate.

The runner's old timeout path recorded Inspector text only after a successful
match. That diagnostic omission is corrected: `CP21-09-first-identity.json`
and `CP21-09-last-identity.json` are now emitted in finally, even on timeout.
They contain the exact expected boundary row and DB-copy path, captured UIA
row order, actual parsed fields/raw text, every name/package/path/UUID check,
duplicate fields, raw Inspector Name/Text/IsOffscreen observations, header
sort HelpText, and grid focus. The original strict predicate, boundary-row
choice, keyboard input, ownership checks, and sorting blocker are unchanged.
No functional selection correction is claimed without the actual evidence.

To diagnose the existing run, provide its V2 artifact ZIP (especially
acceptance.json, CP21-09-selection-uia.json, CP21-09-selection.png,
grid-keyboard-capabilities.json, and results.json), or dispatch this diagnostic
commit and inspect the new comparison JSON. At that revision, UIA descendant
order versus logical boundary order remained a hypothesis; the subsequent
user-provided evidence below confirms the mismatch.

## Confirmed row-order mismatch and positional correction

The user inspected [run 40252417310](https://github.com/PapayaModding/UABS/actions/runs/40252417310)
at `11c9489f1a3ecf3ca517273a12a4415f417be844`: UIA descendant order was
0002, 0001, 0000, whereas Ctrl+Home selected 0000. Package/path/UUID checks
passed; Name and source-filename checks failed against the assumed 0002.
Thus the expected boundary row was derived from the wrong kind of ordering.
No identity check needs to be weakened to correct that assumption.

Avalonia 11.3.3's
[DataGridRowsPresenter.ArrangeOverride](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/Primitives/DataGridRowsPresenter.cs)
arranges `DisplayData.GetScrollingElements()` in increasing vertical position.
Its child index provider separately returns each row's logical Index; visual
child enumeration is not the authoritative navigation order. The grid's
Ctrl+Home/End handlers select the first/last logical visible slot.

CP21-09 now reads each realized DataItem's two-cell identity and its UIA
BoundingRectangle from that same row element. It requires the unchanged
independent complete row-set/count check, then rejects missing, zero/nonfinite,
offscreen, clipped, overlapping/tied, or out-of-grid bounds. Rows must fit
fully below the two verified column headers, and identities must be distinct.
Only then does sorting the observations by Top establish visual boundary
identities. It does not reverse enumeration or sort bundle names to guess
boundaries. Unavailable/ambiguous layout is BLOCKED; a wrong complete result
set remains FAIL.

Rectangles are read-only evidence and are never passed to mouse APIs or used
to move focus. Input remains UIA SetFocus with process/foreground checks and
fixed Ctrl+Home/End shortcuts. Inspector matching, copy-path/SHA checks,
four UUID checks, and full Inspector retention after Names rejection are
unchanged. Complete row set and visual order are rechecked after each key
and rejection. `CP21-09-layout-{before-keys,after-home,after-end,after-rejection}.json`
records rectangles, raw UIA enumeration, derived visual order, and failures;
first/last identity reports retain both enumeration and visual order.
Regression tests cover reversed and scrambled enumeration and ambiguous,
clipped, hidden, invalid, and duplicate-row layouts. Real Windows validation
of this correction is still required. CP21-08 remains BLOCKED and the
checkpoint is not closed.

## Hosted layout and catalog provider investigation

The user reports [run 40253986638](https://github.com/PapayaModding/UABS/actions/runs/40253986638)
at `463d7408af3c0dc47a3eaa8c82901a9320e701b4`: Smoke and V1 passed;
expanded V2 had 10 PASS, 2 BLOCKED, 1 FAIL. CP21-09 reported grid bounds
394,594,264,27, a header at Y594 with height32, and hence a -5px data viewport.
All three expected row identities existed but had zero-sized, offscreen bounds.
This is a real unusable viewport, not justification to bypass visibility.

Source inspection: MainWindow specifies minimum 900x560 but no initial
runtime Width/Height. Search reserves default widths of 290 and 310 for Controls
and Inspector plus splitters, while Results has an Auto-sized wrapping status
header above the star-sized DataGrid. The count label can consume 230px of the
narrow 264px results column, leaving very little width for the wrapping status
text. Auto header growth can consume the grid's available height. This is a
source-supported explanation consistent with the measurements; exact live
window/splitter/work-area dimensions were not available through the artifact
API (Forbidden), so the full causal layout chain is not claimed as verified.

Only CP21-09 now requests UIA WindowVisualState.Maximized on the verified owned
UABS window, requiring Window support/CanMaximize and confirming the resulting
state. There are no hard-coded screen sizes, splitter moves, synthetic row
coordinates, or app layout changes. Smoke and fresh V1 do not use this setup.
`CP21-09-window-{before,after}-maximize.json` records real window/work-area
bounds, exposed Controls/Inspector/splitter geometry, and actual grid scroll
panes/bars. Inaccessible elements are recorded as unavailable or zero matches.
The existing row-layout reports then enforce a valid data viewport and visible
complete row bounds. An insufficient maximized viewport remains BLOCKED;
all Inspector/path/UUID/focus/foreground/retention and SHA assertions remain.

CP21-10's generic `FindAll` E_FAIL (0x80004005) does not prove teardown versus
a stale wrapper or inaccessible provider. The old helper used a cached owner
wrapper and always walked its descendants even after finding a top-level
picker. Catalog reads now reacquire the same owned HWND through UIA FromHandle
and freshly discover the exact process/title picker each attempt. A matching
top-level picker avoids an unnecessary disabled-owner tree walk. Native folder
IDs, path readback, exact catalog identity, and mixed-format assertions remain.

Only read-only queries retry, at most three attempts with 100ms pauses, for
E_FAIL, UIA_E_ELEMENTNOTAVAILABLE, and RPC_E_DISCONNECTED. The existing10/25s
discovery waits and external watchdog still apply. Other COM codes block
immediately; persistent errors block with phase/HRESULT/attempt evidence.
Select/SetValue/Invoke are never replayed. Completion requires the picker to
be absent, the freshly discovered command button to be re-enabled, and the
actual catalog to report zero load errors. Readback errors, incorrect catalog
results, and an observed application exit remain FAIL; provider inaccessibility
is BLOCKED and cannot count as exercised mixed-format rejection.

`catalog-provider-*.json` records owner HWND/PID, exact title/directory, failed
phase, read attempts, invocation state/fault, and the original exception.
Cleanup errors are logged without replacing the primary failure or enabling
a PASS. Transience and any teardown hypothesis require the next Windows run's
actual phase evidence. No remote acceptance or checkpoint closure is claimed.

CP21-08 remains untouched and BLOCKED: a separately approved production or
upstream header Invoke provider routed to existing user-sort behavior is
needed for accessible sorting. No additional runner shortcut is attempted.
