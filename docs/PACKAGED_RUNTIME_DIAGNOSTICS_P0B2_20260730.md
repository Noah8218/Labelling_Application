# P0-B2 Packaged Runtime Diagnostics And Support Bundle

Date: 2026-07-30 KST  
Status: Complete

## 1. Outcome

The versioned `0.1.0` Windows package now owns a bounded, explicit support
workflow:

- startup diagnostics and log configuration use a predictable current-user
  writable root;
- the package folder is not mutated during first launch;
- `설정/도구 -> 진단/지원 -> 환경 점검` reports package, file, framework,
  architecture, manifest, and writable-path health without starting training
  or inference;
- `지원 자료 만들기` creates one privacy-reviewed ZIP and immediately shows
  its file name and exclusion policy;
- retention is bounded by age, count, and stored bytes;
- the support ZIP is built from a fixed allow-list rather than a recursive
  copy of application or project folders.

This completes the P0-B2 contract selected after the commercial release audit
and P0-B1 package baseline.

## 2. Writable Path Contract

Default root:

`%LOCALAPPDATA%\OpenVisionLab\LabelingStudio`

Test and controlled-host override:

`OPENVISIONLAB_LABELING_APP_DATA_ROOT`

Owned children:

| Path | Ownership |
| --- | --- |
| `Config` | Current-user localization catalog and language state |
| `Logs` | log4net rolling application logs |
| `Diagnostics` | Structured startup and explicit self-test JSON |
| `SupportBundles` | Explicitly requested support ZIP files |

If the current-user root cannot be initialized, startup attempts an isolated
temporary root. It does not fall back to writing diagnostics into the package
folder.

Logging initialization is delayed until the writable root is known. Legacy
application-state constructors no longer pre-create unused package-relative
folders, and read-only dataset readiness validation probes the nearest
existing parent without creating the configured dataset root.

## 3. Retention Contract

- Logs: 30 days, 20 MB per rolling file, 10 backups.
- Structured diagnostics: 30 days, at most 50 JSON files and 20 MB.
- Support bundles: 30 days, at most 20 ZIP files and 250 MB total.
- One bundle includes at most 20 recent log files and 5 MB of sanitized log
  text.

Retention cleanup is best-effort. A locked file does not block application
startup or delete unrelated data.

## 4. Environment Self-Test

The explicit self-test records:

- product identity and version;
- product DLL and packaged EXE presence;
- release-manifest product/version agreement when packaged;
- diagnostics and support-directory write/delete health;
- log isolation from the application folder;
- OS, .NET runtime, and process architecture.

Missing packaged-EXE or release-manifest files are warnings in a development
or test host and become normal package checks in the published EXE. The test
does not connect to Python, load weights, run a model, train, infer, inspect a
dataset, or modify labels.

## 5. Support Bundle Privacy Contract

Allowed entries:

- `support-manifest.json`;
- `self-test.json`;
- `config-summary.json`;
- `release-manifest.json` when present;
- the latest redacted startup diagnostic;
- bounded, redacted recent `.log` text.

Excluded by default:

- dataset and source images;
- labels, annotations, masks, and metadata sidecars;
- model weights;
- Recipe/project content;
- raw runtime configuration and credentials;
- memory dumps.

The generated configuration summary contains only support policy and product
runtime settings such as retention, collection mode, and telemetry state. It
does not copy `labeling-runtime.local.json` or another raw configuration file.
Sanitization removes credential assignments, user-profile roots, and absolute
Windows paths from included diagnostic/log text.

## 6. Ownership

- `WpfRuntimeDiagnosticsService`: paths, startup records, self-test, retention,
  redaction, and allow-list ZIP creation.
- `WpfRuntimeDiagnosticsViewModel`: explicit commands and visible result state.
- `WpfLabelingShellWindow.xaml`: the discoverable `진단/지원` surface.
- `OVLog`: delayed appender initialization after the writable root is known.
- `OpenVisionLanguageService`: configured current-user catalog root.
- `YoloDatasetValidator`: read-only writable-parent probing without target
  creation.

