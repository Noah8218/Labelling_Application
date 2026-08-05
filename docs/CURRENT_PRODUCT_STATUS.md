# Current Product Status

Last updated: 2026-08-05 KST

This document is the single current source of truth for product identity,
verified maturity, the active development priority, and the boundary between
locally implementable work and external field validation.

## 1. Authority And Update Rules

Use repository information in this order:

1. `AGENTS.md`: operating, safety, verification, and repository rules.
2. `docs/CURRENT_PRODUCT_STATUS.md`: current product status and priority.
3. `docs/STABLE_VERIFIED_AREAS.md`: behavior that is complete and protected
   from casual reopening.
4. `docs/WORK_TRACKING.md`: chronological implementation and verification
   history.
5. Dated contracts, audits, and completion records: detailed evidence for an
   individual slice.

`README.md`, `docs/NEXT_THREAD_HANDOFF.md`, and `docs/CODEX_NEXT_PROMPT.md` are
navigation and handoff surfaces. They must point to this document rather than
own a different current priority. Live Git status, branch, and commit history
always override a recorded hash.

Update this document in the same change whenever product identity, maturity,
the next priority, or a prerequisite changes. Update the navigation surfaces
in that change and run `--priority-workflow-docs`.

## 2. Product Identity And Scope

OpenVisionLab Labeling Studio is a local Windows, single-operator industrial
image AI workbench. Its supported workflow is:

`dataset setup -> image queue -> class setup -> detection / segmentation /
anomaly labeling -> training -> inference -> review -> reproducible model
evidence`.

The product is not currently a cloud collaboration, account, reviewer
assignment, video tracking, deployment orchestration, camera/PLC control, or
enterprise-governance platform. Those areas remain out of scope unless the
product direction changes explicitly.

## 3. Current Maturity

- Focused single-workstation workflow maturity: `4.0/5`.
- Labeling-editor depth against the reviewed commercial-video baseline:
  `3.4/5`.
- Baseline Git commit for the current clean-source transfer candidate:
  `59e37d8 test: place P0-C smoke on the leftmost monitor`.
- Current protected regression evidence: the solo default internal suite
  passed `267/267` again on 2026-08-05 after the operational viewer-stability
  correction and actual-EXE GIF refresh. Exit code was 0 and the single-runner
  rerun completed in 251.2 seconds. The 2026-08-03 Environment Setup Center,
  restored-workstation D-drive rebind, tutorial-image contract, P1-I anomaly
  error worklist, and P1-J exact-content leakage results remain earlier
  protected evidence.
  The earlier
  `266/266` PatchCore heatmap/CI-gate, `265/265` bounded PatchCore pilot, and
  `264/264` P1-B results remain
  historical evidence.

These are scoped planning estimates. They do not claim production model
accuracy, commercial-platform parity, installation readiness, or clean-machine
operability.

## 4. Capability Status

