# Package V2 — .NET SQLite Validation Spike

**Date:** 2026-10-20. **Baseline:** clean `d0a6834`. **Status:** isolated validation complete; production adoption remains unapproved.

The [experimental project](../../experiments/PackageV2.SqliteSpike/README.md) targets net9.0 and references the existing Unity-independent asset-session project. It is not in the solution and no production service or schema has changed. Microsoft.Data.Sqlite 10.0.12 restored from the local cache; native SQLitePCLRaw bundle 2.1.12 remains experiment-only. No network dependency was needed to restore.

## Provider and isolation

Debug build succeeds with zero warnings/errors using SDK 10.0.401 targeting **net9.0**, matching UABS; execution used **.NET 9.0.5**. The cached provider's .NET Standard assets work on this target; runtime SQLite reports **3.53.3**. SQLite is referenced only by the experimental executable, not `Avalonia.sln`, the application, or production persistence. Restore used `--ignore-failed-sources -p:NuGetAudit=false` for an offline cached experiment, not a production dependency/security approval.

Microsoft.Data.Sqlite supplies the default SQLitePCLRaw `e_sqlite3` bundle; Windows x64 native loading was executed successfully. Cached native RID assets also include Windows ARM64/x86, Linux x64/ARM64/musl and macOS x64/ARM64, but Linux/macOS execution, packaging/signing and filesystem locking were **not** tested. Future production adoption requires RID deployment and dependency/license/security review. No Unity runtime is introduced. [Provider packaging](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions)

The first normal edit required a grant for parent-directory creation. The initial test's move operation was denied in sandbox-owned OS temp; tests were rerun in a fresh authorized, ignored experiment `artifacts/` directory and passed. No process was killed, security setting altered, source bundle modified or real package used as output.

## All-asset enumeration: actual fixture findings

`Tests.Enumerate` calls the existing `AssetsToolsAssetSessionFactory`, persists **every** returned entry and verifies class populations and unchanged SHA-256 of original fixtures. The bundle and standalone serialized-file fixtures each contain **43 assets**: Class 213 ×34, 28 ×2, 74 ×5, 91 ×1, 142 ×1. Thus seven non-Texture2D/Sprite records survive, unlike the production package writer's whitelist. Both fixtures have one serialized file and no reported warnings. Final observed single-run open times were approximately 87.3 ms for `aigirl.ab` and 35.0 ms for `aigirl.assets`; these are not repeated extraction benchmarks.

| Metadata | Acquisition and limitations |
| --- | --- |
| Path ID, class ID, byte size | Existing serialized asset-table enumeration; no full object tree required. Byte size is available from `AssetEntry`, though not a column in this minimal spike. |
| Source/logical identity | Original path plus `AssetEntry.LogicalFile`; mapped to source/serialized rows. Existing API supplies a name, **not directory-entry ordinal or guaranteed-unique logical ID**. Real multi-file/duplicate-logical-name fixtures remain missing. |
| Human-readable type | Existing type-tree/class database lookup, enum/fallback label; unknown numeric classes retained. Mixed-version class-database behavior still needs work. |
| Name | Limited serialized reads by `ReadIdentityFast`/`ReadKnownName`; special GameObject/MonoBehaviour layouts are version-sensitive. Existing API replaces missing/raw whitespace names with display fallback, so raw-null recovery is not demonstrated. Synthetic inputs separately test nullable names. |
| CAB | Nullable experimental column; fixture import does **not** invent CAB by assuming logical name is authoritative. A production extractor must supply structured serialized-file/CAB identity separately. |
| Heavy work | Bundle decompression may allocate an unpacked stream. No texture/sprite/audio/mesh decoder or preview path was invoked; “metadata-only” does not mean no decompression. |

Source: [session factory](../../UABS.UnityAssets/AssetsToolsAssetSessionFactory.cs), `EnumerateAssets`, `ReadIdentityFast`, `ReadSpecialName`. The production builder still filters 28/213; it is intentionally unchanged. Production all-asset ingestion is **not** implemented by this spike.

## Experimental schema and identity

