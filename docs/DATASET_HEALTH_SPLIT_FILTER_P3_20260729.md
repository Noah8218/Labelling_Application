# Dataset Health Visual QA Split Filter

Date: 2026-07-29 KST

Status: Complete

## Scope

Dataset Health Visual QA now narrows its existing read-only worklist by the
split values already carried by catalog items.

Included:

- `전체` plus only the existing `train`, `valid`, and `test` splits;
- composition with the existing `문제만` filter;
- visible filtered count and selected split in the status line;
- preservation of a still-valid split selection across refresh;
- safe fallback to `전체` when the selected split no longer exists;
- balanced healthy-sample selection so a large train split cannot hide
  existing valid/test splits from the bounded 48-item sample budget;
- routing edits to the existing labeling editor.

Excluded:

- class filtering before its operator-review prerequisite was reproduced;
- a second annotation editor or writable Dataset Health action;
- reviewer accounts, assignment, comments, consensus, or cloud workflow;
- changes to labels, masks, split ownership, training, inference, or model
  adoption.

## Ownership

- `WpfDatasetVisualQaService` retains problem-first catalog construction and
  now selects healthy samples round-robin across existing YOLO splits.
- `WpfDatasetHealthViewModel` owns available split values, selected split,
  `문제만` composition, visible count, refresh fallback, and selection.
- `WpfDatasetHealthWindow` remains a presentation-only binding surface.
- Existing annotation and split services remain unchanged.

## Acceptance Criteria

| Criterion | Result |
| --- | --- |
| Existing split values appear after `전체` in train/valid/test order | Pass |
| Selecting valid narrows a five-item fixture to its two valid items | Pass |
| Split and `문제만` filters compose | Pass |
| Refresh retains a still-valid split | Pass |
| Missing split falls back safely to `전체` | Pass |
| More than 48 train samples cannot hide existing valid/test splits | Pass |
| Filter and refresh leave every dataset file byte-identical | Pass |
| Editing still routes to the existing labeling editor | Pass |
| 1920x1080 and 1366x768 layouts remain readable | Pass |

## Verification

```text
isolated test build: pass, warning 0, error 0
current Debug app build: pass, warning 0, error 0
--dataset-health: pass
--wpf-dataset-health-window: pass
--wpf-labeling-shell: pass
--priority-workflow-docs: pass
git diff --check: pass
```

The focused data-tree check records every relative file and SHA-256 before
filtering, problem-only composition, and refresh, then proves the same file set
and bytes afterward.

Visual evidence:

- before, all five items without a split control:
  `artifacts/ui/dataset-health-split-filter-p3-20260729/before-visual-qa-all-1920x1080.png`;
- after, `전체` and five visible items at 1920x1080:
  `artifacts/ui/dataset-health-split-filter-p3-20260729/after-visual-qa-all-1920x1080.png`;
- after, `valid` and two visible items at 1366x768:
  `artifacts/ui/dataset-health-split-filter-p3-20260729/after-visual-qa-valid-1366x768.png`.

The visual tool used a disposable fixture copy. Read-only proof comes from the
focused full-tree hash test, not from the screenshot harness.

## Boundary And Next Dependency

The later real SEG `OK`/`NG` review record satisfied the class-filter
prerequisite. Canonical class filtering is now Complete in
`docs/DATASET_HEALTH_CLASS_FILTER_20260729.md`.

The historical next dependency, the four-point extreme-box geometry,
persistence, editing, export-loss, and backward-compatibility contract, is
Complete in `docs/FOUR_POINT_EXTREME_BOX_CONTRACT_20260729.md`. Its bounded
axis-aligned implementation is also Complete in
`docs/FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md`.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`
