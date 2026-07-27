# Segmentation Merge / Join P1-C

## Outcome

Status: `Complete`

The first user-visible structural mask operation is available in **Saved
Labels**. An operator can check two or more segmentation rows and merge them
into one same-class raster-mask object. Polygon geometry, polygon cutouts,
raster masks, and disconnected components are accepted.

This slice does not implement split/slice, manual hole creation, z-order
controls, or remove-underlying.

## Operator workflow

1. Open a segmentation Recipe and image.
2. Open **Saved Labels**.
3. Check the merge box beside at least two segmentation rows.
4. Confirm the counter reads `병합 선택 N개`.
5. Press **선택 마스크 병합**.
6. Review the merged overlay, then use Undo/Redo if needed.
7. Press **라벨 저장** to persist the result.

Only segmentation rows expose merge checkboxes. Detection boxes and confirmed
AI detections keep their existing single-selection edit behavior.

## Behavior contract

- At least two current segmentation rows are required.
- Every selected source must resolve to the same normalized class name.
- A mixed-class request is rejected before annotation mutation.
- Each polygon is rasterized with its own cutouts before it is OR-combined
  with other source objects. One object's cutout therefore cannot erase pixels
  supplied by another selected object.
- Raster sources are OR-combined without modifying their source buffers.
- The source rows are replaced by one raster object at the first selected
  index.
- The merged object receives a new image-local object ID, component index
  `-1`, maximum source z-order, and `LastStructuralOperation=Merge`.
- The whole merge is one existing annotation-history step.
- Canonical schema v3 writes disconnected regions as component records that
  share the merged object ID and retain `Merge` provenance.
- Label save remains explicit. Merge never auto-saves or auto-confirms.

## Ownership

- Geometry and validation:
  `0. UI\9) WPF\Services\Annotation\WpfSegmentationMergeService.cs`
- Merge selection, counter, enablement, and commands:
  `0. UI\9) WPF\ViewModels\Labeling\WpfObjectReviewPanelViewModel.cs`
- Visible controls:
  `0. UI\9) WPF\Views\WpfObjectReviewPanel.xaml`
- Mutation/history bridge:
  `0. UI\9) WPF\Views\WpfLabelingShellWindow.ObjectReviewCommands.cs`
- Canonical identity and replay:
  `Yolo\YoloSegmentationAnnotationService.cs`

Viewer/OpenGL, ROI interaction, brush, eraser, and mask-drag hot paths were not
changed.

## Acceptance evidence

- Mixed polygon/raster sources produce one raster-mask object.
- Unfilled cutout pixels remain empty.
- Another selected object can fill pixels inside that cutout.
- A disconnected raster component survives the merge.
- Mixed classes are rejected without changing the source list.
- Three source objects merge to one object in one step; Undo restores all
  three source identities and Redo restores the merged identity.
- Save emits canonical schema v3 component records with one shared object ID,
  sequential component indices, and `Merge` provenance.
- Load and re-save retain the shared object ID and provenance.
- The Saved Labels UI exposes checkboxes, selection count, an unambiguous text
  button, tooltip, automation ID, and accessible name.

Focused command:

```powershell
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --segmentation-merge
```

Visual evidence:

- Before:
  `artifacts\ui\segmentation-merge-p1c-20260727\before-current-source-1920x1080.png`
- After:
  `artifacts\ui\segmentation-merge-p1c-20260727\after-current-source-1920x1080.png`

## Completion record

Status: Complete

Scope: same-class segmentation merge/join only.

Acceptance criteria: geometry union, cutout semantics, mixed-class rejection,
new v3 identity/provenance, one-step undo/redo, canonical save/load/re-save,
and visible Saved Labels controls all pass.

Verification: isolated/app Debug builds, `--segmentation-merge`, protected
segmentation/history/performance regressions, documentation gate,
1920x1080 before/after review, and `git diff --check`.

Evidence: this document, the focused test, and the visual artifact folder
listed above.

Boundary / next dependency: this is one structural command, not CVAT/V7 mask
editing parity. The next bounded operation is split/slice with an explicit cut
contract, one-step history, component identity rules, and canonical replay.
Manual hole editing, z-order, and remove-underlying remain later independent
P1-C slices.
