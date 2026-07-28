# P2 Polygon Vertex Insert/Delete

Date: 2026-07-28

Status: `Complete`

## Product contract

This slice adds precision editing for an existing saved manual polygon. It does
not add another permanent tool to the left palette.

- The controls appear only when the selected saved object is a manual polygon.
- They live inside the collapsed `세그먼트 편집 옵션` context.
- `정점 추가` arms one canvas click near a polygon edge.
- `정점 삭제` arms one canvas click near an existing vertex.
- Right-click or the visible `취소` button ends pending input without mutation.
- A successful click performs one mutation and exits pending input.
- No command automatically saves, changes image, runs inference, or changes the
  active class.

This follows the CVAT/V7 lesson relevant to this product: precision actions are
selected-object context, not an always-visible global toolbox.

## Geometry and state rules

- Hit tolerance is eight screen pixels converted through the current canvas
  zoom scale.
- Insert uses the deterministic nearest edge and projects the click onto that
  edge. Equal-distance ties keep the first edge in polygon order.
- Insert rejects an endpoint projection, duplicate point, zero-area result, or
  self-intersecting result.
- Delete uses the deterministic nearest vertex.
- Delete rejects a triangle, fewer than three distinct remaining points,
  zero-area geometry, or a self-intersecting result.
- Rejected edits do not mutate points, provenance, dirty state, or history.
- Successful edits keep the same object instance, object ID, class,
  component index, z-order, cutouts, and selection. They update only points,
  render invalidation, and `VertexInsert`/`VertexDelete` provenance.
- Each successful click is exactly one Undo/Redo step.
- Full lock and hidden state reject direct command and canvas mutation paths.
- Movement pin continues to allow vertex editing because it blocks only
  whole-object translation.
- Image, selected-object, or annotation-tool changes cancel pending input.

## Operator checklist

1. Open a segmentation Recipe and select a saved polygon.
2. Expand `세그먼트 편집 옵션`.
3. Choose `정점 추가` and click near an edge, or choose `정점 삭제` and
   click near a vertex.
4. Confirm the edit visually.
5. Use Undo/Redo if needed.
6. Press `라벨 저장` to persist the canonical v3 annotation.

For a mask or box selection, vertex controls are intentionally absent.

## Evidence

- Geometry owner:
  `0. UI\9) WPF\Services\Annotation\WpfPolygonAnnotationService.cs`
- Command/history bridge:
  `0. UI\9) WPF\Views\WpfLabelingShellWindow.PolygonVertexCommands.cs`
- Context state:
  `0. UI\9) WPF\ViewModels\Labeling\WpfObjectReviewPanelViewModel.cs`
- Focused test:
  `tests\LabelingApplication.Tests\Program.PolygonVertex.cs`
- Before/after current-build captures:
  `artifacts\ui\polygon-vertex-p2-20260728`

The 1920x1080 and 1366x768 after captures select a manual polygon and arm
vertex insertion. They show the two context actions, pending-only cancel and
guidance, ordinary class color, and retained canvas workspace. The before
captures show the default collapsed state with the object list available.

## Verification

- isolated test build: warning 0, error 0
- application Debug build: warning 0, error 0
- `--polygon-vertex`: pass
- `--segmentation-merge`: pass
- `--segmentation-split`: pass
- `--segmentation-hole`: pass
- `--segmentation-zorder`: pass
- `--segmentation-remove-underlying`: pass
- `--object-session-state`: pass
- `--segmentation-interchange-contract`: pass
- `--wpf-undo-redo-shortcuts`: pass
- `--wpf-object-review-panel`: pass
- `--wpf-annotation-object-verification`: pass
- `--wpf-roi-object-manipulation`: pass

## Completion record

Status: Complete

Scope: selected manual-polygon vertex insertion and deletion with contextual
UI, zoom-aware deterministic hit testing, invalid-result rejection, session
protection, one-step history, and canonical v3 replay.

Acceptance criteria:

- polygon-only contextual exposure -> pass
- deterministic zoom-aware edge/vertex hit -> pass
- invalid insert/delete no mutation -> pass
- object ID/class/component/z-order preservation -> pass
- one successful click equals one Undo/Redo step -> pass
- hidden/full-lock rejection and movement-pin allowance -> pass
- save/load/re-save provenance and geometry -> pass
- current-build 1920x1080 and 1366x768 evidence -> pass

Verification: both zero-warning/error builds and the commands listed above
passed against the current source.

Evidence: source, focused test, this document, and
`artifacts\ui\polygon-vertex-p2-20260728`.

Boundary / next dependency: this is manual polygon precision editing, not
automatic boundary snapping, edge-following scissors, video propagation,
persistent collaboration metadata, or field-accuracy evidence. The next
bounded P2 slice is edge-aware intelligent scissors and requires an explicit
latency/accuracy fixture before implementation.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`
