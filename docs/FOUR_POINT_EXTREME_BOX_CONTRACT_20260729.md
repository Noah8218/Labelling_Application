# Four-Point Extreme Box Product Contract

Date: 2026-07-29 KST

Status: Complete

Completion type: product, geometry, persistence, editing, and interchange
contract only. Product implementation is not included in this record.

## 1. Decision

`4점 극점` is an alternative input method for the existing axis-aligned
Rectangle:

1. click the object's top extreme;
2. click the bottom extreme;
3. click the left extreme;
4. click the right extreme.

The four input points produce one ordinary axis-aligned box. They do not create
a free quadrilateral and do not create or imply a rotated rectangle.

The existing `2점 드래그` box remains the default. Both methods produce the
same canonical object, save format, editor behavior, training label, and
export result.

## 2. Evidence And Correction Of The Earlier Assumption

The supplied CVAT source separates three concepts:

- its box dialog offers `By 2 Points` and `By 4 Points`;
- the narration at 01:22-01:45 defines the four points as top, bottom, left,
  and right object extremes, where each point represents one rectangle edge;
- rotation is demonstrated later as a separate edit using the white handle
  above an already completed Rectangle.

Checked sources:

- user-provided `04_CVAT_Bounding_Box_Overview.en-orig.vtt`;
- user-provided `04_CVAT_Bounding_Box_Overview.mp4`;
- `artifacts/four-point-box-contract-20260729/cvat-frames/extreme-points.png`;
- `artifacts/four-point-box-contract-20260729/cvat-frames/four-point-complete.png`;
- `artifacts/four-point-box-contract-20260729/cvat-frames/rotated-result.png`.

Therefore, the earlier backlog wording that treated axis-aligned, rotated, and
quadrilateral meanings as equally plausible is resolved. The selected product
meaning is axis-aligned extreme-point construction.

## 3. Why The Other Meanings Are Rejected

### Free quadrilateral

A free quadrilateral is polygon geometry. Calling it a box would mix object
detection and segmentation semantics, and current YOLO detection labels cannot
preserve its four independent corners. Operators who need an arbitrary
four-corner outline must use the existing Polygon tool.

### Rotated rectangle

The current detection source of truth is `System.Drawing.Rectangle` and YOLO
center-x/center-y/width/height. COCO and Pascal VOC detection exporters also
emit axis-aligned bounds. Current Label Studio and CVAT detection import paths
do not preserve non-zero rotation.

Adding rotation would therefore require a new canonical geometry type, editor
handles, model-task support, import/export loss policy, and migration contract.
It is not part of four-point input and must not be introduced implicitly.

## 4. Geometry Contract

Input points use the active image's top-left-origin pixel coordinate system:

- `pTop`;
- `pBottom`;
- `pLeft`;
- `pRight`.

The semantic floating-point bounds are:

```text
left   = min(pLeft.X, pRight.X)
right  = max(pLeft.X, pRight.X)
top    = min(pTop.Y, pBottom.Y)
bottom = max(pTop.Y, pBottom.Y)
```

The unused coordinate of each extreme point is guidance only and is not
stored. For example, only `pTop.Y` defines the top edge.

Every accepted point must be inside the active image. The final box must have
positive width and height after clipping. A degenerate fourth point does not
create an object or history entry; it is removed and the operator is asked to
click that edge again.

The implementation must create a normal `CanvasRect<float>` and reuse the
existing `ConvertCanvasRectToImageBounds` path for clipping and integer
rounding. It must not create a second pixel-rounding policy.

## 5. Operator Workflow

The existing `라벨링 옵션` surface owns one visible setting:

```text
박스 입력
  2점 드래그 (기본값)
  4점 극점
```

Normal four-point flow:

1. select the class;
2. select `박스`;
3. select or restore `4점 극점`;
4. click `위 -> 아래 -> 왼쪽 -> 오른쪽`;
5. receive one completed ordinary box;
6. continue with the existing review, save, next-image, and reopen flow.

The canvas and status area must show the current role and progress, for example
`4점 극점 · 위 1/4`. Accepted points and edge guides are display-only draft
overlays.

Input rules:

- `Backspace` removes the most recent pending point;
- right-click or `Esc` cancels the entire pending draft;
- tool, image, Recipe, dataset-purpose, active-class, or drawing-method change
  cancels the draft with no object or history mutation;
- clicks outside the active image are rejected and do not advance progress;
- the fourth valid point creates exactly one ROI and one annotation-history
  step;
- `Undo` removes that completed ROI in one step and `Redo` restores it;
- pending points themselves never enter Undo/Redo history.

After completion, move, resize, duplicate, repeat-last, delete, hide, full lock,
movement pin, Object Review, and save use the existing Rectangle behavior.
There are no four-point-specific edit handles after creation.

## 6. Recipe-Scoped Setup

The selected drawing method is a reusable Recipe-scoped preference. The
planned setting is `LabelingProjectSettings.BoxDrawingMethod` with validated
values:

- `TwoPointDrag`;
- `FourPointExtreme`.

