# Engineering Release 0.1.1 Graphics Preflight Evidence

Date: 2026-07-31 KST
Status: Complete

## Outcome

A deliberately versioned self-contained `win-x64` engineering package now
contains the current-source Main Viewer graphics preflight. The published
`0.1.1` EXE launched on the current Windows 10 Pro host, ran the operator
environment check through the real WPF command surface, and reported:

- product version `0.1.1`;
- overall environment status `pass`;
- `8` passed, `0` warning, `0` failed checks;
- NVIDIA GeForce GTX 1060, OpenGL `4.6.0 NVIDIA 560.94`;
- all 11 framebuffer functions required by the SharpGL viewer available.

The package was published twice from the same source state. Both release
manifests and both human-readable publish manifests were byte-identical. After
the packaged EXE ran and closed, fail-closed package verification still passed.

This completes the local engineering-package and package-immutability slice.
It does not complete the external GPU-capable clean-Windows label/save/reopen
gate.

## Package Identity

- package folder:
  `artifacts/publish/Release/win-x64/0.1.1`;
- source commit recorded by the manifest:
  `ed682b26d40d804dbfff84a2ffb405f9f1abab40`;
- source dirty flag: `true`;
- .NET SDK: `8.0.421`;
- runtime: `win-x64`;
- self-contained: `true`;
- payload: `503` files, `264,913,152` bytes;
- package file count including both manifests: `505`.

The dirty flag is intentional evidence, not a clean-source release claim. No
commit or push was authorized during this slice.

## Deterministic Publish Evidence

Two consecutive publishes produced:

| Artifact | SHA-256 | Match |
| --- | --- | --- |
| `release-manifest.json` | `519B437294F80F7B694A1E5AB16CEFB9AB07B8CC68B30533FBD3F3A6E14D1B4D` | yes |
| `publish-manifest.txt` | `80B6CA7F966844136DB11786B049CC43FCC93C3611B66F53167D471F6DE5731A` | yes |

Copies from both runs are preserved under:

`artifacts/release-package-0.1.1-20260731-final`.

## Package-Immutability Defect And Correction

The first packaged smoke exposed a real defect: an untouched startup queue
caused `YoloImageReviewStatusService` to create
`DATA/review-status.json` containing only `[]` beside the packaged EXE. The
post-launch manifest verification correctly failed on that unlisted file.

The correction preserves dataset-owned review-state persistence:

- when no review state is persistable and no cache exists, `SaveReviewStatus`
  does not create an empty file or directory;
- when a cache already exists and all persisted state is reset, it can still be
  rewritten to `[]`;
- non-empty review state remains stored under the active dataset root.

The focused YOLO review-state test now proves both the no-new-empty-file and
existing-cache-clear contracts.

After the correction and fresh republish:

- packaged EXE launch and environment self-test passed;
- package-local `DATA` did not exist;
- package-local `review-status.json` did not exist;
- diagnostics, logs, and localization state were written only below the
  isolated current-user application-data root;
- `publish-win-x64.ps1 -VerifyOnly` passed after EXE close.

## Verification

Commands actually run:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj `
  -c Debug /nr:false -m:1 /p:UseSharedCompilation=false `
  /p:OutDir=artifacts\isolated-out\

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll `
  --yolo-image-review-status

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\publish-win-x64.ps1 `
  -Configuration Release -ReleaseVersion 0.1.1 -SelfContained

dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll `
  --exe-runtime-graphics-smoke `
  --exe .\artifacts\publish\Release\win-x64\0.1.1\OpenVisionLab.LabelingStudio.exe `
  --expected-version 0.1.1 `
  --app-data-root .\artifacts\release-package-0.1.1-20260731-final\packaged-app-data `
  --output .\artifacts\release-package-0.1.1-20260731-final\packaged-runtime-graphics.png

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\publish-win-x64.ps1 `
  -Configuration Release -ReleaseVersion 0.1.1 -SelfContained -VerifyOnly
```

Results:

- isolated Debug build: pass, 0 warnings and 0 errors;
- focused YOLO review-state regression: pass;
- first and second `0.1.1` publish: pass;
- deterministic manifest equality: pass;
- published WPF EXE graphics smoke: pass;
- post-launch full manifest verification: pass;
- final default internal regression: `264/264` pass, 0 failures;
- `--priority-workflow-docs`: pass;
- `git diff --check`: pass.

The first default-suite attempt encountered the existing 500,000-ROI
mouse-event performance threshold during a transient slow run (`2442 ms`).
The unchanged focused gate then passed three consecutive times (`20.167 ms`,
`20.443 ms`, `20.365 ms`). A second full run passed that gate at `57.388 ms`
but later encountered a temporary fake-YOLO test-process file lock during
cleanup. That unchanged focused smoke also passed three consecutive retries.
The final solo run completed all `264` tests with no failure and an empty
stderr log:

`artifacts/full-suite-0.1.1-final-retry.stdout.log`

## Visual And Structured Evidence

- published EXE UI:
  `artifacts/release-package-0.1.1-20260731-final/packaged-runtime-graphics.png`;
- structured self-test:
  `artifacts/release-package-0.1.1-20260731-final/packaged-app-data/Diagnostics/self-test-latest.json`;
- first/second release and publish manifests:
  `artifacts/release-package-0.1.1-20260731-final`.

The screenshot is from the final republished `0.1.1` EXE, not from the earlier
pre-fix package and not from a direct current-source view instantiation.

## Boundary And Next Dependency

The `0.1.1` package records `source.dirty=true`; therefore it is an engineering
candidate and must not be used as clean-source P0-C release evidence. The next
GPU-capable clean-target run requires:

1. direct access to the selected supported GPU-capable clean Windows PC or VM;
2. a deliberately committed, clean-source versioned package containing this
   preflight;
3. a newly recorded transfer ZIP and manifest SHA-256;
4. the complete image-visible -> rectangle -> explicit save -> close/relaunch
   -> saved rectangle visible evidence loop.

Installer lifecycle/signing and independent production-data validation remain
separate.

```text
Status: Complete
Scope: Deterministic self-contained 0.1.1 engineering package, real published-EXE graphics self-test, empty-review-cache immutability correction, and post-launch manifest verification.
Acceptance criteria: Two publishes byte-identical -> pass; product/version/renderer/11 functions visible in packaged UI and structured diagnostics -> pass; package creates no DATA/review-status.json -> pass; all manifest files and hashes remain valid after launch -> pass.
Verification: Isolated Debug build; --yolo-image-review-status; two Release publishes; --exe-runtime-graphics-smoke; publish-win-x64.ps1 -VerifyOnly; final default suite 264/264; --priority-workflow-docs; git diff --check.
Evidence: docs/ENGINEERING_RELEASE_0_1_1_GRAPHICS_PREFLIGHT_EVIDENCE_20260731.md; artifacts/release-package-0.1.1-20260731-final.
Boundary / next dependency: This is a dirty-source engineering candidate, not clean-machine release evidence. Direct GPU-capable clean-target access and a clean-source versioned transfer bundle are still required.
```