| Area | Status | Current boundary |
| --- | --- | --- |
| Detection, segmentation, and anomaly labeling workflows | Complete for the focused workstation scope | Preserve explicit save/confirm and current geometry contracts |
| Recipe, class, queue, training, inference, and model-evidence flow | Complete for verified local workflows | Field accuracy and throughput require independent production evidence |
| Smart Mask auto-first labeling and correction | Complete | No production-boundary accuracy claim |
| Viewer stability during inference and Smart Mask review | Complete for the current workstation workflow | Result and guidance surfaces do not resize the viewer; inference does not force repeated Fit operations; one coalesced Fit remains allowed only after an actual viewport-size change |
| Dataset Health split/class/problem filtering | Complete and read-only | The anomaly-evaluation run gate now blocks exact content leakage; general duplicate, near-duplicate, and domain-shift analysis are not completed Dataset Health contracts |
| Object Review metadata and same-image groups | Complete | No automatic save, shared movement, cross-image tracking, or collaboration |
| Commercial release baseline audit | Complete | Local publishability is verified; commercial release readiness is not |
| Release/publish scripts | Complete through the clean-source `0.1.2` transfer candidate | Versioned self-contained folder, fail-closed verification, deterministic republish, and transfer ZIP are covered; installer/signing remain separate |
| Explicit version, deterministic package provenance, legal inventory | Complete for P0-B1 | Exact evidence is in `docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md`; formal public-release legal review remains external |
| Packaged runtime diagnostics and explicit support export | Complete for P0-B2 | Current-user paths, bounded retention, package/environment self-test, allow-list/redaction, and packaged first-launch immutability are covered |
| Environment Setup Center | Complete for the current-source guided workflow | Read-only app/viewer/current-model-runtime inventory, required/optional status, installation order, safety boundaries, and explicit handoff to the existing model-runtime install surface are covered; no silent install or driver/CUDA automation |
| Current-source viewer graphics capability preflight | Complete | The actual Main Viewer context and 11 required framebuffer functions are checked; a definite failure is visible in diagnostics/support evidence and blocks image opening before state mutation |
| `0.1.1` packaged graphics-preflight engineering evidence | Complete | Two deterministic dirty-source publishes, real packaged-EXE `8/8` diagnostics, and post-launch package immutability passed; this is not clean-source GPU-target evidence |
| `0.1.2` clean-source GPU-target transfer bundle | Complete for package preparation | `source.dirty=false`, two deterministic publishes, packaged headless preflight, 504/504 extracted payload hashes, ZIP SHA-256, and leftmost-monitor harness evidence passed; external GPU labeling remains unexecuted |
| Installer, upgrade/uninstall, code signing, clean-machine evidence | Partial | Windows Sandbox package-safe checks passed; a clean Windows 11 full Hyper-V VM reproduced the SharpGL framebuffer failure, while installer/signing decisions remain open |
| Portable project archive | Complete for P1-A | Saved Recipe plus complete dataset root round trip, SHA-256 validation, path rebasing, non-overwrite import, and explicit Apply are covered |
| Crash recovery journal | Complete for P1-B | One current-image dirty draft, explicit restore/discard, bounded retention/integrity/context validation, and no candidate confirmation or label save |
| PatchCore anomaly pilot, heatmap review, same-split comparison, error worklist, and exact-content leakage preflight | Complete for the bounded local workflow | Normal-only learning, image decision, raw score/threshold, review-only localization, matching-fingerprint comparison, bounded image-level outcome review, and pre-worker split/class exact-leakage blocking are implemented; field quality and location accuracy require approved external evidence |
| Workstation theme policy | Complete | The theme selector is hidden and legacy/internal light-theme requests resolve to the supported dark palette; do not reopen multi-theme work without an explicit product-direction change |
| Headless product CLI | Complete for the first read-only environment command | `--environment-self-test --json` emits structured status and bounded exit codes without WPF or durable writes; batch inference/training/archive/label operations are not CLI contracts |
| CI build/docs/full-regression/release-artifact gates | Complete for the repository workflow contract | The complete suite runs once with a 15-minute timeout; hosted GitHub success requires a pushed run |
| Independent production accuracy, long-run stability, and takt time | Blocked by external evidence | Requires approved, provenance-confirmed field data and intended hardware/runtime |

## 5. Protected Completed Areas

Do not reopen the following work without a changed requirement or a focused
regression:

- safe application close;
- canonical class-index visibility;
- Smart Mask auto-first, correction/restore, explicit Confirm/Skip, and
  save-state wording;
- Dataset Health split and class filtering;
- four-point axis-aligned extreme-box input;
- Object Review `occluded`, Recipe tags, same-image groups, and contextual
  group controls;
- canvas auto-fit after layout changes;
- stable viewer geometry while Smart Mask guidance, inference results, and
  Candidate Review state change;
- actual-viewer graphics capability diagnostics and fail-closed image-load
  guidance;
- the existing viewer zoom/pan/drag, ROI, brush/eraser, history, layer, and
  annotation persistence contracts.
- explicit PatchCore heatmap review without automatic open, save, confirm,
  candidate hide, active-layer change, or model adoption.
- hidden theme selection and dark-only shell rendering, including legacy light
  requests resolving safely to dark.
- one bounded no-argument complete regression invocation in CI before release
  publishing.
- read-only `--environment-self-test --json` dispatch before startup logging,
  mutex acquisition, or WPF creation, with no durable product-data writes.
- read-only Environment Setup Center refresh, explicit model-runtime settings
  handoff, and no automatic package/driver install, training, inference, model
  adoption, or settings save.

The detailed completion records remain in `docs/STABLE_VERIFIED_AREAS.md` and
the linked dated documents.

## 6. Productization Direction

