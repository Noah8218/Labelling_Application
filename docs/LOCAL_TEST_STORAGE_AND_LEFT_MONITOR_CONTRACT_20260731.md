# Local Test Storage And Left-Monitor Contract

Date: 2026-07-31 KST

Status: Complete

## Scope

This contract covers local test data placement and actual desktop EXE smoke
window placement for `C:\Git\Labelling_Application`.

Included:

- generated test, smoke, release-validation, screenshot, recording, and
  evidence artifacts;
- legacy test output and test-project `bin`/`obj` directories;
- repository root and component-project `bin`/`obj`/`artifacts` directories;
- local `packages` and `.vs` regeneration paths;
- the explicitly approved repository-tracked test-fixture `datasets` path,
  while preserving its Git-visible logical path and content;
- test-process `TEMP` and `TMP`;
- actual EXE smoke, UI automation, operator-video, and EXE screenshot windows.

Excluded:

- product source, documentation, and checked-in product dependencies;
- user datasets;
- `.proofline` state;
- direct in-process WPF view rendering that does not launch a desktop EXE;
- CI or targets without a `D:` drive, which must record their isolated fallback.

## Storage Contract

The canonical physical root is:

```text
D:\OpenVisionLab-TestData\Labelling_Application
```

Established repository-relative paths remain usable through directory
junctions. The migration tool verifies every copied source file by length and
SHA-256 before removing the C-drive source directory and creating its
junction. It is restartable after a partial cross-drive copy and supports
Windows extended-length paths.

| Logical path | Physical path |
| --- | --- |
| `artifacts` | `D:\OpenVisionLab-TestData\Labelling_Application\artifacts` |
| `tests\LabelingApplication.Tests\artifacts` | `D:\OpenVisionLab-TestData\Labelling_Application\legacy\LabelingApplication.Tests-artifacts` |
| `tests\LabelingApplication.Tests\bin` | `D:\OpenVisionLab-TestData\Labelling_Application\build-cache\LabelingApplication.Tests-bin` |
| `tests\LabelingApplication.Tests\obj` | `D:\OpenVisionLab-TestData\Labelling_Application\build-cache\LabelingApplication.Tests-obj` |
| `tests\artifacts` | `D:\OpenVisionLab-TestData\Labelling_Application\legacy\tests-artifacts` |
| `datasets` | `D:\OpenVisionLab-TestData\Labelling_Application\fixtures\repository-datasets` |
| repository `bin` / `obj` / `packages` | `D:\OpenVisionLab-TestData\Labelling_Application\build-cache\repository-*` |
| repository `.vs` | `D:\OpenVisionLab-TestData\Labelling_Application\ide-state\repository-vs` |
| `OpenVisionLab\Library\<project>\bin` / `obj` | `D:\OpenVisionLab-TestData\Labelling_Application\build-cache\components\<project>-*` |
| `OpenVisionLab\Library\<project>\artifacts` | `D:\OpenVisionLab-TestData\Labelling_Application\component-artifacts\<project>` |

The completed migration moved approximately `14.29 GiB`. C-drive free space
changed from `130.14 GiB` before migration to `144.16 GiB` after migration and
the subsequent focused build/smoke. The file-level migration record is
`D:\OpenVisionLab-TestData\Labelling_Application\migration-evidence\test-storage-migration.json`.

The later explicitly approved expansion moved another 20 nonempty paths with
1,341 files and exactly `604,817,071` bytes (`576.8 MiB`), then established
52 verified junction mappings in total. It includes all 516 tracked files in
the repository test-fixture `datasets` path. The dataset still matches the Git
index exactly. Windows free-space readings changed because of concurrent
system activity and are not attributed to this migration; the verified file
byte total is the authoritative reclaimed-content measurement.

Local test runner startup sets:

```text
OPENVISIONLAB_TEST_STORAGE_ROOT=D:\OpenVisionLab-TestData\Labelling_Application
TEMP=D:\OpenVisionLab-TestData\Labelling_Application\temp
TMP=D:\OpenVisionLab-TestData\Labelling_Application\temp
```

