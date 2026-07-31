[CmdletBinding()]
param(
    [string]$StorageRoot = "D:\OpenVisionLab-TestData\Labelling_Application",

    [switch]$Apply
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $scriptRoot)).TrimEnd("\")
$storageRoot = [System.IO.Path]::GetFullPath($StorageRoot).TrimEnd("\")
$requiredStoragePrefix = "D:\OpenVisionLab-TestData\"

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git") -PathType Container)) {
    throw "Repository root could not be verified: $repoRoot"
}

if (-not $storageRoot.StartsWith(
        $requiredStoragePrefix,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Storage root must stay under $requiredStoragePrefix"
}

if (-not (Get-PSDrive -Name D -ErrorAction SilentlyContinue)) {
    throw "D drive is not available."
}

$mappings = @(
    [pscustomobject]@{
        Name = "repository-artifacts"
        LogicalPath = Join-Path $repoRoot "artifacts"
        PhysicalPath = Join-Path $storageRoot "artifacts"
    },
    [pscustomobject]@{
        Name = "legacy-test-artifacts"
        LogicalPath = Join-Path $repoRoot "tests\LabelingApplication.Tests\artifacts"
        PhysicalPath = Join-Path $storageRoot "legacy\LabelingApplication.Tests-artifacts"
    },
    [pscustomobject]@{
        Name = "test-build-bin"
        LogicalPath = Join-Path $repoRoot "tests\LabelingApplication.Tests\bin"
        PhysicalPath = Join-Path $storageRoot "build-cache\LabelingApplication.Tests-bin"
    },
    [pscustomobject]@{
        Name = "test-build-obj"
        LogicalPath = Join-Path $repoRoot "tests\LabelingApplication.Tests\obj"
        PhysicalPath = Join-Path $storageRoot "build-cache\LabelingApplication.Tests-obj"
    },
    [pscustomobject]@{
        Name = "legacy-tests-artifacts"
        LogicalPath = Join-Path $repoRoot "tests\artifacts"
        PhysicalPath = Join-Path $storageRoot "legacy\tests-artifacts"
    }
)

function Get-DirectoryBytes {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return [long]0
    }

    $extendedPath = if ($Path.StartsWith("\\?\")) { $Path } else { "\\?\$Path" }
    $totalBytes = [long]0
    foreach ($filePath in [System.IO.Directory]::EnumerateFiles(
            $extendedPath,
            "*",
            [System.IO.SearchOption]::AllDirectories)) {
        $totalBytes += [System.IO.FileInfo]::new($filePath).Length
    }

    return $totalBytes
}

function Get-JunctionTarget {
    param([Parameter(Mandatory)][System.IO.FileSystemInfo]$Item)

    if ($Item.LinkType -ne "Junction") {
        return $null
    }

    return [System.IO.Path]::GetFullPath([string]($Item.Target | Select-Object -First 1)).TrimEnd("\")
}

function Get-FileSha256 {
    param([Parameter(Mandatory)][string]$Path)

    $extendedPath = if ($Path.StartsWith("\\?\")) { $Path } else { "\\?\$Path" }
    $stream = [System.IO.File]::OpenRead($extendedPath)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($sha256.ComputeHash($stream)).Replace("-", "")
    }
    finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

function Copy-Verify-AndRemoveDirectory {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    $extendedSource = if ($Source.StartsWith("\\?\")) { $Source } else { "\\?\$Source" }
    $extendedDestination = if ($Destination.StartsWith("\\?\")) { $Destination } else { "\\?\$Destination" }
    [System.IO.Directory]::CreateDirectory($extendedDestination) | Out-Null
    $sourceDirectories = @([System.IO.Directory]::EnumerateDirectories(
        $extendedSource,
        "*",
        [System.IO.SearchOption]::AllDirectories))
    foreach ($sourceDirectory in $sourceDirectories) {
        $relativePath = $sourceDirectory.Substring($extendedSource.Length).TrimStart("\")
        $destinationDirectory = Join-Path $Destination $relativePath
        [System.IO.Directory]::CreateDirectory("\\?\$destinationDirectory") | Out-Null
    }

    $sourceFiles = @([System.IO.Directory]::EnumerateFiles(
        $extendedSource,
        "*",
        [System.IO.SearchOption]::AllDirectories))
    foreach ($sourceFilePath in $sourceFiles) {
        $relativePath = $sourceFilePath.Substring($extendedSource.Length).TrimStart("\")
        $destinationPath = Join-Path $Destination $relativePath
        $destinationDirectory = Split-Path -Parent $destinationPath
        [System.IO.Directory]::CreateDirectory("\\?\$destinationDirectory") | Out-Null
        [System.IO.File]::Copy($sourceFilePath, "\\?\$destinationPath", $true)
    }

    foreach ($sourceFilePath in $sourceFiles) {
        $sourceFile = [System.IO.FileInfo]::new($sourceFilePath)
        $relativePath = $sourceFilePath.Substring($extendedSource.Length).TrimStart("\")
        $destinationPath = Join-Path $Destination $relativePath
        $destinationFile = [System.IO.FileInfo]::new("\\?\$destinationPath")
        if (-not $destinationFile.Exists -or $destinationFile.Length -ne $sourceFile.Length) {
            throw "Copied file length verification failed: $relativePath"
        }

        $sourceHash = Get-FileSha256 -Path $sourceFilePath
        $destinationHash = Get-FileSha256 -Path $destinationPath
        if (-not $sourceHash.Equals($destinationHash, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Copied file SHA-256 verification failed: $relativePath"
        }
    }

    $resolvedSource = [System.IO.Path]::GetFullPath($Source).TrimEnd("\")
    $requiredSourcePrefix = $repoRoot + "\"
    if (-not $resolvedSource.StartsWith($requiredSourcePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a source outside the repository: $resolvedSource"
    }

    [System.IO.Directory]::Delete("\\?\$resolvedSource", $true)
}

$results = [System.Collections.Generic.List[object]]::new()
$requiredCopyBytes = [long]0
foreach ($mapping in $mappings) {
    $logicalItem = Get-Item -LiteralPath $mapping.LogicalPath -Force -ErrorAction SilentlyContinue
    if ($null -ne $logicalItem -and $logicalItem.PSIsContainer -and $logicalItem.LinkType -ne "Junction") {
        $requiredCopyBytes += Get-DirectoryBytes -Path $mapping.LogicalPath
    }
}

$freeBytes = [long](Get-PSDrive -Name D).Free
if ($requiredCopyBytes -gt $freeBytes) {
    throw "D drive does not have enough free space for the planned move."
}

foreach ($mapping in $mappings) {
    $logicalPath = [System.IO.Path]::GetFullPath($mapping.LogicalPath).TrimEnd("\")
    $physicalPath = [System.IO.Path]::GetFullPath($mapping.PhysicalPath).TrimEnd("\")
    $requiredRepoPrefix = $repoRoot + "\"
    $requiredPhysicalPrefix = $storageRoot + "\"

    if (-not $logicalPath.StartsWith($requiredRepoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Logical path escapes the repository: $logicalPath"
    }

    if (-not $physicalPath.StartsWith($requiredPhysicalPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Physical path escapes the D-drive storage root: $physicalPath"
    }

    $logicalItem = Get-Item -LiteralPath $logicalPath -Force -ErrorAction SilentlyContinue
    $physicalExists = Test-Path -LiteralPath $physicalPath -PathType Container
    $junctionTarget = if ($null -ne $logicalItem) { Get-JunctionTarget -Item $logicalItem } else { $null }

    if ($junctionTarget) {
        if (-not $junctionTarget.Equals($physicalPath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Unexpected junction target for $logicalPath`: $junctionTarget"
        }

        if (-not $physicalExists) {
            throw "Junction target is missing: $physicalPath"
        }

        $bytes = Get-DirectoryBytes -Path $physicalPath
        $results.Add([pscustomobject]@{
            Name = $mapping.Name
            State = "already-migrated"
            LogicalPath = $logicalPath
            PhysicalPath = $physicalPath
            Bytes = $bytes
        })
        continue
    }

    if ($null -ne $logicalItem -and -not $logicalItem.PSIsContainer) {
        throw "Logical path is not a directory: $logicalPath"
    }

    if ($null -eq $logicalItem -and -not $physicalExists) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $physicalPath -Force | Out-Null
            New-Item -ItemType Junction -Path $logicalPath -Target $physicalPath | Out-Null
        }

        $results.Add([pscustomobject]@{
            Name = $mapping.Name
            State = if ($Apply) { "created-empty-target-and-junction" } else { "would-create-empty-target-and-junction" }
            LogicalPath = $logicalPath
            PhysicalPath = $physicalPath
            Bytes = [long]0
        })
        continue
    }

    $bytes = if ($null -ne $logicalItem) { Get-DirectoryBytes -Path $logicalPath } else { Get-DirectoryBytes -Path $physicalPath }
    if ($Apply) {
        if ($null -ne $logicalItem) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $physicalPath) -Force | Out-Null
            Copy-Verify-AndRemoveDirectory -Source $logicalPath -Destination $physicalPath
        }

        New-Item -ItemType Junction -Path $logicalPath -Target $physicalPath | Out-Null
    }

    $results.Add([pscustomobject]@{
        Name = $mapping.Name
        State = if ($Apply) {
            "migrated-and-junctioned"
        }
        elseif ($null -ne $logicalItem -and $physicalExists) {
            "would-resume-migration-and-junction"
        }
        else {
            "would-migrate-and-junction"
        }
        LogicalPath = $logicalPath
        PhysicalPath = $physicalPath
        Bytes = $bytes
    })
}

if ($Apply) {
    foreach ($mapping in $mappings) {
        $logicalItem = Get-Item -LiteralPath $mapping.LogicalPath -Force
        $junctionTarget = Get-JunctionTarget -Item $logicalItem
        $expectedTarget = [System.IO.Path]::GetFullPath($mapping.PhysicalPath).TrimEnd("\")
        if (-not $junctionTarget -or -not $junctionTarget.Equals($expectedTarget, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Migration verification failed for $($mapping.LogicalPath)"
        }
    }

    $evidenceDirectory = Join-Path $storageRoot "migration-evidence"
    New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
    $evidencePath = Join-Path $evidenceDirectory "test-storage-migration.json"
    [pscustomobject]@{
        SchemaVersion = 1
        CreatedAt = [DateTimeOffset]::Now.ToString("o")
        RepositoryRoot = $repoRoot
        StorageRoot = $storageRoot
        Results = $results
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $evidencePath -Encoding utf8
    Write-Output "MIGRATION_EVIDENCE=$evidencePath"
}

$results | Select-Object Name, State, LogicalPath, PhysicalPath, Bytes
