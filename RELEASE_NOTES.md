# Release Notes

## Unreleased

- Saved Recipe and dataset state can now be exported as one validated portable
  project archive and imported into new, non-overwriting targets. Export is
  blocked while annotations, mask commits, AI candidates, or named work remain
  pending; import verifies the manifest, entry set, sizes, and SHA-256 hashes,
  rebases dataset-owned paths, and leaves Recipe activation as an explicit
  operator action. External model/runtime references are disclosed but are not
  embedded in the archive.
- A bounded current-image crash-recovery journal now offers explicit
  `편집 복구` or `초안 폐기` on the next launch. Restore returns box,
  segmentation, and persistent Object Review metadata as dirty in-memory state;
  it never confirms pending AI/Smart Mask candidates or saves label files.
  Journals are atomic, checksum-verified, context-validated, limited to seven
  days, and removed after explicit label save, discard, or normal close.
- Packaged startup now keeps logs, localization state, structured diagnostics,
  and explicit support ZIP files under a bounded current-user writable root.
  `설정/도구 -> 진단/지원` provides a model-free environment self-test and a
  one-action allow-list support export that excludes images, labels, weights,
  raw configuration, and credentials by default.
- Release engineering now pins .NET SDK `8.0.421` and product version `0.1.0`,
  emits a deterministic versioned self-contained `win-x64` folder, ships
  project and third-party notices, verifies every payload SHA-256
  fail-closed, and publishes the verified folder as a CI artifact.

- Object Review now supports persistent per-object `가림` and Recipe-defined
  tags for saved manual boxes and segmentation objects, with row badges,
  combined review filters, explicit reset paths, and a separate per-image
  sidecar written only through `라벨 저장`.
- Saved manual objects on one image can now form a persistent Object Review
  group with a dedicated selection preview, group badges/filtering, member
  removal/dissolve, and group-level `가림` or Recipe-tag actions. Schema-v2
  sidecars remain backward-compatible with v1 and write only on explicit
  label save.
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
