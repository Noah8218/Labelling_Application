# P0-C Hyper-V Clean-Windows Labeling Evidence

Date: 2026-07-31 KST
Status: Incomplete

## Outcome

The clean-source `0.1.0` self-contained `win-x64` release was exercised in a
new Windows 11 Generation 2 Hyper-V VM on the approved Windows 10 Pro host.
The full VM reproduced the same SharpGL framebuffer failure previously seen
in Windows Sandbox.

The following clean-guest preparation and package steps passed:

- official Windows 11 25H2 Korean x64 ISO SHA-256 verification:
  `9F39A222AD4A96BD5BBB18AFE7B5EED583DD18622B225DBAB478C363C4019642`;
- non-overwriting Generation 2 VM creation with fixed `8 GB` RAM, 4 virtual
  processors, dynamic `80 GB` VHDX, Secure Boot, vTPM, Default Switch, and
  automatic checkpoints disabled;
- installed Windows boot from VHD with the installation ISO detached;
- clean installed-Windows checkpoint
  `P0C-Clean-Windows-Installed-20260731`;
- exact transfer ZIP SHA-256
  `1DCA67F42CF25E0A1CB0C001FAA252624600183F593911F96719BAA2B95EAA65`;
- all `503` release-manifest payload files verified inside the guest;
- packaged first launch;
- real detection dataset creation through the packaged wizard;
- Recipe name and writable output root persistence;
- COCO128 fixture copy and image-open request.

The viewer then raised:

```text
Extension function glGenFramebuffersEXT not supported
```

The guest harness failed closed. Its workflow result reports that the canvas
label-save command remained disabled after the attempted sample box. The
guest-side failure capture proves that the image canvas had not initialized,
so this is not evidence that a box was created. No YOLO label file was
produced, and explicit save and saved-image reopen were not attempted.

## Environment And Safety Boundary

- VM: `OpenVisionLab-P0C`
- guest: clean Windows 11 installed from the verified official x64 ISO
- display path: standard Hyper-V synthetic display through VMConnect
- source release: product `0.1.0`, source commit
  `ed682b26d40d804dbfff84a2ffb405f9f1abab40`, clean source
- source package was copied into a guest-local writable working directory
  before execution
- only the public COCO128 fixture was used; no production image, credential,
  model weight, or external runtime was introduced
- the clean installed-Windows checkpoint remains available
- no unofficial GPU partitioning, host driver injection, or product fallback
  was applied

The standard Hyper-V display on this host is therefore not a valid
clean-machine environment for the current SharpGL labeling viewer. This does
not prove failure on a GPU-capable physical Windows target or an explicitly
supported GPU-capable VM.

## Evidence

Host and VM preparation:

- `artifacts/p0c-clean-machine/hyperv-post-restart/windows11-iso-verification.json`;
- `artifacts/p0c-clean-machine/hyperv-vm-preparation/hyperv-vm-result.json`;
- `artifacts/p0c-clean-machine/hyperv-post-restart/clean-windows-checkpoint.json`;
- `artifacts/p0c-clean-machine/hyperv-post-restart/clean-windows-checkpoint-postverify.json`;
- `artifacts/p0c-clean-machine/hyperv-post-restart/vmconnect-after-clean-checkpoint-restart.png`.

Guest transfer and execution:

- `artifacts/p0c-clean-machine/hyperv-guest-transfer/transfer-package.json`;
- `artifacts/p0c-clean-machine/hyperv-guest-transfer/copy-vmfile-result.json`;
- `artifacts/p0c-clean-machine/hyperv-guest-transfer/guest-runonce-restart.json`;
- `artifacts/p0c-clean-machine/hyperv-guest-transfer/vmconnect-guest-run-03.png`.

Recovered guest evidence:

- `artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731/Evidence/label-workflow-result.json`;
- `artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731/Evidence/label-workflow-progress.json`;
- `artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731/Evidence/sandbox-label-workflow-failure.png`;
- `artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731/guest-harness-stderr.txt`;
- `artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731/recovery-manifest.json`;
- `artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731/vm-restart-after-recovery.json`.

The evidence recovery used a graceful Hyper-V shutdown, mounted the current
differencing VHDX read-only, copied only the bounded guest evidence, detached
the VHDX, and restarted the VM. The final VM state is `Running` with heartbeat
`OkApplicationsUnknown`.

## Selected Direction For The Next Viewer Attempt

On 2026-07-31, the operator selected a separate GPU-capable clean Windows PC
or explicitly GPU-capable VM for the next validation attempt. The viewer
compatibility/fallback project is not active.

Execution criteria and the exact evidence-return contract are in
`docs/P0C_GPU_CAPABLE_CLEAN_TARGET_VALIDATION_PLAN_20260731.md`.

Do not repeat Windows Sandbox or standard Hyper-V synthetic-display viewer
testing unless the host, guest display capability, or viewer implementation
changes. Installer lifecycle and code signing remain separate P0-C decisions.
Independent production-data validation still requires approved field data,
thresholds, runtime/weights, and intended target hardware.

## Closure Record

```text
Status: Incomplete
Scope: Clean Windows 11 Hyper-V preparation, checkpoint, portable package transfer, manifest verification, packaged first launch, dataset creation, and SharpGL labeling preflight.
Acceptance criteria: Official ISO and VM contract -> pass; clean checkpoint and VHD boot -> pass; 503 manifest payload hashes -> pass; packaged first launch and dataset creation -> pass; image display -> fail with glGenFramebuffersEXT not supported; ordinary box/save/reopen -> not reached.
Verification: Fresh Hyper-V state/checkpoint queries, guest UI Automation replay, guest-side failure capture, read-only VHD evidence recovery, artifact SHA-256 inventory, and final VM restart/heartbeat check.
Evidence: docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md and artifacts/p0c-clean-machine/hyperv-guest-evidence-20260731 plus the referenced host/transfer artifacts.
Boundary / next dependency: The GPU-capable clean-target path is selected. Access to that target and return of its bounded evidence folder are required before repeating and closing the labeling gate. Installer/signing and production-data validation remain separate.
```
