# Smart Mask Candidate Compare and Restore

Date: 2026-07-28
Status: Complete
Field validation: Not evaluated
Product boundary: local single-operator labeling workstation

## 1. Outcome

A Smart Mask rerun no longer destroys the operator's only view of the initial
automatic candidate.

The current object session retains:

- the first automatic candidate;
- the latest rerun candidate;
- which of those two is currently selected for review.

After a rerun, the compact Smart Mask row contextually exposes:

- `이전 후보 보기`;
- `현재 후보 보기`;
- `확정하면 이 후보만 저장` guidance.

The operator can switch repeatedly without changing annotation dirty state or
writing a label. Candidate Review continues to show exactly one pending
candidate, and its existing explicit Confirm action saves only the selected
version.

## 2. State and ownership contract

| State | Session history | Candidate Review | Disk |
| --- | --- | --- | --- |
| First automatic result | initial=current | one pending candidate | unchanged |
| Rerun result | initial + latest | latest pending | unchanged |
| Previous candidate selected | initial + latest | initial pending | unchanged |
| Current candidate selected | initial + latest | latest pending | unchanged |
| Confirm selected | comparison cleared | selected candidate confirmed | selected candidate only |
| Skip selected | comparison cleared | no pending candidate | unchanged |
| Image/Recipe/session change | comparison cleared | existing owner applies its normal reset | unchanged by comparison |

Ownership remains:

- prompt identity, points, and session-only initial/latest references:
  `WpfSmartMaskPromptSessionService`;
- comparison presentation and command state:
  `WpfCanvasPanelViewModel`;
- replacement of the one visible pending candidate:
  Smart Mask shell adapter;
- confirmation and canonical segment JSON/mask PNG:
  existing Candidate Review and annotation persistence services.

No second persistence store, automatic score, automatic winner, or hidden
autosave was introduced.

## 3. Operator checklist

1. Draw a rough defect box and create the automatic candidate.
2. If it is poor, add one correction point and rerun.
3. Use `이전 후보 보기` and `현재 후보 보기`.
4. Visually choose the better boundary.
5. Confirm only the selected candidate, or skip the object.
6. Add another correction point only if both candidates remain inadequate.

## 4. Acceptance evidence

| Criterion | Result | Evidence |
| --- | --- | --- |
| Retain first and latest only in the current session | Pass | `--mobile-sam-box-prompt` |
| Contextual previous/current actions after rerun | Pass | ViewModel test and 1920x1080 current-source captures |
| Restore previous candidate as the one pending candidate | Pass | `--smart-mask-candidate-compare-restore` |
| No canonical file before confirmation | Pass | focused integration artifact |
| Save only explicitly restored candidate | Pass | saved segment first point matches restored candidate |
| Clear history on confirm/skip/reset/context change | Pass | service and shell focused assertions |
| Preserve Candidate Review/no-autosave ownership | Pass | focused integration and source contract assertions |
| Real MobileSAM correction rerun exposes previous candidate | Pass | actual Debug EXE operator replay |
| Restore, confirm, save, navigate away, and reopen | Pass | one saved 96-point polygon and 7,931-pixel mask |

Focused integration evidence:

`artifacts\smart-mask-candidate-compare-restore\20260728-210121`

Important file:

`candidate-compare-restore-evidence.json`

Current-source 1920x1080 evidence:

- closest reproducible pre-feature baseline:
  `artifacts\ui\smart-mask-candidate-compare-restore-20260728\closest-baseline-1920x1080.png`;
- latest candidate selected:
  `artifacts\ui\smart-mask-candidate-compare-restore-20260728\after-current-candidate-1920x1080.png`;
- previous candidate restored:
  `artifacts\ui\smart-mask-candidate-compare-restore-20260728\after-previous-candidate-restored-1920x1080.png`.

The dedicated pre-edit Smart Mask generation capture stopped before producing a
candidate, so the verified same-source contextual-correction image is retained
as the closest baseline rather than being mislabeled as a fresh exact before
capture.

Actual Debug EXE restore/save/reopen evidence:

`artifacts\operator-video\20260728-smartmask-restore-save-retry1`

- actual sample: KolektorSDD `kos14/Part7.jpg`;
- real sequence: automatic candidate -> one positive/negative correction
  rerun -> previous candidate restore -> explicit Confirm -> next incomplete
  image -> original image reopen;
- saved canonical result: one polygon, 96 points, 7,931 non-zero mask pixels;
- saved mask IoU / precision / recall against the public ground-truth mask:
  `0.3927 / 0.9861 / 0.3948`;
- candidate restore: pass;
- exactly one saved label after reopen, with no pending Smart Mask
  confirmation: pass;
- application-only MP4: 1920x1080, 78.5 seconds, SHA-256
  `D92CAEBCDF5D5391211EA459AFB19B2DFADF00132AB9455711CCD353266D7B86`;
- full-duration visual review found no P0/P1/P2 issue. Two non-blocking P3
  observations are retained under the run's `review` folder.

## 5. Verification

```text
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj
  -c Debug /nr:false -m:1 /p:UseSharedCompilation=false
  /p:OutDir=artifacts\isolated-out\
PASS, warning 0, error 0

--mobile-sam-box-prompt
PASS

--smart-mask-candidate-compare-restore
PASS

--wpf-visual-smoke --smart-mask-candidate-comparison
PASS, 1920x1080

--wpf-visual-smoke --smart-mask-candidate-comparison
  --smart-mask-show-initial-candidate
PASS, 1920x1080

dotnet build .\OpenVisionLab.LabelingStudio.csproj -c Debug
  /nr:false -m:1 /p:UseSharedCompilation=false
  /p:OutDir=artifacts\run\Debug\
PASS, warning 0, error 0

--exe-operator-video-smoke --verify-candidate-restore
  --run-id 20260728-smartmask-restore-save-retry1
PASS, candidateRestore=True, reopen=True
```

## 6. Commercial lesson and boundary

The applicable CVAT/V7 lesson is fast correction recovery without adding every
possible control to the permanent toolbar. Comparison appears only after a
rerun and remains part of the same pending-review context.

This proves session safety and canonical selected-version persistence. It does
not prove:

- that an operator will select an optimal correction point;
- that either candidate is accurate on production-camera data;
- side-by-side difference visualization;
- video propagation, tracking, cloud review, assignment, or CVAT/V7 parity.

## 7. Closure and next dependency

The actual Debug EXE restore/save/reopen gate is Complete. Do not repeat it
unless Smart Mask session selection, confirmation, persistence, the MobileSAM
runtime, or the retained evidence validity changes.

The saved result has high precision but low recall and IoU. This closes the
workflow safety gate, not model quality or field accuracy. The next maintenance
priority is to synchronize current operator/source-of-truth documents so older
Smart Mask and completed P3/P4/P5 wording cannot be selected as future work.
The next unimplemented editor-product contract after documentation alignment is
four-point box creation.

Recommended model: `gpt-5.6-terra`
Reasoning effort: `low`
