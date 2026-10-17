# AI Agent Guide

## Required first reading

Before significant implementation or architectural work, read this document and [project-overview.md](project-overview.md). Then inspect the [session handoff](../05_audit/codex-session-handoff.md), [migration roadmap](../05_audit/migration-roadmap.md), the latest [milestone report](../05_audit/milestone-07-report.md), and the subsystem or architecture documents relevant to the task.

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

Before running commands, inspect the solution, target frameworks, available SDKs, and relevant project files. Prefer focused tests first, then a solution build. Distinguish code regressions from environment restrictions or pre-existing failures.

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

## Scope control

Treat requested analysis, diagnosis, implementation, and documentation work as different authorization levels. Do not turn a documentation task into source refactoring, or a diagnostic task into an implementation. Ask only when a decision would create a major architectural conflict, destructive change, compatibility break, or unavoidable product choice; otherwise follow the documented architecture and keep the change narrow.
