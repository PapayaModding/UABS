# Checkpoint 2.1 — final acceptance gap audit

**Date:** 2026-10-26. **Audited commit:** `26f38e68511cd6eaf0d0c23b7ce9fac9228aab70`,
`v2-recovery`. Documentation/planning only. Checkpoint 2.1 is **not closed**.

**Latest closure-readiness audit (2026-10-26):** [current review](v2-recovery-checkpoint-02-1-closure-readiness-audit.md)
audits `709c615abc9cd362ac03578ddc6c4b62f5716339` and user-verified six-job
Windows run40371215461 supporting G1–G5. Earlier pending G1–G5 implementation
statements below are historical. Artifact APIs remain Forbidden; case records/
full CP21-04 HelpText need review. G6 hover/equivalence remains a user decision;
current Linux full suite failed38/aborted with PAL_SEHException despite122 focused
and20 isolated paging passes. Recommendation **NOT READY for formal closure**,
pending evidence/decision and regression-risk disposition. No closure, new
scenario, production implementation or Checkpoint3 authorization.

## Authority and evidence boundary

Sources: [Checkpoint 2.1 correction and manual checklist](v2-recovery-checkpoint-02-1-search-defaults.md),
[handoff](codex-session-handoff.md), [roadmap](v2-ui-workflow-recovery-roadmap.md),
[UI coverage ledger](../../../tools/windows-ui-poc/CHECKPOINT-2.1.md), and the
runner/fixture preparation at the audited commit. The checklist's four manual
steps are the primary acceptance gate; the correction's explicit retention and
four error-feedback promises define what those checks must not overlook.
Roadmap-wide recovery work is not imported into this checkpoint.

