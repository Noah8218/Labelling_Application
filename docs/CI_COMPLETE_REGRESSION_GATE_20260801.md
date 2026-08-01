# CI Complete Regression Gate

Date: 2026-08-01 KST

## Status

Status: Complete for the repository workflow contract.

The Windows CI job now runs the complete default regression suite exactly once
in one process after the isolated test build and documentation checks. The
step has a 15-minute timeout and precedes release publishing.

This is locally verified workflow-source evidence. It does not claim that a
hosted GitHub Actions run has passed before the change is committed and pushed.

## Contract

- command: `dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll`;
- arguments: none, selecting the complete default suite;
- concurrency: one invocation in the CI job;
- timeout: 15 minutes;
- order: build and documentation checks, complete regression, release publish,
  package verification, artifact upload;
- CI environment storage: use the hosted runner's isolated available storage;
  the workstation-only D-drive routing rule does not block CI.

## Acceptance Criteria And Evidence

| Criterion | Result | Evidence |
| --- | --- | --- |
| CI invokes the no-argument suite exactly once | Pass | `--priority-workflow-docs` source contract |
| CI bounds the invocation | Pass | `timeout-minutes: 15` |
| Current suite passes in one local process | Pass | `267/267`, exit code 0 |
| Failure output is absent | Pass | 0 FAIL rows, stderr 0 bytes |
| Build remains clean | Pass | 0 warnings, 0 errors |

Local evidence root:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\ci-complete-regression-gate-20260801`

Current `267/267` rerun evidence root:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\headless-environment-self-test-20260801`

## Reusable Verification

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Build-LabelingApplicationTests.ps1 `
  -OutputName isolated-out

dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll `
  --priority-workflow-docs

dotnet .\artifacts\tests\isolated-out\LabelingApplication.Tests.dll
```

## Boundary

- The first headless product command is now separately complete as a read-only
  environment self-test; inference, training, archive, labeling, and model
  adoption remain outside this CI gate.
- Installer, signing, clean-machine GPU labeling, and production accuracy are
  unchanged external or separately scoped gates.
- Hosted CI success must be reported only after a pushed GitHub Actions run is
  inspected.

```text
Status: Complete
Scope: One bounded, solo, no-argument complete regression invocation in the Windows CI job.
Acceptance criteria: Exact single invocation -> pass; 15-minute timeout -> pass; current local equivalent run -> 267/267, exit 0, stderr empty; build -> 0 warnings/errors.
Verification: Isolated build, --priority-workflow-docs, solo default suite, documentation information architecture, git diff --check.
Evidence: .github/workflows/ci.yml, tests/LabelingApplication.Tests/Program.cs, and the D-drive evidence root named above.
Boundary / next dependency: Hosted GitHub Actions execution requires commit/push; additional product CLI workflows, installer/signing, GPU clean-target validation, and field validation remain separate.
```
