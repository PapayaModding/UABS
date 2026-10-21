# Package Format V2 — Gate 3 isolated Search and federation

**Date:** 2026-10-21. **Gate 3 baseline:** clean `1af9383`; **Gate 3B baseline:** committed Gate 3 at clean `53c7bea`. **Status:** isolated implementation and Gate 3B calibration complete; reference-budget approval remains pending. Numeric budgets are recommendations until user-approved. No production V1 Search, UI, package writer or approved schema changes.

## Implemented API and semantic authority

[Search contracts](../../UABS.PackageV2/Search/SearchContracts.cs), [session](../../UABS.PackageV2/Search/SearchSession.cs), [query planner](../../UABS.PackageV2/Search/SearchQuery.cs) and [service](../../UABS.PackageV2/Search/V2SearchService.cs) remain inside the isolated V2 library. Register validated read-only packages, explicitly select registration IDs, create immutable sessions, read bounded pages/counts, cancel/invalidate, or unload and drain workers. The application has no new dependency on V2.

Matching follows [V1 Search](../../UABS.UnityAssets/Packages/Search/PackageSearchService.cs): literal .NET OrdinalIgnoreCase; English/Chinese comma terms, trim/deduplicate; OR includes; candidate-wide excludes; Exact applies to excludes/memos too. Asset name/memo are independent searchable fields; Memo only is an additional .NET non-whitespace gate on assets, not package/source rows. Empty/exclude-only criteria do not enumerate the index. Signed Path ID suppresses non-assets and still honors text/excludes. Optional Class ID/exact OrdinalIgnoreCase type-name filters require Asset-only results. An explicit full occurrence identity can execute without text.

Package results search display name/current registered path. Source results represent physical source UUID occurrences and search index directory, original name/path, logical CABs and raw dependency declarations (original/virtual/CAB evidence). Excludes apply across all those selected source fields. They do not invent a single source CAB or collapse separate source occurrences. Assets retain package/generation/source/revision/original logical ordinal/Path ID/table ordinal and identity status; missing names and unknown classes remain eligible for numeric/memo searches. No original source is opened. Richer V2 source evidence and raw nullable names intentionally extend V1's representable data, not its comparison rules.

## Query plan, ordering and bounded ownership

Parameterized per-package candidate branches push result kinds, source/exact occurrence, Path ID, Class ID and compatible nonblank-memo gates into SQLite. Safe exact-ASCII name candidates include both indexed ASCII keys **and all uncovered NULL-key rows**, followed by authoritative .NET matching; unsupported profiles ignore derived keys/flags. No LIKE/NOCASE/lower/FTS dependency. Unicode/short text uses correct cancellable managed predicates; type labels use exact ordinal-ignore-case equality.

All seven V1 primary sorts use its OrdinalIgnoreCase-then-Ordinal text comparator, NULL-first ascending order and numeric Path IDs. Only primary direction reverses; ties remain ascending: package name/path, index directory, source path, asset name, nullable Path ID, kind, then immutable UUID/ordinal key. Query-time .NET collation is not stored in canonical indexes. Expanded lexicographic continuation predicates explicitly handle NULLs and mixed directions; no OFFSET.

```mermaid
flowchart LR
    Session["Immutable criteria + selected revision vector"] --> Workers["Two owned workers, cap four"]
    Workers --> Local["Per-package keyset ORDER BY / LIMIT K+1"]
    Local --> Merge["Bounded global top K+1 candidate set"]
    Merge --> Hydrate["Load memos only for emitted assets"]
    Hydrate --> Check["Revalidate selected vector and registrations"]
    Check --> Page["Publish page / last-emitted opaque cursor"]
```

Per-package top K+1 is sufficient for global top K+1: any locally later row already has K+1 predecessors. The merge retains at most K+1 candidates plus current worker rows and O(selected packages) scope/tasks; it does not retain per-package K-row lists or all matches. Native SQLite may scan/sort many rows and spill to temporary disk; managed bounding is not a latency guarantee. Each worker has one read-only unpooled connection, 4 MiB SQLite cache target, FILE temporary store and query-only mode. Connections close before user think-time. Defaults: two workers, configurable 1–4; selected-package cap 128; page 100, max 500. Time/predicate budgets fail explicitly rather than returning a terminal partial page.

