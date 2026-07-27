# Segmentation Split / Slice P1-C

Date: 2026-07-27
Status: `Complete`
Field validation: `Not evaluated`

## Outcome

Saved Labels now supports one explicit, axis-aligned structural split:

1. Select one saved polygon or raster-mask object.
2. Press **세로 절단** or **가로 절단**.
3. Click one position inside the selected object on the canvas.
4. The application removes the corresponding one-pixel column or row.
5. The edit is committed only when the remaining pixels form at least two
   4-connected components.

This is a bounded image-label editor command. It does not implement freehand
slice, manual hole creation, z-order editing, or remove-underlying.

## Operator Contract

- Only one manual segmentation object can be the split source.
- Starting a split switches to Select input and exposes a clear canvas-click
  instruction in Saved Labels and the command status.
- Right-click or the visible **취소** button cancels location selection.
- A click outside the object's interior or a line that does not create at
  least two components leaves the source object and annotation history
  unchanged. The operator can click another position or cancel.
- An active Smart Mask session must be confirmed or cancelled before split
  begins. This prevents two canvas point-input owners from competing.
- Changing the selected object, image, or annotation tool cancels a pending
  split.

## Geometry And Identity Contract

- `WpfSegmentationMaskGeometryService` converts polygon and raster sources to
  the same full-image binary-mask representation.
- Polygon cutouts are erased during rasterization and remain empty after split.
- `WpfSegmentationSplitService` removes one axis-aligned pixel row/column and
  extracts deterministic 4-connected components.
- Every output is an independent raster object with:
  - a new image-local object ID;
  - component index `-1`;
  - the source class and z-order;
  - `LastStructuralOperation=Split`.
- The source object is not mutated by geometry validation.
- Replacing the source with all results is one full annotation-history step.
  Undo restores the original geometry and identity; redo restores the
  generated split identities.
- Canonical segment JSON v3 save, reopen, and re-save retain both independent
  object IDs and `Split` provenance.

## Ownership

- Shared polygon/raster conversion:
  `WpfSegmentationMaskGeometryService`.
- Cut validation and component extraction:
  `WpfSegmentationSplitService`.
- Command enablement, pending status, and operator guidance:
  `WpfObjectReviewPanelViewModel`.
- Canvas point-input, source replacement, history, dirty state, and selection:
  `WpfLabelingShellWindow.SegmentationSplitCommands`.
- Existing canonical persistence and history services remain the owners of
  storage and undo/redo. Viewer/OpenGL, ROI, brush, eraser, and mask-drag hot
  paths were not rewritten.

## Focused Evidence

`--segmentation-split` covers:

- polygon source with a cutout;
- raster source;
- vertical and horizontal cuts;
- rejected edge cut and unchanged source pixels;
- new independent object IDs, preserved class/z-order, and `Split`
  provenance;
- one-step undo and redo;
- canonical v3 save, reopen, and re-save.

UI evidence:

- before:
  `artifacts\ui\segmentation-split-p1c-20260727\before-current-source-1920x1080.png`
- after, with a saved mask selected and vertical location input armed:
  `artifacts\ui\segmentation-split-p1c-20260727\after-current-source-1920x1080.png`

Verification commands:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-split
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-merge
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-interchange-contract
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-annotation-storage
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-undo-redo-shortcuts
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-segmentation-object-verification
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-mask-performance
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --priority-workflow-docs
git diff --check
```

## Durable Closure

Status: Complete

Scope: one selected polygon/raster object, one vertical/horizontal one-pixel
cut, component publication, cancellation, history, and canonical v3
round-trip.

Acceptance criteria:

- valid polygon/raster cut produces two or more independent components: pass;
- invalid cut produces no mutation or history entry: pass;
- class/z-order and polygon cutout semantics are retained: pass;
- new IDs and `Split` provenance survive undo/redo and v3 re-save: pass;
- current-source Object Review input state is visible at 1920x1080: pass.

Verification: isolated/app Debug builds, focused split/merge/interchange/storage/
history/segmentation/mask-performance regressions, documentation gate, UI
capture, and `git diff --check`.

Evidence: this document, `Program.SegmentationSplit.cs`, and the UI artifact
folder above.

Boundary / next dependency: manual hole editing, z-order, and
remove-underlying remain independent P1-C slices. Remove-underlying requires an
affected-object preview/warning before mutation. This closure does not claim
CVAT/V7 parity, video propagation, collaboration, or field accuracy.
