# Segmentation Remove-Underlying P1-C

Date: 2026-07-28
Status: `Complete`
Field validation: `Not evaluated`

## Outcome

Saved Labels now provides a destructive structural edit as an explicit
two-step workflow:

1. `겹침 분석` computes the affected underlying objects, removed pixels, and
   fully removed objects without changing labels or history.
2. `확인 후 제거` applies that exact analysis as one undoable change.

`취소` clears the preview without mutation. A geometry, class, identity,
z-order, image, tool, or selection change invalidates the preview; Apply then
rejects the stale plan and requires a new analysis.

## Contract

- The selected manual segmentation object is the top/reference object.
- Only objects earlier than it in the stable `(ZOrder, current index)` stack
  are underlying candidates. Overlapping objects in front remain untouched.
- Polygon, polygon-with-cutout, and raster sources are converted through
  `WpfSegmentationMaskGeometryService` before overlap is calculated.
- A partially covered object becomes an exact raster remainder. It preserves
  object ID, class, class item, z-order, and receives component `-1` plus
  `LastStructuralOperation=RemoveUnderlying`.
- A fully covered underlying object is removed.
- The selected object and all unaffected objects retain their existing
  instances and metadata.
- One successful Apply creates exactly one full annotation-history step.
- Preview/cancel/stale rejection create no history and do not mark labels
  dirty.
- Canonical segmentation JSON v3 plus the sibling mask PNG preserve the raster
  remainder, object ID, z-order, and operation provenance through
  save/load/re-save.

The analysis signature covers image size, selected index, object order,
z-order, object ID, class, operation provenance, and every rasterized pixel.
This prevents applying an obsolete destructive plan.

## Ownership

- `WpfSegmentationRemoveUnderlyingService`: raster overlap analysis,
  subtraction, stable-stack boundary, replacement geometry, and stale
  signature.
- `WpfObjectReviewPanelViewModel`: command enablement, pending confirmation,
  and operator guidance.
- `WpfLabelingShellWindow.SegmentationRemoveUnderlyingCommands`: flush,
  preview/apply/cancel, history, mutation, selection, dirty state, and status.
- `WpfLabelingShellWindow.AnnotationPolygonOverlays` and
  `AnnotationMaskOverlays`: orange `REMOVE PREVIEW` presentation only.
- `WpfObjectReviewPanel`: accessible `겹침 분석`, `확인 후 제거`, and `취소`
  controls.

Viewer/OpenGL rendering, ROI, brush, eraser, and mask-drag hot paths were not
redesigned.

## Verification

Focused gate:

```powershell
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-remove-underlying
```

The gate proves:

- lower-stack-only impact analysis;
- partial subtraction and full-object removal;
- analysis/cancel no-mutation;
- orange affected-polygon preview;
- stale-plan rejection;
- selected/front object preservation;
- one-step Undo/Redo;
- canonical v3 save/load/re-save of ID, z-order, raster remainder, and
  `RemoveUnderlying` provenance;
- no-overlap rejection.

Protected regressions passed for z-order, hole, split, merge, interchange,
storage, undo/redo, segmentation object manipulation, mask drag performance,
Object Review, WPF shell, MVVM, labeling productivity, and MobileSAM box
prompt. The isolated test build completed with zero warnings and zero errors.

Current-source UI evidence:

- before:
  `artifacts\ui\segmentation-remove-underlying-p1c-20260728\before-current-build-1920x1080.png`
- after:
  `artifacts\ui\segmentation-remove-underlying-p1c-20260728\after-current-build-1920x1080.png`

The after fixture shows an active confirmation state with one affected object,
195 removed pixels, zero full removals, and the orange warning summary.

## Evaluation and Boundary

This completes the planned P1-C mask-structure command set: merge, split,
enclosed hole edit, saved-object z-order, and remove-underlying. The
labeling-editor depth estimate moves from `2.9/5` to `3.0/5`; focused local
workstation maturity remains `4.0/5`.

The result does not establish CVAT/V7 parity. Exact polygon/raster cross-pass
interleaving remains a renderer limitation, and video propagation,
collaboration, accounts/cloud, reviewer assignment, and field accuracy remain
unimplemented or intentionally out of scope.

## Subsequent Priority Status

P2 session-only hide/lock/pin was completed after this slice without changing
canonical label or export semantics. See
`docs\OBJECT_SESSION_STATE_P2_20260728.md`.

The current next priority is polygon vertex insert/delete as an independently
acceptance-gated precision slice.

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

## Completion Record

Status: Complete

Scope: Two-step remove-underlying analysis/confirmation for saved polygon and
raster segmentation objects, including exact remainder geometry, stale guard,
history, v3 persistence, accessible UI, and current-source visual evidence.

Acceptance criteria: analysis is read-only; cancel/stale paths do not mutate;
only lower overlapping objects change; selected/front/unaffected objects
remain; partial/full coverage semantics are correct; Apply is one Undo/Redo;
v3 round-trip and orange preview pass.

Verification: isolated build plus focused and protected gates listed above;
1920x1080 before/after visual smoke.

Evidence: this document,
`tests\LabelingApplication.Tests\Program.SegmentationRemoveUnderlying.cs`,
and the UI artifact folder above.

Boundary / next dependency: exact polygon/raster renderer interleaving is not
included. P2 session-only hide/lock/pin is complete; polygon vertex
insert/delete is the next independent precision-geometry gate.
