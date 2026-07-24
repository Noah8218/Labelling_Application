# YOLO11 Anomaly Classification Runtime Closure

Date: 2026-07-23

## Decision

Status: Complete

The operator authorized the missing `yolo11n-cls.pt` download and requested
real training and inference using the supplied 500 OK / 500 NG circular-disk
data. The local YOLO11 classification path now passes seed load, one-epoch
connectivity, 20-epoch training, fixed held-out evaluation, and current EXE
close/restart/first-inference persistence.

Runtime support is complete for the recorded local profile. Model adoption is
separate and remains `hold` because the fixed confidence-gated result is below
the existing quality thresholds and below the same-split YOLOv8 result.

## Locked Input And Runtime

```text
Source images: supplied circular-disk all_images/OK + all_images/NG
Images: 1,000 (500 normal, 500 abnormal)
Split seed: 17
Train: 328 normal / 349 abnormal
Validation: 118 normal / 101 abnormal
Test: 54 normal / 50 abnormal
Test fingerprint: 787b7f18fc30936132dfdd1a29906c789b188c1cccf53a5b9888a393bc563fe3
Image size: 128
Batch: 16
Epochs: 1 connectivity, then 20 comparison
Python: C:\Git\yolov8\.venv\Scripts\python.exe
Ultralytics: 8.4.101
Worker: Runtime\Python\openvisionlab_ultralytics_worker.py
Worker SHA-256: 2FE5E940C4AA442660D4E1BF2F7AF7837137C5966853E9FCA9D8CE9F34AC7CEB
Seed SHA-256: C62D41BF9625777760018BF914D2E6CD472420CCD01706D97A61CB6C82502BD7
20-epoch best.pt SHA-256: 4DFFF846D38938F483E6A8A7A96F2856DF2C64DFF581F607E1E179C983503458
```

The source tree stayed at 1,000 files with the identical pre/post SHA-256
`84E41C5DBE77711B3FF2B96DEE8E50DBBF5253C90B95C53CAF24978D6CD1D846`.

## Held-Out Result

At the locked minimum confidence `0.8`:

| Model | Correct | Overall | Normal | Abnormal | Decision |
| --- | ---: | ---: | ---: | ---: | --- |
| YOLOv8 | 90/104 | 86.5% | 52/54 | 38/50 | hold |
| YOLO11 | 82/104 | 78.8% | 43/54 | 39/50 | hold |

YOLO11 raw top-1 was 95/104, but 13 class-matching predictions were below the
required confidence. The guard correctly held the candidate because overall,
normal, and abnormal confidence-gated accuracy did not pass all thresholds.
Training completion therefore did not replace the current inspection model.

## Product Fixes Proven By The Run

- The shared app training service successfully sent
  `model=yolo11`, `task=classify`, and `weight=yolo11n-cls.pt`.
- The persistent batch evaluator now supplies the image-root field required by
  the bundled Ultralytics worker.
- Model Center anomaly evaluation accepts the verified YOLOv8 and YOLO11 local
  runtimes while rejecting other engines.
- The read-only Model Adapter Catalog records YOLO11 classification as a
  verified runtime path and keeps its current quality decision as `hold`.
- The actual EXE reopened the saved YOLO11 profile, inferred a held-out NG image,
  returned an `abnormal` image-level candidate, and persisted `Abnormal`.

## Durable Closure

Status: Complete

Scope: recorded local YOLO11 anomaly-classification seed, training, held-out
evaluation, Model Center evaluation route, and EXE restart inference.

Acceptance criteria: seed load/self-test (pass); one-epoch connectivity (pass);
20-epoch training (pass); fixed held-out evaluation (pass, decision `hold`);
source immutability (pass); current EXE restart/inference persistence (pass);
YOLOv8/YOLO11 comparison on the identical test fingerprint (pass).

Verification: real app-service/TCP training artifacts, persistent batch
evaluation summary, focused C#/Python gates, current solution build, and actual
EXE restart smoke.

Evidence:

- `artifacts/real-yolo11-anomaly-folder-training/circular-disk-supplied-1000-e1-20260723-1318`
- `artifacts/real-yolo11-anomaly-folder-training/circular-disk-supplied-1000-e20-20260723-1321`
- `artifacts/exe-yolo11-anomaly-restart-smoke/current-source-final2-20260723`
- `artifacts/ui/20260723-yolo11-anomaly-closure`

Boundary / next dependency: this is same-source procedural/synthetic evidence.
It proves the product workflow and the recorded local adapter, not independent
camera generalization or production accuracy. Acquire balanced independent
normal/abnormal camera-session data before reconsidering model adoption.
