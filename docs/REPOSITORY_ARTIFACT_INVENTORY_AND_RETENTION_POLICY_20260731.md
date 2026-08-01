# Repository Artifact Inventory And Retention Policy

Last updated: 2026-07-31 KST

## 1. Purpose

This document defines the first repository-structure cleanup slice. It
separates generated files from source and durable evidence without deleting,
moving, or rewriting any project content.

The reusable inventory command is:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Get-RepositoryArtifactInventory.ps1 `
  -OutputJson .\artifacts\repository-structure-inventory-20260731\inventory.json
```

The script is intentionally inventory-only:

- it contains no delete or move command;
- it refuses to write its JSON report outside the repository `artifacts`
  root;
- it excludes its own report subtree from subsequent inventories;
- it reports `deletionPerformed=false`;
- no classification authorizes deletion by itself.

## 2. Classification Contract

| Classification | Meaning | Required action before removal |
| --- | --- | --- |
| `preserve-review` | A tracked Markdown document references the top-level artifact path. It may own UI, release, model, or clean-target evidence. | Open the referencing documents and confirm that replacement evidence exists or the historical record may be retired. |
| `rebuildable-candidate` | Ignored build, IDE, package, component, or isolated-test output appears reproducible. | Confirm the owning build/test command and run a bounded cleanup preview before any deletion. |
| `manual-review` | The directory has mixed or insufficiently identified content. | Assign an owner and explicit keep/delete decision; do not infer from age or size alone. |

The term `candidate` is deliberate. A generated directory can still be needed
for an active debugging session, a clean-target transfer, or a release
comparison.

## 3. Current Inventory Evidence

Source version:

`4374fddea4fb5042b3b028fb49548545ea1c9811`

The current read-only inventory produced:

| Classification | Entries | Files | Size |
| --- | ---: | ---: | ---: |
| `preserve-review` | 96 | 89,622 | 9.482 GiB |
| `rebuildable-candidate` | 45 | 8,460 | 4.486 GiB |
| `manual-review` | 50 | 10,839 | 0.990 GiB |
| Total | 191 | 108,921 | 14.958 GiB |

Largest rebuildable candidates:

| Path | Size | Current rationale |
| --- | ---: | --- |
| `tests/LabelingApplication.Tests/artifacts` | 1.916 GiB | Repeated isolated test outputs |
| `packages` | 0.499 GiB | Ignored package cache |
| `tests/LabelingApplication.Tests/bin` | 0.294 GiB | Test build output |
| `artifacts/p1a-before-build` | 0.252 GiB | Unreferenced build-output naming pattern |
| `artifacts/isolated-visual-qa-p4` | 0.215 GiB | Unreferenced isolated output |
| `artifacts/isolated-visual-qa-p4-before` | 0.215 GiB | Unreferenced isolated output |
| `artifacts/scissors-ui-out` | 0.215 GiB | Unreferenced isolated output |
| `artifacts/scissors-contract-out` | 0.215 GiB | Unreferenced isolated output |
| `artifacts/vertex-check-out` | 0.215 GiB | Unreferenced isolated output |

Large paths that remain protected from automatic cleanup include
`artifacts/run`, `artifacts/publish`, `artifacts/ui`,
`artifacts/p0c-clean-machine`, model-comparison outputs, and operator-video
evidence because tracked documents currently reference them.

The machine-readable report is local ignored evidence:

`artifacts/repository-structure-inventory-20260731/inventory.json`

## 4. Cleanup Gate

No cleanup may run until all of the following are true:

1. A proposed target list is generated from the inventory.
2. Every target resolves below one explicitly allowed generated root.
3. Tracked files, `.git`, `.proofline`, source, documents, datasets, runtime
   scripts, and release notices are excluded.
4. `preserve-review` and `manual-review` entries are excluded by default.
5. The operator explicitly approves the exact reclaimable size and target
   list.
6. The cleanup records before/after free space and confirms `git status`.
7. Required build/test outputs are regenerated through their owning commands,
   not copied from an unrelated prior run.

## 5. Centralized Test Output Slice

Status: `Complete`.

New test final-build outputs are centralized below
`artifacts/tests/<suite-or-purpose>` by:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Build-LabelingApplicationTests.ps1 `
  -OutputName isolated-out
```

The wrapper accepts one validated output-name segment, resolves the final
path below the repository test-artifact root, runs the existing serialized
test build, and verifies that `LabelingApplication.Tests.dll` was produced.
It contains no cleanup command.

Included:

- `scripts/Build-LabelingApplicationTests.ps1` as the canonical test-output
  build wrapper;
- current README, CI, AGENTS, and current handoff command updates;
- test source assertions for the wrapper, central output, and no-delete
  boundary;
- a zero-warning/error build and the default `264/264` suite from the new
  output path.

Excluded:

- deleting existing outputs;
- editing historical command records to imply they used a new path;
- moving WPF, Viewer, ImageCanvas, ROI, brush, or eraser source;
- reorganizing `Yolo` or dated documentation in the same change.

