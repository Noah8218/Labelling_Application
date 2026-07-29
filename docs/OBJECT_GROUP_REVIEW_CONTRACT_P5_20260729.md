# Object Group Review Contract (P5)

Date: 2026-07-29

Status: Complete

Scope: approved product and implementation contract. The bounded product
implementation is Complete in
`docs/OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md`.

## Decision

`group` means a persistent, image-local Object Review unit that relates two or
more saved manual objects belonging to one physical part or one operator review
unit.

Object Review is the first named consumer. It must use the relationship to:

- collect and focus all members of one group;
- filter grouped, ungrouped, or one selected group;
- show member count and a compact group badge;
- apply persistent `occluded` or one Recipe tag to all current group members
  through one explicit group action.

This is not a renamed tag. A tag classifies independent objects. A group
declares that several objects must be reviewed together.

## Eligible Objects

- saved manual rectangles;
- saved manual polygon or raster segmentation objects;
- mixed box and segmentation membership on the same image.

AI candidates are not eligible until the existing explicit confirmation path
turns them into saved manual objects.

Every object may belong to zero or one group. A valid group has at least two
members. Groups never cross images.

## Operator Workflow

1. Select `그룹 구성` in the collapsed Object Review metadata area.
2. Mark at least two eligible rows in a dedicated group-selection set.
   This set is independent from the existing segmentation merge selection.
3. Review a preview containing member count, object types, and classes.
4. Select explicit `그룹 만들기`.
5. Object Review assigns one stable group ID and shows a compact `그룹 N ·
   M개` badge.
6. The operator can filter/focus the group or apply `가림`/Recipe tag to all
   members.
7. `그룹에서 제거` removes only the selected member. `그룹 해제` removes
   the relationship from every member after explicit confirmation.
8. `라벨 저장` persists the group metadata. No group action saves
   automatically.

Cancel clears only the pending group-selection preview. It does not change
saved metadata.

## Identity And Persistence

- Store a stable opaque `GroupId` per object in object-metadata sidecar schema
  v2.
- Do not store a user-facing ordinal as identity. Display `그룹 1`, `그룹 2`,
  and so on from first-member row order for the current image.
- Schema v1 sidecars load with empty group membership.
- A v2 sidecar may contain `occluded`, tags, and optional `GroupId`.
- Keep the sidecar under
  `data/<split>/object-metadata/<image-stem>.json`.
- Do not write group data into YOLO labels or canonical segmentation files.
- Rectangle members reconnect by the existing class, bounds, and duplicate
  occurrence rule.
- Segmentation members reconnect by stable object ID.
- Reject malformed IDs and dissolve persisted single-member groups during load
  with an operator-readable warning.

Recipe configuration does not own group definitions because groups are local
to one image. Recipe continues to own only reusable tag definitions.

## Mutation Rules

| Operation | Group result |
| --- | --- |
| Geometry move/resize or class change | Preserve membership |
| Delete one member | Remove it; dissolve the group if fewer than two remain |
| Duplicate object | New object starts ungrouped |
| Segmentation merge | Preserve only when every source has the same non-empty group; otherwise the merged result is ungrouped |
| Segmentation split | Both outputs inherit the source group; dissolve later if only one member remains |
| Hole, vertex edit, z-order, remove-underlying | Preserve membership with the existing object identity |
| Undo/Redo of geometry | Restore the metadata state associated with the restored object snapshot where the operation already replaces object identity |
| Image change/reopen | Restore only saved group state; unsaved changes stay protected by the existing dirty-label decision |

Group creation, member removal, and dissolution are metadata edits. They create
one metadata history step only if a dedicated metadata-history owner is
introduced; they must not be inserted into geometry history by reaching into
unrelated private state.

## Group-Level Metadata Actions

Group-level `가림` and tag actions must:

- preview the member count;
- affect all current members atomically in memory;
- create one dirty-state reason;
- require the existing explicit label-save action;
- roll back the entire in-memory group action if validation fails;
- never run training, inference, export, or candidate confirmation.

Per-object editing remains available after group assignment.

## UI Contract

- Keep the object/instance list as the primary review surface.
- Put `그룹 구성` and group actions in the existing collapsed persistent
  metadata area.
- Do not convert the whole Object Review list to permanent multi-selection.
- Use a dedicated pending-selection affordance so Delete, class change,
  session state, and segmentation structural commands retain their current
  single-selection contracts.
- Pending selection must expose member count, preview, Apply, and Cancel.
- Group-only controls need text or a familiar icon with tooltip and accessible
  name.
- At 1366x768, secondary group controls may scroll inside Object Review but
  must not reduce the canvas below the existing responsive boundary.

## Explicit Exclusions

- geometry union, segmentation merge, or shared movement;
- cross-image, video-track, temporal, or parent/child relations;
- training weighting, sampling, loss, or metric aggregation;
- CVAT, Label Studio, COCO, Pascal VOC, or YOLO interchange;
- team assignment, reviewer ownership, comments, or cloud synchronization;
- Recipe-level reusable group templates;
- automatic save or automatic group creation.

## Implementation Ownership

- `WpfObjectMetadataStateService`: group membership state and invariants;
- `WpfObjectMetadataPersistenceService`: v1-compatible/v2 sidecar load and v2
  save;
- a cohesive Object Review group-selection service: pending member set,
  preview, create/remove/dissolve plans, and mutation rules;
- `WpfObjectReviewPanelViewModel`: visible group state, filters, badges,
  command enablement, and pending preview;
- `WpfLabelingShellWindow.ObjectMetadata.cs`: UI workflow adaptation and
  explicit save routing only.

Do not reuse the segmentation merge service or its selection set as the group
owner. Merge changes geometry; grouping changes review metadata.

## Implementation Acceptance Gates

- same-image box/segment/mixed grouping and one-group-per-object invariant;
- two-member minimum, member removal, automatic orphan dissolution, and full
  group dissolution;
- stable v2 save/reopen and backward-compatible v1 load;
- duplicate box occurrence and segment object-ID reconnect;
- mutation table coverage for delete, duplicate, merge, split, class change,
  and identity-preserving edits;
- grouped/ungrouped/specific-group filters and focus navigation;
- atomic group-level `occluded` and Recipe-tag application;
- explicit save only, dirty navigation protection, and no candidate
  confirmation side effect;
- unchanged YOLO labels, segmentation artifacts, training, and interchange;
- isolated build, focused Object Review/metadata/history/structural
  regressions, default suite, `--priority-workflow-docs`, and
  `git diff --check`;
- fresh current-build before/after captures at 1920x1080 and 1366x768.

## Evidence And Rationale

- user analysis checklist requires an actual Recipe/export/training/review
  consumer before persistent Group/Instance ID metadata;
- CVAT/V7 evidence supports instance/object-list-centered state and review, but
  does not justify importing video propagation or team workflow;
- current Object Review already owns saved-object selection, badges, filters,
  and persistent `occluded`/tag review metadata;
- current segmentation merge selection is intentionally separate because it
  mutates geometry.

## Boundary / Next Dependency

The user accepted this exact meaning and the bounded implementation is
Complete. Preserve the same-image physical/review-unit meaning and keep group
metadata separate from geometry, training, interchange, collaboration, and
automatic save.
