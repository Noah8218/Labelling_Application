# P1-I Anomaly Evaluation Error Worklist

Date: 2026-08-03 KST

Status: Complete

## Goal

Turn the existing YOLOv8, YOLO11, and PatchCore image-level `samples` evidence
into an operator-readable, read-only Model Benchmark worklist. An operator can
now move from aggregate balanced accuracy and error counts to the exact images
that produced those outcomes without rerunning inference or changing labels.

## Operator workflow

1. Open an anomaly Recipe and run evaluation for one or more supported models.
2. Open `모델 성능 비교` from Model Center.
3. Confirm that the selected runs use the same data fingerprint.
4. In `요약`, compare `정답`, `오검출`, and `미검출` counts for each run.
5. Open `클래스/오류` and review `이미지별 판정 결과`.
6. Select a row to inspect the recorded source image and its saved details.

Example interpretations:

- `오검출(FP) · 정상 → 이상`: a normal image was classified as abnormal;
- `미검출(FN) · 이상 → 정상`: an abnormal image was classified as normal;
- `임계값 미달 · 이상 → 이상`: the class matched, but the saved confidence
  did not meet the decision threshold;
- PatchCore rows show anomaly score, checkpoint threshold, location count, and
  whether a heatmap path was recorded.

## Included contract

- reuse `classification-evaluation-summary.json`; do not create a second
  report or rerun inference;
- load only summaries already selected by the existing evidence-fingerprint
  comparison contract;
- show errors and threshold failures before correct results across all selected
  runs;
- bound the visible worklist to 500 stored sample rows, preserving the report's
  aggregate counts when more samples exist;
- keep the source-image preview lazy and read-only;
- distinguish class mismatch from class match below threshold;
- retain PatchCore heatmap path as hover evidence and show score, threshold,
  location count, and heatmap presence in the selected-row detail;
- keep detection-report TP/FP/FN labels unchanged while anomaly summary rows
  use `정답`, `오검출`, and `미검출`.

## Explicit exclusions

- no automatic image navigation in the labeling workspace;
- no label, review-state, candidate, class, or Recipe mutation;
- no heatmap Main Viewer layer;
- no threshold edit, retraining, rerun, model registration, or adoption;
- no field-quality or model-superiority claim.

## Verification

```text
Build-LabelingApplicationTests.ps1 -OutputName isolated-out -> PASS, 0 warnings/errors
--wpf-model-benchmark-window -> PASS
--anomaly-classification-evaluation -> PASS
--patchcore-anomaly-pilot -> PASS
default protected 267/267 regression baseline -> PASS, exit code 0
--wpf-visual-smoke summary, 1920x1080 -> PASS
--wpf-visual-smoke class/error, 1920x1080 -> PASS
--wpf-visual-smoke class/error, 1366x768 -> PASS
```

Actual-EXE captures used the dynamically selected leftmost active monitor
`\\.\DISPLAY1`, bounds `0,0,1920,1080`. The 1920 window occupied the full
monitor; the 1366x768 window was verified at `277,156,1366,768`.

Evidence root:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\anomaly-evaluation-error-worklist-20260803`

- `before-empty-anomaly-outcome-1920.png`;
- `after-anomaly-outcome-summary-1920.png`;
- `after-anomaly-image-outcomes-1920.png`;
- `after-anomaly-image-outcomes-1366.png`.

## Boundary / next dependency

This worklist makes saved local evidence reviewable. It does not improve or
prove model quality. Production selection still requires provenance-approved,
content-separated field images, acceptance thresholds, intended runtime and
weights, target hardware, and region ground truth when localization accuracy
is claimed.
