# Next Thread Handoff

Last updated: 2026-08-03 KST

This is the current operational handoff for C:\Git\Labelling_Application. It is intentionally shorter than the historical journal. Use it to choose the next task; use the linked records only for the detailed evidence behind a claim.

## 0. 2026-07-31 Current Source-of-Truth Capsule

Read `docs/CURRENT_PRODUCT_STATUS.md` first. It supersedes older product,
maturity, and priority wording later in this file when they conflict. This
handoff remains a navigation surface and historical context, not a second
current-priority authority.

Current product:

- local Windows, single-operator industrial image labeling, training,
  inference, review, and model-evidence workstation;
- focused-workstation maturity `4.0/5`;
- labeling-editor depth `3.4/5`;
- neither score is a model-accuracy or commercial-parity claim.

Current Git/worktree:

- current clean-source transfer baseline:
  `59e37d8 test: place P0-C smoke on the leftmost monitor`;
- reviewed clean-machine historical baseline remains
  `ed682b2 fix: verify clean-machine support bundles`; preserve it as prior
  evidence rather than treating it as the current transfer candidate;
- all safe-close, canonical class-index, Smart Mask, Dataset Health,
  four-point box, Object Review metadata/group, and contextual group-control
  slices are committed and pushed through that baseline;
- the latest solo default internal regression evidence is 267/267 with no
  failures after the P1-J anomaly-evaluation exact-content leakage preflight.
  That gate blocks renamed SHA-256-identical content across train/valid/test
  and normal/abnormal before the worker starts; read
  `docs/ANOMALY_EVALUATION_CONTENT_LEAKAGE_PREFLIGHT_P1J_20260803.md`. The actual
  current-source product EXE returns structured JSON without startup logging,
  mutex acquisition, WPF, or durable writes; Main Viewer graphics remains an
  explicit warning because no UI/OpenGL context exists. Read
  `docs/HEADLESS_ENVIRONMENT_CHECK_CLI_20260801.md`. CI now
  invokes the same no-argument suite exactly once with a 15-minute timeout
  before release publishing. This is a locally verified workflow contract,
  not hosted GitHub Actions success before push. Read
  `docs/CI_COMPLETE_REGRESSION_GATE_20260801.md`; the prior 265/265 pilot and
  264/264 P1-B results remain historical baselines;
- `.proofline/STATE.md` and `.proofline/dashboard/` are local/user-owned and
  must remain untouched;
- local test, smoke, screenshot, validation, and test-temporary data now
  physically use `D:\OpenVisionLab-TestData\Labelling_Application`; the
  established C paths are junction-backed. The expanded contract also covers
  root/component `bin`/`obj`/`artifacts`, `packages`, `.vs`, and the 516-file
  repository test-fixture `datasets` path. All 52 mappings verify, the dataset
  remains Git-identical, and the post-expansion build/default `264/264` suite
  passed. Every actual desktop EXE smoke dynamically selects and verifies the
  leftmost monitor. Preserve
  `docs/LOCAL_TEST_STORAGE_AND_LEFT_MONITOR_CONTRACT_20260731.md`;
- `docs/README.md` is the complete non-authoritative documentation navigation
  hub. It classifies all documents exactly once without moving historical
  records; preserve the CI-backed
  `scripts/Test-DocumentationInformationArchitecture.ps1` gate;
- the operator-approved 32-entry C-drive rebuildable-candidate cleanup is
  Complete. It removed 958,721,091 bytes of approved output, preserved all 13
  D candidates, and passed a zero-warning/error current-source rebuild plus
  the focused workflow-doc test. Read
  `docs/REPOSITORY_C_CANDIDATE_CLEANUP_EXECUTION_20260731.md`. D candidates
  remain unapproved and require a fresh preview before any separate decision;
- the current worktree adds the runtime graphics-preflight slice described in
  `docs/RUNTIME_GRAPHICS_CAPABILITY_PREFLIGHT_P0C_20260731.md`;
- the current worktree also contains the completed bounded PatchCore normal-
  only anomaly pilot in `docs/PATCHCORE_ANOMALY_PILOT_20260731.md`: preserve
  normal-only memory-bank input, score/threshold visibility, review-only
  localization and heatmap file/path, and no automatic YOLO replacement or
  field claim;
- the explicit PatchCore heatmap review view is Complete: `히트맵 보기` loads
  only after the action, uses an owned themed window, fails closed for missing
  or corrupt files, releases on close/candidate change, and never saves,
  confirms, hides, changes a layer, or adopts a model. It is not a Main Viewer
  layer. Read `docs/PATCHCORE_HEATMAP_REVIEW_VIEW_20260801.md`;
- a deliberately versioned dirty-source `0.1.1` engineering package now proves
  deterministic publish, real packaged-EXE `8/8` graphics diagnostics, and
  post-launch package immutability. Read
  `docs/ENGINEERING_RELEASE_0_1_1_GRAPHICS_PREFLIGHT_EVIDENCE_20260731.md`;
- clean-source self-contained `0.1.2` is now the prepared GPU-target transfer
  candidate. Its two publishes are identical, packaged headless diagnostics
  pass `7/1/0` without writes, the transfer ZIP verifies `504/504` extracted
  payload hashes, and its harness records leftmost-monitor placement. Read
  `docs/P0C_CLEAN_SOURCE_TRANSFER_BUNDLE_0_1_2_20260801.md`;
- no commit or push is authorized by this handoff.

Current immediate priority:

- the Commercial Release Baseline audit is Complete in
  `docs/COMMERCIAL_READINESS_AUDIT_20260730.md`;
- `P0-B1 Versioned Deterministic Self-Contained Release Bundle Contract` is
  Complete in `docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md`;
- `P0-B2 Packaged Runtime Diagnostics And Support Bundle` is Complete in
  `docs/PACKAGED_RUNTIME_DIAGNOSTICS_P0B2_20260730.md`;
- `P1-A Portable Project Archive` is Complete in
  `docs/PORTABLE_PROJECT_ARCHIVE_P1A_20260730.md`;
- `P1-B Bounded Crash Recovery Journal` is Complete in
  `docs/BOUNDED_CRASH_RECOVERY_P1B_20260730.md`;
- the bounded PatchCore anomaly implementation is Complete; its next model
  gate is blocked on approved same-split field images, acceptance thresholds,
  and target hardware for YOLO-versus-PatchCore comparison;
- preserve dirty/pending/active preflight blocking, complete saved
  Recipe/dataset inclusion, per-file SHA-256, staging/path rebasing,
  non-overwrite import, and explicit Recipe Apply;
- preserve current-user diagnostics/log/config routing, delayed log
  initialization, bounded retention, explicit self-test/export, support ZIP
  allow-list/redaction, and package-folder immutability;
- preserve one-current-image recovery, atomic/checksummed bounded retention,
  Recipe/dataset/image identity, explicit restore/discard, dirty in-memory
  restore, pending-candidate exclusion, and explicit label save;
- the P0-C Windows Sandbox packaged startup/diagnostics/support/package
  integrity slice passed;
- the full Hyper-V follow-up is `Incomplete`: a clean Windows 11 Generation 2
  guest verified all `503` payload hashes, launched the package, and created a
  real dataset, but its standard synthetic display reproduced
  `glGenFramebuffersEXT not supported`;
- the current-source actual-viewer graphics preflight is Complete: environment
  diagnostics and explicit support export report the renderer/version and 11
  required framebuffer functions, while a definite failure blocks the central
  image-load path with actionable guidance;
- preserve the warning-not-blocked early-startup behavior, fail-closed definite
  failure, and no implicit image/model/save action defined in
  `docs/RUNTIME_GRAPHICS_CAPABILITY_PREFLIGHT_P0C_20260731.md`;
- the immutable `0.1.0` package used by the existing VM evidence predates the
  preflight and must remain unchanged;
- the `0.1.1` package records `source.dirty=true`; do not use it as clean-source
  P0-C target evidence. The clean-source `0.1.2` package and verified transfer
  ZIP are now prepared; preserve their manifest and ZIP hashes;
- preserve checkpoint `P0C-Clean-Windows-Installed-20260731` and the evidence
  in `docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md`;
- do not repeat the Sandbox or standard Hyper-V viewer loop unless the
  display capability or viewer implementation changes;
- the user selected the separate GPU-capable clean Windows PC/VM path in
  `docs/P0C_GPU_CAPABLE_CLEAN_TARGET_VALIDATION_PLAN_20260731.md`; the viewer
  fallback project is not active; the current Windows 10 Pro/GTX 1060 host is
  not an officially supported Hyper-V DDA/GPU-P target. Package preparation is
  complete; execution is blocked until a supported GPU-capable clean target is
  directly accessible;
  Codex, not the operator, owns target-side command execution and evidence
  recovery;
- installer lifecycle and signing decisions remain separate P0-C
  prerequisites;
- independent production validation is blocked until approved data,
  thresholds, runtime/weights, and target hardware are available;
- do not turn P0-B2/P1-A/P1-B into telemetry/cloud support, automatic
  inference/training, label autosave, multi-image recovery, external runtime
  redistribution, or a production-accuracy claim.

Latest completed Smart Mask slices:

1. Auto-first contextual controls: correction points, point undo/clear,
   cancellation, and detail stay behind `보정 옵션`.
2. Real correction response: positive direction `6/6`, negative direction
   `4/4` applicable, combined held-out improvement `3/4`, held-out median IoU
   delta `+0.0988`; two worsening combinations remain disclosed.
3. Candidate recovery: first/latest session candidates can be switched with
   `이전 후보 보기` and `현재 후보 보기`; switching does not save and explicit
   confirmation writes only the displayed candidate.
4. Actual Debug EXE recovery replay: Kolektor `kos14/Part7` completed automatic
   candidate -> positive/negative correction rerun -> previous candidate
   restore -> explicit Confirm -> save -> next image -> saved-image reopen.
   The reopened image contained exactly one saved 96-point polygon and a
   7,931-pixel mask with no pending Smart Mask confirmation. Full-duration
   visual review found no P0/P1/P2 issue. Evidence:
   `artifacts\operator-video\20260728-smartmask-restore-save-retry1`.
5. Automatic-contour normal flow and layout recovery: the operator selects
   `라벨링 옵션 · 자동 윤곽` once per Recipe, a new rectangle starts MobileSAM
   without another action click, confirm/skip returns to the next box, and
   Recipe reopen restores the option without running inference. Canvas
   viewport size changes now auto-fit after layout settles, so the actual EXE
   completed the full run without a `맞춤` click. Evidence:
   `artifacts\operator-video\20260728-smart-contour-auto-fit`.

Read before reopening these slices:

- `docs/SMART_MASK_CONTEXTUAL_CORRECTION_UX_20260728.md`;
- `docs/SMART_MASK_CORRECTION_EFFECTIVENESS_20260728.md`;
- `docs/SMART_MASK_CANDIDATE_COMPARE_RESTORE_20260728.md`.
- `docs/SMART_CONTOUR_AUTO_MODE_AND_LAYOUT_FIT_20260728.md`.

Latest completed safety slice:

- Main-window safe close is `Complete`. Dirty labels/pending mask work use
  explicit save/discard/cancel; candidate-only close uses discard/cancel and
  never confirms or saves the candidate; active work is named before existing
  cleanup runs. Actual Debug EXE dirty cancel/save/reopen and YOLOv8
  candidate-only cancel/discard evidence passed. Read
  `docs/SAFE_APPLICATION_CLOSE_P0_20260729.md`.

Latest completed class-schema presentation slice:

- Canonical class-index visibility is `Complete`. Class Catalog now preserves
  Recipe order, displays `0 · name`, and explains rename versus add/delete
  schema behavior. The canvas keeps `1~9` drawing shortcuts while the
  next-label card shows the canonical YOLO index. Focused YAML/YOLO/reopen/
  shortcut/Batch AI gates, 1920x1080/1366x768 current-build visual review, and
  a current Debug EXE restored-Recipe capture passed. Read
  `docs/CANONICAL_CLASS_INDEX_VISIBILITY_P1_20260729.md`.

Latest completed operator-documentation slice:

- Smart Mask README, tutorial, MobileSAM guide, and visible F1 help now use the
  same Recipe-scoped auto-first, one-point correction, previous/current
  candidate comparison, explicit Confirm/Skip, and save-state contract.
  Current Debug EXE F1 before/after evidence and focused gates passed. The
  approved public GIF was not replaced. Read
  `docs/SMART_MASK_OPERATOR_DOCUMENTATION_TRUTH_P2_20260729.md`.

Latest completed Dataset Health slice:

- Visual QA now offers `전체` plus only the existing train/valid/test splits,
  composes the split with `문제만`, preserves or safely resets the selection on
  refresh, and balances the bounded healthy sample budget across splits.
  Full-tree SHA-256 coverage proves filtering and refresh do not write dataset
  files. Read `docs/DATASET_HEALTH_SPLIT_FILTER_P3_20260729.md`.
- The recorded real 125-image SEG `OK`/`NG` review satisfied the class-filter
  prerequisite. Visual QA now exposes canonical Recipe `index · name`
  classes, rebuilds a class-scoped catalog bounded at 500 rows, composes with
  split and `문제만`, and keeps source files byte-identical. The real replay
  reduced `1 · NG` to 14 images. Read
  `docs/DATASET_HEALTH_CLASS_FILTER_20260729.md`.

Latest completed geometry slice:

- `4점 극점` is defined and implemented as an axis-aligned
  `위 -> 아래 -> 왼쪽 -> 오른쪽` input method that creates the existing
  Rectangle. It is not a free quadrilateral or rotated box. Recipe persistence,
  cancellation, one-step history, Smart Mask timing, current interchange, and
  rotated-import rejection gates pass. Read
  `docs/FOUR_POINT_EXTREME_BOX_CONTRACT_20260729.md` and
  `docs/FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md`.

Immediate next priorities:

1. Same-image Object Review grouping is Complete in
   `docs/OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md`. Do not reopen it
   without a changed contract or focused regression. The remaining renderer
   and field-adoption items below are prerequisite-blocked.

Commercial-video priority ledger after that:

5. Persistent `occluded`, Recipe-tag, and same-image group metadata are
   Complete with Object Review as the named edit/filter/badge consumer.
   Preserve the separate sidecar, explicit label-save boundary, dedicated
   group selection, and session-only hide/full-lock/movement-pin separation;
   training weighting and external interchange remain outside it.
6. Exact polygon/raster cross-family z-order remains a renderer gap, but the
   Viewer/OpenGL path is protected. Reopen only with a reproduced visual-order
   defect and focused performance evidence.
   Recommended model: `gpt-5.6-sol`
   Reasoning effort: `high`
7. Object-detection and anomaly model adoption remain blocked on newly
   approved independent production-camera/cross-session data with provenance
   and content-separated ground truth.
   Recommended model: none until data is available
   Reasoning effort: n/a

Completed commercial-video roadmap items that must not be repeated without a
regression or changed contract:

- P0-A command/repeat productivity;
- P0-B interactive Smart Mask, contextual correction, real correction
  response, and candidate restore;
- P1 mask preservation/schema/merge/split/hole/z-order/remove-underlying;
- P2 session object state, polygon vertex edit, intelligent scissors;
- P3 display-only image aids;
- P4 Dataset Health visual QA;
- P5-A interchange preflight and P5-B batch-AI preflight;
- actual-EXE Smart Mask promotional GIF publication.
- actual-EXE real Smart Mask correction-rerun, previous-candidate restore,
  selected save, and saved-label reopen regression.
- Recipe-scoped automatic-contour box flow and canvas layout auto-fit without a
  manual Fit click.

Still out of scope:

- video tracking/interpolation/propagation productization;
- comments, assignment, multi-reviewer history, accounts, cloud sync, hosting,
  and deployment;
- 3D, keypoint, arbitrary model marketplace;
- camera, lighting, PLC, and I/O control;
- automatic candidate approval/save.

## 1. Mandatory Start Sequence

1. Run git status --short before any other project command.
2. Read AGENTS.md.
3. Read this file.
4. Read CODEX_NEXT_PROMPT.md, docs/WORK_TRACKING.md, docs/STABLE_VERIFIED_AREAS.md, and docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md.
5. Inspect the live branch, status, and diff directly. Git state is more authoritative than this handoff.
6. Before editing, state the immediate priority, remaining product priority, assumptions, and verification plan.

The global instruction source is `C:\Users\user\.codex\AGENTS.md`; the
repository instruction source is `C:\Git\Labelling_Application\AGENTS.md`.
Read both. There is no separate `C:\AGENTS.md` or `C:\Git\AGENTS.md` in this
workstation snapshot.

## 2. Repository Checkpoint

- Workspace: C:\Git\Labelling_Application
- Branch: main. Live HEAD at this handoff is
  `f7751c5 feat: advance labeling QA and smart mask workflow`. Always verify
  the live hashes before new work.
- The labeling-editor structure workflow through P0-A/P0-B, P1-A/P1-B,
  merge/join, and axis-aligned split/slice was committed and pushed as
  `9b2160a feat: advance labeling editor structure workflows` on 2026-07-28.
  Enclosed hole add/fill was committed and pushed as
  `d669b64 feat: add segmentation hole editing`. Saved-object z-order,
  remove-underlying, P2 precision/state, and P3 display-only aids were
  committed and pushed as
  `ed0f826 feat: complete labeling precision and display aids`.
  P4 Dataset Health visual QA, P5-A dataset-interchange preflight, P5-B batch
  AI preflight, field-data intake contracts, and the approved Smart Mask
  promotional media were then committed in `f7751c5`.
- The structural-refactor sequence is `3351abd` (Core contract ownership),
  `5ed829c` (Model/Yolo ownership), and `889abdf` (Yolo contract ownership and
  final declaration audit). This phase is complete; do not reopen mechanical
  splitting without a concrete ownership, navigation, reuse, or test-boundary
  problem.
- The earlier product commit
  `ad569dc feat: add dataset versioning and YOLO11 anomaly validation` closes
  Recipe Dataset Version v2, Recipe/anomaly/adapter truth alignment, and the
  recorded local YOLO11 anomaly-classification runtime slice.
- The verified relocation fixes and evidence records are committed in
  `0f1f91b fix: preserve relocated runtime workflows`. The relocated copy at
  `C:\새 폴더\OpenVisionLab-Labeling-Studio_TEST` was deleted after the original
  path independently passed the Dataset wizard, Worklist, and YOLOv8 restart
  EXE smokes. Use only `C:\Git\Labelling_Application` for development.
- Tracked project files are currently dirty with the verified Smart Mask
  contextual/effectiveness/compare-restore follow-up. Local
  `.proofline/STATE.md` and `.proofline/dashboard/` remain untracked and outside
  project commits. Verify live state with `git status --short`.
- GitHub Actions has not been rechecked for the current structural-refactor and
  closure documentation commits. Do not cite older CI evidence as current CI
  evidence.
- The current focused passes directly verified Dataset Health, external native YOLO intake, model/anomaly comparison, the dedicated Model Center workspace, and the explicit model-adapter catalog slices. The image-queue slice also has a 50,081-image local warm-cache profile and a separate duplicate-file local 8K profile; neither is a network-share or production-camera result.
- The 2026-07-27 review of ten user-provided commercial integration videos is
  recorded in
  `docs\LABELING_STUDIO_COMMERCIAL_VIDEO_REVIEW_20260727.md`. A denser V7/CVAT
  labeling review corrected the initial visual-QA-first decision. P0-A
  command/productivity, P0-B interactive Smart Mask, and the first P1-C
  merge/join, axis-aligned split/slice, enclosed hole add/fill, saved-object
  z-order and remove-underlying are complete. P2 session-only
  hide/full-lock/movement-pin and contextual Object Review correction are
  complete, and polygon vertex insert/delete plus bounded edge-aware intelligent
  scissors are complete. P3 compact display-only image aids are also complete.
  P4 Dataset Health visual QA is complete in current source with a
  problem-first text worklist, selected-image saved-overlay preview,
  `문제만`, and an explicit route to the existing editor.
  P5-A format-conversion preflight is also complete in current source for
  existing COCO/Pascal VOC/Label Studio/CVAT import/export. It uses isolated
  dry-run, source/requested-target fingerprints, skipped-record blocking, and
  explicit Apply in a separate contextual window.
  P5-B batch AI preflight is complete in current source. Visible-row and
  failed-item retry commands now require a contextual preflight and explicit
  Start; scope, model/weight/task, confidence, Recipe class-name mapping,
  existing-label policy/counts, and Candidate Review/no-auto-approval/no-
  autosave are visible. Read
  `docs\BATCH_AI_PREFLIGHT_P5B_20260728.md`; focused gate:
  `--wpf-batch-detection-preflight`.
  The actual-EXE operator video runner and promotional-media candidate are also
  complete. The approved public media is the superseding automatic Smart Mask
  run `20260728-smartmask-final4` on KolektorSDD `kos14/Part7.jpg`, with
  human-path cursor motion, exact application-window capture, explicit
  confirmation, and canonical save. It wrote one 96-point polygon and 7,931
  mask pixels; broad source-label precision was `0.9861`, IoU `0.3927`, and
  recall `0.3948`. The 1024x576, 10fps, 18.7-second GIF and poster are published
  under `docs/tutorial/images/github/`. The older manual-polygon candidate and
  rejected Smart Mask runs are historical evidence only. Read
  `docs\ACTUAL_EXE_VIDEO_AND_GITHUB_GIF_PLAN_20260728.md`. README/public media
  must remain unchanged unless the user explicitly approves replacement.
  The labeling-only estimate is `3.4/5`; bounded Object Review
  `occluded`/tag/group persistence is now complete, while video propagation,
  broader modalities, and collaboration still prevent CVAT/V7 parity.
  This remains separate from the focused local-workstation estimate of `4.0/5`.
  `docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md` is the current
   implementation contract.
- This labeling-editor development sequence started from
  `ef155ed docs: close structural refactoring phase` on 2026-07-27. Its reviewed
  work through split/slice is now in `9b2160a`; use the live branch state for
  later closures. Local `.proofline/STATE.md` and `.proofline/dashboard/` are
  unrelated untracked state and must remain untouched.
- Never push unless the user explicitly says push. A commit request means local commit only.

### Current model-quality dependency after P5

P5-A and P5-B are complete. Independently acquired production-camera/
cross-session data remains the prerequisite for detection, segmentation, and
anomaly model-quality adoption. This is separate from the ready actual-EXE
Smart Mask restore/save regression and contract-dependent editor backlog in
section 0/10. Do not recommend model-quality token spending until the data is
available. Use `docs\FIELD_DATA_INTAKE_PREREQUISITE_20260728.md` as the exact
manifest, directory, content-separation, label-review, and approval checklist.
The GoPxL commercial-reference folder contains videos, not model-quality data.

### Completed P1-A/P1-B development checkpoint (2026-07-27)

Immediate outcome:

- one code-owned preservation matrix covers canonical segment JSON, class-index
  mask PNG, YOLO segmentation, COCO polygon, and CVAT polygon;
- every format declares class, polygon, raster, hole, multi-component,
  instance-grouping, z-order, and remove-underlying provenance as
  `Preserved`, `Conditional`, or `Lost`;
- annotation-derived COCO/CVAT/YOLO export results expose deduplicated warnings;
- a cutout/hole fixture reports `Holes: Lost` for all three polygon export
  paths instead of presenting them as lossless.
- canonical segment JSON v3 stores persistent object ID, component index,
  z-order, and last structural operation;
- disconnected raster components retain one object identity across
  save/load/re-save;
- segment JSON v1/v2 loads with unchanged geometry and deterministic legacy
  metadata.

Owner boundary:

- `Yolo\SegmentationInterchangeContractService.cs`: capability matrix,
  annotation profile, audit, and warning text;
- `Yolo\YoloSegmentationAnnotationService.cs`: v3 serialization and v1/v2
  compatibility;
- `LabelingSegmentationObject` and annotation history: in-memory identity and
  undo preservation; duplicate deliberately starts a new identity;
- COCO/CVAT/YOLO export services: append the shared warnings to existing result
  objects;
- Viewer/OpenGL and brush/eraser input paths: unchanged.

Verification:

- isolated test build: 0 warnings, 0 errors;
- `--segmentation-interchange-contract`: pass;
- existing segmentation storage/export/import regressions must remain part of
  the final gate.

Evidence:

- `docs\SEGMENTATION_INTERCHANGE_PRESERVATION_CONTRACT_20260727.md`;
- `tests\LabelingApplication.Tests\Program.SegmentationInterchangeContract.cs`.

Boundary / next dependency:

- P1-C merge/join, axis-aligned split/slice, enclosed hole add/fill,
  saved-object z-order, and remove-underlying are complete in current source.
- P2 session-only hide/full-lock/movement-pin and contextual exposure are
  complete for manual ROI and segmentation rows. These states do not enter
  canonical labels or export semantics. Protected regressions and fresh
  1920/1366 evidence pass. Polygon vertex insert/delete also passed focused,
  protected, canonical, and 1920/1366 gates. Bounded intelligent scissors then
  passed deterministic 90%/2.5px·250ms, explicit preview/apply/cancel,
  protected/canonical, and 1920/1366 gates; P3 display-only aids then passed
  source/file/history/overlay invariants and current-build 1920/1366 evidence;
  the editor estimate is `3.4/5`.
- P2 object state/precision, P3 display-only aids, P4 Dataset Health visual QA,
  P5-A format-conversion preflight, and P5-B batch AI preflight are Complete.
  Independent production-camera/cross-session data is now the prerequisite.
- Recommended model: no model tokens until the prerequisite exists.

### Completed P2 intelligent-scissors checkpoint (2026-07-28)

- The command is visible only for a selected manual polygon inside the
  collapsed-by-default `세그먼트 편집 옵션`; it is not a global tool.
- `경계 추종` arms one edge click. A bounded Sobel-cost A* path appears as a
  gold `EDGE PREVIEW`; geometry, history, and disk state remain unchanged.
- `미리보기 적용` revalidates the same source geometry and creates one
  Undo/Redo step. `취소`, right-click, selection/tool/image change, and history
  restore discard the preview.
- Hidden and full-lock states reject the command; movement pin permits it.
- Canonical v3 reopen preserves ObjectId, class, component index, z-order,
  bounds, refined geometry, and `IntelligentScissors` provenance.
- Focused gate: `--intelligent-scissors`.
- Evidence:
  `docs\INTELLIGENT_SCISSORS_P2_20260728.md` and
  `artifacts\ui\intelligent-scissors-p2-20260728`.
- Boundary: one adjacent polygon edge only, fixed 24-source-pixel corridor,
  180,000-pixel maximum search, no autonomous whole-object segmentation, no
  field accuracy or CVAT/V7 parity claim.
- P3 display-only brightness/contrast/gamma/invert and histogram/equalization,
  P4 Dataset Health visual QA, P5-A format-conversion preflight, and P5-B batch
  AI preflight are complete in later checkpoints. Independent field data is the
  next prerequisite.
- Recommended model: no model tokens until the prerequisite exists.

### Completed P1-C merge/join checkpoint (2026-07-27)

- Saved Labels exposes merge checkboxes only on segmentation rows, a selected
  count, and an accessible **선택 마스크 병합** command.
- Two or more same-class polygon/raster objects are unioned into one raster
  object. Polygon cutouts are applied per source before the union, so another
  source can fill a cutout without being erased.
- Mixed classes are rejected before mutation.
- The merged object receives a new v3 object ID, component `-1`, maximum source
  z-order, and `LastStructuralOperation=Merge`.
- The replacement is one undo/redo step. Canonical save/load/re-save preserves
  shared identity, sequential disconnected component indices, and provenance.
- Viewer/OpenGL, ROI, brush, eraser, and mask-drag hot paths are unchanged.
- Focused gate: `--segmentation-merge`.
- Evidence:
  `docs\SEGMENTATION_MERGE_P1C_20260727.md` and
  `artifacts\ui\segmentation-merge-p1c-20260727`.
- Split/slice is completed in the following checkpoint.

### Completed P1-C split/slice checkpoint (2026-07-27)