Default operation budget is 30 seconds and 20 million managed predicate/comparison evaluations, shared across that page/count's workers. Callers may configure the deadline (up to ten minutes) and work budget; benchmark samples use 15 seconds. Timer cancellation interrupts worker-owned native handles; caller/session cancellation remains distinguishable. Busy handling is bounded by the provider's existing five-second timeout. These are operational safeguards, not approved performance thresholds or hard memory/temp-disk limits. Single hostile large values, total temporary-disk high water and arbitrary injected blocking observers remain outside proven resource limits; observer hooks are for deterministic tests, not untrusted callbacks.

## Consistency, counts and failures

Opaque typed cursors are process/session-scoped and retain only the last **emitted** immutable sort boundary and loaded count, not prefetched buffers or live handles. Sessions capture immutable criteria, registration IDs/location epochs, package/generation/catalog vectors, comparer profile and cancellation lifetime. Short local read transactions verify generation/catalog/schema cookie before/after work; a fresh vector check plus registry check precedes publication. This is cooperating-writer revision consistency, not an atomic cross-database snapshot or tamper-proof external replacement detection.

Memo/generation/schema change or unload invalidates future pages. Existing immutable published rows are not mutated, reordered or removed. Full exact identity and memo revision are available for later verified-delta adapters; this gate adds no memo writer/draft/UI adapter. Explicit new Search/readmission is required after stale revision. Unload cancels native operations and waits for owned leases; re-registration creates a new ID even at the same location.

No pre-page COUNT: intermediate pages report LoadedAtLeast; exhaustion or explicit cancellable Count reports Exact for indexed data only. Partial index, unavailable metadata, missing optional names, unresolved/raw dependency evidence and unsupported-profile fallback warnings remain even at exact zero. Failures/cancellation expose no partial rows or advanced cursor; count becomes unknown/stale. Caller retains the previous cursor for safe retry after a recoverable failure.

## Historical Gate 3 verification and acceptance boundary

- Sequential Debug library/test/startup and solution builds: **zero warnings/errors**. Focused **41/41 standalone entries pass** (40 functional, one inactive benchmark), covering differential V1 membership/categories and all sorts/directions, NULLs/ambiguous ties, unload draining, queued/sort/count cancellation, budget failure and recovery.
- Full standard in-process xUnit executor: **453 run, 0 failed, 0 skipped, 19.58 s**, including existing headless/Unity dependency audits. Both benchmark opt-ins were disabled. These are **not VSTest results**; the known blocked VSTest transport was not retried/bypassed. No desktop launch/control or visual observation.
- Independent three-assembly reference/output audit passes: no UnityEngine/UnityEditor references and no V2/SQLite libraries in startup output. Approved embedded schema equals canonical design and is unchanged from HEAD. Genuine bundle/serialized fixture hashes are unchanged. Git protected-path, staged/unstaged/new-file whitespace/conflict checks pass; index remains empty. Removed only the new full-test log after identifying its fixture diagnostics; disposable artifact root is empty.
- Documentation checks: **5 documents / 71 local links/anchors**, balanced fences, four Mermaid blocks inspected for syntax (not rendered). Changed files: four isolated Search files, `V2SearchTests.cs`, this report, documentation index and session handoff. No production source, persistence, schema, real package or legacy changes.
- **Acceptance:** safe, buildable implementation checkpoint, but **not full Gate 3 performance acceptance**. Numeric budgets in the [approved design](../03_design/package-format-v2-design.md) remain unapproved; current measurements are preliminary calibration evidence. Next exact task: review/propose approved budgets, profile the measured scan/collation allocation cost, then complete the approved three-process, 32/128-package, long-memo/skew and late-page/resource matrix under those budgets. Do not start Gate 4 or production integration automatically.
- Genuine multi-file/duplicate-CAB/version fixtures, supported-RID/native/filesystem fault coverage and hostile-input limits remain open before production integration. Synthetic Search tests do not close real extraction identity gaps.
- Gate 4 memo/editor adapters and Gate 5 production UI need separate authorization. Gate 3 does not implement either, legacy migration or deferred Stage 7 shutdown/crash recovery.

