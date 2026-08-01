# PatchCore Heatmap Review View

Date: 2026-08-01 KST

Status: Complete

## Goal

Let an operator inspect the PatchCore localization heatmap directly from the
AI Candidate Review workflow without opening Explorer and without turning the
heatmap into an automatic label or model decision.

## Included scope

- show `히트맵 보기` only for a selected PatchCore candidate;
- inspect the configured path without decoding the image during selection;
- load the image only after the explicit action;
- use an owned, themed review window so Compact layouts keep the existing
  confirm/hide actions visible;
- close and release the preview when the operator closes the window or changes
  candidate;
- explain missing, moved, unsupported, corrupt, and unreadable files;
- decode with `BitmapCacheOption.OnLoad`, freeze the result, and release the
  file handle immediately;
- preserve the existing candidate, overlay, label-save, and model-adoption
  contracts.

## Excluded scope

- automatic heatmap opening;
- a canvas or Main Viewer heatmap layer;
- changing the input image, active layer, or viewer zoom;
- automatic contour confirmation, label save, candidate hide, or model
  adoption;
- production defect-quality or YOLO-versus-PatchCore superiority claims.

## Ownership

- `WpfPatchCoreHeatmapReviewService` owns PatchCore recognition, path and
  extension checks, fail-closed image decoding, and no-lock loading.
- `WpfCandidateReviewPanelViewModel` owns visibility, action text, open/closed
  state, status text, and image source.
- `WpfLabelingShellWindow.CandidateReviewListState.cs` is the UI adapter that
  opens/closes the owned window and resets evidence on candidate changes.
- `WpfPatchCoreHeatmapWindow` owns only the themed WPF presentation and theme
  token transfer from its parent.

## Acceptance criteria

| Criterion | Result | Evidence |
| --- | --- | --- |
| Selection does not decode or auto-open | Pass | focused service/ViewModel test |
| Explicit action opens the evidence | Pass | focused test and actual WPF visual smoke |
| Close and candidate change clear the image | Pass | focused state test and shell ownership contract |
| Missing/moved file fails closed with guidance | Pass | focused test |
| Corrupt image fails closed with guidance | Pass | focused test |
| Loaded file is not locked | Pass | file moved immediately after decode in focused test |
| Labels/candidates/models are not mutated | Pass | service has read-only candidate input and no data/model dependency; existing explicit commands remain separate |
| Dark/Wide layout | Pass | current-source 1920x1080 parent capture plus dark heatmap-window capture |
| Light/Compact layout | Pass | current-source 1366x768 parent capture plus light heatmap-window capture; confirm/hide actions remain visible |
| Button visual states | Pass | semantic theme resources plus explicit hover, pressed, keyboard-focus, and disabled triggers; focused source assertions |
| Owned window monitor placement | Pass | parent and child native rectangles intersect dynamically selected leftmost `\\.\DISPLAY2` |
| Build | Pass | isolated build, 0 warnings and 0 errors |
| Focused regressions | Pass | `--patchcore-heatmap-review`, `--wpf-candidate-review-panel`, `--patchcore-anomaly-pilot` |
| Full regression | Pass | default internal suite `266/266` |

No validation-error or dropdown/popup state applies because this surface has
no editable input, validation control, or popup. The missing/corrupt states
cover the read-only error boundary.

## Verification

```text
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-LabelingApplicationTests.ps1 -OutputName isolated-out
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --patchcore-heatmap-review
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-candidate-review-panel
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --patchcore-anomaly-pilot
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-visual-smoke ... --patchcore-heatmap <D evidence PNG> [--open-patchcore-heatmap]
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-DocumentationInformationArchitecture.ps1
git diff --check
```

## Visual evidence

Evidence root:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\patchcore-heatmap-review-20260801`

True before captures made before source editing:

- `before-candidates-visible-1920.png`;
- `before-candidates-light-compact-1366.png`.

Current-source after captures:

- `after-candidate-review-dark-wide-1920.png`;
- `after-heatmap-window-dark-wide-420.png`;
- `after-candidate-review-light-compact-1366.png`;
- `after-heatmap-window-light-compact-420.png`.

The visual smoke dynamically selected the leftmost monitor. The 1920 parent
occupied `-1920,360,1920,1080`; the 1366 parent occupied
`-1643,516,1366,768`; the owned heatmap window occupied
`-1170,680,420,440`. All were on `\\.\DISPLAY2` for this run.

## Durable closure

```text
Status: Complete
Scope: Explicit, read-only PatchCore heatmap inspection from AI Candidate Review with safe path/decode handling, owned themed window, candidate-change reset, and no workflow mutation.
Acceptance criteria: Explicit-only load, fail-closed missing/corrupt handling, no file lock, no automatic save/confirm/adoption, Dark/Wide and Light/Compact evidence, leftmost-monitor ownership, focused tests, build, and 266/266 regression all pass.
Verification: Commands and current-source visual smokes listed above.
Evidence: docs/PATCHCORE_HEATMAP_REVIEW_VIEW_20260801.md and the D-drive visual evidence root.
Boundary / next dependency: This is a review window, not a Main Viewer heatmap layer and not production-quality evidence. The next model gate still requires approved same-split field images, acceptance thresholds, runtime/weights, and target hardware for YOLO-versus-PatchCore comparison.
```