- Saved Labels exposes **세로 절단**, **가로 절단**, and visible cancel
  controls only when one manual segmentation object is selected.
- A canvas click removes one pixel column or row. The command commits only
  when the selected polygon/raster source becomes at least two 4-connected
  components; invalid clicks leave source geometry and history unchanged.
- Polygon cutouts remain empty because merge and split now share
  `WpfSegmentationMaskGeometryService`.
- Every result gets an independent new v3 object ID, component `-1`, preserved
  class/z-order, and `LastStructuralOperation=Split`.
- The source replacement is one undo/redo step. Canonical v3 save/load/re-save
  preserves the generated identities and provenance.
- Pending split is cancelled by right-click, the visible cancel button,
  selected-object/image/tool change, and cannot compete with an active Smart
  Mask session.
- Viewer/OpenGL, ROI, brush, eraser, and mask-drag hot paths are unchanged.
- Focused gate: `--segmentation-split`.
- Evidence:
  `docs\SEGMENTATION_SPLIT_P1C_20260727.md` and
  `artifacts\ui\segmentation-split-p1c-20260727`.
- Hole editing is completed in the following checkpoint. Remove-underlying
  still requires an affected-object warning before mutation.

### Completed P1-C hole editing checkpoint (2026-07-28)

- Saved Labels exposes **구멍 그리기**, **구멍 채우기**, and visible cancel
  controls for one selected manual segmentation object.
- Hole-add accepts a polygon only when all rasterized pixels are foreground
  and the resulting empty component remains enclosed.
- Hole-fill accepts only one empty internal component; exterior-connected
  background, concavities, and open channels are rejected.
- Invalid input works on copied geometry and leaves source/history unchanged.
- Polygon/raster output keeps object ID, class, and z-order; provenance becomes
  `HoleAdd` or `HoleRemove`.
- Each successful operation is one undo/redo step. Canonical v3
  save/load/re-save preserves identity and provenance.
- Right-click, visible cancel, selected-object/image/tool change cancel pending
  input, and Smart Mask cannot own point input concurrently.
- Focused gate: `--segmentation-hole`.
- Evidence:
  `docs\SEGMENTATION_HOLE_P1C_20260728.md` and
  `artifacts\ui\segmentation-hole-p1c-20260728`.
- Saved-object z-order is completed in the following checkpoint.

### Completed P1-C saved-object z-order checkpoint (2026-07-28)

- Saved Labels exposes **맨 뒤**, **한 칸 뒤**, **한 칸 앞**, and **맨 앞**
  for one selected manual segmentation object.
- Lower `ZOrder` is farther back. A successful command stable-sorts legacy
  equal-order objects, moves the selected object, and normalizes the global
  stack to `0..N-1`.
- IDs, classes, polygon/cutout/raster geometry, and component metadata remain
  unchanged. Changed objects record `LastStructuralOperation=ZOrder`.
- First/last boundary commands make no data or history change.
- One Undo/Redo restores/reapplies the whole stack. Canonical v3 save,
  class-independent shell reload, and re-save preserve global order.
- `AnnotationPolygonOverlays` uses this order inside each polygon or raster
  renderer family. Exact polygon/raster interleaving is not claimed because
  the viewer still uses separate render passes.
- Focused gate: `--segmentation-zorder`.
- Evidence:
  `docs\SEGMENTATION_ZORDER_P1C_20260728.md` and
  `artifacts\ui\segmentation-zorder-p1c-20260728`.
- Historical next dependency at z-order closure was remove-underlying; it is
  completed in the following checkpoint.

### Completed P1-C remove-underlying checkpoint (2026-07-28)

- Saved Labels exposes **겹침 분석**, **확인 후 제거**, and **취소**.
- Analysis is read-only and reports affected objects, removed pixels, and full
  removals. Affected underlying overlays and the pending summary are orange.
- Only overlapping lower-stack objects change. Partial survivors preserve
  ID/class/z-order as exact raster remainders with
  `LastStructuralOperation=RemoveUnderlying`; fully covered objects are
  removed. Selected/front/unaffected objects remain unchanged.
- Apply re-analyzes and compares a full geometry/order signature. Stale plans
  are rejected without mutation or history.
- Apply is one Undo/Redo step. Canonical v3 save/load/re-save preserves raster
  remainder, identity, z-order, and provenance.
- Focused gate: `--segmentation-remove-underlying`.
- Evidence:
  `docs\SEGMENTATION_REMOVE_UNDERLYING_P1C_20260728.md` and
  `artifacts\ui\segmentation-remove-underlying-p1c-20260728`.
- Historical next dependency at remove-underlying closure was P2 object state;
  it is completed in the following checkpoint.

### P2 object-state contextual correction checkpoint (2026-07-28)

- Saved manual ROI and manual segmentation rows expose **숨김**, **잠금**, and
  **이동 고정** as independent current-image-session states.
- Hidden objects leave the canvas and structural/canvas edit paths but remain
  selectable in Object Review so they can be shown again.
- Locked objects reject class, delete, duplicate, geometry, ROI handle,
  brush/eraser, merge, split, hole, z-order, and remove-underlying mutation.
  Direct mutation paths are guarded in addition to button enablement.
- Movement-pinned objects keep ordinary class presentation and reject only
  whole-object translation. ROI resize, polygon vertex edit, copy, delete,
  class, and structural commands remain allowed. Gold focus and `PINNED`
  overlay labels are prohibited.
- State controls are compact icons. Merge/split/hole/z-order/remove-underlying
  live in one collapsed segmentation editor visible only for a selected manual
  segment. Cancel/status appear only while the matching operation is pending.
- State is cleared on image change or queue reset and is excluded from
  history, dirty state, canonical JSON, exports, and training input.
- Focused gate: `--object-session-state`.
- The correction is `Complete`: protected regressions and fresh current-build
  1920x1080/1366x768 after captures pass, and all three fixture object rows are
  visible at 1366x768.
- Evidence:
  `docs\OBJECT_SESSION_STATE_P2_20260728.md`,
  `docs\OBJECT_REVIEW_CONTEXTUAL_UI_CORRECTION_20260728.md`, and
  `artifacts\ui\object-review-contextual-20260728`.
- Polygon vertex insert/delete and bounded edge-aware intelligent scissors are
  complete. Next bounded operation: define the P3 display-only image/overlay
  transformation and source/training immutability contract.

### Completed P0-A development checkpoint (2026-07-27)

Immediate outcome:

- add purpose-filtered tool shortcuts and class `1~9` shortcuts;
- use `0` to open Class Catalog as the deterministic larger-catalog fallback;
- repeat the last drawing tool and class without toolbar reselection;
- duplicate the selected box, polygon, or raster mask while preserving class,
  geometry type, history, dirty state, and canonical save behavior;
- keep the current drawing tool and class after object creation;
- expose an in-product shortcut help surface;
- suppress drawing shortcuts while a text-entry or value-editing control owns
  keyboard focus.

Included owner boundary:

- `Services\Annotation`: shortcut mapping and safe duplicate geometry policy;
- `WpfCanvasPanelViewModel`: visible shortcut/help state, class index state, and
  last drawing tool/class state;
- `WpfLabelingShellWindow.ShellInputCommands`: input bridge only;
- existing Annotation history, object review, and save owners: mutation
  registration and refresh;
- `OpenVisionLab.ImageCanvas`: unchanged geometry drawing, hit-test, move, and
  resize owner.

Excluded from this slice:

- Viewer/OpenGL input redesign;
- ROI, brush, or eraser hot-path redesign;
- new AI models, point-prompt Smart Mask, video propagation, cloud/team shortcut
  profiles, and persisted user-custom shortcut profiles.

Evidence captured before code changes:

- current application build:
  `dotnet build .\OpenVisionLab.LabelingStudio.csproj -c Debug /nr:false -m:1
  /p:UseSharedCompilation=false` passed with 0 warnings and 0 errors;
- closest reproducible current-source 1920x1080 baseline:
  `artifacts\ui\labeling-productivity-p0a-20260727\before-current-source-1920x1080.png`;
- `--exe-industrial-object-labeling-smoke` stopped before capture because the
  guide/tools tab selector could not select the expected row;
- `--exe-real-labeling-smoke` stopped before capture because its native click
  could not find the segmentation dataset purpose.

The two pre-existing actual-EXE automation failures remain stale general-smoke
selector limitations, not P0-A regressions. P0-A now has its own current-build
`--exe-labeling-productivity-smoke`, which opens the latest Debug EXE, verifies
the visible help button, clicks it natively, verifies the help text, and captures
`artifacts\ui\labeling-productivity-p0a-20260727\after-actual-exe-1920x1080.png`.

P0-A completion gates:

1. shortcut mapping, purpose filtering, text-entry suppression, class `1~9`, and
   `0` fallback tests pass: `--labeling-productivity` and
   `--wpf-undo-redo-shortcuts`;
2. repeat and box/polygon/raster-mask duplicate preserve tool, class, source
   geometry, and deterministic in-bounds offset or documented same-position
   fallback;
3. each duplicate is one undo step and existing undo/redo behavior remains
   intact;
4. canonical save/reopen and protected ROI/segmentation/mask gates pass;
5. fresh 1920x1080 after evidence shows the help surface and numbered classes;
6. isolated build and `git diff --check` pass.

P0-A durable status: `Complete`. The implementation does not establish V7/CVAT
feature parity; it closes only the command/repeat/duplicate/help foundation.

Full development sequence:

1. P0-A command/productivity: `Complete`.
2. P0-B interactive Smart Mask: `Complete`.
3. P1 mask structure: P1-A preservation/loss, P1-B canonical v3 identity, and
   P1-C merge/join, axis-aligned split/slice, enclosed holes, saved-object
   z-order, and remove-underlying are complete.
4. P2 object state and precision geometry: contextual hide/full-lock/
   movement-pin correction, polygon vertex insert/delete, and bounded
   intelligent scissors are complete. The 4-point extreme-box contract and
   bounded implementation are complete. Contract-backed Object Review
   `occluded`/tag/group persistence is complete.
5. P3 display-only aids: `Complete`; brightness, contrast, gamma, invert,
   histogram/equalization, and overlay alignment preserve canonical source,
   disk file, history, and training pixels.
6. P4 Dataset Health visual label QA: `Complete`; read-only problem-first
   discovery, selected-image saved overlay, and navigation back to the
   canonical editor.
7. P5 interchange and batch preflight: `Complete`; dry-run/Apply validation
   for existing formats and explicit batch AI scope, model, class mapping,
   confidence, and existing-label policy are verified. Reopen only for a
   regression or changed contract.

Data-dependent gates remain separate: production claims for detection,
segmentation, and anomaly quality require independent provenance-confirmed
camera/session data. Do not spend implementation tokens pretending UI work can
close those evidence gaps.

### Completed P0-B development checkpoint (2026-07-27)

P0-B durable status: `Complete`. Field validation: `Not evaluated`.

- `WpfSmartMaskPromptSessionService` owns the start box, image/Recipe/class
  identity, positive/negative points, input mode, generation, polygon detail,
  and next-instance state.
- The bundled worker accepts box plus repeated point labels and deterministic
  48/96/256 polygon limits while preserving the existing box-only path.
- The shell exposes point add/undo/clear, worker cancellation, rerun-replace,
  confirm/skip, and next-instance/new-box flow. Pending candidates do not save;
  confirmation continues through the canonical segment JSON/mask PNG owner.
- Image/Recipe/prompt generation changes reject stale results. Rerun replaces
  one pending Smart Mask candidate while preserving confirmed labels.
- The actual current Debug EXE completed box → positive point → negative point
  → rerun → confirm → next instance → new box readiness. Evidence:
  `artifacts\ui\smart-mask-p0b-20260727`.
- Real MobileSAM fixture evidence:
  `artifacts\mobile-sam-point-correction\20260727-185324\point-correction-evidence.json`.
  Positive input expanded `4431 → 6529`; negative input removed `22113` pixels
  from the wide positive result; the source image hash remained unchanged.
- Existing 24-call exact-box and 96-call box-jitter results remain protected
  regression evidence, not field accuracy.

P5-A format-conversion preflight and P5-B batch AI preflight are complete.
The immediate product dependency is independent production-camera/cross-session
data.
P4 Dataset Health visual QA, P3 display-only image aids, P2 bounded intelligent
scissors, and polygon vertex insert/delete are complete.
Session-only hide/lock/pin is complete. P1-A
JSON/mask-PNG/YOLO/COCO/CVAT preservation/loss semantics and P1-B canonical
v3 object/component identity are complete, and P1-C merge/join plus
axis-aligned split/slice, enclosed hole add/fill, saved-object z-order, and
remove-underlying have
user-visible commands and focused evidence.
Keep the next structural command independently acceptance-gated.

### Completed structural-refactor phase

The behavior-preserving Core/Model/Yolo ownership refactor is complete and
pushed. Public result/report/DTO types were moved from service implementations
into responsibility-named `*Contracts.cs` files; the exact navigation map and
per-slice verification records are in `docs/CODE_STRUCTURE.md` and
`docs/WORK_TRACKING.md`.

The final declaration audit found zero duplicate public type names. Outside
`*Contracts.cs`, only `Yolo/CYolov5.cs` and
`Yolo/YoloSegmentationAnnotationService.cs` intentionally retain multiple public
types. They are documented co-location exceptions, not pending mechanical
splits. Do not restructure the segmentation annotation/materialization path
without a concrete requirement and focused evidence.

Do not continue refactoring merely to reduce file length or public-type count.
Resume structural work only when a concrete change exposes mixed ownership,
stale coupling, an untestable responsibility, or a real navigation problem.
Keep `.proofline/STATE.md` and `.proofline/dashboard/` outside project commits.

### Completed P2 polygon vertex insert/delete (2026-07-28)

Status: `Complete`. Field validation: `Not evaluated`.

- Selected saved manual polygons expose `정점 추가` and `정점 삭제` only in
  the collapsed segment context. Masks and boxes do not expose them.
- Eight screen pixels are converted through current zoom. Insert projects onto
  the deterministic nearest edge; delete selects the nearest vertex.
- Endpoint, duplicate, triangle, zero-area, and self-intersecting results
  reject without mutation or history.
