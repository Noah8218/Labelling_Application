using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.DrawObject;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.ImageCanvas.Model;
using OpenVisionLab.ImageCanvas.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CvMat = OpenCvSharp.Mat;
using CvMatType = OpenCvSharp.MatType;
using CvScalar = OpenCvSharp.Scalar;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class WpfShellStructureTests
{
    internal static void TestWpfLabelingShellWindowConstructs()
    {
        string root = FindRepositoryRoot();
        XName xName = XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");
        XDocument shellXaml = XDocument.Load(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.xaml"));
        XDocument queueXaml = XDocument.Load(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfImageQueuePanel.xaml"));
        string shellSource = ReadWpfLabelingShellWindowSources();
        string shellXamlSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.xaml"));
        string imageQueueViewModelSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "ViewModels", "WpfImageQueuePanelViewModel.cs"));
        string datasetContextPresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Dataset", "WpfDatasetContextPresentationService.cs"));
        string workflowStagePresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Training", "WpfWorkflowStagePresentationService.cs"));
        string inputCommandBehaviorSource = File.ReadAllText(Path.Combine(root, "OpenVisionLab", "Library", "OpenVisionLab.Mvvm", "Behaviors", "InputCommandBehaviors.cs"));
        string shellViewModelSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "ViewModels", "WpfLabelingShellViewModel.cs"));
        string yoloRuntimeStatusSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.YoloRuntimeStatus.cs"));
        string inferenceStatusPresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Detection", "WpfInferenceStatusPresentationService.cs"));
        string keyInputArgsSource = File.ReadAllText(Path.Combine(root, "OpenVisionLab", "Library", "OpenVisionLab.Mvvm", "InputCommandArgs.cs"));
        string testProgramSource = File.ReadAllText(Path.Combine(root, "tests", "LabelingApplication.Tests", "Program.cs"));

        AssertEqual("1920", (string)shellXaml.Root?.Attribute("Width"));
        AssertEqual("1080", (string)shellXaml.Root?.Attribute("Height"));
        AssertEqual("1100", (string)shellXaml.Root?.Attribute("MinWidth"));
        AssertEqual("720", (string)shellXaml.Root?.Attribute("MinHeight"));
        AssertTrue(shellSource.Contains("PreferredInitialShellWidth = 1920D", StringComparison.Ordinal), "WPF shell should prefer a 1920-wide equipment baseline at startup");
        AssertTrue(shellSource.Contains("PreferredInitialShellHeight = 1080D", StringComparison.Ordinal), "WPF shell should prefer a 1080-high equipment baseline at startup");
        AssertTrue(shellSource.Contains("ApplyInitialWindowSizeToWorkArea", StringComparison.Ordinal), "WPF shell startup size should be clamped to the current monitor work area");
        AssertTrue(shellSource.Contains("SystemParameters.WorkArea", StringComparison.Ordinal), "WPF shell should account for smaller monitor work areas instead of only hard-coding the baseline");
        AssertTrue(testProgramSource.Contains("VisualSmokeDefaultWindowWidth = 1920", StringComparison.Ordinal), "visual smoke should use the 1920 equipment baseline by default");
        AssertTrue(testProgramSource.Contains("VisualSmokeDefaultWindowHeight = 1080", StringComparison.Ordinal), "visual smoke should use the 1080 equipment baseline by default");
        AssertTrue(testProgramSource.Contains("VisualSmokeMinimumWindowWidth = 1100", StringComparison.Ordinal), "visual smoke should retain the small-width regression floor");
        AssertTrue(testProgramSource.Contains("VisualSmokeMinimumWindowHeight = 720", StringComparison.Ordinal), "visual smoke should retain the small-height regression floor");
        AssertNamedXamlElement(shellXaml, xName, "Border", "CurrentDatasetPurposeBadge");
        AssertNamedXamlValue(shellXaml, xName, "CurrentDatasetPurposeBadge", "Grid.Column", "3");
        AssertNamedXamlValue(shellXaml, xName, "CurrentDatasetPurposeBadge", "HorizontalAlignment", "Right");
        AssertTrue(WpfInferenceStatusPresentationService.BuildRuntimePythonStatus(
            new PythonModelValidationResult(Array.Empty<string>(), Array.Empty<string>()),
            new PythonModelRuntimeState(PythonModelRuntimeStateKind.Ready, true, true, "fallback", "detail", "next")).Contains("\uC900\uBE44 \uC644\uB8CC", StringComparison.Ordinal), "inference runtime-ready status should be built by the presentation service");
        AssertTrue(WpfInferenceStatusPresentationService.BuildInspectionModelStatusText(new PythonModelSettings { WeightsPath = string.Empty }, hasPendingModelCandidate: false).Contains("\uC5C6\uC74C", StringComparison.Ordinal), "missing inspection-model status should be built by the presentation service");
        AssertTrue(inferenceStatusPresentationSource.Contains("BuildRuntimePythonStatus", StringComparison.Ordinal), "inference status presentation service should own runtime python status wording");
        AssertTrue(yoloRuntimeStatusSource.Contains("BuildRuntimePythonStatus", StringComparison.Ordinal), "YOLO runtime status should delegate runtime python status wording to the presentation service");
        AssertTrue(yoloRuntimeStatusSource.Contains("BuildInspectionModelStatusText", StringComparison.Ordinal), "YOLO runtime status should delegate inspection model/candidate wording to the presentation service");
        AssertTrue(!yoloRuntimeStatusSource.Contains("\uCD94\uB860: \uC900\uBE44 \uC644\uB8CC", StringComparison.Ordinal), "YOLO runtime status should not inline inference-ready wording");
        AssertTrue(!yoloRuntimeStatusSource.Contains("\uAC80\uC0AC \uBAA8\uB378: \uC5C6\uC74C", StringComparison.Ordinal), "YOLO runtime status should not inline missing inspection-model wording");
        AssertTrue(!ContainsVisibleMojibakeArtifact(yoloRuntimeStatusSource), "YOLO runtime status source should not expose mojibake artifacts");
        AssertNamedXamlBinding(shellXaml, xName, "DetectButton", "IsEnabled", "ShellViewModel.IsCurrentImageDetectionEnabled");
        AssertNamedXamlElement(shellXaml, xName, "ToggleButton", "HeaderToolsMenuButton");
        AssertNamedXamlElement(shellXaml, xName, "Popup", "HeaderToolsPopup");
        AssertNamedXamlElement(shellXaml, xName, "PackIconMaterial", "HeaderToolsMenuIcon");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "HeaderToolsMenuTitleText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "HeaderToolsMenuScreenSectionText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "HeaderToolsMenuAssistSectionText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "HeaderToolsMenuWorkflowSectionText");
        AssertNamedXamlElement(shellXaml, xName, "Border", "HeaderTemplateFlowGuidePanel");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "HeaderTemplateFlowTitleText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "HeaderTemplateFlowStepText");
        AssertNamedXamlElement(shellXaml, xName, "Button", "HeaderTemplateBatchButton");
        AssertNamedXamlBinding(shellXaml, xName, "HeaderTemplateBatchButton", "Command", "ImageQueueViewModel.TemplateBatchQueueCommand");
        AssertNamedXamlBinding(shellXaml, xName, "HeaderTemplateBatchButton", "IsEnabled", "ImageQueueViewModel.IsTemplateBatchEnabled");
        XElement headerToolsButton = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "ToggleButton"
                && string.Equals((string)candidate.Attribute(xName), "HeaderToolsMenuButton", StringComparison.Ordinal));
        AssertTrue(
            headerToolsButton != null
                && !headerToolsButton.Descendants().Any(candidate => candidate.Name.LocalName == "TextBlock"),
            "top tools menu button should stay compact and icon-only; rarely used command names belong inside the popup");
        AssertTrue(shellXamlSource.Contains("x:Name=\"HeaderToolsPopup\"", StringComparison.Ordinal),
            "rarely used header commands should be grouped behind the top tools menu instead of occupying the default header");
        AssertTrue(shellXamlSource.Contains("PlacementTarget=\"{Binding ElementName=HeaderToolsMenuButton}\"", StringComparison.Ordinal),
            "top tools popup should open from the compact header tools button");
        AssertTrue(shellXamlSource.Contains("1 기준 라벨 선택 -> 2 라벨 초안 생성 -> 3 위치 확인 -> 4 라벨 저장", StringComparison.Ordinal),
            "template helper menu should show the draft-label review and save sequence");
        AssertTrue(shellXamlSource.Contains("현재 이미지 초안 생성", StringComparison.Ordinal), "template helper menu should distinguish current-image draft creation from AI candidates");
        AssertTrue(shellXamlSource.Contains("전체 이미지 자동 저장", StringComparison.Ordinal), "template helper menu should distinguish batch auto-save from current-image drafts");
        AssertNamedXamlBinding(shellXaml, xName, "TeachingModeButton", "IsEnabled", "ShellViewModel.IsLabelingModeButtonEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "TeachingModeButton", "Tag", "ShellViewModel.IsLabelingModeActive");
        AssertNamedXamlBinding(shellXaml, xName, "InferenceModeButton", "IsEnabled", "ShellViewModel.IsInferenceModeButtonEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "InferenceModeButton", "Tag", "ShellViewModel.IsInferenceModeActive");
        AssertNamedXamlBinding(shellXaml, xName, "ThemeToggleButton", "Command", "ShellViewModel.ToggleThemeCommand");
        AssertNamedXamlBinding(shellXaml, xName, "LoadSampleButton", "Command", "ShellViewModel.LoadSampleCommand");
        AssertNamedXamlBinding(shellXaml, xName, "AddSampleRoiButton", "Command", "ShellViewModel.AddSampleRoiCommand");
        AssertNamedXamlBinding(shellXaml, xName, "SaveAnnotationsButton", "Command", "ShellViewModel.SaveAnnotationsCommand");
        AssertNamedXamlBinding(shellXaml, xName, "TeachingModeButton", "Command", "ShellViewModel.LabelingModeCommand");
        AssertNamedXamlBinding(shellXaml, xName, "InferenceModeButton", "Command", "ShellViewModel.InferenceModeCommand");
        AssertNamedXamlBinding(shellXaml, xName, "DatasetHomeStageButton", "Command", "ShellViewModel.DatasetHomeCommand");
        AssertNamedXamlBinding(shellXaml, xName, "DatasetHomeStageButton", "Tag", "ShellViewModel.IsDatasetStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "LabelingWorkbenchStageButton", "Command", "ShellViewModel.LabelingWorkbenchCommand");
        AssertNamedXamlBinding(shellXaml, xName, "LabelingWorkbenchStageButton", "Tag", "ShellViewModel.IsLabelingStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "InferenceReviewStageButton", "Command", "ShellViewModel.InferenceReviewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "InferenceReviewStageButton", "Tag", "ShellViewModel.IsInferenceStageActive");
        string decodedShellXamlSource = System.Net.WebUtility.HtmlDecode(shellXamlSource);
        AssertTrue(decodedShellXamlSource.Contains("AI 후보 검토", StringComparison.Ordinal), "top workflow stage button should name stage 3 as AI candidate review");
        AssertTrue(decodedShellXamlSource.Contains("3 AI 후보", StringComparison.Ordinal), "top workflow stage button should use the compact AI-candidate label");
        AssertTrue(!decodedShellXamlSource.Contains("3 추론 검토", StringComparison.Ordinal), "top workflow stage button should not use the ambiguous inference-review label");
        AssertTrue(!decodedShellXamlSource.Contains("추론 검토", StringComparison.Ordinal), "workflow mode and stage buttons should not reintroduce the ambiguous inference-review label");
        AssertNamedXamlBinding(shellXaml, xName, "TrainingModelStageButton", "Command", "ShellViewModel.TrainingModelCenterCommand");
        AssertNamedXamlBinding(shellXaml, xName, "TrainingModelStageButton", "Tag", "ShellViewModel.IsTrainingModelStageActive");
        XElement shellTitleBar = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "TitleBar"
                && string.Equals((string)candidate.Attribute(xName), "ShellTitleBar", StringComparison.Ordinal));
        AssertTrue(shellTitleBar != null, "top title bar should have a stable named root");
        AssertEqual("28", (string)shellTitleBar.Attribute("Height"));
        XElement shellHeaderBar = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "ShellHeaderBar", StringComparison.Ordinal));
        AssertTrue(shellHeaderBar != null, "top command header should have a stable named root");
        AssertEqual("44", (string)shellHeaderBar.Attribute("Height"));
        XElement workflowContextHeader = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "WorkflowContextHeader", StringComparison.Ordinal));
        AssertTrue(workflowContextHeader != null, "top workflow context should have a stable named root");
        AssertEqual("82", (string)workflowContextHeader.Attribute("Height"));
        XElement workflowStageRail = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "WorkflowStageRail", StringComparison.Ordinal));
        AssertTrue(workflowStageRail != null, "top workflow stage rail should have a stable named root");
        AssertEqual("48", (string)workflowStageRail.Attribute("Height"));
        AssertTrue(workflowStageRail.Ancestors().Any(candidate => string.Equals((string)candidate.Attribute(xName), "WorkflowContextHeader", StringComparison.Ordinal)),
            "workflow stage rail should be grouped inside the compact workflow context header");
        foreach (string stageButtonName in new[]
        {
            "DatasetHomeStageButton",
            "LabelingWorkbenchStageButton",
            "InferenceReviewStageButton",
            "TrainingModelStageButton"
        })
        {
            XElement stageButton = shellXaml.Descendants()
                .FirstOrDefault(candidate => candidate.Name.LocalName == "Button"
                    && string.Equals((string)candidate.Attribute(xName), stageButtonName, StringComparison.Ordinal));
            AssertTrue(stageButton != null, $"top workflow stage button was not found: {stageButtonName}");
            AssertEqual(1, stageButton.Descendants().Count(candidate => candidate.Name.LocalName == "TextBlock"));
        }

        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSummaryProgressText", "Text", "ShellViewModel.WorkflowStageProgressText");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSummaryTitleText", "Text", "ShellViewModel.WorkflowStageTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSummaryDetailText", "Text", "ShellViewModel.WorkflowStageDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSummaryNextActionText", "Text", "ShellViewModel.WorkflowStageNextActionText");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSummaryPanel", "ToolTip", "ShellViewModel.WorkflowStageDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowViewTitleText", "Text", "ShellViewModel.RightWorkflowViewTitleText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "RightWorkflowViewDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowViewDetailText", "Text", "ShellViewModel.RightWorkflowViewDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "ObjectsReviewTab", "Visibility", "ShellViewModel.IsSavedLabelsViewVisible");
        AssertNamedXamlBinding(shellXaml, xName, "CandidatesReviewTab", "Visibility", "ShellViewModel.IsCandidateReviewViewVisible");
        AssertNamedXamlBinding(shellXaml, xName, "LearningReviewTab", "Visibility", "ShellViewModel.IsGuideToolsViewVisible");
        AssertNamedXamlBinding(shellXaml, xName, "ClassesReviewTab", "Visibility", "ShellViewModel.IsClassCatalogViewVisible");
        AssertNamedXamlBinding(shellXaml, xName, "YoloSettingsReviewTab", "Visibility", "ShellViewModel.IsYoloModelCenterViewVisible");
        AssertTrue(shellXamlSource.Contains("BooleanToVisibilityConverter", StringComparison.Ordinal), "right workflow views should use boolean ViewModel state for visibility");
        AssertTrue(shellXamlSource.Contains("WorkflowViewHostTabItemStyle", StringComparison.Ordinal), "right workflow view host should style stage subnavigation explicitly");
        AssertNamedXamlValue(shellXaml, xName, "ReviewTabControl", "TabStripPlacement", "Top");
        AssertTrue(shellXamlSource.Contains("WorkflowTabHeaderBorder", StringComparison.Ordinal), "right workflow tab headers should use the app dark template instead of the default white WPF tab chrome");
        AssertTrue(shellXamlSource.Contains("Background=\"{TemplateBinding Background}\"", StringComparison.Ordinal)
            && shellXamlSource.Contains("TextElement.Foreground=\"{TemplateBinding Foreground}\"", StringComparison.Ordinal),
            "right workflow tab headers should inherit dark-theme brushes for both tab background and header text");
        AssertTrue(shellXamlSource.Contains("Property=\"IsSelected\" Value=\"True\"", StringComparison.Ordinal)
            && shellXamlSource.Contains("Property=\"BorderBrush\" Value=\"{DynamicResource AccentBrush}\"", StringComparison.Ordinal),
            "right workflow selected tab should be visible as an app-themed active state, not a white selected tab");
        AssertTrue(shellXamlSource.Contains("ShellViewModel.IsRightWorkflowSubNavigationVisible", StringComparison.Ordinal), "right workflow subnavigation should collapse when the active stage has a single view");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowColumn", "Width", "ShellViewModel.RightWorkflowPaneGridLength");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowColumn", "MinWidth", "ShellViewModel.RightWorkflowPaneMinWidth");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowColumn", "MaxWidth", "ShellViewModel.RightWorkflowPaneMaxWidth");
        AssertNamedXamlElement(shellXaml, xName, "ColumnDefinition", "CanvasWorkspaceColumn");
        AssertNamedXamlBinding(shellXaml, xName, "CanvasWorkspaceColumn", "Width", "ShellViewModel.CanvasWorkspacePaneGridLength");
        AssertNamedXamlBinding(shellXaml, xName, "CanvasWorkspaceColumn", "MinWidth", "ShellViewModel.CanvasWorkspacePaneMinWidth");
        AssertNamedXamlElement(shellXaml, xName, "ColumnDefinition", "LeftWorkspaceSplitterColumn");
        AssertNamedXamlBinding(shellXaml, xName, "LeftWorkspaceSplitterColumn", "Width", "ShellViewModel.WorkspaceSplitterPaneGridLength");
        AssertNamedXamlElement(shellXaml, xName, "ColumnDefinition", "RightWorkspaceSplitterColumn");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkspaceSplitterColumn", "Width", "ShellViewModel.WorkspaceSplitterPaneGridLength");
        AssertNamedXamlElement(shellXaml, xName, "GridSplitter", "LeftWorkspaceSplitter");
        AssertNamedXamlElement(shellXaml, xName, "GridSplitter", "RightWorkspaceSplitter");
        AssertNamedXamlValue(shellXaml, xName, "LeftWorkspaceSplitter", "ResizeBehavior", "PreviousAndNext");
        AssertNamedXamlValue(shellXaml, xName, "RightWorkspaceSplitter", "ResizeBehavior", "PreviousAndNext");
        AssertNamedXamlValue(shellXaml, xName, "LeftWorkspaceSplitter", "ShowsPreview", "True");
        AssertNamedXamlValue(shellXaml, xName, "RightWorkspaceSplitter", "ShowsPreview", "True");
        AssertNamedXamlElement(shellXaml, xName, "ColumnDefinition", "ImageQueueColumn");
        AssertNamedXamlBinding(shellXaml, xName, "ImageQueueColumn", "Width", "ShellViewModel.ImageQueuePaneGridLength");
        AssertNamedXamlBinding(shellXaml, xName, "ImageQueueColumn", "MinWidth", "ShellViewModel.ImageQueuePaneMinWidth");
        AssertNamedXamlBinding(shellXaml, xName, "CanvasPanelControl", "Visibility", "ShellViewModel.IsCanvasWorkspaceVisible");
        AssertNamedXamlBinding(shellXaml, xName, "ImageQueuePanelControl", "Visibility", "ShellViewModel.IsImageQueueWorkspaceVisible");
        AssertNamedXamlBinding(shellXaml, xName, "LeftWorkspaceSplitter", "Visibility", "ShellViewModel.IsWorkspaceSplitterVisible");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkspaceSplitter", "Visibility", "ShellViewModel.IsWorkspaceSplitterVisible");
        AssertTrue(shellXamlSource.Contains("LeftWorkspaceSplitter_DragCompleted", StringComparison.Ordinal), "left workspace resize should preserve the dock width binding after dragging");
        AssertTrue(shellXamlSource.Contains("RightWorkspaceSplitter_DragCompleted", StringComparison.Ordinal), "image queue resize should persist after dragging");
        AssertNamedXamlElement(shellXaml, xName, "Button", "ResetWorkspaceLayoutButton");
        AssertNamedXamlBinding(shellXaml, xName, "ResetWorkspaceLayoutButton", "Command", "ShellViewModel.ResetWorkspaceLayoutCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowExpandedContent", "Visibility", "ShellViewModel.IsRightWorkflowDockExpanded");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowCollapsedRail", "Visibility", "ShellViewModel.IsRightWorkflowDockRailVisible");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowDockToggleButton", "Command", "ShellViewModel.ToggleRightWorkflowDockCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowDockToggleButton", "Visibility", "ShellViewModel.IsRightWorkflowDockToggleVisible");
        AssertNamedXamlElement(shellXaml, xName, "Border", "WorkflowStageSubNavigationRail");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSubNavigationRail", "Visibility", "ShellViewModel.IsRightWorkflowShortcutBarVisible");
        XElement workflowStageSubNavigationRailXaml = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "WorkflowStageSubNavigationRail", StringComparison.Ordinal));
        AssertEqual("34", (string)workflowStageSubNavigationRailXaml.Attribute("Height"));
        AssertTrue(workflowStageSubNavigationRailXaml.Ancestors().Any(candidate => string.Equals((string)candidate.Attribute(xName), "RightWorkflowExpandedContent", StringComparison.Ordinal)),
            "workflow task tabs should belong to the expanded left workflow panel");
        AssertTrue(!workflowStageSubNavigationRailXaml.Ancestors().Any(candidate => string.Equals((string)candidate.Attribute(xName), "WorkflowContextHeader", StringComparison.Ordinal)),
            "workflow task tabs should not remain detached in the global context header");
        AssertNamedXamlValue(shellXaml, xName, "ReviewTabControl", "Grid.Row", "2");
        AssertNamedXamlElement(shellXaml, xName, "Border", "RightWorkflowRailContextBadge");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "RightWorkflowRailContextTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowRailCurrentViewText", "Text", "ShellViewModel.RightWorkflowRailCurrentViewText");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowRailOpenButton", "Command", "ShellViewModel.ToggleRightWorkflowDockCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowRailSavedLabelsButton", "Command", "ShellViewModel.ShowSavedLabelsViewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowRailGuideToolsButton", "Command", "ShellViewModel.ShowLabelingGuideViewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowRailClassCatalogButton", "Command", "ShellViewModel.ShowClassCatalogViewCommand");
        AssertTrue(!shellXamlSource.Contains("RightWorkflowRailCandidateReviewButton", StringComparison.Ordinal), "right workflow collapsed rail should not duplicate top workflow inference-stage navigation");
        AssertTrue(!shellXamlSource.Contains("RightWorkflowRailModelCenterButton", StringComparison.Ordinal), "right workflow collapsed rail should not duplicate top workflow model-stage navigation");
        foreach (string railTextName in new[]
        {
            "RightWorkflowRailOpenText",
            "RightWorkflowRailSavedLabelsText",
            "RightWorkflowRailGuideToolsText",
            "RightWorkflowRailClassCatalogText"
        })
        {
            AssertNamedXamlElement(shellXaml, xName, "TextBlock", railTextName);
        }

        AssertTrue(shellXamlSource.Contains("AutomationProperties.Name=\"&#xD604;&#xC7AC; &#xC791;&#xC5C5; &#xBCF4;&#xAE30;\"", StringComparison.Ordinal), "labeling guide shortcut should read as current work instead of a broad tool panel");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.Name=\"&#xD604;&#xC7AC; &#xC791;&#xC5C5; &#xD328;&#xB110; &#xC5F4;&#xAE30;\"", StringComparison.Ordinal), "collapsed guide rail should open the current-work panel");
        AssertTrue(shellXamlSource.Contains("<iconPacks:PackIconMaterial Kind=\"ClipboardTextOutline\" Width=\"12\" Height=\"12\" Margin=\"0,0,4,0\" />", StringComparison.Ordinal), "labeling guide shortcut should use a current-task icon instead of a drawing-shape icon");
        int guideRailTextIndex = shellXamlSource.IndexOf("x:Name=\"RightWorkflowRailGuideToolsText\"", StringComparison.Ordinal);
        AssertTrue(guideRailTextIndex >= 0
            && shellXamlSource.IndexOf("Text=\"&#xC791;&#xC5C5;\"", guideRailTextIndex, StringComparison.Ordinal) > guideRailTextIndex,
            "collapsed guide rail label should display the current-work wording");
        int guideRailButtonIndex = shellXamlSource.IndexOf("x:Name=\"RightWorkflowRailGuideToolsButton\"", StringComparison.Ordinal);
        AssertTrue(guideRailButtonIndex >= 0
            && shellXamlSource.IndexOf("Kind=\"ClipboardTextOutline\"", guideRailButtonIndex, StringComparison.Ordinal) > guideRailButtonIndex,
            "collapsed guide rail should use the current-task icon");

        AssertTrue(shellXamlSource.Contains("<Setter Property=\"Width\" Value=\"60\" />", StringComparison.Ordinal), "right workflow collapsed rail buttons should reserve enough width for icon and short Korean labels");
        AssertTrue(shellXamlSource.Contains("<Setter Property=\"Height\" Value=\"42\" />", StringComparison.Ordinal), "right workflow collapsed rail buttons should reserve enough height for icon and label");
        AssertNamedXamlElement(shellXaml, xName, "UniformGrid", "RightWorkflowShortcutBar");
        AssertNamedXamlValue(shellXaml, xName, "RightWorkflowShortcutBar", "Rows", "1");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowShortcutBar", "Visibility", "ShellViewModel.IsRightWorkflowShortcutBarVisible");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowDatasetHomeButton", "Command", "ShellViewModel.DatasetHomeCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowDatasetHomeButton", "Visibility", "ShellViewModel.IsDatasetStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowSavedLabelsButton", "Command", "ShellViewModel.ShowSavedLabelsViewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowGuideToolsButton", "Command", "ShellViewModel.ShowLabelingGuideViewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowClassCatalogButton", "Command", "ShellViewModel.ShowClassCatalogViewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowInferenceCandidatesButton", "Command", "ShellViewModel.InferenceReviewCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowInferenceInspectButton", "Command", "ShellViewModel.DetectCurrentImageCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingModelButton", "Command", "ShellViewModel.TrainingModelCenterCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingReviewCandidateButton", "Command", "ShellViewModel.ReviewCandidateModelCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingInspectButton", "Command", "ShellViewModel.DetectCurrentImageCommand");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowDatasetHomeButton", "Tag", "ShellViewModel.IsLabelingGuideShortcutActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowSavedLabelsButton", "Tag", "ShellViewModel.IsSavedLabelsShortcutActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowGuideToolsButton", "Tag", "ShellViewModel.IsLabelingGuideShortcutActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowClassCatalogButton", "Tag", "ShellViewModel.IsClassCatalogShortcutActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowSavedLabelsButton", "Visibility", "ShellViewModel.IsLabelingStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowGuideToolsButton", "Visibility", "ShellViewModel.IsLabelingStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowClassCatalogButton", "Visibility", "ShellViewModel.IsClassCatalogViewVisible");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowInferenceCandidatesButton", "Visibility", "ShellViewModel.IsInferenceStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowInferenceInspectButton", "Visibility", "ShellViewModel.IsInferenceStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingModelButton", "Visibility", "ShellViewModel.IsTrainingModelStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingReviewCandidateButton", "Visibility", "ShellViewModel.IsTrainingModelStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingInspectButton", "Visibility", "ShellViewModel.IsTrainingModelStageActive");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowInferenceInspectButton", "IsEnabled", "ShellViewModel.IsCurrentImageDetectionEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingReviewCandidateButton", "IsEnabled", "ShellViewModel.IsModelCenterReviewCandidateEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "RightWorkflowTrainingInspectButton", "IsEnabled", "ShellViewModel.IsModelCenterInspectCurrentImageEnabled");
        AssertNamedXamlValue(shellXaml, xName, "RightWorkflowInferenceInspectButton", "Style", "{StaticResource RightWorkflowPanelActionButtonStyle}");
        AssertNamedXamlValue(shellXaml, xName, "RightWorkflowTrainingReviewCandidateButton", "Style", "{StaticResource RightWorkflowPanelActionButtonStyle}");
        AssertNamedXamlValue(shellXaml, xName, "RightWorkflowTrainingInspectButton", "Style", "{StaticResource RightWorkflowPanelActionButtonStyle}");
        AssertTrue(shellXamlSource.Contains("<Setter Property=\"BorderThickness\" Value=\"0,0,0,2\" />", StringComparison.Ordinal),
            "workflow panel tabs should use a selected underline instead of a filled global toolbar button");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleTrainingStatusText", "Text", "ShellViewModel.ModelCenterTrainingStatusText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleTrainingDetailText", "Text", "ShellViewModel.ModelCenterTrainingDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleCurrentModelTitleText", "Text", "ShellViewModel.ModelCenterCurrentModelTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleCurrentModelText", "Text", "ShellViewModel.ModelCenterCurrentModelDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleCandidateModelTitleText", "Text", "ShellViewModel.ModelCenterCandidateModelTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleCandidateModelText", "Text", "ShellViewModel.ModelCenterCandidateModelDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleAdoptionTitleText", "Text", "ShellViewModel.ModelCenterAdoptionTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleAdoptionText", "Text", "ShellViewModel.ModelCenterAdoptionDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleNextActionTitleText", "Text", "ShellViewModel.ModelCenterNextActionTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleNextActionText", "Text", "ShellViewModel.ModelCenterNextActionDetailText");
        AssertNamedXamlValue(shellXaml, xName, "YoloModelLifecycleDetailPanel", "Visibility", "Collapsed");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelAdoptionDecisionTitleText", "Text", "ShellViewModel.ModelCenterDecisionTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelAdoptionDecisionSummaryText", "Text", "ShellViewModel.ModelCenterDecisionSummaryText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelAdoptionDecisionEvidenceText", "Text", "ShellViewModel.ModelCenterDecisionEvidenceText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelAdoptionDecisionActionText", "Text", "ShellViewModel.ModelCenterDecisionActionText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationPanel", "Visibility", "ShellViewModel.IsModelCenterAnomalyEvaluationVisible");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationTitleText", "Text", "ShellViewModel.ModelCenterAnomalyEvaluationTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationRecommendationText", "Text", "ShellViewModel.ModelCenterAnomalyEvaluationRecommendationText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationMetricsText", "Text", "ShellViewModel.ModelCenterAnomalyEvaluationMetricsText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationDetailText", "Text", "ShellViewModel.ModelCenterAnomalyEvaluationDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationActionText", "Text", "ShellViewModel.ModelCenterAnomalyEvaluationActionText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationRunButton", "Command", "ShellViewModel.RunAnomalyEvaluationCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationRunButton", "IsEnabled", "ShellViewModel.IsModelCenterAnomalyEvaluationPickerEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationRunButton", "Visibility", "ShellViewModel.IsModelCenterAnomalyEvaluationPickerVisible");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationLoadSummaryButton", "Command", "ShellViewModel.LoadAnomalyEvaluationSummaryCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationLoadSummaryButton", "IsEnabled", "ShellViewModel.IsModelCenterAnomalyEvaluationPickerEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "YoloAnomalyEvaluationLoadSummaryButton", "Visibility", "ShellViewModel.IsModelCenterAnomalyEvaluationPickerVisible");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryTitleText", "Text", "ShellViewModel.ModelRegistryTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistrySummaryPrimaryText", "Text", "ShellViewModel.ModelRegistrySummaryPrimaryText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistrySummarySecondaryText", "Text", "ShellViewModel.ModelRegistrySummarySecondaryText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryProfileText", "Text", "ShellViewModel.ModelRegistryProfileText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryTrainingRunText", "Text", "ShellViewModel.ModelRegistryTrainingRunText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryCandidateModelText", "Text", "ShellViewModel.ModelRegistryCandidateModelText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryInspectionModelText", "Text", "ShellViewModel.ModelRegistryInspectionModelText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryActionText", "Text", "ShellViewModel.ModelRegistryActionText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryHistoryPanel", "Visibility", "ShellViewModel.IsModelRegistryHistoryVisible");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryHistoryHeaderText", "Text", "ShellViewModel.ModelRegistryHistoryHeaderText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryHistorySummaryText", "Text", "ShellViewModel.ModelRegistryHistorySummaryText");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryHistoryItems", "ItemsSource", "ShellViewModel.ModelRegistryHistoryItems");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistryHistoryItems", "SelectedItem", "ShellViewModel.SelectedModelRegistryHistoryItem");
        AssertNamedXamlBinding(shellXaml, xName, "ModelRegistrySelectedHistoryPanel", "Visibility", "ShellViewModel.IsSelectedModelHistoryVisible");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryTitleText", "Text", "ShellViewModel.SelectedModelHistoryTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryDecisionText", "Text", "ShellViewModel.SelectedModelHistoryDecisionText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryDetailText", "Text", "ShellViewModel.SelectedModelHistoryDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryMetricText", "Text", "ShellViewModel.SelectedModelHistoryMetricText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryComparisonTitleText", "Text", "ShellViewModel.SelectedModelHistoryComparisonTitleText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "SelectedModelHistoryCurrentRoleText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryCurrentModelText", "Text", "ShellViewModel.SelectedModelHistoryCurrentModelText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "SelectedModelHistorySelectedRoleText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistorySelectedModelText", "Text", "ShellViewModel.SelectedModelHistorySelectedModelText");
        AssertNamedXamlBinding(shellXaml, xName, "SelectedModelHistoryComparisonMetricText", "Text", "ShellViewModel.SelectedModelHistoryComparisonMetricText");
        AssertNamedXamlBinding(shellXaml, xName, "PromoteSelectedModelHistoryButton", "Command", "ShellViewModel.PromoteSelectedModelHistoryCommand");
        AssertNamedXamlBinding(shellXaml, xName, "PromoteSelectedModelHistoryButton", "IsEnabled", "ShellViewModel.IsSelectedModelHistoryActionEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "PromoteSelectedModelHistoryButton", "ToolTip", "ShellViewModel.SelectedModelHistoryActionToolTip");
        AssertNamedXamlBinding(shellXaml, xName, "PromoteSelectedModelHistoryButtonText", "Text", "ShellViewModel.SelectedModelHistoryActionText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelRecoveryTitleText", "Text", "ShellViewModel.ModelCenterRecoveryTitleText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelRecoveryDetailText", "Text", "ShellViewModel.ModelCenterRecoveryDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelRecoveryActionText", "Text", "ShellViewModel.ModelCenterRecoveryActionText");
        AssertTrue(shellXamlSource.Contains("ShellViewModel.IsModelCenterRecoveryVisible", StringComparison.Ordinal),
            "YOLO model center should show failure/recovery guidance from the shell ViewModel, not only in logs");
        AssertNamedXamlBinding(shellXaml, xName, "YoloModelLifecycleProgressBar", "Value", "TrainingSettingsViewModel.TrainingProgressValue");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleRefreshButton", "Command", "TrainingSettingsViewModel.RefreshReadinessCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleStartTrainingButton", "Command", "TrainingSettingsViewModel.StartTrainingCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleStopTrainingButton", "Command", "TrainingSettingsViewModel.StopTrainingCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleReviewCandidateModelButton", "Command", "ShellViewModel.ReviewCandidateModelCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleReviewCandidateModelButton", "IsEnabled", "ShellViewModel.IsModelCenterReviewCandidateEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleReviewCandidateModelButton", "ToolTip", "ShellViewModel.ModelCenterReviewCandidateButtonToolTip");
        AssertNamedXamlValue(shellXaml, xName, "YoloLifecycleReviewCandidateModelButton", "Visibility", "Collapsed");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleReviewCandidateModelButtonText", "Text", "ShellViewModel.ModelCenterReviewCandidateButtonText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleSaveModelSettingsButton", "Command", "YoloModelSettingsViewModel.SaveSettingsCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleSaveModelSettingsButton", "IsEnabled", "ShellViewModel.IsModelCenterConfirmModelEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleSaveModelSettingsButton", "ToolTip", "ShellViewModel.ModelCenterConfirmModelButtonToolTip");
        AssertNamedXamlValue(shellXaml, xName, "YoloLifecycleSaveModelSettingsButton", "Visibility", "Collapsed");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleSaveModelSettingsButtonText", "Text", "ShellViewModel.ModelCenterConfirmModelButtonText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleInspectCurrentImageButton", "Command", "ShellViewModel.DetectCurrentImageCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleInspectCurrentImageButton", "IsEnabled", "ShellViewModel.IsModelCenterInspectCurrentImageEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleInspectCurrentImageButton", "ToolTip", "ShellViewModel.ModelCenterInspectCurrentImageButtonToolTip");
        AssertNamedXamlValue(shellXaml, xName, "YoloLifecycleInspectCurrentImageButton", "Visibility", "Collapsed");
        AssertNamedXamlBinding(shellXaml, xName, "YoloLifecycleInspectCurrentImageButtonText", "Text", "ShellViewModel.ModelCenterInspectCurrentImageButtonText");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"WorkflowStageModelActionPanel\"", StringComparison.Ordinal),
            "workflow stage summary should expose the post-training model actions as a stable automation target");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageModelActionPanel", "Visibility", "ShellViewModel.IsWorkflowStageModelActionPanelVisible");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageReviewCandidateModelButton", "Command", "ShellViewModel.ReviewCandidateModelCommand");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageReviewCandidateModelButton", "IsEnabled", "ShellViewModel.IsModelCenterReviewCandidateEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageReviewCandidateModelButton", "ToolTip", "ShellViewModel.ModelCenterReviewCandidateButtonToolTip");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageReviewCandidateModelButtonText", "Text", "ShellViewModel.ModelCenterReviewCandidateButtonText");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSaveModelSettingsButton", "Command", "YoloModelSettingsViewModel.SaveSettingsCommand");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSaveModelSettingsButton", "IsEnabled", "ShellViewModel.IsModelCenterConfirmModelEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSaveModelSettingsButton", "ToolTip", "ShellViewModel.ModelCenterConfirmModelButtonToolTip");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageSaveModelSettingsButtonText", "Text", "ShellViewModel.ModelCenterConfirmModelButtonText");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageInspectCurrentImageButton", "Command", "ShellViewModel.DetectCurrentImageCommand");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageInspectCurrentImageButton", "IsEnabled", "ShellViewModel.IsModelCenterInspectCurrentImageEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageInspectCurrentImageButton", "ToolTip", "ShellViewModel.ModelCenterInspectCurrentImageButtonToolTip");
        AssertNamedXamlBinding(shellXaml, xName, "WorkflowStageInspectCurrentImageButtonText", "Text", "ShellViewModel.ModelCenterInspectCurrentImageButtonText");
        AssertTrue(shellXamlSource.Contains("x:Name=\"RightWorkflowViewTitleText\"", StringComparison.Ordinal),
            "right review panel title should follow the selected right-workflow view instead of only the top workflow stage");
        AssertTrue(shellXamlSource.Contains("x:Name=\"RightWorkflowViewDetailText\"", StringComparison.Ordinal),
            "right review panel header should explain the active panel role, not only show a short tab label");
        AssertTrue(shellXamlSource.Contains("Header=\"&#xC800;&#xC7A5; &#xB77C;&#xBCA8;\"", StringComparison.Ordinal),
            "object review tab should be titled as saved labels");
        AssertTrue(shellXamlSource.Contains("Header=\"AI &#xD6C4;&#xBCF4;\"", StringComparison.Ordinal),
            "candidate review tab should be titled as AI candidates");
        AssertNamedXamlBinding(shellXaml, xName, "CheckYoloButton", "Command", "ShellViewModel.CheckYoloCommand");
        AssertNamedXamlBinding(shellXaml, xName, "DetectButton", "Command", "ShellViewModel.DetectCurrentImageCommand");
        XElement currentDatasetContextBar = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "CurrentDatasetContextBar", StringComparison.Ordinal));
        AssertTrue(currentDatasetContextBar != null, "current dataset context bar should have a stable named root");
        AssertEqual("34", (string)currentDatasetContextBar.Attribute("Height"));
        AssertEqual("2", (string)currentDatasetContextBar.Attribute("Grid.ColumnSpan"));
        AssertTrue(currentDatasetContextBar.Ancestors().Any(candidate => string.Equals((string)candidate.Attribute(xName), "WorkflowContextHeader", StringComparison.Ordinal)),
            "current dataset context should be grouped inside the compact workflow context header");
        AssertNamedXamlBinding(shellXaml, xName, "CurrentDatasetNameText", "Text", "ShellViewModel.CurrentDatasetName");
        AssertNamedXamlBinding(shellXaml, xName, "CurrentDatasetPurposeText", "Text", "ShellViewModel.CurrentDatasetPurposeText");
        AssertNamedXamlBinding(shellXaml, xName, "CurrentDatasetStoragePathText", "Text", "ShellViewModel.CurrentDatasetStoragePathText");
        AssertNamedXamlBinding(shellXaml, xName, "CurrentDatasetImageRootText", "Text", "ShellViewModel.CurrentDatasetImageRootText");
        XElement datasetStoragePathCard = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "DatasetStoragePathCard", StringComparison.Ordinal));
        XElement datasetImageRootCard = shellXaml.Descendants()
            .FirstOrDefault(candidate => candidate.Name.LocalName == "Border"
                && string.Equals((string)candidate.Attribute(xName), "DatasetImageRootCard", StringComparison.Ordinal));
        AssertTrue(datasetStoragePathCard != null, "dataset storage path detail should stay bound for tooltips/details");
        AssertTrue(datasetImageRootCard != null, "dataset image-root detail should stay bound for tooltips/details");
        AssertEqual("Collapsed", (string)datasetStoragePathCard.Attribute("Visibility"));
        AssertEqual("Collapsed", (string)datasetImageRootCard.Attribute("Visibility"));
        AssertNamedXamlElement(shellXaml, xName, "Border", "DatasetSourceCard");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "CurrentDatasetSourceText");
        AssertNamedXamlBinding(shellXaml, xName, "CurrentDatasetSourceText", "Text", "ShellViewModel.CurrentDatasetSourceText");
        AssertNamedXamlBinding(shellXaml, xName, "ChangeDatasetButton", "Command", "ShellViewModel.ChangeDatasetCommand");
        AssertNamedXamlBinding(shellXaml, xName, "OpenDatasetFolderButton", "Command", "ShellViewModel.OpenDatasetFolderCommand");
        AssertNamedXamlBinding(shellXaml, xName, "OpenDatasetFolderButton", "IsEnabled", "ShellViewModel.IsOpenDatasetFolderEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "ChangeImageFolderButton", "Command", "ShellViewModel.ChangeImageFolderCommand");
        AssertNamedXamlAttachedBinding(shellXaml, xName, "ChangeDatasetButton", "MouseClickInputCommand", "ShellViewModel.ChangeDatasetCommand");
        AssertNamedXamlAttachedBinding(shellXaml, xName, "OpenDatasetFolderButton", "MouseClickInputCommand", "ShellViewModel.OpenDatasetFolderCommand");
        AssertNamedXamlAttachedBinding(shellXaml, xName, "ChangeImageFolderButton", "MouseClickInputCommand", "ShellViewModel.ChangeImageFolderCommand");
        AssertNamedXamlAttachedBinding(shellXaml, xName, "ShellWindow", "LoadedCommand", "ShellViewModel.LoadedCommand");
        AssertNamedXamlAttachedBinding(shellXaml, xName, "ShellWindow", "ClosedCommand", "ShellViewModel.ClosedCommand");
        AssertNamedXamlAttachedBinding(shellXaml, xName, "ShellWindow", "PreviewKeyInputCommand", "ShellViewModel.PreviewKeyDownCommand");
        AssertNamedXamlBinding(queueXaml, xName, "OpenSelectedQueueImageButton", "IsEnabled", "IsOpenSelectedImageEnabled");
        AssertNamedXamlBinding(queueXaml, xName, "OpenSelectedQueueImageButton", "Command", "OpenSelectedQueueImageCommand");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageFolderPathText", "Text", "CurrentImageFolderDisplayText");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageFolderPathText", "ToolTip", "CurrentImageFolderPath");
        AssertNamedXamlBinding(queueXaml, xName, "OpenCurrentImageFolderButton", "IsEnabled", "IsOpenCurrentImageFolderEnabled");
        AssertNamedXamlBinding(queueXaml, xName, "OpenCurrentImageFolderButton", "Command", "OpenCurrentImageFolderCommand");
        AssertNamedXamlElement(queueXaml, xName, "Border", "CurrentImageTaskCard");
        AssertNamedXamlElement(queueXaml, xName, "TextBlock", "CurrentImageTaskTitleText");
        AssertNamedXamlElement(queueXaml, xName, "TextBlock", "CurrentImageTaskDetailText");
        AssertNamedXamlElement(queueXaml, xName, "Border", "CurrentImageTaskBadge");
        AssertNamedXamlElement(queueXaml, xName, "TextBlock", "CurrentImageTaskBadgeText");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageTaskCard", "Tag", "CurrentImageTaskKey");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageTaskCard", "ToolTip", "CurrentImageTaskToolTip");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageTaskTitleText", "Text", "CurrentImageTaskTitleText");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageTaskDetailText", "Text", "CurrentImageTaskDetailText");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageTaskBadge", "Tag", "CurrentImageTaskKey");
        AssertNamedXamlBinding(queueXaml, xName, "CurrentImageTaskBadgeText", "Text", "CurrentImageTaskBadgeText");
        AssertNamedXamlBinding(queueXaml, xName, "DetectSelectedQueueButton", "Command", "DetectSelectedQueueCommand");
        AssertNamedXamlBinding(queueXaml, xName, "BatchDetectQueueButton", "Command", "BatchDetectQueueCommand");
        AssertNamedXamlBinding(queueXaml, xName, "RetryFailedQueueButton", "Command", "RetryFailedQueueCommand");
        AssertNamedXamlBinding(queueXaml, xName, "StopBatchQueueButton", "Command", "StopBatchQueueCommand");
        AssertNamedXamlAttachedBinding(queueXaml, xName, "ImageQueueFilterBox", "SelectedItemChangedCommand", "FilterSelectionChangedCommand");
        AssertNamedXamlAttachedBinding(queueXaml, xName, "ImageQueueSearchBox", "TextInputCommand", "SearchTextChangedCommand");
        AssertNamedXamlBinding(queueXaml, xName, "ImageQueueGrid", "SelectedItem", "SelectedQueueItem");
        AssertNamedXamlAttachedBinding(queueXaml, xName, "ImageQueueGrid", "SelectedItemChangedCommand", "QueueSelectionChangedCommand");
        AssertNamedXamlAttachedBinding(queueXaml, xName, "ImageQueueGrid", "MouseDoubleClickInputCommand", "QueueMouseDoubleClickCommand");
        AssertNamedXamlBinding(queueXaml, xName, "DetectSelectedQueueButton", "IsEnabled", "IsDetectSelectedEnabled");
        AssertNamedXamlBinding(queueXaml, xName, "BatchDetectQueueButton", "IsEnabled", "IsBatchDetectEnabled");
        AssertNamedXamlBinding(queueXaml, xName, "RetryFailedQueueButton", "IsEnabled", "IsRetryFailedEnabled");
        AssertNamedXamlBinding(queueXaml, xName, "StopBatchQueueButton", "IsEnabled", "IsStopBatchEnabled");
        AssertTrue(!imageQueueViewModelSource.Contains("SelectionChangedEventArgs", StringComparison.Ordinal), "image queue ViewModel should not depend on WPF selection event args");
        AssertTrue(!imageQueueViewModelSource.Contains("TextChangedEventArgs", StringComparison.Ordinal), "image queue ViewModel should not depend on WPF text event args");
        AssertTrue(!imageQueueViewModelSource.Contains("MouseButtonEventArgs", StringComparison.Ordinal), "image queue ViewModel should not depend on WPF mouse event args");
        AssertTrue(imageQueueViewModelSource.Contains("RefreshCurrentImageTaskSummary", StringComparison.Ordinal), "image queue ViewModel should own current-image task summary wording");
        AssertTrue(imageQueueViewModelSource.Contains("OnSelectedQueueItemPropertyChanged", StringComparison.Ordinal), "image queue current-image task summary should follow selected row status updates");
        AssertTrue(workflowStagePresentationSource.Contains("후보가 없으면 4 학습/모델, 있으면 확정 또는 숨김", StringComparison.Ordinal), "workflow-stage next action should distinguish the empty route from candidate confirm/hide");
        AssertTrue(!workflowStagePresentationSource.Contains("AI \uD6C4\uBCF4 \uC800\uC7A5/\uC228\uAE40", StringComparison.Ordinal), "workflow-stage next action should not imply AI candidates are saved separately before confirmation");
        AssertTrue(inputCommandBehaviorSource.Contains("TextInputCommandProperty", StringComparison.Ordinal), "input behaviors should expose text-value command routing");
        AssertTrue(inputCommandBehaviorSource.Contains("MouseDoubleClickInputCommandProperty", StringComparison.Ordinal), "input behaviors should expose parameterless double-click command routing");
        AssertTrue(inputCommandBehaviorSource.Contains("MouseClickInputCommandProperty", StringComparison.Ordinal), "input behaviors should expose parameterless mouse-click command routing");
        AssertTrue(!shellViewModelSource.Contains("KeyEventArgs", StringComparison.Ordinal), "shell ViewModel should not depend on WPF key event args");
        AssertTrue(shellViewModelSource.Contains("KeyInputCommandArgs", StringComparison.Ordinal), "shell ViewModel should use the shared key input DTO command contract");
        AssertTrue(shellViewModelSource.Contains("SetDatasetContext", StringComparison.Ordinal), "shell ViewModel should expose the current dataset identity for the main header");
        AssertTrue(shellViewModelSource.Contains("WpfDatasetContextPresentationService", StringComparison.Ordinal), "shell ViewModel should delegate dataset context wording to a presentation service");
        AssertTrue(shellViewModelSource.Contains("CurrentDatasetSourceText", StringComparison.Ordinal), "shell ViewModel should expose class/label source context for the dataset header");
        AssertTrue(datasetContextPresentationSource.Contains("BuildDatasetName", StringComparison.Ordinal), "dataset context presentation service should own dataset-name fallback");
        AssertTrue(datasetContextPresentationSource.Contains("FormatPurposeName", StringComparison.Ordinal), "dataset context presentation service should own dataset-purpose display wording");
        AssertTrue(!shellSource.Contains("FormatShellDatasetPurposeName", StringComparison.Ordinal), "shell code-behind should not own dataset-purpose display wording");
        AssertTrue(shellSource.Contains("WpfDatasetContextPresentationService.BuildDatasetName", StringComparison.Ordinal), "shell dataset context refresh should delegate dataset-name fallback to the context presentation service");
        AssertTrue(shellSource.Contains("WpfDatasetContextPresentationService.FormatPurposeName", StringComparison.Ordinal), "shell dataset context refresh should delegate dataset-purpose display wording to the context presentation service");
        AssertTrue(shellViewModelSource.Contains("ChangeDatasetCommand", StringComparison.Ordinal), "shell ViewModel should expose a main-header dataset change command");
        AssertTrue(shellViewModelSource.Contains("DatasetHomeCommand", StringComparison.Ordinal), "shell ViewModel should expose a dataset-home workflow stage command");
        AssertTrue(shellViewModelSource.Contains("LabelingWorkbenchCommand", StringComparison.Ordinal), "shell ViewModel should expose a labeling-workbench workflow stage command");
        AssertTrue(shellViewModelSource.Contains("InferenceReviewCommand", StringComparison.Ordinal), "shell ViewModel should expose an inference-review workflow stage command");
        AssertTrue(shellViewModelSource.Contains("TrainingModelCenterCommand", StringComparison.Ordinal), "shell ViewModel should expose a training/model workflow stage command");
        AssertTrue(shellViewModelSource.Contains("WorkflowStageNextActionText", StringComparison.Ordinal), "shell ViewModel should expose workflow-stage next-action text for the top rail");
        AssertTrue(shellViewModelSource.Contains("WpfWorkflowStagePresentationService", StringComparison.Ordinal), "shell ViewModel should delegate workflow-stage wording to a presentation service");
        AssertTrue(shellViewModelSource.Contains("SetModelCenterTrainingState", StringComparison.Ordinal), "shell ViewModel should expose model-center training state updates");
        AssertTrue(shellViewModelSource.Contains("SetModelCenterModelState", StringComparison.Ordinal), "shell ViewModel should expose current/candidate model summary updates");
        AssertTrue(shellViewModelSource.Contains("ModelCenterCurrentModelDetailText", StringComparison.Ordinal), "shell ViewModel should expose a separate current-model detail for the model center");
        AssertTrue(shellViewModelSource.Contains("StripModelCenterPrefix", StringComparison.Ordinal), "shell ViewModel should keep model-center display labels separate from model values");
        AssertTrue(shellViewModelSource.Contains("ModelCenterConfirmModelButtonText", StringComparison.Ordinal), "shell ViewModel should expose model-center confirm button text");
        AssertTrue(shellViewModelSource.Contains("IsModelCenterConfirmModelEnabled", StringComparison.Ordinal), "shell ViewModel should own model-center confirm button enablement");
        AssertTrue(shellViewModelSource.Contains("ModelCenterInspectCurrentImageButtonText", StringComparison.Ordinal), "shell ViewModel should expose model-center current-inspection button text");
        AssertTrue(shellViewModelSource.Contains("IsModelCenterInspectCurrentImageEnabled", StringComparison.Ordinal), "shell ViewModel should own model-center current-inspection enablement");
        AssertTrue(shellViewModelSource.Contains("ModelCenterDecisionSummaryText", StringComparison.Ordinal), "shell ViewModel should expose a compact model-adoption decision summary");
        AssertTrue(shellViewModelSource.Contains("ModelCenterDecisionEvidenceText", StringComparison.Ordinal), "shell ViewModel should expose model-adoption evidence text");
        AssertTrue(shellViewModelSource.Contains("ModelCenterDecisionActionText", StringComparison.Ordinal), "shell ViewModel should expose the exact model-adoption action text");
        AssertTrue(shellViewModelSource.Contains("SetModelCenterAnomalyEvaluationState", StringComparison.Ordinal), "shell ViewModel should expose anomaly classification evaluation state for Model Center");
        AssertTrue(shellViewModelSource.Contains("ClearModelCenterAnomalyEvaluationState", StringComparison.Ordinal), "shell ViewModel should clear anomaly classification evaluation state");
        AssertTrue(shellViewModelSource.Contains("RunAnomalyEvaluationCommand", StringComparison.Ordinal), "shell ViewModel should expose a command for running anomaly evaluation");
        AssertTrue(shellViewModelSource.Contains("LoadAnomalyEvaluationSummaryCommand", StringComparison.Ordinal), "shell ViewModel should expose a command for explicitly loading an anomaly evaluation summary");
        AssertTrue(shellViewModelSource.Contains("SetModelCenterAnomalyEvaluationPickerVisible", StringComparison.Ordinal), "shell ViewModel should own anomaly evaluation summary picker visibility");
        AssertTrue(shellSource.Contains("RefreshModelCenterAnomalyEvaluationState", StringComparison.Ordinal), "model-center dashboard should refresh anomaly classification evaluation state from the active output root");
        AssertTrue(shellSource.Contains("ExecuteRunAnomalyEvaluationCommand", StringComparison.Ordinal), "model-center dashboard should route anomaly evaluation execution through a command");
        AssertTrue(shellSource.Contains("ExecuteLoadAnomalyEvaluationSummaryCommand", StringComparison.Ordinal), "model-center dashboard should route explicit anomaly evaluation summary loading through a command");
        AssertTrue(shellSource.Contains("manualModelCenterAnomalyEvaluationSummaryPath", StringComparison.Ordinal), "model-center dashboard should keep a manually selected anomaly evaluation summary until the dataset context changes");
        AssertTrue(shellSource.Contains("FindModelCenterAnomalyEvaluationSummaryPath", StringComparison.Ordinal), "model-center dashboard should own a bounded anomaly evaluation summary lookup");
        AssertTrue(shellSource.Contains("classification-evaluation-summary.json", StringComparison.Ordinal), "model-center dashboard should look for the stable anomaly evaluation summary artifact name");
        AssertTrue(shellSource.Contains("WpfAnomalyClassificationEvaluationPresentationService.Build", StringComparison.Ordinal), "model-center dashboard should delegate anomaly evaluation wording to the presentation service");
        AssertTrue(shellViewModelSource.Contains("SetModelRegistryState", StringComparison.Ordinal), "shell ViewModel should expose model-registry presentation state");
        AssertTrue(shellViewModelSource.Contains("ModelRegistryProfileText", StringComparison.Ordinal), "shell ViewModel should expose the model profile registry row");
        AssertTrue(shellViewModelSource.Contains("ModelRegistryTrainingRunText", StringComparison.Ordinal), "shell ViewModel should expose the training-run registry row");
        AssertTrue(shellViewModelSource.Contains("ModelRegistryCandidateModelText", StringComparison.Ordinal), "shell ViewModel should expose the candidate-model registry row");
        AssertTrue(shellViewModelSource.Contains("ModelRegistryInspectionModelText", StringComparison.Ordinal), "shell ViewModel should expose the inspection-model registry row");
        AssertTrue(shellViewModelSource.Contains("ModelRegistryHistoryItems", StringComparison.Ordinal), "shell ViewModel should expose recent model history rows");
        AssertTrue(shellViewModelSource.Contains("SelectedModelRegistryHistoryItem", StringComparison.Ordinal), "shell ViewModel should expose selected model-history rows");
        AssertTrue(shellViewModelSource.Contains("SelectedModelHistoryComparisonMetricText", StringComparison.Ordinal), "shell ViewModel should expose selected model-history comparison metrics");
        AssertTrue(shellViewModelSource.Contains("PromoteSelectedModelHistoryCommand", StringComparison.Ordinal), "shell ViewModel should expose an explicit model-history apply command");
        AssertTrue(shellViewModelSource.Contains("IsModelRegistryHistoryVisible", StringComparison.Ordinal), "shell ViewModel should expose model-history list visibility");
        AssertTrue(shellViewModelSource.Contains("ModelRegistrySummaryPrimaryText", StringComparison.Ordinal), "shell ViewModel should expose compact model-registry primary summary text");
        AssertTrue(shellViewModelSource.Contains("ModelRegistrySummarySecondaryText", StringComparison.Ordinal), "shell ViewModel should expose compact model-registry secondary summary text");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"ModelRegistrySummaryPanel\"", StringComparison.Ordinal),
            "model center should expose a stable model-registry summary panel");
        XElement modelRegistryDetailExpander = shellXaml.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Expander"
                && string.Equals((string)element.Attribute(xName), "ModelRegistryDetailExpander", StringComparison.Ordinal));
        AssertTrue(modelRegistryDetailExpander != null, "model registry detailed rows should live behind a stable expander");
        AssertEqual("False", (string)modelRegistryDetailExpander.Attribute("IsExpanded"));
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"ModelRegistryHistoryPanel\"", StringComparison.Ordinal),
            "model center should expose a stable model-registry history panel");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"SelectedModelHistoryComparisonPanel\"", StringComparison.Ordinal),
            "model center should expose a stable selected-history comparison panel");
        int historyListStart = shellXamlSource.IndexOf("x:Name=\"ModelRegistryHistoryItems\"", StringComparison.Ordinal);
        int selectedHistoryPanelStart = shellXamlSource.IndexOf("x:Name=\"ModelRegistrySelectedHistoryPanel\"", StringComparison.Ordinal);
        AssertTrue(historyListStart >= 0 && selectedHistoryPanelStart > historyListStart, "model center should keep the model-history list before the selected detail panel");
        string modelHistoryListTemplate = shellXamlSource.Substring(historyListStart, selectedHistoryPanelStart - historyListStart);
        AssertTrue(modelHistoryListTemplate.Contains("AutomationProperties.AutomationId=\"ModelRegistryHistoryItemSummaryText\"", StringComparison.Ordinal),
            "model-history rows should expose a compact summary text target");
        AssertTrue(modelHistoryListTemplate.Contains("AutomationProperties.AutomationId=\"ModelRegistryHistoryItemDecisionText\"", StringComparison.Ordinal),
            "model-history rows should expose a compact decision text target");
        AssertTrue(modelHistoryListTemplate.Contains("ItemContainerStyle=\"{StaticResource ModelRegistryHistoryListBoxItemStyle}\"", StringComparison.Ordinal),
            "model-history rows should use a model-center selection style instead of the global review-list red selection");
        AssertTrue(modelHistoryListTemplate.Contains("MaxHeight=\"56\"", StringComparison.Ordinal)
            && modelHistoryListTemplate.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal),
            "model-history list should stay tightly bounded so selected comparison details remain visible in the right panel");
        AssertTrue(!modelHistoryListTemplate.Contains("Text=\"{Binding DetailText}\"", StringComparison.Ordinal)
            && !modelHistoryListTemplate.Contains("Text=\"{Binding MetricText}\"", StringComparison.Ordinal),
            "model-history rows should keep detail and metrics in the selected detail panel instead of repeating them in the list");
        int selectedComparisonStart = shellXamlSource.IndexOf("x:Name=\"SelectedModelHistoryComparisonPanel\"", StringComparison.Ordinal);
        int selectedHistoryActionStart = shellXamlSource.IndexOf("x:Name=\"PromoteSelectedModelHistoryButton\"", StringComparison.Ordinal);
        AssertTrue(selectedComparisonStart >= 0 && selectedHistoryActionStart > selectedComparisonStart,
            "model center should keep the selected-history comparison before the apply action");
        AssertTrue(selectedComparisonStart < historyListStart,
            "model center should show the selected-history comparison before the dense history list in short right panels");
        int selectedHistoryDetailTextStart = shellXamlSource.IndexOf("x:Name=\"SelectedModelHistoryDetailText\"", StringComparison.Ordinal);
        int selectedHistoryMetricTextStart = shellXamlSource.IndexOf("x:Name=\"SelectedModelHistoryMetricText\"", StringComparison.Ordinal);
        AssertTrue(selectedHistoryDetailTextStart > selectedComparisonStart && selectedHistoryMetricTextStart > selectedComparisonStart,
            "selected-history comparison should appear before long detail and metric text so it remains visible in short model-center panels");
        string selectedComparisonTemplate = shellXamlSource.Substring(selectedComparisonStart, historyListStart - selectedComparisonStart);
        AssertTrue(!selectedComparisonTemplate.Contains("CanvasBrush", StringComparison.Ordinal)
            && !selectedComparisonTemplate.Contains("CornerRadius=", StringComparison.Ordinal)
            && !selectedComparisonTemplate.Contains("BorderThickness=", StringComparison.Ordinal),
            "selected-history comparison should be a thin summary row, not another nested card");
        AssertTrue(selectedComparisonTemplate.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "selected-history comparison row should trim long model paths instead of expanding the right panel");
        AssertTrue(selectedComparisonTemplate.Contains("x:Name=\"SelectedModelHistoryCurrentRoleText\"", StringComparison.Ordinal)
            && selectedComparisonTemplate.Contains("x:Name=\"SelectedModelHistorySelectedRoleText\"", StringComparison.Ordinal),
            "selected-history comparison should label the current inspection model and selected history model separately");
        string selectedHistoryTemplate = shellXamlSource.Substring(selectedHistoryPanelStart, selectedHistoryActionStart - selectedHistoryPanelStart);
        AssertTrue(selectedHistoryTemplate.Contains("BorderBrush=\"{DynamicResource ModelCenterCandidateBrush}\"", StringComparison.Ordinal)
            && selectedHistoryTemplate.Contains("Foreground=\"{DynamicResource ModelCenterDecisionBrush}\"", StringComparison.Ordinal),
            "selected model-history detail should use candidate/decision emphasis instead of global error red");
        AssertTrue(!selectedHistoryTemplate.Contains("AccentBrush", StringComparison.Ordinal),
            "selected model-history detail should not make selectable history look like a failure");
        AssertTrue(shellSource.Contains("WpfModelCenterDashboardPresentationService.Build", StringComparison.Ordinal), "WPF shell should build Model Center state through the dashboard presentation service");
        AssertTrue(shellSource.Contains("ExecutePromoteSelectedModelHistoryCommand", StringComparison.Ordinal), "WPF shell should route model-history promotion through an adapter command");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"ModelCenterPriorityPanel\"", StringComparison.Ordinal),
            "model center should expose a first-visible priority panel before dense history details");
        AssertTrue(shellXamlSource.Contains("x:Key=\"ModelCenterCandidateBrush\"", StringComparison.Ordinal)
            && shellXamlSource.Contains("x:Key=\"ModelCenterDecisionBrush\"", StringComparison.Ordinal),
            "model center should keep candidate/decision emphasis separate from the global red accent");
        int modelHistoryItemStyleStart = shellXamlSource.IndexOf("x:Key=\"ModelRegistryHistoryListBoxItemStyle\"", StringComparison.Ordinal);
        int imageQueueTextStyleStart = shellXamlSource.IndexOf("x:Key=\"ImageQueueTextStyle\"", StringComparison.Ordinal);
        AssertTrue(modelHistoryItemStyleStart >= 0 && imageQueueTextStyleStart > modelHistoryItemStyleStart,
            "model center should define a dedicated model-history item style before generic image queue styles");
        string modelHistoryItemStyleTemplate = shellXamlSource.Substring(modelHistoryItemStyleStart, imageQueueTextStyleStart - modelHistoryItemStyleStart);
        AssertTrue(modelHistoryItemStyleTemplate.Contains("BorderBrush\" Value=\"{DynamicResource ModelCenterCandidateBrush}\"", StringComparison.Ordinal),
            "selected model-history rows should use candidate emphasis instead of error red");
        AssertTrue(!modelHistoryItemStyleTemplate.Contains("AccentBrush", StringComparison.Ordinal),
            "model-history item selection style should not use the global red accent");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"ModelCenterPriorityCurrentModelText\"", StringComparison.Ordinal),
            "model center priority panel should show the active inspection model");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"ModelCenterPriorityCandidateModelText\"", StringComparison.Ordinal),
            "model center priority panel should show the trained candidate model");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"ModelCenterPriorityNextActionText\"", StringComparison.Ordinal),
            "model center priority panel should show the next model action");
        int modelCenterPriorityStart = shellXamlSource.IndexOf("x:Name=\"ModelCenterPriorityPanel\"", StringComparison.Ordinal);
        int modelRegistrySummaryStart = shellXamlSource.IndexOf("x:Name=\"ModelRegistrySummaryPanel\"", StringComparison.Ordinal);
        AssertTrue(modelCenterPriorityStart >= 0 && modelRegistrySummaryStart > modelCenterPriorityStart,
            "model center priority panel should appear before registry details");
        string modelCenterPriorityTemplate = shellXamlSource.Substring(modelCenterPriorityStart, modelRegistrySummaryStart - modelCenterPriorityStart);
        AssertTrue(modelCenterPriorityTemplate.Contains("BorderBrush=\"{DynamicResource ModelCenterCandidateBrush}\"", StringComparison.Ordinal)
            && modelCenterPriorityTemplate.Contains("Foreground=\"{DynamicResource ModelCenterDecisionBrush}\"", StringComparison.Ordinal)
            && modelCenterPriorityTemplate.Contains("Foreground=\"{DynamicResource ModelCenterCandidateBrush}\"", StringComparison.Ordinal),
            "model center priority panel should distinguish candidate and decision states without using error red");
        AssertTrue(!modelCenterPriorityTemplate.Contains("AccentBrush", StringComparison.Ordinal),
            "model center priority panel should not make normal candidate/decision state look like an error");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPriorityReviewCandidateButton", "Command", "ShellViewModel.ReviewCandidateModelCommand");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPriorityReviewCandidateButton", "IsEnabled", "ShellViewModel.IsModelCenterReviewCandidateEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPrioritySaveModelButton", "Command", "YoloModelSettingsViewModel.SaveSettingsCommand");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPrioritySaveModelButton", "IsEnabled", "ShellViewModel.IsModelCenterConfirmModelEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPriorityInspectCurrentButton", "Command", "ShellViewModel.DetectCurrentImageCommand");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPriorityInspectCurrentButton", "IsEnabled", "ShellViewModel.IsModelCenterInspectCurrentImageEnabled");
        AssertNamedXamlBinding(shellXaml, xName, "ModelCenterPriorityButtonStateText", "Text", "ShellViewModel.ModelCenterActionStateText");
        AssertTrue(shellViewModelSource.Contains("ModelCenterActionStateText", StringComparison.Ordinal), "shell ViewModel should expose a visible model-center action state summary");
        AssertTrue(shellXamlSource.Contains("AutomationProperties.AutomationId=\"YoloModelAdoptionDecisionPanel\"", StringComparison.Ordinal), "model center decision card should expose a stable AutomationId for visual smoke checks");
        XElement adoptionDecisionDetailExpander = shellXaml.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Expander"
                && string.Equals((string)element.Attribute(xName), "YoloModelAdoptionDecisionDetailExpander", StringComparison.Ordinal));
        AssertTrue(adoptionDecisionDetailExpander != null, "model adoption decision evidence should live behind a stable detail expander");
        AssertEqual("False", (string)adoptionDecisionDetailExpander.Attribute("IsExpanded"));
        int adoptionDecisionStart = shellXamlSource.IndexOf("x:Name=\"YoloModelAdoptionDecisionPanel\"", StringComparison.Ordinal);
        int adoptionDecisionEnd = shellXamlSource.IndexOf("x:Name=\"YoloDatasetReadinessQuickPanel\"", StringComparison.Ordinal);
        AssertTrue(adoptionDecisionStart >= 0 && adoptionDecisionEnd > adoptionDecisionStart, "model adoption decision panel should be before dataset readiness details");
        string adoptionDecisionTemplate = shellXamlSource.Substring(adoptionDecisionStart, adoptionDecisionEnd - adoptionDecisionStart);
        AssertTrue(adoptionDecisionTemplate.Contains("x:Name=\"YoloModelAdoptionDecisionDetailExpander\"", StringComparison.Ordinal),
            "model adoption decision details should be collapsible");
        AssertTrue(adoptionDecisionTemplate.Contains("x:Name=\"YoloModelAdoptionDecisionSummaryText\"", StringComparison.Ordinal)
            && adoptionDecisionTemplate.Contains("TextTrimming=\"CharacterEllipsis\"", StringComparison.Ordinal),
            "model adoption decision summary should stay one-line in the default view");
        AssertTrue(adoptionDecisionTemplate.Contains("BorderBrush=\"{DynamicResource ModelCenterDecisionBrush}\"", StringComparison.Ordinal)
            && adoptionDecisionTemplate.Contains("Foreground=\"{DynamicResource ModelCenterDecisionBrush}\"", StringComparison.Ordinal),
            "model adoption decision panel should use decision emphasis instead of global error red");
        AssertTrue(!adoptionDecisionTemplate.Contains("AccentBrush", StringComparison.Ordinal),
            "model adoption decision panel should not read as a recovery/error panel");
        int anomalyEvaluationStart = shellXamlSource.IndexOf("x:Name=\"YoloAnomalyEvaluationPanel\"", StringComparison.Ordinal);
        int lifecycleDetailStart = shellXamlSource.IndexOf("x:Name=\"YoloModelLifecycleDetailPanel\"", StringComparison.Ordinal);
        AssertTrue(anomalyEvaluationStart > adoptionDecisionStart && anomalyEvaluationStart < adoptionDecisionEnd,
            "anomaly classification evaluation should appear in the model-center decision flow before dataset readiness details");
        AssertTrue(lifecycleDetailStart > anomalyEvaluationStart,
            "anomaly classification evaluation should remain above the collapsed lifecycle detail table");
        string anomalyEvaluationTemplate = shellXamlSource.Substring(anomalyEvaluationStart, lifecycleDetailStart - anomalyEvaluationStart);
        AssertTrue(anomalyEvaluationTemplate.Contains("Text=\"{Binding ShellViewModel.ModelCenterAnomalyEvaluationRecommendationText}\"", StringComparison.Ordinal)
            && anomalyEvaluationTemplate.Contains("Text=\"{Binding ShellViewModel.ModelCenterAnomalyEvaluationDetailText}\"", StringComparison.Ordinal),
            "anomaly evaluation card should expose recommendation and blocker detail text");
        XElement anomalyEvaluationDetailExpander = shellXaml.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Expander"
                && string.Equals((string)element.Attribute(xName), "YoloAnomalyEvaluationDetailExpander", StringComparison.Ordinal));
        AssertTrue(anomalyEvaluationDetailExpander != null, "anomaly evaluation blocker detail should live behind a stable detail expander");
        AssertEqual("False", (string)anomalyEvaluationDetailExpander.Attribute("IsExpanded"));
        AssertTrue(anomalyEvaluationTemplate.Contains("x:Name=\"YoloAnomalyEvaluationDetailExpander\"", StringComparison.Ordinal),
            "anomaly evaluation detail should be collapsible in the default model-center view");
        AssertTrue(!anomalyEvaluationTemplate.Contains("AccentBrush", StringComparison.Ordinal),
            "anomaly evaluation hold state should use model-decision emphasis instead of global error red");
        XElement datasetReadinessExpander = shellXaml.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "Expander"
                && string.Equals((string)element.Attribute(xName), "YoloDatasetReadinessQuickPanel", StringComparison.Ordinal));
        AssertTrue(datasetReadinessExpander != null, "YOLO dataset readiness details should be an expander in the model-center stack");
        AssertEqual("False", (string)datasetReadinessExpander.Attribute("IsExpanded"));
        AssertEqual("\uD3C9\uAC00 \uB370\uC774\uD130 \uADFC\uAC70", (string)datasetReadinessExpander.Attribute("Header"));
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "YoloEvaluationDataPurposeText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloEvaluationDataPurposeText", "Text", "LearningWorkflowViewModel.SelectedDatasetPurposeMode.Text");
        AssertNamedXamlBinding(shellXaml, xName, "YoloDatasetQuickReadinessText", "Text", "TrainingSettingsViewModel.TrainingReadinessText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloDatasetQuickRefreshButton", "Command", "TrainingSettingsViewModel.RefreshReadinessCommand");
        AssertNamedXamlElement(shellXaml, xName, "Button", "YoloExternalEvaluationAuditButton");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "YoloExternalEvaluationAuditStatusText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "YoloExternalEvaluationAuditDetailText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "YoloExternalEvaluationAuditLimitText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalEvaluationAuditButton", "Command", "LearningWorkflowViewModel.ExternalEvaluationDataAuditCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalEvaluationAuditStatusText", "Text", "LearningWorkflowViewModel.ExternalEvaluationDataAuditStatusText");
        AssertNamedXamlElement(shellXaml, xName, "ComboBox", "YoloExternalYoloDatasetPurposeComboBox");
        AssertNamedXamlElement(shellXaml, xName, "Button", "YoloExternalYoloDatasetSelectButton");
        AssertNamedXamlElement(shellXaml, xName, "Button", "YoloExternalYoloDatasetActivateButton");
        AssertNamedXamlElement(shellXaml, xName, "Button", "YoloExternalYoloDatasetClearButton");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "YoloExternalYoloDatasetStatusText");
        AssertNamedXamlElement(shellXaml, xName, "TextBlock", "YoloExternalYoloDatasetDetailText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetPurposeComboBox", "ItemsSource", "LearningWorkflowViewModel.ExternalYoloDatasetPurposeModes");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetPurposeComboBox", "SelectedItem", "LearningWorkflowViewModel.SelectedExternalYoloDatasetPurposeMode");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetSelectButton", "Command", "LearningWorkflowViewModel.SelectExternalYoloDatasetCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetActivateButton", "Command", "LearningWorkflowViewModel.ActivateExternalYoloDatasetCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetClearButton", "Command", "LearningWorkflowViewModel.ClearExternalYoloDatasetCommand");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetStatusText", "Text", "LearningWorkflowViewModel.ExternalYoloDatasetIntakeStatusText");
        AssertNamedXamlBinding(shellXaml, xName, "YoloExternalYoloDatasetDetailText", "Text", "LearningWorkflowViewModel.ExternalYoloDatasetIntakeDetailText");
        int externalYoloDatasetCardStart = shellXamlSource.IndexOf("x:Name=\"YoloExternalYoloDatasetPurposeComboBox\"", StringComparison.Ordinal);
        int externalYoloDatasetCardEnd = shellXamlSource.IndexOf("<local:WpfYoloStatusPanel", StringComparison.Ordinal);
        AssertTrue(
            externalYoloDatasetCardStart > adoptionDecisionEnd
            && externalYoloDatasetCardStart < externalYoloDatasetCardEnd,
            "external YOLO data.yaml intake should stay in the model-center data evidence panel");
        XElement externalEvaluationLimitText = shellXaml.Descendants()
            .FirstOrDefault(element => string.Equals((string)element.Attribute(xName), "YoloExternalEvaluationAuditLimitText", StringComparison.Ordinal));
        AssertTrue(
            ((string)externalEvaluationLimitText?.Attribute("Text") ?? string.Empty).Contains("SHA-256", StringComparison.Ordinal)
            && ((string)externalEvaluationLimitText?.Attribute("Text") ?? string.Empty).Contains("\uBAA8\uB378 \uCC44\uD0DD", StringComparison.Ordinal),
            "evaluation data evidence should distinguish image independence from model adoption evidence");
        AssertTrue(shellSource.Contains("ExecuteExternalEvaluationDataAuditCommand", StringComparison.Ordinal), "external evaluation folder browsing should stay in the shell UI adapter");
        AssertTrue(shellSource.Contains("ExecuteSelectExternalYoloDatasetCommand", StringComparison.Ordinal), "external YOLO data.yaml selection should stay in the shell UI adapter");
        AssertTrue(shellSource.Contains("YoloExternalDatasetIntakeService.Build", StringComparison.Ordinal), "external YOLO data.yaml parsing should stay in the intake service");
        AssertTrue(shellViewModelSource.Contains("ReviewCandidateModelCommand", StringComparison.Ordinal), "shell ViewModel should expose a model-center candidate review command");
        AssertTrue(shellViewModelSource.Contains("SetModelCenterCandidateReviewState", StringComparison.Ordinal), "shell ViewModel should own model-center candidate review button state");
        AssertTrue(shellViewModelSource.Contains("IsModelCenterReviewCandidateEnabled", StringComparison.Ordinal), "shell ViewModel should own model-center candidate review enablement");
        AssertTrue(shellViewModelSource.Contains("SetWorkflowStage", StringComparison.Ordinal), "shell ViewModel should expose active workflow stage state separately from canvas display mode");
        AssertTrue(shellViewModelSource.Contains("IsSavedLabelsViewVisible", StringComparison.Ordinal), "shell ViewModel should expose saved-label review visibility");
        AssertTrue(shellViewModelSource.Contains("IsCandidateReviewViewVisible", StringComparison.Ordinal), "shell ViewModel should expose AI-candidate review visibility");
        AssertTrue(shellViewModelSource.Contains("IsYoloModelCenterViewVisible", StringComparison.Ordinal), "shell ViewModel should expose YOLO/model-center visibility");
        AssertTrue(shellViewModelSource.Contains("IsWorkflowStageModelActionPanelVisible", StringComparison.Ordinal), "shell ViewModel should own the top model-action panel visibility instead of composing it in XAML triggers");
        AssertTrue(shellViewModelSource.Contains("IsRightWorkflowSubNavigationVisible", StringComparison.Ordinal), "shell ViewModel should expose whether the right workflow view needs subnavigation");
        AssertTrue(shellViewModelSource.Contains("IsRightWorkflowShortcutBarVisible", StringComparison.Ordinal), "shell ViewModel should expose whether compact right workflow shortcuts are visible");
        AssertTrue(shellViewModelSource.Contains("ShowSavedLabelsViewCommand", StringComparison.Ordinal), "shell ViewModel should expose saved-label right workflow shortcut command");
        AssertTrue(shellViewModelSource.Contains("ShowLabelingGuideViewCommand", StringComparison.Ordinal), "shell ViewModel should expose guide/tool right workflow shortcut command");
        AssertTrue(shellViewModelSource.Contains("ShowClassCatalogViewCommand", StringComparison.Ordinal), "shell ViewModel should expose class-schema right workflow shortcut command");
        AssertTrue(shellViewModelSource.Contains("IsSavedLabelsShortcutActive", StringComparison.Ordinal), "shell ViewModel should expose saved-label shortcut active state");
        AssertTrue(shellViewModelSource.Contains("IsLabelingGuideShortcutActive", StringComparison.Ordinal), "shell ViewModel should expose guide/tool shortcut active state");
        AssertTrue(shellViewModelSource.Contains("IsClassCatalogShortcutActive", StringComparison.Ordinal), "shell ViewModel should expose class shortcut active state");
        AssertTrue(shellSource.Contains("ExecuteChangeDatasetCommand", StringComparison.Ordinal), "WPF shell should route the main-header dataset change button to the dataset selection flow");
        AssertTrue(shellSource.Contains("ExecuteDatasetHomeCommand", StringComparison.Ordinal), "WPF shell should route the dataset-home workflow stage through an adapter command");
        AssertTrue(shellSource.Contains("ExecuteLabelingWorkbenchCommand", StringComparison.Ordinal), "WPF shell should route the labeling-workbench workflow stage through an adapter command");
        AssertTrue(shellSource.Contains("ExecuteInferenceReviewCommand", StringComparison.Ordinal), "WPF shell should route the inference-review workflow stage through an adapter command");
        AssertTrue(shellSource.Contains("ExecuteTrainingModelCenterCommand", StringComparison.Ordinal), "WPF shell should route the training/model workflow stage through an adapter command");
        AssertTrue(shellSource.Contains("ExecuteReviewCandidateModelCommand", StringComparison.Ordinal), "WPF shell should route the model-center candidate review action through an adapter command");
        AssertTrue(shellSource.Contains("RefreshModelCenterDashboard", StringComparison.Ordinal), "WPF shell should refresh the training/model dashboard from model comparison state");
        AssertTrue(shellSource.Contains("ExecuteOpenDatasetRootFolderCommand", StringComparison.Ordinal), "WPF shell should route the main-header dataset folder button through a command method");
        AssertTrue(shellSource.Contains("RefreshShellDatasetContext", StringComparison.Ordinal), "WPF shell should refresh the main-header dataset context after dataset state changes");
        string changeDatasetSource = FindMethodSourceBlock(shellSource, "private void ExecuteChangeDatasetCommand()");
        AssertTrue(changeDatasetSource.Contains("WpfDatasetSelectionWindow", StringComparison.Ordinal), "dataset change should open the existing-dataset selector first");
        AssertTrue(changeDatasetSource.Contains("createNewRequested", StringComparison.Ordinal), "dataset creation should be a separate branch after an explicit selector request");
        AssertTrue(changeDatasetSource.Contains("ApplySelectedDatasetRecipe", StringComparison.Ordinal), "dataset change should apply the selected existing dataset");
        AssertTrue(!changeDatasetSource.Contains("new WpfDatasetSetupWizardWindow", StringComparison.Ordinal), "dataset change should not open the creation wizard directly");
        AssertTrue(keyInputArgsSource.Contains("OriginalSource", StringComparison.Ordinal), "key input DTO should preserve original source for shell text-edit shortcut suppression");
        AssertTrue(shellSource.Contains("DataContext = viewModels", StringComparison.Ordinal), "WPF shell should expose ShellViewModel explicitly to XAML bindings");
        AssertTrue(shellSource.Contains("ConfigureShellCommands", StringComparison.Ordinal), "WPF shell should inject top toolbar commands through the shell ViewModel");
        AssertTrue(!shellXamlSource.Contains("Click=", StringComparison.Ordinal), "WPF shell XAML should not use direct Click handlers for toolbar commands");
        AssertTrue(!shellXamlSource.Contains("Loaded=\"Window_Loaded\"", StringComparison.Ordinal), "WPF shell XAML should route Loaded through a lifecycle command behavior");
        AssertTrue(!shellXamlSource.Contains("Closed=\"Window_Closed\"", StringComparison.Ordinal), "WPF shell XAML should route Closed through a lifecycle command behavior");
        AssertTrue(!shellSource.Contains("_Click(", StringComparison.Ordinal), "WPF shell partials should not keep legacy button click wrappers after ViewModel command routing");
        AssertTrue(!shellSource.Contains("RoutedEventArgs", StringComparison.Ordinal), "WPF shell command paths should not depend on routed event args after ViewModel command routing");
        AssertTrue(!shellSource.Contains("PreviewKeyDown +=", StringComparison.Ordinal), "WPF shell should route global shortcut keys through the shell ViewModel command");
        AssertTrue(!shellSource.Contains("WpfLabelingShellWindow_PreviewKeyDown", StringComparison.Ordinal), "WPF shell should avoid direct PreviewKeyDown event handler naming after command routing");
        AssertTrue(shellSource.Contains("ShellViewModel.ApplyWorkflowCommandState", StringComparison.Ordinal), "WPF shell should push current-image detection availability through the shell ViewModel");
        AssertTrue(shellSource.Contains("ShellViewModel?.SetWorkflowModeState", StringComparison.Ordinal), "WPF shell should push top workflow mode button state through the shell ViewModel");
        AssertTrue(shellSource.Contains("ImageQueueViewModel.ApplyWorkflowCommandState", StringComparison.Ordinal), "WPF shell should push queue detection availability through the image queue ViewModel");
        AssertTrue(shellSource.Contains("ConfigureImageQueuePanelCommands", StringComparison.Ordinal), "WPF shell should inject image queue commands through the ViewModel");
        AssertTrue(shellSource.Contains("ExecuteLoadImageRootQueueCommand", StringComparison.Ordinal), "WPF shell should expose image queue load as an event-agnostic execute method");
        AssertTrue(shellSource.Contains("ExecuteOpenCurrentImageFolderCommand", StringComparison.Ordinal), "WPF shell should expose current image-folder open as an event-agnostic execute method");
        AssertTrue(shellSource.Contains("SelectSingleVisibleQueueSearchResult();", StringComparison.Ordinal), "queue search should auto-select an exact single result for stable reopen/review UX");
        AssertTrue(shellSource.Contains("UpdateSelectedQueueImageButton(item)", StringComparison.Ordinal), "single-result queue search should enable the visible open action without forcing an extra row click");
        AssertTrue(shellSource.Contains("GetOpenSelectedQueueSelection()", StringComparison.Ordinal), "queue open should resolve selection through a dedicated helper instead of trusting only the DataGrid control state");
        AssertTrue(shellSource.Contains("ImageQueueViewModel?.SelectedQueueItem", StringComparison.Ordinal), "queue open should fall back to the ViewModel-selected row for UIAutomation and keyboard flows");
        string openQueueSelectionSource = FindMethodSourceBlock(shellSource, "private WpfImageQueueOpenSelection GetOpenSelectedQueueSelection()");
        AssertTrue(openQueueSelectionSource.Contains("FindSingleSearchMatchedQueueItem()", StringComparison.Ordinal), "queue open should use unique search text matches when selection state is stale");
        AssertTrue(!openQueueSelectionSource.Contains("imageQueueView.Refresh", StringComparison.Ordinal), "queue open should use the already-current text/filter view instead of reevaluating every row");
        AssertTrue(openQueueSelectionSource.Contains("imageQueueSelectionService.ResolveOpenSelection", StringComparison.Ordinal), "queue open should resolve candidate priority and open path through the selection service");
        string searchMatchSource = FindMethodSourceBlock(shellSource, "private WpfImageQueueItem FindSingleSearchMatchedQueueItem()");
        AssertTrue(searchMatchSource.Contains("WpfImageQueueFilterService.FindSingleSearchMatch", StringComparison.Ordinal), "queue search fallback should delegate single-match resolution to the filter service");
        AssertTrue(shellSource.Contains("WpfImageQueueFilterService.CountSearchMatches", StringComparison.Ordinal), "queue failure diagnostics should count search matches through the filter service");
        AssertTrue(shellSource.Contains("ExecuteDetectSelectedQueueCommand", StringComparison.Ordinal), "WPF shell should expose selected queue detection as an event-agnostic execute method");
        AssertTrue(shellSource.Contains("ExecuteQueueFilterCandidateCommand", StringComparison.Ordinal), "WPF shell should expose queue quick filters as event-agnostic execute methods");
        AssertTrue(shellSource.Contains("ExecuteQueueFilterUnfinishedCommand", StringComparison.Ordinal), "WPF shell should expose the work-needed queue quick filter as an event-agnostic execute method");
        AssertTrue(!shellSource.Contains("() => LoadImageRootButton_Click(ImageQueuePanelControl, new RoutedEventArgs())", StringComparison.Ordinal), "WPF image queue command wiring should not synthesize RoutedEventArgs for load");
        AssertTrue(!shellSource.Contains("() => DetectSelectedQueueButton_Click(ImageQueuePanelControl, new RoutedEventArgs())", StringComparison.Ordinal), "WPF image queue command wiring should not synthesize RoutedEventArgs for selected detection");
        AssertTrue(!shellSource.Contains("() => QueueFilterAllButton_Click(ImageQueuePanelControl, new RoutedEventArgs())", StringComparison.Ordinal), "WPF image queue command wiring should not synthesize RoutedEventArgs for quick filters");
        AssertTrue(!shellSource.Contains("new RoutedEventArgs()", StringComparison.Ordinal), "WPF shell command paths should not synthesize RoutedEventArgs after MVVM command routing");
        AssertTrue(shellSource.Contains("ExecuteStartTrainingCommand", StringComparison.Ordinal), "WPF training command wiring should target an event-agnostic execute method");
        AssertTrue(shellSource.Contains("ExecuteSaveYoloSettingsCommand", StringComparison.Ordinal), "WPF YOLO settings command wiring should target an event-agnostic execute method");
        AssertTrue(shellSource.Contains("WpfFileDialogService", StringComparison.Ordinal), "WPF shell should delegate file/folder pickers to a service");
        AssertTrue(!shellSource.Contains("new OpenFileDialog", StringComparison.Ordinal), "WPF shell should not construct file dialogs directly");
        AssertTrue(!shellSource.Contains("new OpenFolderDialog", StringComparison.Ordinal), "WPF shell should not construct folder dialogs directly");
        AssertTrue(shellSource.Contains("ImageQueueViewModel.SetSelectedImageAvailability", StringComparison.Ordinal), "WPF shell should push selected queue image availability through the image queue ViewModel");
        AssertTrue(!shellSource.Contains("TeachingModeButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the labeling mode button on the normal path");
        AssertTrue(!shellSource.Contains("InferenceModeButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the inference mode button on the normal path");
        AssertTrue(!shellSource.Contains("ApplyWorkflowModeButtonState", StringComparison.Ordinal), "WPF shell should not keep direct workflow button styling in code-behind");
        AssertTrue(!shellSource.Contains("OpenSelectedQueueImageButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the selected image open button on the normal path");
        AssertTrue(!shellSource.Contains("DetectButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the current-image detect button on the normal path");
        AssertTrue(!shellSource.Contains("DetectSelectedQueueButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the selected queue detect button on the normal path");
        AssertTrue(!shellSource.Contains("BatchDetectQueueButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the batch queue detect button on the normal path");
        AssertTrue(!shellSource.Contains("RetryFailedQueueButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the retry queue detect button on the normal path");
        AssertTrue(!shellSource.Contains("StopBatchQueueButton.IsEnabled", StringComparison.Ordinal), "WPF shell should not directly enable the stop batch button on the normal path");

        string trainingCommandPresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Training", "WpfTrainingCommandPresentationService.cs"));
        string startTrainingCommandSource = FindMethodSourceBlock(shellSource, "private async void ExecuteStartTrainingCommand()");
        string stopTrainingCommandSource = FindMethodSourceBlock(shellSource, "private async void ExecuteStopTrainingCommand()");
        AssertTrue(trainingCommandPresentationSource.Contains("BuildStartCommandResultStatus", StringComparison.Ordinal), "training command presentation service should own start command result wording");
        AssertTrue(trainingCommandPresentationSource.Contains("BuildStopCommandResultStatus", StringComparison.Ordinal), "training command presentation service should own stop command result wording");
        string acceptedTrainingText = WpfTrainingCommandPresentationService.BuildStartCommandResultStatus(true);
        AssertTrue(acceptedTrainingText.Contains("첫 에폭 로그", StringComparison.Ordinal), "training start accepted text should tell the operator what to wait for next");
        AssertTrue(acceptedTrainingText.Contains("아직 성공 완료가 아니며", StringComparison.Ordinal), "training request transport must not be presented as completed training");
        AssertTrue(WpfTrainingCommandPresentationService.BuildStopFailureRecovery("실패").Action.Contains("재시작", StringComparison.Ordinal), "training stop failure recovery should give a concrete next action");
        AssertTrue(startTrainingCommandSource.Contains("WpfTrainingCommandPresentationService", StringComparison.Ordinal), "start training command should delegate status wording to the presentation service");
        AssertTrue(shellSource.Contains("if (TryAutoConnectAnomalyTrainingRuntime())", StringComparison.Ordinal), "successful anomaly runtime auto-connection should proceed to worker restart instead of being blocked by stale YOLOv5 capabilities");
        AssertTrue(stopTrainingCommandSource.Contains("WpfTrainingCommandPresentationService", StringComparison.Ordinal), "stop training command should delegate status wording to the presentation service");
        AssertEqual(1, startTrainingCommandSource.Split(new[] { "SetYoloRecoveryStatus(" }, StringSplitOptions.None).Length - 1);
        AssertEqual(1, stopTrainingCommandSource.Split(new[] { "SetYoloRecoveryStatus(" }, StringSplitOptions.None).Length - 1);
        AssertTrue(!startTrainingCommandSource.Contains("pendingRecoveryTitle", StringComparison.Ordinal), "start training command should keep recovery state in a single typed DTO instead of duplicate title/detail/action locals");
        AssertTrue(!stopTrainingCommandSource.Contains("pendingRecoveryTitle", StringComparison.Ordinal), "stop training command should keep recovery state in a single typed DTO instead of duplicate title/detail/action locals");

        string modelCandidateDecisionPresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Model", "WpfModelCandidateDecisionPresentationService.cs"));
        string modelCandidateDecisionCommandSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.ModelCandidateDecisionCommands.cs"));
        string yoloEnvironmentBrowseCommandSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.YoloEnvironmentBrowseCommands.cs"));
        WpfModelCandidateDecisionPresentation pendingModelDecision = WpfModelCandidateDecisionPresentationService.BuildPendingCandidate(
            Path.Combine("runs", "train", "exp7", "weights", "best.pt"),
            Path.Combine("models", "baseline.pt"),
            canReject: true);
        WpfModelCandidateDecisionPresentation heldModelDecision = WpfModelCandidateDecisionPresentationService.BuildHeldCandidate(
            Path.Combine("runs", "segment", "exp8", "weights", "best.pt"),
            canReject: true);
        WpfModelCandidateDecisionPresentation rejectedModelDecision = WpfModelCandidateDecisionPresentationService.BuildRejectedCandidate("best.pt", string.Empty);
        AssertTrue(modelCandidateDecisionPresentationSource.Contains("BuildPendingCandidate", StringComparison.Ordinal), "model candidate decision presentation service should own pending decision wording");
        AssertTrue(modelCandidateDecisionPresentationSource.Contains("BuildRejectCommandStatus", StringComparison.Ordinal), "model candidate decision presentation service should own reject-result wording");
        AssertTrue(pendingModelDecision.CanSave && pendingModelDecision.CanReject, "pending model candidate decision should allow both save and reject when a baseline exists");
        AssertTrue(pendingModelDecision.StatusText.Contains("best.pt", StringComparison.Ordinal), "pending model candidate decision should name the trained weights");
        AssertTrue(pendingModelDecision.DetailText.Contains("baseline.pt", StringComparison.Ordinal), "pending model candidate decision should name the retained baseline model");
        AssertTrue(!heldModelDecision.CanSave && heldModelDecision.CanReject, "held model candidate decision should block adoption while allowing rejection to keep the baseline");
        AssertTrue(heldModelDecision.StatusText.Contains("\uAC80\uC99D \uBCF4\uB958", StringComparison.Ordinal), "held model candidate decision should explain that validation blocked adoption");
        AssertTrue(WpfModelCandidateDecisionPresentationService.BuildHeldCandidateSaveBlockedStatus().Contains("\uC800\uC7A5\uD558\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4", StringComparison.Ordinal), "held candidate direct-command guard should explain that the model was not saved");
        AssertTrue(rejectedModelDecision.DetailText.Contains("채택하지 않았습니다", StringComparison.Ordinal), "rejected model candidate decision should explain the candidate was not adopted");
        AssertTrue(modelCandidateDecisionCommandSource.Contains("WpfModelCandidateDecisionPresentationService", StringComparison.Ordinal), "model candidate decision command should delegate operator-facing wording to the presentation service");
        AssertTrue(modelCandidateDecisionCommandSource.Contains("ApplyModelCandidateDecisionPresentation", StringComparison.Ordinal), "model candidate decision command should only adapt presentation DTOs into the ViewModel");
        AssertTrue(modelCandidateDecisionCommandSource.Contains("IsModelPromotionHeld", StringComparison.Ordinal), "model candidate save command and panel should fail closed when held-out comparison blocks promotion");
        AssertTrue(yoloEnvironmentBrowseCommandSource.Contains("pendingWeightsRecipeSave && CandidateReviewViewModel?.IsModelPromotionHeld", StringComparison.Ordinal), "generic model-profile save should not bypass a held candidate adoption guard");
        AssertTrue(!modelCandidateDecisionCommandSource.Contains("후보 결정: 저장 또는 거절 필요", StringComparison.Ordinal), "model candidate decision command should not inline pending decision wording");
        AssertTrue(!modelCandidateDecisionCommandSource.Contains("이미 거절된 후보입니다", StringComparison.Ordinal), "model candidate decision command should not inline rejected-candidate tooltips");

        string yoloEnvironmentPresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Runtime", "WpfYoloEnvironmentCommandPresentationService.cs"));
        string yoloEnvironmentRuntimeCommandSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.YoloEnvironmentRuntimeCommands.cs"));
        string yoloEnvironmentLifecycleSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.YoloEnvironmentCommandLifecycle.cs"));
        string modelRuntimeUnavailablePresentationSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Services", "Model", "WpfModelRuntimeUnavailablePresentationService.cs"));
        AssertTrue(yoloEnvironmentPresentationSource.Contains("BuildEnvironmentCheckStartingStatus", StringComparison.Ordinal), "YOLO environment command presentation service should own check-start wording");
        AssertTrue(yoloEnvironmentPresentationSource.Contains("BuildRequirementsInstallResultStatus", StringComparison.Ordinal), "YOLO environment command presentation service should own requirements install result wording");
        AssertTrue(yoloEnvironmentPresentationSource.Contains("BuildRequirementsCheckPresentation", StringComparison.Ordinal), "YOLO environment command presentation service should own requirements check result branching");
        AssertTrue(WpfYoloEnvironmentCommandPresentationService.BuildEnvironmentCheckStartingStatus().Contains("점검", StringComparison.Ordinal), "environment check start text should be operator-readable");
        AssertTrue(WpfYoloEnvironmentCommandPresentationService.BuildRequirementsInstallingLog(new[] { "torch", "ultralytics" }).Contains("torch", StringComparison.Ordinal), "requirements install log should name missing packages");
        WpfRequirementsCheckPresentation failedRequirementsCheck = WpfYoloEnvironmentCommandPresentationService.BuildRequirementsCheckPresentation(new PythonEnvironmentCheckResult
        {
            Errors = new[] { "python missing" }
        });
        WpfRequirementsCheckPresentation readyRequirementsCheck = WpfYoloEnvironmentCommandPresentationService.BuildRequirementsCheckPresentation(new PythonEnvironmentCheckResult());
        WpfRequirementsCheckPresentation installRequirementsCheck = WpfYoloEnvironmentCommandPresentationService.BuildRequirementsCheckPresentation(new PythonEnvironmentCheckResult
        {
            MissingPackages = new[] { "torch", "ultralytics" }
        });
        AssertTrue(!failedRequirementsCheck.ShouldInstallRequirements && failedRequirementsCheck.StatusText.Contains("건너뜀", StringComparison.Ordinal), "requirements errors should skip installation with the existing operator status");
        AssertTrue(!readyRequirementsCheck.ShouldInstallRequirements && readyRequirementsCheck.StatusText.Contains("정상", StringComparison.Ordinal), "ready requirements should not launch installation");
        AssertTrue(installRequirementsCheck.ShouldInstallRequirements && installRequirementsCheck.IsBusy && installRequirementsCheck.LogText.Contains("torch", StringComparison.Ordinal), "missing requirements should retain install-needed busy state and package names");
        AssertTrue(WpfYoloEnvironmentCommandPresentationService.BuildModelTestCompletedStatus().Contains("\uC644\uB8CC", StringComparison.Ordinal), "model test completion wording should be built by the presentation service");
        AssertTrue(WpfYoloEnvironmentCommandPresentationService.BuildModelTestFailureRecovery("boom").Action.Contains("\uD14C\uC2A4\uD2B8", StringComparison.Ordinal), "model test recovery action should direct the operator back to the test command");
        AssertTrue(WpfYoloEnvironmentCommandPresentationService.BuildWorkerRestartFailureStatus("boom").Contains("\uC7AC\uC2DC\uC791 \uC2E4\uD328", StringComparison.Ordinal), "worker restart failure wording should be built by the presentation service");
        AssertTrue(WpfYoloEnvironmentCommandPresentationService.BuildWorkerStopCompletedStatus().Contains("\uC911\uC9C0 \uC644\uB8CC", StringComparison.Ordinal), "worker stop completion wording should be built by the presentation service");
        WpfYoloWorkerCommandPresentation disconnectedWorker = WpfYoloEnvironmentCommandPresentationService.BuildWorkerRestartResult(false, "connection failed");
        WpfYoloWorkerCommandPresentation restartedWorker = WpfYoloEnvironmentCommandPresentationService.BuildWorkerRestartResult(true, "ignored");
        WpfYoloWorkerCommandPresentation restartFailureWorker = WpfYoloEnvironmentCommandPresentationService.BuildWorkerRestartFailure("boom");
        WpfYoloWorkerCommandPresentation stoppedWorker = WpfYoloEnvironmentCommandPresentationService.BuildWorkerStopCompleted();
        WpfYoloWorkerCommandPresentation stopFailureWorker = WpfYoloEnvironmentCommandPresentationService.BuildWorkerStopFailure("boom");
        AssertTrue(disconnectedWorker.StatusText == "connection failed" && disconnectedWorker.Recovery != null, "disconnected worker restart should keep the existing failure text and recovery guidance");
        AssertTrue(restartedWorker.StatusText.Contains("연결 완료", StringComparison.Ordinal) && restartedWorker.Recovery == null, "connected worker restart should not show recovery guidance");
        AssertTrue(restartFailureWorker.StatusText.Contains("재시작 실패", StringComparison.Ordinal) && restartFailureWorker.Recovery != null, "restart exception should provide service-owned recovery guidance");
        AssertTrue(stoppedWorker.StatusText.Contains("중지 완료", StringComparison.Ordinal) && stoppedWorker.Recovery == null, "worker stop completion should not show recovery guidance");
        AssertTrue(stopFailureWorker.StatusText.Contains("중지 실패", StringComparison.Ordinal) && stopFailureWorker.Recovery == null, "worker stop failure should preserve the existing no-recovery behavior");
        AssertTrue(yoloEnvironmentRuntimeCommandSource.Contains("WpfYoloEnvironmentCommandPresentationService", StringComparison.Ordinal), "YOLO environment runtime commands should delegate status wording to a presentation service");
        AssertTrue(yoloEnvironmentRuntimeCommandSource.Contains("ApplyYoloWorkerCommandPresentation", StringComparison.Ordinal), "YOLO environment runtime commands should apply worker result DTOs through one UI adapter");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("BuildWorkerRestartConnectionFailureRecovery", StringComparison.Ordinal), "YOLO environment runtime commands should not build worker restart recovery directly");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("BuildWorkerStopCompletedStatus", StringComparison.Ordinal), "YOLO environment runtime commands should not build worker stop status directly");
        AssertTrue(yoloEnvironmentRuntimeCommandSource.Contains("BuildRequirementsCheckPresentation", StringComparison.Ordinal), "YOLO environment runtime commands should delegate requirements result branching to the presentation service");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("check.Errors.Count", StringComparison.Ordinal), "YOLO environment runtime commands should not branch on requirements errors directly");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("check.MissingPackages.Count", StringComparison.Ordinal), "YOLO environment runtime commands should not branch on missing requirements directly");
        AssertTrue(yoloEnvironmentRuntimeCommandSource.Contains("BuildModelTestFailureRecovery", StringComparison.Ordinal), "model test recovery wording should be delegated to the presentation service");
        AssertTrue(yoloEnvironmentRuntimeCommandSource.Contains("BuildWorkerRestartFailure", StringComparison.Ordinal), "worker restart failure presentation should be delegated to the presentation service");
        AssertTrue(yoloEnvironmentRuntimeCommandSource.Contains("BuildWorkerStopFailure", StringComparison.Ordinal), "worker stop failure presentation should be delegated to the presentation service");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("\\uBAA8\\uB378 \\uD14C\\uC2A4\\uD2B8 \\uCD94\\uB860 \\uC2E4\\uD328", StringComparison.Ordinal), "YOLO environment runtime command should not inline model-test failure wording");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("\\uCD94\\uB860 \\uC2E4\\uD589\\uAE30 \\uC7AC\\uC2DC\\uC791 \\uC2E4\\uD328", StringComparison.Ordinal), "YOLO environment runtime command should not inline restart failure wording");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("\\uCD94\\uB860 \\uC2E4\\uD589\\uAE30 \\uC911\\uC9C0 \\uC644\\uB8CC", StringComparison.Ordinal), "YOLO environment runtime command should not inline worker-stop completion wording");
        AssertTrue(yoloEnvironmentLifecycleSource.Contains("WpfYoloEnvironmentCommandPresentationService.BuildBusyCommandLog", StringComparison.Ordinal), "YOLO environment command lifecycle should delegate busy command text to a presentation service");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("모델 실행 환경 준비 완료", StringComparison.Ordinal), "YOLO environment runtime command should not inline check-ready wording");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("추론 실행 환경 정상", StringComparison.Ordinal), "YOLO environment runtime command should not inline requirements-ready wording");
        AssertTrue(!yoloEnvironmentRuntimeCommandSource.Contains("누락 실행 환경 패키지", StringComparison.Ordinal), "YOLO environment runtime command should not inline missing-package install wording");
        var unavailablePresentation = WpfModelRuntimeUnavailablePresentationService.Build(
            new PythonModelRuntimeState(
                PythonModelRuntimeStateKind.NotInstalled,
                canRunTraining: false,
                canRunInference: false,
                "summary",
                "detail",
                "next"),
            "blocked");
        AssertEqual("blocked", unavailablePresentation.CommandStatusText);
        AssertTrue(unavailablePresentation.CurrentModelText.Contains("\uBBF8\uC124\uCE58", StringComparison.Ordinal), "model runtime unavailable presentation should show missing-runtime current-model text");
        AssertTrue(unavailablePresentation.InspectionStatusText.Contains("\uBBF8\uC124\uCE58", StringComparison.Ordinal), "model runtime unavailable presentation should show missing-runtime inspection status text");
        AssertTrue(unavailablePresentation.ModelStatusText.Contains("\uBBF8\uC124\uCE58", StringComparison.Ordinal), "model runtime unavailable presentation should show missing-runtime top model status text");
        AssertTrue(unavailablePresentation.DecisionEvidenceText.Contains("\uB77C\uBCA8\uB9C1", StringComparison.Ordinal), "model runtime unavailable presentation should explain labeling remains possible");
        AssertTrue(modelRuntimeUnavailablePresentationSource.Contains("WpfModelRuntimeUnavailablePresentation", StringComparison.Ordinal), "model runtime unavailable service should expose a presentation DTO");
        AssertTrue(yoloRuntimeStatusSource.Contains("WpfModelRuntimeUnavailablePresentationService.Build", StringComparison.Ordinal), "YOLO runtime status should delegate unavailable presentation wording to the service");
        AssertTrue(!yoloRuntimeStatusSource.Contains("\\uD604\\uC7AC \\uAC80\\uC0AC \\uBAA8\\uB378: \\uBAA8\\uB378 \\uC2E4\\uD589\\uAE30 \\uBBF8\\uC124\\uCE58", StringComparison.Ordinal), "YOLO runtime status should not inline unavailable current-model text");
        AssertTrue(!yoloRuntimeStatusSource.Contains("\\uBAA8\\uB378 \\uAE30\\uB2A5: \\uBBF8\\uC124\\uCE58", StringComparison.Ordinal), "YOLO runtime status should not inline unavailable inspection status text");
        AssertTrue(!yoloRuntimeStatusSource.Contains("\\uBAA8\\uB378: \\uBBF8\\uC124\\uCE58", StringComparison.Ordinal), "YOLO runtime status should not inline unavailable top model status text");
        AssertTrue(!ContainsVisibleMojibakeArtifact(modelRuntimeUnavailablePresentationSource), "model runtime unavailable presentation service should not contain visible mojibake artifacts");

        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        WpfLabelingShellWindow window = new WpfLabelingShellWindow();
        try
        {
            AssertEqual("OpenVisionLab Labeling Studio", window.Title);
            AssertTrue(window.GetType().BaseType?.FullName == "Wpf.Ui.Controls.FluentWindow", "WPF shell should inherit WPF-UI FluentWindow");
            AssertTrue(window.MainCanvasViewModel != null, "WPF shell canvas view model was not created");
            AssertTrue(window.ShellViewModel != null, "WPF shell view model was not created");
            AssertEqual("WpfLabelingShellWindow", window.ShellViewModel.ViewName);
            AssertTrue(window.FindName("ShellTitleBar") != null, "WPF-UI title bar was not created");
            AssertTrue(window.FindName("ShellTitleBar").GetType().FullName == "Wpf.Ui.Controls.TitleBar", "WPF shell title bar should use WPF-UI TitleBar");
            var shellTitleBarElement = (System.Windows.FrameworkElement)window.FindName("ShellTitleBar");
            AssertEqual(28D, shellTitleBarElement.Height);
            var shellHeaderBarElement = (System.Windows.FrameworkElement)window.FindName("ShellHeaderBar");
            AssertTrue(shellHeaderBarElement != null, "WPF compact command header was not created");
            AssertEqual(44D, shellHeaderBarElement.Height);
            AssertTrue(window.FindName("FirstCheckYoloButton") != null, "WPF YOLO first-check button was not created");
            AssertTrue(window.FindName("InstallRequirementsButton") != null, "WPF YOLO install button was not created");
            AssertTrue(window.FindName("RunYoloSmokeButton") != null, "WPF YOLO test button was not created");
            AssertTrue(window.FindName("RestartPythonWorkerButton") != null, "WPF YOLO restart button was not created");
            AssertTrue(window.FindName("StopPythonWorkerButton") != null, "WPF YOLO stop button was not created");
            AssertTrue(window.FindName("YoloStatusPanelControl") != null, "WPF YOLO status user control was not created");
            AssertTrue(window.FindName("YoloStatusPanelControl").GetType().FullName == "MvcVisionSystem.WpfYoloStatusPanel", "WPF YOLO status should be hosted by a UserControl");
            AssertTrue(((WpfYoloStatusPanel)window.FindName("YoloStatusPanelControl")).ViewModel != null, "WPF YOLO status view model was not created");
            AssertTrue(window.FindName("YoloRuntimeDetailsExpander") != null, "WPF YOLO runtime details expander was not created");
            AssertTrue(window.FindName("ProjectConfigPanelControl") != null, "WPF project config user control was not created");
            AssertTrue(window.FindName("ProjectConfigPanelControl").GetType().FullName == "MvcVisionSystem.WpfProjectConfigPanel", "WPF project config should be hosted by a UserControl");
            AssertTrue(((WpfProjectConfigPanel)window.FindName("ProjectConfigPanelControl")).ViewModel != null, "WPF project config view model was not created");
            AssertTrue(window.FindName("ProjectRecipeNameBox") != null, "WPF project recipe name box was not created");
            AssertTrue(window.FindName("ProjectRecipeListBox") != null, "WPF project recipe list was not created");
            AssertTrue(window.FindName("ProjectConfigPathBox") != null, "WPF project config path box was not created");
            AssertTrue(window.FindName("ApplyProjectRecipeButton") != null, "WPF project recipe apply button was not created");
            AssertTrue(window.FindName("RefreshProjectRecipeListButton") != null, "WPF project recipe refresh button was not created");
            AssertTrue(window.FindName("SaveProjectConfigButton") != null, "WPF project config save button was not created");
            AssertTrue(window.FindName("OpenProjectConfigFolderButton") != null, "WPF project config folder button was not created");
            AssertTrue(window.FindName("ThemeToggleButton") != null, "WPF theme toggle button was not created");
            AssertTrue(window.FindName("ThemeToggleText") != null, "WPF theme toggle text was not created");
            AssertTrue(window.FindName("ThemeToggleButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF theme toggle should use WPF-UI button");
            AssertTrue(window.FindName("YoloCommandStatusText") != null, "WPF YOLO command status text was not created");
            AssertTrue(window.FindName("YoloSettingsScrollViewer") != null, "WPF YOLO settings scroll viewer was not created");
            var modelCenterTaskTabs = window.FindName("YoloModelCenterTaskTabs") as System.Windows.Controls.TabControl;
            var modelCenterOverviewTaskTab = window.FindName("YoloModelCenterOverviewTaskTab") as System.Windows.Controls.TabItem;
            var modelCenterDataTaskTab = window.FindName("YoloModelCenterDataTaskTab") as System.Windows.Controls.TabItem;
            var modelCenterTrainingTaskTab = window.FindName("YoloModelCenterTrainingTaskTab") as System.Windows.Controls.TabItem;
            var modelCenterRuntimeTaskTab = window.FindName("YoloModelCenterRuntimeTaskTab") as System.Windows.Controls.TabItem;
            var modelBenchmarkEntryPanel = window.FindName("ModelBenchmarkEntryPanel") as System.Windows.FrameworkElement;
            var openModelBenchmarkWindowButton = window.FindName("OpenModelBenchmarkWindowButton") as Wpf.Ui.Controls.Button;
            AssertTrue(modelCenterTaskTabs != null, "WPF model center should expose a task selector");
            AssertEqual(4, modelCenterTaskTabs.Items.Count);
            AssertTrue(modelCenterOverviewTaskTab != null && modelCenterDataTaskTab != null && modelCenterTrainingTaskTab != null && modelCenterRuntimeTaskTab != null,
                "WPF model center should declare overview, data, training/comparison, and runtime task tabs");
            AssertTrue(ReferenceEquals(modelCenterOverviewTaskTab, modelCenterTaskTabs.SelectedItem), "WPF model center should open on the compact overview task");
            AssertTrue(modelBenchmarkEntryPanel != null, "WPF model center should expose the separate model-benchmark window entry");
            AssertTrue(openModelBenchmarkWindowButton?.Command != null, "WPF model benchmark entry should bind a shell ViewModel command");
            AssertEqual(System.Windows.Visibility.Collapsed, modelBenchmarkEntryPanel.Visibility);
            AssertTrue(!shellXamlSource.Contains("YoloModelComparisonBenchmarkText", StringComparison.Ordinal), "WPF model center should not keep dense benchmark metrics in the left panel");
            AssertTrue(window.FindName("YoloModelSettingsPanelControl") != null, "WPF YOLO model settings user control was not created");
            AssertTrue(window.FindName("YoloModelSettingsPanelControl").GetType().FullName == "MvcVisionSystem.WpfYoloModelSettingsPanel", "WPF YOLO model settings should be hosted by a UserControl");
            AssertTrue(((WpfYoloModelSettingsPanel)window.FindName("YoloModelSettingsPanelControl")).ViewModel != null, "WPF YOLO model settings view model was not created");
            AssertTrue(window.FindName("YoloModelEngineBox") != null, "WPF YOLO model engine selector was not created");
            AssertTrue(window.FindName("YoloProjectRootBox") != null, "WPF YOLO model settings editor was not created");
            AssertTrue(window.FindName("BrowseYoloPythonButton") != null, "WPF YOLO python browse button was not created");
            AssertTrue(window.FindName("BrowseYoloProjectRootButton") != null, "WPF YOLO project browse button was not created");
            AssertTrue(window.FindName("BrowseYoloClientScriptButton") != null, "WPF YOLO client browse button was not created");
            AssertTrue(window.FindName("BrowseYoloWeightsButton") != null, "WPF YOLO weights browse button was not created");
            AssertTrue(window.FindName("BrowseYoloImageRootButton") != null, "WPF YOLO image root browse button was not created");
            AssertTrue(window.FindName("SaveYoloSettingsButton") != null, "WPF YOLO settings save button was not created");
            AssertTrue(window.FindName("TrainingImageSizeBox") != null, "WPF training settings editor was not created");
            AssertTrue(window.FindName("TrainingProgressText") != null, "WPF training progress text was not created");
            AssertTrue(window.FindName("TrainingEpochText") != null, "WPF training epoch text was not created");
            AssertTrue(window.FindName("TrainingSettingsExpander") != null, "WPF training settings expander was not created");
            AssertTrue(window.FindName("TrainingSettingsPanelControl") != null, "WPF training settings user control was not created");
            AssertTrue(window.FindName("TrainingSettingsPanelControl").GetType().FullName == "MvcVisionSystem.WpfTrainingSettingsPanel", "WPF training settings should be hosted by a UserControl");
            AssertTrue(((WpfTrainingSettingsPanel)window.FindName("TrainingSettingsPanelControl")).ViewModel != null, "WPF training settings view model was not created");
            AssertTrue(window.FindName("StartTrainingButton") != null, "WPF training start button was not created");
            AssertTrue(window.FindName("CandidateReviewPanelControl") != null, "WPF candidate review user control was not created");
            AssertTrue(window.FindName("CandidateReviewPanelControl").GetType().FullName == "MvcVisionSystem.WpfCandidateReviewPanel", "WPF candidate review should be hosted by a UserControl");
            var candidateReviewPanel = (WpfCandidateReviewPanel)window.FindName("CandidateReviewPanelControl");
            AssertTrue(candidateReviewPanel.ViewModel != null, "WPF candidate review view model was not created");
            AssertTrue(candidateReviewPanel.FindName("CandidateReviewModePanel") != null, "WPF candidate review mode panel was not created");
            AssertTrue(window.FindName("CandidateReviewRoleSplitPanel") != null, "WPF candidate/model role split panel was not registered");
            AssertTrue(window.FindName("CurrentImageCandidateRoleCard") != null, "WPF current-image candidate role card was not registered");
            AssertTrue(window.FindName("ModelValidationRoleCard") != null, "WPF model-validation role card was not registered");
            AssertEqual("AI \uD6C4\uBCF4", ((System.Windows.Controls.TabItem)window.FindName("CandidatesReviewTab")).Header?.ToString());
            AssertTrue(window.FindName("CandidateConfidenceSlider") != null, "WPF candidate confidence filter was not created");
            AssertTrue(window.FindName("TeachingModeButton") != null, "WPF labeling mode button was not created");
            AssertTrue(window.FindName("InferenceModeButton") != null, "WPF inference mode button was not created");
            var workflowStageRailElement = (System.Windows.FrameworkElement)window.FindName("WorkflowStageRail");
            AssertTrue(workflowStageRailElement != null, "WPF workflow stage rail was not created");
            AssertEqual(48D, workflowStageRailElement.Height);
            var workflowContextHeaderElement = (System.Windows.FrameworkElement)window.FindName("WorkflowContextHeader");
            AssertTrue(workflowContextHeaderElement != null, "WPF workflow context header was not created");
            AssertEqual(82D, workflowContextHeaderElement.Height);
            AssertTrue(window.FindName("DatasetHomeStageButton") != null, "WPF dataset-home workflow stage button was not created");
            AssertTrue(window.FindName("LabelingWorkbenchStageButton") != null, "WPF labeling-workbench workflow stage button was not created");
            AssertTrue(window.FindName("InferenceReviewStageButton") != null, "WPF inference-review workflow stage button was not created");
            AssertTrue(window.FindName("TrainingModelStageButton") != null, "WPF training/model workflow stage button was not created");
            AssertTrue(window.FindName("WorkflowStageSummaryPanel") != null, "WPF workflow stage summary panel was not created");
            AssertTrue(window.FindName("WorkflowStageSummaryProgressText") != null, "WPF workflow stage progress text was not created");
            AssertTrue(window.FindName("WorkflowStageSummaryTitleText") != null, "WPF workflow stage title text was not created");
            AssertTrue(window.FindName("WorkflowStageSummaryDetailText") != null, "WPF workflow stage detail text was not created");
            AssertTrue(window.FindName("WorkflowStageSummaryNextActionText") != null, "WPF workflow stage next-action text was not created");
            var workflowStageModelActionPanel = (System.Windows.FrameworkElement)window.FindName("WorkflowStageModelActionPanel");
            AssertTrue(workflowStageModelActionPanel != null, "WPF workflow stage post-training action panel was not created");
            AssertTrue(window.FindName("WorkflowStageReviewCandidateModelButton") != null, "WPF workflow stage candidate-review button was not created");
            AssertTrue(window.FindName("WorkflowStageSaveModelSettingsButton") != null, "WPF workflow stage save-model button was not created");
            AssertTrue(window.FindName("WorkflowStageInspectCurrentImageButton") != null, "WPF workflow stage current-inspection button was not created");
            AssertTrue(window.FindName("RightWorkflowViewTitleText") != null, "WPF right workflow title text was not created");
            var workflowStageSubNavigationRail = (System.Windows.FrameworkElement)window.FindName("WorkflowStageSubNavigationRail");
            AssertTrue(workflowStageSubNavigationRail != null, "WPF workflow stage subnavigation rail was not created");
            AssertEqual(34D, workflowStageSubNavigationRail.Height);
            var rightWorkflowShortcutBar = (System.Windows.FrameworkElement)window.FindName("RightWorkflowShortcutBar");
            AssertTrue(rightWorkflowShortcutBar != null, "WPF right workflow shortcut bar was not created");
            AssertTrue(window.FindName("RightWorkflowDatasetHomeButton") != null, "WPF dataset-home shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowSavedLabelsButton") != null, "WPF saved-label shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowGuideToolsButton") != null, "WPF guide/tools shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowClassCatalogButton") != null, "WPF class shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowInferenceCandidatesButton") != null, "WPF inference-candidates shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowInferenceInspectButton") != null, "WPF inference-inspect shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowTrainingModelButton") != null, "WPF training/model shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowTrainingReviewCandidateButton") != null, "WPF training candidate-review shortcut button was not created");
            AssertTrue(window.FindName("RightWorkflowTrainingInspectButton") != null, "WPF training inspect shortcut button was not created");
            var rightWorkflowColumn = (System.Windows.Controls.ColumnDefinition)window.FindName("RightWorkflowColumn");
            var rightWorkflowExpandedContent = (System.Windows.FrameworkElement)window.FindName("RightWorkflowExpandedContent");
            var rightWorkflowCollapsedRail = (System.Windows.FrameworkElement)window.FindName("RightWorkflowCollapsedRail");
            var rightWorkflowDockToggleButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowDockToggleButton");
            var rightWorkflowRailOpenButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowRailOpenButton");
            var modelWorkspaceCanvasPanel = (System.Windows.FrameworkElement)window.FindName("CanvasPanelControl");
            var modelWorkspaceImageQueuePanel = (System.Windows.FrameworkElement)window.FindName("ImageQueuePanelControl");
            var modelWorkspaceImageQueueColumn = (System.Windows.Controls.ColumnDefinition)window.FindName("ImageQueueColumn");
            AssertTrue(rightWorkflowColumn != null, "WPF right workflow column should be named for responsive layout checks");
            AssertTrue(rightWorkflowExpandedContent != null, "WPF right workflow expanded content was not created");
            AssertTrue(rightWorkflowCollapsedRail != null, "WPF right workflow collapsed rail was not created");
            AssertTrue(rightWorkflowDockToggleButton != null, "WPF right workflow dock toggle was not created");
            AssertTrue(rightWorkflowRailOpenButton != null, "WPF right workflow rail open button was not created");
            AssertTrue(modelWorkspaceCanvasPanel != null, "WPF model workspace canvas panel was not created");
            AssertTrue(modelWorkspaceImageQueuePanel != null, "WPF model workspace image queue panel was not created");
            AssertTrue(modelWorkspaceImageQueueColumn != null, "WPF model workspace image queue column was not created");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(rightWorkflowDockToggleButton), window.ShellViewModel.ToggleRightWorkflowDockCommand), "WPF right workflow dock toggle should bind to the shell dock command");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(rightWorkflowRailOpenButton), window.ShellViewModel.ToggleRightWorkflowDockCommand), "WPF right workflow rail open button should bind to the shell dock command");
            var datasetHomeShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowDatasetHomeButton");
            var savedLabelsShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowSavedLabelsButton");
            var guideToolsShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowGuideToolsButton");
            var classCatalogShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowClassCatalogButton");
            var inferenceCandidatesShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowInferenceCandidatesButton");
            var inferenceInspectShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowInferenceInspectButton");
            var trainingModelShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowTrainingModelButton");
            var trainingReviewCandidateShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowTrainingReviewCandidateButton");
            var trainingInspectShortcutButton = (System.Windows.Controls.Button)window.FindName("RightWorkflowTrainingInspectButton");
            var savedLabelsReviewTab = (System.Windows.Controls.TabItem)window.FindName("ObjectsReviewTab");
            var aiCandidatesReviewTab = (System.Windows.Controls.TabItem)window.FindName("CandidatesReviewTab");
            var guideToolsReviewTab = (System.Windows.Controls.TabItem)window.FindName("LearningReviewTab");
            var classCatalogReviewTab = (System.Windows.Controls.TabItem)window.FindName("ClassesReviewTab");
            var yoloModelCenterReviewTab = (System.Windows.Controls.TabItem)window.FindName("YoloSettingsReviewTab");
            AssertEqual(System.Windows.Visibility.Visible, workflowStageSubNavigationRail.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowShortcutBar.Visibility);
            AssertTrue(window.ShellViewModel.IsRightWorkflowDockExpanded, "dataset stage should keep the right workflow panel expanded for onboarding");
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowExpandedContent.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, rightWorkflowCollapsedRail.Visibility);
            AssertEqual(340D, rightWorkflowColumn.Width.Value);
            AssertEqual(System.Windows.Visibility.Visible, datasetHomeShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, savedLabelsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, guideToolsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, classCatalogShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, inferenceCandidatesShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, inferenceInspectShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingModelShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingReviewCandidateShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingInspectShortcutButton.Visibility);
            AssertTrue(object.Equals(datasetHomeShortcutButton.Tag, true), "dataset-home shortcut should be active by default in dataset stage");
            AssertTrue(!object.Equals(classCatalogShortcutButton.Tag, true), "class shortcut should start inactive in dataset stage");
            AssertEqual(System.Windows.Visibility.Collapsed, savedLabelsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, aiCandidatesReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, guideToolsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, classCatalogReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, yoloModelCenterReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, workflowStageModelActionPanel.Visibility);
            window.ShellViewModel.SetWorkflowStage(WpfShellWorkflowStage.Labeling);
            window.UpdateLayout();
            AssertTrue(!window.ShellViewModel.IsRightWorkflowDockExpanded, "labeling stage should collapse the right workflow panel by default");
            AssertEqual(System.Windows.Visibility.Collapsed, rightWorkflowExpandedContent.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowCollapsedRail.Visibility);
            AssertEqual(72D, rightWorkflowColumn.Width.Value);
            window.ShellViewModel.ToggleRightWorkflowDockCommand.Execute(null);
            window.UpdateLayout();
            AssertTrue(window.ShellViewModel.IsRightWorkflowDockExpanded, "right workflow toggle should expand the collapsed labeling rail");
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowExpandedContent.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, rightWorkflowCollapsedRail.Visibility);
            AssertEqual(340D, rightWorkflowColumn.Width.Value);
            AssertEqual(System.Windows.Visibility.Visible, workflowStageSubNavigationRail.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowShortcutBar.Visibility);
            AssertTrue(object.Equals(savedLabelsShortcutButton.Tag, true), "saved-label shortcut should be active by default in labeling stage");
            AssertTrue(!object.Equals(guideToolsShortcutButton.Tag, true), "guide/tools shortcut should start inactive in labeling stage");
            AssertTrue(!object.Equals(classCatalogShortcutButton.Tag, true), "class shortcut should start inactive in labeling stage");
            AssertEqual(System.Windows.Visibility.Collapsed, datasetHomeShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, savedLabelsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, guideToolsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, classCatalogShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, inferenceCandidatesShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, inferenceInspectShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingModelShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingReviewCandidateShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingInspectShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, savedLabelsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, aiCandidatesReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, guideToolsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, classCatalogReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, yoloModelCenterReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, workflowStageModelActionPanel.Visibility);
            InvokePrivate(window, "EnterLabelingWorkbenchStartView");
            window.UpdateLayout();
            AssertTrue(window.ShellViewModel.IsLabelingStageActive, "dataset setup completion should land on the labeling stage");
            AssertTrue(!window.ShellViewModel.IsRightWorkflowDockExpanded, "dataset setup completion should keep the beginner side workflow collapsed");
            AssertTrue(window.ShellViewModel.IsRightWorkflowDockRailVisible, "dataset setup completion should leave only the compact side rail visible");
            AssertTrue(window.ShellViewModel.IsSavedLabelsShortcutActive, "dataset setup completion should select the saved-label view, not class management");
            AssertTrue(!window.ShellViewModel.IsClassCatalogShortcutActive, "dataset setup completion should not force class management active");
            AssertTrue(ReferenceEquals(savedLabelsReviewTab, ((System.Windows.Controls.TabControl)window.FindName("ReviewTabControl")).SelectedItem), "dataset setup completion should keep the saved-label tab selected for the collapsed rail");
            window.ShellViewModel.SetWorkflowStage(WpfShellWorkflowStage.Inference);
            window.UpdateLayout();
            AssertTrue(window.ShellViewModel.IsRightWorkflowDockExpanded, "inference review should keep the right workflow panel expanded");
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowExpandedContent.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, rightWorkflowCollapsedRail.Visibility);
            AssertEqual(340D, rightWorkflowColumn.Width.Value);
            AssertEqual(System.Windows.Visibility.Visible, workflowStageSubNavigationRail.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowShortcutBar.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, datasetHomeShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, savedLabelsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, guideToolsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, classCatalogShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, inferenceCandidatesShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, inferenceInspectShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingModelShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingReviewCandidateShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, trainingInspectShortcutButton.Visibility);
            AssertTrue(object.Equals(inferenceCandidatesShortcutButton.Tag, true), "inference candidates shortcut should be active in inference stage");
            AssertTrue(!object.Equals(savedLabelsShortcutButton.Tag, true), "saved-label shortcut should be inactive outside labeling stage");
            AssertTrue(!object.Equals(guideToolsShortcutButton.Tag, true), "guide/tools shortcut should be inactive outside labeling stage");
            AssertTrue(!object.Equals(classCatalogShortcutButton.Tag, true), "class shortcut should be inactive outside labeling stage");
            AssertEqual(System.Windows.Visibility.Collapsed, savedLabelsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, aiCandidatesReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, guideToolsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, classCatalogReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, yoloModelCenterReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, workflowStageModelActionPanel.Visibility);
            window.ShellViewModel.SetImageQueueExpandedPaneWidth(420D);
            window.UpdateLayout();
            AssertEqual(420D, modelWorkspaceImageQueueColumn.Width.Value);
            window.ShellViewModel.SetWorkflowStage(WpfShellWorkflowStage.TrainingModel);
            window.UpdateLayout();
            AssertTrue(window.ShellViewModel.IsRightWorkflowDockExpanded, "training/model stage should keep the model center expanded");
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowExpandedContent.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, rightWorkflowCollapsedRail.Visibility);
            AssertTrue(window.ShellViewModel.IsModelWorkspaceActive, "training/model stage should activate the dedicated model workspace");
            AssertEqual(System.Windows.GridUnitType.Star, rightWorkflowColumn.Width.GridUnitType);
            AssertEqual(System.Windows.Visibility.Collapsed, modelWorkspaceCanvasPanel.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, modelWorkspaceImageQueuePanel.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, rightWorkflowDockToggleButton.Visibility);
            AssertEqual(0D, modelWorkspaceImageQueueColumn.Width.Value);
            AssertEqual(System.Windows.Visibility.Visible, workflowStageSubNavigationRail.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowShortcutBar.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, datasetHomeShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, savedLabelsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, guideToolsShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, classCatalogShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, inferenceCandidatesShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, inferenceInspectShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, trainingModelShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, trainingReviewCandidateShortcutButton.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, trainingInspectShortcutButton.Visibility);
            AssertTrue(object.Equals(trainingModelShortcutButton.Tag, true), "training/model shortcut should be active in training/model stage");
            AssertEqual(System.Windows.Visibility.Collapsed, savedLabelsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, aiCandidatesReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, guideToolsReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, classCatalogReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, yoloModelCenterReviewTab.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, workflowStageModelActionPanel.Visibility);
            window.ShellViewModel.SetWorkflowStage(WpfShellWorkflowStage.Dataset);
            window.UpdateLayout();
            AssertTrue(!window.ShellViewModel.IsModelWorkspaceActive, "leaving the model center should restore the labeling workspace");
            AssertEqual(System.Windows.Visibility.Visible, modelWorkspaceCanvasPanel.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, modelWorkspaceImageQueuePanel.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, rightWorkflowDockToggleButton.Visibility);
            AssertEqual(420D, modelWorkspaceImageQueueColumn.Width.Value);
            var currentDatasetContextBarElement = (System.Windows.FrameworkElement)window.FindName("CurrentDatasetContextBar");
            AssertTrue(currentDatasetContextBarElement != null, "WPF current dataset context bar was not created");
            AssertEqual(34D, currentDatasetContextBarElement.Height);
            AssertTrue(window.FindName("CurrentDatasetNameText") != null, "WPF current dataset name text was not created");
            AssertTrue(window.FindName("CurrentDatasetPurposeText") != null, "WPF current dataset purpose text was not created");
            var datasetStoragePathCardElement = (System.Windows.FrameworkElement)window.FindName("DatasetStoragePathCard");
            var datasetImageRootCardElement = (System.Windows.FrameworkElement)window.FindName("DatasetImageRootCard");
            var datasetSourceCardElement = (System.Windows.FrameworkElement)window.FindName("DatasetSourceCard");
            AssertTrue(datasetStoragePathCardElement != null, "WPF dataset storage path card was not created");
            AssertTrue(datasetImageRootCardElement != null, "WPF dataset image-root card was not created");
            AssertTrue(datasetSourceCardElement != null, "WPF dataset class/label source card was not created");
            AssertEqual(System.Windows.Visibility.Collapsed, datasetStoragePathCardElement.Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, datasetImageRootCardElement.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, datasetSourceCardElement.Visibility);
            AssertTrue(window.FindName("CurrentDatasetStoragePathText") != null, "WPF dataset storage path text was not created");
            AssertTrue(window.FindName("CurrentDatasetImageRootText") != null, "WPF dataset image-root text was not created");
            AssertTrue(window.FindName("CurrentDatasetSourceText") != null, "WPF dataset class/label source text was not created");
            AssertTrue(window.FindName("ChangeDatasetButton").GetType().FullName == "System.Windows.Controls.Button", "WPF change dataset button should use the standard command Button");
            AssertTrue(window.FindName("OpenDatasetFolderButton").GetType().FullName == "System.Windows.Controls.Button", "WPF open dataset folder button should use the standard command Button");
            AssertTrue(window.FindName("ChangeImageFolderButton").GetType().FullName == "System.Windows.Controls.Button", "WPF change image folder button should use the standard command Button");
            AssertTrue(ReferenceEquals(((System.Windows.Controls.Primitives.ButtonBase)window.FindName("ChangeDatasetButton")).Command, window.ShellViewModel.ChangeDatasetCommand), "WPF change dataset button should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(OpenVisionLab.Mvvm.Behaviors.InputCommandBehaviors.GetMouseClickInputCommand((System.Windows.DependencyObject)window.FindName("ChangeDatasetButton")), window.ShellViewModel.ChangeDatasetCommand), "WPF change dataset button should bind mouse-click input to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("DatasetHomeStageButton")), window.ShellViewModel.DatasetHomeCommand), "WPF dataset-home stage should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("LabelingWorkbenchStageButton")), window.ShellViewModel.LabelingWorkbenchCommand), "WPF labeling-workbench stage should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("InferenceReviewStageButton")), window.ShellViewModel.InferenceReviewCommand), "WPF inference-review stage should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("TrainingModelStageButton")), window.ShellViewModel.TrainingModelCenterCommand), "WPF training/model stage should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("RightWorkflowInferenceCandidatesButton")), window.ShellViewModel.InferenceReviewCommand), "WPF inference-candidates shortcut should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("RightWorkflowInferenceInspectButton")), window.ShellViewModel.DetectCurrentImageCommand), "WPF inference-inspect shortcut should bind to the injected detection command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("RightWorkflowTrainingModelButton")), window.ShellViewModel.TrainingModelCenterCommand), "WPF training/model shortcut should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("RightWorkflowTrainingReviewCandidateButton")), window.ShellViewModel.ReviewCandidateModelCommand), "WPF training candidate-review shortcut should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("RightWorkflowTrainingInspectButton")), window.ShellViewModel.DetectCurrentImageCommand), "WPF training inspect shortcut should bind to the injected detection command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("WorkflowStageReviewCandidateModelButton")), window.ShellViewModel.ReviewCandidateModelCommand), "WPF workflow-stage model-review button should bind to the injected shell command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("WorkflowStageSaveModelSettingsButton")), window.YoloModelSettingsViewModel.SaveSettingsCommand), "WPF workflow-stage save-model button should bind to the injected YOLO settings command at runtime");
            AssertTrue(ReferenceEquals(GetRuntimeButtonCommand(window.FindName("WorkflowStageInspectCurrentImageButton")), window.ShellViewModel.DetectCurrentImageCommand), "WPF workflow-stage current-inspection button should bind to the injected detection command at runtime");
            AssertTrue(window.FindName("ImageQueuePanelControl") != null, "WPF image queue user control was not created");
            AssertTrue(window.FindName("ImageQueuePanelControl").GetType().FullName == "MvcVisionSystem.WpfImageQueuePanel", "WPF image queue should be hosted by a UserControl");
            AssertTrue(((WpfImageQueuePanel)window.FindName("ImageQueuePanelControl")).ViewModel != null, "WPF image queue view model was not created");
            AssertTrue(window.FindName("DetectSelectedQueueButton") != null, "WPF queue selected detect button was not created");
            AssertTrue(window.FindName("BatchDetectQueueButton") != null, "WPF queue batch detect button was not created");
            AssertTrue(window.FindName("TemplateBatchQueueButton") != null, "WPF queue template-batch button was not created");
            AssertTrue(window.FindName("RetryFailedQueueButton") != null, "WPF queue retry button was not created");
            AssertTrue(window.FindName("StopBatchQueueButton") != null, "WPF queue stop button was not created");
            var imageQueueGrid = window.FindName("ImageQueueGrid") as System.Windows.Controls.DataGrid;
            AssertTrue(imageQueueGrid != null, "WPF image queue grid was not created");
            AssertTrue(imageQueueGrid.Columns[0] is System.Windows.Controls.DataGridTemplateColumn, "WPF image queue file column should use icon/detail template");
            string imageQueueXaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "0. UI", "9) WPF", "Views", "WpfImageQueuePanel.xaml"));
            AssertTrue(imageQueueXaml.Contains("EnableRowVirtualization=\"True\"", StringComparison.Ordinal), "WPF image queue should keep row virtualization enabled for large folders");
            AssertTrue(imageQueueXaml.Contains("VirtualizingPanel.VirtualizationMode=\"Recycling\"", StringComparison.Ordinal), "WPF image queue should recycle row containers for large folders");
            AssertTrue(imageQueueXaml.Contains("x:Name=\"TemplateBatchQueueText\"", StringComparison.Ordinal), "WPF image queue should show visible text for template batch instead of icon-only discovery");
            AssertTrue(imageQueueXaml.Contains("전체 자동 저장", StringComparison.Ordinal), "WPF image queue template batch action should expose its batch auto-save meaning");
            AssertTrue(window.FindName("StatusBarPanelControl") != null, "WPF status bar user control was not created");
            AssertTrue(window.FindName("StatusBarPanelControl").GetType().FullName == "MvcVisionSystem.WpfStatusBarPanel", "WPF status bar should be hosted by a UserControl");
            AssertTrue(((WpfStatusBarPanel)window.FindName("StatusBarPanelControl")).ViewModel != null, "WPF status bar view model was not created");
            AssertTrue(window.FindName("DatasetStatusText") != null, "WPF dataset status text was not created");
            AssertTrue(window.FindName("PythonStatusText") != null, "WPF python status text was not created");
            AssertTrue(window.FindName("AnnotationSaveStatusText") != null, "WPF annotation save status text was not created");
            AssertTrue(window.FindName("ModelStatusText") != null, "WPF model status text was not created");
            AssertTrue(window.FindName("ShellLogPanelControl") != null, "WPF log user control was not created");
            AssertTrue(window.FindName("ShellLogPanelControl").GetType().FullName == "MvcVisionSystem.WpfShellLogPanel", "WPF log should be hosted by a UserControl");
            AssertTrue(((WpfShellLogPanel)window.FindName("ShellLogPanelControl")).ViewModel != null, "WPF log view model was not created");
            AssertTrue(window.FindName("ShellLogPanel") != null, "WPF log panel was not created");
            AssertTrue(window.FindName("ShellLogPanel").GetType().FullName == "OpenVisionLab.Logging.Controls.View.LogPanelView", "WPF shell should use the OpenVisionLab logging WPF panel");
            AssertTrue(window.FindName("ObjectReviewSummaryText") != null, "WPF object review summary was not created");
            AssertTrue(window.FindName("ObjectReviewPanelControl") != null, "WPF object review user control was not created");
            AssertTrue(window.FindName("ObjectReviewPanelControl").GetType().FullName == "MvcVisionSystem.WpfObjectReviewPanel", "WPF object review should be hosted by a UserControl");
            var objectReviewPanel = (WpfObjectReviewPanel)window.FindName("ObjectReviewPanelControl");
            AssertTrue(objectReviewPanel.ViewModel != null, "WPF object review view model was not created");
            AssertTrue(objectReviewPanel.FindName("ObjectReviewModePanel") != null, "WPF object review mode panel was not created");
            AssertEqual("\uC800\uC7A5 \uB77C\uBCA8", ((System.Windows.Controls.TabItem)window.FindName("ObjectsReviewTab")).Header?.ToString());
            AssertTrue(window.FindName("DeleteObjectButton") != null, "WPF object delete button was not created");
            AssertTrue(window.FindName("DeleteObjectButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF object delete button should use WPF-UI button");
            AssertTrue(window.FindName("ObjectClassBox") != null, "WPF object class selector was not created");
            AssertTrue(window.FindName("ApplyObjectClassButton") != null, "WPF object class apply button was not created");
            AssertTrue(window.FindName("ApplyObjectClassButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF object class apply button should use WPF-UI button");
            AssertTrue(window.FindName("DetectionResultOverlay") != null, "WPF detection result overlay was not created");
            AssertTrue(window.FindName("CanvasPanelControl") != null, "WPF canvas user control was not created");
            AssertTrue(window.FindName("CanvasPanelControl").GetType().FullName == "MvcVisionSystem.WpfCanvasPanel", "WPF canvas should be hosted by a UserControl");
            AssertTrue(((WpfCanvasPanel)window.FindName("CanvasPanelControl")).ViewModel != null, "WPF canvas panel view model was not created");
            AssertTrue(window.FindName("CanvasSaveAnnotationButton") != null, "WPF canvas local annotation save button was not created");
            AssertTrue(window.FindName("CanvasSaveAnnotationButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF canvas local save button should use WPF-UI button");
            AssertTrue(window.FindName("DetectionOverlaySummaryText") != null, "WPF detection summary text was not created");
            AssertTrue(window.FindName("QueuePreviewImage") == null, "WPF queue preview image should be removed from the one-click canvas workflow");
            AssertTrue(window.FindName("QueuePreviewText") == null, "WPF queue preview text should be removed from the one-click canvas workflow");
            AssertTrue(window.FindName("ClassCatalogPanelControl") != null, "WPF class catalog user control was not created");
            AssertTrue(window.FindName("ClassCatalogPanelControl").GetType().FullName == "MvcVisionSystem.WpfClassCatalogPanel", "WPF class catalog should be hosted by a UserControl");
            AssertTrue(((WpfClassCatalogPanel)window.FindName("ClassCatalogPanelControl")).ViewModel != null, "WPF class catalog view model was not created");
            AssertTrue(window.FindName("ClassNameBox") != null, "WPF class name editor was not created");
            AssertTrue(window.FindName("AddClassButton") != null, "WPF class add button was not created");
            AssertTrue(window.FindName("RenameClassButton") != null, "WPF class rename button was not created");
            AssertTrue(window.FindName("RemoveClassButton") != null, "WPF class remove button was not created");
            AssertTrue(window.FindName("ClassColorBox") != null, "WPF class color selector was not created");
            AssertTrue(window.FindName("ApplyClassColorButton") != null, "WPF class color apply button was not created");
            AssertTrue(window.FindName("OutputRootPathBox") == null, "WPF class catalog should not create an output root path editor");
            AssertTrue(window.FindName("BrowseOutputRootButton") == null, "WPF class catalog should not create an output root browse button");
            AssertTrue(window.FindName("SaveOutputRootButton") == null, "WPF class catalog should not create an output root save button");
            AssertTrue(window.FindName("SaveAnnotationsButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF save annotations button should use WPF-UI button");
            AssertTrue(window.FindName("DetectButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF detect button should use WPF-UI button");
            AssertTrue(window.FindName("ConfirmSelectedCandidateButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF selected confirm button should use WPF-UI button");
            AssertTrue(window.FindName("DetectSelectedQueueButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF queue selected detect button should use WPF-UI button");
            AssertTrue(window.FindName("BatchDetectQueueButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF queue batch detect button should use WPF-UI button");
            AssertTrue(window.FindName("RetryFailedQueueButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF queue retry button should use WPF-UI button");
            AssertTrue(window.FindName("StopBatchQueueButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF queue stop button should use WPF-UI button");
            AssertTrue(window.FindName("FirstCheckYoloButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO first-check button should use WPF-UI button");
            AssertTrue(window.FindName("InstallRequirementsButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO install button should use WPF-UI button");
            AssertTrue(window.FindName("RunYoloSmokeButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO test button should use WPF-UI button");
            AssertTrue(window.FindName("RestartPythonWorkerButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO restart button should use WPF-UI button");
            AssertTrue(window.FindName("StopPythonWorkerButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO stop button should use WPF-UI button");
            AssertTrue(window.FindName("SaveYoloSettingsButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO settings save button should use WPF-UI button");
            AssertTrue(window.FindName("ResetYoloSettingsButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO settings reset button should use WPF-UI button");
            AssertTrue(window.FindName("RefreshTrainingReadinessButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF training readiness button should use WPF-UI button");
            AssertTrue(window.FindName("StartTrainingButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF training start button should use WPF-UI button");
            AssertTrue(window.FindName("StopTrainingButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF training stop button should use WPF-UI button");
            AssertTrue(!string.IsNullOrWhiteSpace(((System.Windows.Controls.ContentControl)window.FindName("RefreshTrainingReadinessButton")).Content?.ToString()), "WPF training refresh button should have visible text");
            AssertTrue(!string.IsNullOrWhiteSpace(((System.Windows.Controls.ContentControl)window.FindName("StartTrainingButton")).Content?.ToString()), "WPF training start button should have visible text");
            AssertTrue(!string.IsNullOrWhiteSpace(((System.Windows.Controls.ContentControl)window.FindName("StopTrainingButton")).Content?.ToString()), "WPF training stop button should have visible text");
            AssertTrue(window.FindName("AddClassButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF class add button should use WPF-UI button");
            AssertTrue(window.FindName("RenameClassButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF class rename button should use WPF-UI button");
            AssertTrue(window.FindName("RemoveClassButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF class remove button should use WPF-UI button");
            AssertTrue(window.FindName("ApplyClassColorButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF class color apply button should use WPF-UI button");
            AssertTrue(window.FindName("BrowseOutputRootButton") == null, "WPF class catalog should keep dataset-storage browse out of the class schema panel");
            AssertTrue(window.FindName("SaveOutputRootButton") == null, "WPF class catalog should keep dataset-storage save out of the class schema panel");
            AssertTrue(window.FindName("BrowseYoloPythonButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO python browse button should use WPF-UI button");
            AssertTrue(window.FindName("BrowseYoloProjectRootButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO project browse button should use WPF-UI button");
            AssertTrue(window.FindName("BrowseYoloClientScriptButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO client browse button should use WPF-UI button");
            AssertTrue(window.FindName("BrowseYoloWeightsButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO weights browse button should use WPF-UI button");
            AssertTrue(window.FindName("BrowseYoloImageRootButton").GetType().FullName == "Wpf.Ui.Controls.Button", "WPF YOLO image root browse button should use WPF-UI button");

            window.FocusYoloSettingsTab();
            AssertTrue(window.FindName("YoloSettingsReviewTab") is System.Windows.Controls.TabItem yoloSettingsTab && yoloSettingsTab.IsSelected, "WPF shell should focus the model settings tab");
            AssertTrue(ReferenceEquals(modelCenterOverviewTaskTab, modelCenterTaskTabs.SelectedItem), "WPF model center focus should return to the overview task");
            AssertEqual(System.Windows.Visibility.Visible, ((System.Windows.FrameworkElement)window.FindName("YoloModelLifecycleDashboardPanel")).Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, ((System.Windows.FrameworkElement)window.FindName("YoloDatasetReadinessQuickPanel")).Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, ((System.Windows.FrameworkElement)window.FindName("TrainingSettingsPanelControl")).Visibility);
            AssertEqual(System.Windows.Visibility.Collapsed, ((System.Windows.FrameworkElement)window.FindName("YoloModelSettingsPanelControl")).Visibility);
            AssertTrue(!((WpfYoloStatusPanel)window.FindName("YoloStatusPanelControl")).RuntimeDetailsExpander.IsExpanded, "WPF model overview should keep runtime details collapsed by default");
            AssertTrue(!((WpfProjectConfigPanel)window.FindName("ProjectConfigPanelControl")).SettingsExpander.IsExpanded, "WPF model overview should keep project settings collapsed by default");
            AssertTrue(!((WpfYoloModelSettingsPanel)window.FindName("YoloModelSettingsPanelControl")).SettingsExpander.IsExpanded, "WPF model overview should keep model settings collapsed by default");
            AssertTrue(!((WpfTrainingSettingsPanel)window.FindName("TrainingSettingsPanelControl")).SettingsExpander.IsExpanded, "WPF model overview should keep training settings collapsed by default");

            modelCenterTaskTabs.SelectedItem = modelCenterDataTaskTab;
            window.UpdateLayout();
            AssertEqual(System.Windows.Visibility.Visible, ((System.Windows.FrameworkElement)window.FindName("YoloDatasetReadinessQuickPanel")).Visibility);
            AssertEqual(System.Windows.Visibility.Visible, ((System.Windows.FrameworkElement)window.FindName("ProjectConfigPanelControl")).Visibility);
            AssertTrue(((System.Windows.Controls.Expander)window.FindName("YoloDatasetReadinessQuickPanel")).IsExpanded,
                "selecting the data task should reveal its readiness controls without another click");
            var evaluationPurposeText = window.FindName("YoloEvaluationDataPurposeText") as System.Windows.Controls.TextBlock;
            var evaluationReadinessText = window.FindName("YoloDatasetQuickReadinessText") as System.Windows.Controls.TextBlock;
            var evaluationLimitText = window.FindName("YoloExternalEvaluationAuditLimitText") as System.Windows.Controls.TextBlock;
            var externalYoloDatasetStatusText = window.FindName("YoloExternalYoloDatasetStatusText") as System.Windows.Controls.TextBlock;
            AssertTrue(evaluationPurposeText != null && !string.IsNullOrWhiteSpace(evaluationPurposeText.Text),
                "data task should show the active dataset purpose next to its evidence");
            AssertTrue(evaluationReadinessText != null
                && System.Windows.Data.BindingOperations.GetBindingExpressionBase(
                    evaluationReadinessText,
                    System.Windows.Controls.TextBlock.TextProperty) != null,
                "data task should bind the saved dataset readiness evidence");
            AssertTrue(evaluationLimitText != null
                && evaluationLimitText.Text.Contains("SHA-256", StringComparison.Ordinal)
                && evaluationLimitText.Text.Contains("\uBAA8\uB378 \uCC44\uD0DD", StringComparison.Ordinal),
                "data task should state that an independent image folder is not model-adoption evidence");
            AssertTrue(externalYoloDatasetStatusText != null
                && externalYoloDatasetStatusText.Text.Contains("data.yaml", StringComparison.Ordinal),
                "data task should state the separate native YOLO data.yaml intake state");
            AssertTrue(window.LearningWorkflowViewModel.ExternalEvaluationDataAuditCommand != null,
                "external evaluation audit should be configured on the learning workflow ViewModel");
            AssertTrue(ReferenceEquals(
                    GetRuntimeButtonCommand(window.FindName("YoloExternalEvaluationAuditButton")),
                    window.LearningWorkflowViewModel.ExternalEvaluationDataAuditCommand),
                "data task should expose the learning-workflow external evaluation audit command");
            AssertEqual(2, window.LearningWorkflowViewModel.ExternalYoloDatasetPurposeModes.Count);
            AssertTrue(window.LearningWorkflowViewModel.SelectExternalYoloDatasetCommand != null
                && window.LearningWorkflowViewModel.ActivateExternalYoloDatasetCommand != null
                && window.LearningWorkflowViewModel.ClearExternalYoloDatasetCommand != null,
                "external YOLO data.yaml actions should be configured on the learning workflow ViewModel");
            AssertTrue(ReferenceEquals(
                    GetRuntimeButtonCommand(window.FindName("YoloExternalYoloDatasetSelectButton")),
                    window.LearningWorkflowViewModel.SelectExternalYoloDatasetCommand),
                "data task should expose the external YOLO data.yaml selection command");
            AssertTrue(ReferenceEquals(
                    GetRuntimeButtonCommand(window.FindName("YoloExternalYoloDatasetActivateButton")),
                    window.LearningWorkflowViewModel.ActivateExternalYoloDatasetCommand),
                "data task should expose the explicit external YOLO next-training command");

            InvokePrivate(window, "FocusYoloTrainingSettingsTab");
            window.UpdateLayout();
            AssertTrue(ReferenceEquals(modelCenterTrainingTaskTab, modelCenterTaskTabs.SelectedItem), "training navigation should select the training/comparison task");
            AssertEqual(System.Windows.Visibility.Visible, ((System.Windows.FrameworkElement)window.FindName("TrainingSettingsPanelControl")).Visibility);
            AssertEqual(System.Windows.Visibility.Visible, modelBenchmarkEntryPanel.Visibility);
            AssertTrue(((WpfTrainingSettingsPanel)window.FindName("TrainingSettingsPanelControl")).SettingsExpander.IsExpanded,
                "selecting the training task should reveal training controls without another click");

            InvokePrivate(window, "FocusYoloModelSettingsTab");
            window.UpdateLayout();
            AssertTrue(ReferenceEquals(modelCenterRuntimeTaskTab, modelCenterTaskTabs.SelectedItem), "model settings navigation should select the runtime task");
            AssertEqual(System.Windows.Visibility.Collapsed, modelBenchmarkEntryPanel.Visibility);
            AssertEqual(System.Windows.Visibility.Visible, ((System.Windows.FrameworkElement)window.FindName("YoloStatusPanelControl")).Visibility);
            AssertEqual(System.Windows.Visibility.Visible, ((System.Windows.FrameworkElement)window.FindName("YoloModelSettingsPanelControl")).Visibility);
            AssertTrue(((WpfYoloModelSettingsPanel)window.FindName("YoloModelSettingsPanelControl")).SettingsExpander.IsExpanded,
                "selecting the runtime task should reveal model controls without another click");

            window.FocusYoloSettingsTab();

            window.FocusClassCatalogTab();
            AssertTrue(window.FindName("ClassesReviewTab") is System.Windows.Controls.TabItem classesReviewTab && classesReviewTab.IsSelected, "WPF shell should focus the class catalog tab");
            AssertTrue(window.FindName("ClassCatalogPanelControl") is WpfClassCatalogPanel, "WPF class catalog should be visible through the shell focus method");
        }
        finally
        {
            window.Close();
        }
    }

}
