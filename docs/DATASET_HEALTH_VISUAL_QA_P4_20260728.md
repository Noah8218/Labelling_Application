# Dataset Health Visual QA P4

Date: 2026-07-28 KST

Status: `Complete`

## Outcome

Dataset Health now has a fourth, read-only `시각 QA` tab. It discovers image-level
saved-label problems across the current Recipe dataset, places problem rows
before bounded healthy samples, renders only the selected image as a saved-label
overlay, and explicitly returns the operator to the existing labeling editor.

This is a dataset analysis surface. It is not a second editor or another image
queue.

## Included Scope

- object-detection label missing, malformed-line, empty-label, and valid-sample
  classification;
- segmentation canonical segment JSON missing/corrupt/invalid geometry and
  valid-sample classification;
- anomaly image review state with unreviewed images first;
- `문제만` filtering;
- problem-first catalog ordering, at most 500 rows, and at most 48 healthy
  samples;
- selected-image-only bitmap decode at a maximum preview width of 800 pixels;
- saved detection box and segmentation polygon/raster-boundary overlay;
- explicit `편집기에서 열기` route through the existing labeling workbench;
- current-build 1920x1080 and 1366x768 visual evidence.

## Excluded Scope

- editing, approval, rejection, or autosave inside Dataset Health;
- a second image queue, task assignment, comments, or review history;
- full-resolution preload or thumbnails for every dataset row;
- automatic model execution;
- video propagation, collaboration, account/cloud, camera, PLC, or deployment
  scope.

## Ownership

- `WpfDatasetVisualQaService`: read-only catalog classification and selected
  preview composition;
- `WpfDatasetVisualQaItem` / `WpfDatasetVisualQaCatalog`: lazy preview and
  presentation contracts;
- `WpfDatasetHealthViewModel`: filtering, selection, status, and editor-route
  command;
- `WpfDatasetHealthWindow`: analysis layout only;
- `WpfLabelingShellWindow.DatasetHealth`: existing-editor navigation adapter;
- `YoloImageLabelStatusService`,
  `YoloSegmentationAnnotationService`,
  `RasterMaskPolygonService`, and
  `AnomalyImageReviewStatusService`: canonical annotation/review truth.

Dataset Health does not own annotation mutation or persistence.

## Safety And Performance Contract

- Catalog construction reads paths, image dimensions, annotation files, and
  anomaly review metadata only.
- Opening Dataset Health on its default overview does not build the visual-QA
  catalog. The existing window selection adapter starts it only when the
  operator opens `시각 QA`; refresh rebuilds it only after that tab has been
  used.
- The worklist contains text metadata, not one bitmap per row.
- `PreviewSource` invokes its factory once, on first access for the selected
  item.
- The preview uses an owned WPF drawing and never changes the source image,
  annotation file, Recipe, history, dirty state, or training input.
- Opening the editor closes the analysis window and uses the existing
  `TryLoadImage` and labeling-workbench path.
- Missing/corrupt annotations are surfaced; they are not repaired
  automatically.

## Verification

- isolated Debug test build: warning 0, error 0;
- `--dataset-health`: detection, segmentation, anomaly, missing/corrupt
  annotation priority, lazy preview, and source SHA-256 invariance;
- `--wpf-dataset-health-window`: fourth tab, filtering, preview, existing-editor
  route, and source SHA-256 invariance;
- `--priority-workflow-docs`;
- `git diff --check`;
- current-build captures:
  - `artifacts\ui\dataset-health-visual-qa-p4-20260728\before-current-build-1920x1080.png`;
  - `artifacts\ui\dataset-health-visual-qa-p4-20260728\before-current-build-1366x768.png`;
  - `artifacts\ui\dataset-health-visual-qa-p4-20260728\after-current-build-1920x1080.png`;
  - `artifacts\ui\dataset-health-visual-qa-p4-20260728\after-current-build-1366x768.png`.

## Evaluation Boundary

P4 closes the bounded dataset-level visual QA gap identified in the commercial
video review. It does not establish CVAT/V7 parity: persistent object metadata,
video propagation, collaborative review, and enterprise governance remain
absent or intentionally out of scope. The labeling-editor depth remains
`3.4/5`, and focused local-workstation maturity remains `4.0/5`; this slice
improves review discoverability without adding model-quality evidence.

## Completion Record

Status: Complete

Scope: read-only problem-first dataset visual QA with selected-image overlay and
existing-editor navigation.

Acceptance criteria:

- problem rows are classified and ordered before bounded healthy samples:
  pass;
- the visual catalog is deferred until its tab is selected and list creation
  does not preload all image pixels: pass;
- selected detection/segmentation preview uses saved geometry: pass;
- filtering and existing-editor navigation work without source mutation: pass;
- 1920x1080 and 1366x768 current-build layouts are usable: pass.

Verification: isolated build, `--dataset-health`,
`--wpf-dataset-health-window`, `--priority-workflow-docs`,
`git diff --check`, and four current-task screenshots.

Evidence: this document, focused tests, and
`artifacts\ui\dataset-health-visual-qa-p4-20260728`.

Boundary / next dependency: P5 interchange/batch preflight is next. It must
reuse implemented import/export and batch services, expose dry-run before
Apply/Start, and preserve explicit execution plus Candidate Review/no-autosave.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `medium`