- Successful edits preserve object ID/class/component/z-order/cutouts and
  selection, create one Undo/Redo step, and replay `VertexInsert` or
  `VertexDelete` through canonical v3.
- Hidden/full-lock mutation is rejected; movement pin remains vertex-editable.
- Focused and protected gates plus current-build 1920x1080 and 1366x768
  captures pass. Read `docs/POLYGON_VERTEX_EDIT_P2_20260728.md`; evidence is
  `artifacts/ui/polygon-vertex-p2-20260728`.
- Labeling-editor depth is now `3.3/5`; focused workstation maturity remains
  `4.0/5`.

The historical next dependency was P3 display-only image aids, now complete.

### Completed P3 display-only image aids (2026-07-28)

Status: `Complete`. Field validation: `Not evaluated`.

- One compact canvas-header `보기 보정` popup owns brightness, contrast, gamma,
  invert, histogram equalization, and reset; the annotation rail is unchanged.
- `WpfImageDisplayAdjustmentService` creates an owned copy. The shell replaces
  only the base texture after a 120ms coalescing delay.
- Canonical in-memory bitmap and disk-file hashes, annotation dirty/history,
  and overlay image coordinates remain unchanged through apply and reset.
- Queue navigation retains the same screen-session settings; Recipe, labels,
  export, training input, autosave, Preview, and Run remain untouched.
- Focused build is zero-warning/error; `--image-display-adjustment`,
  `git diff --check`, and current-build 1920x1080/1366x768 before/after visual
  evidence pass.
- Evidence: `docs\DISPLAY_ONLY_IMAGE_AIDS_P3_20260728.md` and
  `artifacts\ui\display-aids-p3-20260728`.
- Labeling-editor depth is `3.4/5`; focused workstation maturity remains
  `4.0/5`.

### Completed P4 Dataset Health visual QA (2026-07-28)

Status: `Complete`. Field validation: `Not evaluated`.

- The separate Dataset Health window has a fourth `시각 QA` tab.
- Detection/segmentation/anomaly rows are classified read-only; missing,
  corrupt, or unreviewed rows appear before at most 48 healthy samples.
- The text worklist is bounded to 500 rows and does not preload thumbnails.
  Only the selected image is decoded, at a maximum width of 800 pixels.
- Saved box, polygon, and raster-boundary overlays are composed without
  changing the image or annotation source.
- `문제만` narrows the worklist. `편집기에서 열기` closes Dataset Health and
  reuses the existing labeling workbench and image-loading path.
- Isolated build, focused Dataset Health tests, documentation gate,
  `git diff --check`, and current-build 1920x1080/1366x768 before/after
  evidence pass.
- Evidence: `docs\DATASET_HEALTH_VISUAL_QA_P4_20260728.md` and
  `artifacts\ui\dataset-health-visual-qa-p4-20260728`.
- The labeling-editor depth remains `3.4/5`; focused workstation maturity
  remains `4.0/5`.

### Completed P5-A dataset-interchange preflight (2026-07-28)

Status: `Complete`. Field validation: `Not applicable`; this is data
transformation safety, not model accuracy.

- One contextual `변환` action in Model Center > Data opens a separate window;
  the fourteen supported operations are not added to the permanent labeling
  rail.
- Existing COCO, Pascal VOC, Label Studio, and CVAT detection/segmentation
  converters remain the serialization owners.
- Dry-run executes the selected real converter only against an isolated
  temporary destination.
- Source SHA-256 and requested-target fingerprints must remain unchanged.
  Import source identity includes the annotation/archive and external image
  root where applicable.
- Image/annotation/class/skipped counts and segmentation loss warnings are
  shown. Skipped malformed/unsupported records block Apply.
- Apply is enabled only for the unchanged request signature that passed
  dry-run; no automatic conversion or save occurs.
- Focused build, `--dataset-interchange-preflight`,
  `--export-capability-inventory`, documentation gate, `git diff --check`, and
  current-build 1920x1080/1366x768 before/after evidence pass.
- Evidence: `docs\DATASET_INTERCHANGE_PREFLIGHT_P5A_20260728.md` and
  `artifacts\ui\interchange-preflight-p5a-20260728`.

Next dependency: independently acquired production-camera/cross-session data.
Recommended model: no model tokens until the prerequisite exists.

## 3. Product Identity and Direction

OpenVisionLab Labeling Studio is a local Windows workstation for industrial image workflows:

- create and review object-detection, segmentation, and anomaly-classification labels;
- prepare data, train through local YOLO workers, run inference, review candidates, and compare models;
- expose model quality, evidence identity, Takt, failure examples, and adoption guards to an operator;
- keep the full workflow local and reproducible.

Each recipe is the canonical source of image identity, class schema, annotations, splits, and evidence. Selected model adapters format that recipe data for their own input contracts, then normalize their results for the same candidate-review and evidence-comparison workflow.

"Multiple models" means verified adapters for explicitly supported local runtimes or repositories. It does not mean that every GitHub model is automatically compatible: a new adapter must define class, coordinate, split, training/inference, result, and evidence mappings before it is presented as supported.

The product is not currently a cloud collaboration, account, reviewer-assignment, deployment, fleet-management, or annotation-marketplace platform.

Current direction:

1. Make labeling and the role of classes, annotations, splits, and evidence understandable for a single operator.
2. Let one recipe's labeled data be reused across supported model formats instead of relabeling the same images per model.
3. Make training/inference/model comparison evidence-based rather than claim a winner from one metric.
4. Support local YOLOv5 and local-source YOLOv8 workflows without hiding runtime ownership, and add other model adapters only through explicit format and result contracts.
5. Prefer read-only data/model analysis screens over adding more text to the main workflow panel.
6. Keep current stable workflow areas intact unless a concrete defect is reproduced.

Commercial-product lessons already adopted:

- use task-local panels and tabs rather than one long all-purpose control column;
- keep image queue, canvas, current task, and model evidence visually distinct;
- show data readiness and model comparability explicitly;
- open dense analysis in separate windows rather than extending the left workflow panel;
- do not copy cloud collaboration or enterprise governance scope without a product decision.

The latest audit estimates focused single-operator workstation maturity at 4.0/5. This is a workflow-maturity estimate, not a model-accuracy percentage. General commercial-suite breadth and enterprise/team breadth remain materially lower. Source: docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md and docs/LABELING_STUDIO_COMMERCIAL_UX_GAP_REVIEW_20260710.md.

## 4. Non-Negotiable Engineering Rules

- Preserve MVVM: code-behind is a WPF adapter; commands, workflow state, presentation decisions, and persistence rules belong in ViewModel or service code when practical.
- Do not touch Viewer, OpenGL, ROI, brush, eraser, or overlay hot paths unless the user reports a specific defect. Add focused evidence when doing so.
- Do not download model weights, run pip/package upgrades, or change dependencies without explicit approval.
- YOLO11 detection, segmentation, and anomaly classification are verified only
  for the recorded local Ultralytics runtime, compatible task weights, and
  focused app paths. Do not generalize that evidence to every YOLO11 task,
  dataset, runtime, or production deployment. The recorded anomaly candidate
  remains `hold`.
- ONNX can support inference deployment, but does not replace local-source YOLOv8 training.
- UI changes require current 1920x1080 evidence and a README/tutorial-image relevance check.
- Completion requires a build when code changes, focused tests for the changed area, and git diff --check.
- The public README/tutorial must not receive local private paths or conversation notes.

## 5. Local Runtime Contract

YOLOv8 is local-source operated:

- root: C:\Git\yolov8
- source: ultralyticsMaster with the local ultralytics source checkout
- Python: C:\Git\yolov8\.venv\Scripts\python.exe
- adapter: C:\Git\yolov8\labeling_tcp_client.py
- install mode: editable local install
- pretrained seeds such as yolov8n-seg.pt and yolov8n-cls.pt are seeds only, never final production models.

YOLO11 reuses the same local Ultralytics source/runtime root and the bundled
Ultralytics worker; it does not require a separate `C:\Git\yolo11` repository.
Actual detection, segmentation, and anomaly-classification training/inference
evidence exists, but task-specific compatible weights and live worker
capability checks still gate execution. Pretrained `yolo11n-seg.pt` and
`yolo11n-cls.pt` files are seeds, not adopted models.

YOLOv5 remains a separately configured local runtime. Do not mix a weight, model engine, task, class list, or data.yaml across engines without explicit compatibility verification.

## 6. Completed and Protected Product Areas

Treat the following as completed/protected unless there is a reproduced defect:

- SEG brush-first labeling, brush/eraser performance, undo/redo, preview opacity, image-switch preview clearing, class recolor, pending-save navigation.
- Saved segmentation geometry: polygon/mask persistence, candidate review save, JSON/mask PNG/YOLO segment label export.
- YOLOv8 local worker training/inference plumbing and segmentation polygon transport. SEG inference presentation uses closed, unfilled contours with class/confidence labels; it must not fall back to a bounding box for a polygon candidate.
- Candidate Review, rejected-history guard, model-candidate decision safeguards, and separation between candidate evidence and adoption.
- Compact task-oriented shell structure: task tabs, current-work guidance, fixed canvas annotation-tool rail, and labeling-only hiding of duplicate viewer measurement chrome.
- Model-neutral comparison workspace: comparable-evidence guard, Takt/quality presentation, class and ground-truth error review, source-image/error geometry preview.
- Dataset quality audit and duplicate external-evaluation preflight.
- Per-machine splitter/layout persistence.
- Current image queue row state, keyboard navigation, light preview loading, lazy thumbnails, and 10K background catalog/detail indexing.
- Dataset Health is a separate read-only Model Center window, not another long left-panel block.
- External native YOLO data.yaml intake remains an explicitly selected training input and must not overwrite the recipe-owned exported dataset.
- The Model Center adapter catalog is read-only: it exposes only the declared recipe-format, YOLOv5 detection, local-YOLOv8, U-Net segmentation, ONNX inference-only, and verified-scope local-YOLO11 contracts. It must not imply that all GitHub models or all YOLO11 task/runtime combinations are executable.

For the exact contracts and required regression gates, read docs/STABLE_VERIFIED_AREAS.md before changing any protected area.

### Current Capability Snapshot

| Workflow area | Current state | Verified boundary / remaining limitation |
| --- | --- | --- |
| Recipe and dataset setup | Complete and protected | Dataset wizard, class schema, image queue, Dataset Health, external native YOLO intake, and deterministic Recipe Dataset Version v2 are implemented. Dataset Version is local immutable metadata, not a copied dataset or cloud VCS. |
| Labeling and rework | Core storage/review baseline complete; editor depth incomplete | Object boxes; segmentation polygon/brush/eraser/undo/redo; anomaly whole-image OK/NG review; template labeling; MobileSAM box-prompt candidates; Candidate Review; and the 10K review Worklist are covered. V7/CVAT-level repeat commands, object state, mask structure editing, and interactive point correction are not complete. Multi-user assignment, comments, and immutable reviewer history remain out of scope. |
| Training and inference | Complete for declared adapters | Local YOLOv5 detection, local YOLOv8 detection/segmentation/classification, recorded local YOLO11 detection/segmentation/classification, and U-Net segmentation have focused runtime evidence. ONNX remains inference-only. Arbitrary GitHub repositories are not automatically executable. |
| Model comparison and adoption evidence | Complete as a decision workflow | YOLOv5/YOLOv8 detection comparison, U-Net/YOLOv8/YOLO11 segmentation comparison, anomaly evaluation, provenance, error review, and fail-closed adoption guards exist. Current synthetic/same-source candidates are regression evidence and remain non-adopted unless their recorded decision says otherwise. |
| Documentation and beginner workflow | Complete for the current three task types | README/tutorial and current-EXE beginner audits cover object detection, segmentation, and anomaly classification. Refresh screenshots only after a visible UI change. |
| Field model quality | Incomplete because evidence is unavailable | Independent, provenance-confirmed production-camera/cross-session detection, segmentation, and anomaly held-outs are still missing. This limits production/adoption claims, not the implemented workflow. |
| Network/team/platform breadth | Deferred or out of scope | Shared/network-storage queue behavior remains unverified and inactive. Accounts, cloud collaboration, workforce management, deployment, PLC/fleet control, video/3D/keypoints, and a generic model marketplace are outside the approved product direction. |

## 7. Committed Feature Slice Map

The former dirty worktree was split into the following reviewed local commits. Keep these ownership and verification boundaries separate when a future requirement changes one area.

### A0. Anomaly OK/NG Image Review and Folder-Name Consent - committed at 85d91e9

Files include:

- 1. Core/Anomaly/AnomalyImageReviewStatusService.cs
- 1. Core/Anomaly/AnomalyClassificationTrainingReadinessService.cs
- Yolo/AnomalyClassificationDatasetExportService.cs
- 0. UI/9) WPF/ViewModels/Labeling/WpfImageQueuePanelViewModel.cs
- 0. UI/9) WPF/Views/WpfImageQueuePanel.xaml
- 0. UI/9) WPF/Views/WpfLabelingShellWindow.ImageQueue.cs
- 0. UI/9) WPF/Views/WpfLabelingShellWindow.PanelWiring.ImageQueue.cs
- 0. UI/9) WPF/Views/WpfLabelingShellWindow.xaml.cs
- tests/LabelingApplication.Tests/Program.cs

Behavior and boundary:

