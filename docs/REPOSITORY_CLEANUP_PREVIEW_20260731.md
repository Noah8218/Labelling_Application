# Repository Cleanup Preview

Date: 2026-07-31 KST

Status: Complete

## Scope

This is a read-only cleanup proposal generated from the repository's current
artifact inventory after local test outputs moved to the D-drive storage
contract. It identifies exact `rebuildable-candidate` paths and their current
physical drives.

At generation time, this document did not authorize or perform deletion,
movement, compression, archival, or retention-policy changes. The operator
later approved only the unchanged 32-entry C-drive subset; its separate
execution and rebuild evidence is recorded in
`REPOSITORY_C_CANDIDATE_CLEANUP_EXECUTION_20260731.md`. The 13 D-drive
candidates remain outside that approval.

Source state:

- Git HEAD: `56f256a chore: centralize test artifacts and smoke runs`;
- current documentation-information-architecture edits were present in the
  worktree;
- source inventory SHA-256:
  `291E71123E009A60A6A8B7C856E599368C65777973256081EED2512EF0B18492`;
- preview evidence:
  `artifacts/repository-cleanup-preview-20260731/cleanup-preview.json`.

## Summary

| Physical drive | Candidates | Files | Potential size |
| --- | ---: | ---: | ---: |
| `D:` | 13 | 1,886 | 3.562 GiB |
| `C:` | 32 | 6,587 | 0.893 GiB |
| Total | 45 | 8,473 | 4.455 GiB |

Excluded from the proposal:

- `preserve-review`: 98 entries;
- `manual-review`: 51 entries;
- tracked files, source, documents, user datasets, and `.proofline`: always
  excluded;
- operations authorized: `false`;
- deletion performed: `false`.

Free space at preview time was `144.008 GiB` on C and `604.166 GiB` on D.

## Exact Candidate List

Exact byte counts, physical paths, reasons, source-inventory identity, and the
complete excluded-path arrays are in the JSON evidence. The table below is
sorted by current size.

| Logical path | Physical drive | Files | GiB |
| --- | --- | ---: | ---: |
| `tests/LabelingApplication.Tests/artifacts` | D: | 953 | 1.916 |
| `packages` | C: | 494 | 0.499 |
| `tests/LabelingApplication.Tests/bin` | D: | 390 | 0.294 |
| `artifacts/p1a-before-build` | D: | 72 | 0.252 |
| `artifacts/isolated-visual-qa-p4` | D: | 56 | 0.215 |
| `artifacts/isolated-visual-qa-p4-before` | D: | 56 | 0.215 |
| `artifacts/scissors-ui-out` | D: | 56 | 0.215 |
| `artifacts/scissors-contract-out` | D: | 56 | 0.215 |
| `artifacts/vertex-check-out` | D: | 56 | 0.215 |
| `bin` | C: | 67 | 0.147 |
| `.vs` | C: | 41 | 0.110 |
| `obj` | C: | 814 | 0.061 |
| `OpenVisionLab/Library/OpenVisionLab.ImageCanvas/bin` | C: | 47 | 0.057 |
| `tests/artifacts` | D: | 144 | 0.019 |
| `OpenVisionLab/Library/OpenVisionLab.Logging.Controls/obj` | C: | 4,147 | 0.006 |
| `tests/LabelingApplication.Tests/obj` | D: | 40 | 0.005 |
| `OpenVisionLab/Library/OpenVisionLab.ImageCanvas/obj` | C: | 325 | 0.003 |
| `OpenVisionLab/Library/RJControls/bin` | C: | 16 | 0.002 |
| `OpenVisionLab/Library/OpenVisionLab.MessageBox/bin` | C: | 12 | 0.001 |
| `OpenVisionLab/Library/OpenVisionLab.Logging.Controls/bin` | C: | 32 | 0.001 |
| `OpenVisionLab/Library/OpenVisionLab.Localization/obj` | C: | 59 | 0.001 |
| `OpenVisionLab/Library/WpfPropertyGridBridge/bin` | C: | 9 | 0.001 |
| `OpenVisionLab/Library/RJControls/obj` | C: | 42 | 0.001 |
| `OpenVisionLab/Library/OpenVisionLab.Localization/bin` | C: | 12 | 0.001 |
| `OpenVisionLab/Library/OpenVisionLab.Logging/obj` | C: | 42 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Pipeline.Controls/bin` | C: | 10 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Wpf.MessageDialogs/obj` | C: | 71 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Mvvm/obj` | C: | 58 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.ImageSpace.Core/obj` | C: | 58 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Mvvm/bin` | C: | 12 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Controls.Init/bin` | C: | 10 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.MessageBox/obj` | C: | 38 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Logging/bin` | C: | 16 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Pipeline.Controls/obj` | C: | 33 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Wpf.MessageDialogs/bin` | C: | 9 | 0.000 |
| `OpenVisionLab/Library/WpfPropertyGridBridge/obj` | C: | 19 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Controls.Init/obj` | C: | 38 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.ImageSpace.Core/bin` | C: | 12 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Display.Core/obj` | C: | 19 | 0.000 |
| `artifacts/isolated-obj` | D: | 7 | 0.000 |
| `OpenVisionLab/Library/PropertyGrid.Abstractions/obj` | C: | 17 | 0.000 |
| `OpenVisionLab/Library/OpenVisionLab.Display.Core/bin` | C: | 5 | 0.000 |
| `OpenVisionLab/Library/PropertyGrid.Abstractions/bin` | C: | 3 | 0.000 |
| `artifacts/isolated-app-out` | D: | 0 | 0.000 |
| `artifacts/isolated-bin` | D: | 0 | 0.000 |

## Reproduction

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Get-RepositoryArtifactInventory.ps1 `
  -OutputJson .\artifacts\repository-cleanup-preview-20260731\inventory-current.json

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Get-RepositoryCleanupPreview.ps1 `
  -InventoryJson .\artifacts\repository-cleanup-preview-20260731\inventory-current.json `
  -OutputJson .\artifacts\repository-cleanup-preview-20260731\cleanup-preview.json
```

## Acceptance Criteria And Verification

- fresh inventory mode is `inventory-only` and
  `deletionPerformed=false` -> pass;
- preview mode is `cleanup-preview-only`, `deletionPerformed=false`, and
  `operationsAuthorized=false` -> pass;
- candidate totals match the source inventory -> pass;
- every candidate logical path stays below the repository -> pass;
- every physical path stays below the repository or canonical D test-storage
  root -> pass;
- tracked-document references on candidates -> zero;
- mutation-command matches in the preview script -> zero;
- out-of-artifact output and inventory-overwrite attempts -> rejected;
- documentation classification, priority docs, and `git diff --check` -> pass.

Boundary / next dependency:

- sizes and paths can change after another build, test, or evidence capture;
- regenerate both JSON files immediately before any future cleanup decision;
- the unchanged 32-entry C subset was later explicitly approved and executed;
- the D-drive subset remains unapproved and requires a separate fresh exact
  target review and explicit approval;
- preserve-review and manual-review entries are not cleanup candidates;
- this preview does not affect the external GPU-capable P0-C gate.
