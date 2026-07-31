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

## 6. Next Bounded Structure Slice

The next nondestructive repository-structure slice is a documentation
information architecture: keep current authority documents easy to find and
classify dated completion/evidence records without rewriting their historical
claims. Any deletion of the 4.486 GiB rebuildable-candidate set remains a
separate destructive action requiring an exact target list and explicit
operator approval.

## 7. Durable Closure

```text
Status: Complete
Scope: Added a read-only artifact inventory and centralized future test final-build outputs below artifacts/tests through one safe build wrapper.
Acceptance criteria: Inventory is reusable and no-delete; output is constrained below artifacts; the test wrapper rejects unsafe names and owns one central final-output path; active instructions and CI use that path; the protected default suite passes from it.
Verification: Both PowerShell parsers passed; unsafe output names and out-of-root reports were rejected; central build passed with 0 warnings/errors; --priority-workflow-docs passed; default suite passed 264/264; post-change inventory found one artifacts/tests subtree and reported deletionPerformed=false; git diff --check passed.
Evidence: scripts/Get-RepositoryArtifactInventory.ps1, scripts/Build-LabelingApplicationTests.ps1, artifacts/repository-structure-inventory-20260731/inventory.json, and inventory-after-test-centralization.json.
Boundary / next dependency: No existing output or source file was deleted or moved. Historical commands remain historical. Candidate cleanup requires an exact target review and explicit approval; the next nondestructive structure slice is documentation classification.
```
