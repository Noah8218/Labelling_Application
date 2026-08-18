# Segmentation Recipe UX, Accessibility, And Model-State Evidence

Date: 2026-08-19 KST

## Outcome

Status: `Complete` for the three approved UI changes and the user-approved
bounded revalidation scope.

The change closes three operator-facing gaps found by the 2026-08-18 Recipe
UX audit:

1. Segmentation tools now expose stable automation IDs, accessible names,
   help text, keyboard focus, and a visible current-tool state.
2. New Recipe setup keeps purpose, starting data, image folder, initial model,
   optional weights, anomaly mapping, Recipe name, storage, and classes in one
   setup surface. Creating the Recipe remains an explicit action.
3. The model screen separates the currently applied inspection model from the
   unsaved editing draft. Long image/model names use compact display text while
   their full value remains available to tooltips and accessibility clients.

This does not claim five completed post-change segmentation runs. The user
explicitly accepted the available validation after two complete runs and one
partial third run, so runs four and five were not started.

## UX Assessment

Bounded score for the changed slice: `86/100`.

| Dimension | Score | Evidence-based assessment |
| --- | ---: | --- |
| Recognition and accessibility | 23/25 | Tool name, tooltip, stable automation ID, focus border, selected icon, and visible `현재 도구` text identify the brush/eraser/polygon state without relying on icon shape alone. |
| Reachability and action count | 22/25 | First Recipe configuration is one setup surface and one explicit create/apply action. Operators no longer need a separate initial model-configuration trip for the common path. |
| Information hierarchy and readability | 21/25 | Applied model, editing draft, and advanced runtime details are separated; long names are compacted with full-value disclosure. The long setup surface still requires vertical scrolling on the 1080p test monitor. |
| Feedback and safety | 20/25 | Draft changes state that inspection still uses the applied model, and reset loads defaults into the editor without applying them. The third automation run failed to reselect Brush during split labeling, so repeated focus/selection reliability is not claimed beyond the successful runs and focused tests. |

The score is a scoped workstation UX assessment, not a production-readiness,
field-accuracy, or commercial-platform-parity score.

## Acceptance Criteria And Evidence

| Criterion | Result | Evidence |
| --- | --- | --- |
| Icon-only segmentation tools have name, tooltip/help, and stable identity | Pass | `CanvasAnnotationToolItemStyle`, `WpfAnnotationToolItem`, `--wpf-canvas-panel-commands`, and two complete actual-EXE runs |
| Selected segmentation tool is visible and keyboard focus is distinguishable | Pass | `CanvasSelectedToolChip`, item focus trigger, focused XAML contract, and brush screenshots |
| Initial Recipe settings are consolidated without implicit execution | Pass | `WpfDatasetSetupWizardWindow`, request/execution services, `--wpf-dataset-setup-ui`, and `--wpf-dataset-setup-request` |
| Applied and editing models remain distinct until explicit save/apply | Pass | applied snapshot and draft-dirty state in `WpfYoloModelSettingsPanelViewModel`; save, cancel, and draft-default commands; `--wpf-yolo-model-settings-panel` |
| Long image/model names remain readable without losing the full value | Pass | compact image queue display, full tooltip/accessibility value, model tooltips, and `--wpf-image-queue-status` |
| Wide/Compact layouts remain valid | Pass | `--wpf-workspace-layout` and `--wpf-responsive-layout` |
| Existing protected behavior remains intact | Pass | final zero-warning/error build and the no-argument protected suite, exit code 0 |

## Actual EXE Revalidation

All desktop runs routed `TEMP` and `TMP` to the D-drive test root and used the
required dynamic leftmost-monitor placement. The selected monitor was
`\\.\DISPLAY2`, bounds `Left=-1920, Top=365, Width=1920, Height=1080`; the
window rectangle was placed within those bounds.

### Complete run 1

- Recipe: `circular_seg_exe_20260818_201232`
- Train/validation/test segmentation labels: `4/2/2`
- Training: one epoch completed
- Result: trained `best.pt` applied, inference opened `AI 후보 검토`, two
  candidates returned
- Summary: `artifacts\ux-priorities-20260818\segmentation-recipe-01b\summary.txt`

### Complete run 2

- Recipe: `circular_seg_exe_20260818_202051`
- Train/validation/test segmentation labels: `4/2/2`
- Training: one epoch completed
- Result: trained `best.pt` applied, inference opened `AI 후보 검토`, three
  candidates returned
- Summary: `artifacts\ux-priorities-20260818\segmentation-recipe-02\summary.txt`

### Partial run 3

- Recipe: `circular_seg_exe_20260818_202921`
- Passed Recipe creation, initial model setup, image-root loading, first label
  set, training settings, split setup, and the first split label save.
- The automation then failed because Brush was not selected when it attempted
  to reselect the tool during later split labeling. The captured UI showed Box
  as the current tool.
- This run did not reach training or inference. The result is retained as a
  bounded unresolved automation/focus observation, not reported as a product
  pass or a confirmed product defect.
- Evidence:
  `artifacts\ux-priorities-20260818\segmentation-recipe-03\screenshots\failure.latest.png`

## Visual Comparison

- Closest pre-change baseline:
  `artifacts\ux-recipes-20260818\segmentation\run-01\screenshots\failure.latest.png`
- Consolidated Recipe setup:
  `artifacts\ux-priorities-20260818\segmentation-recipe-01b\screenshots\02_segmentation_recipe_wizard.png`
- Visible Brush selection and mask feedback:
  `artifacts\ux-priorities-20260818\segmentation-recipe-01b\screenshots\06_first_brush_strokes.png`
- Applied model after training:
  `artifacts\ux-priorities-20260818\segmentation-recipe-01b\screenshots\11_trained_model_applied.png`

Visual review found no new light-theme leak, clipped primary action, or
unreadable entered text in the captured dark UI. The setup dialog is a single
coherent surface but is scrollable; it is not claimed that every field fits
simultaneously above the fold.

## Verification

- `Build-LabelingApplicationTests.ps1 -OutputName ux-priorities-final2-20260819`
  -> 0 warnings, 0 errors.
- Focused passes:
  `--wpf-dataset-setup-ui`, `--wpf-dataset-setup-request`,
  `--wpf-yolo-model-settings-panel`, `--wpf-image-queue-status`,
  `--wpf-learning-workflow-panel`, `--wpf-labeling-shell`,
  `--wpf-workspace-layout`, `--wpf-responsive-layout`, and
  `--wpf-canvas-panel-commands`.
- No-argument protected suite -> exit code 0.
- Documentation information-architecture and priority-workflow-doc gates ->
  pass. `git diff --check` is the final handoff gate.

## Durable Closure

```text
Status: Complete
Scope: Segmentation tool accessibility/selection visibility, one-surface initial Recipe setup, applied-versus-editing model state, and compact image/model display.
Acceptance criteria: Every criterion in this record passed for the approved implementation scope.
Verification: Zero-warning/error final build; nine focused WPF gates; protected no-argument suite exit code 0; two complete actual-EXE segmentation flows plus one retained partial run.
Evidence: docs/SEGMENTATION_RECIPE_UX_ACCESSIBILITY_20260819.md and the listed actual-EXE screenshots/summaries.
Boundary / next dependency: Five post-change segmentation completions are not claimed. Run 3 stopped when automation failed to reselect Brush; the user accepted the current validation amount and runs 4/5 were not started. External P0-C still requires direct GPU-capable clean-target access.
```
