# Labeling Studio User-Centered Development Direction

Date: 2026-07-29 KST

Status: `Complete` for the analysis and durable direction record. The product
changes listed below are not complete until their own implementation and
verification records pass.

## 1. Source And Evidence Boundary

This direction was selected after comparing live source at
`e1465d6 feat: streamline smart mask labeling workflow` with:

- the current product, handoff, completeness, structure, stable-area, and work
  tracking documents;
- current-source and actual-EXE UI evidence for Labeling, Dataset Health,
  Dataset Interchange, Batch AI, Dataset Version, Smart Mask correction,
  candidate restore, automatic contour, and canvas layout auto-fit;
- the full timelines of ten supplied V7, Label Studio, CVAT, Supervisely,
  Encord, LandingLens, and MVTec DLT videos, approximately 58 minutes total,
  using their supplied subtitles when available.

This is workflow and product-direction evidence. It is not production model
accuracy, commercial parity, or a field-adoption decision. The focused local
workstation estimate remains `4.0/5`; labeling-editor depth remains `3.4/5`.

## 2. Product Direction

Keep OpenVisionLab Labeling Studio as a local Windows, single-operator
industrial image labeling, training, inference, review, and model-evidence
workstation.

The shortest safe normal workflow is the design unit:

```text
Dataset and purpose
-> visible class and canonical index
-> create or review a label
-> explicit save state
-> Dataset QA
-> readiness and Dataset Version
-> explicit training/inference
-> candidate review
-> evidence-backed hold/review/adopt decision
```

Reuse settings only at their narrowest valid scope. Recipe-scoped automatic
contour is the reference pattern: it is visible, editable, resettable, restored
without starting inference, and does not confirm or save a candidate.

Commercial tools teach the project to preserve:

- fast repeat actions and stable tool/class context;
- immediate but unconfirmed AI feedback;
- local correction next to the selected object;
- problem-first queues and class/split review;
- explicit scope, model, threshold, and existing-label policy before batch
  work.

Do not copy commercial platform breadth. Collaboration, accounts, cloud sync,
reviewer assignment, video tracking, 3D/keypoints, camera/lighting/PLC/I/O,
deployment, arbitrary-model marketplaces, and automatic candidate approval or
save remain outside the current direction.

## 3. Newly Confirmed Priority Order

### P0. Safe Application Close

Status: `Complete` on 2026-07-29.

Implementation and actual-EXE evidence:
`docs/SAFE_APPLICATION_CLOSE_P0_20260729.md`.

Why it is first:

- image navigation already calls the canonical pending-annotation save path;
- the main window currently binds a post-close `ClosedCommand` that saves
  workspace layout, cancels background work, closes auxiliary windows, and
  disposes resources;
- no cancelable main-window `Closing` path was found that resolves dirty
  annotations, pending mask-stroke commit work, or an unconfirmed Smart Mask
  candidate before resources are released.

This is a source-identified loss-risk candidate, not a runtime-reproduced loss
claim. The first implementation task must reproduce and freeze the current
close behavior in a focused test before changing it.

Required outcome:

```text
Clean state
-> close directly

Dirty saved-label state
-> Save and close | Discard and close | Cancel

Unconfirmed AI candidate only
-> never convert or save automatically
-> Discard candidate and close | Cancel

Training, inference, or Batch AI active
-> show the exact active work
-> request explicit stop/close confirmation
-> cancel and wait through the existing bounded cleanup path
```

Included:

- cancelable close policy and presentation;
- reuse of the existing canonical annotation-save command;
- explicit treatment of dirty label state, pending mask-stroke work,
  unconfirmed candidates, and active long-running work;
- save failure leaves the window open;
- closing an already clean idle window adds no extra dialog.

Excluded:

- automatic candidate confirmation;
- unconditional auto-save on application exit;
- changes to annotation formats, Viewer/OpenGL, ROI, brush, or eraser hot
  paths;
