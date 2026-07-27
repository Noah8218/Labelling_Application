# Next Thread Handoff

Last updated: 2026-07-27 KST

This is the current operational handoff for C:\Git\Labelling_Application. It is intentionally shorter than the historical journal. Use it to choose the next task; use the linked records only for the detailed evidence behind a claim.

## 1. Mandatory Start Sequence

1. Run git status --short before any other project command.
2. Read AGENTS.md.
3. Read this file.
4. Read CODEX_NEXT_PROMPT.md, docs/WORK_TRACKING.md, docs/STABLE_VERIFIED_AREAS.md, and docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md.
5. Inspect the current diff directly. The dirty worktree is more authoritative than this handoff.
6. Before editing, state the immediate priority, remaining product priority, assumptions, and verification plan.

There is no separate C:\AGENTS.md or C:\Git\AGENTS.md in this workstation snapshot. AGENTS.md in this repository is the available project instruction source.

## 2. Repository Checkpoint

- Workspace: C:\Git\Labelling_Application
- Branch: main. The structural-refactor implementation through
  `889abdf refactor: clarify yolo contract ownership` was explicitly pushed to
  `origin/main` on 2026-07-27. Always verify the live hashes before new work.
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
- Tracked project files were clean immediately after the structural-refactor
  push. Local `.proofline/STATE.md` and `.proofline/dashboard/` remain untracked
  and outside project commits. Verify live state with `git status --short`.
- GitHub Actions has not been rechecked for the current structural-refactor and
  closure documentation commits. Do not cite older CI evidence as current CI
  evidence.
- The current focused passes directly verified Dataset Health, external native YOLO intake, model/anomaly comparison, the dedicated Model Center workspace, and the explicit model-adapter catalog slices. The image-queue slice also has a 50,081-image local warm-cache profile and a separate duplicate-file local 8K profile; neither is a network-share or production-camera result.
- The 2026-07-27 review of ten user-provided commercial integration videos is
  recorded in
  `docs\LABELING_STUDIO_COMMERCIAL_VIDEO_REVIEW_20260727.md`. A denser V7/CVAT
  labeling review corrected the initial visual-QA-first decision. P0-A
  command/productivity, P0-B interactive Smart Mask, and the first P1-C
  merge/join and axis-aligned split/slice commands are now complete in the
  current dirty worktree. The labeling-only estimate moved from the review
  baseline `2.1/5` to `2.7/5`;
  object state, remaining structural mask editing, display
  aids, video propagation, and collaboration still prevent CVAT/V7 parity.
  This remains separate from the focused local-workstation estimate of `4.0/5`.
  `docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md` is the current
   implementation contract.
- Active development started from `ef155ed docs: close structural refactoring
  phase` on 2026-07-27. The current dirty worktree contains the commercial-video
  review and labeling-gap documentation listed above; these are intentional
  project changes. Local `.proofline/STATE.md` and `.proofline/dashboard/` are
  unrelated untracked state and must remain untouched.
- Never push unless the user explicitly says push. A commit request means local commit only.

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

- P1-C merge/join and axis-aligned split/slice are complete and raise the
  labeling-editor depth estimate only to `2.7/5`.
- P1-C continues with structural hole editing or z-order next.
  Remove-underlying remains a separate command with a required affected-object
  preview/warning.
- Recommended model: `gpt-5.6-sol`; reasoning effort: `high`.

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
- Next bounded operation: manual hole editing or z-order. Remove-underlying
  requires an affected-object warning before mutation.

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
   P1-C merge/join plus axis-aligned split/slice are complete. P1-C continues
   with holes, z-order, and remove-underlying warnings.
4. P2 object state and precision geometry: hide/lock/pin, contract-backed
   occlusion/tags/groups, 4-point boxes, polygon vertex insert/delete, and a
   separately proven edge-aware slice.
5. P3 display-only aids: brightness, contrast, gamma, invert,
   histogram/equalization, and overlay alignment without changing source or
   training pixels.
6. P4 Dataset Health visual label QA: read-only dataset-level issue discovery
   with navigation back to the canonical editor.
7. P5 interchange and batch preflight: dry-run/Apply validation for existing
   formats and explicit batch AI scope, model, class mapping, confidence, and
   existing-label policy.

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

The immediate product priority is now P1-C manual hole editing or z-order. P1-A
JSON/mask-PNG/YOLO/COCO/CVAT preservation/loss semantics and P1-B canonical
v3 object/component identity are complete, and P1-C merge/join plus
axis-aligned split/slice have user-visible commands and focused evidence.
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