- Anomaly labeling is an image-level `OK/NG 이미지 판정` workflow. The current-image actions are `정상(OK) → 다음`, `이상(NG) → 다음`, and `미판정으로 되돌리기`; they persist through the existing anomaly review status and do not require drawing.
- Anomaly purpose hides object/segmentation label-save, annotation-tool, saved-object/class, label-layer, and queue detection/batch surfaces. Its queue columns/badges show `판정`/`상태` and `OK`/`NG`/`미판정`; generic YOLO refreshes must not overwrite them. Leaving anomaly purpose restores the standard annotation workspace.
- Parent `OK`/`normal` and `NG`/`abnormal` folders are detected as an optional anomaly-review proposal, never as an implicit label. Opening a folder, checking training readiness, or exporting must leave unreviewed images unreviewed.
- The temporary Image Queue card offers `N장 일괄 판정` and `이미지별 확인`; it is not a permanent top-header action. Applying affects only unreviewed images and preserves saved manual decisions. Direct review hides the proposal for the same image-root session without persistence.
- If the selected anomaly root has no direct images, its child-folder paths are interleaved in the queue and the temporary card title reports the included total. Do not return to a path sort that makes every `NG` row appear before `OK` rows.
- When opening the automatic first nested file or selecting any queue row, retain the operator-selected image root. Never repopulate the queue from the clicked file's leaf `NG` or `OK` parent folder.
- Training readiness and classification export use saved/explicitly approved review states only. Do not restore automatic folder-state import without a new user decision and regression evidence.
- The exact staged tree for commit `85d91e9` passed the isolated 0-warning/0-error build, `--anomaly-folder-auto-review`, `--wpf-anomaly-purpose-flow`, `--anomaly-classification-dataset-export`, `--anomaly-classification-training-workflow`, `--wpf-labeling-shell`, `--priority-workflow-docs`, and diff checks. The dedicated command/persistence/purpose-switch and root-retention assertions are in `--anomaly-folder-auto-review`; UI evidence is `artifacts\ui\anomaly-ok-ng-review-20260719\before-anomaly-review-1920.png` and `after-staged-slice-1920.png`.

### A. Image Queue 10K Responsiveness - committed at 93c6bfb

Files include:

- 0. UI/9) WPF/Models/WpfImageQueueModels.cs
- 0. UI/9) WPF/Services/ImageQueue/WpfImageQueueSelectionService.cs
- 0. UI/9) WPF/Views/WpfLabelingShellWindow.ImageQueue*.cs
- DatasetSetupCommands.cs, ImageQueueCommands.cs, ImageQueuePresentation.cs, PanelWiring.ImageQueue.cs, ShellLifecycle.cs, WpfLabelingShellWindow.xaml.cs
- tests/LabelingApplication.Tests/Program.cs

Behavior:

- interactive root/recipe/refresh commands create a cancellable background catalog;
- stale catalog and detail results cannot replace a newer folder;
- row lookup is path-indexed;
- detail scan uses four background workers and 64-row UI batches;
- live filtering handles row state changes, then one final full view refresh completes exact counts;
- thumbnails remain lazy.

Direct current-session evidence:

- isolated test-project build passed with 0 warnings and 0 errors;
- latest 10K catalog recheck: 16.1ms, one collection reset, stale replacement rejected;
- latest 10K valid-image detail recheck: 70.5s on a synthetic local temporary disk, while UI input completed in 78.9ms and filtering evaluated 10,000 rows once;
- current-source warm-cache profile of the user-provided local mixed root: 50,081 images / 1.47GB; catalog return 13.9ms, catalog completion 11.7s, detail completion 406.5s after catalog, catalog/detail dispatcher input 142.0ms/84.9ms, middle/final selection 207.4ms/318.3ms, no empty dimensions, and 1,036.8MB working set after detail; evidence: `artifacts\image-queue-operator-profile\20260717-225226-warm-cache`;
- current-source local 8K duplicate-file profile of `D:\새 폴더`: 8,000 JPG paths / 476.2MB; catalog return 12.8ms, catalog completion 2.3s, detail completion 80.2s after catalog, catalog/detail dispatcher input 131.1ms/69.9ms, middle/final selection 182.8ms/121.7ms, no empty dimensions, and 365.3MB working set after detail. The before/after metadata manifest SHA-256 remained `072643A7ED96F109E245271AC6BDAF85D26A174BE9A1203D16B245CF462F76F9`. A complete content SHA-256 audit found 250 unique images, each copied 32 times; evidence: `artifacts\image-queue-operator-profile\20260717-231924-local-8k-production-sample`;
- queue status, keyboard, root switch, selection service, 1,200-item lazy-thumbnail, 125-item click-performance, and shell tests passed;
- true before/current-source after 1920x1080 captures: artifacts\ui\image-queue-10k-20260717.

Boundary: the 70.5s synthetic recheck, 50K local warm-cache profile, and local duplicate-file 8K profile are not network-share throughput promises. The 8K folder's production-camera provenance was not supplied; it has only 250 distinct contents repeated 32 times, and its before/after metadata manifest is not a content-tree hash. The local profiles retained an interactive dispatcher while full detail indexing took 6.8 minutes and 80.2 seconds respectively; do not add a database, paging system, or another image cache without a representative network-share or provenance-confirmed production-camera measurement.

Review the isolated file ownership, latest-request-wins contract, shared-file hunk boundaries, and acceptance commands in `docs/IMAGE_QUEUE_10K_REVIEW_SLICE.md` before staging or changing this slice.

### B. Dataset Health - committed at 4f65f08

Committed files include:

- 0. UI/9) WPF/ViewModels/Dataset/WpfDatasetHealthViewModel.cs
- 0. UI/9) WPF/Views/WpfDatasetHealthWindow.xaml
- 0. UI/9) WPF/Views/WpfDatasetHealthWindow.xaml.cs
- 0. UI/9) WPF/Views/WpfLabelingShellWindow.DatasetHealth.cs
- Yolo/YoloDatasetHealthService.cs
- tests/LabelingApplication.Tests/Program.DatasetHealth.cs

Modified shell/XAML/ViewModel files wire the read-only Model Center entry.

Current ownership after the 2026-07-26 structural refactor:

- `Yolo/YoloDatasetHealthContracts.cs` owns the shared Dataset Health result types.
- `Yolo/YoloDatasetHealthService.cs` owns purpose-aware aggregation.

Recorded scope:

- purpose-aware overview for object detection, segmentation, and anomaly classification;
- separate FluentWindow with data summary, split/label state, and class distribution;
- it reuses readiness/quality services and must not modify labels, training, inference, model registry, or adoption;
- it intentionally excludes externally selected native data.yaml aggregation.

Recorded gates are --dataset-health, --wpf-dataset-health-window, --dataset-readiness-purpose, --dataset-quality-audit, and --wpf-labeling-shell. A current-source focused recheck passed all five after an isolated 0-warning/0-error build; its fresh 1920×1080 capture is `artifacts\ui\dataset-health-20260717-current-review\dataset-health-current-1920.png`. The detailed evidence is in docs/STABLE_VERIFIED_AREAS.md. Rerun these gates before changing or committing this slice.

Review the isolated file ownership, SEG false-normal contract, shared-file hunk boundaries, and acceptance commands in `docs/DATASET_HEALTH_REVIEW_SLICE.md` before staging or changing this slice.

### C. External Native YOLO data.yaml Intake - committed at d1ce5fc

Committed files include:

- 0. UI/9) WPF/Views/WpfLabelingShellWindow.ExternalYoloDatasetIntake.cs
- Yolo/YoloExternalDatasetIntakeService.cs
- tests/LabelingApplication.Tests/Program.ExternalYoloDatasetIntake.cs
- tests/LabelingApplication.Tests/Program.RealExternalYoloDatasetTraining.cs

Related committed files include:

- WpfLearningWorkflowPanelViewModel.cs, WpfTrainingSettingsPanelViewModel.cs, TrainingStatus.cs, panel wiring, shell XAML, LabelingProjectSettings.cs, YoloTrainingWorkflowService.cs, LearningProtocol.cs, CCommunicationLearning.cs, Runtime/Python/openvisionlab_ultralytics_worker.py, and Program.cs.

Recorded behavior:

- validate a native object-detection or segmentation data.yaml, including paths, names, split separation, labels, and normalized coordinates;
- persist a separate external profile and require explicit activation for the next training run;
- persist a SHA-256 identity for the YAML plus referenced images/labels; revalidate it immediately before training, require explicit reactivation when it changes, block any silent fallback to the internal recipe dataset until reactivation or explicit clearing, send the original YAML path to the worker, and preserve the internal recipe export unchanged;
- resolve YAML-relative paths from the YAML directory;
- isolate YOLOv5 Unicode-path staging/cache and YOLOv8 cache cleanup from source data;
- latest opt-in runtime evidence: the EasyMatch SEG source completed one YOLOv8 epoch at image size 320/batch 4, while the full 1,207-file source manifest remained exactly unchanged (aggregate SHA-256 `B137A8EE8F2CAB265AA660874CC3B23C1BFA07D59CDBA0A2B74FD1DE26F98E2D`); the artifact-local copied `best.pt` is not registered or adopted;
- do not support external anomaly-classification YAML intake in this slice.

This is source-data interoperability and runtime safety, not source-data quality, model adoption, or a license to mutate user-supplied data. The detailed current evidence and gates are in docs/STABLE_VERIFIED_AREAS.md.

Completion record: this intake review is complete. The current-source isolated build passed with 0 warnings / 0 errors; `--external-yolo-dataset-intake`, `--wpf-labeling-shell`, Python worker compile, and worker `--self-test` passed. Current-source UI evidence is `artifacts\ui\external-yolo-intake-20260717-current-review\external-yolo-intake-current-1920.png`. The existing opt-in 1-epoch artifact remains the source-immutability runtime proof and was not rerun because its completed scope and evidence remain valid. No model was registered or adopted.

Review the isolated file ownership, shared-file hunk boundaries, contract, and acceptance commands in `docs/EXTERNAL_NATIVE_YOLO_INTAKE_REVIEW_SLICE.md` before staging or changing this slice.

### D. Model and Anomaly Workflow Changes - committed at 6f202db and cdad0be

Committed files include:

- 1. Core/Anomaly/AnomalyImageReviewStatusService.cs
- 1. Core/Model/YoloTrainingWorkflowService.cs
- 3. Communication/TCP/CCommunicationLearning.cs
- 3. Communication/TCP/LearningProtocol.cs
- Runtime/Python/openvisionlab_ultralytics_worker.py
- scripts/compare-yolo-models.ps1
- tests/LabelingApplication.Tests/Program.RealYoloV8AnomalyFolderTraining.cs
- tests/LabelingApplication.Tests/Program.cs

Recorded outcomes:

- Washer synthetic anomaly candidate completed runtime persistence/restart evidence but is hold on external circular and MultiIndustry synthetic checks at the current confidence gate.
- EasyMatch native segmentation training/inference and native detection YOLOv5-versus-YOLOv8 comparison completed as controlled synthetic evidence. The 60-image native test favors YOLOv8n for the disclosed conditions, but neither model is auto-registered/adopted.
- Prediction manifests prevent nested path/stem collisions and restore Ultralytics generated prediction labels to original source stems for review.
- The current source packages remain unchanged according to recorded hash/cleanup evidence.

Completion record: this focused review is complete. The isolated build passed with 0 warnings / 0 errors; anomaly folder/training/runtime/evaluation gates, bundled-worker contract, local adapter/worker compile, adapter self-test, comparison run-service contract, and PowerShell parser check all passed. No real training or five-repeat comparison was rerun because it is outside this review scope. Recorded runtime/benchmark evidence must not be reported as independent quality evidence or adoption approval.

Review `docs/MODEL_ANOMALY_COMPARISON_REVIEW_SLICES.md` before staging or changing this group. It separates the nested anomaly/runtime and native comparison-manifest changes, names their shared-file hunks, excludes external-intake behavior, and states the acceptance gates.

### E. Documentation Checkpoint

When current statements disagree, use this source order:

1. `AGENTS.md` for repository operating rules and completion gates.
2. `docs/NEXT_THREAD_HANDOFF.md` for the latest verified project state.
3. `docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md` for current product scope, maturity, and commercial comparison.
4. `CODEX_NEXT_PROMPT.md` for the next bounded action.
5. `docs/WORK_TRACKING.md` and `docs/STABLE_VERIFIED_AREAS.md` for evidence history and protected behavior.

Read the final diff before changing these records. Historical entries remain evidence journals; update only the current checkpoint and a contract whose source or acceptance criteria changed.

## 8. Model Evidence: What Is Complete and What Is Not

### Segmentation

Completed:

- historical 124-target contour correction was approved, backed up, and retrained;
- current local YOLOv8 SEG runtime returns polygon/mask artifacts;
- corrected candidate comparison and runtime smoke exist.

Not complete:

- corrected contour candidate remains hold because its fixed validation set has one NG positive image;
- no independent cross-session/production-camera segmentation test set with sufficient NG masks exists;
- do not perform another historical geometry rewrite without a new source contour, preflight, and explicit user approval.

### Object Detection

Completed:

- YOLOv5 versus YOLOv8 comparison plumbing, evidence fingerprints, model-neutral report, class/error review, and native external-YAML compatibility paths;
- controlled same-source Test01 and recent synthetic EasyMatch comparisons provide engine/regression evidence.
- user-authorized Switch Housing synthetic cross-product test: 60 held-out native object-detection images, five repeats, artifact-only relative-YAML materialization, and a candidate `hold`; the 300-image Switch Housing anomaly evaluation also holds the existing Washer classifier.

Not complete:

- independent NG-rich industrial camera/session test data is missing;
- Test01/Test02 duplication cannot count as independent evidence;
- synthetic packages do not authorize a production model choice.

### Anomaly Classification

Completed:

- image-level normal/abnormal review semantics, local YOLOv8 classification training/runtime mapping, evaluation guard, model profile persistence, and restart smoke;
- external synthetic evaluation paths demonstrate current candidate failure across domain changes.

Not complete:

- no balanced independent production-camera/cross-session normal and abnormal held-out set;
- current Washer candidate is hold, not adopt;
- evaluation data must remain outside training when used as an external regression set.

### YOLO11

Verified for the recorded local Ultralytics detection, segmentation, and
anomaly-classification paths.
The detection 30-epoch benchmark/restart smoke is recorded in
`docs\YOLO11_ENGINE_COMPARISON_20260721.md`; the segmentation 30-epoch
training and normalized three-model comparison are recorded in
`docs\SEGMENTATION_E30_THREE_MODEL_COMPARISON_20260722.md`; the classification
training, fixed evaluation, Model Center route, and actual-EXE restart inference
are recorded in
`docs\YOLO11_ANOMALY_CLASSIFICATION_PREREQUISITE_AUDIT_20260723.md`. At
confidence `0.8`, YOLO11 scored `82/104` versus YOLOv8 `90/104` on the same
synthetic test fingerprint, so both remain `hold`. Keep compatible task weights,
worker capability, source identity, and non-adoption guards explicit. Arbitrary
external YOLO11 runtimes and production accuracy remain unverified.

