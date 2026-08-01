# Headless Environment Self-Test CLI

Date: 2026-08-01 KST

## Status

Status: Complete for the current-source read-only command.

OpenVisionLab Labeling Studio now recognizes:

`--environment-self-test --json`

before application startup, logging initialization, the single-instance mutex,
or WPF window creation. It writes one JSON document to standard output and
returns a machine-readable process exit code.

## Behavior Contract

- no WPF window;
- no label, Recipe, dataset, candidate, model, or support-bundle mutation;
- no startup diagnostics or `self-test-latest.json` persistence;
- no directory creation or write/delete probe in read-only mode;
- existing diagnostics/support paths are checked for existence only;
- the graphics check is a warning because no Main Viewer OpenGL context exists;
- warnings remain visible in JSON but do not make the command fail.

Exit codes:

| Code | Meaning |
| --- | --- |
| `0` | no failed checks; warnings may exist |
| `2` | one or more environment checks failed |
| `64` | invalid command arguments |
| `70` | unexpected internal error |

## Reliable PowerShell Example

The product is a Windows GUI executable, so automation should explicitly wait
and redirect its output:

```powershell
$process = Start-Process `
  -FilePath .\OpenVisionLab.LabelingStudio.exe `
  -ArgumentList @('--environment-self-test', '--json') `
  -RedirectStandardOutput .\environment-self-test.json `
  -RedirectStandardError .\environment-self-test.stderr.log `
  -WindowStyle Hidden `
  -PassThru `
  -Wait

$process.ExitCode
```

JSON fields include `schemaVersion`, `command`, `mode`, `status`, `exitCode`,
`durableWrites`, product/runtime identity, pass/warning/fail counts, and the
individual checks.

## Verification

- isolated build: 0 warnings, 0 errors;
- focused actual-product-EXE test: pass;
- existing runtime diagnostics/support-bundle contract: pass;
- direct current-source EXE: exit 0, schema 1, read-only, 6 pass, 2 warning,
  0 fail, stderr 0 bytes, no persisted self-test;
- invalid arguments: JSON error and exit code 64 without WPF;
- solo default regression: `267/267`, 0 failures, exit 0, stderr 0 bytes,
  258.7 seconds.

Evidence root:

`D:\OpenVisionLab-TestData\Labelling_Application\artifacts\headless-environment-self-test-20260801`

## Boundary

- This is the first product CLI command, not a batch inference, training,
  archive, labeling, or model-adoption CLI.
- The actual Main Viewer graphics capability remains a UI-context check and is
  intentionally not claimed by headless execution.
- Clean-source packaged behavior is now separately verified in
  `docs/P0C_CLEAN_SOURCE_TRANSFER_BUNDLE_0_1_2_20260801.md`; hosted CI and the
  external GPU/UI target run remain separate evidence.

```text
Status: Complete
Scope: Current-source --environment-self-test --json command with read-only JSON output and bounded exit codes.
Acceptance criteria: Pre-WPF dispatch -> pass; valid JSON -> pass; no durable writes -> pass; no window -> pass; invalid arguments -> exit 64 JSON; existing diagnostics regression -> pass; full regression -> 267/267.
Verification: Isolated build, --headless-environment-self-test, --runtime-diagnostics-contract, direct EXE run, solo default suite, documentation and whitespace gates.
Evidence: Program.cs, WpfHeadlessRuntimeCommandService.cs, WpfRuntimeDiagnosticsService.cs, tests, and the D-drive evidence root.
Boundary / next dependency: Clean-source 0.1.2 packaged CLI behavior is separately verified; no external GPU/UI clean-target claim; other product CLI commands require separate operator workflow contracts.
```