Gate 4 prerequisites: review immutable occurrence/addressable keys and stale-vector behavior; implement separately approved transactional expected-original/memo-revision checks, editor exclusion, verified reload/publication and exact-row delta adapters. This Search result already carries identity status and memo revision, but does not make ambiguous/incomplete rows writable. Gate 5 additionally requires accepted adapters, performance calibration, real identity/RID/fault evidence and separate production-integration authorization; the current V1 UI remains authoritative until then.

## Historical Gate 3 measurements (before Gate 3B)

Reproduction: opt-in `V2SearchTests.OptInSearchMeasurements`, `UABS_V2_SEARCH_BENCHMARK=1`, size `UABS_V2_SEARCH_SIZE=10000|100000|1000000`; optional repetitions/workload labels select a bounded sample. Default 20 measured repetitions after two warmups. Run sequentially in a fresh process through the external verification runner's `search-benchmark` argument; set `UABS_V2_TEST_ROOT`, TEMP/TMP to authorized ignored disposable artifacts, not real package paths. Routine tests leave this opt-in inactive. No source bundle is needed.

Windows x64 (10.0.26200), AMD64 Family 23 Model 113, .NET 9.0.5; canonical Gate 2A schema, Microsoft.Data.Sqlite 10.0.12/native SQLite 3.53.3, DELETE/FULL, two workers, page 100, default PackageName sort except text-sort. Eight equal packages, one source/logical file each, unknown numeric class retained. Deterministic names: each hundredth row `HeroIcon`; otherwise final digit 0–2 `钩爪_` + 8-digit ordinal, 3–4 `混合_asset_`, remaining `asset_`. Thus roughly 29% CJK-only, 20% mixed, 50% Latin and 1% selective duplicate names; raw asset names in this performance corpus are non-null (functional tests cover NULL/empty). Eight repeating memo values include NULL, empty, Unicode whitespace, English, CJK, emoji and exclusions. No long-memo or skewed 32/128-package workload is claimed. Signed -77/max IDs and repeated names/IDs across sources/packages remain distinct.

Measured-source SHA-256: `SearchQuery.cs` = `4B22B1B671C8B43084BB0BC06A103ABC06F5DECDCED0461E63378785376D14C0`; `V2SearchTests.cs` (including deterministic corpus/query rules) = `70F2A0A306E43C94BF91DADA66C859AA0B1A43A62C288BC13236494A3DFF3600`. Hashes describe the physical source files used here, not a portable dataset/package identity or committed revision.

Setup/build plus fixture memo seeding is excluded from Search timing: 10k **1080.93 ms / 3,080,192 B**, 100k **13688.06 ms / 19,464,192 B**; read-only admission **76.93/395.21 ms** respectively. These are single setup samples, not three-run storage benchmarks. The temporary V1 fixture baseline is cleared before Search; no full metadata/result universe is retained for measurement. Independent streaming .NET predicate counts and explicit Search counts are compared outside timing; page boundaries are checked for duplicate keys. Full differential membership/order is tested on smaller corpora, not exhaustively timed million-row lists.

Each cell is first-page / next-page **upper sample median ms** (20 repeats after two warmups, process/OS-cache-warm). No next page exists for the eight Path ID matches.

| Workload | 10k | 100k | 100k first-page p95 |
| --- | ---: | ---: | ---: |
| Exact ASCII name | 29.50 / 26.50 | 82.83 / 83.77 | 97.13 |
| Signed Path ID | 20.99 / — | 24.24 / — | 29.95 |
| Selective ASCII substring | 30.59 / 26.03 | 98.66 / 103.62 | 108.68 |
| Two-character Chinese | 34.10 / 35.09 | 149.86 / 165.85 | 163.03 |
| Mixed name OR memo | 48.09 / 51.77 | 220.51 / 244.03 | 227.70 |
| OR includes + exclude | 53.68 / 58.67 | 332.38 / 393.16 | 341.35 |
| Memo only + text | 39.94 / 41.68 | 148.10 / 161.22 | 159.10 |
| Broad asset substring | 44.00 / 49.38 | 215.32 / 261.21 | 227.63 |
| Broad with AssetName sort | 43.52 / 46.80 | 191.24 / 206.89 | 199.93 |

