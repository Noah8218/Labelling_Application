# Next Codex Prompt

Copy the text below into the next Codex chat when continuing C:\Git\Labelling_Application.

~~~text
Continue work in C:\Git\Labelling_Application.

Read and follow AGENTS.md first.

Required start order:
1. Run git status --short first.
2. Read AGENTS.md.
3. Read docs/NEXT_THREAD_HANDOFF.md. It is the current project handoff and source-of-truth summary.
4. Read docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md for current product scope, maturity, and commercial comparison.
5. Read this CODEX_NEXT_PROMPT.md, docs/WORK_TRACKING.md, and docs/STABLE_VERIFIED_AREAS.md for the bounded next action and durable evidence.
6. Inspect the live branch, status, and diff before selecting work. Git state is authoritative over documentation.
7. Before editing or running follow-up commands, state:
   - immediate priority;
   - remaining product priority;
   - product identity;
   - current maturity estimate and source;
   - relevant commercial-product lesson;
   - out-of-scope platform breadth.

Current checkpoint:
- Workspace: C:\Git\Labelling_Application
- Branch: main. The labeling-editor structure workflow through P1-C
  merge/join and axis-aligned split/slice is committed and pushed as
  `9b2160a feat: advance labeling editor structure workflows`. Enclosed hole
  add/fill is committed and pushed as
  `d669b64 feat: add segmentation hole editing`. Saved-object z-order and
  remove-underlying follow it in current uncommitted source. Verify live hashes before work and do not push again
  without a new explicit request.
- The temporary `C:\새 폴더\OpenVisionLab-Labeling-Studio_TEST` clone was deleted
  after the original path independently passed Dataset wizard, Worklist, and
  YOLOv8 restart/inference EXE smokes. Develop only in
  `C:\Git\Labelling_Application`.
- The previously mixed worktree was independently reviewed and split into `687e553 feat: add YOLO11 segmentation comparison evidence`, `0b05986 feat: define synthetic evidence completion contract`, and `549a7d4 feat: add MobileSAM smart-mask labeling`. Each commit passed an isolated build and its focused checks from a detached temporary worktree. Do not recombine or repeat these completed slices without a changed contract or reproduced regression.
- Read `docs/WORK_TRACKING.md` section `2026-07-21 external native YOLO segmentation canonical-mask intake` and `docs/STABLE_VERIFIED_AREAS.md` section `external native YOLO runtime-copy and paired-comparison contract` before selecting work. These newer records supersede stale priorities below.

Product direction:
- Build a local Windows industrial labeling, training, inference, review, and model-evidence workstation.
- Treat each recipe as canonical image/class/annotation/split/evidence data, then use verified adapters to format it for selected model input contracts and compare their normalized evidence.
- Teach the operator what labeling data means and how the same data changes model quality, while keeping model testing reproducible.
- Do not treat every GitHub model as automatically supported; require an explicit adapter contract for class, coordinate, split, training/inference, result, and evidence mapping.
- Keep a single-operator task-oriented UI. Use separate read-only analysis windows for dense data/model information.
- Do not expand into cloud collaboration, accounts, reviewer assignment, deployment, or enterprise governance.
- The focused single-operator maturity estimate remains 4.0/5. It is workflow maturity, not model accuracy.
- Labeling-editor depth moved from the commercial-video baseline `2.1/5` to
  `2.5/5` after P0-A/P0-B, `2.6/5` after P1-C merge/join, `2.7/5`
  after axis-aligned split/slice, `2.8/5` after enclosed hole add/fill, and
  `2.9/5` after saved-object z-order, `3.0/5` after remove-underlying, and
  The first P2 session-only hide/lock/pin pass briefly raised it to `3.1/5`,
  and the commercial-video-grounded contextual-UI and movement-pin correction
  passed all visual/regression gates. Polygon vertex insert/delete then passed
  focused/protected/canonical and 1920/1366 gates. Bounded edge-aware
  intelligent scissors then passed deterministic accuracy/latency,
  explicit-preview, protected/canonical, and 1920/1366 gates. P3 compact
  display-only brightness/contrast/gamma/invert/equalization then passed
  source/file/history/overlay invariants and current-build 1920/1366 gates, so
  the current estimate is `3.4/5`.
  It remains materially below CVAT/V7 because persistent object metadata,
  propagation, and collaboration are still absent or intentionally excluded.
