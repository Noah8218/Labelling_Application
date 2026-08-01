[CmdletBinding()]
param(
    [string]$ReleaseDirectory = "C:\P0C\Release",

    [string]$EvidenceDirectory = "C:\P0C\Evidence",

    [string]$WorkflowRoot = "C:\P0C\Evidence\workflow-host",

    [string]$StagedReleaseDirectory = "C:\P0C\WorkingRelease",

    [string]$RecipeName = "P0C_Portable_Label",

    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
$recipeName = $RecipeName
$outputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    Join-Path $EvidenceDirectory "workflow-output"
}
else {
    $OutputRoot
}
$sourceImage = Join-Path $WorkflowRoot "source\000000000042.jpg"
$targetImage = Join-Path $outputRoot "data\train\images\000000000042.jpg"
$targetLabel = Join-Path $outputRoot "data\train\labels\000000000042.txt"
$resultPath = Join-Path $EvidenceDirectory "label-workflow-result.json"
$screenshotPath = Join-Path $EvidenceDirectory "sandbox-label-save.png"
$failureScreenshotPath = Join-Path $EvidenceDirectory "sandbox-label-workflow-failure.png"
$steps = New-Object System.Collections.Generic.List[object]
$applicationProcess = $null
$failure = $null

function Save-Progress {
    $progress = [ordered]@{
        recordedAtUtc = [DateTime]::UtcNow.ToString("O")
        recipeName = $recipeName
        outputRoot = $outputRoot
        steps = $steps
    }
    $progress |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $EvidenceDirectory "label-workflow-progress.json") -Encoding UTF8
}

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
    Save-Progress
}

function Wait-MainWindow {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutSeconds = 45
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if ($Process.HasExited) {
            throw "Application exited before its main window opened. ExitCode=$($Process.ExitCode)"
        }
        $Process.Refresh()
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            return
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Application main window did not open within $TimeoutSeconds seconds."
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

function Invoke-Element {
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
    throw "Element cannot be invoked: $($Element.Current.AutomationId)"
}

function Set-ElementValue {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Value
    )

    $pattern = $null
    if (-not $Element.TryGetCurrentPattern(
        [System.Windows.Automation.ValuePattern]::Pattern,
        [ref]$pattern)) {
        throw "Element is not editable: $($Element.Current.AutomationId)"
    }
    ([System.Windows.Automation.ValuePattern]$pattern).SetValue($Value)
}

function Expand-Element {
    param([System.Windows.Automation.AutomationElement]$Element)

    $pattern = $null
    if (-not $Element.TryGetCurrentPattern(
        [System.Windows.Automation.ExpandCollapsePattern]::Pattern,
        [ref]$pattern)) {
        throw "Element cannot expand: $($Element.Current.AutomationId)"
    }
    $expandPattern = [System.Windows.Automation.ExpandCollapsePattern]$pattern
    if ($expandPattern.Current.ExpandCollapseState -ne
        [System.Windows.Automation.ExpandCollapseState]::Expanded) {
        $expandPattern.Expand()
    }
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

    throw "File '$Path' was not created within $TimeoutSeconds seconds."
}

function Assert-ReleasePayload {
    param(
        [string]$Directory,
        [object[]]$Files
    )

    foreach ($file in $Files) {
        $path = Join-Path $Directory $file.path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Staged release payload is missing: $($file.path)"
        }

        $item = Get-Item -LiteralPath $path
        if ($item.Length -ne [long]$file.length) {
            throw "Staged release payload length changed: $($file.path)"
        }

        $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if (-not [string]::Equals(
            $actualHash,
            [string]$file.sha256,
            [StringComparison]::OrdinalIgnoreCase)) {
            throw "Staged release payload hash changed: $($file.path)"
        }
    }
}

