[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$EvidenceRoot = "",

    [switch]$EnableHyperV
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path `
        $repoRoot `
        "artifacts\p0c-clean-machine\hyperv-host-preparation"
}

$resolvedEvidenceRoot = [System.IO.Path]::GetFullPath($EvidenceRoot)
$reportPath = Join-Path $resolvedEvidenceRoot "hyperv-host-preparation.json"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Convert-FeatureState {
    param([int]$InstallState)

    switch ($InstallState) {
        1 { "Enabled" }
        2 { "Disabled" }
        3 { "Absent" }
        default { "Unknown" }
    }
}

function Get-FeatureSummary {
    param([string]$Name)

    $escapedName = $Name.Replace("'", "''")
    $feature = Get-CimInstance `
        Win32_OptionalFeature `
        -Filter "Name='$escapedName'" `
        -ErrorAction SilentlyContinue

    if ($null -eq $feature) {
        return [ordered]@{
            name = $Name
            installState = "Unknown"
            installStateCode = $null
        }
    }

    return [ordered]@{
        name = $Name
        installState = Convert-FeatureState ([int]$feature.InstallState)
        installStateCode = [int]$feature.InstallState
    }
}

function Get-HostSnapshot {
    $operatingSystem = Get-CimInstance Win32_OperatingSystem
    $computerSystem = Get-CimInstance Win32_ComputerSystem
    $processor = Get-CimInstance Win32_Processor | Select-Object -First 1
    $videoControllers = @(
        Get-CimInstance Win32_VideoController |
            ForEach-Object {
                [ordered]@{
                    name = [string]$_.Name
                    driverVersion = [string]$_.DriverVersion
                    adapterRamBytes = [long]$_.AdapterRAM
                }
            })
    $systemDrive = Get-CimInstance `
        Win32_LogicalDisk `
        -Filter "DeviceID='$($operatingSystem.SystemDrive)'"

    return [ordered]@{
        capturedAtUtc = [DateTime]::UtcNow.ToString("O")
        isAdministrator = Test-IsAdministrator
        operatingSystem = [ordered]@{
            caption = [string]$operatingSystem.Caption
            version = [string]$operatingSystem.Version
            buildNumber = [string]$operatingSystem.BuildNumber
            architecture = [string]$operatingSystem.OSArchitecture
        }
        hardware = [ordered]@{
            processor = [string]$processor.Name
            secondLevelAddressTranslation = [bool]$processor.SecondLevelAddressTranslationExtensions
            virtualizationFirmwareEnabled = [bool]$processor.VirtualizationFirmwareEnabled
            vmMonitorModeExtensions = [bool]$processor.VMMonitorModeExtensions
            hypervisorPresent = [bool]$computerSystem.HypervisorPresent
            totalMemoryBytes = [long]$computerSystem.TotalPhysicalMemory
            systemDrive = [string]$operatingSystem.SystemDrive
            systemDriveFreeBytes = [long]$systemDrive.FreeSpace
            videoControllers = $videoControllers
        }
        features = @(
            Get-FeatureSummary "Microsoft-Hyper-V-All"
            Get-FeatureSummary "VirtualMachinePlatform"
            Get-FeatureSummary "Containers-DisposableClientVM"
        )
        management = [ordered]@{
            getVmAvailable = [bool](Get-Command Get-VM -ErrorAction SilentlyContinue)
            vmmsServiceAvailable = [bool](Get-Service vmms -ErrorAction SilentlyContinue)
        }
    }
}

$before = Get-HostSnapshot
$enablement = [ordered]@{
    requested = [bool]$EnableHyperV.IsPresent
    executed = $false
    restartNeeded = $false
    result = "Not requested"
}

if ($EnableHyperV.IsPresent) {
    if (-not (Test-IsAdministrator)) {
        throw @"
Hyper-V enablement requires an elevated PowerShell session.
No feature change or restart was performed.
"@
    }

    $hyperVFeature = $before.features |
        Where-Object { $_.name -eq "Microsoft-Hyper-V-All" } |
        Select-Object -First 1

    if ($hyperVFeature.installState -eq "Enabled") {
        $enablement.result = "Already enabled"
    }
    elseif ($PSCmdlet.ShouldProcess(
        "Windows optional feature Microsoft-Hyper-V",
        "Enable with NoRestart")) {
        $featureResult = Enable-WindowsOptionalFeature `
            -Online `
            -FeatureName Microsoft-Hyper-V `
            -All `
            -NoRestart
        $enablement.executed = $true
        $enablement.restartNeeded = [bool]$featureResult.RestartNeeded
        $enablement.result = [string]$featureResult.Online
    }
    else {
        $enablement.result = "Skipped by ShouldProcess"
    }
}

$after = Get-HostSnapshot
$report = [ordered]@{
    schemaVersion = 1
    operation = "P0-C Hyper-V host preparation"
    rebootCommandPresent = $false
    before = $before
    enablement = $enablement
    after = $after
    nextAction = if ($enablement.restartNeeded) {
        "Wait for explicit user approval before restarting Windows."
    }
    elseif (($after.features |
        Where-Object { $_.name -eq "Microsoft-Hyper-V-All" } |
        Select-Object -First 1).installState -eq "Enabled") {
        "Hyper-V is enabled. Verify management tools after the next approved restart."
    }
    else {
        "Run this script from an elevated PowerShell with -EnableHyperV."
    }
}

New-Item -ItemType Directory -Path $resolvedEvidenceRoot -Force | Out-Null
$report |
    ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $reportPath -Encoding UTF8

Write-Output "P0-C Hyper-V host preparation recorded."
Write-Output "Report: $reportPath"
Write-Output "Hyper-V before: $(($before.features | Where-Object name -eq 'Microsoft-Hyper-V-All').installState)"
Write-Output "Hyper-V after: $(($after.features | Where-Object name -eq 'Microsoft-Hyper-V-All').installState)"
Write-Output "Restart needed: $($enablement.restartNeeded)"
Write-Output "No restart command was executed."
