# Smart Mask Real Correction Effectiveness

Date: 2026-07-28
Status: Complete
Field validation: Not evaluated
Production accuracy claimed: No

## 1. Outcome

MobileSAM positive/negative correction was replayed on six fixed non-empty
KolektorSDD defect samples. Two samples were fixed as development evidence and
four different samples were fixed as held-out replay.

The evaluator used the same rule on every sample:

1. build one rough box from the non-empty source-mask bounds with fixed padding;
2. preserve the poor box-only candidate;
3. select one dense false-negative interior as a positive point when present;
4. select one dense false-positive interior as a negative point when present;
5. run positive-only, negative-only, and combined correction separately;
6. compare masks without changing the source image or label.

Coordinates were not tuned per sample. Point selection uses ground truth as an
evaluation oracle, so this proves correction response rather than unaided
operator click quality.

## 2. Completion gate

| Gate | Result |
| --- | --- |
| Every baseline is a genuinely poor candidate (`IoU < 0.50`) | Pass, 6/6 |
| At least one development candidate improves | Pass, 1/2 |
| Held-out majority improves after one combined correction | Pass, 3/4 |
| Held-out median IoU delta is at least `+0.05` | Pass, `+0.0988` |
| Positive-only correction reduces false negatives | Pass, 6/6 |
| Negative-only correction reduces false positives where applicable | Pass, 4/4 |
| Dataset source tree remains unchanged | Pass |

Source tree:

- files before/after: `798` / `798`
- SHA-256 before/after:
  `F09D09AA1A1EC9AB7866087361CF1B48C6E6D32F5C0CC239CE619D39FB9A0474`

Runtime:

- MobileSAM
- Ultralytics `8.4.101`
- Torch `2.12.1+cpu`
- CPU
- weights SHA-256:
  `6DBB90523A35330FEDD7F1D3DFC66F995213D81B29A5CA8108DBCDD4E37D6C2F`

## 3. Results

| Partition | Sample | Baseline IoU | Combined corrected IoU | Delta | Outcome |
| --- | --- | ---: | ---: | ---: | --- |
| development | `kos08_Part2` | 0.2111 | 0.1877 | -0.0234 | worsened |
| development | `kos29_Part0` | 0.2078 | 0.3007 | +0.0929 | improved |
| held-out | `kos06_Part7` | 0.2752 | 0.3740 | +0.0988 | improved |
| held-out | `kos14_Part7` | 0.3749 | 0.3260 | -0.0489 | worsened |
| held-out | `kos35_Part5` | 0.2718 | 0.4339 | +0.1621 | improved |
| held-out | `kos41_Part7` | 0.2306 | 0.4817 | +0.2510 | improved |

Development median delta is `-0.0234`. It is retained as a failure, not hidden.

Held-out median delta is `+0.0988`. Three of four held-out candidates improve.

## 4. Product decision from the failure

`kos14_Part7` proves why correction must be incremental:

- baseline: `0.3749`;
- positive-only: `0.4620`;
- positive+negative together: `0.3260`.

Each point type separately changed its targeted error count in the requested
direction, but combining them produced a worse global mask. Therefore the
product guidance now says:

`한 점 추가 → 후보 다시 생성해 비교 → 부족할 때 다음 점 추가`

The application does not claim that every point or point combination improves
the candidate.

## 5. Evidence

Artifact root:

`artifacts\mobile-sam-correction-effectiveness\20260728-202434`

Important files:

- `selection-manifest.json`
- `sample-results.jsonl`
- `summary.json`
- `summary.md`
- per-sample baseline, positive-only, negative-only, combined masks
- per-sample `comparison.png`

Representative success:

`held-out\kos41_Part7\comparison.png`

Recorded failure:

`held-out\kos14_Part7\comparison.png`

Current-source 1920x1080 incremental-guidance capture:

`artifacts\ui\smart-mask-incremental-guidance-20260728\after-one-point-1920x1080.png`

The closest pre-guidance layout baseline is
`artifacts\ui\smart-mask-contextual-correction-20260728\after-expanded-1920x1080.png`.
A true before capture with one entered point was not available because the
guidance wording had already changed; this baseline is not presented as an
exact before state.

## 6. Verification command

```text
dotnet LabelingApplication.Tests.dll
  --real-mobile-sam-correction-effectiveness
  --dataset-root <KolektorSDD raw expanded root>
```

The focused fail-closed evaluator completed with
`MOBILE_SAM_CORRECTION_GATE=True` and process exit code `0`. Three consecutive
separated-variant runs reproduced all six baseline, corrected, and delta values
exactly.

## 7. Boundary

- This is a local replay of a public industrial dataset copy.
- Ground-truth-guided point selection is an evaluation oracle.
- It does not prove that an operator will always choose the optimal point.
- Source masks have their own annotation granularity and are not independent
  camera-session evidence.
- It does not establish production accuracy or CVAT/V7 parity.
- Two combined corrections worsened the result and remain part of the evidence.

## 8. Next priority

Because a correction can worsen a candidate, the next bounded feature should
preserve the previous Smart Mask candidate and let the operator visually
compare or restore it before confirmation.

Completion gate:

1. retain initial and latest pending candidates only inside the current session;
2. expose `이전 후보 보기` and `현재 후보 보기` contextually after a rerun;
3. allow explicit restoration of the better-looking candidate;
4. keep both candidates pending and unsaved until confirmation;
5. clear comparison history at image/Recipe/session change;
6. verify canonical save contains only the explicitly selected candidate.

Recommended model: `gpt-5.6-sol`
Reasoning effort: `high`

## 9. Follow-up closure

The previous/current pending-candidate compare/restore priority is complete.
See `SMART_MASK_CANDIDATE_COMPARE_RESTORE_20260728.md`.

- first and latest candidate references are session-only;
- one selected version is exposed to Candidate Review at a time;
- switching candidates does not save;
- confirmation persists only the explicitly selected version;
- confirmation, skip, image/Recipe change, and session reset close comparison
  history.
