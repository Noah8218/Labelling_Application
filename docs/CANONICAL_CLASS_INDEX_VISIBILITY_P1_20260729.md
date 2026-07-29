# Canonical Class Index Visibility P1

Date: 2026-07-29 KST

Status: `Complete`

## Scope

This slice makes the Recipe class order visible without adding class reorder
or schema migration behavior.

- Class Catalog preserves `CData.ClassNamedList` order and displays
  `0 · Scratch`, `1 · OK`, and so on.
- The selected-class summary and the always-visible next-label card include the
  same canonical YOLO index.
- Canvas class chips keep their existing `1~9` drawing-shortcut numbers.
  Their tooltip states both the shortcut and canonical index.
- Class Catalog explains that rename preserves an index while add/delete
  changes the schema.

The ordered Recipe contract used by `data.yaml`, YOLO label rows, training,
Batch AI name mapping, and reopen remains unchanged. Drag reorder and automatic
label migration are excluded.

## Ownership

- `WpfClassCatalogPanelViewModel` owns canonical-index list items, selected
  summary text, and the visible schema/shortcut explanation.
- `WpfCanvasPanelViewModel` owns the separate canonical index and drawing
  shortcut presentation for the active class.
- `WpfLabelingShellWindow.ClassCatalog` remains the UI/workflow adapter and now
  passes Recipe order through without alphabetic sorting.
- Existing `CData`, YAML, annotation, training, and Batch AI services retain
  persistence and mapping ownership.

## Acceptance Evidence

| Criterion | Result |
| --- | --- |
| Add classes in `Scratch, OK, Crack` order | Catalog and canvas preserve `0, 1, 2` |
| Rename `OK` to `Pass` | Existing object and canonical index `1` are preserved |
| YOLO label creation after rename | Saved line begins with class index `1` |
| `data.yaml` after rename | Names remain `Scratch, Pass, Crack` |
| Recipe save/reopen | `1 · Pass` is restored |
| Delete `Scratch`, save, reopen | Visible schema shifts to `0 · Pass`, `1 · Crack` |
| Drawing shortcuts | `1~9` select the corresponding canonical-order item without reordering |
| Batch AI mapping regression | Explicit non-destructive preflight gate passes |
| 1920x1080 and 1366x768 | Catalog contract, indexed rows, shortcuts, and next-label card are visible without clipping |
| Current Debug EXE | Restored Recipe shows matching `0 · Defect` catalog/selected/next-label context |

Deletion is intentionally recorded as a schema change: later classes receive
new indices. This slice does not claim to migrate old external label files
after a destructive class deletion.

## Verification

- isolated Debug test build: warning `0`, error `0`;
- `--canonical-class-index`: pass;
- `--wpf-class-catalog-panel`: pass;
- `--wpf-canvas-panel-commands`: pass;
- `--labeling-productivity`: pass;
- `--wpf-batch-detection-preflight`: pass;
- `--exe-canonical-class-index-visual`: pass;
- fresh current-build before/after visual review at 1920x1080 and 1366x768:
  pass;
- `--priority-workflow-docs`: pass;
- `git diff --check`: pass.

## Evidence

- before:
  `artifacts/canonical-class-index-p1-20260729/before/`;
- final after:
  `artifacts/canonical-class-index-p1-20260729/after/class-catalog-after-1920x1080-final.png`;
  `artifacts/canonical-class-index-p1-20260729/after/class-catalog-after-1366x768-final.png`;
- actual Debug EXE:
  `artifacts/canonical-class-index-p1-20260729/actual-exe/canonical-class-index-exe.png`.

## Boundary / Next Dependency

This completion proves presentation and local ordered-contract consistency. It
does not add class reorder, destructive schema migration, multi-user schema
history, or production model accuracy.

The next ready priority is operator documentation truth synchronization for
Recipe-scoped Smart Mask automatic contour, correction, candidate restore,
explicit confirmation, and separate file save.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`
