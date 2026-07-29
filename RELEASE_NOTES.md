# Release Notes

## Unreleased

- Integration hardening: never-loaded startup/test shells no longer open an
  operator close dialog, while loaded main windows retain safe close.
  Non-segmentation object-review row replacement/removal also preserves
  unchanged segmentation aggregate state instead of rescanning very large
  lists.

Current focus:

- Local industrial labeling workflow for object detection and segmentation.
- Independent object-detection test evidence for YOLOv5/YOLOv8 accuracy and model-Takt comparison.
- Independent production/cross-session anomaly-classification runtime evidence.
- YOLOv8 segmentation data/model operating quality.
- Clear separation between saved labels, AI candidates, trained model candidates, and the current inspection model.
- Compact WPF workflow layout for dataset, labeling, candidate review, training, and model center screens.

Recent verified areas:

- Dataset Health Visual QA canonical class filter: `전체` plus Recipe-ordered
  `index · name` classes compose with split and `문제만`; selecting a class
  rebuilds a read-only catalog bounded at 500 matching images while the
  unfiltered view keeps its existing 48-item balanced sample policy.
- Dataset Health Visual QA existing-data split filter: `전체` plus only actual
  train/valid/test values compose with `문제만`, refresh safely preserves or
  resets the selection, and balanced healthy sampling prevents a large train
  split from hiding valid/test within the bounded catalog.
- Recipe-scoped segmentation `자동 윤곽` mode: enable it once, then each new
  rectangle starts a Smart Mask candidate automatically. Candidate approval
  remains explicit; `확정` saves the displayed candidate through the canonical
  label-save path, while generation, comparison, restore, and skip do not
  write that candidate.
- Smart Mask operator guidance in README, tutorial, MobileSAM guide, and F1
  help now uses the same auto-first correction, previous/current comparison,
  explicit confirm/skip, and Recipe restore contract.
- Canvas layout auto-fit after side-panel collapse, expansion, or other
  viewport-size changes; ordinary operator zoom and pan remain unchanged.
- Local YOLOv5/YOLOv8 Detect comparison with separate runtimes, test-preferred/validation-reference split handling, and Candidate Review metrics/Takt presentation.
- Dataset-purpose-aware YOLOv8 Detect/SEG/Classification weight selection when connecting a local runtime folder.
- Segmentation brush/polygon save, reopen, and training-export paths.
- YOLOv8 segmentation local runtime plumbing and model-comparison safeguards.
- Image queue usability and save-before-navigation protection.
- Candidate Review wording and rejected-model adoption guard.
- README, release-note, CI, and known-limitations documentation skeleton.
- `Library-Noah` source-project dependency removed from the app/test build path in favor of checked-in DLL references.

Not a release claim:

- Automatic contour is an assisted-labeling workflow, not automatic candidate
  approval or model-accuracy evidence.
- The current object-detection comparison uses validation with one NG object because the test split is empty; it is not model-adoption evidence.
- Production YOLOv8 segmentation accuracy still requires held-out evaluation on real labeled datasets.
- Anomaly detection remains an active workflow area, not a completed product mode.
- Updating the checked-in `Lib.*` DLLs still needs an intentional binary refresh and build verification.
