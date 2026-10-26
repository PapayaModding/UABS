# Windows UI Automation proof of concept

Standalone FlaUI.UIA3 5.0.0 runner for the first scenario in
[Checkpoint 2.1](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-search-defaults.md#limits-and-manual-acceptance).
It launches the existing Debug application and uses UI Automation patterns;
there is no coordinate fallback, listener, service, or production reference.
It does not establish acceptance of the remaining checkpoint scenarios.

## Prerequisites

- Windows with an unlocked interactive desktop reserved for the run; no UAC
  prompts. Run UABS and the runner without elevation and at the same privilege.
- PowerShell 7, Git, .NET 9 SDK and Windows desktop runtime. Initial NuGet
  restore needs outbound access. Unity Editor is not required.
- `E:\Projects\UABS` on `v2-recovery`, with no tracked changes, and a current
  `Avalonia\UABS\bin\Debug\net9.0\UABS.exe` built from that checkout.
  The runner records the EXE hash; it cannot prove that an existing binary
  matches source, so rebuild it immediately before testing.
- A **disposable, fully indexed V2 `.uabs.sqlite` package**, closed and
  checkpointed with no `-wal`/`-shm` sidecars. Published Unity bundles alone are
  not an indexed package. Independently establish a keyword and the complete
  expected bundle Name/Package pairs (at most 100 loaded matches).

## Run on Windows

Build UABS using the repository's normal Debug build procedure. Then:

```powershell
Set-Location E:\Projects\UABS
dotnet build Avalonia/Avalonia.sln -c Debug
Copy-Item tools/windows-ui-poc/scenario.example.json tools/windows-ui-poc/scenario.local.json
Get-FileHash E:\DisposableFixtures\Smoke.uabs.sqlite -Algorithm SHA256
```

Edit `scenario.local.json`: set `packageSha256` to that hash, and supply the approved disposable package,
known indexed keyword, and exact expected bundle/package display names. Do
not infer expected results from this runner's observed output.

The PowerShell launcher resolves the configured repository's full HEAD SHA
automatically. It requires that exact working-tree root and an attached
`v2-recovery` branch; invalid Git output, detached HEAD, missing Git, and
unexpected branches stop execution. It never pulls or switches branches.
`expectedCommit` in the local scenario can be absent or stale: the launcher
copies the parsed configuration to a temporary JSON file and replaces only
that property. Your original JSON is never written. The temporary file is
removed in `finally`, including build, startup, and watchdog failures.
The runner still independently verifies the branch and expectedCommit, so a
checkout change between preparation and runner startup is rejected.
The launcher prints the tested SHA and records repository/branch/commit in
`checkout.json`, including launcher execution failures after resolution.
The runner's existing preflight evidence also records the commit.

```powershell
pwsh -File tools/windows-ui-poc/Run-WindowsUiPoc.ps1 `
  -Scenario tools/windows-ui-poc/scenario.local.json
```

The script builds the isolated runner and bounds execution to 180 seconds
(configurable 60–600). It kills only the runner's process tree if UIA stalls.
It never attaches to or closes a pre-existing UABS process.

## Scenario and evidence

1. Validate Windows, branch, commit, tracked cleanliness, executable, SQLite
   signature, and input package hash. Copy the DB to a fresh output/work
   directory. Launch UABS with that working directory so `uabs_log.txt` is
   isolated from the user's normal log.
2. Capture the initial UABS window and UIA element/pattern inventory.
3. Select Packages and observe Open V2 invocation concurrently with discovery.
   Identify the picker by its exact application-configured title,
   `Open read-only UABS V2 package`, and the launched process ID, among both
   top-level windows and nested UIA windows. Its class is recorded rather than
   assumed to be `#32770`. Operate the real Windows Common Item Dialog through
   IDs 1148 (filename) and 1 (Open). Unsupported dialog layouts are BLOCKED;
   no keystroke or coordinate workaround is attempted.
   Filename discovery waits up to 25 seconds for exactly one **Edit** with
   ID 1148; the enclosing ComboBox with the same ID is excluded. Duplicate
   Edits block immediately. The Value pattern must exist and be writable;
   the absolute disposable path is set and read back before invoking the
   unique Button with ID 1. Missing/ambiguous controls or a value that never
   confirms produce BLOCKED with `dialog-filename-failure.json` and picker
   screenshot/UIA evidence where available.
4. Select Search and inspect untouched Sources-only / Assets-field defaults.
   Selectors are never changed to make the scenario pass.
5. Set IncludeSearchTextBox and invoke SearchButton. Verify Table/Grid
   patterns or the strict structural accessibility path below, exactly
   Name/Package columns, and the complete expected row set.
6. Capture final evidence and close only the launched application. Confirm
   the original input DB hash is unchanged.

Each run uses an ignored `artifacts/<run>/` directory containing
`results.json` (PASS/FAIL/BLOCKED, details, timestamped actions and observed
rows), `initial.png`, `final.png` or `failure.png`, corresponding UIA JSON
inventories, and `work/uabs_log.txt` when UABS writes a log. Missing patterns
and discovery/timeouts are BLOCKED; observed assertion failures are FAIL.
Exit codes: 0 PASS, 1 FAIL, 2 BLOCKED. Screenshots require an unobscured UABS
window; window capture may include anything overlapping its rectangle.
Screenshots and UIA text can contain local paths and package names: review
before sharing. Upload only reviewed evidence, not the disposable DB copy.

The current IDs come from existing Avalonia `x:Name` fallback. Unnamed
checkboxes are found by type/name within the verified SearchControlsScroller
Pane (SearchControlsPanel is not exposed on Windows); the field
ComboBox must be unique. Ambiguity or inaccessible cells fail explicitly.
Grid virtualization, native picker discovery, accessible cell text, desktop
capture, and provider responsiveness require **actual Windows execution**.
Linux cross-compilation cannot verify any of them. Full Checkpoint 2.1,
including selector retention, errors/tooltips, V1, sorting, selection, and
Refresh, remains pending regardless of this single scenario's result.

### Search accessibility paths

Untouched defaults are read from real Toggle states and ComboBox evidence
within SearchControlsScroller. The unique ComboBox's supported Value is
preferred; a uniquely named selected item is also usable. Avalonia 11.3.3's
[ComboBox peer](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Controls/Automation/Peers/ComboBoxAutomationPeer.cs)
exposes read-only Value and an unrealized selected-item peer when collapsed.
A missing selected-item Name does not prove the displayed value is wrong.
When those representations are unavailable, exactly one visible Text
descendant of the confirmed collapsed ComboBox can establish the field.
No option is selected, expanded, or changed during verification.

`field-selection.json` and result steps record support flags, raw Value,
selected names/unavailable markers, expansion state, visible text, and verdict.
Only a recognized, unambiguous field value can pass. Known non-Assets values
fail; missing, multiple, unknown, or conflicting evidence blocks. Unsupported
properties remain unavailable, never assumed to mean Assets. The screenshot
does not supply an assertion value. Keyword entry uses Value and exact Unicode readback
(including `テスト`) before SearchButton Invoke.

The Grid/Table path remains available when those patterns are supported.
Avalonia DataGrid 11.3.3 also exposes a structural path: its
[grid peer](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/Automation/Peers/DataGridAutomationPeer.cs)
has no Grid provider, while row peers expose DataItem, cell peers expose
Custom controls of class DataGridCell, and column header peers expose
HeaderItem. The fallback requires ordered Name/Package headers and two
DataGridCell descendants per DataItem, each with exactly one Text descendant.
Pairs are assembled within their row in column order in this freshly launched,
unreordered grid; text is never flattened across rows or associated by name
matching. There is no OCR or mouse-coordinate inference.

Completeness must be independently visible in the application's count text:
`1–N displayed / N exact indexed matches`. Exactly N DataItems (at most 100)
must be represented. Partial counts, virtualized/missing rows, missing or
ambiguous cells/headers, and unavailable UIA reads produce BLOCKED. Narrow
the keyword to a small result set if necessary. Observed headers, cell pairs,
and count text are recorded in result steps. The final expectedRows comparison
is still exact, including package association and duplicate multiplicity;
an accessible but incorrect result set is FAIL.

This path is supported by the peer source and the reported Windows DataGrid
limitation. Its actual hierarchy/text exposure still requires Windows
execution; Linux regression tests do not establish UI acceptance.

### Package admission investigation

The tracked `OpenV2PackageAsync` always awaits
`AvaloniaFilePickerService.PickV2PackageAsync`, which calls the owner's
`StorageProvider.OpenFilePickerAsync` with the title above. Avalonia 11.3.3's
Windows provider uses the COM file dialog on a worker task. There is no
startup V2-registration restoration in the inspected composition:
`MainWindowViewModel.InitializeAsync` initializes the directory browser;
`V2BrowseViewModel` starts with an empty catalog and registration collection.
Catalog scanning or opening a package explicitly can register packages.

The Windows report of a loaded Smoke package after discovery timed out does
not establish its file identity or explain the invocation timing. The earlier
runner assumed Invoke had returned before the dialog needed observing and
required class `#32770`. Those assumptions have been removed, but the precise
Windows cause remains unproven until the new evidence is collected.

`dialog-before-invoke.json`, up to four periodic snapshots, and
`dialog-discovery-end.json` record invocation state and at most 30 windows
from the launched process, including titles, classes, handles, visibility,
enabled state, and at most 120 descendant UIA records per window. Discovery
retains a 25-second deadline and the external watchdog. No other process's
window is operated or captured. `open-dialog.png` records the discovered
picker before its filename is set.

Native handles are serialized as signed 64-bit JSON integers (zero is a valid
observed value), never as `IntPtr`. Unsupported handle reads retain the
unavailable/reason marker. Snapshots include the expected process/title and
an invocation object with pending/completed/faulted/canceled/not-started
state, raw task status, and fault details. If window enumeration fails, its
unavailable marker is written alongside that invocation state; subsequent
snapshot attempts still run. File-write errors are recorded in results steps.
Window records include AutomationId, control type, title (`name`), class,
process ID, and handle. The exact-title/process ownership checks and discovery
deadline are unchanged. Timing, a different title, or a window absent from the
owned UIA tree remain hypotheses until Windows diagnostics establish them.

Admission without an observed picker is allowed only when a single V2 scope
exposes the **exact disposable-copy absolute path** through UIA HelpText and
that copy still has the approved SHA256. Display name, source/asset counts,
package GUID, or generation GUID alone do not qualify. The current XAML binds
the scope path to a tooltip, so HelpText may not expose it: in that case the
runner keeps waiting and reports BLOCKED instead of guessing. The fresh
native-dialog path is retained. After either path, exactly one loaded package
scope must exist, it must be selected, and the disposable-copy hash must
remain unchanged. Search defaults and results are still asserted strictly.

UIA inventory fields that throw (including unsupported properties or elements
that disappear) are recorded as `{ "available": false, "reason": "..." }`.
Other fields and elements continue to be collected. This tolerance applies
only to evidence; scenario discovery, required patterns, and assertions retain
their strict reads. Screenshot or tree-enumeration failures still surface.
Run the platform-independent diagnostic-reader regression checks with:

```powershell
dotnet run --project tools/windows-ui-poc/tests/EvidenceChecks.csproj
pwsh -File tools/windows-ui-poc/tests/ScenarioConfiguration.Tests.ps1
```

Source verification: dependency restore and Release cross-compilation on Linux
passed with zero warnings/errors. JSON parsing, generated-file ignore rules,
and whitespace checks passed. The scenario-configuration PowerShell regression
script and launcher syntax checks ran using PowerShell 7 on Linux. The full
Windows launcher and Windows UI Automation have not been executed in Cloud.

## Remote handoff

Codex Remote supplies reviewed source and a scenario template. The user
fills local paths and starts this finite script; the resulting evidence is
uploaded for review. No shell commands are accepted from the scenario JSON,
and the tool performs no Git writes or network listening. Generated binaries,
local configuration, DB copies, screenshots, and logs must stay untracked.

## GitHub-hosted Windows proof of concept

Checkpoint 2.1 expansion: see the [requirement-to-test matrix](CHECKPOINT-2.1.md).
The workflow now has independent `smoke`, `checkpoint21-v2`,
`checkpoint21-v1`, and `checkpoint21-gaps` matrix jobs. The new test-only
[G2/G3/G4 slice](ACCEPTANCE-GAP-SLICE.md) covers non-default retention,
pre-admission text/defaulting and metadata-exclusion feedback; its Windows
execution is pending. Smoke keeps its existing path; expanded jobs
set optional `acceptance` in a generated scenario and use a 600-second
watchdog. Each uploads separate artifacts, including per-case
`acceptance.json`. The user confirmed a later remote run passed Smoke, V1,
and the original implemented V2 cases, including Name sorting, selection and mixed-format rejection. The latest
[coverage matrix](CHECKPOINT-2.1.md)
records those user-confirmed outcomes and the source investigation. CP21-09 has
a guarded UIA-focus/keyboard selection path with exact Inspector identity
retention. The separately approved
[Name-sort accessibility implementation](NAME-SORT-ACCESSIBILITY-PROPOSAL.md)
adds one V2-only standard Sort Name button beside Refresh, with current/next
direction HelpText and a wrapping results footer. CP21-08 now requires real
Button Invoke and focused Enter and exact ascending/descending visual order
against the independent fixture oracle. Complete-set, ownership, visibility and
geometry gates remain strict. That Name-sort case passed the user-confirmed
baseline run 40259657455; new G2/G3/G4 cases still need Windows execution;
separate Checkpoint 2.1 coverage gaps remain open.

The subsequent CP21-09 comparison confirmed reversed UIA enumeration relative
to Ctrl+Home navigation. Boundary expectations now come from verified,
non-overlapping row bounding rectangles below the grid headers, with all
expected rows fully visible. Bounds are diagnostic evidence only: there is
no coordinate-based input. See the matrix's confirmed row-order section for
the strict layout gates and evidence files. The latest user-confirmed run
passed selection; future behavior changes still require their own Windows run.

Hosted layout/provider hardening: CP21-08 and CP21-09 maximize the owned test window
through UIA and logs before/after monitor/window/panel/scroll geometry. It
still blocks on a nonpositive or clipped grid viewport. Catalog reads use
bounded fresh-element discovery; provider COM failures remain explicit BLOCKED
outcomes, never a substitute for mixed-format testing. No UI action is retried.
See the coverage matrix's hosted layout/provider section for retry codes and
evidence filenames. Smoke/V1 assertions and production behavior are unchanged.

`.github/workflows/windows-ui-poc.yml` runs **manual-only** jobs on
`windows-latest`. It runs only for `PapayaModding/UABS` on `v2-recovery`, with
`contents: read`, pinned checkout/setup/upload actions, and no persisted Git
credentials, personal credentials, services, or desktop listener. It does not
run on pull requests or accept arbitrary commands/ref inputs.

Dispatch through Actions → **Windows UI POC - Smoke Search** → Run workflow
→ `v2-recovery`, or with an authorized GitHub CLI account:

```powershell
gh workflow run windows-ui-poc.yml --repo PapayaModding/UABS --ref v2-recovery
gh run list --repo PapayaModding/UABS --workflow windows-ui-poc.yml --limit 5
gh run view <run-id> --repo PapayaModding/UABS
gh run download <run-id> --repo PapayaModding/UABS --dir ui-evidence
```

The checkout must remain attached to `v2-recovery` and HEAD must equal the
immutable dispatch `github.sha`. If the branch advances before checkout, the
job blocks and must be dispatched again; it never resets or substitutes a
different commit. Paths come from `GITHUB_WORKSPACE`. The existing launcher
resolves this same HEAD and the runner independently verifies it.

The job builds UABS Debug, cross-checks the runner's regressions, and uses the
isolated `ci/PrepareSmoke.csproj` tool to index the three bundles explicitly
listed in the published Smoke manifest. It verifies their sizes/hashes before
indexing and rechecks hashes afterwards. The collection's root Unity manifest
bundle is not a listed fixture and is excluded. Preparation calls the existing
public PackageBuilder/AssetsTools extractor with real bundle bytes, rejects
partial indexing/issues, and never changes production behavior or fixtures.

The fixed expectation for the published Smoke seed is keyword `テスト`, exactly
one row `{ Name: uabs-smoke-0000.bundle, Package: Smoke }`. The tool checks the
manifest assignments and independently reads all raw indexed asset names with
read-only SQLite queries to prove this expectation. It never derives expected
results from the Search engine or UI output. `preparation.json` records hashes,
package/generation identity, counts, and matching raw asset witnesses. A fresh
scenario points to the generated SQLite package; the existing runner copies
and SHA-verifies it as usual.

The Smoke FlaUI run has a 180-second watchdog and each job a 15-minute timeout.
It records the hosted image, interactive-session flag, session ID, exact SHA,
and run URL in `environment.json`. Hosted Windows is a candidate graphical
environment, **not a guarantee of usable UIA**: the real existing launch,
dialog, control, and exact-result assertions decide PASS/FAIL/BLOCKED. No
assertion is relaxed for CI. Missing desktop access or required UIA exposure
must remain BLOCKED. Checkpoint 2.1 stays open even if this one scenario passes.

Evidence uploads run even on failure: `ci-results.json`, environment and
preparation JSON, runner results/checkout/UIA/diagnostic JSON, screenshots,
and the isolated application log. The workflow publishes a result summary and
retains artifacts for seven days. It excludes SQLite copies, local scenario
configuration, bundles, and binaries. Build/preparation errors leave no UI
result and are summarized as BLOCKED; inspect the job logs for their cause.

Verified remotely during implementation: the real published bundles indexed
on Linux into a complete 45-asset, three-source package with zero issues, and
the independent Unicode expectation check passed. This validates preparation,
not the hosted Windows desktop. If the hosted job cannot access an interactive
desktop, use a dedicated disposable Windows VM with an unlocked interactive
test session and the same finite runner as a separately authorized fallback.
Do not use a shared privileged self-hosted runner for untrusted code, enable
autologon, or install remote desktop services as part of this proof of concept.
