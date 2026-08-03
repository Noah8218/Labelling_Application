# P1-H Anomaly Same-Split Comparison

Date: 2026-08-03 KST

Status: Complete

## Goal

Evaluate YOLOv8, YOLO11, and PatchCore image-level anomaly decisions on the
same held-out normal/abnormal image content, then compare their errors and
timing in the existing read-only Model Benchmark window without retraining,
relabeling, or automatically adopting a model.

## Operator workflow

1. Open an anomaly Recipe and save Normal/Abnormal review decisions.
2. Select a verified YOLOv8, YOLO11, or PatchCore model profile.
3. In Model Center, run `평가 실행` for that model.
4. Repeat steps 2-3 for the other models without changing the reviewed test
   images or split.
5. Open `모델 성능 비교`. The newest anomaly summary and every current-project
   anomaly run with the same evidence fingerprint are selected together.
6. Check balanced accuracy, normal false positives, abnormal misses, timing,
   model/worker hashes, decision thresholds, and PatchCore review evidence.

The comparison does not compare YOLO class confidence numerically with a
PatchCore anomaly score. YOLO uses the configured class-confidence gate;
PatchCore uses the threshold stored in its checkpoint.

## Included contract

- the existing classification evaluation export remains the single held-out
  `test/normal` and `test/abnormal` source;
- one SHA-256 class/content fingerprint identifies comparable image evidence;
- the shared persistent-adapter batch evaluator supports YOLOv8, YOLO11, and
  PatchCore;
- each summary records model name, weights and worker SHA-256, model root,
  image size, device, machine/CPU/GPU inventory, elapsed time, and decision
  threshold source;
- metrics include accuracy, balanced accuracy, normal false positives,
  abnormal misses, and model-specific evidence counts;
- PatchCore heatmaps are written below the evaluation run, not beside the
  checkpoint;
- PatchCore locations remain review evidence. When no location ground truth
  exists, `localization.groundTruthStatus` is `not-evaluated`;
- the Model Benchmark catalog includes current-project evaluation history and
  preselects matching-fingerprint anomaly runs;
- all reports are read-only. No label, candidate, Recipe model, or adoption
  history changes automatically.

## Explicit exclusions

- no Main Viewer heatmap layer;
- no automatic contour confirmation or label save;
- no automatic threshold tuning, training, model registration, or adoption;
- no raw YOLO-confidence versus PatchCore-score ranking;
- no production accuracy, long-run stability, or commercial-parity claim.

## Current synthetic smoke evidence

The retained PatchCore pilot checkpoint and two content-verified test images
were copied from the recoverable E-drive snapshot into the canonical D-drive
test root before execution. All three copies passed length and SHA-256 checks.

Evidence root:

`D:\OpenVisionLab-TestData\Labelling_Application\anomaly-comparison-real-smoke-20260803`

Common evidence fingerprint:

`6baabdd0383f8ad1193e0f2520b94d0b59e5b527e077806e5a72301286ea807e`

| Model | Image decision | Balanced accuracy | Normal FP | Abnormal miss | Location / heatmap evidence | Average wall time |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| PatchCore | 2/2 | 100% | 0 | 0 | 1 / 2 | 4846.59 ms/image |
| YOLOv8 | 0/2 | 0% | 1 | 1 | 0 / 0 | 4778.06 ms/image |
| YOLO11 | 0/2 | 0% | 1 | 1 | 0 / 0 | 4428.13 ms/image |

This is a two-image CPU connectivity smoke. It uses model-load-inclusive wall
time and images that were prepared for the PatchCore pilot, not independent
field data for the YOLO classifiers. The table proves the common execution and
evidence contract only; it does not rank model quality or production Takt.

The first attempted run used the restored `.venv-gpu`, which contained none of
the required PatchCore packages and failed before model load. The successful
run used the dependency-complete CPU `.venv`; this recovery is environment
evidence, not a hidden package installation.

## Verification

Completed gates:

```text
Build-LabelingApplicationTests.ps1 -OutputName isolated-out -> PASS, 0 warnings/errors
--anomaly-classification-evaluation -> PASS
--wpf-model-benchmark-window -> PASS
--patchcore-anomaly-pilot -> PASS
--priority-workflow-docs -> PASS
openvisionlab_yolo_classification_batch.py --self-test -> PASS
PowerShell parser check: evaluate-yolo-classification.ps1 -> PASS
--wpf-visual-smoke, Model Center -> PASS
--wpf-visual-smoke, three-model benchmark -> PASS, selected 3/6
default protected 267/267 regression baseline -> PASS, exit code 0
Test-DocumentationInformationArchitecture.ps1 -> PASS, 109/109 classified
                                                   0 broken links/duplicates
```

Current-worker summaries:

- PatchCore: `classification-evaluation-20260803-143520`;
- YOLOv8: `classification-evaluation-20260803-143649`;
- YOLO11: `classification-evaluation-20260803-143714`.

All three are below the evidence root's `output` directory and declare the
same fingerprint recorded above.

Current-source actual-EXE visual evidence:

- before: `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\anomaly-same-split-comparison-20260803\before-anomaly-evaluation-1920.png`;
- after Model Center: `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\anomaly-same-split-comparison-20260803\after-anomaly-evaluation-1920.png`;
- after three-model benchmark: `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\anomaly-same-split-comparison-20260803\after-three-model-benchmark-fixed-1920.png`.

Both after captures used the dynamically selected leftmost active monitor
`\\.\DISPLAY1`, bounds `0,0,1920,1080`, and verified the actual window at
`0,0,1920,1080`. The only supported dark theme remained consistent.

## Boundary / next dependency

Production selection remains blocked until provenance-approved, content-
separated normal/defect camera-session images, acceptance thresholds, intended
weights/runtime, and target hardware are available. Location quality also
requires defect-region ground truth; a heatmap or candidate box alone is not a
localization accuracy measurement.