No diagnostics policy was placed in view code-behind.

## 7. Verification

Commands and results:

1. Isolated test-project build:
   - 0 warnings;
   - 0 errors.
2. `--runtime-diagnostics-contract`:
   - pass;
   - structured self-test save/reopen passed;
   - age/count retention passed;
   - allow-list export/reopen passed;
   - image/label/weight/raw-config exclusion passed;
   - path/credential redaction passed;
   - read-only dataset readiness non-creation passed.
3. Current-source 1920x1080 visual smoke:
   - `환경 점검` and `지원 자료 만들기` are visible with familiar icons,
     tooltips, and accessible names;
   - explicit export updates the visible status card.
4. `scripts/publish-win-x64.ps1`:
   - versioned self-contained package republished and verified;
   - 503 payload files, 264,718,540 payload bytes;
   - package manifest SHA-256
     `B8EEE699F190406DC7FC65A0095EF1C59CD9E1BDC7B0744D206BC67FD1AE5C2B`.
5. Current packaged-EXE smoke:
   - published EXE created a main window;
   - one startup diagnostic and the localization catalog were written under
     the isolated user-data root;
   - no `Log`, `CONFIG`, `DATA`, `IMAGE`, `SAVE_IMAGE`, `RECIPE`, or `CAPTURE`
     directory appeared in the package;
   - fail-closed package verification still passed after launch.
6. Final repository gates:
   - isolated build;
   - focused diagnostics contract;
   - release-package contract;
   - WPF shell/visual smoke;
   - priority-document contract;
   - default internal suite;
   - `git diff --check`.

Visual evidence:

- before:
  `artifacts/p0b2-runtime-diagnostics-20260730/before-header-tools-screen.png`;
- after surface:
  `artifacts/p0b2-runtime-diagnostics-20260730/after-header-tools-screen.png`;
- after explicit export:
  `artifacts/p0b2-runtime-diagnostics-20260730/after-support-bundle-screen.png`.

Packaged runtime evidence:

`artifacts/p0b2-runtime-diagnostics-20260730/packaged-exe-app-data`

## 8. Boundary

This contract does not add or prove:

- telemetry or automatic upload;
- cloud support or collaboration;
- automatic training or inference;
- installer, upgrade/uninstall, or code signing;
- crash recovery or portable project archive;
- Python/CUDA/model-weight redistribution;
- clean-machine operability;
- production accuracy, stability, or takt time.

P0-C clean-machine installation evidence remains blocked until an approved
clean Windows environment and installer/signing decisions are available.

```text
Status: Complete
Scope: Current-user startup diagnostics/log/config routing, bounded retention, explicit environment self-test, privacy-safe allow-list support ZIP, visible one-action UI, and packaged first-launch immutability.
Acceptance criteria: Package/path health self-test without model work -> pass; structured startup diagnostics outside package -> pass; bounded retention -> pass; support ZIP manifest/config/self-test/redacted logs -> pass; images/labels/weights/credentials/datasets excluded -> pass; save/export/reopen tests -> pass; current packaged-EXE launch and post-launch manifest verification -> pass.
Verification: Zero-warning isolated build; --runtime-diagnostics-contract; current-source visual smoke; versioned Release publish; packaged EXE launch; post-launch -VerifyOnly; release/package, documentation, default-suite, and diff gates listed above.
Evidence: docs/PACKAGED_RUNTIME_DIAGNOSTICS_P0B2_20260730.md; artifacts/p0b2-runtime-diagnostics-20260730; artifacts/publish/Release/win-x64/0.1.0.
Boundary / next dependency: No telemetry, cloud, implicit model execution, installer/signing, clean-machine, recovery/archive, runtime redistribution, or production-quality claim. P0-C requires an approved clean Windows environment and release lifecycle decisions.
```
