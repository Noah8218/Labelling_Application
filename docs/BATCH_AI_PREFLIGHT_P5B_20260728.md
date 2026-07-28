# Batch AI Preflight P5-B

Date: 2026-07-28

Status: Complete

## Outcome

The visible-row batch-detection and failed-item retry commands no longer start
the worker immediately. Both commands first open one contextual AI batch
preflight window. The operator must review the current execution contract and
press `Start` explicitly.

This keeps batch options out of the permanent labeling rail while making the
settings visible at the moment they affect a run.

## Included scope

- requested scope and unique physical-image count;
- missing/unreadable image blocking;
- selected model engine, dataset task, weight path, and weight-file validation;
- configured inference confidence;
- current Recipe class catalog and case-insensitive `worker className` to
  same-name Recipe mapping contract;
- duplicate or blank Recipe class-name blocking;
- existing-label count and an explicit policy:
  - default: skip images with saved labels;
  - optional: include them while preserving all existing labels;
- exact runnable-image count after policy filtering;
- explicit `Start` only after the current dry-run passes;
- reuse of the existing batch execution/progress/stop loop;
- results routed only to Candidate Review, with no automatic approval or label
  save.

## Important mapping boundary

The current local worker protocol returns `className` and optional `classId`,
but it does not expose a common weight-metadata inspection command. The
preflight therefore shows and validates the actual application contract:
worker `className` is matched case-insensitively to the same Recipe class name.
It does not claim to have extracted the checkpoint's embedded class catalog.
This limitation is visible as a non-blocking warning, and all returned results
still require Candidate Review.

## Excluded scope

- automatic candidate approval or label save;
- overwriting existing annotations;
- checkpoint metadata extraction that the current worker protocol does not
  support;
- model-quality, production-camera, calibration, or throughput claims;
- cloud/team assignment, accounts, video tracking, and deployment.

## Ownership

- `WpfBatchDetectionPreflightService` owns the read-only request analysis,
  runtime/weight validation, class contract, existing-label policy, and
  filtered execution plan.
- `WpfBatchDetectionPreflightViewModel` owns contextual option state,
  presentation, recheck, and Start enablement.
- `WpfBatchDetectionPreflightWindow` is a modal UI adapter that returns only
  the approved plan.
- `WpfLabelingShellWindow.BatchDetection` opens the preflight and passes the
  approved item list to the existing `RunBatchDetectionAsync` loop.
- Existing batch execution, progress, cancellation, queue review state, and
  Candidate Review services remain unchanged owners of their behavior.

## Acceptance criteria

- batch and failed-retry commands cannot enter the worker loop before explicit
  Start: pass;
- valid model/weight, scope, confidence, class contract, and existing-label
  policy are visible: pass;
- default policy excludes labeled images and reports exact counts: pass;
- include policy preserves existing labels and reports a warning: pass;
- missing images, invalid runtime/weight settings, empty classes, blank classes,
  and case-insensitive duplicate classes block Start: pass;
- approved plans reuse the existing execution loop: pass;
- Candidate Review/no-auto-approval/no-autosave policy is visible and covered
  by a focused test: pass;
- existing batch progress and result-display regressions pass: pass;
- current-build 1920x1080 and 1366x768 layouts have no clipped controls: pass.

## Verification

```text
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
--wpf-batch-detection-preflight
--wpf-batch-detection-progress
--wpf-batch-detection-result
--wpf-labeling-shell
--priority-workflow-docs
git diff --check
```

The focused build completed with zero warnings and zero errors. Fresh
current-build before/after captures are stored in:

`artifacts/ui/batch-ai-preflight-p5b-20260728`

## Commercial-product lesson

CVAT/V7-style depth should be contextual rather than exposed as a permanent
wall of controls. This implementation makes consequential batch choices
visible immediately before execution while keeping the normal labeling surface
compact. It closes the bounded P5 preflight slice, not commercial platform
parity.

## Completion record

Status: Complete

Scope: Contextual fail-closed preflight for existing visible-row and failed-item
batch inference, ending in Candidate Review without automatic label writes.

Acceptance criteria: All criteria above passed.

Verification: Zero-warning/error isolated build, focused batch gates,
shell/docs gates, diff check, and two current-build viewport captures.

Evidence: This document, focused tests in
`tests/LabelingApplication.Tests/Program.BatchDetectionPreflight.cs`, and
`artifacts/ui/batch-ai-preflight-p5b-20260728`.

Boundary / next dependency: The next product dependency is independently
acquired production-camera/cross-session data for model-quality gates. More UI
work cannot satisfy that evidence requirement.

Recommended model: no model tokens until independent data is available.

Reasoning effort: not applicable until the prerequisite exists.
