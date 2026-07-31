[CmdletBinding()]
param(
    [string]$ReleaseDirectory = "",

    [string]$EvidenceRoot = "",

    [ValidateRange(4096, 16384)]
    [int]$MemoryInMB = 8192,

    [switch]$EnableNetworking,

    [switch]$EnableVGpu,

    [switch]$UseStandardClient,

    [string]$LogonCommand = "explorer.exe C:\P0C\Release",

    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$sandboxExecutable = Join-Path $env:SystemRoot "System32\WindowsSandbox.exe"

if (-not (Test-Path -LiteralPath $sandboxExecutable -PathType Leaf)) {
    throw @"
Windows Sandbox is not available. On Windows Pro, open an elevated PowerShell,
run the following command, and restart Windows:

Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All
"@
}

if ([string]::IsNullOrWhiteSpace($ReleaseDirectory)) {
    $manifest = Get-ChildItem `
        -Path (Join-Path $repoRoot "artifacts\publish\Release\win-x64") `
        -Filter "release-manifest.json" `
        -File `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $manifest) {
        throw "No release bundle was found. Run scripts\publish-win-x64.ps1 first."
    }

    $ReleaseDirectory = $manifest.Directory.FullName
}

$resolvedReleaseDirectory = [System.IO.Path]::GetFullPath($ReleaseDirectory)
$manifestPath = Join-Path $resolvedReleaseDirectory "release-manifest.json"
$applicationPath = Join-Path $resolvedReleaseDirectory "OpenVisionLab.LabelingStudio.exe"

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Release manifest is missing: $manifestPath"
}

if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
    throw "Packaged application is missing: $applicationPath"
}

$releaseManifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($releaseManifest.build.runtimeIdentifier -ne "win-x64" -or
    $releaseManifest.build.selfContained -ne $true) {
    throw "P0-C requires a self-contained win-x64 release bundle."
}

if ($releaseManifest.source.dirty -ne $false) {
    throw "P0-C requires a release manifest produced from clean tracked source."
}

if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $EvidenceRoot = Join-Path $repoRoot "artifacts\p0c-clean-machine\$timestamp"
}

$resolvedEvidenceRoot = [System.IO.Path]::GetFullPath($EvidenceRoot)
$sandboxEvidenceDirectory = Join-Path $resolvedEvidenceRoot "sandbox-evidence"
$harnessDirectory = Join-Path $repoRoot "scripts\p0c"
if (-not (Test-Path -LiteralPath $harnessDirectory -PathType Container)) {
    throw "P0-C Sandbox harness directory is missing: $harnessDirectory"
}
$resolvedHarnessDirectory = [System.IO.Path]::GetFullPath($harnessDirectory)
New-Item -ItemType Directory -Path $sandboxEvidenceDirectory -Force | Out-Null

function ConvertTo-XmlText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return [System.Security.SecurityElement]::Escape($Value)
}

$releaseXmlPath = ConvertTo-XmlText $resolvedReleaseDirectory
$evidenceXmlPath = ConvertTo-XmlText $sandboxEvidenceDirectory
$harnessXmlPath = ConvertTo-XmlText $resolvedHarnessDirectory
$networkingValue = if ($EnableNetworking.IsPresent) { "Enable" } else { "Disable" }
$vGpuValue = if ($EnableVGpu.IsPresent) { "Enable" } else { "Disable" }
$protectedClientValue = if ($UseStandardClient.IsPresent) { "Disable" } else { "Enable" }
$logonCommandXml = ConvertTo-XmlText $LogonCommand
$configurationPath = Join-Path $resolvedEvidenceRoot "OpenVisionLab-P0C.wsb"

