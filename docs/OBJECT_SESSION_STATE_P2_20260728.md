# Object Session State P2

Date: 2026-07-28

Status: `Complete`

Field validation: `Not evaluated`

## Correction Notice

The first implementation incorrectly described `Pin` as a gold review
bookmark. Re-checking the supplied CVAT bounding-box video and subtitle proved
that CVAT pin means movement-only locking: position cannot move, while resize,
copy, and delete remain allowed.

This document supersedes the earlier gold-bookmark description. The corrected
UI and behavior have now completed all current verification gates. See
`docs\OBJECT_REVIEW_CONTEXTUAL_UI_CORRECTION_20260728.md`.

## State Contract

- `숨김`: keeps the Object Review row selectable while removing the object
  from the canvas and structural/canvas editing.
- `잠금`: blocks class, delete, duplicate, geometry, brush/eraser, and
  structural mutations.
- `이동 고정`: blocks only whole-object translation. ROI resize, polygon
  vertex edit, class, copy, delete, and structural operations remain allowed.

The three states are independent current-image presentation/session states.
They do not change label semantics.

## Lifetime and Persistence

- Supported targets are saved manual ROI and saved manual polygon/raster
  segmentation objects.
- Pending or confirmed AI candidates do not expose these states.
- State clears when the active image or queue session changes.
- Stable segmentation object IDs retain state across in-session history
  clones. ROI state indices shift after a deletion.
- State toggles create no annotation-history entry and do not mark labels
  dirty.
- State is absent from canonical JSON, label files, Recipe data, interchange
  exports, and training input.

## Interaction Matrix

| Operation | Hidden | Locked | Movement pin |
| --- | --- | --- | --- |
| Row selection | Allowed | Allowed | Allowed |
| Canvas display | Excluded | Ordinary class display | Ordinary class display |
| Class/delete | Allowed after show where visibility is required | Blocked | Allowed |
| Copy/duplicate | Blocked until shown | Blocked | Allowed |
| ROI whole movement | Blocked | Blocked | Blocked |
| ROI resize | Blocked | Blocked | Allowed |
| Polygon/mask whole movement | Blocked | Blocked | Blocked |
| Polygon vertex movement | Blocked | Blocked | Allowed |
| Structural commands | Excluded | Blocked | Allowed |
| Save/export/training meaning | No change | No change | No change |

## Ownership

- `WpfObjectSessionStateService`: session state, segment identity, ROI index
  shifting, and reset.
- `WpfObjectReviewPanelViewModel`: visible state, contextual editor
  visibility, and command enablement.
- `WpfLabelingShellWindow.ObjectSessionStateCommands`: toggle/redraw bridge and
  direct mutation guards.
- `CanvasOverlayItem.IsControlLock`: full ROI interaction lock.
- `CanvasOverlayItem.IsMoveLock` plus `RoiImageCanvasViewModel`: ROI
  translation-only lock.
- `WpfLabelingShellWindow.AnnotationSegmentEdit`: mask/polygon translation
  guard that preserves polygon vertex editing.

## Current Verification

Passed:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --object-session-state
```

The focused gate proves:

- service identity, lifetime, and ROI index shifting;
- compact controls and the segment-only collapsed contextual editor;
- hidden overlay removal/restoration;
- no gold or `PINNED` overlay presentation;
- pinned polygon whole-move rejection and vertex-edit acceptance;
- pinned ROI move lock without full control lock;
- locked mutation rejection and mask eraser exclusion;
- no state-only history or dirty-state change.

Also passed:

- protected structural/storage/history/ROI/segmentation/performance/UI
  regressions;
- zero-warning/error application build;
- fresh current-build 1920x1080 and 1366x768 before/after comparison;
- `--priority-workflow-docs` and `git diff --check`.

## Evaluation and Boundary

Labeling-editor depth is restored to `3.1/5`. Focused local-workstation
maturity remains `4.0/5`.

This state slice does not add persistent tags/groups/occlusion, assignments,
comments, reviewer history, cloud synchronization, video propagation, or
server permissions.

## Next Priority

After this correction passes all gates, implement polygon vertex
insert/delete as a separate precision-geometry slice.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`

## Completion Record

Status: Complete

Scope: Correct current-image hide/full-lock/movement-pin semantics and
commercial-style contextual state exposure.

Acceptance criteria: focused semantics, protected regressions, and fresh
1920/1366 visual evidence pass.

Verification: zero-warning/error isolated/application builds, focused and
protected gates, and current-build visual comparison.

Evidence: this document,
`docs\OBJECT_REVIEW_CONTEXTUAL_UI_CORRECTION_20260728.md`, and
`artifacts\ui\object-review-contextual-20260728`.

Boundary / next dependency: polygon vertex insert/delete is the next bounded
slice.