`Store.cs` creates versioned package metadata (`application_id`, `user_version=2`, package ID/generation/version), normalized source paths, serialized-file rows with nullable CAB, and assets with signed 64-bit Path ID, numeric Class ID, nullable type/name/memo, memo revision and a managed `memo_nonblank` flag. Foreign keys and `UNIQUE(serialized,pathid)` enforce local relationships. Name/class/Path-ID B-trees and an external-content FTS5 trigram table are separate build phases. No asset payloads are stored.

```mermaid
flowchart LR
    Package["Experiment: package ID + generation"] --> Source["Source ID + original path"]
    Source --> File["Serialized-file ID + logical name + nullable CAB"]
    File --> Asset["Path ID + Class ID + nullable name/type"]
    Asset --> Memo["Nullable memo + revision"]
```

Memo target key is `(package, generation, source, serialized file, Path ID)`, never name or current selection. Synthetic tests distinguish duplicate names/Path IDs in different source/logical files and retain unknown classes. Package IDs and generation strings are fixture identifiers, **not a finalized UUID/regeneration contract**. Dependency declarations, source revisions, byte ranges and schema-upgrade/forward-version handling remain production contract work.

## Search semantics

All twelve benchmark query shapes compare complete ordered `Row` records against an independent LINQ predicate using `OrdinalIgnoreCase`: exact name, substring, OR includes, excludes, Class ID, numeric Path ID, memo substring, memo-only, combined name/class/memo, broad results, no match, and short terms. Includes OR together; excludes reject before remaining filters. Null and empty memos remain distinct; `memo_nonblank` uses .NET whitespace semantics including Unicode whitespace.

Exact-name indexes register a .NET `ORDINAL_NOCASE` collation on **every connection**. Generic SQLite tools lack this collation, and a changed comparator/runtime can require versioned collation registration and REINDEX; the experiment does not establish a portable permanent sort contract. Non-ASCII substring candidates fall back to a registered .NET `contains_oi` function. For a database known to contain only ASCII names/memos, ordinary SQL uses literal `instr(lower(...))`; trigram candidates are enabled only for eligible ≥3-character ASCII-alphanumeric terms, followed by authoritative literal predicates. Punctuation, short terms and non-ASCII databases fall back, preventing false negatives. A Unicode memo write conservatively clears the database's ASCII flag. Uncooperative external writers could invalidate that optimization: the flag is a prototype writer contract, not a universal database truth.