Commercial labeling tools establish a higher bar than feature count. The next
stage must make the existing workflow installable, diagnosable, recoverable,
and reproducible.

### P0-A. Current-Source Commercial Release Audit And Contract

Status: `Complete`.

Verified outcome:

- executed the existing build, test, Release publish, and first-run checks from
  current source;
- inventoried versioning, publish contents, runtime dependencies, startup
  diagnostics, logs, license obligations, SBOM, installer, code signing,
  upgrade/uninstall, Python/CUDA/weights, recovery, archive, CLI, and CI;
- classified each item as verified, missing, externally blocked, or
  deliberately out of scope;
- selected one smallest local implementation slice based on evidence.

Included:

- read-only source and script inspection;
- current build/test/publish execution;
- documentation of commands, results, package inventory, gaps, and decisions.

Excluded during the audit:

- production-code changes;
- choosing or building an installer before the audit;
- commercial-readiness or production-accuracy claims;
- synthetic failure evidence.

Completion evidence:

1. Isolated build passed with 0 warnings/errors.
2. The default suite passed `260/260` in a solo run.
3. Framework-dependent and self-contained Release folders published and
   opened; the configured local YOLO smoke passed.
4. `docs/COMMERCIAL_READINESS_AUDIT_20260730.md` records source version,
   commands, results, artifacts, gaps, and boundaries.
5. `docs/NEXT_DEVELOPMENT_DECISION_20260730.md` selects exactly one next slice.
6. No production code was changed during the audit.

Recommended model: `gpt-5.6-terra`  
Reasoning effort: `medium`

### P0-B1. Versioned Deterministic Self-Contained Release Bundle

Status: `Complete`.

The bounded contract in `docs/NEXT_DEVELOPMENT_DECISION_20260730.md` is
implemented and verified. SDK `8.0.421`, product `0.1.0`, deterministic build
policy, a versioned self-contained `win-x64` folder, full payload SHA-256
provenance, project/third-party notices, fail-closed verification, focused
tests, and a CI artifact are now owned by the repository.

Completion evidence:

`docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md`

Recommended model: `gpt-5.6-terra`  
Reasoning effort: `high`

### P0-B2. Packaged Runtime Diagnostics And Support Bundle

Status: `Complete`.

Current-user startup/log/config routing, bounded retention, explicit
package/environment self-test, and a privacy-safe allow-list support ZIP are
implemented. The current packaged EXE launches without creating writable
folders in its application directory, and its fail-closed release manifest
still verifies after launch.

Completion evidence:

`docs/PACKAGED_RUNTIME_DIAGNOSTICS_P0B2_20260730.md`

Recommended model: `gpt-5.6-terra`  
Reasoning effort: `high`

### P0-C. Clean-Machine Installation Evidence

Verify install, launch, first-run diagnostics, upgrade, uninstall, and
artifact removal on a clean Windows VM or PC. Installer technology and
code-signing requirements must be explicit before implementation.

Prerequisite for installer lifecycle work: an explicit installer/signing
decision and any required signing credential. Prerequisite for another viewer
attempt: access to the selected supported GPU-capable clean Windows target and
the ability to return its bounded evidence folder.

The first approved environment, Windows Sandbox on the current Windows 10 Pro
host, completed the packaged first-launch, `7/7` environment self-test,
support-bundle, active-log, normal-close, and `505`-file package-immutability
slice. A writable guest extraction also verified all `503` manifest payload
hashes and created a real dataset through the packaged wizard.

The labeling slice remains `Incomplete`. Windows Sandbox failed image load
with `glGenFramebuffersEXT not supported` under all three tested graphics/
client combinations. After the user-approved host restart, an official
Windows 11 25H2 Korean x64 ISO was hash-verified, a fail-closed Generation 2
VM was created, Windows was installed, and a clean installed-Windows
checkpoint was preserved. The same clean-source release verified all `503`
manifest payload files, launched, and created a real dataset in that guest.
The standard Hyper-V synthetic display then reproduced the same
`glGenFramebuffersEXT not supported` viewer failure.

The current-source runtime graphics-preflight slice is `Complete`. The actual
Main Viewer context now reports its OpenGL vendor/renderer/version and checks
all 11 framebuffer functions used by the SharpGL viewer. A definite failure is
included in `환경 점검` and the explicit support bundle, and the central
image-load path stops before annotation state changes while showing actionable
local-PC/GPU-capable-VM guidance. This is documented in
`docs/RUNTIME_GRAPHICS_CAPABILITY_PREFLIGHT_P0C_20260731.md`.

