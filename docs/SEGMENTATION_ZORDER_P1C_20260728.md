# Segmentation Saved-Object Z-Order P1-C

Date: 2026-07-28
Status: `Complete`
Field validation: `Not evaluated`

## Outcome

Saved Labels exposes four explicit stack-order commands for one selected
polygon or raster-mask object:

- **맨 뒤**: move to stack position `0`;
- **한 칸 뒤**: move down one stack position;
- **한 칸 앞**: move up one stack position;
- **맨 앞**: move to the highest stack position.

The panel shows `표시 순서 current/total` and states that a larger number is
in front. Commands that would cross the first or last position are disabled,
and a direct boundary invocation leaves data and history unchanged.

## Ordering Contract

- Lower `ZOrder` means farther back; higher `ZOrder` means farther forward.
- Existing objects are stable-sorted by `(ZOrder, current list position)`
  before a move. This keeps legacy equal-order files deterministic.
- A successful move rewrites the saved-object list in stack order and
  normalizes `ZOrder` to `0..N-1`.
- Newly drawn polygons, duplicated segments, new brush-mask classes, and
  transferred template segments receive the next front order. Existing
  merge/split contracts retain their source z-order values, and reopened
  annotations are sorted without rewriting stored numeric metadata.
- Object IDs, classes, polygon/cutout/raster geometry, component metadata, and
  mask buffers are preserved.
- The selected object remains selected at its new source index.
- Every object whose numeric order changes, plus the explicitly moved object,
  records `LastStructuralOperation=ZOrder`.
- A successful move is exactly one full annotation-history step.
- Reopening saved annotations sorts all classes together by canonical
  `ZOrder`; class dictionary enumeration cannot alter the restored stack.

## Rendering Boundary

The current viewer has separate raster-mask and polygon overlay render passes.
This slice applies canonical order and list order globally and uses `ZOrder`
inside each render family. It does not claim exact polygon-versus-raster
interleaving. Solving that requires a renderer-level composition change and
proportionate OpenGL performance evidence; it was intentionally not folded
into this bounded annotation workflow.

## Ownership

- Planning, deterministic ordering, and boundary validation:
  `WpfSegmentationZOrderService`.
- Mutation, history, dirty state, selection, and operator status:
  `WpfLabelingShellWindow.SegmentationZOrderCommands`.
- Command enablement and selected-position guidance:
  `WpfObjectReviewPanelViewModel`.
- Class-independent canonical reload ordering:
  `WpfLabelingShellWindow.SavedAnnotationLoading`.
- Per-render-family overlay ordering:
  `WpfLabelingShellWindow.AnnotationPolygonOverlays`.
- Canonical v3 persistence and existing history cloning remain with their
  existing owners.

The Viewer/OpenGL implementation, ROI, brush, eraser, and mask-drag hot paths
were not rewritten.

## Evidence

Focused gate: `--segmentation-zorder`.

It covers:

- all four move plans;
- first/last boundary rejection with no mutation;
- explicit four-button UI and enablement;
- stable object identity, class, and geometry;
- normalized order and `ZOrder` provenance;
- one-step undo/redo;
- multi-class canonical v3 save, reopen, and re-save;
- shell reload ordering independent of class grouping.

UI evidence:

- before:
  `artifacts\ui\segmentation-zorder-p1c-20260728\before-current-build-1920x1080.png`
- after, with the selected mask moved to the back:
  `artifacts\ui\segmentation-zorder-p1c-20260728\after-current-source-1920x1080.png`

## Durable Closure

Status: Complete

Scope: explicit saved-object stack moves, deterministic normalization,
selection, history, canonical v3 persistence, and per-render-family overlay
ordering.

Acceptance criteria:

- all four requested moves produce deterministic order: pass;
- boundary moves produce no data or history mutation: pass;
- identity, class, geometry, and selected-object continuity are preserved:
  pass;
- one Undo restores the previous stack and one Redo restores the move: pass;
- multi-class v3 reopen/re-save preserves global canonical order: pass;
- current-source 1920x1080 UI shows all commands and position guidance without
  clipping: pass.

Verification: isolated/app Debug builds; focused z-order and protected
structural/storage/history/segmentation/mask-performance regressions;
documentation gate; current-source UI evidence; `git diff --check`.

Evidence: this document, `Program.SegmentationZOrder.cs`, and the UI artifact
folder above.

Boundary / next dependency: exact polygon/raster render interleaving is not
included. The next P1-C slice is remove-underlying and must preview affected
objects, warn before mutation, preserve unaffected identity/order, and remain
one undoable operation. This work does not claim CVAT/V7 parity, video
propagation, collaboration, field accuracy, or production model readiness.
