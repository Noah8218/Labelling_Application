# Smart Mask Operator Documentation Truth

Date: 2026-07-29 KST

Status: Complete

## Scope

The current Smart Mask operator contract is now stated consistently in:

- `README.md`;
- `docs/tutorial/README.md`;
- `docs/MOBILE_SAM_SMART_MASK.md`;
- the visible F1 labeling help owned by
  `WpfAnnotationProductivityService.ShortcutHelpText`.

The approved public Smart Mask GIF was not replaced.

## Current Operator Contract

1. In a segmentation Recipe, enable
   `라벨링 옵션 · 자동 윤곽` once.
2. Complete a Rectangle around one object to start the MobileSAM candidate
   automatically.
3. Review the candidate before any approval.
4. If correction is needed, add one positive or negative point at a time and
   run `후보 다시 생성`.
5. Compare `이전 후보 보기` and `현재 후보 보기`; switching candidates does
   not write a label.
6. Confirm the displayed candidate or skip it.
7. Confirm invokes the existing canonical annotation-save path. Skip does not
   write the candidate. Manual edits after confirmation require another
   explicit `라벨 저장`.
8. Restoring the Recipe option makes it visible and editable but does not
   select a tool, draw a box, run inference, confirm, skip, or save.

Regular box mode remains available by turning automatic contour off. The
manual active-session action remains `후보 다시 생성`; it is not the normal
first-object start action.

## Acceptance Criteria

| Criterion | Result |
| --- | --- |
| README, tutorial, MobileSAM guide, and F1 help agree | Pass |
| Auto-first Recipe-scoped workflow is the documented normal path | Pass |
| Point correction and previous/current comparison are documented | Pass |
| Generation/restore versus Confirm/Skip save behavior is explicit | Pass |
| Option restoration has no inference or persistence side effect | Pass |
| Approved public GIF remains unchanged | Pass |
| Public documents contain no private machine path | Pass |
| Current Debug EXE F1 wording is visible without clipping at 1920x1080 | Pass |

## Verification

```text
isolated test build: pass, warning 0, error 0
current Debug app build: pass, warning 0, error 0
--labeling-productivity: pass
--mobile-sam-box-prompt: pass
--smart-mask-candidate-compare-restore: pass
--wpf-canvas-panel-commands: pass
--priority-workflow-docs: pass
--exe-labeling-productivity-smoke: pass
git diff --check: pass
```

Visual evidence:

- before:
  `artifacts/ui/smart-mask-operator-doc-truth-20260729/before-f1-help-1920x1080.png`;
- after:
  `artifacts/ui/smart-mask-operator-doc-truth-20260729/after-f1-help-1920x1080.png`.

## Boundary And Next Dependency

This synchronizes operator guidance with existing behavior. It does not change
MobileSAM inference, candidate confirmation, annotation persistence, field
accuracy, or commercial-platform scope.

The historical next priority, a bounded Dataset Health Visual QA split filter,
is Complete in `docs/DATASET_HEALTH_SPLIT_FILTER_P3_20260729.md`. Class
filtering remains observation-gated. The bounded axis-aligned four-point
extreme-box implementation is now Complete in
`docs/FOUR_POINT_EXTREME_BOX_IMPLEMENTATION_20260729.md`.

Recommended model: `gpt-5.6-terra`

Reasoning effort: `low`
