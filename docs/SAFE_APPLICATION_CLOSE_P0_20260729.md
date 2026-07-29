# P0 Safe Application Close

Date: 2026-07-29

Status: `Complete`

## User Outcome

The main window now resolves work that would otherwise be lost before WPF
releases the window:

```text
Clean idle state
-> close immediately

Unsaved annotation or pending mask-stroke work
-> Save and close | Discard and close | Continue working

Unconfirmed AI candidate only
-> Discard and close | Continue working
-> never offer candidate save, automatic confirmation, or automatic label save

Active long-running work
-> name the active work in the close details
-> stop it only after the operator approves closing
```

If canonical annotation persistence returns failure, a warning explains that
the label was not saved and the main window remains open.

## Ownership And Boundaries

- `WpfApplicationClosePolicyService` owns the independently testable
  state-to-prompt policy.
- `WpfLabelingShellWindow.ShellLifecycle` remains the WPF `Closing` adapter,
  maps the dialog result, and calls the existing `SaveCurrentAnnotations`
  persistence path.
- Candidate Review remains the owner of pending/confirmed candidates. The
  close path does not call candidate confirmation or add candidates to saved
  annotations.
- Existing `Closed` cleanup remains the owner of queue cancellation, Batch AI
  cancellation, runtime shutdown, timers, caches, and bitmaps. Smart Mask
  cancellation is now included in that same post-approval cleanup.
- Annotation formats, Viewer/OpenGL, ROI, brush, eraser, and model worker
  protocols were not changed.

Named active work covers Smart Mask generation, current-image inference, Batch
AI, training, model-runtime setup, model comparison, segmentation-adapter
comparison, and anomaly evaluation.

## Verification

Commands:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --application-close
dotnet build .\OpenVisionLab.LabelingStudio.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-label-create-queue-locality-smoke --verify-safe-close --exe .\artifacts\run\Debug\OpenVisionLab.LabelingStudio.exe --artifact-root .\artifacts\exe-safe-close-p0-20260729
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-yolov8-detect-restart-smoke --verify-safe-close --exe .\artifacts\run\Debug\OpenVisionLab.LabelingStudio.exe --artifact-root .\artifacts\exe-safe-close-p0-20260729-candidate-only
```

Results:

- isolated test build: warning 0, error 0;
- application build: warning 0, error 0;
- clean, dirty, candidate-only, combined-state, cancel, discard, and
  save-failure policy/adapter checks: pass;
- actual Debug EXE dirty-label close -> cancel -> save -> clean close ->
  reopen saved label: pass;
- actual Debug EXE YOLOv8 pending candidate -> cancel -> candidate preserved
  without label file -> discard and close without label file: pass;
- 125-image queue locality remained unchanged through create/save:
  invalidation `0`, bulk change `0`, scroll `55.50 -> 55.50 -> 55.50`.

Evidence:

- before:
  `artifacts/ui/safe-close-p0-20260729/before/main-dirty-label-before-close.png`;
- current-source after:
  `artifacts/ui/safe-close-p0-20260729/after/dirty-label-close-dialog.png`,
  `artifacts/ui/safe-close-p0-20260729/after/pending-candidates-close-dialog.png`;
- actual Debug EXE dirty/reopen:
  `artifacts/exe-safe-close-p0-20260729/screenshots/03_safe_close_dialog.png`,
  `artifacts/exe-safe-close-p0-20260729/screenshots/05_reopened_saved_label.png`;
- actual Debug EXE candidate-only:
  `artifacts/exe-safe-close-p0-20260729-candidate-only/screenshots/05_candidate_only_safe_close.png`;
- EXE SHA-256:
  `55D29CF9A9D8FE9BD991ACEAB3A79F322F075A2CF3B966DF59E3469FBD6B8640`.

## Durable Closure

```text
Status: Complete
Scope: Cancelable main-window close for unsaved labels/pending mask work, unconfirmed candidates, and named active work.
Acceptance criteria: Clean close; explicit save/discard/cancel; failed-save retention; no candidate auto-confirm/save; active-work disclosure; actual-EXE dirty and candidate-only replay -> pass.
Verification: Isolated/app builds, --application-close, actual-EXE queue-locality safe-close replay, actual-EXE YOLOv8 candidate-only safe-close replay, fresh UI comparison, documentation gate, and git diff check.
Evidence: This document and the listed artifacts.
Boundary / next dependency: This proves close-state safety, not crash recovery, background autosave, model accuracy, or multi-user recovery. Canonical class-index visibility is now separately Complete.
```

Integration note: the decision contract applies after the main window enters a
loaded operator session. A shell that was never loaded closes without an
operator modal during startup failure or test cleanup. Loaded-window
save/discard/cancel behavior remains unchanged and is protected by
`--application-close`. See
`docs/CURRENT_WORKTREE_INTEGRATION_VERIFICATION_20260729.md`.

The following canonical class-index priority is Complete in
`docs/CANONICAL_CLASS_INDEX_VISIBILITY_P1_20260729.md`.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`