- Synthetic-first evidence may complete product features when the declared-origin, locked-split, SHA-256 non-overlap/source-immutability, runtime-provenance, normalized-result, and non-adopting decision gates in `docs/SYNTHETIC_EVIDENCE_CONTRACT.md` pass. Independent production-camera/cross-session data is an optional field-adoption gate, not an implementation blocker.
- The supplied circular-disk 500 OK / 500 NG package is complete synthetic workflow evidence: exact metadata-backed 5-class detection data, YOLOv5/YOLOv8 one-epoch connectivity, a controlled 20-epoch 150-image test benchmark, and a new 20-epoch anomaly candidate. The anomaly candidate remains `hold`; the detection benchmark favors YOLOv8n (`mAP50/mAP50-95 0.955/0.678`, 27.575ms) over YOLOv5s (`0.900/0.567`, 52.45ms) but is explicitly `engine-benchmark`, not adoption. The fixed comparison cleanup preserves the exact source-tree SHA-256. The package is derived from one earlier OK source image; do not present it as independent camera evidence. Read `docs/CIRCULAR_DISK_SYNTHETIC_1000_EVIDENCE_20260720.md`.

Current immediate priority:
- The 2026-07-27 review of ten user-provided commercial integration videos is
  new workflow evidence. A denser V7/CVAT pass corrects the initial
  visual-QA-first conclusion: the current editor has a stable basic
  box/polygon/brush/eraser and save/review baseline, but repeat commands,
  object state, structural mask editing, and interactive AI correction were
  materially behind. P0-A, P0-B, and P1-C merge/join, split/slice, hole, and
  saved-object z-order and remove-underlying are complete. P2 session-only
  hide/full-lock/movement-pin, contextual exposure, polygon vertex editing, and
  bounded intelligent scissors are complete. P3 display-only image aids are
  also complete; the labeling-only estimate is `3.4/5`, while the focused
  local-workstation product estimate remains
  `4.0/5`.
- P0-A, the labeling command and repeat-productivity foundation, is complete in
  committed source. It adds purpose-filtered tool hotkeys, class
  `1~9`, `0` Class Catalog fallback, last shape+class repeat, selected
  box/polygon/raster-mask duplicate, tool/class retention, text-entry
  suppression, numbered class chips, and an F1 help card.
- P0-A evidence includes `--labeling-productivity`,
  `--wpf-undo-redo-shortcuts`, ROI/segmentation/storage/performance focused
  gates, current-source before/after, and
  `--exe-labeling-productivity-smoke` against the latest Debug EXE. The help
  card stays in a separate Auto row above the WinForms/OpenGL canvas so actual
  EXE airspace cannot hide it.
- P0-B interactive Smart Mask is also complete in committed source.
  It adds box+positive/negative-point correction, point undo/clear,
  48/96/256 detail, async cancellation, rerun-replace, confirm/skip, and
  next-instance box restoration. It preserves no-autosave, stale-result guards,
  runtime/weight provenance, confirmed candidates, and canonical save.
- P0-B evidence includes `--mobile-sam-box-prompt`,
  `--real-mobile-sam-point-correction`, the current Debug EXE
  `--exe-smart-mask-point-smoke`, and
  `artifacts\ui\smart-mask-p0b-20260727`.
- Read
  `docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md` and
  `docs\LABELING_STUDIO_COMMERCIAL_VIDEO_REVIEW_20260727.md` before editing.
- P1-A mask-structure preservation/loss contract is complete in committed
  source. `SegmentationInterchangeContractService` declares one matrix
  for canonical JSON, mask PNG, YOLO, COCO, and CVAT; COCO/CVAT/YOLO export
  results surface deduplicated conditional/loss warnings. The focused gate is
  `--segmentation-interchange-contract`; the durable contract is
  `docs\SEGMENTATION_INTERCHANGE_PRESERVATION_CONTRACT_20260727.md`.
- P1-B canonical schema v3 is complete in committed source.
  Disconnected raster components preserve one object ID, stable component
  indices, z-order, and last structural operation across save/load/re-save.
  Version 1/2 polygon/raster files load with deterministic legacy metadata;
  history preserves identity and duplicate starts a new identity. The focused
  gate remains `--segmentation-interchange-contract`.
