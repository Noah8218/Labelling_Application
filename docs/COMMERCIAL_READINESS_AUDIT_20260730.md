# Commercial Readiness Audit

Date: 2026-07-30 KST  
Source baseline: `4c6718a9b75465a640f480b6f3403ec08ff07436`  
Branch: `main`

## 1. Outcome

The current source can build, pass the complete internal regression suite,
publish both framework-dependent and self-contained `win-x64` folders, open
the published WPF application, and complete the configured local YOLO smoke.

This does not establish a commercial release. The output has no stable
release version, deterministic-build contract, file hashes, bundled
LICENSE/NOTICE/third-party notices, signature, installer, upgrade/uninstall
evidence, clean-machine evidence, packaged runtime self-test, crash recovery,
portable project archive, or headless product CLI.

Audit conclusion: `Local publish baseline verified; commercial release
contract missing.`

## 2. Scope And Boundary

Included:

- current Git, OS, .NET SDK, and PowerShell policy;
- isolated build and complete default regression;
- current `publish-win-x64.ps1` in its default and `-SelfContained` modes;
- published WPF launch and configured local YOLO smoke;
- package contents, version, signature, manifest, legal notices, dependencies,
  logs, diagnostics, runtime bundle, recovery/archive, CLI, and CI;
- selection of one next implementation slice.

Excluded:

- production-code changes;
- installer selection or implementation;
- code signing;
- clean-machine installation;
- production model accuracy, long-run stability, or takt-time claims.

## 3. Environment

| Item | Observed value |
| --- | --- |
| Windows | `Microsoft Windows NT 10.0.19045.0`, x64 |
| Active .NET SDK | `10.0.301` |
| CI SDK policy | `actions/setup-dotnet` with `8.0.x` |
| Target framework | `net8.0-windows` |
| PowerShell execution policy | `Restricted` |
| Product baseline | focused workstation `4.0/5`; editor `3.4/5` |

There is no repository `global.json`, so developer and CI SDK selection is not
the same explicit contract.

## 4. Executed Gates

| Gate | Result | Evidence |
| --- | --- | --- |
| Isolated Debug test build | Pass, 0 warnings, 0 errors | `artifacts/commercial-release-audit-20260730/isolated-build.log` |
| Complete default regression, solo run | Pass, exit 0, `260/260`, no stderr | `default-suite-solo.stdout.log`, `default-suite-solo.stderr.log`, `default-suite-solo.exitcode.txt` |
| Concurrent regression attempt | Failed at anomaly purpose flow because two audit runs shared temporary state; solo rerun passed | `default-suite-complete.stderr.log` |
| Direct publish script invocation | Blocked by local `Restricted` execution policy | `publish-framework-dependent.log` |
| Default framework-dependent Release publish | Pass after explicit `-ExecutionPolicy Bypass` and cleanup of audit-owned stale SDK 8 MSBuild nodes | `publish-framework-dependent-retry.log` |
| Framework-dependent published WPF launch | Pass | `first-run-framework-dependent.log` |
| Self-contained Release publish | Pass | `publish-self-contained.log` |
| Self-contained published WPF launch | Pass | `first-run-self-contained.log` |
| Configured local YOLO smoke | Pass; one candidate, 13,280 ms, local external Python/project/weights | `first-run-self-contained-with-yolo.log` |
| Publish DEV-path guard and required-file checks | Pass in both successful publish runs | publish logs |

The first framework-dependent retry failed because parallel MSBuild could not
write `OpenVisionLab.Localization.deps.json` while an SDK 8 node from the audit
test run still held it. After the audit-owned node was stopped, the same
script passed. This is a build isolation/reliability gap, not a product-runtime
failure.

## 5. Published Output

| Property | Framework-dependent | Self-contained |
| --- | ---: | ---: |
| File count | 47 | 501 |
| Total bytes | 99,062,270 | 263,463,549 |
| Runtime | requires .NET 8 Desktop Runtime | includes .NET 8.0.28 desktop runtime |
| Assembly/file/product version | `1.0.9707.30353` | `1.0.9707.30353` |
| Authenticode | Not signed | Not signed |
| LICENSE | Missing | Missing |
| NOTICE | Missing | Missing |
| Third-party notices | Missing | Missing |
| Manifest hashes | Missing | Missing |
| WPF main window | Opened | Opened |

The final folder left by the audit is the self-contained output at
`artifacts/publish/Release/win-x64`. The framework-dependent measurements were
preserved before that folder was replaced:

- `artifacts/commercial-release-audit-20260730/framework-dependent-inventory.log`;
- `artifacts/commercial-release-audit-20260730/self-contained-inventory.log`.

The version originates from `[assembly: AssemblyVersion("1.0.*")]`.
`OpenVisionLab.LabelingStudio.csproj` also explicitly sets
`<Deterministic>false</Deterministic>`. A rebuild therefore does not have a
stable release identity or a declared same-source reproducibility contract.

The current `publish-manifest.txt` records relative path and byte length only.
It does not record SHA-256, source commit/dirty state, product version, SDK,
RID, publish mode, or creation tool version.

## 6. Dependency And Legal Inventory

The exact resolved application graph contains 20 NuGet packages. The resolved
licenses include MIT, Apache-2.0, legacy .NET license URLs, and YamlDotNet's
package license file. Evidence:

- `artifacts/commercial-release-audit-20260730/package-inventory.log`;
- `artifacts/commercial-release-audit-20260730/package-license-inventory.log`.

The output also contains checked-in/native binaries including
`CircularProgressBar.dll`, `Lib.Common.dll`, `Lib.OpenCV.dll`, `SharpGL.dll`,
`SharpGL.WinForms.dll`, `WinFormAnimation.dll`, `OpenCvSharpExtern.dll`, and
`opencv_videoio_ffmpeg455_64.dll`. Their current file identity and SHA-256 were
recorded in `native-and-checked-in-binary-inventory.log`.

Repository `LICENSE` and `NOTICE` cover this project, but neither is copied to
the publish output. There is no generated third-party notice or SBOM. This
audit does not assert redistribution compliance for the checked-in binaries,
OpenCV native components, Python, CUDA, or model weights.

## 7. Runtime, Diagnostics, And Logs

Verified:

- four Python worker scripts are copied to publish;
- the local runtime config check finds the current machine's Python, YOLO
  source tree, weights, and sample image;
- the application writes runtime logs and the log panel can open the log
  directory;
- `OVLog` contains minidump and retention/file-policy APIs.

Missing or partial:

- no Python executable, environment, CUDA runtime, model weight, runtime config,
  or packaged first-run script is included;
- the passing YOLO smoke depends on paths under the current development
  machine and is not clean-machine evidence;
- the application has no packaged environment/self-test command;
- no one-action diagnostic/support bundle or log export exists;
- `Program.Main` ignores its command-line arguments;
- unhandled application exceptions are caught at the top level and only their
  message is logged; there is no verified nonzero failure exit, structured
  crash report, or recovery journal;
- minidump and retention/file-policy APIs have no production call site;
- the smoke-created log tree contains multiple empty category files and an
  anomalous nested `ALL...ALL.log.log` path, so log packaging/retention is not
  a completed support contract.

## 8. Recovery, Archive, Installer, And CI

| Area | Classification | Evidence/boundary |
| --- | --- | --- |
| Safe explicit close | Verified | existing completed product contract |
| Crash recovery journal | Missing | no app-level recovery implementation |
| Portable project archive | Missing | dataset interchange archives do not constitute a complete Recipe/project round trip |
| Installer | Missing | no WiX/MSIX/Inno/NSIS/installer project |
| Upgrade/uninstall | Missing | no versioned install identity or clean-machine evidence |
| Code signing | Missing/external prerequisite | published EXE is `NotSigned`; credential/policy required |
| Headless product CLI | Missing | `Program.Main` accepts but ignores `args` |
| CI Debug build/docs | Partial pass contract | current workflow builds tests and runs docs smoke |
| CI complete regression | Missing | default 260 suite is not run |
| CI Release publish/WPF artifact | Missing | no publish, package, artifact upload, or EXE gate |
| Clean-machine first run | Blocked | approved clean Windows VM/PC not supplied |
| Production accuracy/stability/takt | Blocked | field data, thresholds, runtime/weights, and hardware not supplied |

The concurrent default-suite failure also proves that the complete regression
cannot safely be run more than once against shared temporary state. CI should
run one isolated suite instance until the anomaly fixture receives a separate
isolation fix.

## 9. Finding Priority

### P0: release identity and provenance are missing

- active SDK is not pinned;
- version is time-derived;
- deterministic build is disabled;
- self-contained mode is optional rather than a release contract;
- manifest lacks hashes and source/build identity;
- legal/third-party files are absent.

### P1: runtime support and failure diagnosis are incomplete

- no packaged self-test;
- no diagnostic/log bundle;
- top-level crash behavior and minidump/recovery are not integrated;
- log path/retention output needs a bounded contract.

### P1 external: install lifecycle evidence is absent

- installer, signing, upgrade/uninstall, and clean-machine verification require
  product decisions or external prerequisites.

### P2 external: field adoption evidence is absent

- current YOLO smoke proves local integration only.

## 10. Decision

The next single implementation slice is:

`P0-B1 Versioned Deterministic Self-Contained Release Bundle Contract`.

Read `docs/NEXT_DEVELOPMENT_DECISION_20260730.md` for the exact included scope,
acceptance criteria, exclusions, and verification.

## 11. Commands

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug /nr:false -m:1 /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -Configuration Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-win-x64.ps1 -Configuration Release -SelfContained
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-first-run.ps1 -Configuration Release -SkipBuild -SkipTests -RunPublishWpfSmoke
dotnet list .\OpenVisionLab.LabelingStudio.csproj package --include-transitive
```

## 12. Durable Closure

```text
Status: Complete
Scope: Current-source commercial release baseline audit without production-code changes.
Acceptance criteria: Current build/default regression/Release publish/first-run were executed; publish contents and all requested release/runtime/recovery/CLI/CI/legal areas were classified; one next implementation slice was selected.
Verification: Isolated build 0 warnings/errors; solo default suite 260/260; framework-dependent and self-contained publish pass; both published WPF smokes pass; configured local YOLO smoke pass; evidence logs and inventories recorded.
Evidence: docs/COMMERCIAL_READINESS_AUDIT_20260730.md and artifacts/commercial-release-audit-20260730.
Boundary / next dependency: This proves local publishability, not installation, redistribution compliance, crash recovery, clean-machine readiness, signing, or production accuracy.
```
