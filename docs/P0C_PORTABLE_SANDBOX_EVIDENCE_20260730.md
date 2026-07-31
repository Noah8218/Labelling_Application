# P0-C Portable Windows Sandbox Evidence

Date: 2026-07-30 KST
Status: Incomplete

2026-07-31 follow-up: the approved full Windows 11 Hyper-V VM was created and
the labeling preflight was executed. The standard Hyper-V synthetic display
reproduced `glGenFramebuffersEXT not supported`. See
`docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md`. The prerequisite section
below records the historical state at the close of 2026-07-30.

## Outcome

The clean-source `0.1.0` self-contained `win-x64` release was exercised in a
new Windows Sandbox instance on the approved Windows 10 Pro host.

The following portable-package slice passed:

- exact release contract `0.1.0` from source commit
  `ed682b26d40d804dbfff84a2ffb405f9f1abab40`;
- packaged first launch without a .NET SDK or Visual Studio in the guest;
- explicit environment self-test: `7` pass, `0` warning, `0` fail;
- explicit support ZIP creation;
- support ZIP inclusion of the active packaged-process logs with redaction;
- `505` release files unchanged before and after the diagnostic run;
- normal packaged-application close.

The actual labeling slice reached all of these steps in a writable
Sandbox-local extraction of the same release:

- `503` manifest payload files copied and SHA-256 verified;
- new detection dataset created through the real wizard;
- Recipe name and writable dataset root applied;
- fixture image copied into `data/train/images`;
- configured image queue command invoked.

It did not pass image display, box creation, label save, or reopen. The
SharpGL viewer raised:

```text
Extension function glGenFramebuffersEXT not supported
```

The failure reproduced with:

1. vGPU disabled and Protected Client enabled;
2. vGPU enabled and Protected Client enabled;
3. vGPU enabled and the standard client.

This is evidence that Windows Sandbox on this host is suitable for packaged
startup, diagnostics, support export, and package-integrity checks, but is not
a valid clean-machine environment for the current SharpGL labeling viewer.
It is not evidence that the same viewer fails on a full Windows VM or target
workstation.

## Evidence

Primary passing artifacts:

- `artifacts/p0c-clean-machine/20260730-live-log-fix-after/sandbox-evidence/portable-smoke-result.json`;
- `artifacts/p0c-clean-machine/20260730-live-log-fix-after/sandbox-evidence/sandbox-packaged-runtime-diagnostics.png`;
- `artifacts/p0c-clean-machine/20260730-live-log-fix-after/sandbox-evidence/SupportBundles/OpenVisionLab-Support-20260730-213757-1816.zip`;
- `artifacts/p0c-clean-machine/20260730-live-log-fix-after/sandbox-evidence/Diagnostics`;
- `artifacts/p0c-clean-machine/20260730-live-log-fix-after/sandbox-evidence/Logs`.

Preserved failure progression:

- `label-workflow-result-harness-contains-failure.json`: Windows PowerShell
  5.1 harness incompatibility, corrected without a product change;
- `label-workflow-result-harness-dataset-name-failure.json`: overly strict
  automation-name assertion, corrected to use persisted dataset evidence;
- `label-workflow-result-canvas-save-disabled.json`: first indication that no
  active image had opened;
- `label-workflow-result-readonly-release.json`: proved that the read-only
  release mapping is a pristine source, not a usable location for explicitly
  created Recipe data;
- `label-workflow-result-vgpu-disabled.json`;
- `label-workflow-result-vgpu-protected-client.json`;
- `label-workflow-result-vgpu-standard-client.json`;
- matching `sandbox-label-workflow-failure-*.png` captures.

The COCO128 fixture and its copied guest-input source were both `56,556`
bytes with SHA-256:

```text
9008ddd846fc6002ec47ea6067cb92cf70f294773cc15765b6a14ffb392f52fa
```

No production image, credential, model weight, or external runtime was used.

## Harness Contract

