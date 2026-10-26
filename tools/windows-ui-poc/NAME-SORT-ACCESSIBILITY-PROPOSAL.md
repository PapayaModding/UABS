# CP21-08: narrowly scoped Name-sort accessibility proposal

Status: the user separately approved this proposal at baseline
`59c7e0762c056e55bb9be0dee67e0f7120d55942`. The scoped production/runner
implementation is now present; real Windows verification at its exact commit
is still required. Checkpoint 2.1 remains open; Checkpoint 3 is out of scope.

## Approved implementation record

- Production: `SearchWorkspaceView.axaml`, its code-behind, and
  `SearchWorkspaceViewModel.cs`. One V2-bundle-only standard `SearchNameSortButton`
  beside Refresh uses the same `BeginHeaderSort`/`SortHeaderAsync` lifecycle as
  the pointer header. The existing next-direction calculation is extracted,
  not duplicated, so HelpText describes both confirmed and queued sort state.
  IsEnabled follows the existing current-session guard; the click also guards
  mode/column/field availability. A wrapping results footer accommodates the
  added action without changing columns, splitters or other workspace layouts.
- Production regressions: `V2BundleNameSortAccessibilityTests.cs` adds five
  cases (two Invoke/Enter/window-size combinations, stale/unavailable state,
  and two queued-toggle/cancellation cases). Existing bundle header tests now
  check the accessible route after both Name and Package headers; V1 and raw
  asset-mode tests verify the button stays hidden. The shared memo integration
  case exercises the button while an asset draft is pending in other Inspectors.
  Re-admission changes registration IDs by design; stable selected row identity,
  package/source UUIDs and paths remain verified, not the transient registration.
- Runner: `SortingInteraction.cs` replaces only CP21-08's unreachable header
  Invoke/enumeration assertions. `SearchAccessibility.cs` and its regressions
  check readable current/next/busy evidence and exact independent Name/Package
  sequences. `SelectionInteraction.cs` and `WindowLayout.cs` accept an optional
  case evidence prefix; CP21-09 keeps its previous defaults and assertions.
  Smoke and V1 runner paths and the workflow are unchanged.
- Evidence: `name-sort-capabilities.json`, `CP21-08-window-*.json`,
  `CP21-08-layout-*.json`, `CP21-08-order-{Ascending,Descending}.json`, both
  direction screenshots/UIA trees, and the existing acceptance/results logs.
  Both actual UIA Invoke and owned, focused Enter must succeed. Unsupported or
  ambiguous controls/state/geometry block; an observed wrong row/order or
  application error fails. No coordinates, OCR, or application API manipulation.

The original design rationale and release plan below remain relevant; their
conditional approval language records the original proposal, not a claim that
the current Windows run has passed. The new button does not add Invoke to the
header peer itself. Further UI changes still require separate authorization.

### Implementation verification (Linux)

- **184 tests PASS in completed runs:** bundle workspace + headers + shared memo
  integration 61/61 (including all five new cases); Search ViewModel 22/22;
  Search integration 18/18; infinite scrolling 7/7; result window 7/7; bundle SQL
  49/49; paging/performance 20/20. Suites were also run individually to separate
  native test-host failures from assertion failures.
- Runner `tests/EvidenceChecks.csproj`: PASS, including the new exact-order,
  current/next/busy, wrong-row/package, duplicate-oracle, and unavailable-target
  checks. Existing ambiguous/offscreen/overlapping geometry and unowned-focus
  regressions remain enforced. PowerShell scenario configuration regressions PASS.
- Debug solution and Release Windows runner cross-build: zero warnings/errors.
  Workflow actionlint and whitespace checks PASS. No workflow change.
- **Full suite is not green on this Linux host.** The first attempt reported
  724 PASS, 38 FAIL, one skip before `PAL_SEHException` aborted VSTest. A separate
  untouched worktree at the approved baseline also aborted with the same native
  exception, reporting 726 PASS, 36 FAIL, one skip. Its failures include Windows
  NTFS/kernel32-only creation/discovery checks, legacy Windows-path expectations
  and file-admission/replacement checks. The first implementation run also had
  two global Activity query-counter failures; paging/performance passes 20/20
  in isolation. Do not claim a clean full-suite regression result or invent a
  definitive native-crash root cause from this comparison.
- A repeated full implementation run aborted after 298 passing tests with no
  assertion failures. Broader Search runs also aborted. Isolated raw
  `V2SearchTests` reported 26 passing tests but aborted, so that suite is **not**
  counted in the 184 clean passes. The isolated baseline raw suite also aborted
  with the same exception, after 27 passing tests. Baseline comparison artifacts and test logs
  are local under `/tmp/uabs-cp21-sort-test-results/`, not committed.

Reproduce focused production validation with:

```sh
dotnet test Avalonia/UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj -c Debug --filter 'FullyQualifiedName~V2BundleSearchWorkspaceTests|FullyQualifiedName~V2SearchHeaderSortTests|FullyQualifiedName~V2MemoIntegrationTests'
dotnet run --project tools/windows-ui-poc/tests/EvidenceChecks.csproj
dotnet build tools/windows-ui-poc/WindowsUiPoc.csproj -c Release
pwsh -File tools/windows-ui-poc/tests/ScenarioConfiguration.Tests.ps1
actionlint .github/workflows/windows-ui-poc.yml
```