The 2026-07-23 Recipe/anomaly/model-adapter truth-alignment, Recipe Dataset
Version v2, and recorded local YOLO11 anomaly runtime priorities are complete
and committed in `ad569dc`. Keep them protected unless a focused regression
fails.

P0-A command/productivity and P0-B interactive Smart Mask are complete in the
current worktree. Preserve P0-A's
purpose-filtered shortcuts, numbered class order, one-step duplicate history,
canonical save path, and separate-row help card unless a focused regression is
reproduced. Preserve P0-B's box-only compatibility, point session,
rerun-replace, cancellation, stale guard, confirm-only save, provenance, and
next-instance behavior.

1. Add P1-C manual hole editing or z-order, then remove-underlying with an
   affected-object warning and undo. Merge/join and axis-aligned split/slice
   are complete. P1-A already states what
   polygon/segment JSON, mask PNG, YOLO, COCO, and CVAT preserve or reject
   before editing code. Do not treat an internal brush-span merge as a
   user-visible object merge.
   Recommended model: `gpt-5.6-sol`
   Reasoning effort: `high`

2. Add object-state and precision-edit slices only after the mask contract.
   Start hide/lock/pin as presentation/session state; persist occluded/tags only
   when Recipe and export consumers are defined. Separate 4-point box,
   polygon-vertex insert/delete, and edge-aware scissors into focused
   acceptance-gated work.
   Recommended model: `gpt-5.6-sol`
   Reasoning effort: `high`

3. Add display-only brightness, contrast, gamma, invert, and
   histogram/equalization without changing source pixels, dataset hashes, or
   training inputs. Overlay alignment and current image-switch behavior must
   remain deterministic.
   Recommended model: `gpt-5.6-terra`
   Reasoning effort: `medium`

4. After the core labeling slices, add the previously designed read-only
   Dataset Health visual-label-QA gallery, followed by dry-run-first
   interchange and batch-AI preflight slices. The gallery still must not edit
   labels, auto-approve candidates, create a new queue, or preload all 10K
   images at full resolution.
   Recommended model: `gpt-5.6-terra`
   Reasoning effort: `medium`

5. Acquire a new, approved NG-rich object-detection camera/session source, define its object classes and box rules, create a content-separated held-out test split, and rerun the unchanged controlled engine comparison. This remains the next object-detection quality priority because current synthetic comparisons prove runtime/format behavior but not field generalization. It includes provenance, label audit, SHA-256 non-overlap, fixed thresholds, and error review; it excludes treating folder-level OK/NG names as boxes or tuning on the held-out test. The operator-excluded `D:\기타이미지\2022.11.16_SIT 이미지` path must not be inspected or used.
   Prerequisite: a newly approved source with trustworthy bounding boxes and enough NG examples.
   Recommended model: none until the data is available
   Reasoning effort: n/a

6. Acquire balanced independent production-camera/cross-session normal and abnormal anomaly data, keep it outside training initially, and rerun the unchanged anomaly evaluation guard. This remains the next anomaly-quality priority and distinguishes a repeatable classifier runtime from generalizable anomaly quality; it includes provenance, content-overlap checks, confidence-gated errors, and an adopt/hold decision, and excludes tuning against the preserved circular or MultiIndustry synthetic evaluation sets.
   On the same 104-image circular synthetic test at confidence `0.8`, YOLOv8
   remains `hold` at `90/104` and YOLO11 remains `hold` at `82/104`. Do not tune
   either model against that test or substitute it for new acquisition evidence.
   Prerequisite: new normal and abnormal images with provenance and representative operating conditions.
   Recommended model: none until the data is available
   Reasoning effort: n/a

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
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --priority-workflow-docs

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-10k-responsive
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-10k-detail-responsive
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-status
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-image-queue-root-switch
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-queue-click-perf --count 125 --measure-detail-refresh --settle-ms 60

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --dataset-health
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-dataset-health-window
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --external-yolo-dataset-intake
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --dataset-readiness-purpose
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --dataset-quality-audit
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --recipe-dataset-version-v2
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-labeling-shell
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --model-adapter-catalog
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --mobile-sam-box-prompt
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --real-mobile-sam-point-correction
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-smart-mask-point-smoke --exe .\artifacts\run\Debug\OpenVisionLab.LabelingStudio.exe
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-yolo-model-settings-panel

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --anomaly-classification-training-workflow
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --anomaly-classification-evaluation
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-yolov8-anomaly-classification-runtime-smoke

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