This current-source preflight is not present in the immutable `0.1.0` package
used by the existing Sandbox/Hyper-V evidence. A deliberately versioned
self-contained `0.1.1` engineering package now contains it. Two consecutive
publishes were byte-identical, the real packaged EXE passed `8/8` environment
checks on the current GTX 1060 host, and all `503` payload hashes still
verified after launch. The empty startup review-cache write discovered during
the first package smoke was corrected; the final package created no local
`DATA/review-status.json`. Exact evidence is in
`docs/ENGINEERING_RELEASE_0_1_1_GRAPHICS_PREFLIGHT_EVIDENCE_20260731.md`.

The `0.1.1` manifest records `source.dirty=true`, so it is an engineering
candidate rather than clean-source target evidence. Do not silently replace
the prior immutable `0.1.0` evidence package or send `0.1.1` into the P0-C
clean-target gate as a release artifact.

A clean-source self-contained `0.1.2` transfer candidate is now prepared from
commit `59e37d8`. Two publishes produced identical manifests and full-package
inventories; the packaged read-only environment command passed `7/1/0`
without writes; the transfer ZIP was newly extracted and all `504/504`
payload hashes passed with no missing or unlisted files. The included harness
also records verified leftmost-monitor placement. Exact evidence is in
`docs/P0C_CLEAN_SOURCE_TRANSFER_BUNDLE_0_1_2_20260801.md`.

Do not repeat the Sandbox or standard Hyper-V viewer loop unless the display
capability or viewer implementation changes. On 2026-07-31, the operator
selected a separate GPU-capable clean Windows PC or explicitly GPU-capable VM
for the next viewer attempt. The viewer compatibility/fallback project is not
active. The historical Windows 10 Pro/GTX 1060 host could not provide an
officially supported Hyper-V DDA/GPU-P target, and its accessible VM had no GPU
adapter.

After the 2026-08-03 Windows reinstall, the host is Windows 11 Pro build 26100
on GPT/EFI and the GTX 1060 driver reports `OK`; Hyper-V is absent. This removes
the previous MBR barrier, but the operator explicitly declined same-PC native
boot. Do not download an ISO, create a VHDX, modify BCD, or request a restart
for P0-C. The package prerequisite remains complete and its transfer ZIP hash
was reverified. Normal product use relies on a real Windows GPU/driver plus the
actual-viewer 11-function graphics capability preflight; incompatible graphics
paths fail before annotation-state mutation with recovery guidance. A later
ordinary GPU-equipped Windows PC may supply the remaining clean-target
label/save/reopen evidence opportunistically. Installer lifecycle and signing
decisions remain separate.

Evidence:

`docs/P0C_PORTABLE_SANDBOX_EVIDENCE_20260730.md`

`docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md`

`docs/P0C_GPU_CAPABLE_CLEAN_TARGET_VALIDATION_PLAN_20260731.md`

`docs/P0C_CLEAN_SOURCE_TRANSFER_BUNDLE_0_1_2_20260801.md`

`docs/RUNTIME_GRAPHICS_CAPABILITY_PREFLIGHT_P0C_20260731.md`

`artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731`

### P1-A. Portable Project Archive

Status: `Complete`.

The last explicitly saved Recipe and complete dataset root now round-trip
through a versioned, per-file SHA-256 archive. Dirty labels, pending candidates,
and active work block export/import. Import stages and validates content,
rebases dataset-owned paths, refuses existing targets, and leaves Recipe Apply
explicit.

Completion evidence:

`docs/PORTABLE_PROJECT_ARCHIVE_P1A_20260730.md`

The final clean solo default regression passed `262/262`.

### P1-B. Bounded Crash Recovery Journal

Status: `Complete`.

One current-image dirty annotation draft is now written atomically below the
current-user application-data root. Startup offers explicit `편집 복구` or
`초안 폐기`; restore validates Recipe, dataset, image identity, age, bounds,
payload size, and SHA-256 before restoring geometry and persistent Object
Review metadata as dirty in-memory state. Pending AI and Smart Mask candidates
are excluded, label save remains explicit, and explicit save/discard or normal
close removes the active journal.

