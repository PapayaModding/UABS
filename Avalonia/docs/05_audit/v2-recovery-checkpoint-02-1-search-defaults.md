# Recovery Checkpoint 2.1 — Search Defaults and Compatibility UX

**Date:** 2026-10-25. **Baseline:** `f3a350a`, clean index/worktree. Scope: Search selector initialization and error presentation only; P1-D1–P1-D15 unchanged.

## Finding and correction

The user reports [Checkpoint 2](v2-recovery-checkpoint-02-search-ui.md) works with Sources only, Assets fields and a known asset-name keyword. Its initial Mixed/Names selectors still came from the V1 ViewModel defaults, however, and the verified bundle backend correctly rejected them. This is user-reported partial manual acceptance, not an agent visual observation or closure of recovery issue #1.

`SearchWorkspaceViewModel.UpdateScopeState` now initializes **untouched** selectors from the actual selected package scope:

| Selected scope | Kind/field defaults while untouched |
| --- | --- |
| V2 only, production bundle projection | Sources only / Assets fields |
| V1 only | Original Packages + Sources + Assets / Names |
| Empty or mixed V1/V2 | No guessed format; leave selectors as they are |

The four kind/field setters record the first actual explicit value change. Thereafter no scope, catalog or workspace transition resets those choices, even when unsupported. Restoring a selector to its original value does not erase that explicit intent. Before customization, moving between unambiguous formats updates only those selectors; it never clears include/exclude text, exact matching, Search memos, Memo only, numeric/type filters or package selection. Text typed before package admission is retained. Existing programmatic non-bundle Search callers retain their old initialization path.

No supported default field group includes bundle-name matching without also including unsupported package fields. Therefore the approved Assets default is used; no new group, field remapping or source-name semantics were invented. [Checkpoint 1.1 compatibility boundaries](v2-recovery-checkpoint-01-1-search-compatibility.md) remain in force.

## Actionable errors

Backend `InvalidRequest` rejection remains unchanged. Bundle Search now presents concise corrective guidance:

- Wrong result kinds: select Sources only; disable Packages/Assets kinds.
- Names/Paths/All: select Assets, CAB codes or Dependencies fields.
- Metadata/mixed exclusion scope: use Assets fields (optionally Search memos) or clear exclusions.
- Indeterminate exclusion coverage: select fully indexed packages or clear exclusions.

The original backend message remains in `ErrorDetails`, bound to a tooltip on the existing error text. New errors/success clear obsolete diagnostics. Controls and confirmed rows/selection remain unchanged after rejection; there is no automatic retry, remapping, modal or broadened query. Other error handling and V1 feedback retain their existing messages.

## Files and verification

- `UABS/Script/ViewModels/SearchWorkspaceViewModel.cs`: untouched-selector initialization, explicit-intent tracking and compatibility feedback/diagnostics.
- `UABS/Script/View/Pages/SearchWorkspaceView.axaml`: existing error text gets a name and diagnostic tooltip; no layout/grid changes.
- `UABS.UnityAssets.Tests/V2BundleSearchWorkspaceTests.cs`: fresh production defaults/query, explicit choices before/after admission, scope/workspace/mode transitions, V1 defaults, mixed-mode rejection and headless tooltip binding. Existing compatibility cases assert both actionable feedback and original rejection details.
- This report, [roadmap](v2-ui-workflow-recovery-roadmap.md) and [handoff](codex-session-handoff.md).

Final automated verification:

- Focused bundle-workspace: **18/18 passed (3.58s)**, including four new cases and all prior Name/Package sorting, paging, exact selection and D5 tests.
- Existing focused Search suites passed: ViewModel22, integration18, header sorting15, infinite scrolling7, bundle SQL49, raw/Unicode Search54. No old cases removed.
- Full **in-process xUnit: 792 total, 791 passed, 0 failed, 1 existing hard-link permission skip (66.93s)**. Not VSTest transport verification. Tests use disposable packages; no corpus benchmark was needed.
- Final `dotnet build Avalonia.sln -c Debug --no-restore -m:1 -nr:false --verbosity quiet`: **zero warnings/errors (1.21s)**, includes actual startup application. Verification-runner build also passed with zero warnings/errors (2.46s). Builds/tests ran sequentially. Initial missing `System.IO` import/test lifecycle-method naming and a V1 fixture setup that inadvertently admitted V2 were corrected before passing verification.
- Canonical/schema-design/Debug embedded-DDL equality, zero-Unity-runtime project/output audit, seven baseline fixture hashes, protected backend/Unity/Assets/Packages/persistence paths, whitespace/conflicts and changed-file scope passed. Three updated documents/52 references and balanced fences checked; no new Mermaid blocks. All17 issue rows remain Reported and index empty. Removed only session-created `uabs_log.txt` from intentional regression failure paths.

Worktree is verified buildable and ready for review/commit as this scoped correction; user-operated Debug acceptance remains pending. No staged changes or commit created.

## Limits and manual acceptance

**Latest readiness review (2026-10-26):** the user verifies six-job Windows
[run40371215461](https://github.com/PapayaModding/UABS/actions/runs/40371215461)
at `709c615abc9cd362ac03578ddc6c4b62f5716339`, supporting G1–G5. The
[closure-readiness audit](v2-recovery-checkpoint-02-1-closure-readiness-audit.md)
records inaccessible case artifacts, pending G6 literal-hover/equivalence choice
and failed/aborted Linux regression disposition. Recommendation NOT READY for
formal closure; checklist unchanged, no automatic acceptance or Checkpoint3.

**2026-10-26 evidence update:** the user confirms all three jobs in
[Windows run 40259657455](https://github.com/PapayaModding/UABS/actions/runs/40259657455)
passed at `26f38e68511cd6eaf0d0c23b7ce9fac9228aab70`. Implemented desktop
cases are supported, not the entire scope of the promises above. The
[final acceptance gap audit](v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md)
records satisfied requirements, remaining mandatory evidence, deferred/optional
work and regression limitations. The original checklist below is unchanged;
neither this update nor green jobs formally accept/close Checkpoint2.1.

Package-field/result-kind membership, source/mixed exclusion scope and incomplete-index policy are still unsupported, not silently reinterpreted. Empty keywords still require the existing keyword/Path ID validation. No SQL, identity, aggregation, cursors, sorting, scrolling, source Inspector, schema/version, memo persistence, V1 behavior or Assets/Packages UI changes. No navigation or Checkpoint 3 implementation. No real OWL/source/package mutation or desktop automation.

User-operated current Debug checklist:

1. Start `E:/Projects/UABS/Avalonia/UABS/bin/Debug/net9.0/UABS.exe`; open a disposable V2 package and enter Search without changing selectors. Confirm Sources only / Assets and a known asset keyword succeeds.
2. Explicitly choose Names or mixed result kinds. Confirm selected controls remain and the error says what to change; hover over it for the original diagnostic.
3. Change package scope, switch workspaces and return. Confirm intentional selectors/text persist; then restore supported selectors and verify sorting, selection and explicit Refresh.
4. In a fresh V1-only workflow confirm original mixed/Names defaults and V1 Search remain unchanged. Mixed-format scopes should still error rather than guess.

Issue #1 stays open pending user acceptance of Checkpoint 2.1; automated verification is complete. No new Unity-exclusive feature decision was discovered. Suggested commit: `fix: align V2 bundle search defaults with supported filters`; do not commit automatically.
