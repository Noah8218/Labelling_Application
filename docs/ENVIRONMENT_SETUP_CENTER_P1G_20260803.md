# Environment Setup Center P1-G

Date: 2026-08-03 KST
Status: Complete

## Scope

The dedicated `환경 설정 센터` is the first-use and recovery guide for a
locally installed OpenVisionLab Labeling Studio. It combines existing product
diagnostics with the selected model-runtime self-test instead of introducing
a second installer or a second settings store.

Included:

- one discoverable entry at `설정/도구 -> 진단/지원`;
- read-only application, user-path, Main Viewer graphics, Python, worker,
  package, model, and image-root checks;
- a YOLOv5 repository-integrity check for `hubconf.py`, `train.py`,
  `detect.py`, and `models/common.py` so a surviving folder is not mistaken
  for a recovered runtime;
- `준비됨`, required action, and optional/recommended counts;
- an actionable next step for every missing or warning item;
- a stable four-step installation guide;
- an explicit handoff to the existing model-runtime settings and package
  install/remove surface;
- dark-only responsive UI and owned-window lifecycle.

Excluded:

- silent Python, package, GPU-driver, or CUDA installation;
- installation on window open or refresh;
- administrator/elevation and reboot orchestration;
- model download, training, inference, model adoption, or settings save;
- installer/signing/upgrade/uninstall lifecycle implementation;
- a clean GPU-target or production-quality claim.

## Operator workflow

1. Open `설정/도구 -> 진단/지원 -> 환경 설정 센터` after installation or
   Windows recovery.
2. Resolve `필수 조치` items from top to bottom.
3. When Python or a model package is missing, open `모델 실행기 설정`, select
   the intended venv `Scripts\python.exe`, inspect the displayed target and
   command, and explicitly run the supported install action.
4. Return to the center and select `다시 점검`.
5. Treat GPU driver/CUDA as optional unless the selected workflow requires GPU
   acceleration.

Opening or refreshing the center is not permission to mutate the machine. It
uses `WpfRuntimeDiagnosticsService.RunReadOnlySelfTest()` plus
`PythonModelRuntimeSelfTestService.BuildReport()` and does not persist the
full self-test result.

## Ownership

- `WpfEnvironmentSetupCenterViewModel`: combined item mapping, counts, next
  actions, installation order, and no-side-effect command state.
- `WpfEnvironmentSetupCenterWindow`: themed responsive presentation only.
- `WpfLabelingShellWindow.EnvironmentSetupCenter.cs`: owned-window lifecycle
  and explicit navigation back to the existing model center.
- `WpfRuntimeDiagnosticsService` and `PythonModelRuntimeSelfTestService`:
  existing source-of-truth checks.

## Acceptance criteria

| Criterion | Result |
| --- | --- |
| Dedicated setup-center entry and owned window | Pass |
| App/viewer/current runtime combined in one list | Pass |
| Required versus optional items visible | Pass |
| Missing Python/package has a concrete next action | Pass |
| Incomplete YOLOv5 repository fails the required-utility check | Pass |
| Open and refresh remain read-only | Pass |
| Existing explicit package-install workflow reused | Pass |
| GPU driver/CUDA remain guide-only | Pass |
| Dark Wide and Compact layouts have current-source captures | Pass |
| Actual EXE opens the center on the dynamically selected leftmost monitor | Pass |

## Verification

- isolated Debug application/test build: 0 warnings, 0 errors;
- `LabelingApplication.Tests.dll --runtime-diagnostics-contract`;
- `LabelingApplication.Tests.dll --wpf-visual-smoke` in dark Wide and Compact
  setup-center states;
- `LabelingApplication.Tests.dll --exe-environment-setup-center-smoke` against
  the current Debug EXE;
- documentation information architecture and priority-workflow checks;
- `git diff --check`.

The final actual-EXE captures used the only monitor currently exposed to the
test process: `DISPLAY1`, bounds `0,0,1920,1080`. The dynamic leftmost-monitor
helper verified the Wide window at `400,160,1120,760` and the Compact window at
`500,230,920,620`. The Wide and Compact captures both show the keyboard-focus
border on `다시 점검`; shared button triggers also cover hover, pressed, and
disabled states. The window contains no editable input, selection, validation,
or popup controls, so those state categories are not applicable.

Current-task screenshots and monitor evidence are retained in the local
bounded test-evidence bundle named `environment-setup-center-20260803`. This
document does not promote those workstation-specific paths into the public
operator guide.

## Completion record

```text
Status: Complete
Scope: Dedicated read-only setup/recovery center with guided handoff to the existing explicit model-runtime installation surface.
Acceptance criteria: Integrated inventory, required/optional classification, actionable guidance, no-side-effect refresh, dark responsive UI, and actual-EXE leftmost-monitor entry -> pass.
Verification: Isolated build, focused runtime contract, Wide/Compact current-source captures, actual-EXE smoke, docs gates, and whitespace check -> pass.
Evidence: ENVIRONMENT_SETUP_CENTER_P1G_20260803.md and the bounded environment-setup-center-20260803 evidence bundle.
Boundary / next dependency: Python/GPU/CUDA external installation, installer/signing lifecycle, clean GPU-target labeling, and production model quality remain separate.
```
