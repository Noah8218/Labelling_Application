# Dataset Health Visual QA Class Filter

Date: 2026-07-29 KST

Status: Complete

## Reproduced Operator Need

The prerequisite for class filtering is satisfied by the recorded real
segmentation review in `docs/LABELING_STUDIO_COMPLETENESS_AUDIT.md`:

- the actual 125-image Recipe contained 120 `OK` records and 14 `NG` records;
- the operator had to decide and review the two classes separately;
- the existing split and `문제만` filters could not isolate `OK` or `NG`;
- the approved 124-target correction, retraining, and comparison are already
  complete and are not reopened by this feature.

The current Recipe inventory still contains the same two-class train/valid
dataset. A current-source read-only replay therefore provides a concrete,
non-synthetic class-filter workflow without inventing a new labeling task.

## Operator Outcome

Dataset Health Visual QA now exposes:

- `전체`;
- canonical Recipe classes in `index · name` order;
- the existing `train` / `valid` / `test` split filter;
- the existing `문제만` filter.

Selecting a class rebuilds the read-only catalog against all stored images and
keeps up to 500 matching rows. This differs intentionally from the unfiltered
view, which preserves the existing problem-first plus 48 healthy-sample
policy. Split and problem filters then compose with the class-scoped catalog.

The real 125-image replay selected `1 · NG` and reduced the worklist to the 14
images that contain an NG object. The status line reports total scanned,
class-matched, problem, catalog, and visible counts.

## Ownership And Safety

- `WpfDatasetVisualQaService` reads canonical detection label indexes or
  segmentation JSON class indexes and builds an optional class-scoped catalog.
- `WpfDatasetHealthViewModel` owns canonical class options, selection,
  refresh preservation/fallback, and class/split/problem composition.
- `WpfDatasetHealthWindow` remains a presentation-only surface.
- `WpfDatasetVisualQaItem` carries distinct canonical class indexes for
  filtering; it does not add label metadata or change the annotation schema.

The filter does not write labels, masks, segment JSON, split ownership,
Recipe configuration, training state, inference state, or model-adoption
records. `편집기에서 열기` remains the only route to annotation changes.

## Acceptance Criteria

| Criterion | Result |
| --- | --- |
| `전체` plus canonical `index · name` classes | Pass |
| Detection labels filter by canonical class index | Pass |
| Segmentation JSON filters by canonical class index | Pass |
| Class, split, and `문제만` compose | Pass |
| Valid class and split survive refresh | Pass |
| Removed or renamed class falls back to `전체` | Pass |
| Unfiltered 48-item balanced sampling remains unchanged | Pass |
| Class-scoped catalog is bounded at 500 rows | Pass |
| Filtering and refresh leave every dataset file byte-identical | Pass |
| Real 125-image Recipe narrows `NG` to 14 images | Pass |
| 1920x1080 and 1366x768 current-build layouts remain readable | Pass |

## Verification And Evidence

Verification:

- required isolated test build: warning 0, error 0;
- `--dataset-health`: pass;
- `--wpf-dataset-health-window`: pass;
- `--wpf-labeling-shell`: pass;
- `--priority-workflow-docs`: pass;
- `--four-point-extreme-box`: pass;
- `git diff --check`: pass;
- current-build real-Recipe visual replay: pass.

Evidence:

- `tests\LabelingApplication.Tests\Program.DatasetHealth.cs`;
- `artifacts\ui\dataset-health-class-filter-20260729\before-class-filter-1920x1080.png`;
- `artifacts\ui\dataset-health-class-filter-20260729\after-class-ng-1920x1080.png`;
- `artifacts\ui\dataset-health-class-filter-20260729\after-class-ng-1366x768.png`.

The before image is a true current-source capture taken before implementation.
The after images use the same 125-image Recipe and select `1 · NG`.

## Completion Record

Status: Complete

Scope: read-only canonical class filtering for Detection and Segmentation
Visual QA, composed with the existing split and problem filters.

Acceptance criteria: the table above passes.

Verification: the commands and artifacts above, plus final protected
regressions and `git diff --check`.

Evidence: this record, focused tests, and current-build captures.

Boundary / next dependency: this is a sampled/read-only dataset review surface,
not a second annotation editor, per-object issue system, reviewer workflow, or
cloud assignment feature. Persistent object metadata remains blocked until a
named Recipe/export/training/review consumer exists.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`
