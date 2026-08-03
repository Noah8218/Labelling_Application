# P0-C GPU-Capable Clean Windows Target Validation Plan

Date: 2026-07-31 KST
Decision status: Complete
Execution status: Blocked

## Decision

The operator selected a separate GPU-capable clean Windows PC or explicitly
GPU-capable VM for the next P0-C labeling validation.

The alternative viewer compatibility/fallback project is not active. Do not
modify the SharpGL viewer, install unofficial GPU-partitioning workarounds, or
repeat Windows Sandbox/standard Hyper-V synthetic-display testing as part of
this decision.

## Local Access And Feasibility Check

Codex owns the target-side command execution. The operator is not expected to
type the PowerShell command manually.

The 2026-07-31 check found no accessible supported GPU-capable clean target:

- host: Windows 10 Pro 22H2 build 19045;
- host GPU: NVIDIA GeForce GTX 1060 3GB, driver status `OK`;
- accessible VM inventory: only `OpenVisionLab-P0C`;
- that VM has no GPU partition adapter;
- the host does not expose a partitionable GPU through
  `Get-VMHostPartitionableGpu`;
- Microsoft states that Hyper-V DDA/GPU-P is not supported on client Windows
  10/11 Pro or desktop-class hardware, and the supported GPU-P list does not
  include GTX 1060.

Official references:

- <https://learn.microsoft.com/en-us/troubleshoot/windows-server/virtualization/troubleshoot-hyper-v-gpu-assignment-partitioning-passthrough-issues>
- <https://learn.microsoft.com/en-us/windows-server/virtualization/hyper-v/gpu-partitioning>

A same-PC native-boot VHDX was also considered without changing the machine.
The host system disk is MBR/legacy boot and Secure Boot verification is not
supported in the current boot mode. The available ISO is Windows 11. Closing
that gap would require a separately approved MBR-to-GPT/UEFI boot conversion
or a different supported Windows image, plus boot-menu modification and a
user-approved restart. Do not treat that as an automatic continuation of this
GPU-target decision.

The 2026-08-03 post-reinstall recheck materially changed that historical host
condition. The host is now Windows 11 Pro build 26100 on a GPT NVMe system disk
with an EFI system partition, and the GTX 1060 driver reports `OK`. The
prepared `0.1.2` transfer ZIP still matches its expected SHA-256. Hyper-V is no
longer installed, so the rejected synthetic-display path remains inactive.

The recheck found that the historical MBR barrier had been removed, while
Secure Boot remained disabled, privileged BCD and BitLocker reads were
unavailable from the current non-elevated session, and no official Windows ISO
was found in the checked local paths. No ISO download, D-drive VHDX creation,
administrator-elevated boot-menu mutation, or restart was performed.

On 2026-08-03 the operator explicitly declined the native-boot approach. Do
not download an ISO, create a native-boot VHDX, modify BCD, or request a restart
for this P0-C path. Keep the existing actual-viewer graphics capability
preflight as the normal compatibility boundary on real Windows PCs. If another
ordinary GPU-equipped Windows PC becomes available later, the prepared
portable bundle may be validated there without reopening native boot,
standard Hyper-V, Windows Sandbox, or unofficial GPU-partitioning work.

Evidence:

- `artifacts/p0c-clean-machine/gpu-target-local-inventory.json`;
- `artifacts/p0c-clean-machine/native-boot-host-prerequisites.json`;
- `artifacts/p0c-clean-machine/native-boot-feasibility-cleanup.json`;
- `D:\OpenVisionLab-TestData\Labelling_Application\artifacts\p0c-clean-machine\native-boot-host-prerequisites-20260803.json`.

## Exact External Prerequisite

Before execution, provide access to one of these targets:

- a separate clean Windows 10/11 x64 physical PC with its supported GPU driver
  installed; or
- a clean Windows 10/11 x64 VM whose supported configuration presents a real
  GPU/OpenGL-capable display adapter to the guest.

The validation must run through the target's local console or VM console.
Record any remote-session use because RDP or another remoting layer can change
the graphics path and invalidate an OpenGL conclusion.

The target must allow the operator to:

- copy and extract the portable ZIP into a writable folder;
- run Windows PowerShell 5.1 scripts as the signed-in standard user;
- capture screenshots and return the bounded evidence folder;
- create a clean checkpoint first when the target is a VM.

No product code, Visual Studio, .NET SDK, Python runtime, model weight,
production image, or credential is required for this fixture-based gate.

## Prepared Portable Bundle

Current clean-source host path:

`artifacts/p0c-clean-machine/gpu-target-transfer-0.1.2-59e37d8/OpenVisionLab-P0C-GPU-Target-0.1.2-59e37d8.zip`

Expected SHA-256:

```text
CFB7BE9D5055AD45B51411CF10D273749059CD98B5B20A930E4D15991DE39C1F
```

When the clean target becomes directly accessible, Codex will:

1. verify the ZIP SHA-256;
2. extract its contents directly into an empty `C:\P0C` folder;
3. open Windows PowerShell as the signed-in standard user;
4. execute:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass `
     -File C:\P0C\Invoke-P0CSandboxLabelWorkflowSmoke.ps1
   ```

The script name retains its original Sandbox history, but its path parameters
are target-independent. It copies the `0.1.2` release to
`C:\P0C\WorkingRelease`,
verifies the manifest, and writes results under `C:\P0C\Evidence`.

The operator does not need to type this command. Run it only once from a clean
extraction. Do not delete or overwrite a prior
`WorkingRelease` to force a retry; preserve the failed evidence and start a
new clean extraction instead.

