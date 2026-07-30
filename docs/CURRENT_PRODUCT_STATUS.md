# Current Product Status

Last updated: 2026-07-30 KST

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

`README.md`, `docs/NEXT_THREAD_HANDOFF.md`, and `CODEX_NEXT_PROMPT.md` are
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
- Baseline Git commit at this review start:
  `4c6718a fix: contextualize object group controls`.
- Current protected regression evidence: the clean solo default internal suite
  passed `264/264` after the P1-B bounded crash-recovery slice.

These are scoped planning estimates. They do not claim production model
accuracy, commercial-platform parity, installation readiness, or clean-machine
operability.

## 4. Capability Status

| Area | Status | Current boundary |
| --- | --- | --- |
| Detection, segmentation, and anomaly labeling workflows | Complete for the focused workstation scope | Preserve explicit save/confirm and current geometry contracts |
| Recipe, class, queue, training, inference, and model-evidence flow | Complete for verified local workflows | Field accuracy and throughput require independent production evidence |
| Smart Mask auto-first labeling and correction | Complete | No production-boundary accuracy claim |
| Dataset Health split/class/problem filtering | Complete and read-only | Duplicate, leakage, near-duplicate, and domain-shift analysis are not yet a completed product contract |
| Object Review metadata and same-image groups | Complete | No automatic save, shared movement, cross-image tracking, or collaboration |
| Commercial release baseline audit | Complete | Local publishability is verified; commercial release readiness is not |
| Release/publish scripts | Complete for the engineering package baseline | Versioned self-contained `0.1.0` folder, fail-closed verification, and CI artifact are covered; installer/signing remain separate |
| Explicit version, deterministic package provenance, legal inventory | Complete for P0-B1 | Exact evidence is in `docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md`; formal public-release legal review remains external |
| Packaged runtime diagnostics and explicit support export | Complete for P0-B2 | Current-user paths, bounded retention, package/environment self-test, allow-list/redaction, and packaged first-launch immutability are covered |
| Installer, upgrade/uninstall, code signing, clean-machine evidence | Missing or unverified | Requires packaging decisions and a clean Windows VM or PC |
| Portable project archive | Complete for P1-A | Saved Recipe plus complete dataset root round trip, SHA-256 validation, path rebasing, non-overwrite import, and explicit Apply are covered |
| Crash recovery journal | Complete for P1-B | One current-image dirty draft, explicit restore/discard, bounded retention/integrity/context validation, and no candidate confirmation or label save |
| Headless CLI and complete CI release gates | Missing or partial | Scope after release-baseline findings |
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
- the existing viewer zoom/pan/drag, ROI, brush/eraser, history, layer, and
  annotation persistence contracts.

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

Prerequisite: an approved clean Windows environment and any required signing
credential. Do not spend implementation tokens on this gate until those are
available.

The approved first environment is Windows Sandbox on the current Windows 10
Pro host. `docs/P0C_WINDOWS_SANDBOX_SETUP_20260730.md` and
`scripts/New-P0CWindowsSandboxConfig.ps1` define a read-only release mapping,
an isolated writable evidence mapping, disabled networking/redirections, and
explicit application launch. This preparation does not complete P0-C:
interactive portable evidence, installer lifecycle decisions, and the later
signing gate remain.

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

### P2. Independent Production Validation

Measure accuracy, stability, failure modes, and takt time on approved,
provenance-confirmed, content-separated production-camera or cross-session
data using the intended runtime, weights, and hardware.

Prerequisite: representative data, acceptance thresholds, runtime/weights,
and target hardware. This is an external adoption gate, not a reason to invent
local product evidence.

### P3. Data-Centric QA And Active-Learning Efficiency

Duplicate/leakage/domain-shift analysis, uncertainty triage, and
human-in-the-loop relabeling may follow only after P0/P1 and after a measured
operator/data problem establishes the contract. Do not add broad editor
features by default.

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
Evidence: docs/CURRENT_PRODUCT_STATUS.md plus the aligned README.md, AGENTS.md, docs/NEXT_THREAD_HANDOFF.md, CODEX_NEXT_PROMPT.md, WORK_TRACKING.md, and STABLE_VERIFIED_AREAS.md.
Boundary / next dependency: P0-B1, P0-B2, P1-A portable project archive, and P1-B bounded crash recovery are complete. P0-C clean-machine installation evidence requires an approved clean Windows environment and release lifecycle decisions; signing and production validation remain separate.
```