`scripts/New-P0CWindowsSandboxConfig.ps1` keeps the source release and harness
read-only and the evidence directory read-write. It supports:

- default hardened diagnostic mode: vGPU off, Protected Client on;
- `-EnableVGpu` for a graphics-capability probe;
- `-UseStandardClient` for the Microsoft-default client isolation mode;
- `-LogonCommand` for a repeatable guest harness launch;
- networking and clipboard redirection off unless separately justified.

`scripts/p0c/Invoke-P0CSandboxLabelWorkflowSmoke.ps1`:

- copies the pristine mapped release to a writable guest-local extraction;
- verifies every manifest payload SHA-256 before launch;
- uses stable UI Automation IDs for the dataset and labeling workflow;
- requires a non-empty YOLO label, persisted Recipe config, unchanged release
  payload, guest screenshot, and normal close before it can pass.

The harness currently fails closed at the unavailable OpenGL capability. It
does not call label save after the canvas fails to enter a dirty state.

## Full-VM Prerequisite (Historical 2026-07-30 State)

The checked host has firmware virtualization, sufficient RAM, and sufficient
free disk. Full Hyper-V was enabled on 2026-07-30 through
`Prepare-P0CHyperVHost.ps1 -EnableHyperV`; Windows returned
`RestartNeeded: true`, and no restart command was executed. Management
commands and services remain unavailable until the user-approved restart. No
Windows ISO/VHD/VHDX installation source is available.

The official Windows 11 download page was checked on 2026-07-30. It listed
Windows 11 `25H2` and Korean x64 SHA-256
`9F39A222AD4A96BD5BBB18AFE7B5EED583DD18622B225DBAB478C363C4019642`.
The operator still owns the Microsoft terms, edition/language selection, and
download; generated links expire after 24 hours.

At the close of 2026-07-30, the required continuation was:

1. obtain explicit user approval for the pending host restart;
2. after restart, verify Hyper-V management commands and `vmms`;
3. provide or approve download of an official x64 Windows ISO;
4. use the non-overwriting VM contract to reserve a dynamic `80 GB` VHDX and
   `8 GB` guest RAM;
5. create a Generation 2 VM without automatically starting it;
6. install guest integration/display support and verify the OpenGL capability
   before repeating the labeling harness;
7. keep installer technology and code-signing decisions separate.

The full VM gate was defined to prove:

- portable extract and manifest verification;
- image display without the SharpGL extension failure;
- dataset create/open;
- queue load and image open;
- one ordinary box;
- explicit label save and saved-image reopen;
- portable project archive export/import;
- one bounded abnormal-close recovery replay.

The 2026-07-31 follow-up reached package launch, dataset creation, and the
image-open request, but the standard synthetic display failed the image
display criterion. The remaining items were therefore not reached.

## Closure Record

```text
Status: Incomplete
Scope: Clean Windows Sandbox portable startup/diagnostics/package evidence plus NoRestart full-Hyper-V host preparation.
Acceptance criteria: Sandbox package-safe criteria -> pass; writable extraction/dataset creation -> pass; Hyper-V optional feature Enabled with RestartNeeded true and no restart command -> pass; full-VM image/box/save/reopen -> pending.
Verification: Sandbox evidence set plus artifacts/p0c-clean-machine/hyperv-host-preparation/hyperv-host-preparation.json and PowerShell 5.1 contract checks.
Evidence: docs/P0C_PORTABLE_SANDBOX_EVIDENCE_20260730.md, artifacts/p0c-clean-machine/20260730-live-log-fix-after, and artifacts/p0c-clean-machine/hyperv-host-preparation.
Boundary / next dependency: This records the 2026-07-30 Sandbox state. The user-approved restart, official ISO, VM creation, and Hyper-V preflight were completed on 2026-07-31; the standard synthetic display reproduced the SharpGL failure. Current next dependency is in docs/P0C_HYPERV_LABELING_EVIDENCE_20260731.md.
```

Recommended model: `gpt-5.6-terra`
Reasoning effort: `high`
