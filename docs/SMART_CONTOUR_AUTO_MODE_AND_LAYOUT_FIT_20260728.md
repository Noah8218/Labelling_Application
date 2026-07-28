# Smart Contour Auto Mode and Layout Auto-Fit

Date: 2026-07-28
Product boundary: local single-operator labeling workstation

## Outcome

Segmentation labeling now exposes one Recipe-scoped option:
`라벨링 옵션 · 자동 윤곽`.

When enabled:

1. the Rectangle tool becomes active;
2. drawing a new rectangle immediately starts MobileSAM;
3. the rectangle is consumed as the prompt after a successful result;
4. the candidate remains unsaved until explicit confirm/skip;
5. confirmation or skip returns directly to the next-box loop;
6. the option remains enabled after Recipe save and reopen.

The operator no longer presses a Smart Mask start button or a Fit button for
each object/layout transition. `후보 다시 생성` remains available only inside
an active correction session.

## Ownership and safety

- `WpfCanvasPanelViewModel` owns visible option state, text, enablement, and
  command presentation.
- `LabelingProjectSettings.SmartMaskAutoContourEnabled` owns Recipe persistence.
- `WpfLabelingShellWindow.SmartMask` bridges a newly completed rectangle to the
  existing MobileSAM request and Candidate Review workflow.
- `WpfLabelingShellWindow.WorkspaceLayout` responds only to actual canvas
  viewport size changes and schedules `ZoomToFit` after layout settles.
- Restoring the option does not select a tool, draw a rectangle, run inference,
  confirm a candidate, or save a label.
- User zoom/pan does not change the viewport size and therefore does not trigger
  automatic fit.

## Acceptance checklist

| Criterion | Result |
|---|---|
| One option click enables repeated automatic-contour boxes | Pass |
| Rectangle completion starts MobileSAM without another action click | Pass |
| Drawing is locked while the candidate is running/reviewed | Pass |
| Candidate still requires explicit confirm/skip | Pass |
| Confirm/skip returns to the next Rectangle | Pass |
| Recipe save/load restores the option without running inference | Pass |
| Workflow layout change re-centers the image without `맞춤` | Pass |
| Existing correction, restore, save, and reopen contracts remain valid | Pass |

## Evidence

Before auto-fit:

- `artifacts\operator-video\20260728-layout-auto-fit-before\evidence\screenshots\failure.png`;
- removing the manual Fit action from the previous EXE exposed the centering
  dependency.

After:

- current-source option view:
  `artifacts\ui\smart-contour-auto-mode-20260728\after\labeling-options-auto-contour-on.png`;
- actual current Debug EXE:
  `artifacts\operator-video\20260728-smart-contour-auto-fit`;
- event 4: `image-auto-fit-observed`, explicitly recording that no Fit action
  was clicked;
- event 7: `candidate-auto-requested`, immediately after rough-box completion;
- saved/reopened result: one polygon, 96 points, 7,931 mask pixels.

The saved candidate measured precision `0.9861`, IoU `0.3927`, and recall
`0.3948` against the retained sample mask. This proves workflow and persistence
safety, not field accuracy.

## Verification

```text
isolated test build: warning 0, error 0
current Debug app build: warning 0, error 0
--mobile-sam-box-prompt: pass
--smart-mask-candidate-compare-restore: pass
--smart-mask-auto-boundary-presentation: pass
--wpf-labeling-shell: pass
--exe-operator-video-smoke --verify-auto-contour-mode --verify-candidate-restore: pass
```

## Boundary

- default for a new/unconfigured Recipe remains regular box mode;
- no automatic candidate approval or save was introduced;
- no Viewer/OpenGL/ROI rendering algorithm was changed;
- the approved public GIF remains unchanged.
