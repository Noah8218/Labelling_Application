# 2026-07-26 Refactoring Commit Scope

This document prepares the current dirty worktree for local commits. It does
not authorize staging, committing, pushing, reverting, or deleting files.

## Current snapshot

- Base commit: `ad569dc`
- Tracked changes: 111 files
- Untracked files: 99
- Deleted tracked files: 70
- Modified tracked files: 41
- `git diff --check`: passed; only existing LF-to-CRLF warnings were reported.
- Build and focused verification are recorded in `docs/WORK_TRACKING.md`.
- Do not use `git add .` or `git add -A` at repository root.

## Move audit

The 70 deleted files directly below `0. UI/9) WPF/Services` each have exactly
one same-named destination below a domain folder.

- 69 files are byte-identical moves.
- `WpfYoloEnvironmentCommandPresentationService.cs` is the only moved file
  with content changes.
- No deleted Service is missing or mapped to more than one destination.

The changed YOLO presentation service now owns requirements-check status,
package-operation log lines, and worker restart/stop presentation. The matching
View change removes those text/presentation decisions from
`WpfLabelingShellWindow.YoloEnvironmentRuntimeCommands.cs`.

## Commit 1: build artifact exclusion

Suggested title:

```text
build: exclude generated artifacts from default compile items
```

Files:

- `OpenVisionLab.LabelingStudio.csproj`

Reason:

- Keeps generated files below `artifacts` outside SDK default item discovery.
- This is independent from the structural and behavior changes.

Suggested staging:

```powershell
git add -- OpenVisionLab.LabelingStudio.csproj
```

Verification:

```powershell
dotnet build .\OpenVisionLab.LabelingStudio.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false
```

## Commit 2: dataset-purpose persistence fix

Suggested title:

```text
fix: persist dataset purpose after wizard bindings settle
```

Files:

- `0. UI/9) WPF/Views/WpfLabelingShellWindow.DatasetSetupCommands.cs`
- `0. UI/9) WPF/Views/WpfLabelingShellWindow.ShellProjectSettings.cs`
- `.proofline/issues/PL-0001.md`

Reason:

- Keeps the concrete `PL-0001` product fix separate from structural moves.
- The deferred save is recipe-name guarded and only the dataset-creation path
  opts into persistence.

Suggested staging:

```powershell
git add -- `
  "0. UI/9) WPF/Views/WpfLabelingShellWindow.DatasetSetupCommands.cs" `
  "0. UI/9) WPF/Views/WpfLabelingShellWindow.ShellProjectSettings.cs" `
  ".proofline/issues/PL-0001.md"
```

Verification:

```powershell
dotnet build .\OpenVisionLab.LabelingStudio.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-dataset-setup-request
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-yolov8-anomaly-restart-smoke
```

Recorded evidence:

- `artifacts/exe-yolov8-anomaly-restart-smoke/codex_yolov8_anomaly_restart_20260726_170631/summary.txt`
- Manifest and `VISION.xml` both record `AnomalyDetection`.
- Restart inference recorded one candidate and `Abnormal`.

## Commit 3: WPF Service domain ownership

Suggested title:

```text
refactor: organize WPF services by owning domain
```

Files:

- All deleted and added files below `0. UI/9) WPF/Services`
- `0. UI/9) WPF/Views/WpfLabelingShellWindow.YoloEnvironmentRuntimeCommands.cs`

Reason:

- Records the 69 exact Service moves and the one real YOLO presentation
  responsibility move as one coherent WPF ownership change.
- Namespace changes are intentionally absent; physical navigation changed
  without creating unrelated source-level dependency churn.

Suggested staging:

```powershell
git add -A -- "0. UI/9) WPF/Services"
git add -- "0. UI/9) WPF/Views/WpfLabelingShellWindow.YoloEnvironmentRuntimeCommands.cs"
```

Required review before commit:

```powershell
git diff --cached --summary
git diff --cached --check
```

The staged summary should show 70 old Service paths paired with 70 domain
destinations. It must not contain tests, unrelated Views, or general docs.

Verification:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
```