- P1-C merge/join is complete in committed source. Saved Labels
  exposes per-segment merge checkboxes and a count/button; same-class
  polygon/raster sources become one raster object with a new v3 ID, `Merge`
  provenance, one-step undo/redo, and save/load/re-save evidence. The focused
  gate is `--segmentation-merge`; read
  `docs\SEGMENTATION_MERGE_P1C_20260727.md`.
- P1-C axis-aligned split/slice is complete in committed source.
  Saved Labels exposes vertical/horizontal point-input commands and cancel;
  polygon/raster sources split only when a one-pixel cut creates 2+
  4-connected components. New v3 IDs, `Split` provenance, invalid-cut
  no-mutation, one-step undo/redo, and save/load/re-save are covered by
  `--segmentation-split`; read
  `docs\SEGMENTATION_SPLIT_P1C_20260727.md`.
- P1-C enclosed hole add/fill is complete in committed source.
  Saved Labels exposes polygon hole drawing and enclosed-background click fill;
  exterior-connected background is rejected without mutation. Stable identity,
  `HoleAdd`/`HoleRemove`, one-step history, and v3 re-save are covered by
  `--segmentation-hole`; read
  `docs\SEGMENTATION_HOLE_P1C_20260728.md`.
- P1-C saved-object z-order is complete in current source. Saved Labels exposes
  four explicit stack moves, normalizes order to `0..N-1`, rejects boundaries
  without mutation, preserves identity/class/geometry, and restores global
  order across class-grouped v3 reload. The focused gate is
  `--segmentation-zorder`; read
  `docs\SEGMENTATION_ZORDER_P1C_20260728.md`. Exact polygon/raster render
  interleaving is not claimed because the viewer uses separate overlay passes.
- P1-C remove-underlying is complete in current source. `겹침 분석` is
  read-only; `확인 후 제거` revalidates a full geometry/order signature and
  applies one undoable change. Partial survivors keep ID/class/z-order as
  raster remainders with `RemoveUnderlying` provenance; full coverage removes
  the object. The focused gate is `--segmentation-remove-underlying`; read
  `docs\SEGMENTATION_REMOVE_UNDERLYING_P1C_20260728.md`.
- P2 session-only hide/full-lock/movement-pin and contextual exposure are complete
  for saved manual ROI and segmentation rows. Hidden rows remain selectable,
  lock guards direct and canvas mutation, and movement pin blocks whole-object
  translation while preserving resize, polygon vertex edit, copy, delete,
  class, and structural commands. Do not add gold focus or a `PINNED` overlay
  label. The three states reset with the current image. Do not persist them into canonical
  labels or exports without a separate consumer contract. Read
  `docs\OBJECT_SESSION_STATE_P2_20260728.md` and
  `docs\OBJECT_REVIEW_CONTEXTUAL_UI_CORRECTION_20260728.md`.
- The contextual Object Review correction and polygon vertex insert/delete
  passed protected regressions and current-build 1920/1366 evidence. Vertex
  insert/delete uses zoom-aware deterministic hits, invalid-result rejection,
  one-step Undo/Redo, canonical v3 replay, and locked/hidden protection while
  movement pin remains editable. Read
  `docs\POLYGON_VERTEX_EDIT_P2_20260728.md`.
- P2 bounded edge-aware intelligent scissors is complete. It refines one
  operator-selected polygon edge inside a fixed corridor, shows a gold
  `EDGE PREVIEW`, mutates only on explicit `미리보기 적용`, preserves one-step
  history and canonical identity/provenance, rejects stale/invalid paths, and
  honors hidden/full-lock while allowing movement pin. The focused gate is
  `--intelligent-scissors`; read
  `docs\INTELLIGENT_SCISSORS_P2_20260728.md`.
- P3 display-only brightness, contrast, gamma, invert, and histogram
  equalization is complete behind one compact canvas-header `보기 보정` popup.
  Canonical source/file hashes, annotation dirty/history, and overlay image
  coordinates remain unchanged. Read
  `docs\DISPLAY_ONLY_IMAGE_AIDS_P3_20260728.md`; focused gate:
  `--image-display-adjustment`.
- The next bounded implementation is P4 Dataset Health visual label QA:
  read-only dataset-level issue discovery with explicit navigation back to the
  existing editor. It must not edit labels inside the gallery, auto-approve
  candidates, duplicate the image queue, or preload all 10K images at full
  resolution.
- Recommended model: `gpt-5.6-terra`; reasoning effort: `medium`.
- P1-C, P2, and P3 are complete. Dataset Health visual QA is now P4.
  Collaboration, video,
  account/cloud/platform scope and automatic candidate save remain excluded.