Completion evidence:

`docs/BOUNDED_CRASH_RECOVERY_P1B_20260730.md`

The final clean solo default regression passed `264/264`.

### P1-C. PatchCore Normal-Only Anomaly Pilot

Status: `Complete` for the bounded local implementation.

PatchCore is now a selectable anomaly model profile beside YOLOv8/YOLO11
supervised classification. It uses reviewed normal train images only, uses
normal validation images for threshold calibration when available, and
returns an image decision, raw score, learned threshold, review-only location
candidates, and a heatmap evidence file/path. A real GTX 1060 smoke trained on three synthetic
normal images, calibrated on one independent normal image, correctly held a
normal test image and flagged the synthetic defect image with a location
candidate.

This is not a field-quality or automatic-adoption claim. The next model gate
requires approved held-out production images and acceptance thresholds for a
same-split comparison against the current YOLO classifiers.

Completion evidence:

`docs/PATCHCORE_ANOMALY_PILOT_20260731.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `high`

### P1-D. PatchCore Heatmap Review View

Status: `Complete`.

PatchCore candidates now expose an explicit `히트맵 보기` action in AI
Candidate Review. Selection performs only path inspection; the image is
decoded after the action into an owned, themed review window. Missing, moved,
unsupported, corrupt, and unreadable files fail closed with guidance. Closing
the window or changing candidate releases the preview. No label, candidate,
active layer, viewer state, or model selection changes automatically.

Completion evidence:

`docs/PATCHCORE_HEATMAP_REVIEW_VIEW_20260801.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P1-H. Anomaly Same-Split Comparison

Status: `Complete` for the bounded local workflow contract.

Model Center can run the existing anomaly evaluation route for YOLOv8,
YOLO11, or PatchCore. Every summary records a common content fingerprint,
model/worker hashes, model-specific decision threshold, balanced accuracy,
normal false positives, abnormal misses, timing conditions, and PatchCore
location/heatmap evidence. Opening Model Benchmark from a project anomaly
summary includes and preselects the current-project anomaly runs with the same
evidence fingerprint. The comparison is read-only and never saves labels,
relabels images, retrains, or adopts a model automatically.

The retained two-image CPU smoke proves the shared execution/evidence contract
only. It is not field-quality or production-Takt evidence, and PatchCore
location quality remains `not-evaluated` without defect-region ground truth.

Completion evidence:

`docs/ANOMALY_SAME_SPLIT_COMPARISON_P1H_20260803.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `high`

### P1-I. Anomaly Evaluation Error Worklist

Status: `Complete` for the bounded read-only workflow.

Model Benchmark now reads the existing image-level anomaly `samples` from
selected same-fingerprint evaluation summaries. The summary shows each run's
correct, normal false-positive, and abnormal-miss counts. The class/error tab
shows a bounded 500-row, error-first worklist with saved source-image preview,
expected-to-predicted class flow, confidence or anomaly score, decision
threshold, and PatchCore location/heatmap evidence. Class-matching predictions
below threshold are identified separately from class mismatches.

The worklist does not rerun inference, navigate the labeling workspace, edit
labels, change review state, tune thresholds, train, or adopt a model.

Completion evidence:

`docs/ANOMALY_EVALUATION_ERROR_WORKLIST_P1I_20260803.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P1-J. Anomaly Evaluation Content-Leakage Preflight

Status: `Complete` for exact content identity in the app-exported anomaly
evaluation dataset.

Before any evaluation worker starts, the run service now reuses the existing
file-length plus SHA-256 audit to compare populated train/valid/test pairs and
normal/abnormal folders within every populated split. Renamed exact duplicates
are blocked with the affected scope, example, directories, and corrective
guidance. An unreadable populated audit scope also fails closed.

This does not detect perceptual near-duplicates or domain shift and does not
move, delete, relabel, or reassign any image automatically.

Completion evidence:

`docs/ANOMALY_EVALUATION_CONTENT_LEAKAGE_PREFLIGHT_P1J_20260803.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `high`

### P1-E. CI Complete Regression Gate

Status: `Complete` for the repository workflow contract.

The Windows CI job now runs the complete no-argument regression suite exactly
once in one process with a 15-minute timeout after build/documentation checks
and before release publishing. The same command passed `266/266` locally with
exit code 0, zero failures, and empty stderr. A hosted GitHub Actions success
claim still requires commit/push and inspection of that run.

Completion evidence:

`docs/CI_COMPLETE_REGRESSION_GATE_20260801.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P1-F. Headless Environment Self-Test CLI