## 9. Known Gaps, Risks, and TODO Scan

- The former dirty feature slices, relocation/runtime verification, Recipe
  Dataset Version v2, truth alignment, and YOLO11 anomaly closure are committed
  and pushed through `ad569dc`. Do not repeat their focused reviews unless
  source, requirements, environment, or evidence validity changes.
- GitHub Actions CI #22 passed for historical commit `58166f8`; CI has not been
  rechecked for `ad569dc` and must not be implied current.
- Image-queue behavior on shared/network storage and provenance-confirmed production-camera folders is unverified. Mixed local 50K warm-cache and local duplicate-file 8K profiles exist, but neither is a network result; the 8K source has only 250 distinct contents copied 32 times and is not a production-data proxy. The operator removed this unavailable profile from the active priority list on 2026-07-18; retain the risk record without treating it as next work.
- Model quality remains data-limited, not implementation-limited.
- The supplied circular-disk 500 OK / 500 NG package now has completed synthetic anomaly and exact metadata-backed object-detection evidence. It does not satisfy the independent production-camera requirement. Full record: `docs\CIRCULAR_DISK_SYNTHETIC_1000_EVIDENCE_20260720.md`.
- Collaboration, reviewer assignment, cloud sync, account management, deployment, and enterprise governance are out of scope.
- A repository source scan for TODO, FIXME, and HACK excluding artifacts/bin/obj/tutorial outputs returned no hits in this handoff pass. This does not mean every product gap is complete; use the explicit gaps above.

## 10. Next Priorities

The authoritative ordering is:

1. Dataset Health class-filter observation gate. Do not implement until a
   real Detection or Segmentation review session reproduces a task that the
   existing split plus problem filters cannot reduce.
   Prerequisite: recorded operator-review observation.
   Recommended model: none until the prerequisite exists
   Reasoning effort: n/a

2. Object Review persistent metadata. `occluded` and Recipe-defined tags are
   Complete with editing, badges, combined filters, Recipe definition
   save/reopen/reset, and separate per-image sidecars written only by explicit
   label save. Existing hide/full-lock/movement-pin stays session-only.
   Same-image group focus/filter/batch metadata is also Complete in
   `docs/OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md`, with schema-v2
   save, v1 load, a dedicated selection set, and defined mutation rules.
   Training behavior and external interchange remain deliberately excluded.

3. Polygon/raster cross-family z-order. Current canonical order and each
   overlay family's order are verified, but exact cross-family composition is
   not. Because this touches a protected renderer path, require a reproduced
   visual-order defect, before/after current-build evidence, and focused
   performance gates.
   Prerequisite: reproduced renderer defect.
   Recommended model: none until the prerequisite exists
   Reasoning effort: n/a

4. Independent object-detection field evaluation.
   Prerequisite: a newly approved NG-rich camera/session source with
   trustworthy boxes, provenance, and content-separated held-out data. The
   operator-excluded `D:\기타이미지\2022.11.16_SIT 이미지` path must not be
   inspected or used.
   Recommended model: none until data is available
   Reasoning effort: n/a

5. Independent anomaly field evaluation.
   Prerequisite: balanced normal/abnormal production-camera and cross-session
   data kept outside training initially. YOLOv8 `90/104` and YOLO11 `82/104`
   on the same circular synthetic test both remain `hold`; do not tune against
   that test.
   Recommended model: none until data is available
   Reasoning effort: n/a

Do not list P3, P4, P5-A, or P5-B as future implementation. They are complete.
Do not reopen P0-A/P0-B/P1/P2 for general polish. Use the explicit ready and
prerequisite-blocked gaps above.

The detailed current contract is
`docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md`. Keep the task
tabs, main shell, image queue, Dataset Health, model-comparison workspace,
Model Center, adapter catalog, Recipe source-of-truth, Candidate Review, and
explicit Preview/Run/training behavior stable. Collaboration, comments,
accounts, cloud sync, deployment, video tracking, 3D, camera/PLC/I/O, and
automatic candidate save remain out of scope.

## 11. Focused Verification Menu

Use only the relevant commands for the slice being changed.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-LabelingApplicationTests.ps1 -OutputName isolated-out

dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --priority-workflow-docs

dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-10k-responsive
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-10k-detail-responsive
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-status
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-root-switch
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-queue-click-perf --count 125 --measure-detail-refresh --settle-ms 60

dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --dataset-health
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-dataset-health-window
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --external-yolo-dataset-intake
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --dataset-readiness-purpose
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --dataset-quality-audit
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --recipe-dataset-version-v2
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-labeling-shell
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --model-adapter-catalog
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --mobile-sam-box-prompt
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --real-mobile-sam-point-correction
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --exe-smart-mask-point-smoke --exe .\artifacts\run\Debug\OpenVisionLab.LabelingStudio.exe
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-yolo-model-settings-panel

dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --anomaly-classification-training-workflow
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --anomaly-classification-evaluation
dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll --wpf-yolov8-anomaly-classification-runtime-smoke

C:\Git\yolov8\.venv\Scripts\python.exe -m py_compile C:\Git\yolov8\labeling_tcp_client.py Runtime\Python\openvisionlab_ultralytics_worker.py
C:\Git\yolov8\.venv\Scripts\python.exe -m py_compile Runtime\Python\openvisionlab_yolo_classification_batch.py
C:\Git\yolov8\.venv\Scripts\python.exe Runtime\Python\openvisionlab_yolo_classification_batch.py --self-test
C:\Git\yolov8\.venv\Scripts\python.exe C:\Git\yolov8\labeling_tcp_client.py --self-test

git diff --check
~~~

Run real external-data training, real EXE, or model-comparison commands only when the user has approved the data/runtime cost and the task specifically needs that evidence.

## 12. 2026-07-18 Model Center Workspace Slice

- Status: `Complete` for the approved stage-4 layout scope.
- `TrainingModel` now uses a dedicated full-width Model Center; it hides the inactive canvas, image queue, splitters, and dock-collapse control only while the model stage is active. Returning to Dataset, Labeling, or Inference restores the normal workspace and its saved image-queue width.
- Current-source evidence: required isolated 0-warning/0-error build; `--wpf-labeling-shell`, `--wpf-responsive-layout`, and `--wpf-training-settings-panel` passed. Before/after evidence is `artifacts\ui\model-workspace-20260718\before-model-center-1920.png`, `after-model-workspace-1920.png`, and `after-model-workspace-1366.png`.
- Boundary: this is presentation-only. It does not add a model engine, a training/inference path, or a model-quality/adoption claim. The separately completed model-adapter catalog retains the existing YOLOv5/YOLOv8 evidence boundaries.

## 13. 2026-07-18 Model Adapter Catalog Slice

- Status: `Complete` for the declared-contract scope.
- The full-width Model Center presents six Korean, read-only cards: implemented recipe interchange formats, YOLOv5 object detection, the local YOLOv8 worker, U-Net segmentation, ONNX inference-only, and verified-scope local YOLO11. Each card declares 작업, 데이터, 실행기, 근거, and 다음 행동. U-Net is segmentation-only. YOLO11 detection, segmentation, and anomaly classification are executable only within the recorded local runtime/weight/app paths; its anomaly candidate remains `hold`, and arbitrary external runtimes remain unverified.
- `ModelAdapterCatalogService` derives the format inventory from the implemented export capability service. It does not create a generic GitHub download/run path, alter a runtime, install a package, modify data, start training/inference, register a model, or make a quality/adoption claim.
- Current-source evidence: required isolated 0-warning/0-error build; `--model-adapter-catalog`, `--wpf-yolo-model-settings-panel`, `--wpf-labeling-shell`, and `--wpf-responsive-layout` passed. Current captures are `artifacts\ui\model-adapter-catalog-20260718\after-model-adapter-catalog-1920.png` and `after-model-adapter-catalog-1366.png`. The closest pre-catalog baseline is `artifacts\ui\model-workspace-20260718\after-model-workspace-1920.png`; it is not a catalog-specific runtime-tab before capture.
- Boundary: do not reopen for generic model-platform breadth. Reopen only for an incorrect/missing declared contract, stale export inventory, binding failure, or reproduced layout defect. Data-dependent quality priorities remain unchanged.

## 14. Final Reporting Contract

When finishing a future task, report:

- changed files;
- exact verification commands and results;
- whether a 1920x1080 screenshot was required and the before/after paths when it was;
- what remains unverified, blocked, or risky;
- the next priority with model and reasoning-effort guidance;
- no claim of model adoption, independent accuracy, YOLO11 scope beyond its recorded evidence, or CI success without current evidence.

## 15. 2026-07-21 Latest Checkpoint: External Native Segmentation Pair

Status: `Complete` for the runtime/provenance feature slice and the controlled YOLO confidence selection plus one held-out replay; production quality remains intentionally unclaimed.

- The external-native-segmentation foundation was committed before this checkpoint. The later YOLO11 comparison extension is independently committed as `687e553`; the synthetic evidence contract is `0b05986`; MobileSAM labeling is `549a7d4`. These commits were pushed to `origin/main` on 2026-07-22.
- An explicitly activated external native YOLO segmentation `data.yaml` is now parsed only by `YoloExternalDatasetIntakeService`; `ExternalYoloSegmentationCanonicalExportService` derives recipe-owned image/mask/class artifacts under `artifacts\unet-ext`. It maps native `val` to canonical `valid`, preserves the native class order, rejects duplicate cross-split content and different-class pixel overlap, and verifies source identity before/after export.
- U-Net training and U-Net/YOLO-seg Model Center comparison use that canonical export. Any external YOLO training, even a conventional `images`/`labels` source, uses a separate app-owned runtime copy so training caches cannot change the selected source. Persisted provenance keeps both the selected source and actual runtime path distinct.
- Current actual evidence: the approved 30-epoch same-source run is complete. U-Net (CUDA) and YOLOv8-seg (installed CPU-only runtime) both used the 360/80/60 EasyMatch Die Array packet, five-class contract, image size 320, and batch 4. The original 2,004-file source tree SHA-256 and native source fingerprint remained unchanged. The 60-image Model Center common-mask report measured U-Net Dice/IoU `0.243091` / `0.156165` and YOLOv8-seg `0.079059` / `0.044103`. See `artifacts\benchmark-external-unet-die-array-e30-20260721-203302\summary.txt`, `artifacts\benchmark-external-yolov8-die-array-e30-20260721-203302\summary.txt`, and `artifacts\benchmark-external-seg-adapter-compare-e30-20260721-203302\summary.txt`.
- The saved test prediction manifests prove the original paired evidence runner deliberately used `confidence=0.00`. The actual Model Center service passes the profile confidence and falls back to `0.25`. A read-only replay of the fixed YOLOv8-seg checkpoint on the 80-image `valid` split at `0.25` (not test) reduced the all-image false-positive flood and yielded per-class Dice `0.782156`-`0.854240`. U-Net's two zero-Dice classes have train support and remain a separate class-confusion/training question. Evidence: `docs\SEGMENTATION_E30_ERROR_ANALYSIS_20260721.md` and `artifacts\segmentation-e30-error-analysis-20260721`.
- The opt-in runner now exposes `--yolo-confidence`, defaults it to `0.25`, rejects values outside `[0,1]`, and records the value in its summary. The selected `0.25` then ran exactly once on unchanged test data: U-Net Dice/IoU `0.243091` / `0.156165`; YOLOv8-seg `0.721702` / `0.570198`; source fingerprint unchanged before/after. Evidence: `docs\SEGMENTATION_E30_CONFIDENCE025_TEST_EVIDENCE_20260722.md` and `artifacts\benchmark-external-seg-adapter-compare-e30-confidence025-test-20260722`.
- U-Net class-confusion, class-weighted, crop, foreground-quality selector, and CE plus foreground soft-Dice experiments are recorded on `train`/`valid`. The selector baseline reached valid macro Dice/IoU `0.204437` / `0.127053`. The soft-Dice run recovered `foreign_particle` but left `contamination_spot` at zero overlap and reduced macro Dice/IoU to `0.189220` / `0.111142`, so it is rejected and its temporary code is removed. The selector remains an internal opt-in evidence harness; normal TCP training still uses unweighted cross-entropy and validation-loss selection. No held-out test was used by the loss experiment. Evidence: `docs\UNET_E30_CLASS_CONFUSION_ANALYSIS_20260722.md`.
- Boundary: this is an engine/model evidence result, not automatic selection or production quality. CUDA/CPU elapsed times cannot be compared. Do not rerun this held-out split unless source, runtime, acceptance criteria, or a deliberately new hypothesis changes.

Production adoption remains blocked on independently acquired camera/session data with trustworthy object-detection boxes, segmentation masks, or balanced anomaly OK/NG decisions. Product feature work is not blocked: synthetic evidence may close a feature under `docs\SYNTHETIC_EVIDENCE_CONTRACT.md`. `D:\라벨테스트` remains synthetic or lacks acceptable acquisition provenance, and the operator-excluded `D:\기타이미지\2022.11.16_SIT` path must not be inspected or used.

## 16. 2026-07-22 Latest Checkpoint: YOLO11 Segmentation and Three-Model Evidence

Status: `Complete` for the declared local runtime, fixed synthetic source, and
normalized held-out comparison. No model was adopted.

- YOLO11-seg completed 30 CPU epochs through the app's real TCP training path
  at image `320`, batch `4`, using the fixed five-class `360/80/60` native
  packet and an app-owned runtime copy.
- The source stayed 2,004 files with identical before/after tree SHA-256
  `5819E2ED72E402D3F06C32CF4F1FB3481A2DF1D70BD8CB8C00B97CE9E28199C2`.
  The YOLO11 checkpoint SHA-256 is
  `4A09B5F668B8F2AA2DAF9FEDB9ADDA4954A607D61CB08C96379AE8CA82462ECA`.
- The same 60 canonical test masks at YOLO confidence `0.25` measured mean
  Dice/IoU U-Net `0.243091/0.156165`, YOLOv8-seg `0.721702/0.570198`, and
  YOLO11-seg `0.773711/0.636553`. This is a synthetic same-source engine
  benchmark, not production evidence or automatic selection.