10k match counts in the same order: 104/8/104/2896/3000/7088/2496/7000/7000; 100k: 1000/8/1000/29000/29992/90696/25000/70000/70000. All independent count comparisons pass. Max retained merge buffer **101 candidates**, observed workers **2**, regardless of match count. Lifetime process peaks about **59.6/65.0 million bytes**, including fixture creation; they are not isolated per-query peaks or retained managed heap. Managed allocation churn per first+next page pair remains high: broad **44.3/303.7 MB**, combined **58.1/490.5 MB** (decimal). This is transient provider/collation/predicate/sort conversion churn, not a full-result retained list; it still merits optimization before performance acceptance. Working sets before/after 100k samples were roughly 57–65 MB. No physical disk-cold cache flush, high-frequency peak sampling or temporary-disk high-water instrumentation was performed.

The earlier pre-optimization 10k run was superseded after removing per-row predicate closures and constant per-package sort comparisons. Current 10k/100k figures use the same final query plan; no causal latency speedup versus that preliminary run or the non-equivalent spike is claimed. Unlimited-result spike timings are not directly comparable to these globally ordered bounded pages.

**1M bounded sample completed:** four workloads, two warmups/three measured repeats rather than the complete three-process calibration protocol. Setup/seeding **122388.20 ms / 204,013,568 B**; admission **3320.08 ms**. Exact name: **484.55/493.52 ms**, 10,000 matches; Path ID: **23.00 ms**, eight matches; CJK: **1311.70/1457.67 ms**, 290,000 matches; broad: **1961.96/2426.78 ms**, 700,000 matches. All independent counts pass. Process lifetime peak ~78.0 MB, buffer 101; CJK/broad churn **1.80/2.90 GB per page pair**. These samples do not establish full 1M or approved performance acceptance.

**Proposed budgets, not approved:** on this reference corpus, consider 100k first-page p95 ≤150 ms for indexed lookup, ≤500 ms for scan/Unicode combinations, next-page p95 ≤600 ms, and incremental process memory ≤64 MiB above a measured post-admission baseline with page buffers ≤501 + active heads. Keep a configurable 15-second hard operation deadline as a diagnostic safety limit, not an acceptable interactive latency target. Calibrate 1M, many-package and late-page budgets separately; allocation churn and temporary-disk limits need explicit targets. Three fresh-process repeats, late-page behavior, 32/128 packages, process-cold probes, skew/long-memo corpora and approved numeric thresholds remain open before **performance acceptance**.

**Unity-Exclusive Features Requiring Decisions:** none new.

Suggested commit: `feat(search): add isolated V2 SQLite search and federation`.

## Gate 3B — profiling and performance calibration

### Baseline, method and reproducibility

Gate 3 is committed at `53c7bea`; worktree was clean. Historical first-page medians (100k/1M) remain Path ID24/23ms, exact83/485ms, CJK150/1312ms and broad215/1962ms. They are not fresh guarantees or paired speedup baselines. The [opt-in calibration helper](../../UABS.UnityAssets.Tests/V2SearchCalibration.cs) creates **owned synthetic** studies, copies only its closed disposable DBs, records per-file SHA-256 manifests, and verifies identical bytes before/after every fresh-process read. No real OWL package, source bundle, schema or production registration is involved.

The paired baseline/optimized 100k study uses the **same eight databases**, original Gate 3 name/memo distribution, identical queries/fields/sort/page100 and emitted-memo materialization. Each version runs in three fresh .NET9.0.5 processes, two initial attempts/warmups then20 measured first/next-page pairs per workload. The original nine workloads are unchanged; Class+text is an explicitly added case with Class1234567 (all rows, deliberately unselective worst case). Counts are checked against independent streaming predicates outside timing. No FTS, alternative comparator, reduced result shape or changed membership is substituted.

