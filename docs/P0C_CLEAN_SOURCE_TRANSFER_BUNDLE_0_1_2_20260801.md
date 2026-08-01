# P0-C Clean-Source Transfer Bundle 0.1.2

Date: 2026-08-01 KST

## Status

Status: Complete for clean-source package preparation and local transfer
verification.

The P0-C GPU-capable clean-target workflow now has a deliberately versioned,
self-contained `win-x64` transfer bundle. The release was produced from clean
commit `59e37d81d54c4d5e2162747b4eaece2173ef7151`; its manifest records
`source.dirty=false`.

This closes the package-preparation prerequisite. It does not execute or close
the external GPU-target image/display/label/save/reopen gate.

## Release Identity

- product version: `0.1.2`;
- configuration/RID: `Release` / `win-x64`;
- deployment mode: self-contained;
- source commit: `59e37d81d54c4d5e2162747b4eaece2173ef7151`;
- source dirty: `false`;
- release payload: `504` manifest files plus the two manifest files;
- final package file count: `506`;
- release-manifest SHA-256:
  `7a54914d2637d227cb5eb75046c248e8e7a86b7e832ba6c61c8144affd552f19`.

Two consecutive publishes from the same commit produced identical release
manifests and identical full-package path/length/SHA-256 inventories.

Package path:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\publish\Release\win-x64\0.1.2`

Package evidence:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\release-package-0.1.2-clean-59e37d8-20260801`

## Packaged Headless Preflight

The final packaged EXE ran:

`--environment-self-test --json`

Results:

- exit code `0`;
- schema `1`;
- `7` pass, `1` warning, `0` fail;
- the warning is the intentional absence of a Main Viewer UI/OpenGL context;
- `durableWrites=false`;
- stderr `0` bytes;
- no `self-test-latest.json` persisted;
- the complete package inventory remained unchanged after execution;
- fail-closed package verification passed again after execution.

## Transfer Bundle

Transfer ZIP:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\p0c-clean-machine\gpu-target-transfer-0.1.2-59e37d8\OpenVisionLab-P0C-GPU-Target-0.1.2-59e37d8.zip`

- ZIP length: `107,713,965` bytes;
- ZIP SHA-256:
  `cfb7be9d5055ad45b51411cf10d273749059cd98b5b20a930e4d15991de39c1f`;
- bundle files: `509`;
- contents: `Release`, P0-C label-workflow harness, bundle manifest, and one
  approved fixture image;
- no SDK, Python runtime, model weight, production image, or credential is
  required by this fixture gate.

The ZIP was extracted into a new D-drive verification directory. Verification
proved:

- ZIP hash matches the companion transfer manifest;
- release-manifest hash matches the bundle manifest;
- `504/504` release payload lengths and SHA-256 hashes pass;
- missing payload count `0` and unlisted payload count `0`;
- fixture and harness hashes match;
- the extracted PowerShell harness parses;
- the harness dynamically selects the monitor with the smallest `Left` bound,
  moves the EXE there, verifies intersection, and records
  `monitor-placement.json`;
- a single-monitor target records an explicit fallback.

Transfer evidence directory:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\p0c-clean-machine\gpu-target-transfer-0.1.2-59e37d8`

## Boundary

- The transfer ZIP is not an installer and is not code signed.
- No hosted GitHub Actions success is claimed because this work was not
  pushed in this task.
- No actual Main Viewer graphics result is claimed from the headless command.
- The next P0-C run still requires direct access to a supported GPU-capable
  clean Windows PC or VM through its local/VM console.
- Image display, one rectangle, explicit save, non-empty label, normal close,
  relaunch, saved-image reopen, and returned evidence remain target-side gates.

```text
Status: Complete
Scope: Clean-source 0.1.2 self-contained package, deterministic republish, packaged read-only environment CLI, transfer ZIP, and clean extraction/hash verification.
Acceptance criteria: source.dirty=false -> pass; deterministic two-publish inventory -> pass; 504/504 payload hashes -> pass; packaged CLI -> 7 pass/1 warning/0 fail; no durable writes -> pass; post-run package immutability -> pass; ZIP/extraction/harness/fixture verification -> pass.
Verification: publish-win-x64.ps1 publish and VerifyOnly; two full package inventories; packaged --environment-self-test --json; ZIP SHA-256; new-directory Expand-Archive; per-file length/SHA-256 verification; PowerShell harness parse and leftmost-monitor contract check.
Evidence: package and transfer evidence directories named above, docs/P0C_CLEAN_SOURCE_TRANSFER_BUNDLE_0_1_2_20260801.md, and source commit 59e37d81d54c4d5e2162747b4eaece2173ef7151.
Boundary / next dependency: Direct access to the selected supported GPU-capable clean Windows target; installer/signing and production-data validation remain separate.
```
