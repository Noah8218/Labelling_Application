# Dataset Interchange Preflight P5-A

Date: 2026-07-28

Status: Complete

## Outcome

OpenVisionLab Labeling Studio now exposes one contextual `변환` entry in
Model Center > Data. It opens a separate dataset-interchange window instead of
adding fourteen import/export controls to the permanent labeling rail.

The window covers the existing implemented COCO, Pascal VOC, Label Studio, and
CVAT detection/segmentation import/export operations. Every operation requires
a successful `Dry-run` before `적용` becomes available.

## Included scope

- COCO detection and segmentation JSON import/export;
- Pascal VOC detection directory import/export;
- Label Studio detection and segmentation JSON import/export;
- CVAT detection and segmentation archive import/export;
- current dataset purpose and target-split validation;
- image, annotation, class, and skipped-record counts;
- existing segmentation preservation/loss warnings;
- SHA-256 source immutability checks;
- proof that dry-run does not create or change the requested target;
- stale-input invalidation: changing format, path, image root, or split disables
  `적용` until dry-run is repeated;
- explicit Apply using the same existing format service that passed dry-run.

## Excluded scope

- new interchange formats such as Labelbox NDJSON;
- automatic conversion, automatic approval, or automatic label save;
- batch inference preflight, which remains P5-B;
- cloud/team accounts, reviewer assignment, video tracking, and deployment;
- production-data or model-quality claims.

## Ownership

- `Yolo/DatasetInterchangePreflightService.cs` owns the common dry-run/apply
  contract, request validation, isolated temporary execution, count
  normalization, and source/target fingerprints.
- Existing COCO, Pascal VOC, Label Studio, and CVAT services remain the only
  owners of actual serialization and import behavior.
- `WpfDatasetInterchangeViewModel` owns operation selection, input state,
  dry-run invalidation, result presentation, and Apply enablement.
- `WpfDatasetInterchangeWindow` is presentation-only.
- `WpfLabelingShellWindow.DatasetInterchange` owns window lifecycle and native
  file/folder dialog adaptation.

## Safety contract

1. Dry-run executes the selected real converter against an isolated temporary
   destination.
2. Export source data stays read-only. Import dry-run writes only to a cloned
   `CData` rooted in the temporary destination.
3. Export source fingerprint covers the current dataset tree. Import source
   fingerprint covers both the annotation/archive input and the external image
   root when the format requires one.
4. The requested destination fingerprint must be unchanged after dry-run.
5. Any malformed/unsupported skipped record is blocking rather than silently
   accepted.
6. Segmentation semantic-loss warnings are visible but do not fabricate
   geometry or silently approve a conversion.
7. Apply is enabled only for the unchanged request signature that produced the
   passing dry-run.

## Acceptance criteria

- Existing fourteen implemented external interchange operations remain
  discoverable: pass.
- COCO detection export dry-run reports exact image/annotation/class counts,
  preserves the source tree, and does not create the requested output: pass.
- Explicit export Apply creates the requested output and preserves the source:
  pass.
- COCO detection import dry-run uses an isolated target, preserves the external
  annotation and image source, and does not create the current dataset target:
  pass.
- Explicit import Apply writes the target dataset and preserves the external
  source: pass.
- Changing an input after a passing dry-run disables Apply: pass.
- Current-build 1920x1080 and 1366x768 layouts are readable without clipped
  controls: pass.

## Verification

```text
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
--dataset-interchange-preflight
--export-capability-inventory
--priority-workflow-docs
git diff --check
```

The isolated build completed with zero warnings and zero errors. Focused and
documentation gates passed. Current-build before/after captures are stored in:

`artifacts/ui/interchange-preflight-p5a-20260728`

## Commercial-product lesson

CVAT/V7-style capability depth is easier to operate when options appear at the
execution context, show consequences before mutation, and keep destructive
actions explicit. This implementation follows that lesson with one contextual
entry and a dedicated preflight surface; it does not attempt platform parity.

## Boundary / next dependency

P5-A completes format-conversion preflight only. Its historical next dependency,
P5-B batch AI preflight, is now complete in
`docs\BATCH_AI_PREFLIGHT_P5B_20260728.md`.

Current next dependency: independently acquired production-camera/cross-session
data.

Recommended model: no model tokens until the prerequisite exists.

Reasoning effort: not applicable until the prerequisite exists.
