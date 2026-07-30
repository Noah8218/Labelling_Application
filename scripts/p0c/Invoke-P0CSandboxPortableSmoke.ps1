[CmdletBinding()]
param(
    [string]$ReleaseDirectory = "C:\P0C\Release",

    [string]$EvidenceDirectory = "C:\P0C\Evidence"
)

$ErrorActionPreference = "Stop"
$startedAtUtc = [DateTime]::UtcNow
$steps = New-Object System.Collections.Generic.List[object]
$applicationProcess = $null
$failure = $null
$selfTestTitle = ""
$supportTitle = ""
$packageUnchanged = $false
$screenshotPath = Join-Path $EvidenceDirectory "sandbox-packaged-runtime-diagnostics.png"

New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null

function Add-Step {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Detail
    )

    $steps.Add([ordered]@{
        name = $Name
        status = $Status
        detail = $Detail
        recordedAtUtc = [DateTime]::UtcNow.ToString("O")
    })
}

function Get-PackageSnapshot {
    param([string]$Root)

    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    return @(
        Get-ChildItem -LiteralPath $Root -Recurse -File |
            Sort-Object FullName |
            ForEach-Object {
                [ordered]@{
                    path = $_.FullName.Substring($resolvedRoot.Length).Replace('\', '/')
                    length = $_.Length
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            }
    )
}

function Wait-MainWindow {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutSeconds = 45
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if ($Process.HasExited) {
            throw "Packaged application exited before its main window opened. ExitCode=$($Process.ExitCode)"
        }

        $Process.Refresh()
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            return
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Packaged application main window did not open within $TimeoutSeconds seconds."
}

function Find-AutomationElement {
    param(
        [int]$ProcessId,
        [string]$AutomationId,
        [int]$TimeoutSeconds = 15
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $processCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)

    do {
        $elements = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            $processCondition)

        foreach ($element in $elements) {
            if ($element.Current.AutomationId -eq $AutomationId) {
                return $element
            }
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Automation element was not found: $AutomationId"
}

function Invoke-AutomationElement {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [switch]$Toggle
    )

    $pattern = $null
    if ($Toggle.IsPresent -and
        $Element.TryGetCurrentPattern(
            [System.Windows.Automation.TogglePattern]::Pattern,
            [ref]$pattern)) {
        ([System.Windows.Automation.TogglePattern]$pattern).Toggle()
        return
    }

    if ($Element.TryGetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern,
        [ref]$pattern)) {
        ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
        return
    }

    throw "Automation element does not expose the required pattern: $($Element.Current.AutomationId)"
}

function Wait-ElementName {
    param(
        [int]$ProcessId,
        [string]$AutomationId,
        [string]$ExpectedPrefix,
        [int]$TimeoutSeconds = 30
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $element = Find-AutomationElement `
            -ProcessId $ProcessId `
            -AutomationId $AutomationId `
            -TimeoutSeconds 2
        $name = $element.Current.Name
        if ($name.StartsWith($ExpectedPrefix, [StringComparison]::Ordinal)) {
            return $name
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Automation element '$AutomationId' did not reach '$ExpectedPrefix'."
}

function Wait-File {
    param(
        [string]$Path,
        [int]$TimeoutSeconds = 30
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (Test-Path -LiteralPath $Path -PathType Leaf) {
            return
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Expected file was not created: $Path"
}

function Wait-NewestFile {
    param(
        [string]$Directory,
        [string]$Filter,
        [DateTime]$NotBeforeUtc,
        [int]$TimeoutSeconds = 45
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $file = Get-ChildItem `
            -LiteralPath $Directory `
            -Filter $Filter `
            -File `
            -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTimeUtc -ge $NotBeforeUtc } |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($null -ne $file) {
            return $file
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Expected file was not created in '$Directory': $Filter"
}

function Save-WindowCapture {
    param(
        [IntPtr]$WindowHandle,
        [string]$Path
    )

    Add-Type -AssemblyName System.Drawing
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class P0CSandboxCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr windowHandle, out RECT rectangle);
}
'@

    $rectangle = New-Object P0CSandboxCapture+RECT
    if (-not [P0CSandboxCapture]::GetWindowRect($WindowHandle, [ref]$rectangle)) {
        throw "Unable to resolve packaged application window bounds."
    }

    $width = $rectangle.Right - $rectangle.Left
    $height = $rectangle.Bottom - $rectangle.Top
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $rectangle.Left,
            $rectangle.Top,
            0,
            0,
            $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

try {
    $manifestPath = Join-Path $ReleaseDirectory "release-manifest.json"
    $applicationPath = Join-Path $ReleaseDirectory "OpenVisionLab.LabelingStudio.exe"
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
        throw "The mapped release bundle is incomplete."
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.source.dirty -ne $false -or
        $manifest.build.runtimeIdentifier -ne "win-x64" -or
        $manifest.build.selfContained -ne $true) {
        throw "The mapped release is not a clean-source self-contained win-x64 bundle."
    }
    Add-Step "release-contract" "pass" "$($manifest.productVersion) / $($manifest.source.commit)"

    $beforeSnapshot = Get-PackageSnapshot -Root $ReleaseDirectory
    Add-Step "package-snapshot-before" "pass" "$($beforeSnapshot.Count) files"

    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes

    $applicationProcess = Start-Process -FilePath $applicationPath -PassThru
    Wait-MainWindow -Process $applicationProcess
    Add-Step "packaged-first-launch" "pass" "PID $($applicationProcess.Id)"

    $applicationDataRoot = Join-Path $env:LOCALAPPDATA "OpenVisionLab\LabelingStudio"
    $diagnosticsDirectory = Join-Path $applicationDataRoot "Diagnostics"
    $supportDirectory = Join-Path $applicationDataRoot "SupportBundles"
    $logDirectory = Join-Path $applicationDataRoot "Logs"
    $latestSelfTestPath = Join-Path $diagnosticsDirectory "self-test-latest.json"

    $menuButton = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "HeaderToolsMenuButton"
    Invoke-AutomationElement -Element $menuButton -Toggle

    $selfTestButton = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "RuntimeSelfTestButton"
    Invoke-AutomationElement -Element $selfTestButton

    Wait-File -Path $latestSelfTestPath
    $selfTest = Get-Content -LiteralPath $latestSelfTestPath -Raw | ConvertFrom-Json
    if ([int]$selfTest.failedCount -gt 0) {
        throw "Packaged runtime self-test reported $($selfTest.failedCount) failed checks."
    }
    $selfTestStatusElement = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "RuntimeDiagnosticsStatusTitleText"
    $selfTestTitle = $selfTestStatusElement.Current.Name
    Add-Step `
        "runtime-self-test" `
        "pass" `
        "passed=$($selfTest.passedCount), warnings=$($selfTest.warningCount), failed=$($selfTest.failedCount)"

    $supportStartedAtUtc = [DateTime]::UtcNow
    $supportButton = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "CreateSupportBundleButton"
    Invoke-AutomationElement -Element $supportButton

    $supportArchive = Wait-NewestFile `
        -Directory $supportDirectory `
        -Filter "OpenVisionLab-Support-*.zip" `
        -NotBeforeUtc $supportStartedAtUtc
    $supportStatusElement = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "RuntimeDiagnosticsStatusTitleText"
    $supportTitle = $supportStatusElement.Current.Name
    Add-Step "support-bundle-ui" "pass" $supportArchive.Name

    Save-WindowCapture `
        -WindowHandle $applicationProcess.MainWindowHandle `
        -Path $screenshotPath
    Add-Step "guest-window-capture" "pass" (Split-Path -Leaf $screenshotPath)

    foreach ($sourceDirectory in @($diagnosticsDirectory, $supportDirectory, $logDirectory)) {
        if (Test-Path -LiteralPath $sourceDirectory -PathType Container) {
            $targetDirectory = Join-Path $EvidenceDirectory (Split-Path -Leaf $sourceDirectory)
            Copy-Item -LiteralPath $sourceDirectory -Destination $targetDirectory -Recurse -Force
        }
    }
    Add-Step "runtime-evidence-copy" "pass" $applicationDataRoot

    $afterSnapshot = Get-PackageSnapshot -Root $ReleaseDirectory
    $beforeJson = $beforeSnapshot | ConvertTo-Json -Depth 3 -Compress
    $afterJson = $afterSnapshot | ConvertTo-Json -Depth 3 -Compress
    $packageUnchanged = $beforeJson -eq $afterJson
    if (-not $packageUnchanged) {
        throw "The read-only release package changed during first launch and diagnostics."
    }
    Add-Step "package-immutability" "pass" "$($afterSnapshot.Count) files unchanged"
}
catch {
    $failure = $_.Exception.GetType().Name + ": " + $_.Exception.Message
    Add-Step "portable-smoke" "fail" $failure
}
finally {
    if ($null -ne $applicationProcess -and -not $applicationProcess.HasExited) {
        $applicationProcess.CloseMainWindow() | Out-Null
        if (-not $applicationProcess.WaitForExit(10000)) {
            Add-Step "normal-close" "warning" "Application remained open after the normal close request."
        }
        else {
            Add-Step "normal-close" "pass" "Packaged application closed normally."
        }
    }

    $result = [ordered]@{
        schemaVersion = 1
        startedAtUtc = $startedAtUtc.ToString("O")
        completedAtUtc = [DateTime]::UtcNow.ToString("O")
        status = if ([string]::IsNullOrWhiteSpace($failure)) { "pass" } else { "fail" }
        failure = $failure
        selfTestTitle = $selfTestTitle
        supportTitle = $supportTitle
        packageUnchanged = $packageUnchanged
        releaseDirectory = $ReleaseDirectory
        applicationDataRoot = Join-Path $env:LOCALAPPDATA "OpenVisionLab\LabelingStudio"
        steps = $steps
    }

    $resultPath = Join-Path $EvidenceDirectory "portable-smoke-result.json"
    $result |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath $resultPath -Encoding UTF8

    if (-not [string]::IsNullOrWhiteSpace($failure)) {
        Write-Error $failure
        exit 1
    }

    Write-Output "P0-C portable Sandbox smoke passed."
    Write-Output "Result: $resultPath"
}
