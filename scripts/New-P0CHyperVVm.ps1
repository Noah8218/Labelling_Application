[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$IsoPath,

    [string]$VmName = "OpenVisionLab-P0C",

    [string]$VmRoot = "C:\Hyper-V",

    [ValidateRange(4, 16)]
    [int]$MemoryGB = 8,

    [ValidateRange(2, 8)]
    [int]$ProcessorCount = 4,

    [ValidateRange(60, 160)]
    [int]$VhdMaxGB = 80,

    [ValidateRange(80, 240)]
    [int]$RequiredFreeGB = 90,

    [string]$SwitchName = "",

    [switch]$Start
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$evidenceRoot = Join-Path `
    $repoRoot `
    "artifacts\p0c-clean-machine\hyperv-vm-preparation"
$resultPath = Join-Path $evidenceRoot "hyperv-vm-result.json"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-IsAdministrator)) {
    throw "Run this script from an elevated PowerShell session."
}

foreach ($commandName in @(
    "Get-VM",
    "Get-VMSwitch",
    "New-VM",
    "New-VHD",
    "Set-VM",
    "Set-VMMemory",
    "Set-VMProcessor",
    "Set-VMFirmware",
    "Set-VMKeyProtector",
    "Enable-VMTPM",
    "Add-VMDvdDrive",
    "Start-VM")) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        throw "Hyper-V management command is unavailable after restart: $commandName"
    }
}

$resolvedIsoPath = [System.IO.Path]::GetFullPath($IsoPath)
if (-not (Test-Path -LiteralPath $resolvedIsoPath -PathType Leaf)) {
    throw "Windows ISO was not found: $resolvedIsoPath"
}
if (-not [string]::Equals(
    [System.IO.Path]::GetExtension($resolvedIsoPath),
    ".iso",
    [StringComparison]::OrdinalIgnoreCase)) {
    throw "The installation source must be an ISO file."
}

$resolvedVmRoot = [System.IO.Path]::GetFullPath($VmRoot)
$vmDirectory = Join-Path $resolvedVmRoot $VmName
$vhdDirectory = Join-Path $vmDirectory "Virtual Hard Disks"
$vhdPath = Join-Path $vhdDirectory "$VmName.vhdx"

if (Get-VM -Name $VmName -ErrorAction SilentlyContinue) {
    throw "A Hyper-V VM already exists and will not be overwritten: $VmName"
}
if (Test-Path -LiteralPath $vmDirectory) {
    throw "The VM target directory already exists and will not be overwritten: $vmDirectory"
}

$vmDriveRoot = [System.IO.Path]::GetPathRoot($resolvedVmRoot)
$driveInfo = New-Object System.IO.DriveInfo($vmDriveRoot)
$requiredFreeBytes = [long]$RequiredFreeGB * 1GB
if ($driveInfo.AvailableFreeSpace -lt $requiredFreeBytes) {
    throw "VM storage requires at least $RequiredFreeGB GB free on $vmDriveRoot."
}

if (-not [string]::IsNullOrWhiteSpace($SwitchName) -and
    -not (Get-VMSwitch -Name $SwitchName -ErrorAction SilentlyContinue)) {
    throw "Requested Hyper-V switch was not found: $SwitchName"
}

$isoHash = (Get-FileHash -LiteralPath $resolvedIsoPath -Algorithm SHA256).Hash.ToLowerInvariant()
$memoryBytes = [long]$MemoryGB * 1GB
$vhdBytes = [long]$VhdMaxGB * 1GB
$created = $false
$started = $false
$failure = $null

try {
    if ($PSCmdlet.ShouldProcess(
        "$VmName at $vmDirectory",
        "Create Generation 2 Hyper-V VM")) {
        New-Item -ItemType Directory -Path $vhdDirectory -Force | Out-Null
        New-VHD `
            -Path $vhdPath `
            -Dynamic `
            -SizeBytes $vhdBytes |
            Out-Null

        $newVmArguments = @{
            Name = $VmName
            Generation = 2
            MemoryStartupBytes = $memoryBytes
            VHDPath = $vhdPath
            Path = $vmDirectory
        }
        if (-not [string]::IsNullOrWhiteSpace($SwitchName)) {
            $newVmArguments.SwitchName = $SwitchName
        }

        New-VM @newVmArguments | Out-Null
        $created = $true
        Set-VMProcessor -VMName $VmName -Count $ProcessorCount
        Set-VMMemory -VMName $VmName -DynamicMemoryEnabled $false
        Set-VM `
            -Name $VmName `
            -AutomaticCheckpointsEnabled $false `
            -AutomaticStopAction ShutDown
        Set-VMFirmware `
            -VMName $VmName `
            -EnableSecureBoot On `
            -SecureBootTemplate MicrosoftWindows
        Set-VMKeyProtector -VMName $VmName -NewLocalKeyProtector
        Enable-VMTPM -VMName $VmName

        $dvd = Add-VMDvdDrive -VMName $VmName -Path $resolvedIsoPath -Passthru
        Set-VMFirmware -VMName $VmName -FirstBootDevice $dvd

        if ($Start.IsPresent) {
            Start-VM -Name $VmName | Out-Null
            $started = $true
        }
    }
}
catch {
    $failure = $_.Exception.GetType().Name + ": " + $_.Exception.Message
}

$result = [ordered]@{
    schemaVersion = 1
    recordedAtUtc = [DateTime]::UtcNow.ToString("O")
    status = if ([string]::IsNullOrWhiteSpace($failure)) { "prepared" } else { "failed" }
    failure = $failure
    vm = [ordered]@{
        name = $VmName
        generation = 2
        directory = $vmDirectory
        vhdPath = $vhdPath
        vhdMaximumBytes = $vhdBytes
        memoryBytes = $memoryBytes
        processorCount = $ProcessorCount
        secureBoot = $true
        virtualTpm = $true
        switchName = $SwitchName
        created = $created
        started = $started
    }
    iso = [ordered]@{
        path = $resolvedIsoPath
        length = [long](Get-Item -LiteralPath $resolvedIsoPath).Length
        sha256 = $isoHash
    }
    safety = [ordered]@{
        existingVmOverwrite = $false
        existingDirectoryOverwrite = $false
        automaticCheckpoints = $false
        restartCommandPresent = $false
        graphicsPreflightRequired = $true
    }
    nextAction = if ($created) {
        "Install Windows, then verify the SharpGL framebuffer capability before copying project data."
    }
    else {
        "Resolve the reported failure before retrying."
    }
}

New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$result |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $resultPath -Encoding UTF8

if (-not [string]::IsNullOrWhiteSpace($failure)) {
    Write-Error $failure
    exit 1
}

Write-Output "P0-C Hyper-V VM definition prepared."
Write-Output "VM: $VmName"
Write-Output "Result: $resultPath"
Write-Output "Started: $started"
Write-Output "No host restart command was executed."
