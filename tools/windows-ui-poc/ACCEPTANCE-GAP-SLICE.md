# Checkpoint 2.1 G2/G3/G4 test-only slice

Authority: [final acceptance-gap audit](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md)
at `cd2e01e8e2b504b49319e51d2a42158bec41302b`. Implementation is present;
**Windows execution is pending**, not acceptance or checkpoint closure.

The manual-only Windows workflow adds one independent `checkpoint21-gaps` matrix
job. Existing `smoke`, `checkpoint21-v2` and `checkpoint21-v1` paths/assertions
remain unchanged. Every job still prepares the published, hash/size-verified
complete Smoke package with independently cross-checked Unicode result
expectations, exact branch/commit verification, disposable copy and failure
artifact upload. No new bundles, fixture dataset, machine paths or permissions.

## Cases and actual observations required

| Case | Requirement and assertions |
| --- | --- |
| `CP21-G3-pre-admission-input` | Fresh owned process, no V1/V2 scopes; bind empty writable criteria semantically; type/read back Unicode include text plus nonempty exclude, signed Path ID, Class ID and type sentinels before opening any package. Never edit kind/field selectors. |
| `CP21-G3-defaulting-retention` | After the existing real picker/admission path, verify exact copy path/SHA, all pre-entered values and untouched Sources-only/Assets defaults. Capture retained sentinels before clearing non-query fields. No sentinel query is executed. |
| `CP21-G3-independent-query` | The normal baseline query then proves the original known Unicode result set/Name/Package identities. All three G3 phases must pass; an input-only PASS is not acceptance. Missing later cases mean they were not exercised. |
| `CP21-G2-nondefault-retention` | Set distinct nonempty numeric/type/exclusion/include values, explicit Names/mixed kinds and all three exact/memo flags. Read back real changed values; compare after scope off, scope on, Assets/Packages/Search return, opening the same catalog, and explicit catalog Refresh. Check exact selected disposable package path/hash and Toggle state, not its display name. Restore supported criteria before Search. |
| `CP21-G4-metadata-exclusion` | Establish the exact known query and selected Inspector via guarded real keyboard navigation. Select `CabCodes` plus nonempty exclusion; execute and verify corrective “exclusions require Assets” text and original `InvalidRequest` / `source/mixed automatic scope` diagnostic. Verify every criterion/flag/selector, complete result set, full Inspector and copy SHA are retained. Restore Assets/clear exclusion and verify supported results and obsolete visible diagnostic clearing. |

G2 numeric/type values are retention sentinels, **not invented matching assets**:
signed Path ID `-42424241`, Class ID `114`, type
`CP21_Retention_Type_テスト`, Unicode exclusion `__cp21_exclusion_保持__`.
They are never submitted to Search. G4 tests validation/rejection before matching
metadata; no CAB-name hit or additional fixture capability is assumed.

## Deterministic control identity, not textbox order

The production controls have one existing include AutomationId and four unnamed
numeric/type/exclude TextBoxes. Inspection of pinned Avalonia 11.3.3
[TextBoxAutomationPeer](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Controls/Automation/Peers/TextBoxAutomationPeer.cs),
[ControlAutomationPeer](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Controls/Automation/Peers/ControlAutomationPeer.cs)
and [Fluent TextBox template](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Themes.Fluent/Controls/TextBox.xaml)
establishes the Value provider and visible named `PART_Watermark` TextBlocks.
No unsupported inference that the Edit's own Name equals its watermark is used.

Initially empty Edits are bound to roles by **exact unique descendant watermark
text**, then to their supported UIA RuntimeIds. Include additionally must agree
with `IncludeSearchTextBox`. Every later read reacquires the same RuntimeIds
within the owned SearchControlsScroller. Neither descendant position nor an
expected sentinel value is used to rediscover a role. Duplicate/missing labels,
unsupported Value/runtime identity, disabled/read-only controls, or replaced
identities are BLOCKED; actual changed values are FAIL. There is no “clear it
to reveal a watermark” fallback after typing or transition.

The headless control-peer test experimentally verifies the actual application
template's unique watermark exposure, standard writable Value providers and
peer/value retention across tab switches. **It does not prove hosted Windows
UIA RuntimeIds or native dialog behavior**; those require the new job's evidence.
Fresh preparation refuses persisted/nonempty role values or loaded scopes rather
than silently removing unrelated packages/state. Read-only COM queries use the
existing bounded reader; actions are not retried. Provider/action COM failures
are BLOCKED with HRESULT diagnostics, not success. No coordinates/OCR/VM calls.

## Evidence and execution

Every subcase records `acceptance.json`, `results.json`, screenshot and UIA tree.
Additional `CP21-G3-*.json`, `CP21-G2-*.json` and `CP21-G4-*.json` reports contain
role/RuntimeId bindings, expected and observed values, flags, selected scope and
package hash, corrective/original diagnostics and Inspector before/after text.
An error TextBlock absent from the successful UIA tree is recorded as absent;
if still exposed, its visible error and obsolete HelpText must be clear.
The original source package hash is also checked by the existing runner cleanup.
Failures during admission/defaulting/query outside a subcase add an explicit
`CP21-gap-stage-failure` result; an input-only PASS never counts as completed G3.

## Available validation

- 185 application tests PASS in clean focused runs: bundle workspace/header/memo
  plus new control-peer contract 62; Search ViewModel 22; integration 18; infinite
  scroll 7; result window 7; bundle SQL 49; paging/performance 20.
- Runner contract regressions PASS, including ambiguous/missing/read-only/nonempty
  watermark binding, swapped/default/changed values, lost/replaced/duplicate
  RuntimeIds, Unicode readback and enumeration-order independence.
- PowerShell scenario regressions PASS; Debug solution and Release Windows
  cross-build PASS with zero warnings/errors; workflow actionlint and documentation
  checks PASS. No full Linux suite rerun/green claim: the prior baseline failures/
  native host crash and their disposition remain open in the final audit.

Dispatch the existing [Windows UI workflow](https://github.com/PapayaModding/UABS/actions/workflows/windows-ui-poc.yml)
on `v2-recovery`, or:

```sh
gh workflow run windows-ui-poc.yml --repo PapayaModding/UABS --ref v2-recovery
```

For a prepared local scenario (same prerequisites as the existing runner):

```powershell
$config = Get-Content tools/windows-ui-poc/scenario.local.json -Raw | ConvertFrom-Json -AsHashtable
$config['acceptance'] = 'checkpoint21-gaps'
$config | ConvertTo-Json -Depth 100 | Set-Content tools/windows-ui-poc/scenario.gaps.local.json -Encoding utf8NoBOM
pwsh -File tools/windows-ui-poc/Run-WindowsUiPoc.ps1 -Scenario tools/windows-ui-poc/scenario.gaps.local.json -TimeoutSeconds 600
```

The local example writes a separate ignored local configuration; never overwrite
the original. The launcher independently resolves/verifies HEAD and cleans its
temporary expectedCommit scenario. Inspect the tested SHA, all five case results,
G3 before/after values and untouched defaults, each G2 transition, and G4 exact
identity/error/recovery evidence. A green compile or a prior Windows run at an
older SHA is not proof of this slice. STOP at BLOCKED evidence; do not add
production AutomationIds or relax identity assertions without separate approval.

Remaining open: **G1 Package sorting, G5 indeterminate coverage, G6 tooltip-hover/
HelpText equivalence, Linux full-suite regression disposition**, and actual Windows
validation of G2/G3/G4. Checkpoint 2.1 remains open; no Checkpoint 3 work.