### Current-Source Preflight Version Boundary

The current prepared bundle is clean-source `0.1.2` from commit `59e37d8`. It
contains the completed current-source graphics capability preflight and the
read-only headless environment command. Its deterministic package and transfer
verification are recorded in
`docs/P0C_CLEAN_SOURCE_TRANSFER_BUNDLE_0_1_2_20260801.md`.

A self-contained `0.1.1` dirty-source engineering package now proves the new
preflight and post-launch package immutability on the current host. Its
manifest records `source.dirty=true`, and it has no approved target-transfer
ZIP, so it must not be used for this clean-target gate. Read
`docs/ENGINEERING_RELEASE_0_1_1_GRAPHICS_PREFLIGHT_EVIDENCE_20260731.md`.

The clean-source package prerequisite is now satisfied. Preserve the immutable
`0.1.0` historical evidence bundle and local dirty-source `0.1.1` engineering
evidence; do not overwrite either with `0.1.2`. Direct access to the selected
GPU-capable clean target is the remaining prerequisite for execution.

## Included Validation

Use a deliberately versioned clean-source self-contained `win-x64` release
that contains the completed `viewerGraphics` preflight. Do not start this gate
with immutable `0.1.0` or dirty-source engineering `0.1.1`.

The gate must prove, in order:

1. target identity and graphics driver evidence;
2. transfer ZIP and release-manifest SHA-256 verification;
3. every manifest-declared payload file verified in a writable target-local
   copy (the current `0.1.2` candidate contains `504` payload files);
4. packaged first launch;
5. real detection dataset creation;
6. fixture image display without an OpenGL/SharpGL exception;
7. one ordinary axis-aligned rectangle;
8. explicit label save;
9. non-empty YOLO label file;
10. normal application close;
11. packaged application relaunch;
12. the same Recipe/dataset/image reopened with the saved rectangle visible;
13. staged release payload unchanged after the workflow.

The existing UI Automation harness may prove steps 3 through 10. It now moves
the packaged EXE to the active monitor with the smallest `Left` bound, verifies
that the resulting window intersects that monitor, and writes
`monitor-placement.json`. Reopen is a separate required check until the
harness records explicit relaunch and saved-object restoration evidence.

## Target Readiness Record

Before launching the product, record:

- Windows edition, version, build, and x64 architecture;
- physical PC or VM platform and console/session type;
- display-adapter name;
- GPU driver provider, version, and date;
- Device Manager/display-adapter warning state;
- `dxdiag` output;
- whether the machine had any OpenVisionLab files before extraction;
- clean-VM checkpoint name when applicable.

Do not use adapter name or `dxdiag` alone as proof that SharpGL works. The
actual fixture image rendering in the packaged application is the capability
gate.

## Required Evidence To Return

Return one evidence folder containing:

- target-readiness JSON and `dxdiag.txt`;
- transfer ZIP SHA-256 and release manifest;
- `monitor-placement.json` with monitor name/bounds, actual window bounds, and
  the single-monitor fallback when applicable;
- `label-workflow-progress.json`;
- `label-workflow-result.json`;
- successful image/box/save screenshot;
- the non-empty YOLO label file;
- persisted `VISION.xml`;
- close/relaunch/reopen screenshot;
- package-integrity result after save/reopen;
- application diagnostics/logs or an explicit support bundle;
- a short operator record naming the target, console/session type, start/end
  time, and any warning or intervention.

## Fail-Closed Rules

- Any SharpGL/OpenGL exception is a failed image-display criterion.
- A created dataset without a visible image is not a labeling pass.
- A UI Automation click without a non-empty persisted label is not a save
  pass.
- A saved file without a relaunch and visible restored rectangle is not a
  reopen pass.
- Do not install a workaround or change the graphics/session mode mid-run
  without ending the run and recording a new target configuration.
- Do not claim physical deployment, production accuracy, long-run stability,
  or installer/signing readiness from this fixture-based portable run.

## Execution Checklist

```text
[ ] GPU-capable clean target is available
[ ] Local/VM console session is identified
[ ] GPU driver and dxdiag evidence are captured
[ ] Clean checkpoint exists when target is a VM
[ ] Transfer ZIP SHA-256 matches
[ ] 504/504 release payload hashes pass
[ ] EXE window intersects the dynamically selected leftmost monitor
[ ] Packaged application launches
[ ] Dataset is created
[ ] Fixture image renders without SharpGL/OpenGL error
[ ] One ordinary rectangle is visible
[ ] Explicit label save creates a non-empty YOLO label
[ ] Application closes normally
[ ] Application relaunches
[ ] Same image reopens with the saved rectangle visible
[ ] Package payload remains unchanged
[ ] Evidence folder is returned
```

## Closure Record

```text
Status: Blocked
Scope: Execute the selected GPU-capable clean Windows target labeling validation without changing the viewer implementation.
Acceptance criteria: Target/driver evidence; all 504 payload hashes verified; leftmost-monitor placement evidence; image display; one rectangle; explicit save; non-empty label; normal close; relaunch; saved-image reopen; unchanged package.
Verification: Target-side UI Automation plus console screenshots, persisted artifacts, diagnostics/logs, and package hash comparison.
Evidence: docs/P0C_GPU_CAPABLE_CLEAN_TARGET_VALIDATION_PLAN_20260731.md; execution evidence does not yet exist.
Boundary / next dependency: Direct access from this Codex workspace to a supported GPU-capable clean Windows PC or VM. Codex will execute the prepared command and recover the evidence; the operator does not need to run it manually.
```