Requirements:

- default and invalid/stale fallback: `TwoPointDrag`;
- changing the option saves only the preference;
- reopening the same Recipe restores it visibly;
- another Recipe keeps its own value;
- an explicit reset returns to `TwoPointDrag`;
- restoration must not select a tool, create an ROI, start Smart Mask
  inference, confirm a candidate, save a label, or change the active layer.

## 7. Smart Mask Interaction

For a segmentation Recipe with `자동 윤곽` enabled, the first three extreme
points are draft input only. The fourth point creates one normal Rectangle and
then the existing `RoiAdded` policy may start MobileSAM exactly once.

No point click may independently start inference. Candidate comparison,
correction, Confirm/Skip, and canonical save behavior remain unchanged.

## 8. Canonical Storage And Reopen

The per-object canonical value remains:

```text
System.Drawing.Rectangle(X, Y, Width, Height)
```

The four source points and input method are not stored on the object because
they do not change its geometry or downstream meaning. No label schema,
segmentation JSON version, YOLO line shape, object ID, or export DTO changes.

Reopen must reconstruct the same ordinary axis-aligned Rectangle. Existing
labels remain valid without migration.

## 9. Interchange Contract

| Path | Four-point extreme result | Loss policy |
| --- | --- | --- |
| YOLO detection | existing normalized center/size line | no new loss beyond current integer/normalization rounding |
| COCO detection | existing `[x, y, width, height]` bbox | no new loss |
| Pascal VOC detection | existing `xmin/ymin/xmax/ymax` | no new loss |
| Label Studio detection export | existing rectangle with `rotation = 0` | no new loss |
| CVAT image-task export | existing `xtl/ytl/xbr/ybr` box | no new loss |
| Segmentation formats | no automatic polygon conversion | not applicable |

Rotated detection input is explicitly unsupported:

- Label Studio import must reject or skip any item whose image rotation or
  rectangle rotation is non-zero;
- CVAT detection import must reject or skip a box with a non-zero `rotation`
  attribute;
- preflight must expose the skipped unsupported item and block Apply under the
  existing skipped-record policy;
- it is prohibited to silently flatten a rotated input to its unrotated or
  axis-aligned bounds.

These guards are implementation acceptance criteria because the current
importers read axis-aligned coordinates but do not preserve non-zero rotation.

## 10. Planned Ownership

Implementation should use these durable boundaries:

- `WpfFourPointBoxService`: point-role state, pure bounds calculation,
  validation, and cancellation;
- `WpfCanvasPanelViewModel`: visible drawing-method option, progress, guidance,
  and reset command;
- `WpfLabelingShellWindow.FourPointBox`: UI event adapter, lifecycle
  cancellation, history coordination, and one completed-ROI handoff;
- `LabelingProjectSettings`: Recipe-scoped drawing-method preference;
- `RoiImageCanvasViewModel`: at most one narrow API that adds a completed
  ordinary Rectangle through the existing overlay/RoiAdded contract.

Do not modify the high-frequency Viewer/OpenGL mouse-move, ROI drag/resize,
brush, or eraser implementations for this feature.

## 11. Implementation Acceptance Matrix

The later implementation is Complete only when all of these pass:

- deterministic top/bottom/left/right geometry, reversed extreme positions,
  clipping, and degenerate rejection;
- pending progress, Backspace, right-click/Esc, and every lifecycle
  cancellation path;
- zero object/history/save before the fourth valid point;
- exactly one ordinary ROI and one history entry on completion;
- one-step Undo/Redo and existing move/resize/duplicate/repeat/delete behavior;
- class capture/cancellation behavior and current object-row presentation;
- Recipe save/reload/reopen, cross-Recipe isolation, stale-value fallback, and
  explicit reset with no restoration side effect;
- Smart Mask starts zero times before completion and exactly once afterward
  when automatic contour is enabled;
- YOLO save/reopen plus COCO, Pascal VOC, Label Studio, and CVAT axis-aligned
  interchange regression;
- non-zero Label Studio/CVAT rotation is rejected and blocks preflight Apply;
- protected ROI, Viewer/OpenGL, brush, eraser, annotation-history, and
  productivity gates;
- fresh current-build 1920x1080 and 1366x768 before/after evidence.

## 12. Completion Record

Status: Complete

Scope: axis-aligned four-point extreme-box meaning, interaction, Recipe
preference, canonical storage, editing, Smart Mask timing, interchange, planned
ownership, and implementation gates.

Acceptance criteria: supplied CVAT narration and frames distinguish four-point
extreme construction from later rotation; current Rectangle, YOLO, COCO,
Pascal VOC, Label Studio, and CVAT code paths were inspected; one exact product
meaning and loss policy are selected.

Verification: `--priority-workflow-docs` and `git diff --check`.

Evidence: this document, the checked local CVAT subtitle/video, and
`artifacts/four-point-box-contract-20260729/cvat-frames`.

Boundary / next dependency: this contract itself did not implement product
behavior. The later bounded implementation is Complete in
`docs/FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md`.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`