Use:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Move-LabelingTestStorageToDDrive.ps1 -Apply

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Build-LabelingApplicationTests.ps1 `
  -OutputName isolated-out
```

The build helper fails locally when `D:` exists but the repository artifact
junction does not target the canonical physical root. CI is explicitly
exempt from this workstation-only assertion.

## Actual EXE Monitor Contract

All actual desktop EXE smoke entry points use the shared
`PlaceExeSmokeWindowOnLeftmostMonitor` helper. It:

1. enumerates active screens;
2. selects the screen with the smallest bounds `Left` coordinate, then
   smallest `Top` coordinate;
3. places the EXE window inside that monitor's bounds;
4. reads the actual native window rectangle;
5. fails the smoke when the selected and actual screen names differ or the
   window does not intersect the selected monitor;
6. optionally writes monitor JSON and a virtual-desktop screenshot through
   `OPENVISIONLAB_EXE_SMOKE_MONITOR_EVIDENCE` and
   `OPENVISIONLAB_EXE_SMOKE_DESKTOP_SCREENSHOT`.

The rule is topology-based rather than display-number-based. On the current
workstation it resolved to `\\.\DISPLAY2`, bounds
`Left=-1920, Top=360, Width=1920, Height=1080`. The verified EXE rectangle was
exactly `-1920,360,1920,1080`.

The before capture already happened to open on the left monitor because of
the workstation's remembered window state. It was not an enforced test
contract. The after smoke proves deterministic selection, placement, and
native-coordinate verification.

## Acceptance Criteria And Evidence

- all 52 owned local data/output paths resolve to their canonical D targets ->
  pass;
- newly migrated nonempty paths -> 20 paths, 1,341 files, 604,817,071 bytes,
  with length and SHA-256 verification before C-source removal -> pass;
- repository test-fixture dataset -> 516 tracked files and no Git content
  difference -> pass;
- local test `TEMP` and `TMP` resolve under the D test root -> pass;
- central test build produces the logical and physical DLL with identical
  SHA-256 -> pass;
- actual current EXE smoke runs on the dynamically selected leftmost monitor
  and records the actual rectangle -> pass;
- focused build -> pass, zero warnings and zero errors;
- `--priority-workflow-docs` -> pass;
- post-expansion build -> pass, zero warnings and zero errors;
- post-expansion default regression -> pass, `264/264`, exit code `0`, `254`
  seconds;
- post-expansion artifact inventory -> 45 rebuildable candidates on D, zero
  rebuildable candidates physically on C;
- default regression -> pass, `264/264`, exit code `0`, `272.4` seconds;
- final `git diff --check` -> pass.

Evidence:

- `D:\OpenVisionLab-TestData\Labelling_Application\migration-evidence\test-storage-migration.json`;
- `artifacts/repository-structure-monitor-20260731/before-default-monitor.png`;
- `artifacts/repository-structure-monitor-20260731/before-default-monitor.json`;
- `artifacts/repository-structure-monitor-20260731/after-left-monitor.json`;
- `artifacts/repository-structure-monitor-20260731/after-left-monitor-desktop.png`;
- `artifacts/repository-structure-monitor-20260731/after-canonical-class-index.png`;
- `artifacts/tests/storage-monitor-20260731/LabelingApplication.Tests.dll`.
- `artifacts/repository-cleanup-preview-20260731/inventory-post-expanded-d-migration.json`;
- `artifacts/repository-cleanup-preview-20260731/cleanup-preview-post-expanded-d-migration.json`.

Boundary / next dependency:

- this proves local storage routing and current multi-monitor EXE placement;
- it does not prove the separate P0-C GPU-capable clean-target labeling gate;
- the repository fixture dataset is physically D-backed only on this approved
  workstation and remains logically tracked; do not extend that rule to user
  datasets, source, docs, product dependencies, or `.proofline`;
- do not hard-code the current `DISPLAY2` name if the monitor topology changes.
