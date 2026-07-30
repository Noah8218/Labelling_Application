# P0-C Windows Sandbox Setup

Date: 2026-07-30 KST

## Purpose

Use the current Windows 10 Pro development PC as both the host and the
clean-machine evidence machine without installing the product on the host.
Windows Sandbox creates a disposable clean Windows instance on every launch.

This setup proves the portable release and later unsigned engineering
installer behavior. It does not prove code signing, customer policy
compatibility, production-model accuracy, or physical deployment hardware.

## Host Readiness

The checked host has:

- Windows 10 Pro 64-bit, build 19045;
- AMD Ryzen 5 2600 with firmware virtualization enabled;
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

The generated Sandbox contract is:

- release bundle mapped to `C:\P0C\Release` as read-only;
- evidence folder mapped to `C:\P0C\Evidence` as read-write;
- P0-C automation harness mapped to `C:\P0C\Harness` as read-only;
- networking disabled by default;
- vGPU, microphone, camera, printer, and clipboard redirection disabled;
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

Networking can be enabled only for a separately justified runtime test:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\New-P0CWindowsSandboxConfig.ps1 `
  -EnableNetworking
```

## Portable ZIP Evidence Checklist

Inside Windows Sandbox:

1. Confirm that Visual Studio and the .NET SDK were not installed by this
   workflow.
2. Open `C:\P0C\Release`.
3. Verify the manifest source commit and `source.dirty: false`.
4. Run `OpenVisionLab.LabelingStudio.exe`.
5. Run the packaged environment self-test.
6. Create or open a disposable test Recipe and image set.
7. Save one label and reopen it.
8. Export and import one portable project archive using Sandbox-local paths.
9. Exercise one abnormal-close current-image recovery case.
10. Confirm the read-only release mapping did not receive logs, settings, or
    mutable application data.
11. Copy screenshots, exported support ZIP, and written observations into
    `C:\P0C\Evidence`.
12. Close Windows Sandbox only after the evidence is visible in the host
    `sandbox-evidence` directory.

Closing the Sandbox destroys all guest files outside the mapped evidence
directory. Start the `.wsb` file again for a new clean run.

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

## Closure Record

```text
Status: Incomplete
Scope: Reproducible Windows Sandbox preparation for P0-C portable-package and later unsigned installer evidence.
Acceptance criteria: Supported host detected; release is mapped read-only; evidence is isolated read-write; networking and redirections default off; launch remains explicit.
Verification: Generate and XML-validate the .wsb file from a current clean-source release, then launch it and complete the portable evidence checklist.
Evidence: scripts/New-P0CWindowsSandboxConfig.ps1, this setup document, and generated artifacts/p0c-clean-machine/<timestamp>.
Boundary / next dependency: The user must allow the Windows feature/restart if Sandbox does not launch, then perform or authorize the interactive guest evidence run.
```
