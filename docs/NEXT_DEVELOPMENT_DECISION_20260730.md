# Next Development Decision

Date: 2026-07-30 KST

## 1. Decision

Implement exactly one next slice:

`P0-B1 Versioned Deterministic Self-Contained Release Bundle Contract`.

Why this is next:

- the application already passes the complete local regression and launches
  from a self-contained publish folder;
- the current output cannot be identified or verified as a stable release
  because SDK, version, deterministic settings, hashes, and legal inventory
  are not a release contract;
- an installer, support bundle, or crash recovery feature would build on an
  unidentified package and should not precede this foundation.

This is an engineering package baseline, not a public release declaration.
Use `0.1.0` as the initial engineering product version unless the user supplies
a different release version before implementation.

Recommended model: `gpt-5.6-terra`  
Reasoning effort: `high`

## 2. Current Owner To Intended Owner

- SDK/version/build identity:
  `Properties/AssemblyInfo.cs` plus implicit local SDK selection ->
  repository-level release properties and SDK policy.
- publish-folder policy:
  optional flags and size-only text manifest in
  `scripts/publish-win-x64.ps1` ->
  one versioned self-contained release-bundle contract owned by that script.
- legal inventory:
  repository-only `LICENSE`/`NOTICE` and untracked dependency knowledge ->
  publish-copied project notices plus a source-controlled exact
  third-party-notice inventory.

Do not create a new service, ViewModel, WPF surface, or installer for this
slice.

## 3. Included Scope

1. Pin a supported .NET 8 SDK policy for repository and CI use.
2. Replace wildcard/time-derived application versioning with explicit,
   reproducible assembly, file, product, and informational versions.
3. Enable deterministic Release build behavior.
4. Make the release command produce a self-contained `win-x64` folder under a
   versioned output root.
5. Write a machine-readable release manifest containing at least:
   - product version;
   - assembly/file/informational version;
   - source commit and dirty flag;
   - SDK version;
   - RID and self-contained mode;
   - relative path, byte length, and SHA-256 for every payload file.
6. Copy `LICENSE` and `NOTICE` into the release folder.
7. Add and copy an exact third-party notice inventory covering resolved NuGet
   packages and checked-in/native redistributed binaries.
8. Fail the release command when version identity, required notices, required
   files, or manifest hashes are missing.
9. Add a focused package-contract test and CI release-publish gate.

## 4. Excluded Scope

- installer, MSIX/WiX/Inno/NSIS selection;
- Authenticode signing or certificate acquisition;
- Python/CUDA/model-weight redistribution;
- runtime diagnostic UI or support-bundle export;
- log-path redesign;
- crash recovery or project archive;
- headless product CLI;
- clean-machine, upgrade, or uninstall verification;
- production accuracy or throughput validation.

## 5. Observable Contract

One approved command must produce one identifiable release folder. A reviewer
must be able to answer, from the folder alone:

- which product/version/source produced it;
- which SDK/RID/publish mode was used;
- whether each file matches its recorded SHA-256;
- which project and third-party notices apply;
- whether the package is self-contained.

No release command may change Recipe, label, workspace, or user data.

## 6. Acceptance Criteria

1. Repository SDK selection and CI use the declared .NET 8 policy.
2. The product reports `0.1.0` engineering identity consistently, or the
   explicitly supplied replacement version.
3. Release compilation is deterministic and no `1.0.*` wildcard remains.
4. The default release command emits a versioned self-contained `win-x64`
   folder.
5. Manifest metadata and every payload SHA-256 verify successfully.
6. `LICENSE`, `NOTICE`, and the third-party notice inventory are present.
7. Missing/changed payload, notice, version, or manifest entry fails closed.
8. Existing publish required-file and private DEV-path guards still pass.
9. Current published WPF smoke passes.
10. Complete default regression passes once, in isolation.
11. CI runs the focused package contract and produces the release folder as a
    workflow artifact; CI does not claim code signing or clean-machine
    installation.
12. `git diff --check` passes and the completion record names exact commands,
    artifact root, source commit, and boundaries.

## 7. Verification Plan

```powershell
dotnet --version
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --release-package-contract
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -Configuration Release
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-first-run.ps1 -Configuration Release -SkipBuild -SkipTests -SkipYoloSmoke -RunPublishWpfSmoke
git diff --check
```

Run the default regression only once at a time because the audit reproduced
shared temporary-state interference between concurrent suites.

## 8. External Prerequisites

None for this engineering slice.

The following remain later prerequisites:

- clean Windows VM/PC for install lifecycle evidence;
- signing identity/certificate for signed distribution;
- redistribution decisions for Python, CUDA, weights, and native runtimes;
- field data, thresholds, and target hardware for production claims.

## 9. Closure State

```text
Status: Complete
Scope: Selected and bounded one next implementation slice from the commercial readiness audit.
Acceptance criteria: One coherent owner, included/excluded scope, observable contract, verification plan, and external boundary are documented.
Verification: Cross-checked against docs/COMMERCIAL_READINESS_AUDIT_20260730.md and the current publish/build/test evidence.
Evidence: docs/NEXT_DEVELOPMENT_DECISION_20260730.md.
Boundary / next dependency: P0-B1 implementation is Complete in docs/RELEASE_PACKAGE_CONTRACT_P0B1_20260730.md. The next task is P0-B2 Packaged Runtime Diagnostics And Support Bundle; installer, signing, clean-machine, recovery, and field validation remain separate.
```