The user confirms [run 40259657455](https://github.com/PapayaModding/UABS/actions/runs/40259657455)
at the audited commit passed **Smoke, checkpoint21-v2 and checkpoint21-v1**.
Accept that as real Windows evidence for the implemented CP21-01–CP21-11 checks,
including the new Name-sort action. This session's read-only GitHub run API
request returned Forbidden; no artifact download, screenshot inspection or
new Windows execution is claimed. Case-level evidence paths below describe
what the runner records, not newly observed artifact contents. Passing cases
need not be implemented again. A case identifier is not a complete coverage
claim for every behavior in its topic.

## Final acceptance matrix

“Mandatory” below means evidence is needed for an existing documented behavior,
not authorization for a new feature or necessarily a new automated test. Existing
recorded manual evidence can close a gap if it proves the exact behavior.

| Requirement / authority | Existing evidence | Closure disposition |
| --- | --- | --- |
| Untouched V2 Sources-only / Assets defaults; known indexed Unicode keyword; exact bundle Name/Package results (manual 1) | Smoke + CP21-01, SHA-checked package and independently verified expected rows | **Satisfied**, user-confirmed Windows PASS; do not duplicate |
| Explicit Names/Paths/All and mixed-kind rejection, corrective text and retained choices/results (manual 2) | CP21-02 variants, CP21-03 | **Satisfied** for these combinations; not proof of the other feedback branches |
| Original diagnostic is accessible (manual 2) | CP21-04 reads UIA HelpText containing `InvalidRequest`, records the value | **Satisfied** for accessible information; literal tooltip hover/content review remains G6 |
| Explicit selectors, include text and exercised flags survive scope/workspace/catalog transitions (manual 3) | CP21-05/06 read controls and criterion snapshots | **Satisfied** for exercised values; non-default criteria remain G2 |
| Criteria edits do not run Search; explicit Refresh publishes/restores exact results (manual 3) | CP21-07 | **Satisfied**; no new Refresh feature/test needed |
| Name ascending/descending sort, UIA Invoke and focused Enter, exact visible order and complete row set (manual 3) | CP21-08; `CP21-08-order-{Ascending,Descending}.json`, geometry and keyboard ownership evidence | **Satisfied**, user-confirmed Windows PASS; do not revisit the former blocker |
| Selection/Inspector identity and retention after unsupported Names rejection (manual 3) | CP21-09; exact package/source identity/path/UUID checks | **Satisfied**; no repeat selection workaround needed |
| Mixed-format rejection and fresh V1 mixed/Names defaults, original result presentation (manual 4) | CP21-10 and separate CP21-11 job | **Satisfied**, user-confirmed Windows PASS |
| Nontrivial Package sort (manual 3; Checkpoint 2 Name/Package global headers) | CI Smoke has only package `Smoke`; backend/headless multi-package coverage exists | **Mandatory G1**: real multi-package UI order remains unexercised |
| All named criteria retained, including non-default numeric/type and exclude text (correction's initialization guarantee; manual 3) | CP21-05/06 snapshot empty numeric/type/exclude fields; flags are not non-default in every transition | **Mandatory G2**: a nonempty retention matrix, not a new filter algorithm |
| Text before initial admission survives untouched format defaulting (correction's explicit guarantee) | Headless coverage; current runner admits the package before Search entry | **Mandatory G3**: fresh pre-admission UI evidence |
| Metadata/mixed-exclusion corrective feedback (Actionable errors) | Headless `UnsupportedControls_ReturnBackendError_WithoutChangingControlsOrPreviousRows`, backend exclusion tests; no such desktop case | **Mandatory G4**: existing rejection UI branch, not support for new exclusion semantics |
| Indeterminate exclusion coverage corrective feedback (Actionable errors) | Backend partial-eligible/unavailable-name tests; CI package is fully indexed | **Mandatory G5**: controlled incomplete evidence; partial package label alone is insufficient |
| Tooltip hover exposes original diagnostic (literal manual 2) | CP21-04 does not hover or inspect a popup, and checks a prefix rather than equality with the complete original diagnostic | **Mandatory G6 evidence or explicit equivalence decision**, not an invented CP21-04 failure |

No document explicitly defers G1–G5 to a later checkpoint. In particular,
deferring *support* for unsafe exclusion/coverage mappings does not defer testing
their already shipped corrective errors. Conversely, the checklist does not
require a new desktop Cartesian product of every field, package and transition.

## Smallest evidence needed for the mandatory gaps

### G1 — Package order, without another speculative runner workaround

Use two independently built/admitted disposable V2 packages with distinct ASCII
package labels whose intended order differs from the source-name order. Preserve
each full package/source identity and an independently specified Name/Package
row oracle, cross-checked against raw indexed metadata before hashing. Do not
copy one database and pretend its package UUID became independent. Existing
`V2BundleSearchTests.GlobalOrdering_Scope_DuplicateNamesAndCursorIdentity` and
`V2BundleSearchWorkspaceTests.ActualHeaders_ToggleGlobalBundleOrderAndPreserveSelection`
already cover backend/global and header behavior; reuse those results, not a new
sorting algorithm or a headless claim of desktop acceptance.

On Windows, activate the existing Package header in both directions and verify
the exact full visible row sequence, count, selection and associated package
identities. Current headers have no supported coordinate-free Invoke/keyboard
sort action; `SearchNameSortButton` targets Name only. The smallest immediate
route is **recorded human header interaction**, not coordinate-driven automation.
If fully remote automation is required, obtain a separate narrow Package-sort
accessibility approval first. Do not silently repurpose the Name button.

### G2 — One retention scenario with meaningful non-default values

Extend retention coverage only: populate include/exclude text, signed Path ID,
Class ID, exact type text, and the existing exact/memo flags using real controls.
Use distinct nonempty sentinel values; for pure retention they need not claim
matching indexed rows, and **do not execute that sentinel query**. Exercise
scope deselect/reselect, workspace return and disposable catalog reload, then
compare every field/flag/selector and selected package identity by semantic
control identity, not Edit enumeration position. Reuse existing UIA diagnostics
to identify controls; a missing or ambiguous pattern is BLOCKED, not a guessed
textbox or production AutomationId change without approval. Reset to the known
supported query before subsequent checks. Published fixtures stay untouched.

This covers the remaining non-default exclude text and catalog flag states as
well as the named numeric/type gap. It does not invent a successful combined
filter query or require all cross-products of existing successful cases.

### G3 — Fresh process, enter text before opening a package

Use an isolated disposable catalog/state and prove no package is already loaded.
Navigate to Search, type the configured known Unicode keyword before admission,
then open the SHA-verified package using the existing picker path. Return to
Search and verify the exact text, untouched Sources/Assets defaults and known
results. Also retain a nonempty exclusion/criterion sentinel through defaulting
without executing it, then reset safely. Reuse the headless pristine V1-to-V2
format-transition test as complementary evidence; a new exhaustive mode-switch
suite is not required. This is an additive fresh scenario, not a rewrite of the
passing Smoke or V1 job.

### G4 — Existing complete Smoke package, no new dataset

Establish a supported confirmed query/selection. Select CAB codes (an exposed
supported positive metadata field) with a nonempty exclusion, as in the existing
headless rejection case. Verify “exclusions require Assets” corrective guidance,
the original source/mixed-scope diagnostic, unchanged entered values, confirmed
row set and selected identity, and subsequent error clearing on a known supported
query. This exercises the formatter's metadata/mixed automatic-scope branch;
do not use Names/All, whose earlier unsupported-package-field rejection masks it.
There is no separate mixed metadata field selector to invent for the UI test.

### G5 — Controlled incomplete eligible bundle, not merely a partial catalog

The existing backend test
`PartialEligibleBundle_BlocksWholeScope_NoSilentSkippingOrFalseExactCount`
constructs one complete and one **eligible partial bundle**, then an Assets-field
query with a nonempty exclusion. It proves an indeterminate rejection and that
complete-only scope succeeds. `CompleteEligibleBundles_CanExcludeDespiteUnrelatedPartialPackage`
proves an unrelated partial source is not enough to trigger the error.

Prepare an additional explicitly labelled test-only package through the normal
PackageBuilder metadata/extraction contract with acknowledged partial evidence,
following that backend fixture recipe; no post-admission SQL flag patch and no
Search stub. Validate its admission, eligible source kind, incomplete coverage
and expected diagnostics independently, record provenance and SHA **before**
the runner creates its disposable copy. These would be synthetic metadata test
fixtures, not claimed Unity-extracted published Smoke or new AssetBundles.
First confirm UI admission of this package is supported; otherwise record BLOCKED
and propose a reviewed fixture alternative, not a weakened guard.

Run the real UI rejection with Assets fields, Sources only and exclusion text;
verify “fully indexed packages or clear exclusions”, complete original
indeterminate diagnostic, no false successful count/partial results, and retained
controls/previous confirmed rows/selection. Clearing exclusions or selecting only
the independently verified complete package must clear the obsolete error on a
supported query with independently established expected rows. Fixture values
and hashes must be derived/validated during future preparation, not fabricated
in this planning audit.

### G6 — Original tooltip evidence, or an explicit acceptance decision

First inspect the existing CP21-04 recorded HelpText and confirm the full backend
diagnostic, not merely `InvalidRequest`, is retained. This can reuse evidence;
it need not become another rejection case. A single recorded **human hover** and
tooltip screenshot would satisfy the literal manual instruction. Alternatively,
the user may explicitly accept the existing real UIA HelpText plus headless exact
tooltip-binding evidence as equivalent. That decision is not made by this audit.
No OCR/coordinate automation or new tooltip behavior is authorized.

## Deferred versus optional work

| Classification | Work | Authority / boundary |
| --- | --- | --- |
| Explicitly later checkpoint/slice | Go to Assets, original-source navigation/history, native source provenance and related drafts | Handoff forbids Checkpoint 3/navigation; Phase1B proposed source-browser/navigation slices 4/5 and roadmap #2 remain separate; historical numbering is not new authorization |
| Explicitly later milestone/phase | Genuine bundle memos/Memo sorting; broad grid/scroll work; V1 parity/retirement; unrelated UI polish | Roadmap P1-D3/P1-D5, Phases 2–5; none is a CP21 closure prerequisite |
| Optional additional coverage | Stress-size/virtualized desktop sorting, exhaustive type/filter combinations, every non-default package-name filter, all post-reload sort/selection combinations, duplicate-label UI stress | Not separately required by the four CP21 manual steps; existing identity/paging/backend tests remain relevant. Do not claim these were exercised or remove them from broader recovery planning |

G2 checks retained fields; successful backend numeric/type matching is already
covered by existing tests and is not a demand for new matching semantics.
Broader error-policy implementation remains deferred; G4/G5 test explicit errors,
not removal of compatibility restrictions.

## Linux suite and closure disposition

The [implementation record](../../../tools/windows-ui-poc/NAME-SORT-ACCESSIBILITY-PROPOSAL.md#implementation-verification-linux)
records 184 clean focused passes, successful builds/runner checks, and a **failed/
aborted full Linux VSTest run** (724 passes, 38 failures, one skip before native
`PAL_SEHException`). The untouched `59c7e07` baseline also failed/aborted (726
passes, 36 failures, one skip); Windows NTFS/kernel32-only checks and legacy
Windows-path/file-admission expectations account for documented baseline
limitations. Isolated raw Search runs also crashed on both revisions.

Two first-run global Activity query-counter failures are not proven to be the
same as the 36 baseline failures; isolated paging/performance passed 20/20.
Do not label every failure “known baseline” or assert a fully diagnosed crash.
No new CP21 production regression is demonstrated by this comparison, but
neither a clean full regression suite nor its irrelevance has been proved.
The passing hosted workflow builds application tests but runs **runner** checks
and UI scenarios; it does not execute the full application xUnit suite.

The checklist does not prescribe a green full **Linux VSTest** run as its manual
acceptance condition. Thus baseline platform limitations do not automatically
invalidate passing Windows UI evidence, and repairing unrelated Windows-only
tests is not silently added to CP21. Before closure, however, record an explicit
regression-risk disposition: preferably run the full application suite on its
supported Windows environment (separate verification task), or recover the
documented in-process method and record remaining limitations/owner acceptance.
Any demonstrated new regression must be resolved or explicitly dispositioned;
neither this audit nor three green UI jobs waive it. No new tests were run for
this documentation-only audit.

## Recommended next slice and closure sequence

1. **Next implementation slice: test-only retention/defaulting plus existing
   metadata-exclusion feedback (G2, G3, G4).** Reuse complete Smoke and fixed
   independent oracle. Add isolated optional scenarios/evidence, no production
   changes, no new fixture collection, and preserve passing Smoke/V1/CP21 cases.
   Test, publish and run Windows; do not mark acceptance from compilation.
2. Add the acknowledged-partial fixture preparation and real G5 rejection/recovery
   scenario in a separately reviewed test-only slice. Never falsify the existing
   Smoke manifest or loosen its complete-index admission checks.
3. Obtain G1 manual multi-package header evidence and G6 tooltip evidence (or the
   explicit equivalence decision). If zero-human operation is required, review
   the separate accessibility scope before implementation. Package ordering is
   not deferred simply because its current header is inaccessible to FlaUI.
4. Consolidate per-gap PASS/FAIL/BLOCKED evidence with exact SHAs, fixture hashes,
   actions and independent expectations; complete the regression disposition
   above and obtain the user's formal acceptance. Passing implemented scenarios
   remain valid and should not be duplicated as new work.

Until those obligations are supported, recommend **not yet ready for formal
closure**. This audit changes no checkpoint/issue status, no acceptance criteria,
no runner/production code, and authorizes neither a new accessibility control
nor Checkpoint 3. Final acceptance remains the user's decision.

## Interrupted-session recovery

Recovered the existing untracked audit and four modified documentation files:
the handoff, Checkpoint 2.1 report, recovery roadmap and UI coverage ledger.
At recovery, both local HEAD and remote `v2-recovery` were still
`26f38e68511cd6eaf0d0c23b7ce9fac9228aab70`, with an empty index: these audit
edits had not been committed or pushed. Their existing analysis and evidence
boundaries were retained, not regenerated or replaced.

Review against the unchanged four-step manual checklist and explicit correction
guarantees confirmed that the matrix, G1–G6 evidence plans, deferred/optional
classification, Linux regression disposition and recommended G2/G3/G4 next
slice are present. Documentation verification checks local file references,
the audit's linked local anchors, balanced fences, whitespace/conflict markers,
unchanged checklist text and all 17 roadmap issue rows. No application tests or
Windows scenarios are rerun for this documentation-only recovery. Existing
untracked `obj/` directories are preserved and excluded from publication.
