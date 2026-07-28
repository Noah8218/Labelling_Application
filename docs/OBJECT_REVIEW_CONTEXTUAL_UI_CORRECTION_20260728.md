# Object Review Contextual UI Correction

Date: 2026-07-28

Status: `Complete`

Product identity: local, single-operator industrial image-labeling workstation.

## Why This Correction Exists

The first Object Review implementation exposed every segmentation command in
one always-visible left panel. At 1366x768 the command stack consumed the
available height and the actual object list had no visible rows. It also
implemented `Pin` as a gold review bookmark.

Both choices conflict with the commercial-video evidence:

- V7 keeps a compact tool rail and treats the object/instance list as a primary
  work surface.
- CVAT keeps common object state near the object row/property area.
- CVAT exposes mask add/subtract and structural operations through the active
  mask/toolbar context, not as a permanent full command inventory.
- CVAT `Pin` prevents position movement but still permits resize, copy, and
  delete. It is not a review bookmark.

Evidence:

- `artifacts\commercial-video-review-20260727\labeling-detail\01-v7-annotations-page-01.jpg`
- `artifacts\commercial-video-review-20260727\labeling-detail\04-cvat-box-page-01.jpg`
- `artifacts\commercial-video-review-20260727\labeling-detail\05-cvat-mask-page-02.jpg`
- `artifacts\commercial-video-review-20260727\04_CVAT_Bounding_Box_Overview.en.vtt`
- `artifacts\commercial-video-review-20260727\05_CVAT_Brush_Mask_Overview.en-orig.srt`

## Automatic Versus Contextual Exposure

| Function | Exposure rule |
| --- | --- |
| Hide, lock, movement pin | Compact icon controls for the selected saved object, with tooltip and accessible name |
| Class and delete | Compact selected-object properties; always available for a supported unlocked object |
| Image quality review | One collapsed `Quality review options` expander; image-level QA remains available without displacing object rows |
| Merge, split, hole, z-order, remove-underlying | One collapsed `Segmentation edit options` expander, visible only for a selected manual segment |
| Cancel | Visible only while the matching split, hole, or remove-underlying interaction is pending |
| Pending guidance | Visible only while that interaction is active |
| Object/instance rows | Remain visible below the compact selected-object panel and must not be displaced by inactive tools |

The application does not infer or run a structural edit automatically.
Contextual means that the relevant option is exposed when the selected object
supports it. Preview, confirmation, label save, and Candidate Review remain
explicit operator actions.

## Correct Object-State Semantics

| State | Canvas | Mutation |
| --- | --- | --- |
| Hidden | Not drawn; row remains selectable | Geometry/structural editing excluded until shown |
| Locked | Drawn | Class, delete, duplicate, geometry, brush/eraser, and structural mutations blocked |
| Movement pin | Drawn in its ordinary class color | Whole-object translation blocked; ROI resize, polygon vertex edit, class, copy, delete, and structural commands remain allowed |

All three states remain current-image session state. They do not enter
canonical label data, annotation history, exports, training input, or
collaboration semantics.

## Implementation Ownership

- `WpfObjectReviewPanelViewModel`: selected object type, expander state,
  command enablement, and pending-state visibility.
- `WpfObjectReviewPanel`: compact state icons and contextual segmentation
  expander.
- `WpfObjectSessionStateService`: current-image object state.
- `CanvasOverlayItem.IsMoveLock` and `RoiImageCanvasViewModel`: ROI translation
  guard without disabling resize/copy/delete.
- `WpfLabelingShellWindow.AnnotationSegmentEdit`: whole mask/polygon movement
  guard while preserving polygon vertex editing.
- Existing structural services remain the geometry owners.

## Acceptance Criteria

- Segment selection shows one collapsed contextual editor; ROI selection hides
  it.
- Inactive cancel controls and pending guidance are not visible.
- 1920x1080 and 1366x768 current-build captures keep object rows visible.
- Movement pin does not add gold color or a `PINNED` overlay label.
- Pinned polygon whole movement is rejected and vertex movement is accepted.
- Pinned ROI resolves `IsMoveLock=true` and `IsControlLock=false`; locked ROI
  still uses `IsControlLock=true`.
- Hide/lock/session-lifetime and protected structural/editor regressions pass.

## Evidence

Fresh pre-change current-build captures:

- `artifacts\ui\object-review-contextual-20260728\before-current-build-1920x1080.png`
- `artifacts\ui\object-review-contextual-20260728\before-current-build-1366x768.png`

The 1366x768 baseline shows zero visible Object Review rows because the
inactive command inventory occupies the panel.

Current verification:

- isolated test build: warning 0, error 0;
- application solution build: warning 0, error 0;
- `--object-session-state`: pass;
- protected structural, storage, history, ROI, segmentation,
  mask-performance, Object Review, shell, MVVM, productivity, and MobileSAM
  regressions: pass;
- `--priority-workflow-docs`: pass;
- `git diff --check`: pass (line-ending warnings only).

Fresh current-build after captures:

- `artifacts\ui\object-review-contextual-20260728\after-current-build-1920x1080.png`
- `artifacts\ui\object-review-contextual-20260728\after-current-build-1366x768.png`

Visual comparison: the 1366x768 baseline had zero visible object rows; the
after capture shows all three fixture rows. Both quality review and
segmentation editing are collapsed options, while the selected object's class,
delete, and three compact state icons remain immediately available.

## Evaluation Boundary

The corrected slice restores the labeling-editor depth estimate to `3.1/5`.
Focused local-workstation maturity remains `4.0/5`. This correction does not
claim CVAT/V7 parity and does not add collaboration, accounts, comments,
server synchronization, video interpolation/tracking, 3D labeling, or
camera/PLC/I/O scope.

## Next Priority

After this correction closes, implement polygon vertex insert/delete with
deterministic hit testing, invalid-polygon rejection, one-step Undo/Redo,
canonical v3 replay, and hidden/locked protection.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`

## Completion Record

Status: Complete

Scope: Commercial-video-grounded Object Review information architecture and
movement-pin semantic correction.

Acceptance criteria: contextual object-type exposure, pending-only controls,
movement-pin semantics, object-list recovery at 1920/1366, and protected
regressions passed.

Verification: zero-warning/error isolated and application builds; focused,
protected, documentation, and diff gates listed above; fresh current-build
1920/1366 visual comparison.

Evidence: this document and the before-capture paths above.

Boundary / next dependency: this proves the corrected local contextual UI and
movement-pin contract, not CVAT/V7 parity. Polygon vertex insert/delete is the
next bounded slice.
