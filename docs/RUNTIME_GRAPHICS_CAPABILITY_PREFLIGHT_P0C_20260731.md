# P0-C Runtime Graphics Capability Preflight

Date: 2026-07-31 KST
Status: Complete

## Outcome

OpenVisionLab Labeling Studio now checks the graphics capability of the actual
Main Viewer OpenGL context before an image is opened. A definite unsupported
result is reported through `설정/도구 -> 진단/지원`, included in an explicit
support bundle, and blocks the central image-load path before label or
annotation state is changed.

This completes the current-source graphics-preflight slice. It does not
complete the broader P0-C clean-machine labeling gate.

## User Contract

- `환경 점검` reports the OpenGL vendor, renderer, version, and viewer-graphics
  result without starting training or inference.
- The probe uses the Main Viewer's attached `ImageCanvasControl`; it does not
  substitute an unrelated OpenGL capability test.
- The following 11 framebuffer functions used by the current SharpGL viewer
  must be available:
  - `glGenFramebuffersEXT`
  - `glBindFramebufferEXT`
  - `glFramebufferTexture2DEXT`
  - `glCheckFramebufferStatusEXT`
  - `glDeleteFramebuffersEXT`
  - `glGenRenderbuffersEXT`
  - `glBindRenderbufferEXT`
  - `glRenderbufferStorageEXT`
  - `glFramebufferRenderbufferEXT`
  - `glDeleteRenderbuffersEXT`
  - `glGenerateMipmapEXT`
- A definite missing-function or probe failure blocks image opening, changes
  the dataset status to `이미지 뷰어 환경 확인 필요`, writes an actionable log,
  and shows a warning dialog when the operator window is visible.
- The warning tells the operator to use a local Windows session or a
  GPU/OpenGL-capable VM console and points to `진단/지원`.
- A context that is not ready during early startup or a headless test is a
  warning, not a false permanent failure. It does not block startup restore.
- A pass or definite failure is cached for the window session; a warning is
  retried when the viewer becomes ready.
- Restoring or running the check never opens an image, saves a label, creates a
  layer, starts a model, or starts training/inference.

## Ownership

- `Services/Runtime/WpfOpenGlRuntimeCapabilityProbe.cs` owns the actual viewer
  context and required-function probe.
- `WpfRuntimeDiagnosticsService` owns the structured `viewerGraphics` check and
  support-bundle inclusion.
- `WpfRuntimeDiagnosticsViewModel` owns caching and operator-facing readiness
  state.
- `WpfLabelingShellWindow.ImageLoading.cs` owns the fail-closed central
  image-load boundary.
- `WpfLabelingShellWindow.xaml` keeps the explicit operator entry point visible
  under `설정/도구`.

## Included And Excluded Scope

Included:

- current-source capability probe;
- explicit diagnostics and support-export integration;
- central image-load prevention for a definite unsupported result;
- actionable status, log, and modal guidance;
- deterministic supported, warning, and failure tests;
- current-source before/after UI evidence.

Excluded:

- replacing or weakening the SharpGL viewer;
- a software renderer or OpenGL compatibility fallback;
- GPU driver installation or VM GPU configuration;
- installer, upgrade/uninstall, or signing;
- republishing or silently replacing the immutable `0.1.0` package;
- clean-machine label save/reopen proof;
- production accuracy, stability, or takt-time claims.

## Acceptance Evidence

Current-source baseline: Git `ed682b2`, plus the documented working-tree
implementation in this slice.

Acceptance checklist:

- [x] Isolated Debug build completed with 0 warnings and 0 errors.
- [x] `--runtime-diagnostics-contract` passed supported, early-warning, and
      deterministic missing-`glGenFramebuffersEXT` behavior.
- [x] The current host's actual Main Viewer reported NVIDIA GeForce GTX 1060,
      OpenGL 4.6.0, and all 11 required functions.
- [x] A deterministic unsupported environment appeared as one failed
      `viewerGraphics` check with actionable detail.
- [x] The central image-load path refused the deterministic unsupported
      environment and displayed the operator warning.
- [x] Current-source 1920x1080 captures were generated after the final source
      change.
- [x] The final default internal regression suite passed `264/264`.

UI evidence:

- before:
  `artifacts/ui/runtime-graphics-preflight-20260731/before.png`;
- supported current host:
  `artifacts/ui/runtime-graphics-preflight-20260731/after-supported.png`;
- deterministic failed diagnostics:
  `artifacts/ui/runtime-graphics-preflight-20260731/after-blocked.png`;
- deterministic central image-load block:
  `artifacts/ui/runtime-graphics-preflight-20260731/after-image-load-blocked-dialog.png`.

The deterministic failure captures prove presentation and fail-closed routing;
they are not a new Hyper-V or clean-machine execution.

## Release And P0-C Boundary

The prepared `0.1.0` ZIP used by the Sandbox and Hyper-V evidence predates this
current-source implementation. It must not be described as containing the new
preflight.

A separate follow-up produced a deliberate self-contained `0.1.1` engineering
package containing this implementation. Its published EXE passed `8/8`
environment checks on the current GTX 1060 host, reported all 11 functions,
and retained a valid full manifest after launch. The manifest records
`source.dirty=true`, so it proves local packaged behavior and immutability but
is not clean-source P0-C target evidence. See
`docs/ENGINEERING_RELEASE_0_1_1_GRAPHICS_PREFLIGHT_EVIDENCE_20260731.md`.

The broader P0-C labeling slice remains blocked until a supported GPU-capable
clean Windows PC or VM is directly accessible. That run must still prove:

`image visible -> rectangle drawn -> explicit save -> application relaunch ->
same image and rectangle visible`.

Installer lifecycle/signing and independent production-data validation remain
separate decisions.

```text
Status: Complete
Scope: Actual Main Viewer OpenGL capability probe, structured viewerGraphics diagnostics/support evidence, and fail-closed central image-load guidance.
Acceptance criteria: Actual supported host reports renderer/version and 11 required functions -> pass; early unavailable context remains non-blocking -> pass; deterministic missing function appears in diagnostics and blocks image opening before state mutation -> pass; current-source UI evidence -> pass.
Verification: Isolated Debug build; --runtime-diagnostics-contract; current-source 1920x1080 visual smoke captures; final default suite, --priority-workflow-docs, and git diff --check recorded in the work-tracking closure.
Evidence: docs/RUNTIME_GRAPHICS_CAPABILITY_PREFLIGHT_P0C_20260731.md; artifacts/ui/runtime-graphics-preflight-20260731.
Boundary / next dependency: No viewer fallback, immutable 0.1.0 package replacement, clean-machine labeling pass, installer/signing claim, or field-quality claim. Local 0.1.1 packaged behavior is verified separately, but a directly accessible GPU-capable clean target and a clean-source versioned package are required for the next packaged labeling gate.
```
