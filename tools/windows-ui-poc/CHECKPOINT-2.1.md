# Checkpoint 2.1 UI acceptance coverage

Source of truth: [manual checklist](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-search-defaults.md#limits-and-manual-acceptance),
[handoff](../../Avalonia/docs/05_audit/codex-session-handoff.md), and
[roadmap](../../Avalonia/docs/05_audit/v2-ui-workflow-recovery-roadmap.md).
No recovery issue or checkpoint status is changed by this tool document.

The user confirmed [run 40246911317](https://github.com/PapayaModding/UABS/actions/runs/40246911317)
at `6991888bd7f8fe7704290f4b3c777b3ec45feedb` passed the original Smoke
scenario. This establishes hosted desktop feasibility and that scenario's
observed defaults/query/results only. The current session could not independently
download that run through the GitHub API (Forbidden). All expanded checks below
require a new actual Windows run; source tests/builds do not establish UI acceptance.

| Documented requirement | Scenario/case | Windows result before expanded run | Evidence / remaining validation |
| --- | --- | --- | --- |
| Untouched V2 Sources-only / Assets defaults | unchanged `smoke`; CP21-01 | PASS, user-confirmed baseline | Linked baseline run; fresh SHA gets its own artifact |
| Known Unicode keyword returns exact bundle Name/Package identities | unchanged `smoke`; CP21-01 | PASS, user-confirmed baseline | `preparation.json`, `final.png`, observed rows in `results.json` |
| Explicit Names choice, corrective error, controls and confirmed rows retained | CP21-02-Names (also Paths/All feedback) | Not executed | Per-case screenshot/tree, field-selection evidence, error text and exact rows |
| Mixed result kinds produce corrective feedback and retain choices/rows | CP21-03 | Not executed | Toggle readback, corrective text, exact rows |
| Original backend diagnostic available through tooltip | CP21-04 | Not executed; UIA support unknown | Requires original `InvalidRequest` through UIA HelpText; otherwise BLOCKED and actual hover validation remains outstanding |
| Explicit selectors/text/other criteria survive scope and workspace transitions | CP21-05 | Not executed | Toggle/ComboBox readback and all readable Edit/criterion-flag snapshots before/after |
| Explicit choices survive catalog reload | CP21-06 | Not executed; folder-picker accessibility unknown | Exact disposable catalog path, native folder Edit 1152 Value readback, real Refresh Invoke, retained criteria |
| Explicit Refresh unchanged; editing criteria alone does not rerun | CP21-07 | Not executed | Prior row retained, Refresh publishes exact empty result, second Refresh restores Unicode result |
| Search sorting unchanged | CP21-08 | Not executed; header Invoke support unknown | Independently indexed three-bundle `asset` oracle, complete reversed Name order; unsupported action is BLOCKED. Nontrivial Package sorting remains uncovered (one package) |
| Selection retained including after rejection | CP21-09 | Not executed; row SelectionItem support unknown | UIA selected state before/after unsupported Names query; unsupported action is BLOCKED |
| Mixed V1/V2 scope rejects rather than guesses | CP21-10 | Not executed | Exact two disposable scopes, error text, selected states retained |
| Fresh V1-only original mixed/Names defaults and Search behavior | separate `checkpoint21-v1`, CP21-11 | Not executed; folder-picker accessibility unknown | SHA-checked copy of LegacyPackage fixture, six-column exact portrait/LegacyPackage/character.ab/1001/memo row and exact count |

The metadata/mixed-exclusion and indeterminate-coverage guidance described in
the report has no new real-desktop fixture here. Numeric/type filters are
captured for retention but non-default numeric/type values and text entered
before initial package admission are not newly exercised. These remain gaps;
do not infer acceptance from headless tests. Sorting/selection post-reload
identity beyond the explicitly exercised checks also remains subject to the
actual per-case evidence.

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
Smoke SQLite copy. Native folder controls, ComboBox selection, header Invoke,
and row SelectionItem require supported UIA patterns. A missing pattern or
ambiguous control blocks; there is no coordinate/OCR substitute. Original
diagnostic tooltips are not assumed to be accessible because the binding exists.

Final acceptance requires inspection of every applicable actual case result
and resolution of remaining gaps. Checkpoint 2.1 and issue #1 remain open.
