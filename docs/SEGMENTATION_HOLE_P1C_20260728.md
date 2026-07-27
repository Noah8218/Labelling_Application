# Segmentation Hole Editing P1-C

Date: 2026-07-28
Status: `Complete`
Field validation: `Not evaluated`

## Outcome

Saved Labels now exposes two bounded internal-hole operations for one selected
polygon or raster-mask object:

- **구멍 그리기**: draw a polygon fully inside the selected foreground, then
  close it by clicking the first point or double-clicking.
- **구멍 채우기**: click one empty internal hole to fill only that enclosed
  background component.

Right-click or the visible cancel button exits either input mode without
changing geometry or annotation history.

## Safety Contract

- Hole-add points must rasterize to a non-empty polygon strictly inside the
  selected object's bounds.
- Every polygon pixel must be foreground before subtraction.
- The removed background component must remain enclosed; any region connected
  to the exterior is rejected.
- Hole fill accepts only a background point inside the object bounds.
- Flood fill must not reach the object's bounds; exterior background,
  concavities, and open channels are rejected.
- Validation works on a copied full-image mask and never mutates the source.
- Smart Mask point input and structural-hole input cannot be active together.
- Selected-object, image, or annotation-tool changes cancel pending input.

## Identity And History

- Polygon and raster sources share
  `WpfSegmentationMaskGeometryService` rasterization.
- Structural geometry output is a raster object.
- Existing object ID, class, and z-order are preserved.
- A previously unidentified in-memory object receives an ID before publication.
- Component index becomes `-1`.
- Provenance is `HoleAdd` or `HoleRemove`.
- Each successful add/fill is exactly one full annotation-history step.
- Undo restores the prior geometry and representation; redo restores the
  edited geometry and identity.
- Canonical segment JSON v3 save, reopen, and re-save retain the object ID and
  structural-operation provenance.

## Ownership

- Geometry validation and enclosed-background flood fill:
  `WpfSegmentationHoleService`.
- Polygon draft state: a dedicated `WpfPolygonAnnotationService` instance.
- Enablement and operator guidance:
  `WpfObjectReviewPanelViewModel`.
- Canvas input, mutation, history, dirty state, and selection:
  `WpfLabelingShellWindow.SegmentationHoleCommands`.
- Existing save/history owners remain unchanged.
- Viewer/OpenGL, ROI, brush, eraser, and mask-drag hot paths were not
  rewritten.

## Evidence

Focused gate: `--segmentation-hole`.

It covers:

- polygon-source hole creation;
- raster-mask publication;
- hole fill by enclosed-background click;
- partially external add rejection;
- foreground-click rejection;
- exterior-connected background rejection;
- source immutability on invalid input;
- visible/right-click cancellation without a history entry;
- stable identity, class, and z-order;
- `HoleAdd`/`HoleRemove` provenance;
- one-step undo/redo;
- canonical v3 save, reopen, and re-save.

UI evidence:

- before:
  `artifacts\ui\segmentation-hole-p1c-20260728\before-current-build-1920x1080.png`
- after, with a selected mask and add-hole polygon input armed:
  `artifacts\ui\segmentation-hole-p1c-20260728\after-current-source-1920x1080.png`

## Durable Closure

Status: Complete

Scope: polygon hole creation, one enclosed-hole fill, cancellation, validation,
identity, history, and canonical v3 round-trip.

Acceptance criteria:

- valid polygon/raster hole edit changes only one selected object: pass;
- exterior-connected or otherwise invalid input causes no mutation: pass;
- object identity/class/z-order and operation provenance are retained: pass;
- add and fill are each one undo/redo step: pass;
- v3 reopen/re-save preserves identity and provenance: pass;
- current-source 1920x1080 UI shows the commands and active guidance: pass.

Verification: isolated/app Debug builds, focused hole and protected structural/
storage/history/segmentation/mask-performance regressions, documentation gate,
current-source UI evidence, and `git diff --check`.

Evidence: this document, `Program.SegmentationHole.cs`, and the UI artifact
folder above.

Boundary / next dependency: freehand hole brush, z-order, and
remove-underlying are not included. The next P1-C slice is z-order. The later
remove-underlying command requires an affected-object preview/warning before
mutation. This work does not claim CVAT/V7 parity, video propagation,
collaboration, or field accuracy.