function Initialize-WindowInterop {
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName System.Windows.Forms
    if ($null -eq ("P0CLabelWorkflowWindow" -as [type])) {
        Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class P0CLabelWorkflowWindow
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

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(
        IntPtr windowHandle,
        int x,
        int y,
        int width,
        int height,
        bool repaint);
}
'@
    }
}

function Get-WindowBounds {
    param([IntPtr]$WindowHandle)

    Initialize-WindowInterop
    $rectangle = New-Object P0CLabelWorkflowWindow+RECT
    if (-not [P0CLabelWorkflowWindow]::GetWindowRect($WindowHandle, [ref]$rectangle)) {
        throw "Unable to resolve application window bounds."
    }

    return [System.Drawing.Rectangle]::FromLTRB(
        $rectangle.Left,
        $rectangle.Top,
        $rectangle.Right,
        $rectangle.Bottom)
}

function Place-ExeSmokeWindowOnLeftmostMonitor {
    param(
        [IntPtr]$WindowHandle,
        [string]$EvidencePath
    )

    Initialize-WindowInterop
    $screens = @([System.Windows.Forms.Screen]::AllScreens)
    if ($screens.Count -eq 0) {
        throw "No active display is available for the EXE smoke test."
    }

    $monitor = $screens |
        Sort-Object { $_.Bounds.Left }, { $_.Bounds.Top } |
        Select-Object -First 1
    $currentBounds = Get-WindowBounds -WindowHandle $WindowHandle
    $workingArea = $monitor.WorkingArea
    $width = [Math]::Min($currentBounds.Width, $workingArea.Width)
    $height = [Math]::Min($currentBounds.Height, $workingArea.Height)
    $left = $workingArea.Left + [Math]::Max(0, [int](($workingArea.Width - $width) / 2))
    $top = $workingArea.Top + [Math]::Max(0, [int](($workingArea.Height - $height) / 2))

    if (-not [P0CLabelWorkflowWindow]::MoveWindow(
        $WindowHandle,
        $left,
        $top,
        $width,
        $height,
        $true)) {
        throw "Unable to place the EXE smoke window on the leftmost monitor."
    }

    Start-Sleep -Milliseconds 250
    $actualBounds = Get-WindowBounds -WindowHandle $WindowHandle
    $intersects = $monitor.Bounds.IntersectsWith($actualBounds)
    $placement = [ordered]@{
        recordedAtUtc = [DateTime]::UtcNow.ToString("O")
        monitorCount = $screens.Count
        fallback = if ($screens.Count -eq 1) { "single-monitor" } else { "none" }
        monitor = [ordered]@{
            deviceName = $monitor.DeviceName
            left = $monitor.Bounds.Left
            top = $monitor.Bounds.Top
            width = $monitor.Bounds.Width
            height = $monitor.Bounds.Height
        }
        window = [ordered]@{
            left = $actualBounds.Left
            top = $actualBounds.Top
            width = $actualBounds.Width
            height = $actualBounds.Height
        }
        intersects = $intersects
    }
    $placement |
        ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $EvidencePath -Encoding UTF8

    if (-not $intersects) {
        throw "The EXE smoke window does not intersect the selected leftmost monitor '$($monitor.DeviceName)'."
    }

    return $placement
}

