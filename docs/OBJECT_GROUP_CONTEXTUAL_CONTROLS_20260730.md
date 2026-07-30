# Object Review Contextual Group Controls

Date: 2026-07-30

Status: Complete

## Scope

The Object Review metadata panel now presents group commands according to the
operator's current state:

1. a selected, ungrouped saved object shows only `그룹 구성`;
2. group-selection mode shows only the selection preview,
   `선택한 객체로 그룹 만들기`, and `취소`;
3. a selected grouped object shows only its current group, member removal,
   dissolve, group occluded, and group Recipe-tag actions.

The ViewModel owns these presentation decisions. The View remains a binding
adapter. The existing group selection, mutation, sidecar persistence, and
explicit label-save services are unchanged.

## Included And Excluded

Included:

- contextual visibility for the three group-workflow states;
- explicit wording for the group-create action;
- focused state-transition coverage;
- current Debug EXE create/save/navigate/reopen replay;
- fresh before/after screenshots and full-duration videos.

Excluded:

- geometry merge or shared movement;
- automatic group creation, save, or candidate approval;
- cross-image/video tracking;
- training, inference, or interchange semantics;
- collaboration or cloud review.

## Acceptance Criteria

- ungrouped object -> only group-start action: pass;
- group-selection mode -> only selection preview and create/cancel: pass;
- grouped object -> only current-group actions: pass;
- group creation still requires at least two eligible saved objects: pass;
- group creation remains pending until explicit label save: pass;
- save, image navigation, source-image reopen, and two-member restoration:
  pass;
- current EXE visual evidence for the ordinary, selection, and grouped states:
  pass.

## Verification

- isolated test build:
  `dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\`
  -> 0 warnings, 0 errors;
- focused switches:
  `--object-group-review`, `--object-metadata-review`,
  `--wpf-object-review-panel`, and `--wpf-labeling-shell` -> pass;
- current application build:
  `dotnet build .\OpenVisionLab.LabelingStudio.sln -c Debug -p:Platform="Any CPU" -m:1 -nr:false`
  -> 0 warnings, 0 errors;
- actual EXE replay:
  `--exe-dataset-wizard-smoke --verify-object-group` -> pass;
- default internal regression suite -> 260/260 pass, 0 failures, empty
  stderr;
- captured EXE SHA-256:
  `AFA592B0CC6C8DD5CD177233C77928968A549DC7AA8CC5AB02805D7AE53475B3`;
- full-duration before video: 72.8 seconds;
- full-duration after video: 66.53 seconds.

## Evidence

- before:
  `artifacts/ui/object-group-contextual-20260730/before/object-group-before.png`;
- before video:
  `artifacts/ui/object-group-contextual-20260730/before/object-group-before.mp4`;
- after:
  `artifacts/ui/object-group-contextual-20260730/after/object-group-after.png`;
- after video:
  `artifacts/ui/object-group-contextual-20260730/after/object-group-after.mp4`;
- ordinary -> selection -> grouped state sequence:
  `artifacts/ui/object-group-contextual-20260730/after/frames/group-panel-sequence.png`;
- before/after comparison:
  `artifacts/ui/object-group-contextual-20260730/object-group-before-after.png`.
- complete regression logs:
  `artifacts/verification/object-group-contextual-20260730`.

## Boundary / Next Dependency

This closes the concrete Object Review control-density gap reproduced during
the actual-EXE user audit. It does not reopen the completed same-image group
data contract.

No further unblocked labeling-editor code priority is established by this
evidence. Independent production evaluation first requires
operator-approved, provenance-confirmed, content-separated camera/session
train/validation/test data plus the intended runtime, weights, and hardware.
