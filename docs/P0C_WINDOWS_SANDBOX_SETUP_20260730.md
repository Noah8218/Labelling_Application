# P0-C Windows Sandbox Setup

Date: 2026-07-30 KST

## Purpose

Use the current Windows 10 Pro development PC as both the host and the
clean-machine evidence machine without installing the product on the host.
Windows Sandbox creates a disposable clean Windows instance on every launch.

This setup proves the portable release's startup, diagnostics, support export,
and package integrity. On the checked host it does not prove SharpGL viewer
operation or an end-to-end labeling workflow. It also does not prove code
signing, customer policy compatibility, production-model accuracy, or
physical deployment hardware.

## Host Readiness

The checked host has:

- Windows 10 Pro 64-bit, build 19045;
- AMD Ryzen 5 2600 with firmware virtualization enabled;
- NVIDIA GeForce GTX 1060 3 GB on the host;
- 23.9 GB RAM;
- 162.2 GB free on the system drive at the readiness check;
- the Microsoft hypervisor running;
- `WindowsSandbox.exe` present.

The repository does not contain a WiX, Inno Setup, NSIS, or MSIX installer at
this checkpoint. Portable-bundle evidence therefore runs first. Installer
technology remains a separate explicit decision.

## One-Time Windows Setup

1. Open Start and search for `Windows Sandbox`.
2. If it opens, close it. The feature is ready.
3. If it does not open, start PowerShell as Administrator and run:

   ```powershell
   Enable-WindowsOptionalFeature -Online `
     -FeatureName Containers-DisposableClientVM `
     -All
   ```

4. Restart Windows when requested.
5. Open Windows Sandbox once from Start to confirm it launches.

Do not install Hyper-V Manager or download a Windows ISO for the first P0-C
portable-package pass. Windows Sandbox owns the disposable guest.

## Prepare The Current Release

Run from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\publish-win-x64.ps1 `
  -Configuration Release
```

The release manifest must report:

- `runtimeIdentifier: win-x64`;
- `selfContained: true`;
- `source.dirty: false`.

The publish contract verifies every payload SHA-256 before the Sandbox setup
is generated.

## Generate The Disposable Test Environment

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\New-P0CWindowsSandboxConfig.ps1
```

The command creates an ignored artifact directory containing:

- `OpenVisionLab-P0C.wsb`;
- `p0c-host-context.json`;
- a writable `sandbox-evidence` directory.

The default generated Sandbox contract is:

- release bundle mapped to `C:\P0C\Release` as read-only;
- evidence folder mapped to `C:\P0C\Evidence` as read-write;
- P0-C automation harness mapped to `C:\P0C\Harness` as read-only;
- networking disabled by default;
- vGPU, microphone, camera, printer, and clipboard redirection disabled;
- Protected Client enabled;
- 8 GB guest memory;
- no automatic application run or label operation.

Double-click `OpenVisionLab-P0C.wsb`. The release folder opens automatically,
but the operator explicitly starts the application.

For the repeatable first-launch, environment-self-test, support-bundle, and
package-immutability smoke, run inside Sandbox:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File C:\P0C\Harness\Invoke-P0CSandboxPortableSmoke.ps1
```

The harness uses the packaged UI Automation IDs to open `설정/도구`, run
`환경 점검`, create the privacy-safe support bundle, capture the packaged
window inside the guest, compare every release file hash before and after,
and copy only diagnostic evidence into `C:\P0C\Evidence`. It does not create
or save a label, start inference/training, or alter a Recipe.

For repeatable automation, `-LogonCommand` can start one mapped harness after
Sandbox login. Keep complex logic in the mapped script:

```powershell
$command = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\P0C\Harness\Invoke-P0CSandboxPortableSmoke.ps1'
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\New-P0CWindowsSandboxConfig.ps1 `
  -LogonCommand $command
```

`-EnableVGpu` and `-UseStandardClient` are capability-probe options. The latter
disables Protected Client and returns to Microsoft's standard client mode.
They do not enable networking or clipboard redirection:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\New-P0CWindowsSandboxConfig.ps1 `
  -EnableVGpu `
  -UseStandardClient
```

The current SharpGL viewer still failed with
`glGenFramebuffersEXT not supported` in that most permissive tested Sandbox
graphics/client combination. Do not repeat the Sandbox label-save loop unless
the host, Sandbox implementation, or viewer capability requirement changes.

Networking can be enabled only for a separately justified runtime test:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\New-P0CWindowsSandboxConfig.ps1 `
  -EnableNetworking
```

## Portable ZIP Evidence Checklist

Inside Windows Sandbox, complete only the package-safe slice:

1. Confirm that Visual Studio and the .NET SDK were not installed by this
   workflow.
2. Open `C:\P0C\Release`.
3. Verify the manifest source commit and `source.dirty: false`.
4. Run `OpenVisionLab.LabelingStudio.exe`.
5. Run the packaged environment self-test.
6. Copy the pristine release to a writable guest-local extraction before any
   explicit Recipe creation.
7. Confirm the read-only release mapping did not receive logs, settings, or
    mutable application data.
8. Copy screenshots, exported support ZIP, and written observations into
    `C:\P0C\Evidence`.
9. Close Windows Sandbox only after the evidence is visible in the host
    `sandbox-evidence` directory.

Closing the Sandbox destroys all guest files outside the mapped evidence
directory. Start the `.wsb` file again for a new clean run.

The following checks must run in a full Windows VM or approved physical
machine with the required OpenGL framebuffer capability:

1. display a real image;
2. create one ordinary box;
3. explicitly save and reopen its YOLO label;
4. export/import one portable project archive;
5. replay one bounded abnormal-close recovery case.

## Safety Boundaries

