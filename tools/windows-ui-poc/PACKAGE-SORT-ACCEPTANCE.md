# G1 Package sorting — approved implementation, Windows pending

Authority: explicit user approval of the [proposal](PACKAGE-SORT-ACCESSIBILITY-PROPOSAL.md)
at `571fdf2d3300e2bf4ce95ea87f6668683f6f610f`. The implementation is present;
this is **not** a claim of Windows acceptance. G5 indeterminate-coverage feedback,
G6 tooltip equivalence, Linux full-suite regression disposition and Checkpoint 2.1
remain open. No Checkpoint 3 work or fixture dataset publication.

## Changed components

| Files | Responsibility |
| --- | --- |
| `Avalonia/UABS/Script/View/Pages/SearchWorkspaceView.axaml` and `.axaml.cs` | One standard V2-bundle-only Sort Package button beside Name/Refresh; same GlobalSort/BeginHeaderSort path, availability guards and current/next/busy HelpText. No new comparator, columns, schema, ViewModel or dependency changes. |
| `V2PackageSortButtonTests.cs`, `V2PackageSortAccessibilityAuditTests.cs`, bundle-workspace/header/memo test updates | Invoke/Tab/Enter/Space, footer wrapping at 900/1300 width, direction/tie/criteria/selection, stale/disabled-grid guards, queued/cancelled ownership, shared drafts and unchanged V1/raw-mode visibility. Package header pointer route remains supported; its peer still lacks Invoke. |
| `PackageSortContract.cs`, `PackageSortInteraction.cs`, optional hooks in `Program.cs` | Independent fixture/sequence contract, canonical identity/hash checks, native catalog admission, actual Package UI action, validated geometry and strict PASS/FAIL/BLOCKED. |
| `ci/package-sort/PreparePackageSort.csproj` / `Program.cs` | Two real independent PackageBuilder databases from unchanged published bundles. No Search API/comparer or synthetic metadata. |
| `ci/PrepareSmoke.csproj` | Excludes the nested new preparer's sources to preserve the original program's build; original Smoke preparation logic is unchanged. |
| `tests/test_package_sort_fixture.py`, runner contract tests | Read-only raw SQLite/hash/UUID/witness cross-check, tampered-expectation rejection, exact primary/tie oracle, active-field and unsupported-action regressions. |
| `.github/workflows/windows-ui-poc.yml`, local `.gitignore` and documentation | One isolated optional fifth matrix job; existing four preparation/scenario paths, read-only permissions, SHA verification and failure artifact uploads preserved. Only the new project file is allowlisted; generated DBs/binaries/logs remain excluded. |

The footer's existing WrapPanel handles the new action without redesign. Tests
force wrapping and verify all actions remain within the footer; actual hosted
window layout and all five jobs still require the new Windows run. Existing
selection-restoration/versioning, revision guards and confirmed-query sorting
are reused, not replaced with local-window sorting or an implicit Refresh.

## Fixture and oracle

The new preparer verifies the published Smoke profile/seed/LZ4 metadata, exact
three-bundle allowlist, sizes/SHA-256 and unlinked input paths. It builds Alpha
from 0002/0001 in reverse input order and Zeta from 0000 with distinct new
package/generation/source/revision identities. It independently admits each DB,
checks actual labels, source counts, complete indexing and zero issues, then
reads raw SQLite metadata and asset-name witnesses for `asset`. Stored source
UUIDs are decoded in **big-endian** format and checked against the build request.
Both readers close before database hashes are recorded; sidecars are rejected.
The original input hashes are rechecked after extraction. `preparation.json`
records actual PackageInfo, all source evidence, paths, UUIDs and hashes.

Persisted full-string independent sequences (abbreviated here):

| Direction | Exact `(Name, Package)` sequence |
| --- | --- |
| Ascending | `(0001, Alpha)`, `(0002, Alpha)`, `(0000, Zeta)` |
| Descending | `(0000, Zeta)`, `(0001, Alpha)`, `(0002, Alpha)` |

`000N` means `uabs-smoke-000N.bundle`. Alpha ties stay ascending in both directions.
No production comparer, observed UI order, or whole-array reversal produces the
expectation. Unique original paths with a shared directory and empty index
directories establish SourcePath ties independently; UUID fallback is not reached.
Case/culture edge cases and duplicate package labels are not invented here.

## Actual Windows scenario

Select `acceptance: checkpoint21-package-sort` in the generated scenario. Common
preflight checks still require the exact recovery checkout, Debug executable and
primary package hash; the new mode additionally validates and copies both DBs.
It starts a fresh owned process with no loaded scopes and opens its disposable
catalog through the established native folder picker (not direct VM admission).
Exactly two selected scopes must expose the expected copy paths, hashes and
labels. Original and copied DBs and published inputs are verified again after
each action and after the owned app closes.