First-attempt latency is separately recorded; only the first Search in each process is process-first (after admission/provider initialization). All files have already been read for hashing/admission; **no physical disk-cold claim**. Report median of three process medians; p95 is nearest-rank within20 measured queries **per process**, with worst observed per-process p95 where shown. Three samples/runs alone never supply a meaningful p95. The1M bounded protocol uses three measured pairs per process and reports ranges/medians, not p95.

Raw logs are retained outside Git in the scratch verification workspace (`C:\Users\kolynlin\Documents\Codex\2026-10-16\files-pasted-by-the-user-i`): `gate3b-baseline100k-p1..p3.jsonl`, `gate3b-after100k-p1..p3.jsonl`, and `gate3b-<study>-p1..p3.jsonl`. The paired table below uses the final-source20-repeat three-process `gate3b-finalsteady100k-p1..p3.jsonl` runs; the earlier after-runs precede only the unload-lifetime linkage. Final128 scope and resource checks are in `gate3b-final128-p1..p3.jsonl` and `gate3b-final-resources.jsonl`. A separate three-repeat100k sanity run was noisier and is not used to estimate p95. `gate3b-studies.jsonl` and ignored `UABS.PackageV2/artifacts/calibration-*/study.json` identify the disposable immutable datasets/hashes. Reuse them rather than blindly rebuilding/repeating completed work. The existing external runner invokes `calibration prepare/profile/run/resources`; it is not production infrastructure or VSTest.

### Measured bottlenecks and narrowly scoped changes

- Profiled100k broad first page: **272096 predicate callbacks +224976 text-sort callbacks,129734736B allocation,17 short-lived connections**,100 emitted/101 buffered rows. These counts include provider/native-to-managed callbacks, not272096 distinct matching assets. Registered observers perturb elapsed time, so profile times are not benchmark latency claims.
- Controlled read-only single-package SQL returned the exact same101 asset IDs with/without sort columns proven constant in its source table. Comparison callbacks **28095→9349**, thread-local allocations **9814424→4118712B**. Single timing samples were noisy and are not used as a speedup claim. The byte/count change isolates repeated UTF-8/string callback conversion and constant comparisons; it does not imply all native time is managed comparison time.
- Admission now captures exact BINARY min/max equality for source index directory/name/path under the admitted revision. Only proven constant sort levels are removed; their **global continuation comparisons are folded using the same .NET comparator**, including descending primary/ascending ties. Package-result NULL fields disable source folding. Mixed-source values retain the general path. No persistent sort-key cache or schema/index alteration.
- Queries with no exclusions no longer invoke exclude UDFs. Ordinary text-only assets no longer reevaluate a redundant category bitmask; numeric/identity-plus-text requests retain their required text gate. Include/memo/exclude and Unicode rules remain unchanged.
- Optimized same-data profile: **101048 predicate +74992 sort callbacks,39709016B**, still17 connections/100 emitted/101 buffered. Count reductions and~69% first-page allocation reduction identify the main improvement. Path lookup stays dominated by connection/query setup; no connection-pooling or consistency-check removal was introduced.
- DTO/global merge/cursor costs are bounded by emitted/package-head rows; callback counts and the controlled ID-only query explain why a101-row buffer did not imply low churn. Pure native execution, individual provider string allocations, DTO construction and cursor time are **not independently sampled to precise percentages**; no unavailable allocation profiler is implied. Only CSharpier was installed globally, not dotnet-trace; controlled counters were used without installing tools.

### Paired 100k results

Median of three process medians, milliseconds. Allocation is average of process-median allocated bytes per first+next pair, decimal MB; Path ID has only one page. All ten workload counts match before/after.

