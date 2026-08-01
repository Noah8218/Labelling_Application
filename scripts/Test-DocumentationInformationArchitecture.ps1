[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $scriptRoot)).TrimEnd("\")
$docsRoot = Join-Path $repoRoot "docs"
$indexPath = Join-Path $docsRoot "README.md"
$rootReadmePath = Join-Path $repoRoot "README.md"
$agentPath = Join-Path $repoRoot "AGENTS.md"
$ciPath = Join-Path $repoRoot ".github\workflows\ci.yml"

if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git") -PathType Container)) {
    throw "Repository root could not be verified: $repoRoot"
}

if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "Documentation index does not exist: $indexPath"
}

foreach ($requiredFile in @($rootReadmePath, $agentPath, $ciPath)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Documentation navigation contract file does not exist: $requiredFile"
    }
}

$index = Get-Content -LiteralPath $indexPath -Raw -Encoding utf8
$requiredHeadings = @(
    "## Start here",
    "## Lifecycle labels",
    "## 1. Current authority and repository navigation",
    "## 2. Operator guides and repeatable procedures",
    "## 3. Productization, release, recovery, and external validation",
    "## 4. Feature contracts and completion records",
    "## 5. Verification evidence, datasets, and performance analysis",
    "## 6. Historical plans, audits, and migration records",
    "## Contribution checklist"
)

foreach ($heading in $requiredHeadings) {
    if ($index.IndexOf($heading, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Documentation index is missing required heading: $heading"
    }
}

$documents = Get-ChildItem -LiteralPath $docsRoot -File -Recurse -Filter *.md |
    Where-Object { -not ($_.FullName.Equals($indexPath, [System.StringComparison]::OrdinalIgnoreCase)) } |
    Sort-Object FullName
$missing = [System.Collections.Generic.List[string]]::new()
$duplicate = [System.Collections.Generic.List[string]]::new()
$classificationStart = $index.IndexOf(
    "## 1. Current authority and repository navigation",
    [System.StringComparison]::Ordinal)
$classificationEnd = $index.IndexOf(
    "## Contribution checklist",
    $classificationStart,
    [System.StringComparison]::Ordinal)
if ($classificationStart -lt 0 -or $classificationEnd -le $classificationStart) {
    throw "Documentation classification section boundaries are invalid."
}

$classificationIndex = $index.Substring(
    $classificationStart,
    $classificationEnd - $classificationStart)
$lifecycleEntryPattern = '(?m)^- `(CURRENT|GUIDE|CONTRACT|EVIDENCE|HISTORY)` \[[^\]]+\]\([^)]+\)\s*$'
$lifecycleEntryCount = [regex]::Matches(
    $classificationIndex,
    $lifecycleEntryPattern).Count
if ($lifecycleEntryCount -ne $documents.Count) {
    throw "Every classified Markdown document must have exactly one lifecycle label. Expected $($documents.Count), found $lifecycleEntryCount."
}

foreach ($document in $documents) {
    $relativePath = $document.FullName.Substring($docsRoot.Length + 1).Replace("\", "/")
    $linkPattern = "\(" + [regex]::Escape($relativePath) + "(?:#[^)]+)?\)"
    $referenceCount = [regex]::Matches(
        $classificationIndex,
        $linkPattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
    if ($referenceCount -eq 0) {
        $missing.Add($relativePath)
    }
    elseif ($referenceCount -gt 1) {
        $duplicate.Add("$relativePath ($referenceCount references)")
    }
}

if ($missing.Count -gt 0) {
    throw "Documentation index is missing classification links: $($missing -join ', ')"
}

if ($duplicate.Count -gt 0) {
    throw "Documentation index classifies files more than once: $($duplicate -join ', ')"
}

$brokenLinks = [System.Collections.Generic.List[string]]::new()
$markdownLinks = [regex]::Matches($index, "\[[^\]]+\]\((?<target>[^)]+)\)")
foreach ($markdownLink in $markdownLinks) {
    $target = $markdownLink.Groups["target"].Value.Trim()
    if ($target -match "^(?i:https?://|mailto:)") {
        continue
    }

    $pathOnly = ($target -split "#", 2)[0]
    if ([string]::IsNullOrWhiteSpace($pathOnly)) {
        continue
    }

    $decodedPath = [System.Uri]::UnescapeDataString($pathOnly).Replace("/", "\")
    $resolvedPath = [System.IO.Path]::GetFullPath((Join-Path $docsRoot $decodedPath))
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        $brokenLinks.Add($target)
    }
}

if ($brokenLinks.Count -gt 0) {
    throw "Documentation index has broken local links: $($brokenLinks -join ', ')"
}

$rootReadme = Get-Content -LiteralPath $rootReadmePath -Raw -Encoding utf8
$agent = Get-Content -LiteralPath $agentPath -Raw -Encoding utf8
$ci = Get-Content -LiteralPath $ciPath -Raw -Encoding utf8
if ($rootReadme.IndexOf("(docs/README.md)", [System.StringComparison]::Ordinal) -lt 0) {
    throw "Root README does not link to the documentation index."
}

if ($agent.IndexOf("docs\README.md", [System.StringComparison]::Ordinal) -lt 0) {
    throw "Repository AGENT does not define documentation-index ownership."
}

if ($ci.IndexOf("Test-DocumentationInformationArchitecture.ps1", [System.StringComparison]::Ordinal) -lt 0) {
    throw "CI does not run the documentation information-architecture verifier."
}

Write-Output "DOCUMENTATION_INDEX=docs/README.md"
Write-Output "CLASSIFIED_MARKDOWN_FILES=$($documents.Count)"
Write-Output "LIFECYCLE_LABELED_FILES=$lifecycleEntryCount"
Write-Output "BROKEN_LOCAL_LINKS=0"
Write-Output "DUPLICATE_CLASSIFICATIONS=0"
Write-Output "ROOT_NAVIGATION_AND_CI_CONTRACT=PASS"