`CP21-G1-package-order` runs the supported `asset` query, checks all three rows and
complete count, maximizes through the existing owned-window UIA pattern and
derives visual order only from strictly validated non-overlapping in-viewport
row rectangles paired with Name/Package identities. It selects a real boundary
row with guarded keyboard input, validating Inspector package/generation/source/
revision UUIDs and DB/source paths against raw-prepared metadata. Transient
unavailable selection can wait boundedly; a settled readable wrong canonical
identity is FAIL. Ephemeral registration IDs may legitimately change on sorting.

The case enters an unexecuted Include edit, then activates **Sort Package** through
Invoke, ordinary Tab from Name then Enter, and focused Space. Each action must
complete with Package HelpText/header active and Name inactive; a proved switch
to a different primary field is FAIL, not a matching-row-order PASS. Both exact
directions, Alpha tie order, full row multiset/count, canonical selected identity,
all live criteria/flags/selectors, confirmed last-executed `asset` criteria and
both scope/path/hash identities must remain correct. Sorting intentionally
re-admits the confirmed query; no zero-session assertion from G2 is reused.

Missing/ambiguous controls, unsupported patterns, incomplete/clipped geometry
or unverifiable state are BLOCKED. Wrong complete results/order, changed criteria,
canonical selection or hashes are FAIL. Missing evidence is never guessed by
coordinates, OCR, input values, or UIA descendant enumeration.

## Reproduce validation / execute

```powershell
$repo = (Get-Location).Path
$sha = git rev-parse HEAD
dotnet run --project tools/windows-ui-poc/ci/package-sort/PreparePackageSort.csproj -- $repo "$repo/tools/windows-ui-poc/artifacts/package-sort/prepared" $sha
python tools/windows-ui-poc/tests/test_package_sort_fixture.py --repository $repo --prepared "$repo/tools/windows-ui-poc/artifacts/package-sort/prepared"
dotnet run --project tools/windows-ui-poc/tests/EvidenceChecks.csproj -- --package-fixture "$repo/tools/windows-ui-poc/artifacts/package-sort/prepared/scenario.local.json"
pwsh -File tools/windows-ui-poc/Run-WindowsUiPoc.ps1 -Scenario "$repo/tools/windows-ui-poc/artifacts/package-sort/prepared/scenario.local.json" -TimeoutSeconds 600
```

Use a new output directory each time; do not delete unrelated artifacts or change
the existing scenarios. Before local execution, build the actual UABS Debug
solution. The launcher independently resolves and verifies the current recovery
commit. No machine-specific checkout paths or Unity installation are required.

The manually dispatchable [Windows workflow](https://github.com/PapayaModding/UABS/actions/workflows/windows-ui-poc.yml)
adds `checkpoint21-package-sort` to the matrix, conditionally prepares its two DBs
and runs the independent Python fixture regressions before real UI admission.
The original four jobs use their old preparer and scenario settings. Inspect
all five actual jobs; do not infer their result from compilation or a previous SHA.

Artifacts include `preparation.json`, `results.json`, `acceptance.json`,
`CP21-G1-fixtures.json`, `CP21-G1-window-*`, `CP21-G1-layout-*`,
`CP21-G1-inspector-*`, `CP21-G1-completion-*`, `CP21-G1-action-*`, per-action
screenshots/UIA trees and the app's logs. Evidence is uploaded on failure too.
No generated SQLite databases or bundles are committed/published as datasets.

## Implementation validation (Linux, 2026-10-26)

- Debug solution, Release Windows-targeted runner and original Smoke preparer:
  builds succeeded without warnings or errors. New Alpha/Zeta preparer builds
  successfully with NU1900: NuGet's vulnerability feed is unreachable here;
  vulnerability-audit availability is not asserted.
- Focused application accessibility/sorting/criteria/memo suites: 70/70 passed.
  Broader combined application selection: 191/193 passed. The two failures were
  `V2SearchPagingPerformanceTests.SelectiveCriteria_DoNotForceTheAsciiIndex`
  (`class` and `identity`, expected query count 1, observed 2). Isolated
  paging/performance rerun: 20/20 passed. This reproduces the audit's shared
  Activity-counter concern; it is not a green full Linux suite or proof that
  all existing Linux failures are explained.
- Runner evidence/interaction contracts passed, including exact Package/tie
  ordering, active-field evidence, supported actions and geometry ambiguity.
  Real PackageBuilder extraction succeeded; all three independent Python
  read-only fixture tests passed, including seven tampered-expectation cases.
  Actual prepared metadata also passed the runner JSON/identity contract.
- Workflow actionlint and Git whitespace validation passed. Windows UIA,
  native folder admission, actual layout/keyboard activation and preservation
  of all four previously passing Windows scenarios remain pending a new run.