Native probes show trigram MATCH `as` →0, `äpf` →1 for `Äpfel`, and two-character Chinese `钩爪` →0; the application-style fallback still finds short/CJK substrings. Default SQLite case rules are not silently substituted for .NET semantics. FTS update triggers keep memo candidates synchronized; insert/delete/reindex maintenance is not a complete production implementation. [SQLite trigram behavior](https://www.sqlite.org/fts5.html#the_trigram_tokenizer)

This is an **asset-query kernel**, not a replacement UABS request adapter: `Query()` deliberately enumerates all rows for diagnostics, and class-only/memo-only filters are V2 workloads. Includes/excludes and Exact here apply to asset names; the separate memo predicate is substring AND filtering for combined workloads. Production Search can OR name/memo selected fields and apply Exact/excludes across those fields. Existing UI empty-text rejection, package/source result kinds, selectable fields, term normalization and current sort options still need explicit adapter tests. These differences are intentional spike scope, **not approved changes to Search**. The reference predicate does not call production `PackageSearchService`; passing these checks is not full service/UI compatibility certification.

## Federation, cancellation and result limits

The same selected path snapshot is queried sequentially or with semaphore-bounded concurrency 2/4, one unpooled connection per active worker. Results merge by numeric synthetic ID then package ID; immutable package/generation/source/logical provenance is retained. The selected-scope assertion excludes unselected databases. Connections dispose on completion; a closed database was moved/reopened successfully. This demonstrates handle lifetime, **not** an implemented application registry, active-unload race protocol or package-copy disambiguation UI.

An eight-database ATTACH alternative was measured with sequential reads on one connection. It avoids some connection overhead but is not a single cross-database SQL query or parallel scan. The native build rejects attachment 11 with “max 10”; do not design unlimited fan-out around ATTACH. Keyset pagination (`id > last`, fixed per-package limit) reconstructs a complete test result without gaps/duplicates. The timed 100-row retrieval is **one package**, not a global federated page; production requires merge cursors, selected sort keys, revision consistency and a visible no-silent-truncation policy.

Cancellation is checked before work, while waiting for the semaphore and during materialization. `sqlite3_interrupt` is registered on the query-owned connection and unregistered before disposal. A deterministic recursive query cancels from its first callback and reports native interrupt; pre-cancelled queries also reject. No arbitrary sleeps are used. Live cross-workspace generations, unload during active federation and cancellation of busy writer waits are not implemented here. Provider database calls are synchronous; federation deliberately uses bounded background tasks. [Provider async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)

## Memo transactions and portable journals

`Store.Save` uses a non-deferred transaction (`BEGIN IMMEDIATE`), exact full key, expected original memo (`IS` preserves NULL/empty) and expected row revision. Only the target memo/revision/derived flag is updated, including FTS maintenance; commit is followed by an independent readback of memo/revision/key. Rollback leaves the full record snapshot unchanged. Two concurrent connections racing the same original value/revision produce **one verified save and one conflict**. A separately held write lock produces busy/locked failure without mutation. Other assets remain byte-for-byte equal at the serialized record level.

This is row-level optimistic concurrency, not the prior audit's conservative expected **package** revision. Missing identity and conflict currently share `ConflictOrMissing`; busy exceptions remain provider exceptions. There is no editor guard/catalog publisher/draft integration, file-replacement detection, injected commit/verification failure, corrupt-file recovery or automatic merge. Failed draft retention remains a responsibility of the existing coordinator in a future adapter. Pre-commit cancellation rolls back; committing/readback completes synchronously. This is not production persistence approval.

Tests observed `-wal` and `-shm` while a WAL transaction was open, their removal after last close, and a `-journal` during a rollback-journal write. Rollback followed by integrity check succeeds; closed DELETE-mode DB moves and reloads. These observations do not guarantee crash cleanup. A live WAL main file must not be copied alone; checkpoint/close or SQLite backup is required. DELETE mode is the recommended initial portable/local-file policy; evaluate WAL for sustained concurrent UI readers only with explicit backup/checkpoint/unload rules. Network filesystems and multi-package atomic saves remain outside this test. [SQLite WAL limitations](https://www.sqlite.org/wal.html)

## Benchmark method and results

### Method and retained evidence

Each size uses eight independent packages, 1,000 assets/source, two synthetic logical files/source, duplicate names, signed repeated Path IDs across sources, eight class labels (including unknown), nullable names and 60% null / 10% empty / 10% spaces / 20% varying nonempty memos. Dataset rules mirror the [Python audit](package-format-v2-architecture-audit.md), but schemas/materialization differ. Three complete fresh builds are run sequentially; insert, secondary B-tree and FTS phases are timed separately. Their medians are additive approximations, not a separately measured total median. SQL defaults remain DELETE journal with FULL synchronization; final database totals have no live sidecars.

Queries compare full ordered **13-column** records (including exact key, memo revision, original source path and logical name), never IDs only. First execution and three subsequent repetitions are retained; tables show three-run medians. Each query opens/closes unpooled connections, so “warm” means process/JIT/OS cache warm, not a persistent SQLite connection cache. The in-memory baseline scans an already-hydrated list and materializes new immutable record snapshots; it excludes the cost/memory of loading that list. Equality hashes are streaming and outside timed queries. Each size passes 36 first/plain/trigram full-record comparisons plus six federation, one attachment and three verified-save checks.

`probe.ps1` separately runs three fresh processes. These are **process-cold, OS-cache-warm**, not disk-cold; elapsed time excludes process launch but includes first provider/native/query setup. Filesystem caches were not flushed. Process memory is approximate aggregate working-set/managed/peak sampling across build, snapshots, hashing and queries—not isolated per-index or steady-state memory. Concurrent writers and journal tests use small disposable packages, not the performance datasets.

Retained local outputs beneath `experiments/PackageV2.SqliteSpike/artifacts/`:

- Final focused tests: `uabs-v2-spike-bed5164db65a45ee8f144d1d28799727` (`tests.json`, `enumeration.json`, native probes).
- Final 10k / 100k / 1M: `uabs-v2-spike-3f727a6402274a29902dc0a06f058e48`, `uabs-v2-spike-c921bad98ed54b45a7d05bda4ea57b18`, `uabs-v2-spike-87ffa8ddf46f48e5ae58e764e0707310` (`benchmark.json`, `process-cold.json`, disposable DBs).

Artifacts are deliberately ignored, not committed production data. Reproduction commands are in the experimental README; fresh runs generate new IDs.

### Storage, update and memory measurements

All timings are milliseconds; sizes include all eight package databases with FTS. Single-memo timings include finding a current target, conditional transaction, commit and fresh verification, without production catalog/UI callbacks or editor guards.

| Assets | MiB | Insert median | B-tree median | FTS median | Verified memo median | Aggregate process peak MiB |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10,000 | 2.12 | 202.9 | 88.2 | 98.2 | 10.30 | 96.8 |
| 100,000 | 16.38 | 696.3 | 258.6 | 269.6 | 9.32 | 338.3 |
| 1,000,000 | 161.84 | 6896.9 | 2730.7 | 2243.5 | 11.67 | 2828.9 |

The million-row process retained a full metadata baseline and repeatedly created large result snapshots; its ~2.8 GiB peak is evidence **against** unbounded materialization, not SQLite's intrinsic catalog footprint. A separate fresh selective/page probe stayed around 35–37 MiB. No million-row real Unity corpus was opened.

### One-million-asset query medians

| Query | Rows | Plain SQL | Trigram policy | Preloaded .NET scan |
| --- | ---: | ---: | ---: | ---: |
| Exact `heroicon` | 965 | 20.04 | 18.91 | 136.96 |
| Substring `hero` | 965 | 404.37 | 22.73 | 146.80 |
| OR `hero` / `0499` | 1160 | 614.70 | 29.66 | 142.09 |
| `texture`, exclude `0040` | 121307 | 755.31 | 814.66 | 190.97 |
| Class ID 43 | 125000 | 484.95 | 578.58 | 183.86 |
| Path ID -77 | 1000 | 17.07 | 16.42 | 134.29 |
| Memo `approved` | 100000 | 586.92 | 753.53 | 154.08 |
| Memo only | 200000 | 740.78 | 782.34 | 186.83 |
| Name `asset` + Class 43 + memo `approved` | 24301 | 270.37 | 1987.28 | 137.58 |
| Broad `asset` | 972007 | 3411.22 | 4632.86 | 390.38 |
| No match `impossible_needle` | 0 | 362.00 | 363.39 | 156.60 |
| Short `as` | 972007 | 3221.54 | 3388.93 | 384.71 |

“Trigram policy” means opt-in candidates where the conservative gate permits them; it is **not** FTS for every row above. `impossible_needle` contains punctuation and falls back, explaining why this no-result query does not reproduce Python's fast FTS no-match. Exact/type/Path-ID results are governed by ordinary indexes/planner and full materialization. The combined FTS query builds broad candidate intersections and is materially worse than the ordinary class-filter path. Do not promote this heuristic into a production query planner.

### Federation and bounded retrieval

At 1M total assets, all workers produce identical order/provenance:

| Query | Sequential worker | Bounded 2 | Bounded 4 | Eight ATTACH, sequential reads |
| --- | ---: | ---: | ---: | ---: |
| Selective `hero`, 965 rows | 23.32 | 17.06 | 12.91 | 23.90 |
| Broad `asset`, 972,007 rows | 4792.25 | 3601.20 | 2610.77 | Not benchmarked |

This supports bounded federation, not unlimited parallelism or a universal four-worker setting; disk contention and package counts beyond eight remain unmeasured.

Fresh-process selective medians and per-package 100-row page probes:

| Total assets | Process-cold `hero` | Direct bounded scan, 100 rows | Forced trigram candidates, 100 rows |
| ---: | ---: | ---: | ---: |
| 10,000 | 62.50 | 1.30 | 3.71 |
| 100,000 | 69.08 | 1.38 | 16.35 |
| 1,000,000 | 81.78 | 1.37 | 140.97 |

Page columns report medians of three process-local three-run medians. **LIMIT does not fix a bad candidate plan:** the broad FTS `IN(SELECT rowid...)` work is performed before the limit helps. Direct keyset scanning delivers a page cheaply. Global federation paging still needs a designed merge cursor.

### Comparison and remaining performance uncertainty

The Python audit's million-row trigram prototype was 209.54 MiB; this experimental schema is 161.84 MiB. Selective substring was 6.1 ms there versus 22.7 ms here, broad full results 4566.9 versus 4632.9 ms, and verified prototype memo updates roughly 6.5 versus 11.7 ms. These are **not direct language/provider rankings**: SQLite versions, schema, collations, result fields, transaction verification, connection lifecycle and allocation differ. No .NET compact-JSON read/write benchmark was added; earlier JSON comparisons remain prototype evidence, not a new .NET measurement.

The direction remains consistent: selective indexes and fine-grained memo updates are useful; compact JSON/preloaded scans can still win size or broad query latency. One Unicode value disables the package-wide ASCII fast path in this conservative spike; a real mixed-CJK/Latin corpus could behave very differently. Production needs versioned normalization/index contracts or a tested Unicode-safe candidate scheme, a selectivity-aware planner, bounded results, RID tests and representative multi-file/version inputs before claiming a scalable replacement.

## Production integration effects and approval gates

| Area | Reuse | Required production work (not implemented) |
| --- | --- | --- |
| Creation | Read-only asset sessions / cancellation / progress | Raw name status, stable logical-file ordinal, per-file versions/errors, all-class ingestion, dependency extraction in one lifecycle |
| Loading/catalog | Selection and lifecycle boundaries | Lightweight versioned database registration, handles, freshness and duplicate package generations instead of full document hydration |
| Search | Long-lived ViewModel, criteria snapshots, stale-generation protection | Versioned query adapter, Unicode/index policy, bounded connection ownership and global paging/current sort semantics |
| Memo Stages 1–6 | Shared drafts/debounce/Save Now/Retry, UI save states, failed-text retention, exact-row notification | New immutable identity and revision adapter, package guard integration, verified revisioned delta publication and all-exact-row synchronization |
| Package Editor | Isolated edit state and explicit Save/Discard/Cancel | Database snapshot/base-revision commit, continued clean/dirty editor exclusion; no implicit save or stale clone overwrite |
| Dependency analysis | Resolved/unresolved/ambiguous model | Persist raw per-serialized-file external/CAB relationships, avoid using CAB as global identity |
| Persistence/UI publication | Application coordinator, verification/outcome contracts, dispatcher boundary | DB transaction/recovery/backup protocol; publish only confirmed changes on UI thread without rerunning Search |

**Recommendation:** proceed to a separately approved V2 schema/identity and extraction-contract milestone, not replacement of the package loader. Pin supported local storage/RIDs, decide row versus package revision policy, define generation/regeneration and copied-package behavior, and approve global paging/Unicode semantics first. SQLite is viable in the actual .NET runtime but not universally faster; retain scan paths and avoid unconditional FTS for broad queries. Stage 7 shutdown/crash draft safeguards remain deferred. No new Unity-exclusive replacement decision was found.

## Final verification and changed scope

- Debug experimental executable and referenced infrastructure project: **0 warnings, 0 errors**. Final focused executable run: **60 passed, 0 failed**, plus successful 10k/100k/1M benchmark assertion runs and three process-cold probes per size. These are not a new production-suite count.
- Narrow assembly-reference/top-level-output checks found no UnityEngine/UnityEditor dependency; fixture hashes remained unchanged. No desktop interaction or real package writes occurred.
- Documentation validation: four documents, **42 local links**, balanced fences and one manually reviewed Mermaid block. No graphical Mermaid renderer was run. Staged/unstaged/new-file whitespace and scope checks pass; LF/CRLF notices are informational.
- Added experiment: project file, `Store.cs`, `Search.cs`, `Program.cs`, `probe.ps1`, README and local ignore rules. The local exception keeps its project file visible despite the repository's Unity-era `*.csproj` ignore; build outputs and disposable datasets remain ignored. Updated only this report, the docs index and session handoff outside the experiment.
- Production code, application dependencies and solution configuration are unchanged; neither a startup-app rebuild nor full production regression suite was needed under this task's verification rule. No production UI readiness, Linux/macOS execution, crash safety or V2 migration is claimed. Index unchanged; no staging/commit/reset/clean/branch change.

Suggested commit: `spike(packages): validate per-package SQLite on .NET`.
