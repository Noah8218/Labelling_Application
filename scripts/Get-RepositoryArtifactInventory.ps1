[CmdletBinding()]
param(
    [string]$OutputJson = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $scriptRoot)).TrimEnd("\")
$artifactRoot = Join-Path $repoRoot "artifacts"
$inventoryPolicyRelativePath =
    "docs/REPOSITORY_ARTIFACT_INVENTORY_AND_RETENTION_POLICY_20260731.md"

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git") -PathType Container)) {
    throw "Repository root could not be verified: $repoRoot"
}

$resolvedOutputPath = $null
$excludedOutputSubtree = $null
if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputJson)
    $requiredOutputPrefix = (
        [System.IO.Path]::GetFullPath($artifactRoot).TrimEnd("\") + "\")
    if (-not $resolvedOutputPath.StartsWith(
        $requiredOutputPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Inventory output must stay under the repository artifact root: $resolvedOutputPath"
    }

    $relativeOutputPath = $resolvedOutputPath.Substring(
        $requiredOutputPrefix.Length)
    $firstSeparatorIndex = $relativeOutputPath.IndexOf("\")
    if ($firstSeparatorIndex -gt 0) {
        $topLevelOutputName = $relativeOutputPath.Substring(
            0,
            $firstSeparatorIndex)
        $excludedOutputSubtree = Join-Path $artifactRoot $topLevelOutputName
    }
}

function Get-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FullPath
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($FullPath)
    $requiredPrefix = $repoRoot + "\"
    if (-not $resolvedPath.StartsWith(
        $requiredPrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes the repository root: $resolvedPath"
    }

    return $resolvedPath.Substring($requiredPrefix.Length).Replace("\", "/")
}

function Get-DirectoryStatistics {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LiteralPath
    )

    if (-not (Test-Path -LiteralPath $LiteralPath -PathType Container)) {
        return [pscustomobject]@{
            fileCount = 0
            bytes = [long]0
        }
    }

    $measure = Get-ChildItem `
        -LiteralPath $LiteralPath `
        -Recurse `
        -File `
        -Force `
        -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum

    $bytes = if ($null -eq $measure.Sum) {
        [long]0
    }
    else {
        [long]$measure.Sum
    }

    return [pscustomobject]@{
        fileCount = [int]$measure.Count
        bytes = $bytes
    }
}

function New-InventoryEntry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LiteralPath,

        [Parameter(Mandatory = $true)]
        [string]$Kind,

        [Parameter(Mandatory = $true)]
        [string]$Disposition,

        [Parameter(Mandatory = $true)]
        [string]$Reason,

        [string[]]$ReferencedBy = @()
    )

    $statistics = Get-DirectoryStatistics -LiteralPath $LiteralPath
    return [pscustomobject][ordered]@{
        path = Get-RepositoryRelativePath -FullPath $LiteralPath
        kind = $Kind
        disposition = $Disposition
        reason = $Reason
        fileCount = $statistics.fileCount
        bytes = $statistics.bytes
        sizeGiB = [math]::Round($statistics.bytes / 1GB, 3)
        referencedByTrackedDocs = @($ReferencedBy)
    }
}

function Get-TrackedMarkdownCorpus {
    $trackedMarkdown = @(& git -C $repoRoot ls-files "*.md")
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed while reading tracked Markdown files."
    }

    $corpus = @()
    foreach ($relativePath in $trackedMarkdown) {
        if ($relativePath.Replace("\", "/").Equals(
            $inventoryPolicyRelativePath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $fullPath = Join-Path $repoRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $normalizedText = [System.IO.File]::ReadAllText($fullPath).
            Replace("\", "/").
            ToLowerInvariant()
        $corpus += [pscustomobject]@{
            path = $relativePath.Replace("\", "/")
            text = $normalizedText
        }
    }

    return $corpus
}

function Get-ArtifactDisposition {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string[]]$ReferencedBy
    )

    if ($ReferencedBy.Count -gt 0) {
        return [pscustomobject]@{
            disposition = "preserve-review"
            reason = "Tracked documentation references this artifact path; verify the evidence contract before removal."
        }
    }

    if (
        $Name -eq "run" -or
        $Name -eq "isolated-out" -or
        $Name -match "^isolated-" -or
        $Name -match "-out$" -or
        $Name -match "-check$" -or
        $Name -match "-before-build$") {
        return [pscustomobject]@{
            disposition = "rebuildable-candidate"
            reason = "The name matches a build or isolated verification output pattern and no tracked Markdown reference was found."
        }
    }

    return [pscustomobject]@{
        disposition = "manual-review"
        reason = "No automatic evidence or rebuildable-output rule is strong enough to recommend removal."
    }
}

$markdownCorpus = @(Get-TrackedMarkdownCorpus)
$entries = @()

if (Test-Path -LiteralPath $artifactRoot -PathType Container) {
    foreach ($directory in Get-ChildItem -LiteralPath $artifactRoot -Directory -Force) {
        if (
            $null -ne $excludedOutputSubtree -and
            $directory.FullName.Equals(
                $excludedOutputSubtree,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $token = (
            "artifacts/" + $directory.Name).ToLowerInvariant()
        $referencedBy = @(
            $markdownCorpus |
            Where-Object { $_.text.Contains($token) } |
            ForEach-Object { $_.path } |
            Sort-Object -Unique
        )
        $classification = Get-ArtifactDisposition `
            -Name $directory.Name `
            -ReferencedBy $referencedBy

        $entries += New-InventoryEntry `
            -LiteralPath $directory.FullName `
            -Kind "artifact-subtree" `
            -Disposition $classification.disposition `
            -Reason $classification.reason `
            -ReferencedBy $referencedBy
    }
}

$knownGeneratedRoots = @(
    ".vs",
    "bin",
    "obj",
    "packages",
    "tests\artifacts",
    "tests\LabelingApplication.Tests\artifacts",
    "tests\LabelingApplication.Tests\bin",
    "tests\LabelingApplication.Tests\obj"
)

foreach ($relativePath in $knownGeneratedRoots) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        continue
    }

    $kind = if ($relativePath -like "*artifacts") {
        "test-output-root"
    }
    elseif ($relativePath -eq "packages") {
        "package-cache"
    }
    else {
        "build-cache"
    }

    $reason = if ($kind -eq "test-output-root") {
        "Test output generated by verification commands; regenerate through the owning test command after review."
    }
    elseif ($kind -eq "package-cache") {
        "Local package cache is ignored by Git and can be restored by the owning package workflow."
    }
    else {
        "Ignored IDE or build output; no source file is owned by this directory."
    }

    $entries += New-InventoryEntry `
        -LiteralPath $fullPath `
        -Kind $kind `
        -Disposition "rebuildable-candidate" `
        -Reason $reason
}

