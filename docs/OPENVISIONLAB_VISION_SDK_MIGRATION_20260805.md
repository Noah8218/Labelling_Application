# OpenVisionLab Vision SDK Migration

Date: 2026-08-05 KST
Status: Complete

## Source Contract

- Authoritative library: `OpenVisionLab-Vision-SDK`.
- SDK product version: `3.0.0`.
- SDK source commit: `ba0055b713e0bf434b9d0a7fd3f4b0e445c1f982`.
- The SDK `src` tree was clean when the consumer assemblies were built. Local
  SDK test-only changes were not modified or absorbed into this repository.

The Labeling Studio consumes versioned checked-in SDK assemblies so a clean
checkout and CI build do not depend on a sibling source-tree path. Future SDK
updates must refresh these assemblies intentionally from a verified SDK build.

## Ownership Change

| Responsibility | Previous owner | Current owner |
| --- | --- | --- |
| Common geometry and conversion helpers | `Lib.Common.dll` | `OpenVisionLab.Core.dll` or the app's UI adapter |
| OpenCV matching and result contracts | `Lib.OpenCV.dll` | `OpenVisionLab.Vision2D.dll` |
| Bitmap/Mat UI conversion | legacy common library | aligned `OpenCvSharp4.Extensions` package |
| Template-matching options | app-local `IOpenCVPropertyMatching` implementation | SDK `MatchingToolProperty` |
| Matching execution result lifetime | ignored legacy return value | disposed SDK `VisionToolResult` |

The app keeps only UI-framework-specific adaptation. Algorithm policy,
properties, results, geometry helpers, and OpenCV tool behavior stay with the
Vision SDK.

## Runtime Dependency Contract

Checked-in SDK payload:

| File | Version | SHA-256 |
| --- | --- | --- |
| `OpenVisionLab.Core.dll` | `3.0.0.0` | `57130FDAA0CC6D47DDE90420FB6786EC86795D6980528284E0DEE38F6A3C1276` |
| `OpenVisionLab.Vision2D.dll` | `3.0.0.0` | `02645C66BA89D759CB5BE720949261823D4FD60913C2DC5819B25EF3DF492C06` |

`Lib.Common.dll` and `Lib.OpenCV.dll` are removed. `OpenCvSharp.Blob.dll` is
not redistributed because no active Labeling Studio SDK call uses the SDK Blob
API. The app continues to own one aligned managed/native OpenCvSharp runtime at
`4.5.5.20211231`; SDK migration must not overwrite that runtime with a second
managed or native version.

## Preserved Behavior

- Template matching keeps the previous score, candidate-count, magnification,
  Canny, no-angle-search, and pyramid-proposal settings.
- A valid no-match remains a successful run with zero candidates; other SDK
  validation/execution failures return an actionable failed result.
- SDK tools and execution results are disposed according to the SDK ownership
  contract.
- Bitmap/Mat conversion remains an app UI boundary and does not make the SDK
  depend on WPF or WinForms.
- Recipe directory creation, display conversion, detection overlays, viewer
  image loading, and measurement geometry preserve their existing contracts.

## Verification

- SDK Release solution build: passed with 0 warnings and 0 errors.
- SDK smoke suite: passed `142/142`.
- Labeling Studio isolated test build: passed with 0 warnings and 0 errors.
- Template batch labeling: passed 3 focused tests.
- Current-image template no-candidate workflow: passed 2 focused tests.
- Detection overlay/display focused checks: passed.
- Dependency/output contract: only the two required SDK assemblies are present;
  no `Lib.*` or unused Blob DLL is present.
- Solo protected regression: passed `267/267` in 329.7 seconds.
- Versioned self-contained engineering publish `0.1.3`: passed publish,
  manifest/payload SHA-256 verification, notice/package inventory, deliberate
  tamper rejection, and exact restore verification. It contains `505` manifest
  payloads; manifest SHA-256 is
  `248D35136D19FBE4D24CB7C2A85E33A892736ECDEF69C3142A0DF86DBB3533E4`.
  The historical immutable `0.1.0` package was not overwritten.

## Upgrade Checklist

1. Record the SDK version and source commit; require a clean SDK product-source
   tree.
2. Run the SDK Release build and complete SDK smoke suite.
3. Refresh only SDK assemblies used by the app and record their hashes.
4. Prefer the SDK's public property/result/tool types; do not duplicate its
   contracts inside the app.
5. Preserve the app-owned OpenCvSharp managed/native version unless a separate
   ABI migration is approved and verified.
6. Run focused template, display, geometry, output-inventory, documentation,
   and complete regression gates.
7. Update release notices and this contract before packaging.

## Durable Closure

```text
Status: Complete
Scope: Replaced the active Lib.Common/Lib.OpenCV consumer contract with OpenVisionLab Vision SDK 3.0.0 assemblies and public APIs while retaining app-owned UI adapters and the pinned OpenCvSharp runtime.
Acceptance criteria: Old project/runtime dependencies removed; required SDK assemblies and APIs used; behavior-preserving focused tests pass; full protected regression passes; notices and durable direction are updated.
Verification: SDK Release build, SDK 142/142 smoke, Labeling Studio zero-warning isolated build, focused template/detection checks, output inventory, solo 267/267 regression, and versioned 0.1.3 self-contained release-package contract.
Evidence: OpenVisionLab.LabelingStudio.csproj, migrated consumers, checked-in SDK assembly hashes above, tests/LabelingApplication.Tests/Program.cs, and this completion record.
Boundary / next dependency: The 0.1.3 package is dirty-source engineering evidence, not a clean-source release candidate. This work does not prove field model accuracy, a new installer/signing contract, or the separate P0-C GPU-capable clean-target labeling gate.
```
