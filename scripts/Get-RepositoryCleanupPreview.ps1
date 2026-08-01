[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InventoryJson,

    [Parameter(Mandatory = $true)]
    [string]$OutputJson
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $scriptRoot)).TrimEnd("\")
$artifactRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot "artifacts")).TrimEnd("\")
$testStorageRoot = "D:\OpenVisionLab-TestData\Labelling_Application"
$resolvedInventoryPath = [System.IO.Path]::GetFullPath($InventoryJson)
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputJson)
$requiredArtifactPrefix = $artifactRoot + "\"

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git") -PathType Container)) {
    throw "Repository root could not be verified: $repoRoot"
}

foreach ($path in @($resolvedInventoryPath, $resolvedOutputPath)) {
    if (-not $path.StartsWith(
            $requiredArtifactPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Cleanup preview inputs and outputs must stay under the repository artifact root: $path"
    }
}

if ($resolvedInventoryPath.Equals(
        $resolvedOutputPath,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Cleanup preview output must not overwrite the source inventory."
}

if (-not (Test-Path -LiteralPath $resolvedInventoryPath -PathType Leaf)) {
    throw "Inventory JSON does not exist: $resolvedInventoryPath"
}

function Resolve-PhysicalDirectoryPath {
    param([Parameter(Mandatory = $true)][string]$LogicalPath)

    $resolvedLogicalPath = [System.IO.Path]::GetFullPath($LogicalPath).TrimEnd("\")
    $probe = $resolvedLogicalPath
    while (
        $probe.Length -ge $repoRoot.Length -and
        $probe.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $item = Get-Item -LiteralPath $probe -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and $item.LinkType -eq "Junction") {
            $junctionTarget = [System.IO.Path]::GetFullPath(
                [string]($item.Target | Select-Object -First 1)).TrimEnd("\")
            $remainder = $resolvedLogicalPath.Substring($probe.Length).TrimStart("\")
            if ([string]::IsNullOrWhiteSpace($remainder)) {
                return $junctionTarget
            }

            return [System.IO.Path]::GetFullPath(
                (Join-Path $junctionTarget $remainder)).TrimEnd("\")
        }

        if ($probe.Equals($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }

        $probe = [System.IO.Path]::GetDirectoryName($probe).TrimEnd("\")
    }

    return $resolvedLogicalPath
}

$inventory = Get-Content -LiteralPath $resolvedInventoryPath -Raw -Encoding utf8 |
    ConvertFrom-Json
if ($inventory.schemaVersion -ne 1) {
    throw "Unsupported inventory schema version: $($inventory.schemaVersion)"
}

if ($inventory.mode -ne "inventory-only" -or $inventory.deletionPerformed -ne $false) {
    throw "Cleanup preview requires a read-only inventory with deletionPerformed=false."
}

$inventoryRepositoryRoot = [System.IO.Path]::GetFullPath(
    [string]$inventory.repositoryRoot).TrimEnd("\")
if (-not $inventoryRepositoryRoot.Equals(
        $repoRoot,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Inventory belongs to a different repository: $inventoryRepositoryRoot"
}

$candidates = @(
    $inventory.entries |
    Where-Object { $_.disposition -eq "rebuildable-candidate" } |
    Sort-Object -Property @{ Expression = "bytes"; Descending = $true }, path
)
if ($candidates.Count -eq 0) {
    throw "Inventory contains no rebuildable candidates."
}

$proposalEntries = [System.Collections.Generic.List[object]]::new()
foreach ($candidate in $candidates) {
    $normalizedRelativePath = ([string]$candidate.path).Replace("/", "\")
    $logicalPath = [System.IO.Path]::GetFullPath(
        (Join-Path $repoRoot $normalizedRelativePath)).TrimEnd("\")
    $requiredRepoPrefix = $repoRoot + "\"
    if (-not $logicalPath.StartsWith(
            $requiredRepoPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Candidate escapes the repository root: $logicalPath"
    }

    if (-not (Test-Path -LiteralPath $logicalPath -PathType Container)) {
        throw "Candidate directory no longer exists: $logicalPath"
    }

    if (@($candidate.referencedByTrackedDocs).Count -gt 0) {
        throw "Rebuildable candidate unexpectedly has tracked-document references: $($candidate.path)"
    }

    $physicalPath = Resolve-PhysicalDirectoryPath -LogicalPath $logicalPath
    $requiredPhysicalRepoPrefix = $repoRoot + "\"
    $requiredTestStoragePrefix = $testStorageRoot.TrimEnd("\") + "\"
    $physicalPathAllowed =
        $physicalPath.StartsWith(
            $requiredPhysicalRepoPrefix,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        $physicalPath.StartsWith(
            $requiredTestStoragePrefix,
            [System.StringComparison]::OrdinalIgnoreCase)
    if (-not $physicalPathAllowed) {
        throw "Candidate physical path is outside the allowed repository and test-storage roots: $physicalPath"
    }

    $proposalEntries.Add([pscustomobject][ordered]@{
        logicalPath = ([string]$candidate.path).Replace("\", "/")
        physicalPath = $physicalPath
        physicalDrive = [System.IO.Path]::GetPathRoot($physicalPath).TrimEnd("\")
        kind = $candidate.kind
        fileCount = [int]$candidate.fileCount
        bytes = [long]$candidate.bytes
        sizeGiB = [math]::Round(([long]$candidate.bytes) / 1GB, 3)
        reason = $candidate.reason
        referencedByTrackedDocs = @($candidate.referencedByTrackedDocs)
    })
}

$candidateBytes = [long](
    $proposalEntries |
    Measure-Object -Property bytes -Sum).Sum
$inventoryCandidateSummary = @(
    $inventory.summary.byDisposition |
    Where-Object { $_.disposition -eq "rebuildable-candidate" }
) | Select-Object -First 1
if (
    $null -eq $inventoryCandidateSummary -or
    [long]$inventoryCandidateSummary.bytes -ne $candidateBytes -or
    [int]$inventoryCandidateSummary.entryCount -ne $proposalEntries.Count) {
    throw "Candidate totals do not match the source inventory summary."
}

$excludedPreserve = @(
    $inventory.entries |
    Where-Object { $_.disposition -eq "preserve-review" } |
    ForEach-Object { $_.path } |
    Sort-Object
)
$excludedManual = @(
    $inventory.entries |
    Where-Object { $_.disposition -eq "manual-review" } |
    ForEach-Object { $_.path } |
    Sort-Object
)
$inventoryHash = (Get-FileHash -LiteralPath $resolvedInventoryPath -Algorithm SHA256).Hash
$cDrive = Get-PSDrive -Name C
$dDrive = Get-PSDrive -Name D -ErrorAction SilentlyContinue
$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    generatedAt = (Get-Date).ToString("o")
    repositoryRoot = $repoRoot
    mode = "cleanup-preview-only"
    deletionPerformed = $false
    operationsAuthorized = $false
    sourceInventory = [pscustomobject][ordered]@{
        path = $resolvedInventoryPath
        sha256 = $inventoryHash
        generatedAt = $inventory.generatedAt
    }
    allowedPhysicalRoots = @(
        $repoRoot,
        $testStorageRoot
    )
    storage = [pscustomobject][ordered]@{
        cFreeBytes = [long]$cDrive.Free
        cFreeGiB = [math]::Round(([long]$cDrive.Free) / 1GB, 3)
        dFreeBytes = if ($null -ne $dDrive) { [long]$dDrive.Free } else { $null }
        dFreeGiB = if ($null -ne $dDrive) { [math]::Round(([long]$dDrive.Free) / 1GB, 3) } else { $null }
    }
    summary = [pscustomobject][ordered]@{
        candidateCount = $proposalEntries.Count
        candidateFileCount = [int](
            $proposalEntries |
            Measure-Object -Property fileCount -Sum).Sum
        candidateBytes = $candidateBytes
        candidateSizeGiB = [math]::Round($candidateBytes / 1GB, 3)
        preserveReviewExcludedCount = $excludedPreserve.Count
        manualReviewExcludedCount = $excludedManual.Count
    }
    candidates = $proposalEntries
    exclusions = [pscustomobject][ordered]@{
        preserveReview = $excludedPreserve
        manualReview = $excludedManual
        trackedFiles = "Always excluded"
        proofline = "Always excluded"
        sourceAndDocumentation = "Always excluded"
    }
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$report |
    ConvertTo-Json -Depth 10 |
    Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8

$proposalEntries |
    Select-Object logicalPath, physicalDrive, fileCount, sizeGiB |
    Format-Table -AutoSize
Write-Output (
    "CLEANUP_PREVIEW candidates={0} files={1} sizeGiB={2} preserveExcluded={3} manualExcluded={4} deletionPerformed={5} operationsAuthorized={6}" -f
    $report.summary.candidateCount,
    $report.summary.candidateFileCount,
    $report.summary.candidateSizeGiB,
    $report.summary.preserveReviewExcludedCount,
    $report.summary.manualReviewExcludedCount,
    $report.deletionPerformed,
    $report.operationsAuthorized)
Write-Output "REPORT=$resolvedOutputPath"
