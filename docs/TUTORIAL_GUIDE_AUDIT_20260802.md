# Tutorial Guide Currency And Followability Audit

Date: 2026-08-02 KST

Status: Incomplete

## Scope

This audit covers the operator-facing tutorial surfaces:

- `docs/tutorial/README.md`;
- `docs/tutorial/labeling-workbench-tutorial.html`;
- `docs/tutorial/labeling-workbench-tutorial-standalone.html`;
- screenshots and the Smart Mask GIF under `docs/tutorial/images`;
- the root README links that route first-time users to the tutorial.

The audit asks whether a first-time operator can choose the correct workflow,
complete the task, recognize success, and recover from a common failure without
reading engineering history documents.

## External Manual Baseline

The following official documentation was reviewed:

- [Label Studio project setup](https://labelstud.io/guide/setup.html) and
  [step-by-step labeling](https://labelstud.io/learn/getting-started-with-label-studio/labeling-your-data/):
  project-context setup, short numbered tasks, warnings, and explicit submit
  semantics;
- [CVAT manual annotation](https://docs.cvat.ai/docs/annotation/manual-annotation/),
  [single-shape mode](https://docs.cvat.ai/docs/manual/advanced/single-shape/),
  and [annotation navigation](https://docs.cvat.ai/docs/annotation/annotation-editor/navbar/):
  tool-oriented procedures, control references, save behavior, and shortcuts;
- [V7 Auto-Annotate](https://docs.v7labs.com/docs/auto-annotate): rough-box
  prompting, correction actions, warnings, and the explicit save boundary;
- [Supervisely getting started](https://docs.supervisely.com/getting-started/how-to-annotate)
  and [image labeling toolbox](https://docs.supervisely.com/labeling/labeling-toolbox/images):
  short onboarding followed by screen-region and tool reference pages.

The selected local documentation pattern is:

`quick start -> screen orientation -> task recipes -> success signal ->
failure recovery -> reference/checklist`.

## Pre-Update Findings

| Finding | Impact |
| --- | --- |
| The core detection, segmentation, anomaly-classification, training, and candidate-review path was documented | A first-time operator could complete basic labeling |
| PatchCore and explicit heatmap review were absent | The newest anomaly path was undiscoverable |
| Dataset Health split/class/problem filtering was absent | Operators could not learn the current read-only QA workflow |
| Four-point extreme box input was absent | A completed detection input option was hidden from the manual |
| Object metadata and same-image grouping were absent | The current review workflow was incomplete |
| Portable archive, crash recovery, diagnostics, and support export were absent | Recovery and product-support procedures required engineering documents |
| The HTML guide used a linear screenshot walkthrough without a sticky task index | Long-page navigation was slower than current commercial guides |
| Many steps described actions but not a success signal or recovery path | Operators could not always distinguish completion from an intermediate state |

## Updated Information Architecture

The Markdown manual now owns the detailed operator reference. The HTML guide
is the web-style visual path, and the standalone HTML is the portable copy of
that visual guide.

Each major task now supplies the smallest applicable set of:

- goal and prerequisite;
- menu location;
- numbered actions;
- screenshot or animation;
- success signal;
- save/confirm side-effect boundary;
- common failure and recovery;
- detailed contract link when deeper behavior matters.

## Feature Coverage

| Product area | Procedure | Current visual | Success/recovery |
| --- | --- | --- | --- |
| Dataset and Recipe setup | Pass | Pass | Pass |
| Image queue and Worklist | Pass | Shared current overview | Pass |
| Canonical class index | Pass | Shared setup/current overview | Pass |
| Detection two-point box | Pass | Pass | Pass |
| Four-point extreme box | Pass | Current-source 2026-08-02 | Pass |
| Segmentation polygon/brush/eraser | Pass | Pass | Pass |
| Smart Mask auto contour and correction | Pass | Actual-EXE GIF | Pass |
| YOLO anomaly classification | Pass | Pass | Pass |
| PatchCore normal-only and heatmap review | Pass | Current-source 2026-08-02 | Pass |
| Saved label, AI candidate, and quality status | Pass | Pass | Pass |
| Object occluded/tag/group review | Pass | Current-source 2026-08-02 | Pass |
| Dataset Health filters | Pass | Verified 2026-07-29 data view | Pass |
| Template, batch, and interchange | Pass | Pass | Pass |
| Training, runtime profile, comparison, adoption | Pass | Pass | Pass |
| Portable project archive | Pass | Current-source shared tools view | Pass |
| Bounded crash recovery | Pass | Current-source 2026-08-02 | Pass |
| Runtime diagnostics and support export | Pass | Current-source 2026-08-02 | Pass |
| Dark-only theme and layout auto-fit | Pass | Current-source overview | Pass |

## Screenshot Currency

The current-source screenshots were generated after the required isolated test
build and inspected at original resolution. The visual smoke dynamically used
the active leftmost monitor and recorded the selected monitor and window
bounds for every current capture.

New tracked images are under:

`docs/tutorial/images/features-20260802/`

They cover the current overview, Object Review metadata/groups, PatchCore
candidate and heatmap, four-point box input, archive/diagnostics, and crash
recovery. Dataset Health uses the most recent meaningful verified data view
instead of a current empty-state capture. Older screenshots remain only where
their documented task and visible contract are still valid.

## Followability Review

The guide passes the following desk-test questions:

1. Can a new operator create and save one box without reading another file?
2. Can the operator tell image input from label output?
3. Can the operator distinguish saved labels from AI candidates?
4. Does every advanced AI path retain explicit Confirm/Skip or review semantics?
5. Can the operator find the four-point and Smart Mask options and understand
   what is persisted?
6. Can the operator understand that PatchCore heatmaps are review-only?
7. Can the operator find data QA, archive, crash recovery, and support actions?
8. Does each major failure name a concrete first recovery action?
9. Does the visual guide remain usable at desktop and narrow widths?
10. Does the portable HTML contain every local image rather than broken paths?

## Verification

Passed:

- required isolated test build: warning 0, error 0;
- eight current/recent verified feature screenshots inspected at original
  resolution;
- every current capture placed on the dynamically selected leftmost monitor,
  `\\.\DISPLAY2` at `Left=-1920`;
- focused `--priority-workflow-docs` regression;
- documentation information architecture: 107/107 Markdown files classified,
  broken local links 0, duplicate classifications 0;
- static guide check: 25 Markdown links, 13 HTML task sections, 16 linked
  images, 8 current feature images, and 16 matching embedded standalone
  images, with errors 0;
- standalone image payload SHA-256 comparisons against all linked source
  assets;
- `git diff --check`.

Not run:

- final raster capture of the local HTML page. The available browser surface
  rejected the repository `file:` URL under its local-file security policy,
  including when the operator opened the exact local page first. No alternate
  browser or local-server workaround was used.

## Completion Record

Status: Incomplete

Scope: Current operator tutorial content, visual web guide, portable standalone
guide, current feature screenshots, and durable followability audit.

Acceptance criteria: Every current workstation feature group has a discoverable
task explanation (pass); key workflows have current or still-valid visual
evidence (pass); steps expose success, explicit save/confirm, and recovery
semantics (pass); standalone images are embedded (pass); repository
documentation gates pass (pass); final rendered desktop and narrow-width HTML
visual inspection (not run).

Verification: The passed commands and counts are listed above. The rendered
HTML visual inspection remains unavailable under the current browser policy.

Evidence: This document, `docs/tutorial`, and the focused verification output.

Boundary / next dependency: To close the remaining visual criterion, provide
one desktop-width and one narrow-width screenshot of the already-open local
HTML page. The review must inspect the sticky table of contents, image scaling,
text clipping, horizontal overflow, and callout contrast. This audit does not
claim production model accuracy, commercial platform parity, installation
readiness, or external GPU-target labeling completion.
