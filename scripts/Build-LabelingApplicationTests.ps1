[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidatePattern("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    [string]$OutputName = "isolated-out"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $scriptRoot)).TrimEnd("\")
$projectPath = Join-Path `
    $repoRoot `
    "tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj"
$testArtifactRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot "artifacts\tests")).TrimEnd("\")
$repositoryArtifactRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot "artifacts")).TrimEnd("\")
$expectedLocalArtifactRoot = "D:\OpenVisionLab-TestData\Labelling_Application\artifacts"
$outputDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $testArtifactRoot $OutputName)).TrimEnd("\")
$requiredOutputPrefix = $testArtifactRoot + "\"

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git") -PathType Container)) {
    throw "Repository root could not be verified: $repoRoot"
}

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Test project does not exist: $projectPath"
}

$isCi = $env:CI -eq "true" -or $env:GITHUB_ACTIONS -eq "true"
if (-not $isCi -and (Test-Path -LiteralPath "D:\" -PathType Container)) {
    $artifactItem = Get-Item -LiteralPath $repositoryArtifactRoot -Force -ErrorAction SilentlyContinue
    $junctionTarget = if ($null -ne $artifactItem -and $artifactItem.LinkType -eq "Junction") {
        [System.IO.Path]::GetFullPath([string]($artifactItem.Target | Select-Object -First 1)).TrimEnd("\")
    }
    else {
        $null
    }

    if (-not $junctionTarget -or -not $junctionTarget.Equals(
            $expectedLocalArtifactRoot,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Local test artifacts must use $expectedLocalArtifactRoot. Run scripts\Move-LabelingTestStorageToDDrive.ps1 -Apply first."
    }
}

if (
    -not $outputDirectory.StartsWith(
        $requiredOutputPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Test output path escapes the repository test artifact root: $outputDirectory"
}

$arguments = @(
    "build",
    $projectPath,
    "-c",
    $Configuration,
    "/nr:false",
    "-m:1",
    "/p:UseSharedCompilation=false",
    "/p:OutDir=$outputDirectory\"
)

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "LabelingApplication.Tests build failed with exit code $LASTEXITCODE."
}

$testDll = Join-Path $outputDirectory "LabelingApplication.Tests.dll"
if (-not (Test-Path -LiteralPath $testDll -PathType Leaf)) {
    throw "Expected test assembly was not produced: $testDll"
}

$relativeOutputDirectory = $outputDirectory.
    Substring($repoRoot.Length + 1).
    Replace("\", "/")
$relativeTestDll = $testDll.
    Substring($repoRoot.Length + 1).
    Replace("\", "/")

Write-Output "TEST_OUTPUT_DIR=$relativeOutputDirectory"
Write-Output "TEST_DLL=$relativeTestDll"
