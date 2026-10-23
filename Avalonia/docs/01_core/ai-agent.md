# AI Agent Guide

## Required first reading

Before significant implementation or architectural work, read this document and [project-overview.md](project-overview.md). Then inspect the [session handoff](../05_audit/codex-session-handoff.md), [migration roadmap](../05_audit/migration-roadmap.md), the latest [milestone report](../05_audit/milestone-08-report.md), and the subsystem or architecture documents relevant to the task.

Documentation is context, not a substitute for source inspection. Verify details against the current code before changing behavior. If code and documentation disagree, record the discrepancy and update the appropriate document as part of the authorized task.

## Project purpose and repositories

UABS is migrating a Unity-based asset browser into a standalone Avalonia desktop application.

- Legacy Unity reference: `E:\Projects\UABS\UABS`
- Avalonia migration target: `E:\Projects\UABS\Avalonia`

The legacy repository is a read-only reference implementation. Never modify it unless the user explicitly changes that rule. All new product implementation belongs under the Avalonia repository.

## Non-negotiable Unity boundary

The final Avalonia application must have zero dependencies on the Unity Engine, Unity Editor, Unity runtime, or Unity-runtime-dependent plugins.

The legacy Unity project is a reference implementation only. Processing Unity asset formats with standalone .NET libraries such as AssetsTools.NET is allowed when those libraries do not require Unity itself. The Avalonia application must run on a machine that has never had Unity installed.

Do not introduce `UnityEngine`, `MonoBehaviour`, Unity lifecycle APIs, Unity UI types, Unity texture objects, Unity editor packages, or runtime installation assumptions into the Avalonia solution.

## Unity-exclusive feature annotation and decision policy

Whenever legacy investigation finds behavior that requires Unity Engine, Unity Editor, Unity-native runtime behavior, or a Unity-dependent plugin, make that boundary explicit before implementing a replacement. AssetsTools.NET and other standalone libraries are not Unity-runtime dependencies merely because they process Unity formats.

Record each meaningful dependency in [unity-exclusive-features.md](../05_audit/unity-exclusive-features.md) with a stable `UE-###` identifier, exact legacy file/method, dependency, required behavior, migration status, candidates, tradeoffs, and open decisions. Keep that identifier synchronized with the closest relevant Avalonia C# boundary using this searchable form:

```csharp
// [UNITY-DEPENDENCY:REPLACEMENT-REQUIRED]
// Finding: UE-###
// Legacy source: <relative path and class/method>
// Unity API/plugin: <specific dependency>
// Purpose: <behavior provided by Unity>
// Migration status: NOT_IMPLEMENTED | PARTIALLY_REPLACED | REPLACED | BLOCKED
// Replacement: REQUIRES_DISCUSSION | <verified standalone replacement>
```

Do not create fake implementation stubs just to hold annotations, and do not repeat a marker for every call to the same dependency. Annotate one meaningful class or method boundary. Use `REPLACED` only when tests establish the required behavior without Unity.

Do not silently select a consequential replacement when compatibility, fidelity, licensing, performance, native packaging, or architecture could materially change. Document standalone .NET, native-library, external-plugin, custom, or platform-service categories and their known tradeoffs; leave the decision pending for user discussion. At the end of every migration milestone or Unity-dependent subsystem investigation, include a final-response section titled **Unity-Exclusive Features Requiring Decisions**. Prioritize findings that block upcoming work and explicitly state when no new findings were discovered.

## Migration strategy

Work in narrow, testable vertical slices:

1. Trace the relevant legacy workflow and validate it against source.
2. Define or reuse UI-neutral contracts.
3. Adapt file-processing behavior behind infrastructure interfaces.
4. Integrate through ViewModels and explicit presentation services.
5. Add representative fixtures and lifecycle/error tests.
6. Build and verify before marking roadmap work complete.
7. Update the corresponding architecture, system, audit, and milestone documentation.

