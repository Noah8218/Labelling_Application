# P2 Edge-Aware Intelligent Scissors

Date: 2026-07-28 KST

## Outcome

Selected manual polygons now support a bounded image-edge refinement workflow:

1. Expand `세그먼트 편집 옵션` for the selected polygon.
2. Press `경계 추종`.
3. Click one existing polygon edge.
4. Inspect the gold `EDGE PREVIEW` path and the measured path-point/latency
   status.
5. Press `미리보기 적용` or `취소`.
6. Press `라벨 저장` separately when the edited label should be written.

Planning and preview do not mutate geometry, create history, or save a file.
Apply revalidates the source geometry and records exactly one annotation-history
step.

## Commercial-video lesson and exposure contract

The supplied CVAT AI-tools video showed intelligent scissors inside a connected
correction workflow, while the V7/CVAT review also showed that advanced
annotation functions are not all permanently exposed.

The implementation therefore does not add another global tool or left tool-rail
mode. It is visible only when a manual polygon is selected and only inside the
collapsed-by-default advanced segment editor. Text labels are retained because
`경계 추종`, `미리보기 적용`, and `취소` are less ambiguous than icon-only
commands; no decorative icon was added.

This is one-edge refinement, not autonomous whole-object segmentation:

- the operator chooses the object and the edge;
- the service searches only a fixed-width corridor around that edge;
- the operator sees the proposed path before mutation;
- Apply and label save remain separate explicit actions.

## Ownership

| Owner | Responsibility |
| --- | --- |
| `WpfIntelligentScissorsService` | grayscale extraction, Sobel edge cost, bounded deterministic A* path, path simplification, stale-plan detection |
| `WpfPolygonAnnotationService` | final simple-polygon validation and canonical point mutation/provenance |
| `WpfLabelingShellWindow.IntelligentScissorsCommands` | selected-object session, preview/apply/cancel, history, dirty state, status/log |
| `WpfObjectReviewPanelViewModel` | contextual visibility, pending/preview state, conflicting-command enablement |
| `WpfLabelingShellWindow.AnnotationPolygonOverlays` | gold open-path preview; source polygon remains unchanged until Apply |

Viewer/OpenGL, ROI, brush, eraser, model runtime, and training-input owners were
not changed.

## Accuracy and latency fixture

`--intelligent-scissors` includes a deterministic 128x128 black/white curved
boundary fixture. Its polygon begins with one straight edge whose endpoints lie
on the known curve.

Contract:

- identical image, polygon, click, and options return identical path and
  replacement points;
- at least 90% of simplified path points are within 2.5 source pixels of the
  known boundary;
- plan creation completes within 250 ms in the focused Debug fixture;
- the path begins and ends at the original adjacent vertices;
- a uniform image that produces only the existing straight edge is rejected
  without mutation;
- search is rejected above 180,000 pixels rather than blocking the UI on an
  unbounded edge;
- replacement must remain a non-duplicate, non-zero-area, non-self-intersecting
  polygon.

Default product search radius is 24 source pixels. A long edge whose bounded
search rectangle exceeds the limit must first be divided with `정점 추가`.

## Interaction and preservation gates

The focused shell fixture proves:

- right-click cancels before preview;
- preview adds `EDGE PREVIEW` but leaves geometry/history unchanged;
- object selection change cancels preview;
- Apply is one Undo/Redo step;
- ObjectId, class, component index, z-order, bounds, and
  `LastStructuralOperation=IntelligentScissors` survive canonical v3
  save/reopen;
- hidden and full-lock states reject direct command execution;
- movement pin continues to allow boundary refinement;
- stale preview geometry is rejected without a second mutation;
- no autosave occurs.

## UI evidence

Before, current build before this UI change:

- `artifacts\ui\intelligent-scissors-p2-20260728\before-current-build-1920x1080.png`
- `artifacts\ui\intelligent-scissors-p2-20260728\before-current-build-1366x768.png`

After, current build with an active edge preview:

- `artifacts\ui\intelligent-scissors-p2-20260728\after-current-build-1920x1080.png`
- `artifacts\ui\intelligent-scissors-p2-20260728\after-current-build-1366x768.png`

Both after captures keep the object list and main canvas as the primary
surfaces. The added row appears only in the selected-polygon advanced editor;
pending guidance disappears when the session ends.

## Completion record

Status: Complete

Scope: deterministic bounded refinement of one selected manual-polygon edge
with explicit preview/apply/cancel and separate label save.

Acceptance criteria: deterministic path, 90%/2.5px fixture accuracy, 250ms
fixture latency, invalid/stale no-mutation, explicit preview, one-step
Undo/Redo, canonical identity/provenance, hidden/full-lock protection,
movement-pin allowance, and 1920/1366 contextual UI all pass.

Verification: final commands and regression results are recorded in
`docs\WORK_TRACKING.md` under `2026-07-28 P2 edge-aware intelligent scissors`.

Evidence: this document, `Program.IntelligentScissors.cs`, and
`artifacts\ui\intelligent-scissors-p2-20260728`.

Boundary / next dependency: this does not prove CVAT/V7 parity, arbitrary
natural-image boundary accuracy, video propagation, multi-user review, or field
camera performance. The next bounded editor priority is P3 display-only
brightness/contrast/gamma/invert and histogram/equalization with source pixels,
dataset hash, saved labels, and training input unchanged.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`