Run the focused YOLO environment presentation/runtime switches recorded in
`docs/WORK_TRACKING.md` before committing if the index is assembled in a new
session.

## Commit 4: explicit test-suite ownership

Suggested title:

```text
refactor: assign test suites to explicit owners
```

Files:

- All tracked and untracked `.cs` files below
  `tests/LabelingApplication.Tests`

Reason:

- Moves large self-contained suites out of hidden `Program` partial ownership.
- Keeps `Program.cs` as the CLI/default-regression and shared UI Automation
  harness.
- Adds `TestSupport.cs` as the shared non-UI helper owner.
- Leaves one `Program` partial declaration in the entry-point file and 46
  independent test-class files.

Suggested staging:

```powershell
git add -A -- "tests/LabelingApplication.Tests"
```

Required review before commit:

```powershell
git diff --cached --stat
git diff --cached --check
rg -n -g "Program*.cs" "internal static partial class Program" tests\LabelingApplication.Tests
```

Expected structural search:

- Only `tests/LabelingApplication.Tests/Program.cs` declares the partial
  `Program` entry-point type.

Verification:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --recipe-dataset-version-v2
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-dataset-version-smoke
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-external-evaluation-data-audit
```

The full circular-segmentation EXE command remains opt-in because it requires
the external `D:\circular_defect_labeling_dataset_v1\images` source and starts
real model training.

## Commit 5: refactoring rules and durable documentation

Suggested title:

```text
docs: record sustainable refactoring ownership
```

Files:

- `AGENTS.md`
- `docs/CODEX_NEXT_PROMPT.md`
- `docs/CODE_STRUCTURE.md`
- `docs/COMMIT_SCOPE_20260702.md`
- `docs/COMMIT_SCOPE_20260726_REFACTORING.md`
- `docs/IMAGE_QUEUE_10K_REVIEW_SLICE.md`
- `docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md`
- `docs/NEXT_THREAD_HANDOFF.md`
- `docs/SEGMENTATION_UX_COMPLETION.md`
- `docs/STABLE_VERIFIED_AREAS.md`
- `docs/WORK_TRACKING.md`
- `docs/YOLOV5_TRAINING_RESULT_WORKFLOW.md`

Reason:

- Keeps architecture navigation, path corrections, completion records, and
  future refactoring rules together after the code commits they describe.
- `docs/WORK_TRACKING.md` has a large accumulated evidence diff and should not
  be mixed into a product bug or mechanical move commit.

Suggested staging:

```powershell
git add -- `
  AGENTS.md `
  docs/CODEX_NEXT_PROMPT.md `
  docs/CODE_STRUCTURE.md `
  docs/COMMIT_SCOPE_20260702.md `
  docs/COMMIT_SCOPE_20260726_REFACTORING.md `
  docs/IMAGE_QUEUE_10K_REVIEW_SLICE.md `
  docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md `
  docs/NEXT_THREAD_HANDOFF.md `
  docs/SEGMENTATION_UX_COMPLETION.md `
  docs/STABLE_VERIFIED_AREAS.md `
  docs/WORK_TRACKING.md `
  docs/YOLOV5_TRAINING_RESULT_WORKFLOW.md
```

Verification:

```powershell
git diff --cached --check
```

## Explicit exclusions

Do not stage these with the five commits above:

- `.proofline/dashboard/*`: generated static dashboard assets.
- `.proofline/STATE.md`: local Proofline dashboard usage note.
- `artifacts/**`: ignored runtime/build/screenshot evidence.
- Any local runtime path, model weight, dataset, or Visual Studio user file.

Only `.proofline/issues/PL-0001.md` is part of the proposed commit history.

## Final pre-commit checklist

Run this for each assembled index:

```powershell
git diff --cached --name-status
git diff --cached --check
```

Before the final local commit:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
git status --short
```

Do not commit until the staged file list matches exactly one scope above.
Do not push unless the user separately requests `PUSH`.