$configuration = @"
<Configuration>
  <VGpu>$vGpuValue</VGpu>
  <Networking>$networkingValue</Networking>
  <AudioInput>Disable</AudioInput>
  <VideoInput>Disable</VideoInput>
  <PrinterRedirection>Disable</PrinterRedirection>
  <ClipboardRedirection>Disable</ClipboardRedirection>
  <ProtectedClient>$protectedClientValue</ProtectedClient>
  <MemoryInMB>$MemoryInMB</MemoryInMB>
  <MappedFolders>
    <MappedFolder>
      <HostFolder>$releaseXmlPath</HostFolder>
      <SandboxFolder>C:\P0C\Release</SandboxFolder>
      <ReadOnly>true</ReadOnly>
    </MappedFolder>
    <MappedFolder>
      <HostFolder>$evidenceXmlPath</HostFolder>
      <SandboxFolder>C:\P0C\Evidence</SandboxFolder>
      <ReadOnly>false</ReadOnly>
    </MappedFolder>
    <MappedFolder>
      <HostFolder>$harnessXmlPath</HostFolder>
      <SandboxFolder>C:\P0C\Harness</SandboxFolder>
      <ReadOnly>true</ReadOnly>
    </MappedFolder>
  </MappedFolders>
  <LogonCommand>
    <Command>$logonCommandXml</Command>
  </LogonCommand>
</Configuration>
"@

$configuration | Set-Content -LiteralPath $configurationPath -Encoding UTF8

[xml]$configurationCheck = Get-Content -LiteralPath $configurationPath -Raw
$mappedFolders = @($configurationCheck.Configuration.MappedFolders.MappedFolder)
if ($mappedFolders.Count -ne 3 -or
    $mappedFolders[0].ReadOnly -ne "true" -or
    $mappedFolders[1].ReadOnly -ne "false" -or
    $mappedFolders[2].ReadOnly -ne "true") {
    throw "Generated Sandbox configuration did not preserve the mapping contract."
}

$hostContext = [ordered]@{
    schemaVersion = 1
    capturedAtUtc = [DateTime]::UtcNow.ToString("O")
    host = [ordered]@{
        osCaption = (Get-CimInstance Win32_OperatingSystem).Caption
        osVersion = [Environment]::OSVersion.Version.ToString()
        architecture = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
        totalMemoryBytes = [long](Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory
        hypervisorPresent = [bool](Get-CimInstance Win32_ComputerSystem).HypervisorPresent
    }
    release = [ordered]@{
        productVersion = [string]$releaseManifest.productVersion
        sourceCommit = [string]$releaseManifest.source.commit
        sourceDirty = [bool]$releaseManifest.source.dirty
        sdkVersion = [string]$releaseManifest.build.sdkVersion
        runtimeIdentifier = [string]$releaseManifest.build.runtimeIdentifier
        selfContained = [bool]$releaseManifest.build.selfContained
        manifestSha256 = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    sandbox = [ordered]@{
        memoryInMB = $MemoryInMB
        networking = $networkingValue
        vGpu = $vGpuValue
        protectedClient = $protectedClientValue
        releaseMapping = "read-only"
        evidenceMapping = "read-write"
        harnessMapping = "read-only"
        logonCommand = $LogonCommand
    }
}

$hostContextPath = Join-Path $resolvedEvidenceRoot "p0c-host-context.json"
$hostContext |
    ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $hostContextPath -Encoding UTF8

Write-Output "P0-C Windows Sandbox configuration created."
Write-Output "Configuration: $configurationPath"
Write-Output "Evidence folder: $sandboxEvidenceDirectory"
Write-Output "Release bundle: $resolvedReleaseDirectory"
Write-Output "Networking: $networkingValue"
Write-Output "vGPU: $vGpuValue"
Write-Output "Protected client: $protectedClientValue"
Write-Output ""
Write-Output "Double-click the .wsb file, or rerun this command with -Launch."
Write-Output "Closing Windows Sandbox permanently discards everything except files copied to C:\P0C\Evidence."

if ($Launch.IsPresent) {
    Start-Process -FilePath $configurationPath
}
