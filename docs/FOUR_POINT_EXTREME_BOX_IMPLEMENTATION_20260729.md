# Four-Point Extreme Box Implementation

Date: 2026-07-29 KST

Status: Complete

## Operator Outcome

The existing Rectangle tool now exposes one Recipe-scoped `박스 입력` option:

- `2점 드래그` remains the default;
- `4점 극점` accepts `위 -> 아래 -> 왼쪽 -> 오른쪽`.

The four-point path shows the next edge and accepted-point count, draws
display-only point/edge guides, and creates one ordinary axis-aligned
Rectangle only after the fourth valid point. The completed object uses the
existing Object Review, move, resize, duplicate, delete, save, reopen,
Undo/Redo, and Smart Mask `RoiAdded` paths.

`Backspace` removes the latest pending point. Right-click or `Esc` cancels the
pending draft. Image, Recipe, dataset-purpose, active-class, drawing-method,
or non-Rectangle tool changes cancel it without creating an object or history
entry.

## Persisted Setup

`LabelingProjectSettings.BoxDrawingMethod` owns the Recipe preference:

- `TwoPointDrag`;
- `FourPointExtreme`.

Missing, invalid, or stale values normalize to `TwoPointDrag`. Restore updates
only visible/editable option state and input readiness; it does not select a
tool, create an ROI, run inference, confirm a candidate, save a label, or
change a layer. The reset button explicitly returns to `TwoPointDrag`.

## Geometry And Ownership

- `WpfFourPointBoxService` owns four semantic point roles, deterministic
  bounds, degenerate rejection, Backspace, and cancel state.
- `WpfCanvasPanelViewModel` owns method items, visibility, progress text,
  restore suppression, and reset.
- `WpfLabelingShellWindow.FourPointBox` adapts image clicks and lifecycle
  changes, persists the option, and hands one completed image Rectangle to the
  canvas.
- `RoiImageCanvasViewModel.AddCompletedImageRectangle` is the only new canvas
  API. It adds the same ordinary `CanvasRect<float>` and raises the existing
  `RoiAdded` event. The high-frequency Viewer/OpenGL mouse-move, ROI
  drag/resize, brush, and eraser paths are unchanged.

Only the top/bottom points' Y and left/right points' X define the result. The
unused coordinates are guidance only. Reversed extreme positions are
normalized with min/max, and the completed canvas Rectangle returns through
the existing `ConvertCanvasRectToImageBounds` integer/clipping path.

## Smart Mask And History

The first three points are draft overlays only. They produce no ROI, history,
save, or Smart Mask request. Point four raises one existing `RoiAdded` event:

- one ordinary Rectangle is created;
- one existing annotation-history snapshot is registered;
- one-step Undo removes it and one-step Redo restores it;
- when segmentation automatic contour is enabled, the existing
  `TryStartAutoSmartMaskForNewRoi` policy is reached exactly once.

Candidate review, correction, Confirm/Skip, and explicit label save remain
unchanged.

## Rotated-Input Safety

Four-point input does not introduce rotated geometry.

- Label Studio detection import skips non-zero `image_rotation` or rectangle
  `rotation`.
- CVAT detection import skips a box with a non-zero `rotation` attribute.
- The existing Dataset Interchange skipped-record policy exposes the count and
  blocks Apply.

No importer silently flattens rotated input to axis-aligned bounds.

## Verification And Evidence

Verification:

- required isolated test build: warning 0, error 0;
- `--four-point-extreme-box`: pass;
- `--wpf-undo-redo-shortcuts`: pass;
- `--wpf-roi-object-manipulation`: pass;
- `--labeling-productivity`: pass;
- `--mobile-sam-box-prompt`: pass;
- `--wpf-labeling-shell`: pass;
- `--label-studio-detection-export`: pass;
- `--label-studio-detection-import`: pass, including rotated-record blocking;
- `--cvat-image-export`: pass;
- `--cvat-detection-import`: pass, including rotated-box blocking;
- `--priority-workflow-docs`: pass;
- fresh current-build 1920x1080 and 1366x768 visual smoke: pass.

Evidence:

- `tests\LabelingApplication.Tests\Program.FourPointExtremeBox.cs`;
- `artifacts\four-point-extreme-box-20260729\before-rectangle-labeling-options.png`;
- `artifacts\four-point-extreme-box-20260729\after-four-point-progress-1920x1080.png`;
- `artifacts\four-point-extreme-box-20260729\closest-baseline-1366x768.png`;
- `artifacts\four-point-extreme-box-20260729\after-four-point-progress-1366x768.png`.

The 1920x1080 before image is a true pre-edit current-source capture. The
1366x768 closest baseline was captured after implementation with the new
option inactive; it is not represented as a true pre-edit image.

## Completion Record

Status: Complete

Scope: Recipe-scoped two-point/four-point box input choice, four semantic click
roles, pending guides/progress/cancel, one ordinary Rectangle handoff,
one-step history, existing Smart Mask timing, and rotated Label Studio/CVAT
import rejection.

Acceptance criteria: deterministic geometry, no mutation before point four,
one ROI/history step on completion, Undo/Redo, Recipe save/reload/default and
restore no-action behavior, current UI at both target sizes, and rotated-input
preflight blocking pass.

Verification: commands and artifacts listed above.

Evidence: this document, the focused test suite, import regression suites, and
the current-build visual artifacts.

Boundary / next dependency: this is axis-aligned Rectangle input, not free
quadrilateral or rotated-box support. Dataset Health class filtering is now
Complete under `docs/DATASET_HEALTH_CLASS_FILTER_20260729.md`; persistent
metadata requires a named consumer; cross-family z-order requires a reproduced
renderer defect; field model adoption requires independent
provenance-confirmed data.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`
