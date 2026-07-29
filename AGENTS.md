# AGENTS.md

This file defines how Codex should work in this repository.

## Operating Rules

- Start every development run with `git status --short`.
- Do not revert or overwrite existing user/Codex changes unless the user explicitly asks for that exact action.
- Do not run `git push` unless the user explicitly asks for `push`.
- A commit request means a local commit only. Push requires a separate explicit request.
- Keep MVVM boundaries: View code-behind may act as a UI adapter, but command/state/workflow/presentation logic should live in ViewModel or Service classes where feasible.
- Avoid Viewer/OpenGL/ROI/brush/eraser performance paths unless the task explicitly requires them.
- Public README/tutorial docs must not include local private paths, conversation notes, portfolio-only wording, or machine-specific details.

## Think Before Coding

- State the concrete goal before editing.
- List assumptions briefly. If an assumption affects behavior or data safety, verify it by opening files/logs/tests or ask the user.
- If the problem becomes unclear, stop and inspect the relevant file, log, or test instead of guessing.

## No Guessing

- Do not present unverified claims as facts.
- If you do not know, open the file, run the command, or inspect the log that can prove it.
- When explaining a conclusion, cite the file, test, command output, or log that supports it.
- If verification is interrupted or unavailable, mark the work as incomplete.

## Simplicity First

- Make the smallest change that satisfies the request.
- Do not add features, abstractions, or extra error handling unless they directly support the current goal.
- Prefer existing local patterns and services over new architecture.

## Surgical Changes

- Touch only the files needed for the request.
- Keep unrelated refactors out of the patch.
- Do not modify verified hot paths unless the request requires it and focused verification is included.

## Structure and Refactoring Rules

The goal is fast, reliable navigation for both people and LLMs. Prefer clear ownership over maximal decomposition.

- Before a structural change, state `current owner -> intended owner`, the included scope, the behavior that must not change, and the verification to run.
- Organize files by durable feature/domain ownership, never by file-size targets alone. A large cohesive class or file may remain intact.
- Do not create a new folder, DTO, service, interface, or partial file for a short one-off command path. Create one only for repeated logic, a durable domain boundary, or an independently testable responsibility.
- Keep WPF Views as UI adapters: view lifecycle, control integration, and narrowly scoped event bridging may remain in code-behind. Commands, screen state, enablement, workflow, and visible-text decisions belong in a ViewModel or the appropriate service.
- Keep reusable workflow, calculation, persistence, and presentation coordination in the matching service domain. Do not move namespaces merely to mirror a physical-folder move unless the namespace itself is misleading.
- `0. UI\9) WPF\Services` is organized by domain: `Annotation`, `Anomaly`, `CandidateReview`, `Dataset`, `Detection`, `ImageQueue`, `Infrastructure`, `Model`, `ObjectReview`, `Project`, `Runtime`, and `Training`. Put new services in the nearest existing domain; introduce a new domain only when it has a durable, clearly named boundary and more than one related responsibility.
- Keep `WpfLabelingShellWindow.<Domain>.cs` partials focused on one recognizable shell domain. Do not add a partial simply because an individual method is short.
- Keep test execution and shared helpers in `tests\LabelingApplication.Tests\Program.cs`; place only large, self-contained domain suites in clearly named `Program.<Domain>.cs` partial files. Do not mechanically split tests to reduce line count.
- When ownership or physical layout changes, update `docs\CODE_STRUCTURE.md` and every affected in-repository reference. Keep public documentation free of local paths and conversation-specific notes.
- Avoid restructuring Viewer, OpenGL, ROI, brush, or eraser paths unless the requested work requires it; these are performance-sensitive areas.

### Refactor Decision Check

Proceed only when all answers are clear:

1. Which existing or new domain owns this code?
2. What concrete discovery, maintenance, or reuse problem does the change solve?
3. Why is this a durable boundary rather than a one-off extraction?
4. Which build, focused tests, search checks, and documentation updates prove the move is complete?

## Goal-Driven Execution

- Convert broad requests into concrete completion goals.
- Prefer goals like "focused tests pass and wording is service-owned" over vague goals like "improve UX".
- Keep a clear next step in the final response.
- When completing priority-driven work, explicitly state any remaining next-priority work in the final response instead of leaving the next step implicit.

## Priority Communication

- A compact priority label is never sufficient by itself. Whenever reporting or proposing a priority, explain in plain language before acting: why it is next, the concrete outcome, included and excluded scope, completion evidence, and any prerequisite or safety boundary.
- Keep `Recommended model` and `Reasoning effort` as metadata after that explanation; do not let those two fields replace the explanation.
- If the priority is blocked by data, credentials, hardware, or an explicit user decision, name that prerequisite first and do not imply that more implementation alone will complete it.

