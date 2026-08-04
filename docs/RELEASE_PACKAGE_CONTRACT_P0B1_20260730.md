# P0-B1 Versioned Deterministic Self-Contained Release Bundle

Date: 2026-07-30 KST

## 1. Closure

Status: Complete

The repository now produces one identifiable engineering release package at:

`artifacts\publish\Release\win-x64\0.1.0`

This is a local engineering package baseline. It is not an installer,
code-signed public release, clean-machine certification, or production model
validation.

## 2. Completed Scope

- `global.json` pins .NET SDK `8.0.421` with roll-forward disabled so a newer
  preinstalled feature-band patch cannot change the release manifest.
- `Directory.Build.props` owns product `0.1.0`, assembly/file `0.1.0.0`,
  explicit informational-version policy, and deterministic compilation.
- the legacy `1.0.*` wildcard and project-level deterministic opt-outs were
  removed from the active publish graph;
- `scripts/publish-win-x64.ps1` defaults to self-contained `win-x64`, resolves
  a numeric release version, replaces only its exact version folder, and
  supports read-only `-VerifyOnly`;
- `release-manifest.json` records schema, product/assembly/informational
  identity, full source commit and dirty state, SDK/configuration/RID/mode,
  and normalized path/length/SHA-256 for every package payload;
- `publish-manifest.txt` provides the same payload hashes in a compact
  text form;
- `LICENSE`, `NOTICE`, and `THIRD-PARTY-NOTICES.txt` ship in the package;
- the third-party inventory lists the resolved NuGet graph plus redistributed
  checked-in/native components at the versions used by this build;
- existing required-file and private development-path guards remain active;
- the focused test checks source policy, manifest identity, exact payload set,
  all payload hashes, resolved package notice coverage, verifier success,
  tamper rejection, and exact restoration;
- CI uses SDK `8.0.421`, publishes the package, runs the focused contract, and
  uploads `openvisionlab-labeling-studio-0.1.0-win-x64`.

## 3. Package Evidence

The verified package was produced from:

- source commit: `4c6718a9b75465a640f480b6f3403ec08ff07436`;
- source dirty flag: `true`, because this completion work was not committed;
- product version: `0.1.0`;
- assembly/file version: `0.1.0.0`;
- informational version: `0.1.0+4c6718a9b754`;
- SDK: `8.0.421`;
- configuration/RID/mode: `Release` / `win-x64` / self-contained.

Package inventory before first launch:

- `503` hashed payload files;
- `2` unhashed manifest metadata files;
- `264,624,552` payload bytes;
- deterministic manifest SHA-256:
  `F9C589EA1AF73101170AB0AE5736B0353E9DC91098401D85AB9AD35B1A419A23`.

The manifest itself is excluded from its payload hash list to avoid a
self-referential hash. Both manifest files are mandatory and their
machine-readable payload entries are verified fail-closed.

Two consecutive publishes from unchanged source produced byte-identical
`release-manifest.json` files with the SHA-256 above.

## 4. Verification

Commands run:

```powershell
dotnet --version
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -Configuration Release
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --release-package-contract
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-first-run.ps1 -ConfigPath .\config\labeling-runtime.example.json -SkipBuild -SkipTests -SkipYoloSmoke -RunPublishWpfSmoke
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --priority-workflow-docs
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll
git diff --check
```

Results:

- SDK selection: `8.0.421`;
- isolated test-project build: 0 warnings, 0 errors;
- package publish and built-in identity/hash/legal/private-path validation:
  pass;
- focused package contract including payload tamper rejection: pass;
- two-publish deterministic manifest comparison: pass;
- current published WPF shell open smoke: pass;
- documentation contract: pass;
- default regression: `260/260` pass in one isolated process;
- whitespace validation: pass.
- CI workflow publish/test/upload contract: implemented and covered by the
  local documentation/source gate; no hosted GitHub Actions run occurred
  because no commit or push was requested.

Determinism evidence:

`artifacts\release-package-contract-20260730\first-release-manifest.json`

`artifacts\release-package-contract-20260730\second-release-manifest.json`

## 5. Boundaries And Next Dependency

The WPF open smoke creates runtime-local `CONFIG` and `Log` state under a
launched copy. The final distributable folder is regenerated after smoke so
its recorded inventory remains the pre-first-run package. Relocating,
structuring, retaining, and exporting runtime diagnostics belongs to P0-B2.

The package includes an engineering third-party inventory, but that record
does not replace formal legal, export-control, security, or redistribution
review before a public/commercial release. Python, CUDA, model weights, and
external YOLO assets are not bundled.

Installer choice, code signing, clean-machine install/upgrade/uninstall,
recovery/archive, product CLI, and production-data accuracy/throughput remain
outside this completed slice.

```text
Status: Complete
Scope: Explicit 0.1.0 deterministic self-contained win-x64 engineering release package with source/build/file provenance, legal inventory, fail-closed verification, focused test, and CI artifact.
Acceptance criteria: SDK/version/determinism -> pass; versioned self-contained output -> pass; complete payload SHA-256 manifest -> pass; required legal files -> pass; tamper rejection -> pass; current WPF launch -> pass; default regression -> 260/260 pass; CI publish/test/upload workflow contract -> locally validated, hosted run not executed.
Verification: Commands and results in sections 3 and 4.
Evidence: docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md, artifacts/publish/Release/win-x64/0.1.0, and artifacts/release-package-contract-20260730.
Boundary / next dependency: P0-B2 Packaged Runtime Diagnostics And Support Bundle is next; installer/signing/clean-machine/recovery/field validation remain separate.
```