- Never map the repository root or a user dataset folder into Sandbox.
- Keep the release mapping read-only.
- Write only to the dedicated evidence mapping.
- Keep networking disabled for portable and installer lifecycle tests unless
  an external runtime dependency is explicitly under test.
- Do not use production images or credentials in the disposable guest.
- Do not call the portable pass installer, upgrade, uninstall, signing, or
  customer-environment completion evidence.

## Installation-Lifecycle Follow-Up

After portable evidence passes, define an unsigned internal engineering
installer. A single Sandbox session can perform:

1. clean installation;
2. first launch;
3. previous-version to next-version upgrade;
4. uninstall;
5. user Recipe/label/dataset preservation check.

Close and reopen the `.wsb` file to repeat the sequence from a clean Windows
state. Code signing remains a separate external-release gate.

## Full Hyper-V VM Follow-Up

The current PC has full Hyper-V enabled. After the user performed the approved
host restart, Hyper-V management and `vmms` became available. The official
Windows 11 25H2 Korean x64 ISO was verified, the Generation 2 VM was created,
Windows was installed, and clean checkpoint
`P0C-Clean-Windows-Installed-20260731` was preserved.

The reusable host preparation command is:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Prepare-P0CHyperVHost.ps1
```

To enable Hyper-V, open PowerShell as Administrator and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Prepare-P0CHyperVHost.ps1 `
  -EnableHyperV
```

The script calls `Enable-WindowsOptionalFeature` with `-All -NoRestart`,
records before/after feature state, and contains no restart command. The
2026-07-30 evidence is:

`artifacts/p0c-clean-machine/hyperv-host-preparation/hyperv-host-preparation.json`

Official references:

- Microsoft Hyper-V enablement:
  <https://learn.microsoft.com/en-us/windows-server/virtualization/hyper-v/get-started/Install-Hyper-V?context=%2Fvirtualization%2Fhyper-v-on-windows%2Fcontext%2Fcontext&pivots=windows>
- Microsoft Windows 11 x64 ISO:
  <https://www.microsoft.com/software-download/windows11>
- Microsoft Generation 2 VM guidance:
  <https://learn.microsoft.com/en-us/windows-server/virtualization/hyper-v/plan/Should-I-create-a-generation-1-or-2-virtual-machine-in-Hyper-V>

As checked on 2026-07-30, Microsoft's page identifies the current release as
Windows 11 `25H2`, offers the x64 multi-edition ISO for VM installation, and
publishes this Korean 64-bit SHA-256:

```text
9F39A222AD4A96BD5BBB18AFE7B5EED583DD18622B225DBAB478C363C4019642
```

The operator must select the edition/language and accept Microsoft's download
terms on the official page. Generated download links expire after 24 hours.
Recheck the page's current release and hash if the download occurs later; do
not use an unofficial mirror or third-party ISO retrieval script.

The completed sequence was:

1. obtain the official x64 Windows ISO and verify its published SHA-256;
2. rerun `Prepare-P0CHyperVHost.ps1` without `-EnableHyperV` and verify
   `Get-VM` plus `vmms` are available;
3. create, but do not automatically start, the fail-closed Generation 2 VM:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass `
     -File .\scripts\New-P0CHyperVVm.ps1 `
     -IsoPath "C:\Path\To\Official-Windows-x64.iso"
   ```

4. inspect the recorded ISO SHA-256, VM path, `8 GB` fixed guest memory,
   4 processors, dynamic `80 GB` VHDX, Secure Boot, virtual TPM, and disabled
   automatic checkpoints;
5. start the VM after that inspection, install Windows, and create a
   named clean checkpoint before copying the release;
6. transfer the clean-source portable release and run the guest
   graphics/OpenGL labeling preflight.

The VM creation script refuses to overwrite an existing VM or target
directory, requires at least `90 GB` free before creating the default dynamic
disk, and does not restart the host. The checked host had approximately
`166 GB` free before Hyper-V activation.

Hyper-V creation alone does not prove that the guest exposes the framebuffer
extension required by SharpGL. The standard synthetic display failed that
preflight with `glGenFramebuffersEXT not supported` after all `503` manifest
payload files, package launch, and dataset creation passed. See
`docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md`.

Do not repeat the standard Hyper-V or Sandbox viewer loop unless display
capability or viewer implementation changes. Choose an explicitly supported
GPU-capable clean Windows target or separately approve a viewer compatibility
fallback; do not apply an unofficial GPU-partitioning workaround silently.

## Closure Record

```text
Status: Incomplete
Scope: Reproducible Windows Sandbox package-safe evidence, Hyper-V host preparation, fail-closed Generation 2 VM creation/install/checkpoint, and clean-guest SharpGL labeling preflight.
Acceptance criteria: Sandbox startup/self-test/support/package integrity -> pass; Hyper-V management/official ISO/VM/install/checkpoint -> pass; guest payload verification/package launch/dataset creation -> pass; SharpGL image display -> fail with glGenFramebuffersEXT not supported; box/save/reopen -> not reached.
Verification: XML-validated .wsb files; packaged UI automation; release hashes; Sandbox graphics-mode attempts; PowerShell 5.1 parser/ASCII checks; elevated Hyper-V state queries; guest-side failure capture; read-only VHD evidence recovery.
Evidence: docs/P0C_PORTABLE_SANDBOX_EVIDENCE_20260730.md, docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md, scripts/Prepare-P0CHyperVHost.ps1, scripts/New-P0CHyperVVm.ps1, artifacts/p0c-clean-machine/hyperv-host-preparation, and artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731.
Boundary / next dependency: Another viewer attempt requires either a supported GPU-capable clean Windows target or an explicitly scoped viewer compatibility fallback. Installer/signing remain separate.
```