- The approved YOLO11 anomaly-classification runtime slice is complete. The
  official classification seed, one-epoch connectivity, 20-epoch app/TCP
  training, fixed 104-image evaluation, Model Center evaluation route, and
  actual-EXE restart inference passed. At confidence `0.8`, YOLO11 scored
  `82/104` versus YOLOv8 `90/104`; both remain `hold`. Do not repeat or tune on
  this test split. Read
  `docs/YOLO11_ANOMALY_CLASSIFICATION_PREREQUISITE_AUDIT_20260723.md`.
- The next anomaly-quality priority requires newly acquired balanced
  production-camera/cross-session normal and abnormal images with provenance
  and content-overlap checks. Until that prerequisite exists, do not spend
  model tokens repeating same-source synthetic training.
- Recipe Dataset Version v2 is complete and committed/pushed in `ad569dc`. It hashes exact
  recipe-owned image/annotation content, ordered classes, and split ownership;
  records immutable metadata-only history; links training/model history to that
  identity; and passed current-source plus actual-EXE evidence. Do not reopen it
  without an identity, provenance, source-mutation, or presentation regression.
  Read `docs/RECIPE_DATASET_VERSION_V2_20260723.md`.
- The original-path relocation closure is complete and pushed in `0f1f91b`.
  Repository-root discovery recognizes the current solution/project names,
  startup Dataset restore waits for asynchronous queue completion, and native
  EXE automation restores minimized windows and uses a stable save automation
  ID. Do not reopen the deleted-clone exercise unless relocation behavior
  regresses. Evidence is recorded in `docs/WORK_TRACKING.md` and
  `docs/STABLE_VERIFIED_AREAS.md` under the 2026-07-23 entries.
- The user-approved 10,000-image `확인 필요 Worklist` slice and current-EXE mixed-state follow-up are complete in the current worktree. It exposes the existing unfinished filter, keeps completed OK/NG and saved/rejected rows out, uses summary counts so visible status cannot lag live filtering, and explicitly focuses the next incomplete row after Worklist-only label save. The final controlled actual-EXE Recipe individually proved candidate, failed, needs-fix, and requested inclusion plus completed-label exclusion; two consecutive runs then produced `120->119`, completion `5->6`, zero queue invalidations/bulk changes, and active/selected image 001. Read `docs/IMAGE_QUEUE_ACTION_WORKLIST_20260722.md`; do not reopen it without a reproduced membership, transition, focus, or latency defect. The next implementation requires new operator evidence rather than another speculative queue system.
- The accumulated 2026-07-22 changes were separated into the three local
  feature commits listed above and independently verified from detached
  worktrees. Do not recombine them or repeat their checks unless the source,
  contract, or reproduced behavior changes. A remote update requires a new,
  explicit push request and must use a normal non-force push.
- The 2026-07-22 source-of-truth synchronization and current-EXE beginner
  object-detection/segmentation/anomaly audit are complete. Read
  `docs\BEGINNER_END_TO_END_UX_AUDIT_20260722.md` before reopening either
  slice. The current built EXE passed all three task flows. The audit also
  corrected missing canvas-tool automation names and object-detection geometry
  wording shown for image-level anomaly candidates.
- The reproduced dataset-wizard generated-name issue is complete. Purpose
  changes now synchronize only an untouched generated Recipe name and default
  storage path; any operator edit permanently protects that field for the
  current wizard session. The current EXE passed create, close/restart, and
  first anomaly inference. Read
  `docs\DATASET_PURPOSE_AUTOMATIC_NAME_SYNC_20260722.md` and do not reopen the
  slice without a focused regression.
- The prior rule that point/negative MobileSAM prompts required a newly
  reproduced box-prompt failure was superseded by the user-approved commercial
  labeling-parity direction. P0-B is now complete; do not reopen it without a
  failed regression or changed contract.