Verification evidence:

- the wrapper built `artifacts/tests/isolated-out` with 0 warnings and 0
  errors;
- `--priority-workflow-docs` passed from the new DLL;
- the default suite passed `264/264` from the new DLL;
- the post-change inventory reported exactly one `artifacts/tests` entry with
  90 files and 0.253 GiB;
- the previous `tests/LabelingApplication.Tests/artifacts` output remains
  present with 953 files and 1.916 GiB;
- the post-change inventory remained read-only and reported
  `deletionPerformed=false`.

Post-change local evidence:

`artifacts/repository-structure-inventory-20260731/inventory-after-test-centralization.json`

The later explicitly approved D-drive expansion also routes root/component
`bin`/`obj`/`artifacts`, local `packages`/`.vs`, and the repository-tracked
test-fixture `datasets` path through verified junctions. It moved 1,341 files
and 604,817,071 bytes from 20 nonempty paths. This is a storage placement
contract, not permission to move product source, docs, dependencies, user
datasets, or `.proofline`. See
`LOCAL_TEST_STORAGE_AND_LEFT_MONITOR_CONTRACT_20260731.md`.

## 6. Documentation Information Architecture

Status: `Complete`.

`docs/README.md` now classifies every repository Markdown document below
`docs` exactly once as current navigation, an operator guide, a feature or
productization contract, verification evidence, or historical context.
Current authority order remains unchanged, and no existing document was
moved, renamed, deleted, or rewritten as part of the classification.

`scripts/Test-DocumentationInformationArchitecture.ps1` fails when a Markdown
document is missing, classified more than once, lacks a lifecycle label, or
is linked through a missing local target. The root README links to the index,
repository instructions define its non-authoritative navigation role, and CI
runs the verifier before the existing documentation smoke.

Any deletion of the previously inventoried rebuildable-candidate set remains
a separate destructive action requiring a fresh exact target list, current
size, and explicit operator approval. It is not an automatic next step.

## 7. Current Read-Only Cleanup Preview

Status: `Complete`.

The post-D-drive-migration preview found 45 rebuildable candidates containing
8,473 files and 4.455 GiB. Of that total, 3.562 GiB is physically on D and
0.893 GiB is physically on C. It excluded 98 preserve-review and 51
manual-review entries. The preview records exact logical/physical paths and
bytes while keeping `deletionPerformed=false` and
`operationsAuthorized=false`.

Evidence:

- `docs/REPOSITORY_CLEANUP_PREVIEW_20260731.md`;
- `artifacts/repository-cleanup-preview-20260731/inventory-current.json`;
- `artifacts/repository-cleanup-preview-20260731/cleanup-preview.json`.

This preview itself did not authorize deletion. The operator subsequently
approved only its unchanged 32-entry C-drive subset. See the bounded execution
record below. The D-drive subset remains unapproved.

## 8. Approved C-Drive Candidate Cleanup

Status: `Complete`.

The approved C-only execution removed 32 rebuildable candidates representing
6,587 files and 958,721,091 bytes. Direct checks found zero C targets remaining
and all 13 D candidates still present immediately after deletion. A clean
current-source test build then completed with zero warnings and errors, and
the focused priority-workflow documentation test passed. The rebuild recreated
only eight C `obj` candidates totaling 15,016,091 bytes; the recorded
post-rebuild C free-space value remained 948,568,064 bytes above the
pre-delete point-in-time value.

The executor locks the preview's count and bytes, permits only C targets below
the repository, rejects junction/reparse/overlapping paths, checks live file
totals, and records progress after every target. Full acceptance evidence is
in `REPOSITORY_C_CANDIDATE_CLEANUP_EXECUTION_20260731.md`.

The D-drive candidates, protected evidence, user data, source, documentation,
and `.proofline` remain outside this cleanup.

## 9. Durable Closure

```text
Status: Complete
Scope: Added a read-only artifact inventory, centralized future test final-build outputs below artifacts/tests, and executed the separately approved 32-entry C-only rebuildable cleanup.
Acceptance criteria: Inventory and preview remain read-only; test output is constrained below artifacts; approved cleanup is count/byte/path locked; C targets were removed while all D candidates were retained; current-source rebuild and focused test passed.
Verification: PowerShell parsers passed; unsafe output paths were rejected; C cleanup removed 32/32 targets and preserved D 13/13; post-cleanup build passed with 0 warnings/errors; --priority-workflow-docs passed; post-rebuild inventory remained read-only; git diff --check passed.
Evidence: scripts/Get-RepositoryArtifactInventory.ps1, scripts/Get-RepositoryCleanupPreview.ps1, scripts/Invoke-ApprovedRepositoryCleanup.ps1, docs/REPOSITORY_C_CANDIDATE_CLEANUP_EXECUTION_20260731.md, and artifacts/repository-cleanup-preview-20260731/*.json.
Boundary / next dependency: D candidates and all protected scopes remain untouched and require separate approval. Historical commands remain historical. P0-C remains blocked on an accessible GPU-capable clean Windows target.
```
