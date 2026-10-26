# Checkpoint 2.1 — closure-readiness audit

**2026-10-26 (America/New_York). Audited code:**
`709c615abc9cd362ac03578ddc6c4b62f5716339`, `v2-recovery`.
**Recommendation: NOT READY for formal closure. Checkpoint 2.1 remains open.**
This audit changes documentation only; it authorizes no implementation or Checkpoint 3.

## Authority and evidence boundary

The [original checklist](v2-recovery-checkpoint-02-1-search-defaults.md#limits-and-manual-acceptance),
[gap audit](v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md),
[roadmap](v2-ui-workflow-recovery-roadmap.md), [handoff](codex-session-handoff.md)
and [coverage ledger](../../../tools/windows-ui-poc/CHECKPOINT-2.1.md) remain authoritative.
This supersedes the gap audit's historical G1–G5 pending-implementation statements,
not its G6 decision requirement, regression obligations or acceptance criteria.

The user verifies [Windows run 40371215461](https://github.com/PapayaModding/UABS/actions/runs/40371215461)
at the SHA above, on recovery, with **all six jobs successful** and G1–G5 behavioral
acceptance supported. Accept that as user-supplied real Windows evidence; do not
duplicate the implemented scenarios or resurrect resolved blockers.

This session's requests to the run, jobs and artifacts APIs all returned Forbidden.
No artifact was downloaded, screenshot visually reviewed or Windows test executed
here. The table distinguishes user-confirmed job outcomes from unavailable
case-level artifact re-review. Workflow success alone does not establish the
contents of an inaccessible screenshot, hash record or full diagnostic string.
Local checkout branch/HEAD were verified; the workflow checks branch and exact
GITHUB_SHA, but its actual environment.json is unavailable in this session.

| Job | Conclusion / evidence | Independent artifact inspection |
| --- | --- | --- |
| smoke | PASS, user-verified run 40371215461 / audited SHA | BLOCKED: API Forbidden |
| checkpoint21-v1 | PASS, same source | BLOCKED: API Forbidden |
| checkpoint21-v2 | PASS, same source | BLOCKED: API Forbidden |
| checkpoint21-gaps | PASS, same source | BLOCKED: API Forbidden |
| checkpoint21-package-sort | PASS, same source | BLOCKED: API Forbidden |
| checkpoint21-indeterminate | PASS, same source | BLOCKED: API Forbidden |

## Requirement-to-evidence matrix

“PENDING re-review” below is **not** a new failed scenario: the user confirms the
Windows behavioral baseline. It identifies exactly what this audit cannot inspect
without the existing artifacts. No new desktop cases are proposed.

| Requirement | Source at audited SHA / expected existing evidence | Verdict and remaining action |
| --- | --- | --- |
| Untouched V2 Sources/Assets, Unicode `テスト`, exact bundle Name/Package rows | Smoke, CP21-01; preparation.json, results.json, UIA/screenshots | User-confirmed PASS; PENDING case-artifact re-review |
| Explicit selectors, unsupported fields/kinds, mixed-format rejection, V1 defaults/behavior | CP21-02/03/05/06/10/11; V1/V2 acceptance.json and UIA | User-confirmed PASS; PENDING re-review |
| Explicit Refresh/no implicit query, Name sorting, selection/Inspector retention | CP21-07/08/09; direction/layout/Inspector/criteria reports | User-confirmed PASS; PENDING re-review |
| G1 nontrivial Package sorting | CP21-G1-package-order; independent Alpha/Zeta preparation, action/completion/layout/Inspector reports | User-confirmed PASS; PENDING re-review of both orders, ascending Alpha ties and Invoke/Tab-Enter/Space |
| G2 non-default numeric/type/text/flag retention across required transitions | CP21-G2-nondefault-retention; semantic-role snapshots, stale retained-results evidence | User-confirmed PASS; PENDING re-review of exact values/confirmed rows, not just controls existing |
| G3 text before admission and defaulting | CP21-G3-pre-admission-input/independent-query and retained-sentinel evidence | User-confirmed PASS; PENDING re-review |
| G4 metadata-exclusion feedback and retained state/recovery | CP21-G4-metadata-exclusion; rejection-state, retained Inspector, recovery reports | User-confirmed PASS; PENDING full diagnostic/state re-review |
| G5 eligible incomplete exclusion feedback, no false publication/count, retention and recovery | CP21-G5-01 through 05; raw preparation, hash reports, full rejection and two recovery reports | User-confirmed PASS; PENDING provenance/full-diagnostic/retention re-review |
| Full original error binding, error clearing | Exact headless binding test in current 122/122 focused run; ErrorDetails setter and Search XAML | PASS for implementation/headless behavior; not hover evidence |
| Full original Names-rejection UIA HelpText (G6) | CP21-04 results.json “original diagnostic accessibility” step | BLOCKED for content inspection; assertion checks only `InvalidRequest`, not full equality |
| Literal hover shows original diagnostic, or explicit equivalence | Checklist manual step 2; decision below | PENDING: no hover screenshot supplied/available; user has not accepted equivalence |
| Full regression-risk disposition | Current Linux results below; historical baseline comparison | FAIL/aborted suite; PENDING supported-Windows verification or explicit risk disposition, not a waiver inferred from UI jobs |
| Final checkpoint acceptance | User decision | PENDING; no automatic closure |

### Fixture and assertion integrity (source-reviewed, not an artifact PASS)

- [Smoke preparer](../../../tools/windows-ui-poc/ci/Program.cs) verifies published
  profile/seed/LZ4, allowlist/hash/size and real extraction. Fixed Unicode expectations
  are cross-checked against manifest assignments and raw SQLite names, never Search output.
- [Package preparation](../../../tools/windows-ui-poc/ci/package-sort/Program.cs)
  builds independent Alpha (0001/0002) and Zeta (0000) databases, validates raw
  witnesses/identities/paths/coverage/hashes and preserves ascending ties in both
  independent Package directions. UI order comes from validated row geometry,
  not descendant order. Primary Package field and real action/focus checks remain.
- [G5 preparation](../../../tools/windows-ui-poc/ci/indeterminate/IndeterminateFixtureBuilder.cs)
  uses explicitly synthetic metadata contracts, not fake Unity extraction. Eligible
  complete/partial bundle evidence, acknowledgement, raw declared/captured counts,
  UUIDs, source/DB hashes and no post-admission SQL patch remain required. The hash
  correction changes hex representation comparison only, not expected digest values.
- Exact row sets/counts, independent paths/hashes, canonical Inspector identities,
  retained confirmed versus live criteria and strict PASS/FAIL/BLOCKED gates remain
  in runner source. G2's stale-state contract and G5's retained old confirmed count
  are explicit assertions, not manufactured fresh successful counts. No assertion
  weakening was identified in this review. Actual records still require inspection.
- These Windows jobs build application tests but execute **runner** regressions/UI
  scenarios, not the full application xUnit suite. Six green jobs do not clear Linux
  failures or establish a green Windows application regression suite.

## G6 — diagnostic implementation, automated evidence and user decision

Confirmed implementation: `ExecuteV2SearchAsync` preserves `e.Message` in ErrorDetails
while presenting corrective Error text. XAML binds `ToolTip.Tip` to ErrorDetails.
New errors and successful queries clear obsolete diagnostics. Current headless
`CompatibilityFeedback_RendersActionAndOriginalDiagnosticTooltip_ThenClearsOnSuccess`
passed: the visible error text equals Error and `ToolTip.GetTip(error)` equals
**all of ErrorDetails**, then becomes null on successful recovery. It does not
open/render a tooltip popup or prove hover geometry, clipping or readability.

CP21-04 records the actual UIA HelpText but only requires it to contain
`InvalidRequest`. Thus its PASS does not itself prove the **full** original Names
diagnostic. To check the existing value, compare it in results.json against
`InvalidRequest: ` followed by this unchanged BundleSearchContracts diagnostic:

```text
Bundle projection supports source/name/path/index/CAB/dependency and asset name/memo fields, with SourceFile results only. Package fields and other result kinds require an approved bundle membership mapping. Exclusions use asset-only selected fields or an explicit AssetName/Memo mask; source/mixed automatic scope remains unresolved.
```

G4 checks the original metadata/mixed-scope diagnostic branch. G5 independently
compares HelpText to its **entire expected indeterminate backend message** and
checks clearing after recovery. Those are stronger complementary information-
accessibility checks, not proof of the literal Names tooltip popup or full CP21-04
string. Actual latest HelpText values cannot be confirmed here without artifacts.
No human-hover screenshot is present in supplied/accessible evidence, and no runner
case hovers. Do not assert that no such human evidence exists elsewhere.

Two permissible options, **for the user to choose**:

**A. Literal verification:** in the verified Windows Debug build, open an approved
disposable package, select Names and execute the known query. Hover SearchErrorText;
capture the actual popup and readable complete original diagnostic. Record SHA,
actions and screenshot. Restore Assets and verify obsolete error/tooltip clears.
No new automation, coordinate fallback, production change or fixture mutation needed.

**B. Explicit equivalence acceptance:** after reviewing the existing complete UIA
HelpText value(s), the user may explicitly accept real HelpText information access
plus exact headless tooltip-binding/clearing as equivalent to the checklist hover
instruction. Record that decision and its evidence scope, including the remaining
unobserved popup rendering/readability risk. This audit does **not** choose B.

## Current Linux regression disposition

Environment: Debian13 linux-x64, kernel6.18.55, SDK10.0.401, net9.0 tests hosted by
Microsoft.NETCore.App9.0.5. Code SHA is the audited SHA. No assertions, platform
guards, parallelism settings or tests were changed/suppressed for this audit.

| Run | Actual result | Evidence / effect |
| --- | --- | --- |
| Full application VSTest, bounded to240s | **731 passed,38 failed,1 existing skip; ABORTED by PAL_SEHException**, about60s test time | /tmp/uabs-cp21-final-linux.T1LtTP/linux-full.trx and console.log. 770 recorded outcomes are not a complete discovered-suite total. Exit1, not timeout. |
| Focused checkpoint/criteria/bundle/header/memo/G5 suites | **122/122 PASS** | /tmp/uabs-cp21-final-focused.c1yWum/focused.trx; includes exact G6 binding test |
| Isolated paging/performance | **20/20 PASS** | same directory, paging-isolated.trx |
| Isolated representative failures | **8 FAIL,2 PASS** | /tmp/uabs-cp21-final-isolation.b3Zxz5/isolated-failures.trx; replacement fails, memo/generation change variants pass |
| Isolated raw V2SearchTests | **27 passed then ABORTED by PAL_SEHException**, about7.46s | /tmp/uabs-cp21-final-raw.kIS25c/raw-search.trx and console.log; no assertion failures reported before crash, not a passing suite |

Local /tmp evidence is not a durable GitHub artifact and is not committed as logs.
Reproduce the complete test invocation from the verified checkout (use a new
results directory and preserve output even when the command exits nonzero):

```sh
dotnet test Avalonia/UABS.UnityAssets.Tests/UABS.UnityAssets.Tests.csproj -c Debug --no-restore -m:1 -nr:false -p:UseSharedCompilation=false --logger 'trx;LogFileName=linux-full.trx' --logger 'console;verbosity=normal' --results-directory /tmp/NEW-results
```

The full run used a240s outer timeout but crashed before that limit. Isolated
paging uses `--filter FullyQualifiedName~V2SearchPagingPerformanceTests`; isolated
raw Search uses `--filter FullyQualifiedName~UABS.UnityAssets.Tests.V2SearchTests.`
with a120s outer bound. A supported-Windows full application run should execute
the same test project on the audited SHA and retain its TRX, not merely rebuild it.

No native dump/backtrace was obtained. The last reported passing test does not
identify the crashing test or root cause; native host, runtime and dependency
causes remain unresolved. Isolated reproduction means the crash is not explained
away by the two global counter assertions.

### Failure inventory and risk classification

All names below are under `UABS.UnityAssets.Tests`. Counts are cases, including
theory parameters. These seven test files and relevant backend/package/creation/
browse implementations have no diff from historical baseline
`59c7e0762c056e55bb9be0dee67e0f7120d55942`. That supports a pre-existing-source
assessment, **not proof all failures are harmless or that every historical failure
was identical**. The old per-case baseline logs/in-process runner are not available
in the repository/current scratch files; no new baseline worktree was created.

| Group | Cases / current evidence | Classification and required disposition |
| --- | --- | --- |
| V2CreationTests | 25 failures, inventory below; isolated SingleSource also fails | Explicit service Windows/local fixed NTFS destination guard fires before extraction on Linux. Reproducible unsupported-platform prerequisite; secondary null/timeout/status failures are not successful creation tests. Verify on Windows; no creation guard removal. |
| V2FolderDiscoveryTests | 5; four report missing kernel32.dll, one selection merge mismatch; HeaderDiscovery independently fails | Four directly environmental Windows interop failures. Merge omission is consistent with rejected discovery but not separately root-caused; not marked harmless. |
| PackageMemoServiceTests | 3 Inherit failures, all independently reproduce | Hard-coded C:\\old and D:\\new paths plus host Path.GetFileNameWithoutExtension; Windows-path/host-separator mismatch is source-supported. Legacy/non-CP21 path; Windows behavior still needs regression evidence. |
| PackageSearchServiceTests | 1 source-path failure, independently reproduces | Query uses `missing-sources\\alpha` against host-built Linux paths. Source-supported test/platform path assumption, not evidence V2 default search failed. |
| V2BrowseIntegrationTests | 1 replacement failure, independently reproduces; memo/generation variants pass | In-place File.Copy fails with file-in-use before stale-read assertions. File sharing/platform/test-contract issue; exact intended Windows behavior needs evidence, not a safety waiver. |
| V2MemoWriteAdmissionTests | 1 publication-reader failure, independently reproduces | CheckAdmissionAsync rejects because memo pin is unavailable; pin acquisition is Windows-only. Linux capability boundary; successful Windows regression is not supplied. |
| V2SearchPagingPerformanceTests | 2 failures: SelectiveCriteria_DoNotForceTheAsciiIndex, memo expected1/actual3; class expected1/actual4 | Global UABS.Search ActivityListener can count unrelated candidate-query activities across concurrently executing tests. Isolation20/20 passes; source supports test-isolation interference as a hypothesis, not a proved per-event explanation or production request-query defect. Full-run failure remains real; separate investigation or explicit risk acceptance required. |

Exact failing case inventory (class prefixes abbreviated as above):

```text
V2CreationTests:
  OptionalEnrichmentWarnings_DoNotRequestPartialApproval
  BadSources_HaveActionableDiagnosticsWithoutPublication [unreadable,missing,unsupported]
  PartialCoverage_RequiresExplicitDecisionAndDoesNotReextract [True,False]
  DuplicatePhysicalInputs_AreRejected [normalized,same]
  SingleSource_RoundTripsAllTypesNamesSignedIdsOrdinalsAndDependencies
  ExistingDestinationSidecars_AreNeverDeletedOrAdmitted
  RegistrationFailure_IsSeparateFromVerifiedCreationAndKeepsSavedFile
  PublicationCollisionAfterValidation_PreservesCompetingOutput
  LateCancellation_DoesNotClaimRollbackAfterPublication
  VerificationFailure_AcknowledgesPublishedFileWithoutDeletingIt
  SourceRevisionFailure_DiscardsProvisionalRowsWithoutPartialOverride
  ReleasedSourceChangedAfterExtraction_IsRejectedByFinalFingerprintCheck
  ExplicitApprovalAndCommandDisabling_AreWiredInActualCreationView
  LargeBuildCancellation_CleansStagingAndClosesSourceHandles
  LargeActualSourceBuild_UsesOneExtractorAndReleasesNonCurrentHandles [1000,257]
  CreationForm_RegistersAndOpensThroughExistingCatalogWithoutChangingDirtyEditor
  MultipleSources_EqualNamesAndBytesKeepOrderedDistinctOccurrenceIdentities
  GenuineInputs_CopiedToDisposableSources_RoundTrip86Assets
  CancellationWhileAwaitingApproval_LeavesNoPackageAndPreservesDetails
  NoPartialCallback_NeverPublishesIncompleteCoverage
V2FolderDiscoveryTests:
  FolderCommand_MergesManualAndOverlappingFoldersAndSupportsRemoval
  LargeRecursiveDiscovery_HasNoLimitAndReleasesEveryHeaderHandle [257,1200]
  ManyUnsupportedFiles_KeepOnlyBoundedDiagnosticSamples
  HeaderDiscovery_IncludesNestedExtensionlessSourcesAndReportsUnsupportedFiles
PackageMemoServiceTests:
  Inherit_UsesSameRelativeIndexAndPreservesEmptyIndexesAndUnknownFields
  Inherit_ReportsAmbiguousBundleAndAssetNamesInsteadOfSelectingFirst
  Inherit_HandlesBundleNamesWithoutUnderscores
PackageSearchServiceTests.Search_FindsSourcePathWithoutRequiringItToExist
V2BrowseIntegrationTests.ExternalChanges_InvalidateRatherThanPublishCachedRows [replacement]
V2MemoWriteAdmissionTests.PublicationReader_BlocksStalePagingEvenAfterMemoRefresh
V2SearchPagingPerformanceTests.SelectiveCriteria_DoNotForceTheAsciiIndex [memo,class]
```

The existing skipped case is `V2CreationTests.PhysicalHardlinkAlias_IsRejected`,
explicitly skipped for restricted Win32 hardlink permissions; it was not newly
disabled. Historical full in-process791-pass evidence is a different execution
method/environment and is not substituted for today's failed VSTest run. Historical
baseline36 failures/PAL crash and current38 failures are similar groups; a matching
count alone does not establish identical cases or native root cause.

No new CP21 production regression is demonstrated by these results. Equally,
the crash and incomplete full suite cannot be declared irrelevant or harmless.
The checklist does not mandate green Linux VSTest, so repairing Windows-only
creation APIs on Linux is not silently added to CP21. The **regression-risk
disposition itself remains mandatory**: preferably obtain full application xUnit
evidence on supported Windows at this exact code SHA, separately from UI jobs,
and triage the native crash/counter isolation. Alternatively the responsible user
must explicitly accept documented baseline/platform limitations and residual
unresolved risk with follow-up ownership. This audit does not grant that waiver.
Any demonstrated new product defect would require resolution or explicit separate
disposition. No actual product defect is established solely by these platform/
fixture assertions or crash; the native cause remains unknown.

## Required next actions — no implementation authorization

1. Supply existing run artifacts (or review them in an authorized GitHub session):
   all acceptance.json/results.json/preparation.json, G1 action/order/Inspector
   reports, G2 retained-state records, G5 hash/rejection/recovery reports and images.
   Record exact SHA/branch and per-case outcomes; do not rerun passing cases merely
   because this session cannot download them. CP21-04's actual complete HelpText
   is specifically needed for the G6 information-equivalence review.
2. Choose G6 option A or explicitly approve B with its evidence and limitations.
3. Obtain supported-Windows full regression evidence and/or an explicit responsible
   regression-risk disposition, including the unresolved native crash, counter
   isolation and platform-only failures. Do not infer this from the six UI jobs.
4. Only after those decisions/evidence are recorded, ask the user for explicit
   Checkpoint2.1 acceptance/closure. Do not change issue statuses or start Checkpoint3.