- new background persistence or recovery frameworks.

Ownership:

- WPF window remains the `Closing` event adapter;
- reusable decision/state policy belongs in the nearest existing
  Infrastructure or Annotation service only if it forms an independently
  testable policy used by more than one close path;
- existing annotation persistence, candidate, training, runtime, and batch
  owners retain their data and cancellation responsibilities.

Acceptance evidence:

- focused clean/dirty/save/discard/cancel/save-failure close tests;
- pending Smart Mask candidate is never written as a label;
- pending mask-stroke state is either committed through the canonical path or
  the close is canceled;
- active batch/training/inference state names the work and follows existing
  cancellation boundaries;
- actual current Debug EXE dirty-label close, cancel, save, reopen, and
  candidate-only close evidence;
- isolated build, relevant focused gates, `git diff --check`, and fresh
  1920x1080 before/after UI evidence.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`

### P1. Canonical Class Index Visibility

Status: `Complete` on 2026-07-29.

Implementation and current-build visual evidence:
`docs/CANONICAL_CLASS_INDEX_VISIBILITY_P1_20260729.md`.

Why it follows:

- the class catalog previously displayed items alphabetically;
- rename preserves the underlying YOLO class order;
- the visible list therefore does not explain the canonical index that
  save/export/training and external class mapping use.

Required outcome:

- show the canonical index with the class name in Class Catalog and the active
  class summary, for example `0 · OK`, `1 · Scratch`;
- explain that rename preserves index and that changing the schema is different
  from selecting a drawing shortcut;
- keep the underlying ordered class contract unchanged;
- do not add drag reorder until a separate schema-migration contract exists.

Acceptance evidence:

- add/rename/delete/save/reopen preserves and displays the expected index;
- `data.yaml`, YOLO save/export, Batch AI class mapping, and class shortcuts
  remain consistent;
- 1920x1080 and 1366x768 show the index without clipping;
- focused class-catalog and workflow documentation gates pass.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`

### P2. Operator Documentation Truth Synchronization

Status: `Complete` on 2026-07-29.

Completion record:
`docs/SMART_MASK_OPERATOR_DOCUMENTATION_TRUTH_P2_20260729.md`.

Confirmed stale statements:

- `README.md` still says point prompts are a future expansion;
- `docs/tutorial/README.md` and `docs/MOBILE_SAM_SMART_MASK.md` still present
  drawing a box and pressing `박스 -> 스마트 마스크` as the normal first path.

Required outcome:

- document Recipe-scoped `라벨링 옵션 · 자동 윤곽`;
- document rectangle completion -> automatic candidate as the shortest normal
  path when enabled;
- keep regular box mode and explicit rerun available;
- document positive/negative point correction, one-point-at-a-time rerun and
  compare, previous/current candidate restore, explicit Confirm/Skip, and the
  file-save state: generation/restore do not save, Confirm runs the canonical
  save path, Skip writes no candidate, and later manual edits require
  `라벨 저장`;
- state that restoring the option does not run inference or save anything;
- do not replace the approved public GIF without explicit user approval.

Acceptance evidence:

- README, tutorial, MobileSAM guide, and visible F1/help wording agree with
  current source;
- no public document contains local private paths;
- `--priority-workflow-docs` and `git diff --check` pass.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`

### P3. Dataset Health Review Navigation

Status: split-filter slice `Complete` on 2026-07-29.

Completion record:
`docs/DATASET_HEALTH_SPLIT_FILTER_P3_20260729.md`.

The existing P4 Visual QA feature is complete and must not be rebuilt. The
remaining problem is narrower:

- `문제만` exists;
- every item already carries split text;
- a reviewer cannot yet narrow the Visual QA worklist by split or by contained
  class in the same problem-first surface.

Development order:

1. add a low-risk split filter using existing item data: `Complete`;
2. observe real Detection and Segmentation review work: prerequisite pending;
3. add class identity/filter only if it reduces a reproduced review task;
4. keep Dataset Health read-only and route edits to the existing editor:
   preserved.

Do not add reviewer accounts, consensus, comments, or a second annotation
editor.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`

