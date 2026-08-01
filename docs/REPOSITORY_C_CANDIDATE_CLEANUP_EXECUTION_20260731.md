# Approved C-Drive Repository Candidate Cleanup

Date: 2026-07-31 KST

Status: Complete

## Scope

The operator approved deletion of only the 32 C-drive entries in the fresh
`rebuildable-candidate` preview. The D-drive candidate set, all
`preserve-review` and `manual-review` entries, tracked files, source,
documentation, user datasets, and `.proofline` were excluded.

The approved pre-delete set contained 6,587 files and 958,721,091 bytes
(0.893 GiB). It matched the earlier reviewed C-drive logical-path set exactly:
32 entries, zero additions, and zero removals.

## Safety Contract

`scripts/Invoke-ApprovedRepositoryCleanup.ps1` is fail-closed. Execution
requires all of the following:

- explicit `-Apply`;
- the approved drive is exactly `C:`;
- caller-supplied candidate count and byte total match the fresh preview;
- every logical and physical target is below this repository;
- logical and physical paths are identical, so junction-routed targets are
  refused;
- target and descendant reparse points are refused;
- live file count and bytes still match the preview;
- overlapping target directories are refused;
- evidence is written before deletion and after every completed target.

## Result

| Check | Result |
| --- | ---: |
| Approved C targets deleted | 32 / 32 |
| Approved files represented | 6,587 |
| Approved bytes represented | 958,721,091 |
| C targets remaining immediately after delete | 0 |
| D preview targets preserved | 13 / 13 |
| D preview targets missing | 0 |
| Immediate measured free-space increase | 969,969,664 bytes |
| Post-rebuild C candidates regenerated | 8 entries / 15,016,091 bytes |
| Recorded post-rebuild net free-space increase | 948,568,064 bytes (0.883 GiB) |

Free-space values are point-in-time filesystem measurements and can vary with
unrelated operating-system activity. Exact approved/deleted byte totals come
from the locked preview and live pre-delete enumeration.

## Verification

The following current-source checks passed:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Build-LabelingApplicationTests.ps1 `
  -Configuration Debug `
  -OutputName post-c-cleanup-20260731

dotnet .\artifacts\tests\post-c-cleanup-20260731\LabelingApplication.Tests.dll `
  --priority-workflow-docs
```

- rebuild: 0 warnings, 0 errors;
- focused priority-workflow documentation test: pass;
- the post-rebuild inventory remained read-only;
- the recreated C outputs were limited to eight `obj` candidates totaling
  0.014 GiB;
- the large `packages`, `.vs`, root `bin`, and component `bin` candidates did
  not return during this verification.

Evidence:

- `artifacts/repository-cleanup-preview-20260731/inventory-pre-delete.json`;
- `artifacts/repository-cleanup-preview-20260731/cleanup-preview-pre-delete.json`;
- `artifacts/repository-cleanup-preview-20260731/c-cleanup-execution.json`;
- `artifacts/repository-cleanup-preview-20260731/inventory-post-rebuild.json`;
- `artifacts/repository-cleanup-preview-20260731/cleanup-preview-post-rebuild.json`;
- `artifacts/tests/post-c-cleanup-20260731/LabelingApplication.Tests.dll`.

## Durable Closure

```text
Status: Complete
Scope: Deleted only the explicitly approved 32 C-drive rebuildable candidates; preserved all D-drive and protected scopes.
Acceptance criteria: Fresh path set unchanged -> pass; exact count/bytes locked -> pass; C targets removed -> 32/32; D targets retained -> 13/13; current-source rebuild -> 0 warnings/errors; focused workflow-doc test -> pass.
Verification: Approved cleanup evidence, direct post-delete existence checks, current-source rebuild, focused test, and post-rebuild inventory/preview.
Evidence: scripts/Invoke-ApprovedRepositoryCleanup.ps1 and artifacts/repository-cleanup-preview-20260731/*.json.
Boundary / next dependency: Removed data was untracked build/package/IDE output and was not backed up; it is recoverable through the owning restore/build workflows. D candidates, protected evidence, user data, source, docs, and .proofline were not removed. This does not advance the external GPU-capable P0-C validation gate.
```