Status: `Complete` for the current-source read-only command.

`OpenVisionLab.LabelingStudio.exe --environment-self-test --json` dispatches
before startup diagnostics, mutex acquisition, and WPF creation. It emits one
JSON document, returns exit code 0/2/64/70, does not persist self-test output or
probe/create directories, and reports the unavailable UI-context graphics
probe as a warning. The actual product EXE and the full `267/267` regression
passed; packaged clean-machine behavior remains a separate gate.

Completion evidence:

`docs/HEADLESS_ENVIRONMENT_CHECK_CLI_20260801.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `high`

### P1-G. Environment Setup Center

Status: `Complete` for the current-source guided setup workflow.

`설정/도구 -> 진단/지원 -> 환경 설정 센터` combines the existing application
diagnostics and selected model-runtime self-test into one dedicated dark UI.
It classifies required and optional items, shows the next action for each
missing path/package, and explains a deterministic setup order. Opening or
refreshing the center uses the read-only diagnostics path and does not persist
a self-test, install/remove packages, save model settings, train, infer, or
adopt a model. The explicit `모델 실행기 설정` action returns to the existing
model center, where the operator can verify the target venv and command before
running an already supported package installation.

Python itself, GPU drivers, and CUDA remain guided external prerequisites.
They are not silently downloaded or installed because administrator rights,
hardware compatibility, licensing, and reboot requirements vary by target.
This center does not complete the installer/signing lifecycle or the external
GPU-capable clean-target labeling gate.

Completion evidence:

`docs/ENVIRONMENT_SETUP_CENTER_P1G_20260803.md`

Recommended model: `gpt-5.6-terra`
Reasoning effort: `medium`

### P2. Independent Production Validation

Measure accuracy, stability, failure modes, and takt time on approved,
provenance-confirmed, content-separated production-camera or cross-session
data using the intended runtime, weights, and hardware.

Prerequisite: representative data, acceptance thresholds, runtime/weights,
and target hardware. This is an external adoption gate, not a reason to invent
local product evidence.

### P3. Data-Centric QA And Active-Learning Efficiency

Perceptual near-duplicate/domain-shift analysis, uncertainty triage, and
human-in-the-loop relabeling may follow only after a measured operator/data
problem establishes the contract. Exact anomaly-evaluation byte-content
leakage is already blocked by P1-J. Do not add broad editor features by
default.

Recommended model: `gpt-5.6-terra`  
Reasoning effort: `high`

## 7. External Decisions And Prerequisites

- Clean-machine verification environment.
- Installer and upgrade/uninstall policy.
- Code-signing certificate and release identity, if signed distribution is
  required.
- Redistribution and licensing decisions for Python, CUDA, model weights, and
  third-party native runtimes.
- Representative production data, ground truth, thresholds, and target
  hardware for field claims.

## 8. Durable Closure

```text
Status: Complete
Scope: Consolidated the current product identity, maturity, protected completed areas, authority order, and commercial-productization roadmap into one current source of truth.
Acceptance criteria: One authority document exists; navigation documents point to it; the immediate audit is bounded and evidence-based; locally implementable work is separated from external prerequisites; completed editor slices are protected.
Verification: --priority-workflow-docs and git diff --check for this documentation change.
Evidence: docs/CURRENT_PRODUCT_STATUS.md plus the aligned README.md, AGENTS.md, docs/NEXT_THREAD_HANDOFF.md, docs/CODEX_NEXT_PROMPT.md, WORK_TRACKING.md, and STABLE_VERIFIED_AREAS.md.
Boundary / next dependency: P0-B1, P0-B2, P1-A portable project archive, P1-B bounded crash recovery, the current-source actual-viewer graphics preflight, local 0.1.1 engineering evidence, and the clean-source 0.1.2 transfer bundle are complete. P0-C package-safe clean-machine evidence is partial; the standard Hyper-V display failed the SharpGL gate. Direct access to the selected GPU-capable clean Windows target is required before the next packaged labeling attempt. Installer/signing and production validation remain separate.
```