| Workload | Before first | After first | Before allocated MB | After allocated MB | Worst after first-page p95 |
| --- | ---: | ---: | ---: | ---: | ---: |
| Exact name | 73.60 | 54.03 | 63.57 | 34.54 | 68.12 |
| Path ID | 15.89 | 16.75 | 0.49 | 0.50 | 25.28 |
| Selective ASCII | 63.09 | 48.75 | 116.32 | 62.49 | 54.70 |
| CJK two-character | 95.46 | 68.02 | 188.60 | 68.60 | 75.25 |
| Mixed name/memo | 140.55 | 94.14 | 313.05 | 122.44 | 107.92 |
| Includes/excludes | 209.36 | 157.65 | 489.72 | 220.64 | 187.13 |
| Memo only + text | 94.45 | 63.17 | 199.87 | 69.36 | 67.50 |
| Broad | 137.71 | 92.41 | 303.10 | 80.41 | 104.00 |
| Broad text sort | 123.09 | 94.90 | 176.67 | 87.49 | 106.73 |
| Class + text | 97.25 | 70.09 | 188.59 | 68.60 | 79.44 |

Broad first-page latency improves~33%, allocation churn~73%; Path ID is effectively unchanged/slightly slower within observed variation and new safety bookkeeping. No universal speedup is claimed. Native SQLite high-water counters after optimization are separate from managed allocation/heap and process working set; observed100k native high water~0.57–3.2MB, **zero native SQLite allocation after connections close**. These do not measure filesystem I/O or temporary-disk high water.

### Resource correction and regression checkpoint