## Reasoning Effort

- Low effort: typo fixes, formatting, simple text edits, one-line test expectation updates.
- Medium effort: single-service refactors, focused WPF binding changes, small documentation updates.
- High effort: workflow redesign, model runtime behavior, dataset persistence, performance work, training/inference execution, or cross-module refactors.
- Increase verification rigor with higher effort.

## Completion Definition

Completion must be proven by commands, not by wording alone.

- C# / WPF default:
  - `dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\`
  - Run the focused `LabelingApplication.Tests.dll` switches for the changed area.
  - `git diff --check`
- WPF UI visual changes:
  - Run the focused build/tests.
  - Capture or regenerate the relevant 1920x1080 screenshot when layout/visuals changed.
  - Update README/tutorial images only with current UI captures.
- Python worker changes:
  - Run Python compile/self-test commands relevant to the touched worker scripts.
  - Run the matching C# focused tests if the worker is called from WPF.
- Documentation-only changes:
  - Run `git diff --check`.
  - Run `--priority-workflow-docs` when workflow/readme/tutorial policy is touched.
- If the repository later adds other stacks, use their native gates:
  - Node: `pnpm test`, linter, typecheck.
  - Python: `pytest`, formatter/linter if configured.
  - Rust: `cargo test`, `cargo clippy`, `cargo fmt --check`.

Do not claim complete if the required verification did not run or did not pass.

## Current Project Priorities

- The combined current worktree passed 19/19 focused integration switches and
  the default internal suite at 258/258 in
  `docs/CURRENT_WORKTREE_INTEGRATION_VERIFICATION_20260729.md`. Preserve the
  loaded-window safe-close boundary, never-loaded cleanup behavior, and
  non-segmentation object-row aggregate-scan optimization.
- Follow
  `docs/LABELING_STUDIO_USER_CENTERED_DEVELOPMENT_DIRECTION_20260729.md`.
  Main-window safe close is Complete in
  `docs/SAFE_APPLICATION_CLOSE_P0_20260729.md`; do not reopen it without a
  changed requirement or focused regression.
- Canonical class-index visibility is Complete in
  `docs/CANONICAL_CLASS_INDEX_VISIBILITY_P1_20260729.md`; do not add reorder or
  schema migration without a separate contract.
- Smart Mask operator-documentation truth is Complete in
  `docs/SMART_MASK_OPERATOR_DOCUMENTATION_TRUTH_P2_20260729.md`; preserve the
  auto-first, correction/restore, explicit Confirm/Skip, and save-state wording.
- Dataset Health existing-data split filtering is Complete in
  `docs/DATASET_HEALTH_SPLIT_FILTER_P3_20260729.md`; preserve read-only
  split/problem composition, refresh fallback, and balanced per-split healthy
  sampling. Canonical read-only class filtering is also Complete in
  `docs/DATASET_HEALTH_CLASS_FILTER_20260729.md`; preserve class/split/problem
  composition, class-scoped 500-row bounds, refresh fallback, and source
  immutability.
- The four-point extreme-box implementation is Complete in
  `docs/FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md`. Preserve the
  Recipe-scoped `2점 드래그` / axis-aligned `top -> bottom -> left -> right`
  option, fourth-point-only ordinary Rectangle/history/Smart Mask handoff, and
  rotated Label Studio/CVAT import rejection. Do not reopen it as
  free-quadrilateral or rotated-box geometry.
- Object Review persistent `occluded` plus Recipe-tag metadata is Complete in
  `docs/OBJECT_METADATA_REVIEW_CONSUMER_P4_20260729.md`. Preserve the separate
  per-image sidecar, explicit label-save boundary, box occurrence/segment ID
  reconnect rules, visible/editable/resettable Recipe definitions, and
  combined review filters.
- Same-image Object Review grouping is Complete in
  `docs/OBJECT_GROUP_REVIEW_IMPLEMENTATION_P5_20260729.md`. Preserve the
  two-member minimum, one-group-per-object rule, schema-v2/v1-compatible
  sidecar, explicit save, dedicated selection set, group filters/badges,
  orphan dissolution, and documented merge/split mutation rules. Keep
  grouping separate from geometry merge, shared movement, training,
  interchange, collaboration, and automatic save.
- Continue improving OpenVisionLab Labeling Studio as a full workflow tool: dataset setup, image queue, class setup, object detection/segmentation/anomaly labeling, template labeling, training, inference, model runtime setup, and model comparison.
- Avoid repeating items already documented in `docs/WORK_TRACKING.md` and `docs/STABLE_VERIFIED_AREAS.md`.
- Keep verified items documented after completion.