- The bounded MobileSAM single-box smart-mask slice, fixed 8-class exact-box matrix, deterministic box-jitter matrix, and P0-B point-correction session are completed regressions. The 24 exact prompts produced 24/24 usable candidates. The later 96-call matrix applied 20% expansion, 10% contraction, and 10% translation in each diagonal direction; all 96 remained usable, overall median IoU was `0.856132`, lowest class median was `crack 0.704918`, and the 4,525-file source tree SHA-256 stayed unchanged. The P0-B fixture separately proved positive expansion, negative removal, 48-point detail, source immutability, and actual-EXE confirm/next-instance behavior. Do not reinterpret any of this as commercial editor parity or field accuracy. Read `docs\MOBILE_SAM_SMART_MASK.md`, `docs\MOBILE_SAM_8_CLASS_USABILITY_MATRIX_20260722.md`, `docs\MOBILE_SAM_BOX_JITTER_MATRIX_20260722.md`, and `docs\LABELING_EDITOR_COMMERCIAL_GAP_AND_ROADMAP_20260727.md` before reopening it.
- The fixed three-model segmentation comparison is complete. On the unchanged 60-image canonical test masks, mean Dice/IoU is U-Net `0.243091/0.156165`, YOLOv8-seg at confidence `0.25` `0.721702/0.570198`, and YOLO11-seg at confidence `0.25` `0.773711/0.636553`. The YOLO11 30-epoch app/TCP run preserved the native 2,004-file tree SHA-256 and records full runtime/checkpoint provenance. This is a synthetic same-source engine benchmark, not adoption or production evidence. See `docs\SEGMENTATION_E30_THREE_MODEL_COMPARISON_20260722.md`.
- The external native YOLO segmentation source contract and approved 30-epoch same-data benchmark are complete: selected `data.yaml` source is read-only, U-Net receives a recipe-owned canonical raster-mask export, and every YOLO training request receives an app-owned runtime copy so cache files cannot mutate the source. U-Net CUDA versus YOLOv8-seg CPU completed 30 epochs at image 320/batch 4 on the same 360/80/60 packet; the 60-image common-mask report favors U-Net (`Dice/IoU 0.243091/0.156165`) over YOLOv8-seg (`0.079059/0.044103`), without selecting either model.
- Do not repeat the 30-epoch benchmark unless source, runtime, behavior, acceptance criteria, or a deliberate hyperparameter decision changes. The controlled error analysis and one final held-out replay are complete: the runner now records an explicit YOLO `0.25` confidence, which was selected on `valid` only; the unchanged test replay measured YOLOv8-seg Dice/IoU `0.721702` / `0.570198` versus U-Net `0.243091` / `0.156165`. Do not adopt either model automatically. Keep U-Net's two zero-Dice classes as a separate class-confusion/training hypothesis; independent camera/session data remains required for any production claim. See `docs\SEGMENTATION_E30_CONFIDENCE025_TEST_EVIDENCE_20260722.md`.
- The U-Net valid-only diagnosis, weighted/crop experiments, opt-in foreground selector, and CE plus foreground soft-Dice loss experiment are recorded. The soft-Dice run recovered `foreign_particle` but left `contamination_spot` at zero overlap and reduced same-valid macro Dice/IoU from the selector baseline `0.204437/0.127053` to `0.189220/0.111142`. It is rejected, its temporary code is removed, and held-out stayed closed. See `docs\UNET_E30_CLASS_CONFUSION_ANALYSIS_20260722.md`.
- The previously dirty Dataset Health, external native YOLO data.yaml intake, Image Queue responsiveness, anomaly isolation, model-comparison manifest, and anomaly OK/NG workspace slices are independently reviewed and committed. Keep their documented contracts stable unless a focused regression fails or requirements change.
- The operator deferred the unavailable real camera/network Image Queue profile. Keep the 10,000-image queue path stable and do not add paging, a DB, or another thumbnail cache from the existing local profiles.
- `D:\라벨테스트` now supplies completed synthetic cross-product evidence: native Switch Housing object detection held the YOLOv8n candidate after a five-repeat test comparison, and its 300-image anomaly mapping held the Washer classifier. Preserve the artifacts and do not rerun them unless source, weight, threshold, or acceptance criteria change.
- Continue synthetic-first feature development under `docs/SYNTHETIC_EVIDENCE_CONTRACT.md`. The operator explicitly excluded `D:\기타이미지\2022.11.16_SIT`; do not inspect or use it. A current audit found 33 roots under `D:\라벨테스트` and 130 metadata files explicitly declaring synthetic/procedurally generated data. These sources may prove reproducible product behavior when the contract gates pass, but they must remain `Field validation: Not evaluated` and must not be presented as production accuracy.
- The detailed 2026-07-20 SIT audit found 9,996 unique image contents, 111 duplicate-content groups, and 18 groups carrying conflicting OK/NG labels; no duplicate group crosses PC1/PC2. Remove or adjudicate those conflicts and prove content-hash split separation after box labeling. See `docs\WORK_TRACKING.md` for the reusable audit record.
- Do not start another broad UI redesign without a reproduced operator defect. The current task tabs, Dataset Health window, model-comparison workspace, Model Center workspace, and model-adapter catalog are completed contracts.
- The narrow user-approved Model Center workspace slice is complete: stage 4 is now full-width and presentation-only, with current-build evidence under `artifacts\ui\model-workspace-20260718`. Do not reopen it for general polish; reopen only for a reproduced layout or state-preservation defect.
- The explicit Model Adapter Catalog/Contract slice is complete and truth-aligned as of 2026-07-23: the Model Center shows six declared contracts—Recipe format, YOLOv5 detection, local YOLOv8, U-Net segmentation, ONNX inference-only, and verified-scope local YOLO11—with task/data/runtime/evidence/next-action boundaries. YOLO11 detection, segmentation, and the recorded local anomaly-classification path are verified; its current anomaly candidate remains `hold`, and arbitrary YOLO11 runtimes remain unverified.
- The anomaly `OK`/`NG` folder-name intake is complete as explicit review consent, not auto-labeling: opening folders, readiness, and export must keep images unreviewed until the temporary Image Queue card's `N장 일괄 판정` action is chosen. `이미지별 확인` stays unreviewed for that image-root session, saved manual decisions always win, and the header must not gain permanent buttons. Evidence and exact regression gates are in `docs/STABLE_VERIFIED_AREAS.md` and `docs/NEXT_THREAD_HANDOFF.md` section A0.

