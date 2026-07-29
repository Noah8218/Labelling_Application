# Current Worktree Integration Verification

Date: 2026-07-29 KST

Status: Complete

## Scope

This verification closes the current uncommitted integration slice containing:

- main-window safe close;
- canonical class-index presentation;
- Smart Mask operator-documentation truth;
- Dataset Health split and canonical class filtering;
- four-point extreme-box input and rotated-import rejection.

It does not add a new labeling feature, change model quality, run external
training, or authorize commit/push.

## Defects Found And Corrected

### Never-loaded shell cleanup

The safe-close policy correctly protected an operator-visible main window, but
test windows that had never entered a loaded operator session could open the
same modal during `finally` cleanup. This stopped the default suite after 59
passes.

`WpfLabelingShellWindow.OnClosing` now treats a never-loaded shell as
non-operator cleanup. A loaded main window still uses the existing
save/discard/cancel contract. The anomaly and Template Auto Label tests also
make their cleanup intent explicit.

### 100,000-row object-review edit

The default suite then exposed a deterministic performance failure after 177
passes. Replacing or deleting one manual ROI caused `RefreshActionState` to
scan the 100,000-row list twice for segmentation counts even though no
segmentation row changed.

`WpfObjectReviewPanelViewModel` now preserves segmentation-derived action state
when a non-segmentation row is replaced or removed. Segment insertion,
replacement, removal, selection, merge, z-order, remove-underlying, and session
state paths retain their full refresh behavior.

## Acceptance Criteria

| Criterion | Result |
| --- | --- |
| Required isolated build | Pass, warning 0 / error 0 |
| Current Debug application build | Pass, warning 0 / error 0 |
| Completed-slice focused switches | Pass, 19 / 19 |
| Final combined focused regression rerun | Pass, 26 / 26 |
| Default internal regression suite | Pass, 258 / 258 |
| Default-suite stderr | Empty |
| Never-loaded dirty shell cleanup | Pass |
| Loaded safe-close policy regression | Pass |
| 100,000-row settings/ViewModel performance | Pass, three consecutive focused runs |
| Segmentation aggregate-state regressions | Pass |

## Verification

- `dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\`
- `dotnet build .\OpenVisionLab.LabelingStudio.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false`
- default `LabelingApplication.Tests.dll`: 258 PASS, 0 FAIL;
- 19 focused switches covering safe close, class index, Smart Mask, Dataset
  Health, WPF shell, four-point input, history/ROI, and Label Studio/CVAT
  detection interchange;
- final combined focused rerun: 26/26, including the 19 completed-slice gates
  plus settings performance, anomaly, Template Auto Label, segmentation
  aggregate-state, and object session-state checks;
- `--wpf-settings-viewmodels`: three consecutive passes;
- `--segmentation-merge`, `--segmentation-zorder`,
  `--segmentation-remove-underlying`, `--object-session-state`: pass;
- `--wpf-anomaly-purpose-flow`,
  `--wpf-template-current-image-no-candidate`, `--application-close`: pass;
- `--priority-workflow-docs`: pass;
- `git diff --check`: pass.

Evidence:

- `artifacts\integration-verification-20260729\default-suite-complete.stdout.log`;
- `artifacts\integration-verification-20260729\default-suite-complete.stderr.log`;
- focused test output from the current source;
- the completion records for each included slice.

The successful default suite ran from 12:29:31 to 12:34:05 KST. Earlier
timeout and failure logs remain diagnostic evidence and are not presented as
successful runs.

## Completion Record

Status: Complete

Scope: current-worktree build, focused integration, default internal
regression, and the two defects discovered by that verification.

Acceptance criteria: all criteria in the table pass.

Verification: commands and logs listed above.

Evidence: this document and
`artifacts\integration-verification-20260729`.

Boundary / next dependency: this proves current-source internal integration,
not independent field-model accuracy or commercial-platform parity. Persistent
object metadata still requires a named Recipe/export/training/review consumer;
cross-family renderer changes require a reproduced visual-order defect; field
adoption requires approved independent data.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`