function Save-WindowCapture {
    param(
        [IntPtr]$WindowHandle,
        [string]$Path
    )

    $bounds = Get-WindowBounds -WindowHandle $WindowHandle
    $bitmap = New-Object System.Drawing.Bitmap(
        $bounds.Width,
        $bounds.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $bounds.Left,
            $bounds.Top,
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
    New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
    if (-not (Test-Path -LiteralPath $sourceImage -PathType Leaf)) {
        throw "Workflow fixture image is missing: $sourceImage"
    }

    $manifestPath = Join-Path $ReleaseDirectory "release-manifest.json"
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.source.dirty -ne $false) {
        throw "Label workflow requires a clean-source release."
    }

    if (Test-Path -LiteralPath $StagedReleaseDirectory) {
        throw "The guest release staging directory already exists: $StagedReleaseDirectory"
    }
    New-Item -ItemType Directory -Path $StagedReleaseDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $ReleaseDirectory -Force |
        Copy-Item -Destination $StagedReleaseDirectory -Recurse -Force
    Assert-ReleasePayload `
        -Directory $StagedReleaseDirectory `
        -Files @($manifest.files)
    Add-Step `
        "release-staged" `
        "pass" `
        "$(@($manifest.files).Count) manifest payload files verified"

    $applicationPath = Join-Path $StagedReleaseDirectory "OpenVisionLab.LabelingStudio.exe"
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes

    $applicationProcess = Start-Process `
        -FilePath $applicationPath `
        -WorkingDirectory $WorkflowRoot `
        -PassThru
    Wait-MainWindow -Process $applicationProcess
    Add-Step "packaged-first-launch" "pass" "PID $($applicationProcess.Id)"
    $placement = Place-ExeSmokeWindowOnLeftmostMonitor `
        -WindowHandle $applicationProcess.MainWindowHandle `
        -EvidencePath (Join-Path $EvidenceDirectory "monitor-placement.json")
    Add-Step `
        "exe-window-leftmost-monitor" `
        "pass" `
        "$($placement.monitor.deviceName) | $($placement.window.left),$($placement.window.top),$($placement.window.width),$($placement.window.height)"

    $changeDataset = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "ChangeDatasetButton"
    Invoke-Element -Element $changeDataset

    $createFirst = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "CreateFirstDatasetButton"
    Invoke-Element -Element $createFirst

    $recipeNameBox = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "WizardRecipeNameBox"
    $outputRootBox = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "WizardOutputRootPathBox"
    $classNamesBox = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "WizardClassNamesBox"
    Set-ElementValue -Element $recipeNameBox -Value $recipeName
    Set-ElementValue -Element $outputRootBox -Value $outputRoot
    Set-ElementValue -Element $classNamesBox -Value "Defect"
    Add-Step "dataset-wizard-values" "pass" $outputRoot

    $createDataset = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "WizardCreateButton"
    if (-not $createDataset.Current.IsEnabled) {
        throw "Dataset create button is disabled after valid values were supplied."
    }
    Invoke-Element -Element $createDataset

    $datasetYamlPath = Join-Path $outputRoot "data.yaml"
    Wait-File -Path $datasetYamlPath
    $currentDatasetName = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "CurrentDatasetNameText"
    Add-Step `
        "dataset-created" `
        "pass" `
        "$recipeName | UI name: $($currentDatasetName.Current.Name) | $datasetYamlPath"

    New-Item -ItemType Directory -Path (Split-Path -Parent $targetImage) -Force | Out-Null
    Copy-Item -LiteralPath $sourceImage -Destination $targetImage -Force
    Add-Step "fixture-image-added" "pass" $targetImage

    $loadImageRoot = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "LoadConfiguredImageRootButton"
    Invoke-Element -Element $loadImageRoot

    $nextUnlabeled = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "NextUnlabeledPrimaryButton"
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    while (-not $nextUnlabeled.Current.IsEnabled -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 300
        $nextUnlabeled = Find-AutomationElement `
            -ProcessId $applicationProcess.Id `
            -AutomationId "NextUnlabeledPrimaryButton" `
            -TimeoutSeconds 2
    }
    if (-not $nextUnlabeled.Current.IsEnabled) {
        throw "Image queue did not enable the next-unlabeled action."
    }
    Invoke-Element -Element $nextUnlabeled
    Start-Sleep -Seconds 2
    Add-Step "image-open-requested" "pass" (Split-Path -Leaf $targetImage)

    $menuButton = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "HeaderToolsMenuButton"
    Invoke-Element -Element $menuButton -Toggle
    $templateSection = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "HeaderToolsMenuTemplateSection"
    Expand-Element -Element $templateSection

    $addSampleRoi = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "AddSampleRoiButton"
    if (-not $addSampleRoi.Current.IsEnabled) {
        throw "The sample box command is disabled after opening the fixture image."
    }
    Invoke-Element -Element $addSampleRoi

    $canvasSaveAnnotation = Find-AutomationElement `
        -ProcessId $applicationProcess.Id `
        -AutomationId "CanvasSaveAnnotationButton"
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not $canvasSaveAnnotation.Current.IsEnabled -and
        [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 300
        $canvasSaveAnnotation = Find-AutomationElement `
            -ProcessId $applicationProcess.Id `
            -AutomationId "CanvasSaveAnnotationButton" `
            -TimeoutSeconds 2
    }
    if (-not $canvasSaveAnnotation.Current.IsEnabled) {
        throw "The canvas label save command is disabled after adding a box."
    }
    Add-Step "sample-box-added" "pass" "central rectangle"
    Add-Step `
        "explicit-save-ready" `
        "pass" `
        "canvas label save enabled"
    Invoke-Element -Element $canvasSaveAnnotation

    Wait-File -Path $targetLabel
    $labelText = (Get-Content -LiteralPath $targetLabel -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($labelText)) {
        throw "The saved label file is empty."
    }
    Add-Step "explicit-label-save" "pass" $labelText

    $recipeConfigPath = Join-Path `
        $StagedReleaseDirectory `
        "RECIPE\$recipeName\VISION.xml"
    Wait-File -Path $recipeConfigPath
    Add-Step "recipe-config-persisted" "pass" $recipeConfigPath

    Assert-ReleasePayload `
        -Directory $StagedReleaseDirectory `
        -Files @($manifest.files)
    Add-Step `
        "staged-payload-unchanged" `
        "pass" `
        "$(@($manifest.files).Count) manifest payload files verified after save"

    Save-WindowCapture `
        -WindowHandle $applicationProcess.MainWindowHandle `
        -Path $screenshotPath
    Add-Step "guest-window-capture" "pass" (Split-Path -Leaf $screenshotPath)
}
catch {
    $failure = $_.Exception.GetType().Name + ": " + $_.Exception.Message
    if ($null -ne $applicationProcess -and
        -not $applicationProcess.HasExited -and
        $applicationProcess.MainWindowHandle -ne [IntPtr]::Zero) {
        try {
            Save-WindowCapture `
                -WindowHandle $applicationProcess.MainWindowHandle `
                -Path $failureScreenshotPath
            Add-Step "guest-failure-capture" "pass" (Split-Path -Leaf $failureScreenshotPath)
        }
        catch {
            Add-Step "guest-failure-capture" "warning" $_.Exception.Message
        }
    }
    Add-Step "label-workflow" "fail" $failure
}
finally {
    if ($null -ne $applicationProcess -and -not $applicationProcess.HasExited) {
        $applicationProcess.CloseMainWindow() | Out-Null
        if ($applicationProcess.WaitForExit(10000)) {
            Add-Step "normal-close" "pass" "Application closed normally."
        }
        else {
            Add-Step "normal-close" "warning" "Application remained open after the normal close request."
        }
    }

    $result = [ordered]@{
        schemaVersion = 1
        completedAtUtc = [DateTime]::UtcNow.ToString("O")
        status = if ([string]::IsNullOrWhiteSpace($failure)) { "pass" } else { "fail" }
        failure = $failure
        recipeName = $recipeName
        outputRoot = $outputRoot
        sourceImage = $sourceImage
        savedLabel = $targetLabel
        steps = $steps
    }
    $result |
        ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath $resultPath -Encoding UTF8

    if (-not [string]::IsNullOrWhiteSpace($failure)) {
        Write-Error $failure
        exit 1
    }

    Write-Output "P0-C packaged label workflow passed."
    Write-Output "Result: $resultPath"
}
