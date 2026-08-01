[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PreviewJson,

    [Parameter(Mandatory = $true)]
    [string]$EvidenceJson,

    [Parameter(Mandatory = $true)]
    [ValidateSet("C:")]
    [string]$ApprovedDrive,

    [Parameter(Mandatory = $true)]
    [int]$ExpectedCandidateCount,

    [Parameter(Mandatory = $true)]
    [long]$ExpectedCandidateBytes,

    [switch]$Apply
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $scriptRoot)).TrimEnd("\")
$artifactRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repoRoot "artifacts")).TrimEnd("\")
$resolvedPreviewPath = [System.IO.Path]::GetFullPath($PreviewJson)
$resolvedEvidencePath = [System.IO.Path]::GetFullPath($EvidenceJson)
$requiredArtifactPrefix = $artifactRoot + "\"
$requiredRepoPrefix = $repoRoot + "\"

if (-not $Apply) {
    throw "Approved cleanup is fail-closed. Pass -Apply only after the exact drive, candidate count, and byte total have been approved."
}

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git") -PathType Container)) {
    throw "Repository root could not be verified: $repoRoot"
}

foreach ($path in @($resolvedPreviewPath, $resolvedEvidencePath)) {
    if (-not $path.StartsWith(
            $requiredArtifactPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Cleanup inputs and evidence must stay under the repository artifact root: $path"
    }
}

if ($resolvedPreviewPath.Equals(
        $resolvedEvidencePath,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Cleanup evidence must not overwrite the approved preview."
}

if (-not (Test-Path -LiteralPath $resolvedPreviewPath -PathType Leaf)) {
    throw "Approved cleanup preview does not exist: $resolvedPreviewPath"
}

$preview = Get-Content -LiteralPath $resolvedPreviewPath -Raw -Encoding utf8 |
    ConvertFrom-Json
if ($preview.schemaVersion -ne 1) {
    throw "Unsupported cleanup preview schema version: $($preview.schemaVersion)"
}

if (
    $preview.mode -ne "cleanup-preview-only" -or
    $preview.deletionPerformed -ne $false -or
    $preview.operationsAuthorized -ne $false) {
    throw "Cleanup requires an unmodified preview-only report."
}

$previewRepositoryRoot = [System.IO.Path]::GetFullPath(
    [string]$preview.repositoryRoot).TrimEnd("\")
if (-not $previewRepositoryRoot.Equals(
        $repoRoot,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Cleanup preview belongs to a different repository: $previewRepositoryRoot"
}

$targets = @(
    $preview.candidates |
    Where-Object { $_.physicalDrive -eq $ApprovedDrive } |
    Sort-Object logicalPath
)
$targetBytes = [long](
    $targets |
    Measure-Object -Property bytes -Sum).Sum
if (
    $targets.Count -ne $ExpectedCandidateCount -or
    $targetBytes -ne $ExpectedCandidateBytes) {
    throw (
        "Approved cleanup totals changed. Expected {0} candidates/{1} bytes; found {2}/{3}." -f
        $ExpectedCandidateCount,
        $ExpectedCandidateBytes,
        $targets.Count,
        $targetBytes)
}

if ($targets.Count -eq 0) {
    throw "No approved cleanup targets were found for $ApprovedDrive."
}

$validatedTargets = [System.Collections.Generic.List[object]]::new()
foreach ($target in $targets) {
    $logicalPath = [System.IO.Path]::GetFullPath(
        (Join-Path $repoRoot ([string]$target.logicalPath).Replace("/", "\"))).TrimEnd("\")
    $physicalPath = [System.IO.Path]::GetFullPath(
        [string]$target.physicalPath).TrimEnd("\")

    if (-not $logicalPath.StartsWith(
            $requiredRepoPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Cleanup target escapes the repository root: $logicalPath"
    }

    if (-not $physicalPath.StartsWith(
            $requiredRepoPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "C-drive cleanup target is outside the repository root: $physicalPath"
    }

    $physicalDrive = [System.IO.Path]::GetPathRoot($physicalPath).TrimEnd("\")
    if (-not $physicalDrive.Equals(
            $ApprovedDrive,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Cleanup target is on an unapproved drive: $physicalPath"
    }

    if (-not $logicalPath.Equals(
            $physicalPath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "C-drive cleanup refuses junction-routed targets: $logicalPath -> $physicalPath"
    }

    $item = Get-Item -LiteralPath $physicalPath -Force -ErrorAction Stop
    if (-not $item.PSIsContainer) {
        throw "Cleanup target is not a directory: $physicalPath"
    }

    if (
        $item.LinkType -or
        (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw "Cleanup target is a reparse point: $physicalPath"
    }

    $nestedReparsePoint = Get-ChildItem -LiteralPath $physicalPath -Force -Recurse |
        Where-Object {
            ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
        } |
        Select-Object -First 1
    if ($null -ne $nestedReparsePoint) {
        throw "Cleanup target contains a nested reparse point: $($nestedReparsePoint.FullName)"
    }

    $files = @(Get-ChildItem -LiteralPath $physicalPath -Force -File -Recurse)
    $liveBytes = [long]($files | Measure-Object -Property Length -Sum).Sum
    if (
        $files.Count -ne [int]$target.fileCount -or
        $liveBytes -ne [long]$target.bytes) {
        throw (
            "Cleanup target changed after preview: {0}. Expected {1} files/{2} bytes; found {3}/{4}." -f
            $physicalPath,
            $target.fileCount,
            $target.bytes,
            $files.Count,
            $liveBytes)
    }

    $validatedTargets.Add([pscustomobject][ordered]@{
        logicalPath = [string]$target.logicalPath
        physicalPath = $physicalPath
        fileCount = $files.Count
        bytes = $liveBytes
    })
}

for ($left = 0; $left -lt $validatedTargets.Count; $left++) {
    for ($right = $left + 1; $right -lt $validatedTargets.Count; $right++) {
        $leftPrefix = $validatedTargets[$left].physicalPath + "\"
        $rightPrefix = $validatedTargets[$right].physicalPath + "\"
        if (
            $validatedTargets[$right].physicalPath.StartsWith(
                $leftPrefix,
                [System.StringComparison]::OrdinalIgnoreCase) -or
            $validatedTargets[$left].physicalPath.StartsWith(
                $rightPrefix,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Cleanup target set contains overlapping directories."
        }
    }
}

$previewHash = (Get-FileHash -LiteralPath $resolvedPreviewPath -Algorithm SHA256).Hash
$driveBefore = [System.IO.DriveInfo]::new($ApprovedDrive)
$completedTargets = [System.Collections.Generic.List[object]]::new()
$report = [ordered]@{
    schemaVersion = 1
    generatedAt = (Get-Date).ToString("o")
    completedAt = $null
    repositoryRoot = $repoRoot
    mode = "approved-cleanup-execution"
    status = "started"
    deletionPerformed = $false
    approvedScope = [ordered]@{
        physicalDrive = $ApprovedDrive
        candidateCount = $ExpectedCandidateCount
        candidateFileCount = [int](
            $validatedTargets |
            Measure-Object -Property fileCount -Sum).Sum
        candidateBytes = $ExpectedCandidateBytes
        candidateSizeGiB = [math]::Round($ExpectedCandidateBytes / 1GB, 3)
    }
    sourcePreview = [ordered]@{
        path = $resolvedPreviewPath
        sha256 = $previewHash
        generatedAt = $preview.generatedAt
    }
    storage = [ordered]@{
        freeBytesBefore = [long]$driveBefore.AvailableFreeSpace
        freeBytesAfterDelete = $null
        freeBytesDelta = $null
    }
    plannedTargets = @($validatedTargets)
    completedTargets = $completedTargets
    excludedScope = [ordered]@{
        otherDrives = "Not authorized; no D-drive candidate is deleted."
        preserveReview = "Excluded"
        manualReview = "Excluded"
        trackedFiles = "Excluded"
        proofline = "Excluded"
        sourceAndDocumentation = "Excluded"
    }
}

$evidenceDirectory = Split-Path -Parent $resolvedEvidencePath
New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
function Write-Evidence {
    $report.completedTargets = @($completedTargets)
    $report |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $resolvedEvidencePath -Encoding utf8
}

Write-Evidence
try {
    foreach ($target in $validatedTargets) {
        Remove-Item -LiteralPath $target.physicalPath -Recurse -Force
        if (Test-Path -LiteralPath $target.physicalPath) {
            throw "Cleanup target still exists after deletion: $($target.physicalPath)"
        }

        $completedTargets.Add([pscustomobject][ordered]@{
            logicalPath = $target.logicalPath
            physicalPath = $target.physicalPath
            deletedAt = (Get-Date).ToString("o")
        })
        Write-Evidence
    }
}
catch {
    $report.status = "incomplete"
    $report | Add-Member -NotePropertyName failure -NotePropertyValue ([ordered]@{
        occurredAt = (Get-Date).ToString("o")
        message = $_.Exception.Message
    }) -Force
    Write-Evidence
    throw
}

$driveAfter = [System.IO.DriveInfo]::new($ApprovedDrive)
$report.completedAt = (Get-Date).ToString("o")
$report.status = "complete"
$report.deletionPerformed = $true
$report.storage.freeBytesAfterDelete = [long]$driveAfter.AvailableFreeSpace
$report.storage.freeBytesDelta =
    [long]$driveAfter.AvailableFreeSpace - [long]$driveBefore.AvailableFreeSpace
Write-Evidence

Write-Output (
    "CLEANUP_COMPLETE drive={0} candidates={1} files={2} bytes={3} freeBytesDelta={4}" -f
    $ApprovedDrive,
    $report.approvedScope.candidateCount,
    $report.approvedScope.candidateFileCount,
    $report.approvedScope.candidateBytes,
    $report.storage.freeBytesDelta)
Write-Output "EVIDENCE=$resolvedEvidencePath"
