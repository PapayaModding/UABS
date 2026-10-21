# Package Format V2 — Gate 3 isolated Search and federation

**Date:** 2026-10-21. **Baseline:** clean `1af9383`. **Status:** isolated implementation and regression verification complete; full performance calibration/acceptance pending. No production V1 Search, UI, package writer or approved schema changes.

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

## Final verification and acceptance boundary

- Sequential Debug library/test/startup and solution builds: **zero warnings/errors**. Focused **41/41 standalone entries pass** (40 functional, one inactive benchmark), covering differential V1 membership/categories and all sorts/directions, NULLs/ambiguous ties, unload draining, queued/sort/count cancellation, budget failure and recovery.
- Full standard in-process xUnit executor: **453 run, 0 failed, 0 skipped, 19.58 s**, including existing headless/Unity dependency audits. Both benchmark opt-ins were disabled. These are **not VSTest results**; the known blocked VSTest transport was not retried/bypassed. No desktop launch/control or visual observation.
- Independent three-assembly reference/output audit passes: no UnityEngine/UnityEditor references and no V2/SQLite libraries in startup output. Approved embedded schema equals canonical design and is unchanged from HEAD. Genuine bundle/serialized fixture hashes are unchanged. Git protected-path, staged/unstaged/new-file whitespace/conflict checks pass; index remains empty. Removed only the new full-test log after identifying its fixture diagnostics; disposable artifact root is empty.
- Documentation checks: **5 documents / 71 local links/anchors**, balanced fences, four Mermaid blocks inspected for syntax (not rendered). Changed files: four isolated Search files, `V2SearchTests.cs`, this report, documentation index and session handoff. No production source, persistence, schema, real package or legacy changes.
- **Acceptance:** safe, buildable implementation checkpoint, but **not full Gate 3 performance acceptance**. Numeric budgets in the [approved design](../03_design/package-format-v2-design.md) remain unapproved; current measurements are preliminary calibration evidence. Next exact task: review/propose approved budgets, profile the measured scan/collation allocation cost, then complete the approved three-process, 32/128-package, long-memo/skew and late-page/resource matrix under those budgets. Do not start Gate 4 or production integration automatically.
- Genuine multi-file/duplicate-CAB/version fixtures, supported-RID/native/filesystem fault coverage and hostile-input limits remain open before production integration. Synthetic Search tests do not close real extraction identity gaps.
- Gate 4 memo/editor adapters and Gate 5 production UI need separate authorization. Gate 3 does not implement either, legacy migration or deferred Stage 7 shutdown/crash recovery.

Gate 4 prerequisites: review immutable occurrence/addressable keys and stale-vector behavior; implement separately approved transactional expected-original/memo-revision checks, editor exclusion, verified reload/publication and exact-row delta adapters. This Search result already carries identity status and memo revision, but does not make ambiguous/incomplete rows writable. Gate 5 additionally requires accepted adapters, performance calibration, real identity/RID/fault evidence and separate production-integration authorization; the current V1 UI remains authoritative until then.

## Search measurements (current query implementation)

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
