# P1-B Bounded Crash Recovery Journal

Date: 2026-07-30 KST  
Status: Complete

## User Outcome

If OpenVisionLab Labeling Studio ends abnormally after the current image was
edited but before `라벨 저장`, the next launch offers exactly two choices:

1. `편집 복구`: load the original image and restore the draft as dirty
   in-memory annotation state.
2. `초안 폐기`: remove the draft and continue normal startup.

Recovery never writes the label file. After restoring, the operator must
inspect the image and press `라벨 저장` explicitly.

## Operator Check

1. Open one dataset image and add or edit a box, polygon, raster mask, or
   Object Review metadata.
2. Confirm that the UI says `라벨 저장 필요`.
3. Simulate an abnormal process end. Do not use the normal close dialog,
   because approved normal close intentionally removes the recovery draft.
4. Restart with the same Recipe and dataset.
5. Review the Recipe, image, draft time, reason, and object counts in the
   recovery dialog.
6. Press `편집 복구`.
7. Confirm that the geometry and metadata are visible and the UI still says
   that label save is required.
8. Press `라벨 저장` only after review, or close/discard without saving.

## Journal Contract

The single current-image journal lives below the current-user application data
root:

`Recovery/current-image-draft.json`

It records:

- schema/format and payload SHA-256;
- application version and UTC draft time;
- Recipe, dataset-root, and source-image identity;
- source-image byte length, last-write time, and pixel dimensions;
- dirty reason;
- manual box bounds, class, and Rectangle/Ellipse shape;
- confirmed annotation geometry represented as ordinary box or segmentation
  draft geometry;
- polygon points, cutout polygons, raster-mask pixels, object ID, component,
  z-order, and structural-operation metadata;
- persistent `occluded`, Recipe tags, and valid same-image group IDs.

It deliberately does not record:

- pending or skipped AI candidates;
- an unconfirmed Smart Mask candidate or Smart Mask prompt session;
- annotation undo/redo history;
- active training, inference, comparison, or background work;
- unsaved Recipe configuration;
- label files, model files, credentials, or cloud state.

## Safety And Lifecycle

- Dirty-state notifications are coalesced until WPF context idle so the
  snapshot observes the completed edit rather than the pre-edit history
  snapshot.
- Deferred brush/eraser work is captured only after its canonical mask commit
  queue is empty.
- Serialization runs off the UI continuation path and promotes through
  `.tmp -> current-image-draft.json`.
- A revision barrier prevents an older queued write from recreating a journal
  after explicit label save, discard, or normal close.
- The journal is limited to one image, 4,096 objects, 1,000,000 polygon
  points, 256 MiB, and seven days.
- Read validates SHA-256, Recipe, dataset root, image existence/size/time,
  image bounds, object counts, polygon points, and raster-mask dimensions.
- Corrupt, expired, wrong-Recipe, wrong-dataset, missing-image, or changed-image
  journals fail closed and move to `Recovery/Invalid` when possible.
- Explicit `라벨 저장`, explicit draft discard, and approved normal
  application close remove the active journal.
- Recovery clears pending candidate state and restores geometry as dirty
  manual annotation state. It does not call annotation save or candidate
  confirmation.

## Ownership

- `Services/Annotation/WpfCrashRecoveryJournalService.cs` owns the bounded
  envelope, validation, retention, integrity, atomic write, revision barrier,
  discard, and quarantine.
- `WpfLabelingShellWindow.CrashRecovery.cs` captures/restores current-image
  annotation state and adapts the startup dialog.
- Existing annotation persistence remains the only label-file writer.
- Existing Candidate Review and Smart Mask services remain the only owners of
  candidate confirmation and prompt-session state.

## Verification

Commands:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\p1b-check\
.\tests\LabelingApplication.Tests\artifacts\p1b-check\LabelingApplication.Tests.exe --crash-recovery
.\tests\LabelingApplication.Tests\artifacts\p1b-check\LabelingApplication.Tests.exe --application-close
.\tests\LabelingApplication.Tests\artifacts\p1b-check\LabelingApplication.Tests.exe --wpf-labeling-shell
.\tests\LabelingApplication.Tests\artifacts\p1b-check\LabelingApplication.Tests.exe --wpf-visual-smoke --width 1920 --height 1080 --screen-capture --show-crash-recovery-dialog --output .\artifacts\p1b-crash-recovery-20260730\after-crash-recovery-dialog.png
```

Focused evidence covers:

- box/shape, polygon, mask, and persistent metadata round trip;
- SHA-256 tamper rejection and quarantine;
- seven-day retention;
- Recipe mismatch rejection;
- explicit discard and stale queued-write suppression;
- no implicit annotation save or AI-candidate confirmation;
- startup and normal-close lifecycle wiring;
- current-source 1920x1080 recovery-dialog capture.

The final clean solo default regression passed `264/264`.

Visual evidence:

- `artifacts/p1b-crash-recovery-20260730/before-no-recovery-dialog.png`
- `artifacts/p1b-crash-recovery-20260730/after-crash-recovery-dialog.png`

```text
Status: Complete
Scope: One current-image, current-Recipe dirty annotation journal with explicit restore/discard, geometry and persistent metadata restoration, bounded validation/retention, atomic write, integrity check, quarantine, and normal lifecycle cleanup.
Acceptance criteria: Dirty current-image annotation draft round trip -> pass; restored state remains dirty -> pass; pending AI/Smart Mask candidate state excluded -> pass; no implicit label save or candidate confirmation -> pass; explicit discard/save/normal close cleanup -> pass; stale queued-write suppression -> pass; corrupt/stale/wrong-context rejection -> pass; current-source UI evidence -> pass.
Verification: Zero-warning/error isolated build, --crash-recovery, --application-close, --wpf-labeling-shell, current-source 1920x1080 before/after visual smoke, --priority-workflow-docs, clean solo default regression 264/264, and git diff --check.
Evidence: This document, WpfCrashRecoveryJournalService.cs, WpfLabelingShellWindow.CrashRecovery.cs, Program.CrashRecovery.cs, and artifacts/p1b-crash-recovery-20260730.
Boundary / next dependency: This does not provide background label autosave, candidate/session recovery, multi-image history, Recipe autosave, cloud sync, installer/signing evidence, or production model-quality evidence. P0-C still requires an approved clean Windows environment and release lifecycle decisions; independent production validation still requires approved data, thresholds, runtime/weights, and hardware.
```
