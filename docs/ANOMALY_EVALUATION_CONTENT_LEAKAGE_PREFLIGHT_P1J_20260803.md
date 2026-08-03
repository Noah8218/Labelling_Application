# P1-J Anomaly Evaluation Content-Leakage Preflight

Date: 2026-08-03 KST

Status: Complete

## Goal

Prevent an anomaly evaluation worker from running when exact image content is
reused across `train`, `valid`, and `test`, or when one split contains the same
content under both `normal` and `abnormal`. This closes an evidence-integrity
gap that a shared test fingerprint alone cannot detect.

## Operator workflow

1. Review normal and abnormal images and choose `평가 실행` in Model Center.
2. The app exports the current anomaly splits as before.
3. Before PowerShell or the Python worker starts, the app checks exact file
   length plus SHA-256 content identity.
4. If leakage is found, evaluation is blocked. The status and log identify the
   affected split/class scope, an example pair, the directories to inspect,
   and the corrective action.
5. After the duplicate is removed from the wrong split or class, run the
   evaluation explicitly again.

Example blocked scopes:

- `train/test 분할`: the same captured image would train and test the model;
- `test normal/abnormal`: the same image would carry conflicting answer
  classes in one evaluation split.

## Included contract

- reuse `YoloExternalEvaluationDataAuditService`; do not create a second hash
  engine or report format;
- compare all populated split pairs: `train/valid`, `train/test`, and
  `valid/test`;
- compare `normal` against `abnormal` inside each populated split;
- detect content by file length plus SHA-256 even when file names differ;
- fail closed when a populated audit scope contains an unreadable image;
- validate in `WpfAnomalyClassificationEvaluationRunService` before
  `Process.Start`, so direct service callers cannot bypass the gate;
- keep the shell's immediate validation and actionable log presentation;
- restore normal execution after the conflicting content is removed.

## Explicit exclusions

- no perceptual or near-duplicate detection;
- no domain-shift, camera/session provenance, or scene-family analysis;
- no automatic file move, deletion, relabel, split reassignment, or review-
  state mutation;
- no threshold change, training, inference, model registration, or adoption;
- no field-quality or model-superiority claim.

## Acceptance criteria

| Criterion | Result | Evidence |
| --- | --- | --- |
| Clean exported anomaly request remains valid | Pass | focused test validates zero errors before and after leakage fixtures |
| Renamed train/test exact duplicate is blocked | Pass | focused test copies test content under a different train name and receives `train/test` plus `SHA-256` guidance |
| Same test image in normal and abnormal is blocked | Pass | focused test receives `test normal/abnormal` plus `SHA-256` guidance |
| Worker cannot start through direct `RunAsync` | Pass | direct call returns the validation failure before process execution |
| Existing workflows remain stable | Pass | protected default suite passed 267/267, exit code 0 |
| Operator can read the blocked state | Pass | current-source Wide and Compact WPF captures show the dark themed expanded execution log |

## Verification

```text
Build-LabelingApplicationTests.ps1 -OutputName isolated-out -> PASS, 0 warnings/errors
--anomaly-classification-evaluation -> PASS
default protected regression -> PASS, 267/267, exit code 0, 362.3 seconds
--wpf-visual-smoke leakage log, 1920x1080 -> PASS
--wpf-visual-smoke leakage log, 1366x768 -> PASS
```

The UI captures used the only active display, which is also the dynamically
selected leftmost display: `\\.\DISPLAY1`, bounds `0,0,1920,1080`. The
1920x1080 window filled that display; the compact capture used 1366x768. Both
retain the supported dark theme and show the actionable leakage message
without an unthemed control leak.

Evidence roots:

- logs:
  `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\logs\anomaly-evaluation-leakage-preflight-20260803`;
- UI:
  `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\anomaly-evaluation-leakage-preflight-20260803`.

Relevant captures:

- true pre-change workflow baseline:
  `before-anomaly-evaluation-ready-1920.png`;
- blocked leakage log, Wide:
  `after-anomaly-evaluation-leakage-log-visible-1920.png`;
- blocked leakage log, Compact:
  `after-anomaly-evaluation-leakage-log-visible-1366.png`.

## Boundary / next dependency

This gate proves only exact byte-content separation inside the app-exported
evaluation dataset. Production selection still requires provenance-approved
field images, acceptance thresholds, intended runtime and weights, target
hardware, and defect-region ground truth when localization accuracy is
claimed. Near-duplicate and domain-shift analysis remain separate future
contracts that require a measured field-data problem.
