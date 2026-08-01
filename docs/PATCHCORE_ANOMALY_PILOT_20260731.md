# PatchCore Normal-Only Anomaly Pilot

Date: 2026-07-31 KST

Status: Complete

## Scope

Implemented a bounded PatchCore-style anomaly adapter beside the existing
YOLOv8/YOLO11 supervised image classifiers. The adapter learns only from
reviewed normal images and returns an image decision, raw anomaly score,
checkpoint threshold, review-only localization candidates, and a heatmap.

Included:

- selectable `PatchCore` model profile and persisted runtime paths;
- bundled `openvisionlab_patchcore_worker.py` using the existing TCP
  `HealthCheck`, `TrainYolo`, and `DetectImage` transport;
- normal-only readiness and deterministic Recipe split export;
- ImageNet WideResNet50-2 layer2/layer3 patch embeddings;
- full-frame resize preprocessing so border regions are not discarded by a
  classification-style center crop;
- bounded greedy coreset and nearest-neighbour anomaly scoring;
- independent normal-val threshold calibration when available, with an
  explicit train-normal fallback warning;
- self-contained learned checkpoint containing backbone state, memory bank,
  threshold, and provenance counts;
- candidate score/threshold/heatmap transport through C# and review wording;
- D-drive model cache and smoke artifacts.

Excluded:

- automatic label save, candidate confirmation, or model adoption;
- replacing the existing YOLO classification workflow;
- treating abnormal images as PatchCore learning features;
- production accuracy, takt, long-run stability, or commercial-parity claims;
- completing the separate P0-C clean GPU target gate.

## Acceptance criteria

| Criterion | Result | Evidence |
| --- | --- | --- |
| Normal-only learning contract | Pass | `PatchCoreAnomalyTrainingReadinessService`, focused test |
| NG excluded from memory-bank input | Pass | worker reads only `train/normal`; focused source/flow test |
| Real GPU training | Pass | 3 normal train + 1 independent normal validation image |
| Normal held-out decision | Pass | score `0.3795437` <= threshold `0.4214765` |
| Synthetic defect decision | Pass | score `0.5839282` > threshold `0.4214765` |
| Location candidate | Pass | synthetic defect candidate `(82,49,53,29)` after full-frame resize |
| Heatmap artifacts | Pass | normal and abnormal PNG files under D evidence root |
| App contract parsing | Pass | anomaly score, threshold, path, and review-only wording test |
| Real app smoke adapter | Pass | C# `YoloWorkerSmokeTestService` launched the bundled worker and preserved defect score, threshold, location, and heatmap path |
| Build | Pass | 0 warnings, 0 errors |
| Full regression | Pass | default suite `265/265`, exit code 0 |
| Current-source UI | Pass | PatchCore profile captured at 1920x1080 on the dynamically selected leftmost monitor |

## Verification

```text
python -m py_compile Runtime\Python\openvisionlab_patchcore_worker.py
python Runtime\Python\openvisionlab_patchcore_worker.py --self-test --device cuda
python Runtime\Python\openvisionlab_patchcore_worker.py --train-smoke ...
python Runtime\Python\openvisionlab_patchcore_worker.py --smoke-test ...normal-test.png...
python Runtime\Python\openvisionlab_patchcore_worker.py --smoke-test ...abnormal-test.png...
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
LabelingApplication.Tests.exe --patchcore-anomaly-pilot
LabelingApplication.Tests.exe --model-adapter-catalog
LabelingApplication.Tests.exe --wpf-yolo-model-settings-panel
LabelingApplication.Tests.exe --real-patchcore-smoke
LabelingApplication.Tests.exe --priority-workflow-docs
LabelingApplication.Tests.exe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-DocumentationInformationArchitecture.ps1
LabelingApplication.Tests.exe --wpf-visual-smoke --review-tab model --right-workflow-expanded --expand-model-runtime-details --model-engine PatchCore --width 1920 --height 1080 --screen-capture --output <D evidence path>
```

The opt-in real app smoke uses `OPENVISIONLAB_PATCHCORE_SMOKE_ROOT` for the
prepared D evidence root and `OPENVISIONLAB_PATCHCORE_PYTHON` for the verified
Python executable.

## Evidence

- Source worker: `Runtime/Python/openvisionlab_patchcore_worker.py`
- Focused contract test: `tests/LabelingApplication.Tests/Program.AnomalyClassification.cs`
- D smoke root:
  `D:\OpenVisionLab-TestData\Labelling_Application\patchcore-pilot-20260731`
- Learned checkpoint:
  `D:\OpenVisionLab-TestData\Labelling_Application\patchcore-pilot-20260731\runtime\runs\anomaly\pilot\weights\best.pt`
  SHA-256 `E5113D1891C549DF86D0BD09BC87AA40D910DC067C7D4AFEBE0723CCFD5984A5`
- Normal heatmap:
  `D:\OpenVisionLab-TestData\Labelling_Application\patchcore-pilot-20260731\evidence\normal-heatmap.png`
- Abnormal heatmap:
  `D:\OpenVisionLab-TestData\Labelling_Application\patchcore-pilot-20260731\evidence\abnormal-heatmap.png`
- Current-source PatchCore model-settings capture:
  `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\patchcore-anomaly-pilot-20260731\after-patchcore-model-settings-1920.png`
  SHA-256 `DC8808D93EAC30B6195975490925C1282F3848F59437BEF9FE043148FC497BDD`
- Leftmost-monitor placement evidence:
  `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ui\patchcore-anomaly-pilot-20260731\leftmost-monitor-model-settings.json`

A true pre-edit `before` capture is unavailable because the UI impact had
already been implemented before the visual evidence pass. The final evidence
therefore uses only the fresh current-source `after` capture and does not
mislabel a historical screen as a true before image.

## Boundary / next dependency

This proves the bounded implementation and a synthetic GPU smoke only. The
heatmap is persisted as an evidence file. A subsequent explicit, read-only
review window is completed in
`docs/PATCHCORE_HEATMAP_REVIEW_VIEW_20260801.md`; it is not a dedicated Main
Viewer layer. The
next model-development gate is a same-split held-out comparison against the
current YOLOv8/YOLO11 classifiers using approved field images. Required inputs
are provenance-approved normal/defect images, defect acceptance rules, target
hardware, and an agreed false-positive/false-negative threshold. P0-C remains
separately blocked on direct access to the selected GPU-capable clean Windows
target.
