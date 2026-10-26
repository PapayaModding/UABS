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

## G2 retained stale-result correction

The user confirms [Windows run 40330005576](https://github.com/PapayaModding/UABS/actions/runs/40330005576)
at `e12238943933ae817f1953655d6c0b1757f90b86`: Smoke/V1/V2 PASS, four gap
cases PASS, G2 BLOCKED. All five non-default values matched; the actual count
was `1 displayed · stale count`. G2 incorrectly called the unchanged **fresh**
reader, which requires an exact indexed-match summary. Artifact API access is
Forbidden; this is supplied Windows evidence, not a new artifact inspection.

Source semantics: ordinary criteria edits set `CriteriaChanged` without necessarily
invalidating the admitted snapshot. Scope/session invalidation calls
`MarkV2Stale`, preserving rows/selection and displaying `N displayed · stale count`
and `Stale — refresh required`. Header Status can subsequently be replaced by a
scope-ready message; the bound stale indicator and count are the state evidence.
LastExecutedCriteria changes only when actual query results are published.

G2 now first captures its complete **fresh** independent Smoke baseline and exact
last-executed criteria, then maximizes only its owned test window through the
existing UIA Window pattern. Every retention checkpoint (changed values, scope
off/on, workspace return, catalog open, catalog Refresh) records and verifies:

- The complete retained Name/Package multiset and realized row count, against
  that independently checked baseline; no flattened text or row-order guess.
- Either a genuine still-current old-query count at the initial edit checkpoint,
  or the exact stale count plus one exact stale indicator. After scope invalidation,
  stale state is mandatory. Stale totals are **retained displayed counts**, not
  a claim about fresh matches for the edited criteria.
- One unchanged last-executed summary, the exact changed-criteria indicator,
  no active Search Cancel control and no reported Search error (including clipped
  error text). Together with
  retained rows these are user-visible no-implicit-query checks, not invented
  internal execution telemetry. The headless regression additionally counts
  admitted Search sessions across those actual ViewModel transitions.
- All existing exact field/flag/selector, selected scope and copy-path/SHA checks.

Only the final explicitly requested supported Search uses fresh expectations
again. G3, G4, prior CP21 cases and `ReadAccessibleRows`/`CompleteRows` remain
unchanged. No production change, synthetic fresh count, catch-and-pass or blanket
“allow stale” option was added to the fresh reader.

New `CP21-G2-fresh-baseline.json` and per-transition
`CP21-G2-*-retained-results.json` contain the baseline and observed headers/cells,
count summaries, state markers, executed criteria and error/activity evidence
before assertions. Wrong readable retained counts/identities or query activity
are FAIL. Missing/duplicate/conflicting markers or incomplete/virtualized UIA
cells are BLOCKED. Fresh presentation after required invalidation is FAIL.
An updated Windows run is still required for G2; four prior passing gap cases
must not be confused with a pass of the uncompleted retention sequence.

## Deterministic control identity, not textbox order

The user reports [Windows run 40326632909](https://github.com/PapayaModding/UABS/actions/runs/40326632909)
at `aaadcd0cb26af075a9087de108f475b1e0768bf7` passed Smoke/V1/V2 but blocked
G3 before input: six empty writable Edits, every Watermarks array empty.
The artifact API returned Forbidden; this is supplied Windows evidence, not an
independently inspected screenshot/tree. Headless peer traversal did not prove
native UIA discoverability. Inspection of pinned Avalonia 11.3.3
[TextBoxAutomationPeer](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Controls/Automation/Peers/TextBoxAutomationPeer.cs),
[ControlAutomationPeer](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Controls/Automation/Peers/ControlAutomationPeer.cs)
and [TextBlockAutomationPeer](https://github.com/AvaloniaUI/Avalonia/blob/11.3.3/src/Avalonia.Controls/Automation/Peers/TextBlockAutomationPeer.cs)
establishes the distinction: template-internal TextBlocks return false for
IsControlElement; TextBox inherits Name from explicit Name/LabeledBy, not
Watermark. A visual peer can exist without the expected public UIA descendant
identity. The exact native filtering path in that artifact remains uninspected;
no claim that a different raw-tree search would succeed is needed for this fix.

The user separately approved production accessibility metadata only. The five
TextBoxes now expose these exact stable ID/Name pairs:

| Role | AutomationId (x:Name, exposed by the standard Avalonia peer) | Accessible Name |
| --- | --- | --- |
| include | IncludeSearchTextBox (preserved) | Include terms |
| exclude | ExcludeSearchTextBox | Exclude terms |
| pathId | PathIdSearchTextBox | Exact Path ID |
| classId | ClassIdSearchTextBox | V2 Class ID |
| typeName | TypeNameSearchTextBox | V2 exact type name |

Initially empty writable Edits bind by **both exact properties**. Matching either
property detects duplicates/conflicts; exactly one candidate must match both.
Each role retains its supported RuntimeId. Before later reads/writes, all five
roles are revalidated by RuntimeId plus ID/Name within SearchControlsScroller.
Unrelated unnamed Edits are not assigned roles. No descendant position, watermark
internals or expected input values identify a field. Missing/duplicate/conflicting/
replaced identities or unavailable writable Value remain BLOCKED; incorrect
retained values remain FAIL. Nothing is cleared or remapped to manufacture a pass.

Headless tests verify public ID/Name pairs, unchanged watermarks/bindings/defaults,
writable Value and peer/value/label retention across tab switches. A separate
regression proves a template watermark is not a control element and does not
name/label its parent Edit. **These do not prove hosted Windows RuntimeId stability
or native dialog behavior**; the updated gap job still needs Windows execution.
Search behavior, visible watermarks and result expectations are unchanged.
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

- 187 application tests PASS in clean focused runs: bundle workspace/header/memo
  plus control-peer/stale-retention regressions 64; Search ViewModel 22; integration 18; infinite
  scroll 7; result window 7; bundle SQL 49; paging/performance 20.
- Runner contract regressions PASS, including missing/duplicate/conflicting ID/Name,
  unavailable/read-only/nonempty binding, swapped/default/changed values,
  lost/replaced/duplicate RuntimeIds, Unicode readback and enumeration independence.
  Retained-result regressions also cover fresh/stale phase separation, wrong
  counts/identity/package/duplicates, missing or ambiguous summaries/markers,
  incomplete cells, changed executed criteria and unexpected Search activity.
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
older SHA is not proof of this slice. STOP at BLOCKED evidence; further production
accessibility changes or relaxed identity assertions need separate approval.

Remaining open: **G1 Package sorting, G5 indeterminate coverage, G6 tooltip-hover/
HelpText equivalence, Linux full-suite regression disposition**, and actual Windows
validation of G2/G3/G4. Checkpoint 2.1 remains open; no Checkpoint 3 work.
