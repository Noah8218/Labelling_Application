# Field Data Intake Prerequisite

Date: 2026-07-28

Status: Complete

## Purpose

This document defines the smallest auditable input packet required before
OpenVisionLab Labeling Studio resumes field model-quality work. It prevents
synthetic, duplicated, same-session, or weakly labeled evidence from being
reported as production-camera generalization.

This is an intake and authorization contract. It does not authorize training,
comparison, model registration, adoption, or source-data mutation by itself.

## Current readiness audit

Checked in this audit:

- the current repository status and current handoff/status documents;
- immediate candidate directory names under `C:\Git`;
- the user-provided commercial-reference folder
  `C:\Git\GoPxL_Video\새 폴더`.

Observed:

- no newly approved, provenance-confirmed field dataset was identified in the
  checked scope;
- the commercial-reference folder contains 10 MP4 files, 13 SRT files, and
  4 VTT files. It is product-workflow evidence, not labeled camera evidence;
- existing EasyMatch, circular, Washer, MultiIndustry, and `D:\라벨테스트`
  records remain synthetic, same-source, or provenance-ineligible for a new
  production claim.

Not checked:

- the full workstation or arbitrary drives;
- any path that the handoff explicitly prohibits inspecting;
- source contents that the user has not approved for model evaluation.

## Required common manifest

Provide one `acquisition-manifest.yaml` beside the packet:

```yaml
packet_id: line-a-camera-2-session-20260728
task: detection # detection | segmentation | anomaly
product_or_part: ""
site_or_line: ""
camera_model: ""
lens_and_working_distance: ""
lighting: ""
resolution: "WIDTHxHEIGHT"
exposure_and_gain: ""
acquisition_sessions:
  - session_id: ""
    captured_at: ""
    operating_condition: ""
    image_count: 0
label_policy_version: v1
label_author: ""
review_author: ""
usage_approved_by: ""
usage_scope: evaluation-only
known_exclusions: []
```

Required rules:

- `packet_id` and every `session_id` are stable and unique.
- Capture sessions describe actual acquisition boundaries, not folders created
  after capture.
- Original files are read-only. Derived labels, manifests, runtime copies, and
  reports use application-owned destinations.
- Label author and reviewer are different people when a production adoption
  claim is intended. If they are the same, record that limitation.
- Unknown fields remain empty and are treated as missing evidence; they are not
  guessed.

## Detection packet

Required layout:

```text
packet-root/
  acquisition-manifest.yaml
  data.yaml
  images/
    train/
    val/
    test/
  labels/
    train/
    val/
    test/
  label-policy.md
```

Acceptance checklist:

- every defect class and bounding-box inclusion/exclusion rule is defined in
  `label-policy.md`;
- every labeled box has a reviewed class and tightness decision;
- background/OK images have an explicit reviewed empty label;
- SHA-256 duplicate groups do not cross train/val/test;
- the held-out `test` split contains a separate acquisition session wherever
  possible;
- test labels are frozen before candidate comparison;
- folder-level OK/NG names are never converted into fabricated boxes;
- class support is sufficient to place reviewed examples in every required
  split. If it is not, the packet is intake evidence only.

## Segmentation packet

Use the same YOLO image/label layout with polygon labels or the application
canonical segment/mask export.

Additional checklist:

- polygon or mask rules define boundary ambiguity, holes, disconnected
  components, touching instances, and ignore regions;
- saved overlays are visually reviewed, not accepted from file counts alone;
- different-class overlap is either prohibited or explicitly defined;
- at least five reviewed positive mask images and five reviewed background
  images are required as the existing comparison eligibility floor. This is
  not a production-readiness sample-size claim;
- the held-out test split is not used for threshold tuning or geometry repair.

## Anomaly packet

Required layout:

```text
packet-root/
  acquisition-manifest.yaml
  train/
    normal/
    abnormal/
  val/
    normal/
    abnormal/
  test/
    normal/
    abnormal/
  decision-policy.md
```

Acceptance checklist:

- normal and abnormal decisions follow a written defect policy;
- both classes are present in the held-out test set;
- the external test remains outside training initially;
- SHA-256 duplicate groups and source-derived crops do not cross splits;
- normal and abnormal examples cover representative acquisition conditions;
- thresholds and minimum per-class accuracy are frozen before test evaluation.

## Content-separation gate

Before any training or comparison:

1. inventory relative path, byte size, image dimensions, session ID, class, and
   SHA-256;
2. group byte-identical contents and derived crops;
3. reject cross-split duplicates;
4. compare the new packet fingerprint against preserved historical evaluation
   fingerprints;
5. freeze the test manifest and record its SHA-256;
6. do not tune models, thresholds, or labels from test errors; corrections
   require a new evidence version.

## Execution sequence after approval

1. Read-only intake and provenance audit.
2. Dataset Health and selected-image overlay review.
3. Freeze content-separated test evidence and its fingerprint.
4. Train only after the user approves runtime cost and engine scope.
5. Compare baseline and candidate under identical recorded settings.
6. Review false positives, false negatives, class confusion, and background
   behavior.
7. Return `promote`, `review`, or `hold`; never auto-adopt.
8. Register a model only after an explicit operator decision.

Existing command templates, to be filled only after approval:

```powershell
# Detection or segmentation comparison.
.\scripts\compare-yolo-models.ps1 `
  -DataYaml "<approved-data.yaml>" `
  -BaselineWeights "<baseline-weight>" `
  -CandidateWeights "<candidate-weight>" `
  -ModelTask detect `
  -Task test `
  -ImageSize 320 `
  -BatchSize 1 `
  -BenchmarkRepeatCount 5 `
  -UiConfidence 0.25 `
  -OutputDirectory "<artifact-output>"

# Anomaly classification held-out evaluation.
.\scripts\evaluate-yolo-classification.ps1 `
  -Weights "<candidate-weight>" `
  -DatasetRoot "<approved-packet-root>" `
  -Split test `
  -ImageSize 320 `
  -Confidence 0.8 `
  -OutputDirectory "<artifact-output>"
```

The exact engine, image size, confidence, repetition count, and acceptance
thresholds must be agreed and recorded before running. The examples above are
templates, not automatic defaults for an unknown packet.

## Intake approval checklist

```text
[ ] Source path and usage were explicitly approved.
[ ] acquisition-manifest.yaml is complete enough to identify sessions.
[ ] Label/decision policy is present.
[ ] Class names and semantic rules are unambiguous.
[ ] Image-label pairs open successfully.
[ ] Saved overlays were visually sampled.
[ ] Duplicate and derived-content groups were audited.
[ ] No group crosses train/val/test.
[ ] Test evidence is from an independent session where feasible.
[ ] Test manifest and source tree fingerprints were recorded.
[ ] Training/runtime cost and engines were explicitly approved.
[ ] The output is evaluation evidence, not automatic adoption.
```

## Completion record

Status: Complete

Scope: Audit the currently identified candidate locations and define the
reusable field-data packet required for detection, segmentation, or anomaly
quality work.

Acceptance criteria:

- current known evidence boundaries preserved: pass;
- commercial-reference videos not misclassified as model data: pass;
- prohibited and unapproved paths not inspected: pass;
- reusable provenance, layout, leakage, review, and approval templates
  provided: pass.

Verification: documentation policy gate and `git diff --check`.

Evidence: this document and the 2026-07-28 read-only extension inventory of the
commercial-reference folder.

Boundary / next dependency: supply and explicitly approve one packet satisfying
the relevant checklist.

Recommended model: no model tokens until an eligible packet is available.

Reasoning effort: not applicable until the prerequisite exists.