$openVisionLabRoot = Join-Path $repoRoot "OpenVisionLab"
if (Test-Path -LiteralPath $openVisionLabRoot -PathType Container) {
    $nestedBuildDirectories = @(
        Get-ChildItem `
            -LiteralPath $openVisionLabRoot `
            -Directory `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @("bin", "obj") }
    )

    foreach ($directory in $nestedBuildDirectories) {
        $entries += New-InventoryEntry `
            -LiteralPath $directory.FullName `
            -Kind "component-build-cache" `
            -Disposition "rebuildable-candidate" `
            -Reason "Ignored build output under the internal OpenVisionLab component source tree."
    }
}

$entries = @(
    $entries |
    Sort-Object path -Unique
)

$dispositionSummary = @(
    $entries |
    Group-Object disposition |
    ForEach-Object {
        $bytes = [long](
            $_.Group |
            Measure-Object -Property bytes -Sum).Sum
        [pscustomobject][ordered]@{
            disposition = $_.Name
            entryCount = $_.Count
            fileCount = [int](
                $_.Group |
                Measure-Object -Property fileCount -Sum).Sum
            bytes = $bytes
            sizeGiB = [math]::Round($bytes / 1GB, 3)
        }
    } |
    Sort-Object disposition
)

$totalBytes = [long](
    $entries |
    Measure-Object -Property bytes -Sum).Sum
$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    generatedAt = (Get-Date).ToString("o")
    repositoryRoot = $repoRoot
    mode = "inventory-only"
    deletionPerformed = $false
    excludedPaths = @(
        if ($null -ne $excludedOutputSubtree) {
            Get-RepositoryRelativePath -FullPath $excludedOutputSubtree
        }
    )
    referenceCorpusExcludes = @($inventoryPolicyRelativePath)
    policy = [pscustomobject][ordered]@{
        preserveReview = "Referenced evidence or release output; inspect its owning contract before removal."
        rebuildableCandidate = "Generated output that appears reproducible; this report does not authorize deletion."
        manualReview = "Mixed or unknown content requiring an explicit retention decision."
    }
    summary = [pscustomobject][ordered]@{
        entryCount = $entries.Count
        fileCount = [int](
            $entries |
            Measure-Object -Property fileCount -Sum).Sum
        bytes = $totalBytes
        sizeGiB = [math]::Round($totalBytes / 1GB, 3)
        byDisposition = $dispositionSummary
    }
    entries = $entries
}

if ($null -ne $resolvedOutputPath) {
    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $report |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
}

$report.summary.byDisposition |
    Format-Table `
        disposition,
        entryCount,
        fileCount,
        sizeGiB `
        -AutoSize

Write-Output (
    "INVENTORY entries={0} files={1} sizeGiB={2} deletionPerformed={3}" -f
    $report.summary.entryCount,
    $report.summary.fileCount,
    $report.summary.sizeGiB,
    $report.deletionPerformed)

if ($null -ne $resolvedOutputPath) {
    Write-Output "REPORT=$resolvedOutputPath"
}
