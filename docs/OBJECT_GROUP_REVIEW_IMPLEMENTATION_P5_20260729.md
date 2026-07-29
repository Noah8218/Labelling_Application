# Object Group Review Implementation (P5)

Date: 2026-07-29

Status: Complete

## Scope

Implement the approved same-image Object Review group contract for saved
manual boxes, polygons, and raster masks.

- A group represents one physical part or one operator review unit.
- Each object belongs to zero or one group.
- A valid group has at least two members and never crosses images.
- Grouping changes review metadata only. It does not merge geometry, move
  members together, change training, or alter interchange formats.

## Operator Workflow

1. Open `검수 메타데이터` in Saved Labels.
2. Select `그룹 구성`.
3. Check two or more ungrouped saved-object rows. The preview reports the
   member count, object types, and classes.
4. Select `만들기`.
5. Use the group badge/filter to focus the review unit, or apply group-level
   `가림` and the selected Recipe tag.
6. Select the existing `라벨 저장` action to persist the relationship.

The pending selection set is dedicated to grouping. It does not reuse the
segmentation merge selection and disappears on Cancel or image transition.

## Persistence And Identity

- `WpfObjectMetadataPersistenceService` writes schema v2 sidecars under
  `data/<split>/object-metadata/<image-stem>.json`.
- Schema v1 remains readable and loads objects as ungrouped.
- Boxes reconnect by class, bounds, and duplicate occurrence.
- Segmentation objects reconnect by stable object ID.
- Group IDs are opaque normalized GUID values; display names such as
  `그룹 1 (2개)` are image-local presentation only.
- Metadata writes only through explicit label save. YOLO labels and canonical
  segmentation files remain separate.

## Mutation Rules

- move, resize, class change, hole/vertex editing, z-order, and
  remove-underlying preserve group membership;
- duplicate creates an ungrouped object;
- delete removes the member and dissolves a one-member remainder;
- segment merge inherits a group only when every source has the same nonempty
  group;
- segment split assigns the source group to every output;
- full dissolve requires explicit confirmation and does not delete objects.

## Ownership

- `Services/ObjectReview/WpfObjectMetadataService.cs`: persistent metadata,
  group invariant, schema v1/v2 load, and v2 save.
- `Services/ObjectReview/WpfObjectReviewGroupSelectionService.cs`: dedicated
  pending group selection and create validation.
- `WpfObjectReviewPanelViewModel`: group badges, filters, preview, and command
  state.
- `WpfLabelingShellWindow.ObjectMetadata.cs`: UI workflow adaptation and group
  batch actions.

## Acceptance Criteria

- same-image box/segment grouping and one-group-per-object: pass;
- two-member minimum, member removal, orphan dissolution, and confirmed full
  dissolve: pass;
- stable v2 save/reopen and v1 backward-compatible load: pass;
- grouped, ungrouped, and specific-group filtering: pass;
- group-level `occluded` and Recipe-tag actions: pass;
- delete, merge, split, class/edit preservation, and duplicate boundaries:
  pass;
- pending selection is separate from segmentation merge and canceled on image
  transition: pass;
- explicit save only; no training, inference, interchange, or candidate
  confirmation side effect: pass;
- 100,000-row ordinary replacement/removal keeps the existing incremental
  performance contract: pass.

## Verification

- isolated Debug test build: warning 0, error 0;
- `--object-group-review`: pass;
- `--object-metadata-review`: pass;
- `--wpf-object-review-panel`: pass;
- `--object-session-state`: pass;
- `--segmentation-merge`: pass;
- `--segmentation-split`: pass;
- `--wpf-labeling-shell`: pass;
- `--wpf-annotation-object-verification`: pass;
- `--wpf-project-config-panel`: pass;
- `--wpf-undo-redo-shortcuts`: pass;
- `--wpf-labeling-session-smoke`: pass;
- `--wpf-settings-viewmodels`: three consecutive passes;
- Object Review responsive layout at 1920x1080 and 1366x768: pass;
- default internal suite: 260/260 pass.

## Evidence

- contract: `docs/OBJECT_GROUP_REVIEW_CONTRACT_P5_20260729.md`;
- focused tests: `tests/LabelingApplication.Tests/Program.ObjectMetadata.cs`;
- current-source 1920 before and 1920/1366 after captures:
  `artifacts/ui/object-group-review-20260729`;
- closest current-source pre-group 1366 baseline:
  `artifacts/ui/object-metadata-review-20260729/after-object-review-1366x768.png`.

## Boundary / Next Dependency

This completes local same-image review grouping. It does not establish
cross-image/video tracking, shared geometry movement, training semantics,
external-format group preservation, collaboration, or cloud review.

Exact polygon/raster cross-family renderer ordering remains blocked until a
visual defect is reproduced. Production model adoption remains blocked until
provenance-confirmed, content-separated camera/session ground truth exists.
