# P1-A Portable Project Archive

Date: 2026-07-30 KST  
Status: Complete

## Outcome

OpenVisionLab Labeling Studio can export the last explicitly saved Recipe and
its complete dataset root to a checksum-verified ZIP, then import that ZIP as
a new Recipe and a new dataset folder on another local Windows workspace.

This is a project-transfer contract, not background autosave or crash
recovery.

## Operator Workflow

Export:

1. Resolve every pending Smart Mask or other AI candidate with Confirm or
   Skip.
2. Press `라벨 저장` for the current image when the save state is dirty.
3. Press `설정 저장` when Recipe settings were changed.
4. Open `설정/도구 -> 프로젝트 이동`.
5. Press `프로젝트 아카이브 내보내기` and select a ZIP destination outside
   the Recipe and dataset folders.

Import:

1. Finish or stop active labeling, inference, training, comparison, or
   evaluation work.
2. Open `설정/도구 -> 프로젝트 이동`.
3. Press `프로젝트 아카이브 가져오기`.
4. Select the archive and a dataset parent folder.
5. Review the imported Recipe in the list, then press `적용` explicitly.

Import never overwrites an existing Recipe or dataset folder and never applies
the imported Recipe automatically.

## Archive Contract

`archive-manifest.json` records:

- archive schema and format;
- source/minimum-compatible application version;
- Recipe name, dataset purpose, dataset version ID, and canonical class order;
- every included path, byte length, SHA-256, kind, and train/valid/test split
  when applicable;
- absolute model/runtime/data references that remain outside the dataset and
  therefore are not included.

Included:

- the complete saved Recipe directory, including `VISION.xml`,
  `dataset.manifest.json`, dataset-version history, and Recipe-owned support
  files;
- the complete dataset root, including images, detection labels, segment
  JSON, masks, per-image `object-metadata` sidecars, review state, reports,
  and other evidence stored beneath that root.

Not made portable automatically:

- Python installations and external model repositories;
- weights or evidence outside the Recipe/dataset roots;
- credentials, cloud state, accounts, or deployment configuration.

Those external paths stay explicit in the manifest and must be rechecked on
the receiving machine.

## Safety And Compatibility

- Export is blocked while labels are dirty, mask-stroke commits are pending,
  AI candidates are unconfirmed, or named background work is active.
- Export does not call label save, Recipe save, candidate Confirm, training,
  or inference.
- Import validates schema, exact entry set, entry lengths, and every SHA-256
  before extraction.
- Unsafe/duplicate ZIP paths, reparse points, checksum changes, excessive
  entry count, and excessive declared uncompressed size fail closed.
- Import extracts to staging folders, rebases dataset-owned XML/JSON/YAML
  references, validates Recipe class order and output root, and promotes only
  after validation.
- A failure rolls back newly promoted targets. Existing targets are never
  deleted or overwritten.
- Dataset-owned paths are rebased to the new root. External runtime/weight
  references remain unchanged and visible.

## Verification

Commands:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\p1a-check\
.\tests\LabelingApplication.Tests\artifacts\p1a-check\LabelingApplication.Tests.exe --project-archive
.\tests\LabelingApplication.Tests\artifacts\p1a-check\LabelingApplication.Tests.exe --wpf-visual-smoke --width 1920 --height 1080 --open-header-tools-menu --screen-capture --output .\artifacts\p1a-portable-project-archive-20260730\after-header-tools-menu.png
```

Focused evidence covers:

- Recipe/config/class-order round trip;
- train/valid/test identity;
- images, labels, segment JSON, masks, object metadata, review state, reports,
  and dataset-version evidence;
- dataset-owned path rebasing and external-weight reference preservation;
- second-import non-overwrite failure;
- checksum-tamper rejection;
- failed-import cleanup;
- UI command presence and source checks proving there is no implicit save,
  Confirm, or Apply call.

The final clean solo default regression passed `262/262` with no failures.

Visual evidence:

- `artifacts/p1a-portable-project-archive-20260730/before-header-tools-menu.png`
- `artifacts/p1a-portable-project-archive-20260730/after-header-tools-menu.png`

The after capture shows both archive actions, the saved-state boundary, the
non-overwrite rule, and explicit Apply disclosure without obscuring the
existing diagnostics controls.

```text
Status: Complete
Scope: Explicit export/import of the last saved Recipe plus complete dataset root with manifest/checksum validation, staging, path rebasing, non-overwrite promotion, and explicit Apply.
Acceptance criteria: Saved Recipe/classes/annotations/metadata/splits/evidence round trip -> pass; dirty/pending/active state blocks -> pass; no implicit save/Confirm/Apply -> pass; checksum and path validation -> pass; existing targets never overwritten -> pass; failed import cleanup -> pass; current-source UI evidence -> pass.
Verification: Zero-warning/error focused build, --project-archive, current-source 1920x1080 before/after visual smoke, documentation gate, clean solo default regression 262/262, and git diff --check.
Evidence: This document, WpfPortableProjectArchiveService.cs, Program.ProjectArchive.cs, artifacts/p1a-portable-project-archive-20260730, and default-suite.stdout.log.
Boundary / next dependency: This does not provide crash recovery, autosave, cloud transfer, installer/signing, external runtime/weight redistribution, or production-quality evidence. P1-B bounded crash-recovery journal remains next local work.
```
