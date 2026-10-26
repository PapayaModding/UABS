# CP21 G5 — indeterminate exclusion coverage (Windows pending)

Authority: [final gap audit G5](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md#g5--controlled-incomplete-eligible-bundle-not-merely-a-partial-catalog)
and [Checkpoint 2.1 requirements](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-search-defaults.md).
Test infrastructure only: no production changes, SQL flag patching, Search stub,
new bundle publication or Checkpoint 3 work. Checkpoint 2.1 stays open.

The user confirms all five jobs in [run 40365210988](https://github.com/PapayaModding/UABS/actions/runs/40365210988)
passed at `a1bb77b694545c25e90d5b07b0c894b664d4302f`, including G1 Package sorting.
That is the preserved Windows baseline, not evidence for the new G5 job.

## Investigation and fixture

`PartialEligibleBundle_BlocksWholeScope_NoSilentSkippingOrFalseExactCount`
constructs verified complete/partial **bundle** evidence through IV2MetadataExtractor
and PackageBuilder with partial acknowledgement. `CompleteEligibleBundles_CanExcludeDespiteUnrelatedPartialPackage`
shows a partial serialized source or unverified bundle does not trigger the gate.
V2SearchService captures eligibility from source kind=bundle, complete/partial
source status and non-null content hash/size, not the package's partial label.
Eligible incomplete coverage blocks asset-name/memo exclusions before querying
any selected source. Dependency warnings alone do not qualify.

`ci/indeterminate/IndeterminateFixtureBuilder.cs` follows that metadata contract
recipe, without the backend fixture's unrelated memo SQL seeding. Two separately
built databases use small JSON provenance inputs: **explicit synthetic metadata
contract fixtures, not Unity-extracted bundles**. Source kind represents eligible
bundle evidence in the DB; the JSON is never claimed to be a real AssetBundle.
Published Smoke inputs/manifests are not read or modified by this preparer.

| Fixture | Source / indexed witness | Coverage |
| --- | --- | --- |
| G5 complete synthetic metadata | `g5-complete.metadata.json` / `g5_known_asset` | Verified bundle, one logical file, declared=1, captured=1, complete |
| G5 partial synthetic metadata | `g5-partial.metadata.json` / `g5_known_asset` | Verified bundle, one logical file, declared=2, captured=1, partial |

The partial contract deliberately withholds one declared asset and emits the
typed `g5-deliberate-incomplete` table diagnostic. PackageBuilder's acknowledgement
callback approves only the expected summary (one asset, one matching issue).
A refusal regression proves no partial database is published. No SQLite mutation
occurs after builder publication/admission; no unknown asset content is invented.

PackageReader admission and read-only raw SQL verify labels, statuses, source
kind, canonical paths, source hashes/size, declared/captured counts and witnesses.
Reader closure/absence of sidecars precedes DB hashing. Independent Python reads
actual SQLite/UUIDs/issues and input JSON, verifies all eight distinct identities
and hashes, and rejects corrupted coverage/hash/identity/label expectations without
calling Search. `preparation.json` records provenance, hashes, identities, expected
rows, keyword, exclusion and the full independently specified backend diagnostic.

## Admission gate and evidence limits

Before adding the runner, a headless test built both packages, loaded them through
the actual `PackageManagementViewModel.LoadCatalogAsync`/V2BrowseCatalog path and
created the real Search view. Normal Search services admitted both, returned both
indexed witnesses, rejected exclusions with the full expected diagnostic, retained
rows/count/confirmed criteria/selection, and recovered after clearing exclusions.
The expanded test also verifies complete-only recovery with exclusions retained.
This proves application admission locally, not native Windows UIA/dialog admission.
No ViewModel is called by FlaUI.

The Windows job first opens its SHA-verified disposable DB copies through the
existing native catalog folder picker. Exactly two scope paths, hashes, actual
labels/statuses and supported Toggle controls must be proved before querying.
If admission fails, later phases do not run: investigate fixture design rather
than weakening guards or claiming backend proof is Windows UI acceptance.

## Isolated Windows cases

| Case | Required real evidence |
| --- | --- |
| CP21-G5-01-native-admission | Both scopes admitted; complete-only Assets/Sources query returns exactly the known complete row. |
| CP21-G5-02-indexed-baseline | Select both, query the known keyword, verify both exact rows/count and confirmed two-package criteria. Select the actual first visible row using validated geometry and owned keyboard focus; verify Inspector UUIDs and DB/source paths against preparation. |
| CP21-G5-03-indeterminate-rejection | Enter `g5_absent_exclusion`, execute Search, verify exact corrective guidance and complete original backend HelpText; retain criteria/selectors/scopes, all prior rows, count summary, confirmed query and full selected Inspector. Require visible “No new rows published.” evidence. |
| CP21-G5-04-complete-scope-recovery | Deselect only the verified partial scope, retain exclusions, execute Search and verify exactly the complete row plus cleared corrective/original diagnostic. |
| CP21-G5-05-clear-exclusion-recovery | Clear exclusions and reselect both, execute Search and verify both exact indexed rows and cleared obsolete error/diagnostic. |

The old exact **indexed** count may legitimately remain beside the rejected query
and “No new rows published.” It is checked as an unchanged historical confirmed
result, not a successful exclusion count or complete count of unknown assets.
No count is manufactured. Wrong complete rows/counts/diagnostics/criteria/identity
are FAIL; missing/ambiguous/unsupported UIA evidence is BLOCKED. Prerequisites
gate later cases, so unexecuted phases cannot be accepted.

The sixth `checkpoint21-indeterminate` matrix job preserves the original five
preparation/scenario paths, read-only permissions, exact commit verification,
disposable isolation, bounded launcher and always-upload policy. G1's shared
Inspector helper only gains an optional evidence prefix; its default is unchanged.
No mouse coordinates, OCR, production changes or synthetic sorting shortcuts.

## Execute and review

Build UABS Debug first. From `v2-recovery`, use a new output directory:

```powershell
$repo = (Get-Location).Path
$sha = git rev-parse HEAD
$prepared = Join-Path $repo 'tools/windows-ui-poc/artifacts/g5/prepared'
dotnet run --project tools/windows-ui-poc/ci/indeterminate/PrepareIndeterminate.csproj -- $repo $prepared $sha
python tools/windows-ui-poc/tests/test_indeterminate_fixture.py --prepared $prepared
dotnet run --project tools/windows-ui-poc/tests/EvidenceChecks.csproj -- --indeterminate-fixture "$prepared/scenario.local.json"
pwsh -File tools/windows-ui-poc/Run-WindowsUiPoc.ps1 -Scenario "$prepared/scenario.local.json" -TimeoutSeconds 600
```

Or manually dispatch [Windows UI POC](https://github.com/PapayaModding/UABS/actions/workflows/windows-ui-poc.yml)
on the published recovery commit. Inspect all six jobs. Artifacts include
preparation.json, results.json, acceptance.json, per-case screenshots/UIA trees,
CP21-G5-fixtures.json, baseline/layout/Inspector reports, full rejection before/
after evidence, recovery reports and application logs. Preparation failures
produce BLOCKED CI summaries; compilation alone never establishes UI acceptance.

Local validation: **122/122 focused application tests passed**, including both
G5 admission/acknowledgement cases and existing bundle/sorting/accessibility/memo
regressions. Three independent Python fixture tests, runner contracts and the
PowerShell launcher regressions passed. Debug solution, Release Windows runner,
new G5 and both existing preparers built with zero warnings/errors; workflow
actionlint, local documentation links and Git whitespace checks passed.
New Windows execution is required; G6 tooltip equivalence and Linux regression
disposition remain open. HelpText retains the original diagnostic evidence but
does not resolve literal hover equivalence. No checkpoint closure.