Preserve useful behavior and data compatibility before redesigning. Do not port Unity scene wiring, global event dispatch, manual UI pooling, or copied-result cache workarounds merely because they exist in the reference application.

## Current implemented boundary

Milestones 1 and 2 introduced:

- `UABS`: Avalonia Views, ViewModels, file-picker adapter, and composition.
- `UABS.UnityAssets`: AssetsTools.NET integration, file detection, neutral asset contracts, and disposable sessions.
- `UABS.UnityAssets.Tests`: real-fixture integration tests and ViewModel lifecycle tests.
- `BrowserViewModel`/`DirectoryBrowserService`: independent filesystem navigation, history, refresh, and folder selection.
- `AssetListViewModel`: cached filtering, numeric sorting, stable selection, and cancellable lazy details.
- `UABS.UnityAssets.Packages`: legacy-schema neutral package models plus load/create/validate/list/safe-save services with task, cancellation, progress, source-relative folder-import mappings, and non-I/O Windows path-identity contracts. Milestone 7.5 targets practical interoperability rather than exact Unity-writer quirks; read the [compatibility audit](../05_audit/legacy-package-format-compatibility.md) before changing package persistence.
- `UABS.UnityAssets.Packages.Search`: in-memory package metadata search with virtual source references, exact numeric identity filtering, deterministic sorting, and cancellation.
- `UABS.UnityAssets.Packages.Dependencies`: direct CAB dependency resolution over loaded metadata with resolved/unresolved/ambiguous outcomes, original-path provenance, duplicate-import collapsing, and cancellation.
- `PackageManagementViewModel`/`PackageManagementView`: package-directory catalog, package contents, metadata search, and direct dependency presentation with cancellation and stale-result guards.
- `PackageEditorViewModel`: first-package creation, isolated editing, selected-asset/source reference management, explicit destination/overwrite/discard interactions, staged save verification, and dirty-close protection.
- `PackageMemoService` and the package editor memo panel: asset-level missing/empty/value state, rich in-memory identity, explicit-save editing, and transactional Safe/Overwrite/Force package-to-package memo copy. The legacy schema has no package/directory/source memo field or parent-directory inheritance.

Only `UABS.UnityAssets` may directly depend on AssetsTools.NET. Avalonia Views and ViewModels must not own or expose AssetsTools.NET objects. See [current Avalonia architecture](../02_architecture/avalonia-architecture.md), [asset loading](../04_systems/asset-loading.md), and [package systems](../04_systems/package-search-dependency-memo.md).

## Coding and implementation principles

- Keep opened game/Unity files read-only and preserve original paths, logical file provenance, class IDs, and path IDs.
- Give streams, AssetsTools managers, decoded images, and other resources explicit ownership and disposal.
- Keep expensive parsing and decoding off the UI thread; marshal only UI-bound state updates to Avalonia.
- Prefer cancellation-aware `Task` APIs and structured errors over raw threads, `async void` workflows, logging-and-null, or arbitrary delays.
- Keep Views declarative and code-behind limited to UI composition or genuinely view-specific behavior.
- Use the smallest maintainable abstraction that protects the UI/infrastructure boundary; avoid speculative frameworks.
- Preserve existing interfaces, schema behavior, and verified algorithms unless the task requires change.
- Avoid unrelated cleanup, broad renames, dependency upgrades, or feature expansion.
- Do not claim support or completion without evidence proportional to the claim.

## Testing and verification

### Mandatory Debug startup builds and grid conventions