### Complete: Four-Point Extreme Box

The supplied CVAT narration and frames resolve the earlier ambiguity:

- `4점 극점` means `위 -> 아래 -> 왼쪽 -> 오른쪽` extreme-point input;
- it creates the existing axis-aligned Rectangle;
- it is not a free quadrilateral or rotated rectangle;
- rotation shown later in the CVAT video is a separate edit feature.

Canonical storage, Recipe preference, cancellation, Undo/Redo, Smart Mask
timing, current YOLO/COCO/Pascal VOC/Label Studio/CVAT behavior, and rotated
import rejection are fixed in
`docs/FOUR_POINT_EXTREME_BOX_CONTRACT_20260729.md`.

The bounded implementation now follows that matrix. It preserves the existing
Rectangle contract and protected Viewer/OpenGL/ROI drag/resize paths; evidence
is in `docs/FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md`.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`

## 4. Completed Areas That Must Not Be Re-Proposed

Do not reopen the following without a changed requirement, source/runtime
contract, or reproduced regression:

- P0-A tool/class shortcuts, repeat, duplicate, and retention;
- P0-B Smart Mask box and positive/negative correction;
- initial/latest candidate compare and restore;
- Recipe-scoped automatic contour and layout auto-fit;
- merge, split, hole, z-order, remove-underlying, vertex edit, and intelligent
  scissors;
- session-only hide, full lock, and movement pin;
- display-only brightness, contrast, gamma, invert, and equalization;
- Dataset Health Visual QA;
- Dataset Interchange preflight;
- Batch AI preflight;
- Worklist and local quality-review state;
- Dataset Version v2 and provenance;
- detection model TP/FP/FN, error examples, comparability guard, and adoption
  history.

## 5. Blocked Or Conditional Work

- Persistent `occluded` plus Recipe-tag metadata: Complete with Object Review
  as the named consumer.
- Persistent group metadata: same-image Object Review focus/filter/batch
  metadata is Complete in
  `docs/OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md`.
- Exact polygon/raster cross-family interleaving: blocked until a visual-order
  defect is reproduced with focused performance evidence.
- Detection, segmentation, and anomaly production adoption: blocked until
  provenance-confirmed, content-separated production-camera or cross-session
  ground truth is available.
- Smart Mask field-accuracy claims: blocked until representative production
  boundary data and acceptance thresholds are supplied.
- Network-storage optimization: blocked until an approved representative
  source reproduces a performance problem.

## 6. Phased Direction

### Phase 1. Safety And Truth

- P0 safe close: `Complete`;
- canonical class-index visibility: `Complete`;
- operator-document truth synchronization.

Completion requires focused tests, current Debug EXE evidence, current
1920x1080/1366x768 UI evidence where visible UI changes occur, and durable
records in `WORK_TRACKING.md` and `STABLE_VERIFIED_AREAS.md` for completed
product behavior.

### Phase 2. Observation-Grounded Review Efficiency

- Dataset Health Visual QA split filter: `Complete`;
- the recorded real SEG `OK`/`NG` review satisfied the class-filter
  prerequisite;
- Dataset Health canonical class filter: `Complete`;
- future density changes still require a reproduced operator problem.

Completion requires measured task outcomes, not feature presence alone.

### Phase 3. Contract-Based Geometry And Field Evidence

- four-point extreme-box contract and bounded implementation: `Complete`;
- preserve completed `occluded`/tag/group metadata and its explicit-save,
  same-image review-unit boundary;
- cross-session model evaluation only with approved independent data.

Annotation format changes are incomplete until save/reopen, Undo/Redo,
import/export, Dataset Health, training, and legacy Recipe compatibility pass
together.

## 7. Durable Closure

```text
Status: Complete
Scope: Read-only product/source/commercial-video analysis and durable future-development direction through P0/P1/P2/contract-ready/blocked classification.
Acceptance criteria: Current source and completed roadmap were cross-checked; duplicate proposals were removed; new work has user problem, boundary, ownership, evidence, and prerequisites.
Verification: Live source at e1465d6, current project documents, ten full commercial-video timelines, source searches for window lifecycle/class presentation/operator wording, and final documentation gates for this record.
Evidence: This document plus NEXT_THREAD_HANDOFF.md, LABELING_STUDIO_COMPLETENESS_AUDIT.md, LABELING_PROGRAM_DIRECTION.md, WORK_TRACKING.md, and STABLE_VERIFIED_AREAS.md.
Boundary / next dependency: This record does not implement the listed product changes or prove production model accuracy. Product completion must be recorded separately after its required focused and actual-EXE gates pass.
```

## 8. 2026-07-30 Commercial Productization Transition

The editor and focused workstation workflow now have enough verified breadth
that additional general annotation features are no longer the default next
step. The current direction is to make the completed workflow installable,
diagnosable, recoverable, and reproducible.

The authoritative current plan is `docs/CURRENT_PRODUCT_STATUS.md`. This dated
document remains the rationale and earlier product-analysis record.

Ordered direction:

1. Audit current-source build, regression, Release publish, first run,
   dependencies, versioning, diagnostics, license/SBOM, installer/signing,
   recovery/archive, CLI, and CI without changing production code.
   `Complete`: read `docs/COMMERCIAL_READINESS_AUDIT_20260730.md`.
2. `Complete`: implement the selected `P0-B1 Versioned Deterministic
   Self-Contained Release Bundle Contract`. Read
   `docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md`.
3. `Complete`: implement `P0-B2 Packaged Runtime Diagnostics And Support
   Bundle`: explicit self-test, structured startup diagnostics, bounded
   retention, and privacy-safe explicit export without implicit
   training/inference. Read
   `docs/PACKAGED_RUNTIME_DIAGNOSTICS_P0B2_20260730.md`.
4. Verify install, launch, upgrade, and uninstall on an approved clean Windows
   environment.
5. `Complete`: add the P1-A portable project archive with complete saved
   Recipe/dataset coverage, per-file SHA-256, path rebasing, non-overwrite
   import, and explicit Apply. Read
   `docs/PORTABLE_PROJECT_ARCHIVE_P1A_20260730.md`.
6. `Complete`: add a bounded P1-B one-current-image crash-recovery journal
   with explicit restore/discard, dirty in-memory restore, context/integrity
   validation, and no implicit save/confirm. Read
   `docs/BOUNDED_CRASH_RECOVERY_P1B_20260730.md`.
7. Validate accuracy, long-run stability, and takt time only with approved
   independent field data and intended hardware/runtime.
8. Add data-centric QA or active-learning efficiency only when measured
   operator evidence establishes the need.

Commercial products teach this project to optimize the complete operator
outcome: install, diagnose, recover, reproduce, and only then scale review
efficiency. They do not require this product to copy cloud collaboration,
accounts, video tracking, deployment orchestration, or enterprise governance.

```text
Status: Complete
Scope: Persisted the commercial-productization transition and routed current priority ownership to docs/CURRENT_PRODUCT_STATUS.md.
Acceptance criteria: Productization order is explicit; editor feature expansion is no longer the default; local work and external prerequisites are separated; commercial platform breadth remains out of scope.
Verification: --priority-workflow-docs and git diff --check.
Evidence: docs/CURRENT_PRODUCT_STATUS.md and this section.
Boundary / next dependency: The audit, P0-B1 release package, P0-B2 packaged diagnostics, P1-A portable project archive, and P1-B bounded crash recovery are complete. P0-C requires a clean Windows environment and release lifecycle decisions; signing and production validation remain separate.
```
