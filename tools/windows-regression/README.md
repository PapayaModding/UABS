# Standalone full Windows application regression

Manual [workflow](https://github.com/PapayaModding/UABS/actions/workflows/windows-application-regression.yml):
`windows-application-regression.yml`, **v2-recovery only**. Separate from the
existing six-job Windows UI workflow, which is unchanged. No production changes,
FlaUI scenarios, assertion edits, test filters, retries, parallelism overrides,
new test skips, dependency upgrades or generated fixture publication.

The user visually verified the Search Results layout at
`8875ba4c9468b5fc8da64eb1d9cbba398d4b158e`. This new workflow runs the full
application regression suite; that visual confirmation does not automatically
close G6 or Checkpoint 2.1. Both remain open pending the user's decisions.

## Exact checkout and execution

Dispatch on recovery (never main):

```sh
gh workflow run windows-application-regression.yml --repo PapayaModding/UABS --ref v2-recovery
```

The workflow permits only manual dispatch for PapayaModding/UABS and the recovery
ref. Checkout preserves an actual recovery branch with credentials removed. Git
must resolve the correct repository root, approved origin, branch and full HEAD
equal to the exact workflow GITHUB_SHA; detached HEAD, another branch/repository,
or a branch advance between dispatch and checkout blocks execution. It never
pulls, switches, resets, merges or silently substitutes a different commit.
`environment.json` records verification, SHA, runner image/version, OS and run URL.

GitHub-hosted windows-latest uses .NET9.x and existing repository dependencies.
The workflow restores and builds
`Avalonia/UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj` in Debug, including
its application/library references and fixture content, then executes exactly:

```powershell
dotnet test Avalonia/UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj -c Debug --no-build --no-restore --nologo `
  --logger 'trx;LogFileName=windows-full.trx' --logger 'console;verbosity=normal' `
  --results-directory tools/windows-regression/artifacts/test-results `
  --blame --diag tools/windows-regression/artifacts/vstest.log
```

This is the **complete application test project**, not runner checks, compilation
alone or a selected test list. Existing source-defined skips remain reported,
including the hardlink-permission skip; none are newly suppressed. Normal xUnit
parallelism is preserved, so previously flaky Activity-counter assertions can
still fail and will be reported honestly. No personal credentials or secrets are
provided; permissions are contents:read only. No Unity Editor, desktop listener,
remote-control software or local PC access is required.

## Evidence and failure handling

Console output is tee'd to a complete file and the native test exit code is saved
before further processing; nonzero exits fail the test step. The suite step has
a15-minute bound and the job a30-minute bound. The summarizer and artifact upload
use always(), including restore/build/test failures. If the hosted machine is
lost or GitHub forcibly terminates the entire job, upload cannot be guaranteed;
missing evidence remains incomplete/BLOCKED, never an inferred PASS.

The artifact `windows-full-application-<SHA>-<attempt>` retains for14 days:

- environment.json, dotnet-info.log, restore.log, build.log, console.log;
- execution.json with start/end/exit/SHA, complete windows-full.trx;
- VSTest diagnostic logs and any blame Sequence.xml (no process dumps requested);
- summary.json and summary.md; summarizer self-test output.

No databases, bundles, binaries or machine secrets are intentionally uploaded.
Raw TRX/console retain complete messages/stacks. summary.json lists **every** test
outcome plus failed, skipped and individually aborted/error/timeout tests, reasons,
counts, run-abort signals and unfinished blame entries. A host crash may produce
no individual aborted test result: the report records a run-level abort without
inventing the responsible test. An unfinished blame entry is a diagnostic clue,
not a proved crash cause. Partial recorded counts are not a discovered-suite total.

PASS requires the verified SHA, a valid matching process exit receipt with code0,
successful test/job steps, a nonempty coherent completed TRX, and console evidence.
Skips are separately enumerated, not called passing tests or accepted features.
Assertion failures, nonzero exit or reported abort/native crash yield FAIL. Missing,
corrupt, inconsistent, timed-out or unverified evidence cannot yield PASS; unavailable
evidence without a demonstrated failure is BLOCKED. The workflow itself remains
failed for FAIL/BLOCKED. No continue-on-error or comparison-based waiver is used.

## Compare against Linux without waiving failures

[linux-baseline.json](linux-baseline.json) transcribes the exact38 failed names
from the [closure-readiness audit](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-closure-readiness-audit.md)
at `709c615abc9cd362ac03578ddc6c4b62f5716339`:731 passed,38 failed,1 skipped,
then PAL_SEHException abort. Isolated raw Search also aborted after27 passes;
isolated paging passed20/20. Groups include NTFS/kernel32 prerequisites, legacy
Windows-path expectations, replacement/memo-admission and global query counters.
This is historical evidence at a different code SHA/runtime/OS, not an expected-
failure allowlist. The current layout also adds two headless test cases.

The summary records exact overlapping Windows failures, failures absent from that
Linux set, **actual outcomes for each of the38 Linux failures** (including skipped
or not-reported), and native crash signals. Missing/skipped is never “fixed”. A new
unmatched Windows failure is not automatically a new production regression, nor
does overlap make a failure harmless. Any Windows assertion failure stays FAIL.
No claim that the PAL risk is resolved is possible until complete real Windows
results are inspected; even a Windows pass does not diagnose the Linux native cause.

## Local checks and next review

```sh
python -m unittest discover -s tools/windows-regression/tests -v
actionlint .github/workflows/windows-application-regression.yml
```

The parser tests use synthetic TRX evidence, not invented Windows results. They
cover completed pass/skips, failure overlap without waivers, native/individual
aborts, blame evidence, missing/corrupt/inconsistent TRX, nonzero/missing exits,
wrong branch/commit/repository and missing console. The inventory was independently
compared with the retained real Linux TRX: all38 exact names match.

Local validation (2026-10-26):13/13 summarizer tests passed, actionlint passed,
and all five inline PowerShell blocks parsed successfully without executing Windows
commands. No complete Windows application run has been observed in this session.

After manual dispatch, verify environment.json and actual project/test invocation,
inspect the complete TRX and failed/skipped/aborted lists, and compare summary.json
with raw logs. A workflow source implementation or Linux parser test is not a
Windows full-suite result. Preserve all failures for explicit disposition; do not
close Checkpoint2.1 or begin Checkpoint3 automatically.
