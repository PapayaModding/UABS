# CP21 G1 — Package sorting accessibility and fixture proposal

Status: **separately approved and implemented; Windows acceptance pending**.
The user approved this proposal at `571fdf2d3300e2bf4ce95ea87f6668683f6f610f`.
See the [implementation/evidence guide](PACKAGE-SORT-ACCEPTANCE.md). The original
investigation/design below records audited recovery commit
`7343b139c20795a4dbc66928663db10987dc97df`; historical absence/pending-approval
statements are superseded by the explicit approval, not new Windows evidence.

The user confirms [run 40338427998](https://github.com/PapayaModding/UABS/actions/runs/40338427998)
at that commit passed all four jobs, including G2/G3/G4. Those are real Windows
outcomes reported by the user, not a new artifact inspection or execution here.
The [manual checklist](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-search-defaults.md#limits-and-manual-acceptance)
and [final gap audit](../../Avalonia/docs/05_audit/v2-recovery-checkpoint-02-1-final-acceptance-gap-audit.md)
remain authoritative. G1, G5, G6 and Linux regression disposition remain open.

## Current implementation and accessible action

- `SearchWorkspaceView.axaml` exposes a Package column with
  `SortMemberPath="PackageName"`. `GlobalSort` maps this to `PackageName`;
  `OnResultsSorting` handles V2 sorting to prevent local retained-window sorting.
  `BeginHeaderSort` / `SortHeaderAsync` retain selection/version ownership and
  update header indicators. V1 keeps its native DataGrid route.
- `SearchWorkspaceViewModel.SortV2Async` already permits Package sorting for
  bundle results. It toggles only the requested field's primary direction,
  re-admits the **confirmed** criteria/revisions, cancels superseded work and
  retains the established stale/session/selection/draft safeguards.
- `SearchNameSortButton` is a standard accessible Button, but its handler resolves
  the Name column and sorts `SourceName`. It is not a general sort selector.
  Package header HelpText reports state; neither it nor setting focus is an action.

Pinned Avalonia DataGrid 11.3.3's
[header peer](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/Automation/Peers/DataGridColumnHeaderAutomationPeer.cs)
exposes HeaderItem without Invoke or SelectionItem. The
[header](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/DataGridColumnHeader.cs)
defaults IsTabStop to false and routes pointer sorting through internal
InvokeProcessSort/ProcessSort; it has no keyboard sort handler. The
[grid's key dispatcher](https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/11.3.3/src/Avalonia.Controls.DataGrid/DataGrid.cs)
contains navigation/edit/copy shortcuts, not a sort shortcut.

The new `PackageHeaderCurrentlyHasNoInvokeProvider_AndNameButtonIsNotAPackageAction`
headless regression inspects the real rendered UABS header peer, sort eligibility,
help and Name Button. This is source/headless capability evidence, **not a new
Windows Package interaction**. No supported coordinate-free Package action was
established. Do not invent Enter/F3, invoke an unrelated descendant, use internal
reflection, change a ViewModel from FlaUI, or reinterpret Name sorting as Package
sorting. Current automated Package acceptance stays **BLOCKED**.

## Smallest production approval scope

Recommend one standard `SearchPackageSortButton`, accessible Name **Sort Package**,
beside the existing Name/Refresh actions in the existing wrapping results footer.
Visible only for V2 bundle results and enabled only when the equivalent Package
header can sort (`IsBundleSearch`, `CanChangeV2Sort`, visible/sortable column).
Its ordinary tab/Enter/Space and UIA Invoke route resolves the actual Package
column through `GlobalSort`, then calls the same `BeginHeaderSort` lifecycle.
HelpText discloses current/next Package order and busy state using the existing
`NextV2SortDirection(PackageName)` calculation, not a second toggle implementation.

Preserve Name Button behavior, Package pointer headers, V1/raw asset-mode UI,
exactly two bundle columns, global comparator, criteria/revision vector, queued
sort ownership, Inspector/draft retention and explicit Refresh policy. Review
the existing footer at minimum width and maximized hosted layout; an extra tab
stop/button and possible extra wrapped line are the visible tradeoffs. No grid
redesign, extra result columns, schema changes or dependency fork.

Alternatives: recorded human Package-header clicks need no product change but
do not meet fully remote automation. An upstream accessible header peer is a
larger dependency change; internal sort methods are not a supported UABS API.
An undiscoverable shortcut or repurposed Name button is not recommended.

**Separate approval has now been received for this exact slice.** Further changes
need their own authorization. No Package test should pass without performing
its actual supported user action.

## Minimal deterministic fixture and independent oracle

Existing preparation builds **one** SQLite package named Smoke from all three
published bundles. Multiple Sources do not create multiple Package values; that
fixture cannot prove primary Package direction. Do not rename/copy its database
and pretend a new package identity exists.

Propose two fresh independent PackageBuilder builds using the same verified
published bundles/real AssetsTools extraction, **three rows total** (the minimum
for two Package values plus a same-Package tie):

| Package label | Original published bundle inputs | Query |
| --- | --- | --- |
| Alpha | `uabs-smoke-0002.bundle`, `uabs-smoke-0001.bundle` (reverse input order) | Assets fields, Sources only, `asset` |
| Zeta | `uabs-smoke-0000.bundle` | Same |

These are proposed package labels/assignments, **not an already built fixture or
observed result**. Existing Smoke preparation independently verifies that `asset`
has raw indexed-name witnesses in each of those three bundles. Future preparation
must repeat that verification separately in each new DB, validate actual package
labels, source paths/kind/complete coverage and zero issues, require distinct
package/generation/source identities, and record all original bundle hashes,
canonical DB paths and closed/checkpointed DB hashes before UI admission.
No generated bundles, manifest edits or dataset publication are required.

`SearchQuery.Text` uses OrdinalIgnoreCase then Ordinal text ordering, not current
culture. `SearchQuery.Compare` and SQL keyset ORDER BY reverse **only the primary
field** for Descending. Tie-breakers stay ascending: PackageName, PackagePath,
IndexDirectory, SourcePath, AssetName, PathId, Kind and canonical row key.
For bundle rows AssetName/PathId are NULL and Kind is the source kind. UUIDs
are a final canonical textual-key fallback, not Guid.CompareTo ordering.

Within Alpha, package path/index directory are constant and source paths share
the original bundle directory; `...0001.bundle` precedes `...0002.bundle`.
Validate those path/uniqueness preconditions from raw metadata, not assumptions
about input order. The proposed independent exact **(Name, Package)** sequences:

| Direction | Expected visible sequence |
| --- | --- |
| Ascending | `(0001, Alpha)`, `(0002, Alpha)`, `(0000, Zeta)` |
| Descending | `(0000, Zeta)`, `(0001, Alpha)`, `(0002, Alpha)` |

Here `000N` abbreviates the full `uabs-smoke-000N.bundle` name; persisted oracle
records must use full strings. Descending is deliberately **not** a full reversal
of ascending: the Alpha tie remains ascending. Name-primary order also differs
from the same-direction Package order. Cross-direction sequences can coincide
(Name ascending equals Package descending in this minimal fixture), so verifying
the actual Package control and active field/direction is mandatory; row shape
alone must never identify which sort action occurred. Both Package directions
and tie ordering must be independently asserted.

Future preparation must hard-code these independent pairs, cross-check raw asset
witnesses/source metadata and derive identity/path/hash evidence without calling
Search, its comparer, or learning expected values from UI order. Block if path
ties would reach an unmodelled UUID fallback. Duplicate Package labels across
different databases or case/culture edge cases are optional extra coverage, not
needed for this minimal same-Package source tie.

## Future acceptance implementation after approval

1. Add an isolated optional Package-sort scenario/job; preserve all four passing
   jobs, their single-package guards and fixture/oracle preparation. Admit exactly
   the two allowlisted SHA-verified DB copies through real native picker controls.
   Require their exact scope paths/hashes and selected state, not display names.
2. Capture the confirmed `asset` query, criteria/flags and complete three-row set.
   Use the existing owned-window/maximize and validated visible row geometry:
   paired Name/Package cell identities, finite positive nonoverlapping rectangles,
   below-header viewport and all rows fully realized. Never use UIA enumeration,
   OCR or rectangle coordinates as input. Ambiguous/clipped evidence is BLOCKED.
3. Select a known boundary row through supported owned/focused keyboard input.
   Check Inspector identity against that row's independently prepared package/
   generation/source/revision UUIDs and exact DB/source paths. Re-admission can
   change ephemeral registration IDs; do not require whole Inspector strings to
   be identical across valid sorts or weaken canonical identity checks instead.
4. Activate the actual Package Button through verified Invoke and focused keyboard
   input, read current/next/completed state, and compare **both exact visible
   sequences** above. Preserve complete count/multiset and tied-row order after
   each action. Add regressions rejecting whole-array reversal, Name-primary order,
   reversed tie order, wrong package/row, missing rows and ambiguous geometry.
5. Verify unchanged scope, live criteria, selected identity and no implicit use of
   newly edited/unexecuted criteria. Sorting intentionally re-admits the confirmed
   query: do **not** demand zero Search sessions as in retention-only G2. Assert
   no unrequested explicit Refresh/filter reset and retain stale/revision guards.
   Reuse existing header/sort/draft regressions rather than rewriting semantics.
6. Upload exact SHAs, manifest/DB hashes, scope/admission diagnostics, sort state,
   before/after visible-order and Inspector identity reports, screenshots and
   per-case PASS/FAIL/BLOCKED even on failure. A readable wrong order/set is FAIL;
   missing action/identity/complete visual evidence is BLOCKED. Compilation alone
   cannot accept G1. A **new Windows run is required after implementation**.

G1 is not resolved by this proposal, by the headless capability regression, or by
rerunning the unchanged four jobs (which contain no Package-sort acceptance).
G5 indeterminate-coverage feedback, G6 tooltip equivalence and Linux full-suite
regression disposition remain open. Checkpoint 2.1 stays open; no Checkpoint 3.

## Validation of this investigation

- 65 focused application/headless tests PASS, including the new real Package-header
  capability check and existing Name/Package header, selection, criteria and memo
  regressions. The new audit test records the current missing action, not a
  permanent requirement that Package accessibility remain absent; update it when
  a production change is separately approved.
- Existing runner contract regressions PASS. Release Windows runner cross-build
  passes with zero warnings/errors. Workflow actionlint and documentation link/
  anchor/fence/whitespace checks PASS. Production, runner actions, fixture
  preparation and all four workflow jobs are unchanged.
- Initial sandboxed MSBuild execution failed before tests because local named-pipe
  IPC was denied. The focused tests succeeded with local IPC permission; single-node
  cross-build succeeded without changing project/runtime settings. This is not a
  product failure or a fresh full-Linux-suite pass. Prior Linux disposition remains.
- No new Windows run was dispatched: the Package action is unavailable and no
  Package scenario was implemented. New Windows validation is necessary only
  after the approved action/fixture/scenario are implemented, or for independently
  recorded manual Package-header evidence.