- The anomaly labeling workspace is complete as an image-level `OK/NG 이미지 판정` flow: no drawing/label-save/class-object controls are shown for anomaly purpose; the queue owns `정상(OK) → 다음`, `이상(NG) → 다음`, and `미판정으로 되돌리기`, shows `OK`/`NG`/`미판정`, persists through `anomaly-review-status.json`, and restores the standard annotation workspace when the purpose changes. The folder-consent buttons are now `N장 일괄 판정` and `이미지별 확인`; manual decisions still win. Do not reopen this slice unless a focused regression fails or the saved-state/training contract changes.
- The 2026-07-23 truth-alignment follow-up also makes `dataset.manifest.json` record `image-level-normal-abnormal` with navigation-only `panZoom`, removes anomaly drawing/edit tools from the learning ViewModel, and replaces defect-region beginner wording with whole-image OK/NG guidance. Returning to object detection restores Select. Evidence is under `artifacts/ui/20260723-contract-truth-alignment`; do not restore the obsolete box-and-mask metadata.

- Reproduced anomaly image-root defect fixed: after selecting an `images` root with nested `NG` and `OK` folders, automatic first-file loading and later queue-row clicks retain `images` as the queue root. The exact regression is in `--anomaly-folder-auto-review`; do not reintroduce leaf-folder queue repopulation.

Runtime rules:
- YOLOv8 uses C:\Git\yolov8, local ultralyticsMaster, local .venv, editable install, and labeling_tcp_client.py.
- ONNX is inference-only support, not a replacement for local training.
- Do not download weights, upgrade pip/packages, or alter dependencies without explicit approval.
- YOLO11 detection, segmentation, and anomaly classification are proven only for the recorded local Ultralytics runtime, compatible task weights, and focused app paths. The anomaly candidate is not adopted; arbitrary YOLO11 runtimes remain unverified.
- Preserve MVVM. Avoid Viewer/OpenGL/ROI/brush/eraser changes unless a specific defect requires them.
- Do not push unless the user explicitly says push. A commit request is local only.

Evidence rules:
- Code changes require isolated build, focused tests, and git diff --check.
- UI changes require current 1920x1080 evidence and a README/tutorial image relevance check.
- Do not claim model adoption, independent accuracy, or production readiness from synthetic, duplicate, validation-only, or same-source data.

Read docs/NEXT_THREAD_HANDOFF.md sections 7 through 11 before selecting focused tests. It lists:
- completed/protected areas;
- committed feature-slice map;
- current model evidence boundaries;
- unverified/blocked risks;
- ordered next priorities and matching verification commands.

Final report must include changed files, command results, screenshot requirement, remaining risk/unverified status, and the next priority with recommended model and reasoning effort.
~~~