- The comparison runner now records an explicit YOLOv8/YOLO11 engine. A
  reproduced Windows long-path failure is closed by compact artifact names
  while manifests retain the full SHA-256 identities; the final deliberately
  long real comparison passed.
- Evidence: `docs\SEGMENTATION_E30_THREE_MODEL_COMPARISON_20260722.md`,
  `artifacts\benchmark-external-yolo11-die-array-e30-20260722\summary.txt`, and
  `artifacts\benchmark-external-seg-adapter-compare-yolo11-e30-confidence025-test-pathfix2-20260722\summary.txt`.

The subsequent U-Net CE plus foreground soft-Dice valid-only hypothesis is now
complete and rejected: same-valid macro Dice/IoU fell to `0.189220/0.111142`
and `contamination_spot` remained zero-overlap. The temporary loss path was
removed and the 60-image test set stayed closed. Production-readiness remains
blocked on independently acquired camera/session evidence, but product feature
work continues under the synthetic evidence contract.

## 17. 2026-07-22 Latest Checkpoint: MobileSAM Box Smart Mask

Status: `Complete` for the bounded local labeling-assist feature. Field
validation is `Not evaluated`; no production accuracy is claimed.

- In a segmentation recipe, the last operator-drawn rectangle can invoke
  `박스 → 스마트 마스크`. The app reuses the existing local Ultralytics runtime
  and `mobile_sam.pt`, shows one polygon as an unconfirmed AI candidate, and
  requires the normal confirm/skip flow. Confirmation invokes the existing
  canonical annotation save path.
- The assist preserves confirmed candidates, does not auto-save, and fails
  closed if the current image or prompt changes during inference. The prompt
  rectangle is removed only after its candidate is accepted into review state.
- A real synthetic defect prompt `[369,226,43,18]` produced a 44-point contour
  and 540-pixel mask through MobileSAM / Ultralytics `8.4.101` / Torch
  `2.12.1+cpu`. Confirmation wrote canonical segment JSON and mask PNG. The
  source image SHA-256 stayed
  `92202A4CBC1A6C5949FC0AE7AF9918304288FD1CC8863214010AC843EBA611D4`.
- Weight SHA-256:
  `6DBB90523A35330FEDD7F1D3DFC66F995213D81B29A5CA8108DBCDD4E37D6C2F`.
- Evidence: `docs\MOBILE_SAM_SMART_MASK.md`,
  `artifacts\mobile-sam-box-prompt\20260722-150938\mobile-sam-evidence.json`,
  and current-build prompt/candidate captures under
  `artifacts\ui\smart-mask-20260722`.
- Historical boundary: the completed slice is box prompt only.
  Point/negative/text prompts, multi-object automatic labeling, MobileSAM
  training, automatic confirmation, and field accuracy were excluded from that
  slice. The 2026-07-27 labeling-editor roadmap now approves positive/negative
  point refinement as a separate P0-B product slice after P0-A; text prompts,
  broad automatic labeling, training, automatic confirmation, and field
  accuracy remain excluded.

## 18. 2026-07-22 Latest Checkpoint: MobileSAM 8-Class Usability Matrix

Status: `Complete` for the fixed synthetic exact-box evaluation. Field
validation is `Not evaluated`; section 21 supersedes the earlier unevaluated
small-box tolerance boundary.

- One single-defect image per class was fixed from each train/valid/test split:
  24 unique images, three per each of eight defect classes.
- All 24 real MobileSAM calls produced candidates with IoU `>= 0.50`. Overall
  median IoU was `0.8562`; the lowest class median was `crack 0.7129`.
- Runtime was MobileSAM / Ultralytics `8.4.101` / Torch `2.12.1+cpu` / CPU.
  Weight SHA-256 stayed
  `6DBB90523A35330FEDD7F1D3DFC66F995213D81B29A5CA8108DBCDD4E37D6C2F`.
- The source remained 4,525 files with tree SHA-256
  `4E511A2E08F2ED609B78B40D6B789DE691C968E71ED5A298B76A1E7CA1FB52A8`
  before and after.
- Historical decision: keep the current box-only plus polygon/brush fallback.
  The later fixed box-jitter regression passed. The 2026-07-27 commercial
  labeling review supersedes only the old product-priority restriction and
  schedules positive/negative refinement as P0-B; the completed matrix and its
  field-accuracy boundary remain unchanged.
- Evidence: `docs\MOBILE_SAM_8_CLASS_USABILITY_MATRIX_20260722.md` and
  `artifacts\mobile-sam-usability-matrix\20260722-153003`.

## 19. 2026-07-22 Latest Checkpoint: Feature-Slice Commit Review

Status: `Complete`. The mixed worktree was split without discarding or
overwriting existing changes.

- `687e553 feat: add YOLO11 segmentation comparison evidence`: YOLO11 engine
  selection, compact collision-checked prediction paths, and the controlled
  three-model report. Detached-worktree isolated build passed with 0 warnings
  and 0 errors; canonical export, segmentation comparison, and Python exporter
  self-test passed.
- `0b05986 feat: define synthetic evidence completion contract`: synthetic
  completion/field-validation boundary, rejected U-Net experiment record, and
  model-comparison wording. Detached-worktree isolated build, model-comparison
  review, and priority-doc tests passed.
- `549a7d4 feat: add MobileSAM smart-mask labeling`: local box-prompt worker,
  review-first WPF flow, canonical save integration, fixed 8-class matrix,
  public tutorial, and current captures. Independent review added a missing
  prompt-coordinate equality guard so an edited prompt cannot accept a stale
  result. Detached-worktree isolated build, Python compile/self-test,
  MobileSAM contract, polygon save, WPF shell, and priority-doc tests passed.
- This historical review ended before push. The later explicit push advanced `origin/main` to `6a4ab11f576ed6a422d7025645c98a8613806129`.

Boundary / next dependency: these feature slices are complete, committed, and
pushed. Do not repeat their
training/evaluation merely to produce another result; reopen only for changed
source/runtime/contracts or a focused regression.

## 20. 2026-07-22 Latest Checkpoint: Explicit Main Push

Status: `Complete`.

- The operator explicitly requested push. A normal, non-force
  `git push origin main` advanced the remote from `4dda0d9` to `6a4ab11`.
- Local HEAD, `origin/main`, and `git ls-remote --heads origin main` all matched
  `6a4ab11f576ed6a422d7025645c98a8613806129` after fetch.
- No pull request was created. No source file changed as part of the push.

## 21. 2026-07-22 Latest Checkpoint: MobileSAM Box-Jitter Matrix

Status: `Complete` for the declared synthetic small-box-error range. Field
validation remains `Not evaluated`.

- The same fixed 24 images were evaluated with four deterministic prompt
  variants each: 20% expansion, 10% contraction, and 10% translation in each
  diagonal direction. All 96 real MobileSAM calls produced IoU `>= 0.50`.
- Overall median IoU was `0.856132`; the lowest class median was
  `crack 0.704918`; the lowest variant median was
  `shrink-10pct 0.850117`.
- The 4,525-file source tree SHA-256 stayed
  `4E511A2E08F2ED609B78B40D6B789DE691C968E71ED5A298B76A1E7CA1FB52A8`.
  Runtime and weight identity matched the exact-box matrix.
- Evidence: `docs\MOBILE_SAM_BOX_JITTER_MATRIX_20260722.md` and
  `artifacts\mobile-sam-box-jitter-matrix\20260722-165800`.
- Independent review preserved the existing exact-box artifact layout:
  `predicted-masks/<split>/<class>`. Only the jitter command adds
  `predicted-masks/<variant>/<split>/<class>`.

Boundary / next dependency: keep the current box-only plus polygon/brush
fallback as protected regression behavior. The 2026-07-27 labeling-editor
roadmap supersedes the former “new reproducible failure required” product
priority and permits positive/negative refinement as a separate P0-B after
P0-A. The existing synthetic evidence still does not prove point-refinement
quality or production accuracy; production adoption requires an approved
independent camera/session packet.

## 22. 2026-07-22 Latest Checkpoint: Documentation Baseline And Beginner EXE Audit

Status: `Complete`.

- The active authority order is `AGENTS.md`, this handoff,
  `docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md`, then
  `CODEX_NEXT_PROMPT.md`. Older dated priority lists are historical evidence.
- The current built EXE passed object-detection box labeling, segmentation
  brush/eraser labeling, and anomaly close/restart/first-inference persistence
  with 1920x1080 captures under
  `artifacts/ui/beginner-e2e-audit-20260722`.
- The audit fixed two reproduced defects: selectable canvas tool containers now
  expose their names to UI automation/accessibility, and image-level anomaly
  candidates use whole-image OK/NG language instead of outside-image and box
  overlap language.
- Detailed evidence, commands, timings, and boundaries are in
  `docs/BEGINNER_END_TO_END_UX_AUDIT_20260722.md`.

Boundary / next dependency: workflow maturity remains `4.0/5`; this is not a
model-accuracy or production-adoption claim. The generated-name follow-up is
complete in section 23; manually edited names and paths remain protected.

## 23. 2026-07-22 Latest Checkpoint: Dataset Purpose Generated Defaults

Status: `Complete`.

- Changing purpose in the creation wizard now resolves a new generated Recipe
  name only when the initial name was genuinely generated and has never been
  operator-edited.
- The default storage path follows only while it is also untouched. Manual
  names and paths remain protected even after their text is restored to the
  original generated value.
- The final current EXE showed `AnomalyDetection` in both fields, created the
  Recipe, restored its YOLOv8 profile after close/restart, returned one
  candidate, and persisted `Abnormal`.
- Evidence: `docs/DATASET_PURPOSE_AUTOMATIC_NAME_SYNC_20260722.md` and
  `artifacts/ui/dataset-purpose-auto-name-20260722/after`.

Boundary / next dependency: existing Recipes, folders, source data, labels,
and model settings are unchanged. Do not reopen without a reproduced
name/path-preservation regression. No further internal feature is justified by
this finding alone; select the next task from a newly reproduced operator defect
or changed approved contract.

## 24. 2026-07-22 Latest Checkpoint: Independent Local Commit Separation

Status: `Complete`.

Scope: separate the accumulated MobileSAM evidence, beginner labeling-review
UX, and generated dataset-default changes into independently reviewable local
feature commits, then record the current documentation state separately. Do not
push, rebase, force-update, or mix unrelated source changes.

Acceptance criteria and evidence:

- `f952915 test: add MobileSAM box-jitter evidence` contains the MobileSAM
  matrix, test entry point, focused README link, and no beginner/dataset-default
  source. A detached-worktree isolated build completed with zero warnings and
  errors; `--mobile-sam-box-prompt`, `--priority-workflow-docs`, and commit
  `diff --check` passed.
- `ac8c50f fix: clarify beginner labeling review UX` contains the candidate
  wording, canvas accessibility, and beginner workflow checks, without the
  dataset-default service. A detached-worktree isolated build completed with
  zero warnings and errors; `--wpf-canvas-panel-commands`,
  `--wpf-candidate-review-presentation`, `--wpf-anomaly-purpose-flow`,
  `--wpf-labeling-shell`, and commit `diff --check` passed.
- `f515bdf fix: sync generated dataset defaults` contains only the generated
  name/path synchronization implementation, regressions, EXE smoke update, and
  its evidence document. A detached-worktree isolated build completed with
  zero warnings and errors; `--wpf-dataset-setup-ui`,
  `--wpf-dataset-setup-request`, `--wpf-labeling-shell`, and commit
  `diff --check` passed.
- The documentation checkpoint on top is limited to project guidance and
  evidence records. Its detached-worktree isolated build,
  `--priority-workflow-docs`, and commit `diff --check` passed.

Current cumulative EXE evidence remains the current-source anomaly restart
smoke recorded in section 23: the final Debug EXE created an anomaly Recipe,
restored the YOLOv8 profile after restart, returned one candidate, and persisted
`Abnormal`. Its build completed with zero warnings and errors.

Boundary / next dependency: all four commits are local only. `origin/main`
remains at `6a4ab11`; no push occurred. A remote update requires an explicit
operator request and must be a normal non-force push. Do not reopen or recombine
these completed slices without a changed contract or reproduced regression.

## 25. 2026-07-22 Latest Checkpoint: Image Queue Action Worklist

Status: `Complete`.

- The Worklist slice follows baseline `c317278`. Verify the current local and remote hashes directly; this supersedes stale remote statements in historical sections.
- The existing unfinished-image filter is now a visible `확인 필요 Worklist` card in the right Image Queue. It gathers unreviewed, save-required, AI-candidate, failed, and needs-fix images without creating a second queue or persistence format.
- Saved labels, confirmed/skipped/no-candidate rows, and completed anomaly OK/NG decisions remain complete and do not appear in the Worklist.
- One completed row leaves the filtered view through live filtering. The 10,000-row gate retained all row instances, emitted no view reset, evaluated the filter once, and changed the visible set from 5 to 4.
- Final local synthetic timings were `4.3ms` for the one-pass 10K summary and `113.3ms` for one completion plus status update. These are bounded regression gates, not production or network-share throughput claims.
- The first actual-EXE use exposed stale visible-count text and nondeterministic queue focus after the completed selected row left the live-filtered view. Counts now come from the queue summary, and Worklist-only label save explicitly advances from the completed path.
- Two consecutive runs of the current EXE on an isolated mixed-state 125-image Recipe individually proved candidate, failed, needs-fix, and requested inclusion plus completed-label exclusion, then produced `completed=5->6`, `worklist=120->119`, zero queue invalidations/bulk changes, and active/selected `queue-local-001.jpg`. Verified EXE SHA-256: `B62AFCDF5B7820632CACF22C185DFC23E47E9F6844F7DAC30A79B5CBE531A70D`.
- Current-source captures at 1920x1080 and 1366x768 are under `artifacts/ui/image-queue-worklist-20260722`; current-EXE category and transition evidence is under `artifacts/exe-image-queue-worklist/current-exe-20260722-categories-repeat`. Detailed scope and commands are in `docs/IMAGE_QUEUE_ACTION_WORKLIST_20260722.md`.

