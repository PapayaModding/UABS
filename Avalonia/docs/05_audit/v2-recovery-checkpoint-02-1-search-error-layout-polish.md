# Checkpoint 2.1 — Search Results error layout polish

2026-10-26. User-approved presentation-only follow-up on `v2-recovery`.
Baseline: `2ddb7f4d797f22921e3b86064fbc402169f6b034`.
The user reports human verification that corrective error and technical tooltip
appear, followed by approval to reduce header crowding. That is not automatic
G6/checkpoint closure or a new screenshot record in this session.

## Correction and scope

The old error Border shared the DataGrid's star-sized row, allowing it to cover
retained results. The header also gave count text an Auto column competing with
the heading, status and always-expanded indexing warnings.

`SearchWorkspaceView.axaml` now uses distinct rows for:

1. Persistent Search Results heading and existing progress indicator.
2. Corrective error region, not an overlay. Long corrective text scrolls in a
   bounded96px viewport, leaving the status and applicable retained-results notice
   outside that scroll area. The existing full ErrorDetails tooltip binding and
   SearchErrorText identity are unchanged.
3. Confirmed-results information after a Search has actually executed: unchanged
   CountSummary and status, plus a standard initially collapsed Indexing warnings
   Expander. Full warning text is retained without truncation; the120px bounded
   information scroller allows all expanded content to be read without consuming
   the grid. Status is not repeated here when the error region or empty-state
   message already presents it.
4. Existing DataGrid and unchanged Inspector/selection bindings.
5. Existing Refresh/Name/Package/retry controls and actual stale-snapshot indicator.

For retained rows after rejection, a notice identifies them as previous results
stale **for the current Search** only when HasError, HasResults and CriteriaChanged
apply. This is presentation, not a change to IsStale, frozen criteria, count values,
session lifecycle or backend eligibility. Restoring confirmed criteria hides that
notice; a successful supported query clears the corrective error and full obsolete
diagnostic through existing behavior. Results, selection, Inspector, scopes,
sorting, schema and backend code are not changed.

## Validation and remaining risk

Two headless layout cases at900x560 and1300x700 exercise real acknowledged-partial
fixtures and real Search rejection/recovery. They verify:

- Heading visibility and hidden irrelevant information before a confirmed query,
  including first-query rejection; confirmed zero-result counts remain available.
- Non-overlapping heading/error/info/grid bounds, readable heading width, a usable
  DataGrid body, and no error overlay with collapsed/expanded warnings.
- Exact row collection, selected item/Inspector, entered/confirmed criteria and
  count retention; IsStale is not changed to satisfy a presentation label.
- Full actual warning/tooltip binding and clearing. Presentation-only stress text
  (150 warning lines and a very long corrective message) tests bounded scrolling;
  it is not fabricated extraction evidence or a backend diagnostic modification.

Focused Search/ViewModel/integration/bundle/header/paging-window/memo/accessibility/
G5 tests: **178/178 passed**, including the two new cases. Debug solution and
Release Windows runner cross-builds passed with zero warnings/errors. Existing
runner contracts, launcher regressions and workflow actionlint passed. No existing
assertions were weakened, no FlaUI scenario/workflow changed, no fixture published.

This Linux headless geometry verification is not a real Windows screenshot or
hover/layout acceptance. Review the updated Debug window at normal/small sizes,
high DPI/font scaling, long diagnostics and expanded warnings. Scrollbars are
intentional for long content; native tooltip rendering remains the existing
implementation. The existing six Windows scenarios may be rerun unchanged; no
cosmetic-only scenario was added. Historical full Linux suite failures/native
crash remain as documented in the [readiness audit](v2-recovery-checkpoint-02-1-closure-readiness-audit.md).
G6 and Checkpoint 2.1 remain open; no Checkpoint 3 work.