- Routine development and verification must build the actual startup application in Debug: from `Avalonia`, run `dotnet build UABS/UABS.csproj -c Debug`. A solution build with `-c Debug` that includes this project is also valid; building supporting libraries alone is insufficient.
- Before declaring UI changes ready for manual testing, confirm the successful Debug build contains those changes and identify `UABS/bin/Debug/net9.0/UABS.exe` as the launch target. Never substitute a successful Release build for failed Debug verification. If a running UABS instance locks Debug output, report the blocker and ask the user to close it normally; never terminate their process without explicit permission. Release builds are permitted when specifically requested, but do not replace Debug verification.
- Affected asset-oriented grids must follow the [mandatory shared grid conventions](../03_design/ui-and-workflow-design.md#mandatory-shared-grid-conventions): an existing Kind/icon column is leftmost, icon-only, blank-header, user-resizable with a 52 px default, 80 px maximum, and explicit per-column `MinWidth="0"` for full hiding. Preserve other columns' minimums and clip narrow icons. Restore a hidden Kind column using the next header's left resize edge; reuse equivalent icon mappings and shared green zebra resources without adding unrelated controls or palettes.
- Whenever creating or changing an affected grid, check applicable Kind configuration, shared zebra styling, selection/hover precedence, and parity after collection changes or virtualized reuse, then confirm the Debug startup build. Report compilation, automated style/layout checks, and actual visual observation separately. Existing implementation gaps are not exceptions to the standard; document them without expanding an unrelated task.

Documentation-only edits require documentation validation, not an application build or full regression suite.

### Risk-Based Verification and Token Efficiency

Classify work by its highest relevant risk and verify proportionately:

- **Micro-fix:** Small, localized, low-risk changes. Inspect only the necessary context, make the smallest edit, and run focused validation only. New tests, documentation updates, and full-suite runs are not mandatory unless the change exposes meaningful risk.
- **Small feature:** Scoped functionality or UI behavior. Run relevant tests and a build; add regression tests for meaningful behavioral risks.
- **Milestone / high-risk change:** Architecture, persistence, concurrency, asset loading, package operations, or major UI changes. Run the full regression suite and relevant audits, and update affected documentation.

Prefer concise command output and final reports. Reuse established, verified repository context instead of repeatedly exploring the entire codebase. Batch several micro-fixes when practical and perform one full verification before a major commit or milestone acceptance. Escalate verification when unexpected behavior, failures, or broader dependencies appear. Never skip necessary safety checks to save tokens, weaken or delete existing tests, or bypass the Git and desktop-control restrictions below.

When relevant context is not already established, inspect the solution, target frameworks, available SDKs, and affected project files before running commands. Prefer focused tests first and distinguish code regressions from environment restrictions or pre-existing failures.

For asset work, cover the relevant subset of:

- format detection and invalid input;
- real fixture loading and enumeration;
- identity/provenance preservation;
- replacement, cancellation, and disposal;
- missing optional metadata/classdata;
- repeated open/close and released file handles;
- malformed, version-specific, multi-file, or large-file cases when fixtures exist.

Never modify source fixtures. Record exact commands, results, limitations, and whether graphical behavior was manually verified.

## Documentation navigation and maintenance

The [documentation index](../README.md) is the navigation root:

- `01_core`: durable entry context and rules.
- `02_architecture`: structure, dependencies, and architectural decisions.
- `03_design`: user workflows and interaction design.
- `04_systems`: subsystem behavior and implementation details.
- `05_audit`: feature status, roadmap, risks, open questions, and milestone evidence.

Keep one source of truth for each concern. Put durable guidance here, current human context in `project-overview.md`, implementation structure in architecture/system documents, and time-specific evidence in audit reports. Use relative links and lowercase kebab-case filenames. Keep [codex-session-handoff.md](../05_audit/codex-session-handoff.md) as the concise, actionable continuation record for active implementation work; still keep durable rules here, roadmap state in the roadmap, and final evidence in milestone reports.

### Mermaid documentation guideline

Use fenced `mermaid` diagrams when architecture, dependencies, lifecycle, workflows, state, or migration mappings are materially clearer visually. Base diagrams on verified source and label proposed or legacy-only elements explicitly. Prefer several focused diagrams over one dense diagram, use meaningful names, keep syntax compatible with GitHub Mermaid, and update diagrams whenever the corresponding implementation changes. Do not add decorative diagrams or present planned components as implemented.

## Session Safety, Token Limits, and Recovery

Treat recoverability as a continuous implementation requirement, not an end-of-session cleanup step.

At the beginning of every coding session:

1. Read this guide and [project overview](project-overview.md).
2. Read the [session handoff](../05_audit/codex-session-handoff.md), current [roadmap](../05_audit/migration-roadmap.md), and newest milestone report.
3. Inspect Git status and the actual implementation before trusting documentation or resuming work. Never restart a partially completed milestone blindly.
4. Preserve every existing user and agent change. Never reset, clean, revert, overwrite, commit, amend, switch branches, or rewrite history without explicit authorization.

Divide substantial work into small, independently verifiable phases. After each meaningful phase, make the code internally consistent, run focused tests and a build when practical, and update [codex-session-handoff.md](../05_audit/codex-session-handoff.md). The handoff is the authoritative continuation record and must state the current milestone/task, completed and partial phases, files changed, latest build/tests, failures or blockers, decisions and assumptions, Git status, buildability, and the exact next step. Do not wait until the final response to record a major checkpoint.

Prefer checkpoints with a buildable solution, prior tests passing, no accidentally enabled partial feature, and no unknown temporary state. If a clean checkpoint is not possible, name every incomplete file, exact error, and recovery step. At milestone completion, update roadmap/report evidence and affected links or diagrams without duplicating the full progress narrative across documents.

Because interruptions can occur without warning, keep checkpoints frequent even when no limit is visible. If context, token, usage, or execution capacity appears constrained, stop starting features, finish the smallest active unit if feasible, leave source files syntactically complete, run the most relevant verification, and update the handoff with unfinished work. Do not claim an exact remaining budget unless the system exposes it, and never call a milestone complete merely because the session is ending.

If work must stop incomplete, the final response must contain **Safe Checkpoint — Continuation Required** with completed work, remaining tasks, latest build/test results, known issues, whether the app is safe to run, the handoff location, and the next exact action.

## User-Driven UI Verification and Computer-Control Restrictions

Desktop visual and interactive verification is user-driven by default. Do not automatically launch or use computer-control tools, move the pointer, type into native applications, open file/folder pickers, or capture desktop screenshots merely because a task requests verification or a smoke test. A general instruction to verify an implementation does not authorize desktop control, and approval from an earlier task or milestone never carries forward.

Computer control requires the user's explicit permission for each specific desktop-control task. Without that permission, use normal automated tests, builds, XAML compilation/validation, and noninteractive diagnostics, then provide a concise manual UI checklist for the user. Do not start a computer-use session solely to inspect appearance.

Reports must distinguish (1) automated verification, (2) behavior the user has manually verified and reported, and (3) behavior that remains unverified. Keep UI verification proportionate so it does not consume excessive context or jeopardize a recoverable checkpoint.

## Scope control

Treat requested analysis, diagnosis, implementation, and documentation work as different authorization levels. Do not turn a documentation task into source refactoring, or a diagnostic task into an implementation. Ask only when a decision would create a major architectural conflict, destructive change, compatibility break, or unavoidable product choice; otherwise follow the documented architecture and keep the change narrow.

### UI and workflow compatibility

**Existing functionality and UI behavior are protected by default.** For UI/workflow tasks, read the [V2 recovery roadmap](../05_audit/v2-ui-workflow-recovery-roadmap.md), inspect affected workflows and identify comparable workspaces before editing. Preserve unrequested controls, commands, columns, layouts, defaults and navigation; never disable a feature to simplify another or silently introduce extra columns/actions. Cross-workspace consistency does not authorize modifying every workspace, and the roadmap's proposed phases are not automatic implementation permission.

Keep changes small/reviewable, obtain direction before resolving behavior conflicts or broadening scope, test existing as well as new behavior, and report user-visible additions, removals, preserved and intentionally changed behavior. Visible UI changes require user-operated Debug acceptance separately from automated verification. Preserve all existing Git, desktop-control, safety and verification restrictions; V1 retirement and new bundle-memo/sort semantics require their own approved implementation decisions.