Boundary / next dependency: do not add accounts, assignments, comments, server sync, a new DB, paging, or a second review screen from this slice. The next implementation requires a current Recipe to reproduce a missed category, incorrect transition, or unacceptable latency. Otherwise this Worklist is closed and protected.

## 26. 2026-07-23 Latest Checkpoint: Original-Path Relocation Closure

Status: `Complete`, committed, and pushed.

- The nine relocation fixes/tests/documents were transplanted from the temporary
  copy to the clean original repository with normalized-content equality for all
  nine mapped files and no behavioral deviation.
- The original-path isolated test build and separate EXE build completed with
  zero warnings and errors. Fourteen focused gates passed.
- The original-path EXE independently completed the COCO128 Dataset wizard
  labeling loop, the 125-image Worklist transition (`5->6`, `120->119`, zero
  invalidation/bulk change), and YOLOv8 save/close/restart/first inference with
  one candidate. External checkpoint, input, and worker hashes stayed unchanged.
- Fresh evidence is under `artifacts/copy-verification-relocation-fixed`,
  `artifacts/copy-verification-worklist/post-relocation-fix-20260723`, and
  `artifacts/copy-verification-model-runtime/yolov8-detect-restart-20260723`.
- The approved temporary clone was deleted only after original-path evidence and
  zero running clone processes were confirmed. Commit `0f1f91b` was then pushed
  normally and local `HEAD` matched `origin/main`.

Boundary / next dependency: this improves relocation and regression confidence,
not model accuracy or product maturity; the focused-workstation estimate remains
`4.0/5`. Do not recreate or use the deleted clone. No new product implementation
is justified without a reproduced operator defect or changed approved adapter/data
contract. Independent camera/session data remains an optional field-adoption gate.

## 27. 2026-07-23 Latest Checkpoint: Recipe Dataset Version v2

Status: `Complete`; committed and pushed in `ad569dc`.

- Exact recipe-owned image/annotation content, ordered classes, and split
  ownership now produce a deterministic `dsv2-<64 hex>` identity.
- Repeated unchanged saves reuse one immutable metadata history entry. Label
  geometry, class, or split changes create a new identity without copying or
  mutating the source dataset.
- Internal training and model-registry records retain the exact Dataset Version
  and content SHA-256. External native YOLO intake maps its existing read-only
  source fingerprint to an external Dataset Version.
- Anomaly classification includes generated class-folder split ownership.
  Training-progress-only metadata saves reuse the version captured at start and
  do not rescan the dataset on every status update.
- The Model Center Project panel shows the version, shortened content SHA-256,
  image/label counts, and immutable-history count.
- The isolated build, seven focused switches, current-source UI capture, and
  actual-EXE smoke passed. The first EXE attempt exposed only an invalid test
  fixture: four arbitrary bytes had been named `.png`; after creating a real
  PNG, Recipe application and version presentation passed.
- Evidence: `docs/RECIPE_DATASET_VERSION_V2_20260723.md` and
  `artifacts/ui/recipe-dataset-version-v2`.

Boundary / next dependency: this is local reproducibility metadata, not a full
dataset copy, cloud VCS, collaboration feature, automatic adoption, or
field-quality proof. Maturity remains `4.0/5`.

## 28. 2026-07-23 Latest Checkpoint: Recipe And Adapter Contract Truth Alignment

Status: `Complete`; committed and pushed in `ad569dc`.

- Anomaly Recipe manifests now record `image-level-normal-abnormal` and
  navigation-only `panZoom` instead of the obsolete `box-and-mask` drawing
  profile. Existing image-level review counts remain the label evidence.
- Dataset setup and learning guidance consistently say that the whole image is
  judged `정상(OK)` or `이상(NG)`. The anomaly learning-tool state contains only
  Pan/Zoom and no undo/redo/delete annotation commands. Returning to object
  detection restores Select.
- The read-only Model Adapter Catalog contains six contracts. U-Net is
  presented as the verified segmentation-only PyTorch adapter using the
  app-owned canonical raster-mask export. YOLO11 names its verified local
  detection, segmentation, and anomaly-classification scope, keeps the current
  anomaly result as `hold`, and leaves arbitrary external runtimes unverified.
- The isolated build passed with zero warnings and errors. Focused gates
  `--wpf-anomaly-purpose-flow`, `--wpf-learning-workflow-panel`,
  `--wpf-dataset-setup-ui`, `--model-adapter-catalog`, and
  `--wpf-labeling-shell` passed.
- Current-source before/after 1920x1080 evidence is under
  `artifacts/ui/20260723-contract-truth-alignment`. The after Model Center shows
  the U-Net card in the catalog; the after anomaly guide presents the dedicated
  image-level OK/NG task instead of defect-region instructions.

Boundary / next dependency: no model worker, weight, labels, source images,
inference results, adoption state, or canvas hot path changed. The focused
workstation estimate remains `4.0/5`. The next field-quality priorities are
blocked on newly approved independent camera/session data. Recipe Dataset
Version v2 is complete in section 27; the approved YOLO11 anomaly-classification
runtime closure is complete in section 29. Independent camera/session evidence
remains the anomaly model-adoption prerequisite.

## 29. 2026-07-23 Latest Checkpoint: YOLO11 Anomaly Runtime Closure

Status: `Complete`; committed and pushed in `ad569dc`.

- With explicit approval, official `yolo11n-cls.pt` SHA-256
  `C62D41BF9625777760018BF914D2E6CD472420CCD01706D97A61CB6C82502BD7`
  was loaded in the existing Ultralytics `8.4.101` environment.
- One-epoch connectivity and 20-epoch app-service/TCP training passed on the
  supplied circular 500 OK / 500 NG data. The 1,000-file source tree SHA-256
  remained unchanged.
- On the identical 104-image test fingerprint at confidence `0.8`, YOLO11
  scored `82/104` (`78.8%`) versus YOLOv8 `90/104` (`86.5%`). Both remain
  `hold`; training completion did not adopt a model.
- The bundled batch evaluator compatibility gap was fixed, Model Center
  evaluation accepts verified YOLOv8/YOLO11 profiles, and the actual EXE
  reopened the YOLO11 profile and persisted `Abnormal` after held-out NG
  inference.
- Evidence:
  `docs/YOLO11_ANOMALY_CLASSIFICATION_PREREQUISITE_AUDIT_20260723.md`.

Boundary / next dependency: runtime/workflow support is complete only for the
recorded local profile. Independent normal/abnormal camera-session data is
required before any production-quality or adoption claim.

## 30. 2026-07-27 Latest Checkpoint: Interactive Smart Mask P0-B

Status: `Complete`. Field validation: `Not evaluated`.

- Segmentation now retains Rectangle as a selectable tool because the explicit
  Smart Mask session starts from an operator-drawn box.
- The session supports positive/negative points, point undo/clear, 48/96/256
  polygon detail, cancellation, pending-candidate replacement, confirm/skip,
  and next-instance box restoration.
- Stale image/Recipe/prompt generations fail closed. Pending results do not
  save; confirmation uses the existing canonical segment JSON/mask PNG path.
- Real-worker evidence and source immutability:
  `artifacts\mobile-sam-point-correction\20260727-185324`.
- Current Debug EXE evidence before/after confirmation:
  `artifacts\ui\smart-mask-p0b-20260727`.
- Preserve the completed box-only 24-call exact and 96-call jitter matrices as
  regressions. They are not field-accuracy evidence.

Boundary / next dependency: P0-B completes one local single-operator
correction session, not V7/CVAT parity. P1 must define canonical
JSON/mask-PNG/YOLO/COCO/CVAT structure semantics before merge/split/hole,
multi-component, z-order, or remove-underlying implementation.

## 31. 2026-07-28 Latest Checkpoint: Smart Mask Auto-Boundary Hero

Status: `Complete`. Field validation: `Not evaluated`.

- The requested CVAT/V7-like base behavior already existed: a drawn Rectangle
  is sent to MobileSAM and returns a polygon candidate. The missing visible
  parity was pending-mask presentation.
- The selected pending Smart Mask candidate is now rasterized into a
  translucent blue fill while the existing contour remains visible.
- Candidate Review now says `자동 경계 N점`; the accepted real candidate showed
  96 points before confirmation.
- Actual current Debug EXE run `20260728-smartmask-final4` used
  KolektorSDD `kos14/Part7.jpg` and completed Box -> automatic filled mask ->
  review -> Confirm -> canonical save -> Next Incomplete.
- Saved output: one polygon, 96 points, 7,931 mask pixels; broad source-label
  precision `0.9861`, IoU `0.3927`, recall `0.3948`.
- The source label is a broad rectangular defect cover while the candidate
  tightly follows the visible Y-shaped crack. Do not convert this mismatch
  into a production-accuracy claim.
- Final local review candidate:
  `artifacts\operator-video\20260728-smartmask-final4\publish\smart-mask-auto-boundary-hero.gif`;
  1024x576, 10fps, 18.7 seconds, 1,021,823 bytes.
- User approval was received on 2026-07-28. The GIF and poster are now copied
  to `docs/tutorial/images/github/` and the README embeds the actual-EXE demo.

Rejected/historical boundary:

- `20260728-175240` manual-polygon GIF is no longer the representative
  promotional candidate;
- optional point-correction attempts on this sample reduced precision, so the
  accepted auto-first flow confirms the stronger initial candidate;
- this closes automatic candidate presentation for one sample, not general
  field accuracy or CVAT/V7 parity.

Next priority: make Smart Mask correction contextual. Default to Box ->
automatic candidate, collapse positive/negative/detail controls, and reveal
them only when the operator chooses to correct a poor result. Preserve
explicit confirmation and no pending-candidate autosave.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`

## 34. 2026-07-28 Latest Checkpoint: Smart Mask Candidate Compare/Restore

Status: `Complete`. Field validation: `Not evaluated`.

- A Smart Mask session retains only the initial and latest candidate
  references after a rerun.
- Candidate Review still owns exactly one visible pending candidate.
- `이전 후보 보기` and `현재 후보 보기` switch that pending candidate without
  changing dirty state or writing canonical files.
- The compact row states that confirmation saves only the displayed version.
- Confirm/skip resolves comparison history. Image, Recipe, and session changes
  reset it through the existing Smart Mask context boundary.
- Focused integration proved no segment JSON before confirmation and proved
  the restored initial candidate's first point is the point written after
  confirmation.
- Evidence:
  `docs\SMART_MASK_CANDIDATE_COMPARE_RESTORE_20260728.md` and
  `artifacts\smart-mask-candidate-compare-restore\20260728-210121`.
- Current-source 1920x1080 selected/restored states:
  `artifacts\ui\smart-mask-candidate-compare-restore-20260728`.

Boundary / next dependency:

- the focused integration uses deterministic synthetic candidates, while the
  actual Debug EXE replay under
  `artifacts\operator-video\20260728-smartmask-restore-save-retry1` uses real
  MobileSAM on Kolektor `kos14/Part7`;
- that actual replay completed corrected rerun -> previous candidate restore
  -> explicit Confirm -> canonical save -> saved-image reopen, with exactly
  one saved 96-point polygon and a 7,931-pixel mask;
- this proves workflow and persistence safety, not MobileSAM field accuracy;
- the historical documentation synchronization, four-point box contract, and
  bounded extreme-box implementation are Complete;
- the approved public GitHub GIF remains unchanged.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`

## 33. 2026-07-28 Latest Checkpoint: Real Smart Mask Correction Effectiveness

Status: `Complete`. Field validation: `Not evaluated`.

- A fixed evaluator now replays two development and four held-out KolektorSDD
  defect samples.
- Every baseline is poor (`IoU < 0.50`) and is preserved before correction.
- Positive-only correction reduces false negatives in `6/6`.
- Negative-only correction reduces false positives in `4/4` applicable cases.
- Combined correction improves held-out `3/4`; held-out median IoU delta is
  `+0.0988`.
- Two combined runs worsen and remain in the evidence. In particular,
  `kos14_Part7` improves from `0.3749` to `0.4620` with positive-only but falls
  to `0.3260` with positive+negative.
- Product guidance now says to add one point, rerun/compare, and add another
  only if needed.
- The 798-file dataset tree SHA-256 remains
  `F09D09AA1A1EC9AB7866087361CF1B48C6E6D32F5C0CC239CE619D39FB9A0474`.
- Detailed record:
  `docs\SMART_MASK_CORRECTION_EFFECTIVENESS_20260728.md`.

Boundary / next dependency:

- click selection uses ground truth as an evaluation oracle;
- this proves real-sample correction response, not unaided operator click
  quality, independent camera evidence, production accuracy, or parity;
- next priority is session-only previous/current pending-candidate
  compare/restore so a worse rerun can be rejected safely before confirmation.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`

## 32. 2026-07-28 Latest Checkpoint: Auto-First Contextual Smart Mask Correction

Status: `Complete`. Field validation: `Not evaluated`.

- The Smart Mask session now defaults to the automatic filled candidate and a
  single `보정 옵션` action.
- Positive/negative points, undo/clear, generation cancel, and 48/96/256-point
  boundary detail appear only after explicit expansion.
- Expansion persists across reruns of the same object. Session end, image
  change, and next-object reset return it to collapsed.
- Explicit confirm/skip, pending no-autosave, MobileSAM execution, and canonical
  save behavior are unchanged.
- Current-source compact/expanded evidence:
  `artifacts\ui\smart-mask-contextual-correction-20260728`.
- Actual Debug EXE run
  `artifacts\operator-video\20260728-smartmask-contextual-correction`
  verified default hidden -> expand -> collapse -> confirm -> canonical save on
  KolektorSDD `kos14/Part7.jpg`.
- Saved evidence remains one polygon, 96 points, and 7,931 mask pixels.
- Detailed contract:
  `docs\SMART_MASK_CONTEXTUAL_CORRECTION_UX_20260728.md`.

Boundary / next dependency:

- this closes contextual disclosure, not correction effectiveness or
  commercial parity;
- labeling-editor depth remains `3.4/5`; focused workstation maturity remains
  `4.0/5`;
- next priority is to preserve a genuinely poor automatic candidate, prove
  positive/negative correction improves it in the requested direction, and
  replay that result on held-out samples.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`