Work/time budgets alone did not bound large values or retained text. `SearchOptions` now additionally configures SQLite value/**encoded row** length (default1MiB via `SQLITE_LIMIT_LENGTH`) and conservative retained UTF-16 candidate/emitted memo text bytes (default16MiB). Oversized admission/query values and retained-text overflow produce **BudgetExceeded**, no partial rows, advanced cursor or false Exact result. Text is never truncated; callers may explicitly configure larger limits. An oversized value can conservatively refuse admission even if a later query would not select it. This is not a whole-process/native-sort/temp-disk cap; object overhead, transient conversion and caller-retained pages remain separate.

Latest focused verification: **54/54 entries passed** (53 functional/one inactive benchmark). Added all-sort/direction mixed-source/Package-NULL cases, exact exhaustion across8/32/128 packages with1/2/4 bounded workers, native/retained-text failures and full long memo preservation within budget. Debug builds pass0 warnings/errors. Additional three-process sizes, scope/deep/resource samples are complete; final verification is recorded below.

### Three-process sizes, federation and retained-memory findings

Additional runs completed with the same eight-package rules at10k/1M.10k uses20 measured pairs/process;1M uses3 pairs/process across **all ten workloads**, not just Path ID. The1M values below are medians/ranges only; no p95 is inferred from three samples. These are optimized calibration results; historical10k/1M Gate3 values are not paired causal baselines. The controlled before/after comparison is the identical100k study above.

| Workload | 10k first / next ms | 1M first / next ms | 1M observed first-page range ms |
| --- | ---: | ---: | ---: |
| Exact name | 23.70 / 16.15 | 241.33 / 238.59 | 229.71–345.90 |
| Path ID | 17.44 / — | 16.20 / — | 15.47–19.67 |
| ASCII substring | 25.34 / 15.23 | 344.90 / 332.09 | 315.19–350.29 |
| CJK two-character | 23.55 / 23.33 | 544.70 / 543.43 | 534.24–563.75 |
| Mixed name/memo | 26.13 / 26.03 | 768.20 / 773.16 | 758.86–785.85 |
| Includes/excludes | 29.73 / 30.64 | 1455.76 / 1489.55 | 1441.68–1509.17 |
| Memo only + text | 22.79 / 22.64 | 482.55 / 489.61 | 469.63–506.60 |
| Broad | 25.04 / 24.67 | 767.29 / 782.07 | 755.14–814.78 |
| Broad text sort | 27.07 / 28.25 | 776.81 / 844.60 | 752.67–788.36 |
| Class + text | 21.60 / 21.53 | 543.94 / 549.81 | 526.23–554.53 |

The many-package studies hold total rows at8192,25% nonmatching packages, same name rules in the rest. Finite per-package rounding changes exact hit counts; all independent counts/exhaustions agree. Default workers remain **two**, never128 connections simultaneously. First/next timing excludes separate count and correctness-exhaustion jobs. Sampled deep times include row/order assertions and are not p95 estimates.

| Selected packages | Broad first / next median ms | Worst first / next per-process p95 ms | Sparse / empty first median ms | Exact broad rows / pages | Page20 / page40 sample median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| 8 | 29.59 / 28.77 | 33.76 / 32.43 | 17.43 / 14.35 | 4290 / 43 | 24.40 / 15.74 |
| 32 | 72.04 / 73.07 | 85.49 / 85.08 | 57.84 / 44.53 | 4272 / 43 | 54.08 / 46.13 |
| 128 | 202.97 / 198.01 | 233.53 / 227.64 | 224.11 / 175.76 | 4128 / 42 | 182.69 / 175.44 |

Every page is checked for exact synthetic name/Path ID, selected package bounds, strict independent global comparator order and duplicate keys; exhaustion equals the independent count. No OFFSET/restart loop or false final page. Sparse matches66/72/96 and empty0 all pass. Process-first session creation medians~5.1/5.9/6.0ms; admission~101/164/442ms at8/32/128, separately measured. A profiled128-package sparse query emits96 rows through **352 short-lived connections**, zero managed sort calls:128 candidate+96 memo hydration+128 vector validation. Connection/setup/consistency overhead explains why sparse federation can be slower than broad retrieval; checks were not removed to improve scores.

Separate2048-row/eight-package long-memo study appends8192 CJK characters to one in eight memos, retaining original searchable prefix; it is a labeled additional corpus, not a hidden change to the core baseline. Three-process first/next medians: mixed52.45/47.35ms, memo-only29.06/27.53ms, broad19.42/19.18ms; counts616/512/1424 pass, worst first p95≤65.97ms. Full memo contents are returned or fail a configured budget, never truncated.

Allocation churn is **not retained heap**:1M pair allocations still~0.73GB broad,1.14GB mixed and2.21GB includes/excludes. SQLite high water≤9.1MB at1M; zero native allocation after owned connections close. Last-GC managed heap samples generally~1–6MB. A separate forced-collection probe over20 reads retaining one100-row page reports live managed~0.62–1.35MB (largest128-package delta~0.26MB), not a peak during transient allocation. Process lifetime peaks up to~272MB at1M include file hashing/admission/LOH warming; do **not** describe them as retained Search rows. Measured working-set changes are samples, sometimes strongly negative after hashing; no dedicated per-query RSS peak or temp-disk high-water guarantee is claimed.

Controlled warm SQLite query recorded264 cache hits/0 misses for both same-ID variants. This is native pager-cache evidence, not physical device I/O. No OS cache flush, I/O throughput or disk-cold claim. Cancellation probes returned no partial rows/cursor in~4–7ms with workers drained. Initial1M unload correctly returned StaleCursor but global page completion took~830ms because peer packages continued scanning; operation cancellation is now linked to **every selected registration's lifetime**, preserving the captured vector and interrupting peers/queued work. Final targeted probes:1M cancellation5.32ms/unload-and-page1.33ms;128 packages4.64/2.08ms, no partial rows, workers0. These are controlled response observations, not latency percentiles. Final128-package three-process20-repeat broad first/next medians202.53/204.15ms; worst p95 across broad/sparse/empty248.31ms. SQL/matching/sort plans are unchanged by the lifetime linkage.

### Provisional reference budgets — approval required

These are recommendations for this machine/runtime, the specified synthetic corpora, page100 and two workers, not universal guarantees or user-approved thresholds. Normalized baselines and measured after values are above; historical Gate3 figures are not paired comparisons. First-attempt/cold-process observations are recorded separately in raw logs; neither those nor warmed samples establish disk-cold guarantees.

| Workload/envelope | Recommended first / next limit | Observation and disposition |
| --- | --- | --- |
| 10k, all ten workloads | warm p95 75 / 75ms | Met in three-process calibration |
| 100k Path ID/exact/ASCII | warm p95 150 / 150ms | Met; Path ID has no next page |
| 100k CJK/mixed/excludes/memo/broad/text/class | warm p95 300 / 350ms | Met; includes/excludes remains most expensive |
| 1M Path ID / exact / ASCII | median 50 / 400 / 500ms respectively; next same applicable limit | Met; no reliable1M p95 from three repeats/process |
| 1M CJK/class/memo | median 800 / 800ms | Met |
| 1M mixed/broad/text sort | median 1200 / 1200ms | Met |
| 1M includes/excludes | median 2000 / 2200ms | Met (~1456/1490ms), still costly |
| 8 / 32 / 128 scopes,8192 rows | warm p95 75 / 150 / 350ms respectively, first and next | Met including sparse/empty; sampled deep pages also below these values, not a deep-page p95 |
| Long-memo2048 reference | warm p95 100 / 100ms | Met; no truncated memo |
| Controlled cancellation/unload | response100ms | Met after lifetime correction; not a worst-case shutdown guarantee |
| Native SQLite / forced-GC live managed | high water16MiB / live8MiB for reference page100 | Observed≤9.1MB native/~1.38MB live managed; not total process caps |
| Allocation churn100k /1M | ≤250MB /2.5GB per page pair | Met (~221MB /2.21GB worst); allocation, not retained memory |

The configured1MiB SQLite record/value and16MiB retained-text guards are actual conservative runtime limits, unlike these proposed acceptance budgets. No whole-process RSS, temporary-disk, extreme criteria-size or physical I/O bound is established. First-process admission and session setup are separately recorded; connection overhead scales with selected registrations even when few results match.

### Acceptance recommendation and remaining work

Accept Gate3B as an **isolated functional/calibration checkpoint**, subject to review and explicit approval of the proposed reference budgets. Do not label all production Search performance accepted. Unicode scans still allocate heavily at1M; the next evidence-backed options are reducing native/managed string conversion and repeated predicate evaluation, followed by separately calibrated safe connection reuse. Neither approximate matching nor unapproved FTS/schema changes are justified.

The core performance corpus is Asset-only with mostly single-source packages and one synthetic class; mixed-source, nullable keys, all result kinds and comparison semantics have functional coverage, not equivalent production-scale latency coverage. Genuine multi-file/duplicate-CAB and RID/fault prerequisites from earlier gates remain open. Physical-cold I/O, dedicated query RSS peak, temp-disk high water, more1M percentile samples and production all-kind distributions remain unmeasured. No Gate4/5 authorization is implied. No new Unity-exclusive feature decision was introduced.

### Final Gate3B verification and scope

- Sequential Debug solution build, including `UABS/UABS.csproj` startup and isolated library/tests: **0 warnings/errors**. Focused current-source **54/54 entries**; full standard in-process xUnit executor **466 passed,0 failed,0 skipped,29.05s**. Inactive opt-in benchmark Facts count as passing entries; calibration runs are separate evidence. These are **not VSTest** results; known local test-host communication restriction was not retried or bypassed.
- Three-assembly zero-Unity reference audit passed; startup output contains no Unity,V2 or SQLite assemblies. Production/V1/legacy trees, canonical schema and genuine fixture files have no Git changes; two genuine fixture SHA-256 values match the prior checkpoint. All study reads verified immutable disposable DB hashes. No real package/source was a write target.
- Five-document/73-local-link/anchor check passed; fences balanced. Four Mermaid blocks syntax-reviewed, not rendered. Canonical DDL equals approved design SQL. Staged/unstaged whitespace and new-helper whitespace/conflict checks pass. Only four isolated Search files, two test/helper files and two audit documents changed; index remains empty.
- Removed only newly generated regression-test `uabs_log.txt` (not present at baseline, header-inspection fixture log); no pre-existing work was removed. No incomplete build/journal/WAL/SHM artifacts remain. Ignored, closed calibration DBs/manifests are deliberately retained for reproducibility. No desktop application was launched or controlled; no commits/staging/reset/branch operation.
- Safe buildable checkpoint, ready for diff review/commit. Next: approve/revise the proposed reference budgets and decide whether outstanding production-scale/I/O/RID evidence is needed before performance acceptance. Do not start another gate without authorization.
