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
git rev-parse HEAD
Get-FileHash E:\DisposableFixtures\Smoke.uabs.sqlite -Algorithm SHA256
```

Edit `scenario.local.json`: set `expectedCommit` to the full checkout SHA,
`packageSha256` to that hash, and supply the approved disposable package,
known indexed keyword, and exact expected bundle/package display names. Do
not infer expected results from this runner's observed output.

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
   patterns, exactly Name/Package columns, and the complete expected row set.
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
checkboxes are found by type/name within SearchControlsPanel; the field
ComboBox must be unique. Ambiguity or inaccessible cells fail explicitly.
Grid virtualization, native picker discovery, accessible cell text, desktop
capture, and provider responsiveness require **actual Windows execution**.
Linux cross-compilation cannot verify any of them. Full Checkpoint 2.1,
including selector retention, errors/tooltips, V1, sorting, selection, and
Refresh, remains pending regardless of this single scenario's result.

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
```

Source verification: dependency restore and Release cross-compilation on Linux
passed with zero warnings/errors. JSON parsing, generated-file ignore rules,
and whitespace checks passed. PowerShell execution and Windows UI Automation
have not been run in Cloud.

## Remote handoff

Codex Remote supplies reviewed source and a scenario template. The user
fills local paths and starts this finite script; the resulting evidence is
uploaded for review. No shell commands are accepted from the scenario JSON,
and the tool performs no Git writes or network listening. Generated binaries,
local configuration, DB copies, screenshots, and logs must stay untracked.
