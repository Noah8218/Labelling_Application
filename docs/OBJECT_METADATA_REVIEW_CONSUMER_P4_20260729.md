# Object Review Persistent Metadata Consumer (P4)

Date: 2026-07-29

Status: Complete

## Scope

Object Review is the first named consumer for persistent per-object metadata.
The bounded contract includes:

- `occluded` (`가림`) for saved manual boxes and saved manual segmentation
  objects;
- Recipe-defined free-text tags, normalized to at most 16 definitions and 32
  characters per tag;
- selected-row editing, combined `가림` plus tag filtering, visible row badges,
  filter reset, and Recipe tag-definition reset;
- Recipe-scoped tag-definition save/reopen;
- per-image JSON sidecars under
  `data/<split>/object-metadata/<image-stem>.json`;
- persistence only through the existing explicit label-save action.

The sidecar is intentionally separate from canonical YOLO box labels and
segmentation artifacts. Box metadata reconnects by class, bounds, and duplicate
occurrence. Segmentation metadata reconnects by the existing stable object ID.
Empty/default metadata removes a stale sidecar.

## User Workflow

1. Select a saved manual box or segmentation object in Object Review.
2. Toggle `가림`, or choose/type a Recipe tag and select `적용/해제`.
3. Optionally reduce the review list with `가림만` and a tag filter.
4. Select `라벨 저장` to write both the canonical annotation and its separate
   metadata sidecar.
5. Reopen the image; Object Review restores badges and the same filters can
   find the marked objects.

Recipe tag definitions remain visible and editable. `Recipe 태그 초기화`
returns only the definition list to its empty default; it does not silently
delete tags already stored on objects. Restoring Recipe settings does not save
labels, run inference, change the active tool, or apply a filter.

## Excluded Scope

- object grouping from the P4 contract itself; the separately approved P5
  group contract is now implemented without changing P4 tag/occluded meaning;
- training behavior or sample weighting;
- CVAT, Label Studio, COCO, Pascal VOC, or YOLO export changes;
- AI-candidate metadata before manual confirmation;
- team review, assignment, comments, accounts, or cloud synchronization;
- automatic label save.

Session-only hide, full-lock, and movement-pin remain separate and are not
persisted by this contract.

## Acceptance Criteria

- Object Review supplies selected-row `가림` and Recipe-tag controls: pass.
- Row badges and combined `가림` plus tag filters consume persisted metadata:
  pass.
- Filter and Recipe-tag-definition reset paths are explicit: pass.
- Recipe definitions survive save/reopen and reject duplicate/oversized input:
  pass.
- Duplicate identical boxes reconnect by occurrence and segments by object ID:
  pass.
- Metadata is written only by the explicit label-save path: pass.
- Existing train/valid/test YOLO label files remain unchanged: pass.
- Default metadata removes its stale sidecar: pass.
- No group, training, or external interchange behavior was introduced by P4:
  pass. Group behavior is owned separately by P5.

## Verification

- isolated Debug test build: warning 0, error 0;
- `--object-metadata-review`: pass;
- `--wpf-object-review-panel`: pass;
- `--object-session-state`: pass;
- `--wpf-labeling-shell`: pass;
- `--wpf-annotation-object-verification`: pass;
- `--wpf-project-config-panel`: pass;
- default internal suite: 259/259 pass;
- `--priority-workflow-docs`: pass;
- Object Review responsive layout at 1920x1080 and 1366x768: pass;
- `git diff --check`: pass;
- current-source visual smoke at 1920x1080 and 1366x768: pass.

## Evidence

- implementation:
  `0. UI/9) WPF/Services/ObjectReview/WpfObjectMetadataService.cs`;
- shell workflow:
  `0. UI/9) WPF/Views/WpfLabelingShellWindow.ObjectMetadata.cs`;
- focused tests:
  `tests/LabelingApplication.Tests/Program.ObjectMetadata.cs`;
- before/after current-source captures:
  `artifacts/ui/object-metadata-review-20260729`.

## Boundary / Next Dependency

This proves local per-object `occluded`/tag semantics. The separately owned
same-image group implementation is Complete in
`docs/OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md`. Neither slice proves
training usefulness, external-format group round-trip, or multi-user review.
Independent model adoption remains blocked until provenance-confirmed field
images and trustworthy ground truth exist.
