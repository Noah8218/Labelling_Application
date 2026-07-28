# Smart Mask Contextual Correction UX

Date: 2026-07-28
Status: Complete
Product boundary: local single-operator labeling workstation

## 1. Outcome

Smart Mask now leads with the automatic candidate instead of exposing every
correction control on every object.

Default session flow:

1. In a segmentation Recipe, select `라벨링 옵션 · 자동 윤곽` once.
2. Draw one rough Rectangle around the defect.
3. Box completion immediately starts the MobileSAM candidate; no separate
   Smart Mask start click is required.
4. Review the filled automatic candidate.
5. Confirm or skip it when it is already good.
6. Open `보정 옵션` only when the candidate needs correction.
7. Add include/exclude points or change boundary detail, then run
   `후보 다시 생성`.
8. After confirm/skip, the retained mode returns directly to the next box.

The compact default row shows:

- `Smart Mask 자동 후보`;
- candidate/session guidance;
- `보정 옵션`;
- `다음 객체` when the current candidate has been resolved.

The expanded row retains:

- positive include point;
- negative exclude point;
- point undo and clear;
- in-flight generation cancel;
- 48/96/256-point boundary detail.

## 2. State contract

- A new Smart Mask session starts with correction options collapsed.
- Explicit expansion remains open while the same object candidate is rerun.
- Ending the session, changing image, or moving to the next object collapses
  correction options again.
- Collapsing the controls does not clear prompt points or change polygon detail.
- Pending candidates still require explicit confirm/skip and are never
  auto-saved.
- The automatic-contour option is stored at Recipe scope. Reopening restores
  the visible/editable option but does not itself start inference.
- Canvas viewport size changes caused by app layout automatically run
  `ZoomToFit` after layout settles; user zoom/pan alone does not trigger it.
- The existing MobileSAM worker, candidate replacement, canonical segment
  JSON/mask PNG, and confirmation contracts are unchanged.

## 3. Commercial lesson applied

The reviewed V7/CVAT flows make automatic/interactor output the main object and
keep correction tools contextual to a poor result. The local workstation now
applies that disclosure pattern without copying cloud review, task assignment,
video propagation, or team-permission scope.

This is a workflow-density improvement, not a claim of CVAT/V7 feature parity.

## 4. Acceptance evidence

| Criterion | Result | Evidence |
|---|---|---|
| Correction tools hidden by default | Pass | current-source and actual-EXE candidate captures |
| One explicit action expands all correction controls | Pass | ViewModel test and actual-EXE automation |
| Same-session rerun preserves expansion | Pass | `--mobile-sam-box-prompt` |
| Session end restores collapsed state | Pass | `--mobile-sam-box-prompt` |
| Existing automatic filled boundary remains visible | Pass | `--smart-mask-auto-boundary-presentation` |
| Actual candidate can still confirm and save | Pass | actual-EXE run below |

Current-source 1920x1080 evidence:

- `artifacts\ui\smart-mask-contextual-correction-20260728\before-1920x1080.png`
- `artifacts\ui\smart-mask-contextual-correction-20260728\after-collapsed-1920x1080.png`
- `artifacts\ui\smart-mask-contextual-correction-20260728\after-expanded-1920x1080.png`

Actual Debug EXE evidence:

- run: `artifacts\operator-video\20260728-smartmask-contextual-correction`
- sample: KolektorSDD `kos14/Part7.jpg`
- compact candidate:
  `evidence\screenshots\02_initial_candidate.png`
- expanded correction controls:
  `evidence\screenshots\02b_contextual_correction_expanded.png`
- saved result: one polygon, 96 points, 7,931 mask pixels
- recorded flow: 473 human-path cursor moves
- measured against the broad source label: precision `0.9861`, IoU `0.3927`,
  recall `0.3948`

The low IoU/recall is still explained by the broad rectangular source label
versus the tight visible-crack candidate. It is not production-accuracy
evidence.

## 5. Verification

```text
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
PASS, warning 0, error 0

--mobile-sam-box-prompt
PASS

--smart-mask-auto-boundary-presentation
PASS

dotnet build .\OpenVisionLab.LabelingStudio.sln -c Debug /nr:false -m:1 /p:UseSharedCompilation=false
PASS, warning 0, error 0

--exe-operator-video-smoke --verify-contextual-correction
PASS
```

## 6. Public media boundary

The approved public GIF remains
`docs\tutorial\images\github\labeling-studio-smart-mask-workflow.gif`.
It already communicates the preferred auto-first path and should not be
replaced by a longer options-demonstration recording. The new actual-EXE run is
regression evidence, not a new promotional deliverable.

## 7. Correction-effectiveness follow-up

The real correction-effectiveness gate is complete in
`SMART_MASK_CORRECTION_EFFECTIVENESS_20260728.md`.

- positive direction: 6/6;
- negative direction: 4/4 applicable;
- combined held-out improvement: 3/4;
- held-out median IoU delta: `+0.0988`;
- failures retained: 2/6 combined runs.

The next priority is pending-candidate compare/restore because a point
combination can still make the current candidate worse.

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

## 8. Candidate recovery follow-up

Contextual previous/current candidate comparison and restoration are complete
in `SMART_MASK_CANDIDATE_COMPARE_RESTORE_20260728.md`.

The comparison row appears only after a rerun. It does not reopen the default
compact correction controls or change the explicit-confirm/no-autosave
contract.

## 9. Superseding automatic-contour and layout-fit evidence

The normal start flow above supersedes the earlier manual
`박스 → 스마트 마스크` start action. That action remains visible only during an
active session as `후보 다시 생성` for explicit point-correction replay.

Actual current Debug EXE evidence:

- `artifacts\operator-video\20260728-smart-contour-auto-fit`;
- one Recipe-scoped option click;
- workflow-panel width change followed by automatic image centering;
- no `맞춤` action click;
- rough box completion immediately started MobileSAM;
- previous candidate restore, explicit confirmation, save, and reopen passed;
- one saved 96-point polygon and 7,931-pixel mask;
- full visual review found no P0/P1/P2 issue.

This shortens the normal path without changing explicit candidate confirmation,
canonical persistence, or the field-accuracy boundary.
