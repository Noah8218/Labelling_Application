# P3 Display-Only Image Aids

Date: 2026-07-28
Status: Complete
Field validation: Not evaluated

## Outcome

The labeling canvas now exposes one compact `보기 보정` popup in the canvas
header. It provides:

- brightness `-100..100`;
- contrast `50..200%`;
- gamma `0.20..3.00`;
- color inversion;
- luminance histogram equalization;
- one explicit reset action.

The controls are not added to the permanent annotation tool rail. This follows
the supplied V7/CVAT evidence: display aids are contextual view options, while
the main rail remains focused on the current labeling action.

## Ownership and safety contract

| Concern | Owner | Contract |
| --- | --- | --- |
| canonical source image | `WpfLabelingShellWindow.activeImageBitmap` | Never replaced or modified by display aids |
| disk/training input | existing image path and dataset services | File bytes and training source remain unchanged |
| display transformation | `WpfImageDisplayAdjustmentService` | Creates an owned 24-bpp copy only |
| popup state and commands | `WpfCanvasPanelViewModel` | Screen-session state; not Recipe or annotation state |
| base texture refresh | `WpfLabelingShellWindow.DisplayAdjustment.cs` | Replaces only the base texture |
| box/polygon/mask coordinates | existing canvas overlay collections | Never transformed; image coordinates remain canonical |

Slider changes are coalesced by a 120ms dispatcher timer. Settings remain active
while moving through the image queue so operators can compare images under the
same view condition. Clearing the active image closes the popup; reset restores
the original display copy. Neither adjustment nor reset creates annotation
history, dirty state, autosave, label save, or Candidate Review actions.

## Included and excluded scope

Included:

- display-only pixel transformation;
- source/file/hash immutability;
- overlay alignment;
- compact contextual UI;
- 1920x1080 and 1366x768 current-build evidence.

Excluded:

- changing or exporting adjusted source pixels;
- persisting settings in Recipe/dataset metadata;
- automatic label correction;
- video/frame propagation;
- cloud review, assignment, comments, or collaboration;
- a claim of CVAT/V7 feature parity or production-camera validation.

## Acceptance evidence

The focused contract test proves:

- default settings return a separate pixel-identical copy;
- non-default settings change only the display copy;
- equalization expands a deterministic low-contrast fixture;
- canonical in-memory bitmap hash stays unchanged;
- source image file SHA-256 stays unchanged;
- annotation dirty state and undo history stay unchanged;
- polygon overlay image coordinates stay unchanged through apply and reset;
- 1920x1080 full adjustment completes in `333.7ms` in the final recorded Debug run.

Current-build visual evidence:

- before:
  - `artifacts\ui\display-aids-p3-20260728\before-current-build-1920x1080.png`
  - `artifacts\ui\display-aids-p3-20260728\before-current-build-1366x768.png`
- after:
  - `artifacts\ui\display-aids-p3-20260728\after-display-popup-1920x1080.png`
  - `artifacts\ui\display-aids-p3-20260728\after-display-popup-1366x768.png`

Visual comparison: the new entry is one header button, the controls are hidden
until requested, the popup remains within the usable canvas area at both sizes,
the annotation rail is unchanged, and the existing overlays remain aligned with
the adjusted image.

## Verification

```text
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-display-p3\
Result: 0 warnings, 0 errors

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-display-p3\LabelingApplication.Tests.dll --image-display-adjustment
Result: PASS

dotnet ...\LabelingApplication.Tests.dll --wpf-visual-smoke ... --open-display-adjustment --width 1920 --height 1080 --screen-capture
Result: PASS, current-build capture produced

dotnet ...\LabelingApplication.Tests.dll --wpf-visual-smoke ... --open-display-adjustment --width 1366 --height 768 --screen-capture
Result: PASS, current-build capture produced

git diff --check
Result: PASS; existing line-ending conversion notices only
```

## Completion record

Status: Complete

Scope: Brightness, contrast, gamma, invert, histogram equalization, compact
popup exposure, base-texture refresh, and source/overlay/history safety.

Acceptance criteria: all included controls work through one contextual popup;
canonical source/file hashes, overlay coordinates, dirty state, and history
remain unchanged; current-build 1920/1366 captures pass.

Verification: zero-warning/error focused build, `--image-display-adjustment`,
two current-build visual smokes, and `git diff --check`.

Evidence: this document, focused test output, and
`artifacts\ui\display-aids-p3-20260728`.

Boundary / next dependency: this proves a bounded display-only image workflow,
not production-camera usefulness or CVAT/V7 parity. The historical next
priority, P4 Dataset Health visual label QA, is complete in
`docs\DATASET_HEALTH_VISUAL_QA_P4_20260728.md`. The current next priority is P5
interchange/batch preflight.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`