Windows acceptance is pending. Manually dispatch
[Windows UI POC](https://github.com/PapayaModding/UABS/actions/workflows/windows-ui-poc.yml)
on `v2-recovery` at the published implementation SHA. The existing workflow
checks the exact dispatch SHA, builds Debug UABS, prepares the SHA-verified
disposable Smoke package and independent oracle, and runs Smoke/V1/expanded V2
with evidence upload. Inspect both CP21-08 direction reports and input steps,
all per-case `acceptance.json` results, and Smoke/V1 artifacts. Compilation and
headless provider tests do not prove real Windows UIA or final acceptance.

## Evidence and current implementation

The user confirms [Windows run 40256326602](https://github.com/PapayaModding/UABS/actions/runs/40256326602):
Smoke and V1 PASS; expanded V2 has 12 PASS and only CP21-08 BLOCKED.
CP21-09 selection retention and CP21-10 mixed-format rejection PASS. These are
user-confirmed Windows outcomes, not a new execution or independently downloaded
artifact in this investigation. Source inspection establishes the following:

- [Search XAML](../../Avalonia/UABS/Script/View/Pages/SearchWorkspaceView.axaml)
  uses a read-only DataGrid with `CanUserSortColumns=True` and
  `Sorting=OnResultsSorting`. Bundle results show exactly Name and Package.
- [Search view](../../Avalonia/UABS/Script/View/Pages/SearchWorkspaceView.axaml.cs)
  leaves V1 on native DataGrid sorting. For V2, `OnResultsSorting` handles the
  event so a retained page/window cannot be sorted locally. `GlobalSort` maps
  bundle Name to `SourceName` and Package to `PackageName` (raw asset-mode Name
  maps to `AssetName`). `SortHeaderAsync` calls the existing global operation,
  preserves the header-sort lifecycle/version, restores selection after layout,
  logs failures, and queues indicator updates. Header HelpText and arrows report
  the displayed global order; they do not expose an action.
- [Search ViewModel](../../Avalonia/UABS/Script/ViewModels/SearchWorkspaceViewModel.cs)
  guards sorting through `CanChangeV2Sort`, including stale/session ownership
  checks. `SortV2Async` toggles an already ascending field to descending;
  otherwise it selects ascending. Queued requests use the pending direction.
  It retains admitted criteria and revision checks, cancels obsolete work and
  does not silently refresh a changed catalog. Bundle mode permits only Name
  and Package sorts. These semantics must not change.

Pinned Avalonia DataGrid **11.3.3** sources were inspected directly:

- [DataGridColumnHeaderAutomationPeer](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/Automation/Peers/DataGridColumnHeaderAutomationPeer.cs)
  exposes HeaderItem but no Invoke provider.
- [DataGridColumnHeader](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/DataGridColumnHeader.cs)
  defaults `IsTabStop` to false. Pointer handling reaches internal
  `InvokeProcessSort`, waits for lost focus/row edit commit, then posts
  `ProcessSort`, which raises the sorting event. No header keyboard-sort handler
  is present.
- [DataGrid](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/DataGrid.cs)
  handles navigation/edit/copy keys, not a Name-sort keyboard command.

The missing capability is an accessible user action, not a missing backend
sort. Header HelpText, focus, or a descendant's unrelated Invoke cannot supply
that action. Inventing an F3/Enter shortcut or calling the ViewModel from FlaUI
would not exercise the existing user interface.

## Alternatives and recommendation

| Option | Benefit | Cost / reason |
| --- | --- | --- |
| Upstream header Invoke and keyboard support | No additional visible control; benefits all grids | Best long-term dependency improvement, but internal sort entry points and generated headers make a local peer substitution nontrivial. Forking/upgrading affects other grids and requires broader compatibility testing. Not the smallest UABS slice. |
| Search-only keyboard shortcut | Little layout impact | Requires a new command, focus/conflict rules and discoverability. No existing shortcut is supported by evidence; an undocumented binding is a poor accessibility surface. |
| Standard Search-only “Sort Name” Button | Native keyboard/Invoke surface, explicit target, no dependency fork | Adds one visible action and tab stop. Requires approved placement, narrow-width layout and actual UIA validation. Recommended bounded change. |

**Proposed approval scope:** one standard Button, `SearchNameSortButton`, in
the existing Search results action area beside Refresh Search, visible only
for V2 bundle results. Keep Name/Package columns, header pointer sorting,
Refresh/Retry actions, selectors, splitters, Inspector, and V1/raw asset-mode
presentation unchanged. No custom header, new result column, new sort algorithm,
global shortcut, schema, or persistence change.

The accessible name should identify “Sort Name”; HelpText should disclose the
current order (including when another field is active) and next direction.
Use ordinary tab focus and Button activation, not a custom key hook. Confirm
standard Button Invoke and keyboard behavior experimentally rather than
assuming that compilation proves provider compatibility.

Implementation must share the existing guarded view-level sort entry with the
Name header: resolve its field through `GlobalSort`, retain enabled guards,
`_restoringHeaderSelection`, versioning, `HeaderSortTask`, and
`SortHeaderAsync`/indicator updates. Do not duplicate direction logic or expose
a test-only ViewModel API. Disable the action when the equivalent header cannot
sort; do not implicitly execute or refresh Search. The grid is read-only, but
normal focus transitions and shared memo-draft retention still need regression
coverage; do not invoke Avalonia's internal pointer/edit methods by reflection.

This deliberately does **not** make the header peer itself accessible. It adds
an equivalent user-operated route to the same global Name sort. Package sorting
accessibility and other workspaces are separate approval scopes. If review
requires header-only activation or absolutely no visible addition, retain
CP21-08 BLOCKED and pursue the upstream solution instead.

The footer is currently a horizontal StackPanel: an added control may clip at
small sizes. Review minimum-window and maximized layout before merging. If a
wrapping action-area change is required, include that specific visible change
in the production review; do not silently redesign the workspace.

## Proposed runner verification, after production approval

1. Preserve Smoke/V1 paths. CP21-08 alone prepares a usable owned-window layout
   using the already guarded maximize mechanism, and searches `asset`.
2. Preserve the independently prepared complete three-row oracle from
   [CI preparation](ci/Program.cs): `uabs-smoke-0000.bundle`, `0001.bundle`,
   `0002.bundle`, each in Smoke. Expectations come from fixed fixture identities
   cross-checked against raw indexed assets, not observed results or Search's
   own sorting implementation. Expected ascending/descending sequences are
   explicit and distinct; the fixture's ASCII unique names avoid collation ties.
3. Discover exactly one enabled `SearchNameSortButton` in the verified results
   area of the owned UABS window. Capture Name, HelpText, enabled/focus states,
   patterns, displayed header direction and complete pre-action row identities.
   Missing/ambiguous controls or unsupported patterns are BLOCKED, not PASS.
4. Invoke through its actual UIA Invoke provider. Also verify a normal focused
   keyboard activation with owned foreground HWND and focus readback, releasing
   keys in finally. Never call application methods from the runner. Read back
   current/next direction to select the required number of toggles with a fixed
   bound; do not assume the query initially uses a different field.
5. Assert both exact ascending and descending Name sequences against the
   independent oracle after completed sorting. Use fresh row identities paired
   with read-only UIA rectangles and the existing strict visible/nonoverlapping
   complete-row gates, not descendant enumeration order. Generalize the
   CP21-09 positional helper only as needed for evidence labels. Rectangles
   must never become input coordinates. An unusable viewport is BLOCKED.
6. Keep the full Name/Package set/count check before and after each action;
   reversal alone is insufficient. A reliably observed wrong order is FAIL.
   Wait for completed state/order with bounded fresh discovery, not fixed sleeps
   or an intermediate sorted frame. Record before/after direction, expected and
   actual order, UIA trees/screenshots and action completion evidence.
7. Re-run all expanded cases, including strict Inspector/path/UUID/selection
   retention and mixed-format rejection. Do not infer their PASS from this case.

The current CP21-08 implementation is correctly blocked before interacting,
but its unreachable order assertions compare `ReadAccessibleRows` enumeration
and its reversal. Prior CP21-09 Windows evidence disproves enumeration as a
navigation/visual-order oracle. Replace those assertions as part of the reviewed
runner change; merely supplying Invoke is not enough to validate sorting.

## Regression and release gates for the later implementation

- Extend existing [header-sort tests](../../Avalonia/UABS.UnityAssets.Tests/V2SearchHeaderSortTests.cs)
  and bundle-workspace tests to prove both UI routes reach identical global
  Name direction/indicators, preserve selection, criteria and drafts, respect
  stale/disabled state and queued-sort ownership, and leave V1 native behavior
  and Package/header pointer behavior intact. Add Button peer/keyboard tests;
  headless tests are not Windows acceptance.
- Extend runner pure regressions for reversed/scrambled UIA enumeration,
  ascending/descending exact sequences, wrong/partial/duplicate rows, ambiguous
  or offscreen bounds, unsupported Invoke/focus/state, and initial sort direction.
  No catch that turns unavailable evidence or failed assertions into PASS.
- Cross-build, run available Search/runner regressions and workflow lint, then
  execute the existing manual Windows workflow at the exact reviewed commit.
  Inspect every matrix artifact. Only actual Windows evidence can resolve
  CP21-08; never extrapolate from a Linux build or source review.

This proposal does not resolve the [separate acceptance gaps](CHECKPOINT-2.1.md)
(nontrivial Package sorting, non-default numeric/type retention, text before
admission, metadata/mixed-exclusion and indeterminate coverage feedback).
No issue status or final user acceptance changes, even if Name sorting later
passes. The visible change was separately authorized as recorded above; actual
Windows acceptance remains pending.
