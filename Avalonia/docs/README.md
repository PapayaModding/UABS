# UABS Documentation

This documentation is the durable source of project context for the Unity-to-Avalonia migration. Start with the two core documents, then follow the area-specific links below.

## Start here

- [AI agent guide](01_core/ai-agent.md) — mandatory repository rules, implementation practices, verification expectations, and session-continuity guidance.
- [Project overview](01_core/project-overview.md) — human-readable purpose, current status, feature scope, and milestones.

## Documentation areas

### 01 Core

Primary orientation shared by developers and coding agents. This folder intentionally contains exactly two documents:

- [AI agent guide](01_core/ai-agent.md)
- [Project overview](01_core/project-overview.md)

### 02 Architecture

How the legacy and replacement applications are structured, including implemented and proposed boundaries:

- [Legacy Unity architecture](02_architecture/unity-architecture.md)
- [Current Avalonia architecture](02_architecture/avalonia-architecture.md)
- [Target Avalonia architecture](02_architecture/target-architecture.md)
- [Dependency management](02_architecture/dependency-management.md)
- [Unity dependency boundary](02_architecture/unity-dependency-boundary.md)

### 03 Design

Interaction and workflow specifications, with current behavior separated from proposals:

- [UI and workflow design](03_design/ui-and-workflow-design.md)
- [UX redesign foundation and design audit](03_design/ux-redesign-foundation.md) — proposed four-workspace shell, shared inspector, behavior matrix, wireframes, and incremental sequence.

### 04 Systems

Focused subsystem documentation:

- [Asset loading and enumeration](04_systems/asset-loading.md)
- [Filesystem browser and asset list](04_systems/filesystem-browser.md)
- [Texture and sprite processing](04_systems/texture-and-sprite-processing.md)
- [Package, search, dependency, and memo systems](04_systems/package-search-dependency-memo.md)

### 05 Audit

Evidence, migration tracking, risks, decisions, and implementation history:

- [Migration roadmap](05_audit/migration-roadmap.md)
- [Milestone 01 report](05_audit/milestone-01-report.md)
- [Milestone 02 report](05_audit/milestone-02-report.md)
- [Milestone 03 report](05_audit/milestone-03-report.md)
- [Milestone 04 report](05_audit/milestone-04-report.md)
- [Milestone 05 report](05_audit/milestone-05-report.md)
- [Milestone 06 report](05_audit/milestone-06-report.md)
- [Milestone 07 report](05_audit/milestone-07-report.md)
- [Milestone 7.4 stability report](05_audit/milestone-07-4-stability-report.md)
- [Milestone 7.5 compatibility report](05_audit/milestone-07-5-compatibility-report.md)
- [Milestone 08 memo report](05_audit/milestone-08-report.md)
- [Legacy package format compatibility audit](05_audit/legacy-package-format-compatibility.md)
- [Package Format V2 architecture audit and storage benchmark](05_audit/package-format-v2-architecture-audit.md) — proposed clean-break indexing/storage design; no production migration.
- [Current Codex session handoff](05_audit/codex-session-handoff.md)
- [Feature comparison](05_audit/feature-comparison.md)
- [Legacy feature inventory](05_audit/legacy-feature-inventory.md)
- [Reusable-code analysis](05_audit/reusable-code.md)
- [Migration risks](05_audit/migration-risks.md)
- [Unity-exclusive features and replacement decisions](05_audit/unity-exclusive-features.md)
- [Open questions](05_audit/open-questions.md)

## Status language

- **Implemented** means source exists in the Avalonia repository.
- **Verified** means the stated build, automated test, or manual check was actually run.
- **Proposed** means a target design, not current behavior.
- **Legacy observed** means a source path was traced in the Unity reference project but may not have been freshly executed.

Use relative links and lowercase kebab-case filenames. Keep milestone evidence in `05_audit` and update architecture or system documents when implementation changes their claims.
