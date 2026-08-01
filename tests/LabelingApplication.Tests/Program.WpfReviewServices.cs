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
using System.Drawing.Imaging;
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

internal static class ReviewServicesTests
{
    internal static void TestWpfPatchCoreHeatmapReviewService()
    {
        string panelPath = Path.Combine(FindRepositoryRoot(), "0. UI", "9) WPF", "Views", "WpfCandidateReviewPanel.xaml");
        string panelSource = File.ReadAllText(panelPath);
        string windowPath = Path.Combine(FindRepositoryRoot(), "0. UI", "9) WPF", "Views", "WpfPatchCoreHeatmapWindow.xaml");
        string windowSource = File.ReadAllText(windowPath);
        AssertTrue(panelSource.Contains("x:Name=\"TogglePatchCoreHeatmapButton\"", StringComparison.Ordinal), "candidate review should declare a dedicated PatchCore heatmap action");
        AssertTrue(panelSource.Contains("Visibility=\"{Binding PatchCoreHeatmapVisibility}\"", StringComparison.Ordinal), "non-PatchCore candidates should not show the heatmap surface");
        AssertTrue(panelSource.Contains("Command=\"{Binding TogglePatchCoreHeatmapCommand}\"", StringComparison.Ordinal), "heatmap preview should require an explicit ViewModel command");
        AssertTrue(windowSource.Contains("x:Name=\"PatchCoreHeatmapPreviewImage\"", StringComparison.Ordinal), "explicit heatmap action should open a dedicated evidence window without changing panel layout");
        AssertTrue(windowSource.Contains("Source=\"{Binding PatchCoreHeatmapSource}\"", StringComparison.Ordinal), "heatmap image should bind to ViewModel-owned evidence state");
        AssertTrue(windowSource.Contains("WindowStartupLocation=\"CenterOwner\"", StringComparison.Ordinal), "heatmap evidence window should inherit the placed parent monitor");
        AssertTrue(windowSource.Contains("Background=\"{DynamicResource PanelBrush}\"", StringComparison.Ordinal), "heatmap evidence should use semantic theme resources");
        AssertTrue(windowSource.Contains("<Trigger Property=\"IsMouseOver\" Value=\"True\">", StringComparison.Ordinal), "heatmap window action should define a themed hover state");
        AssertTrue(windowSource.Contains("<Trigger Property=\"IsPressed\" Value=\"True\">", StringComparison.Ordinal), "heatmap window action should define a themed pressed state");
        AssertTrue(windowSource.Contains("<Trigger Property=\"IsKeyboardFocusWithin\" Value=\"True\">", StringComparison.Ordinal), "heatmap window action should define a themed keyboard-focus state");
        AssertTrue(windowSource.Contains("<Trigger Property=\"IsEnabled\" Value=\"False\">", StringComparison.Ordinal), "heatmap window action should define a themed disabled state");
        AssertTrue(panelSource.Contains("<Trigger Property=\"IsMouseOver\" Value=\"True\">", StringComparison.Ordinal), "candidate action style should define a themed hover state");
        AssertTrue(panelSource.Contains("<Trigger Property=\"IsPressed\" Value=\"True\">", StringComparison.Ordinal), "candidate action style should define a themed pressed state");
        AssertTrue(panelSource.Contains("<Trigger Property=\"IsKeyboardFocusWithin\" Value=\"True\">", StringComparison.Ordinal), "candidate action style should define a themed keyboard-focus state");

        string root = CreateTempRoot();
        try
        {
            string heatmapPath = Path.Combine(root, "patchcore-heatmap.png");
            using (var bitmap = new Bitmap(16, 16))
            {
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Navy);
                graphics.FillRectangle(Brushes.Red, 5, 4, 7, 8);
                bitmap.Save(heatmapPath, ImageFormat.Png);
            }

            var candidate = new YoloWorkerSmokeCandidate
            {
                Index = 1,
                ClassName = "Defect",
                PredictionType = "patchcore",
                ImageLevel = true,
                HeatmapPath = heatmapPath
            };
            var service = new WpfPatchCoreHeatmapReviewService();
            string originalClassName = candidate.ClassName;
            string originalPredictionType = candidate.PredictionType;
            WpfPatchCoreHeatmapAvailability availability = service.Inspect(candidate);
            AssertTrue(availability.IsPatchCoreCandidate, "PatchCore candidate should expose the heatmap review surface");
            AssertTrue(availability.CanOpen, "existing PatchCore heatmap should be explicitly openable");
            AssertEqual(Path.GetFullPath(heatmapPath), availability.FullPath);

            var viewModel = new WpfCandidateReviewPanelViewModel();
            viewModel.SetPatchCoreHeatmapAvailability(availability);
            AssertEqual(System.Windows.Visibility.Visible, viewModel.PatchCoreHeatmapVisibility);
            AssertEqual(System.Windows.Visibility.Collapsed, viewModel.PatchCoreHeatmapPreviewVisibility);
            AssertTrue(viewModel.PatchCoreHeatmapSource == null, "selection should inspect metadata without decoding or opening the heatmap");
            AssertTrue(!viewModel.IsPatchCoreHeatmapOpen, "selection should not auto-open the heatmap");
            AssertEqual(originalClassName, candidate.ClassName);
            AssertEqual(originalPredictionType, candidate.PredictionType);

            WpfPatchCoreHeatmapLoadResult loaded = service.Load(candidate);
            AssertTrue(loaded.Succeeded && loaded.ImageSource != null, "explicit heatmap load should decode a valid image");
            viewModel.ShowPatchCoreHeatmap(loaded);
            AssertEqual(System.Windows.Visibility.Visible, viewModel.PatchCoreHeatmapPreviewVisibility);
            AssertTrue(viewModel.IsPatchCoreHeatmapOpen, "explicit open should show the heatmap");
            AssertTrue(viewModel.PatchCoreHeatmapActionText.Contains("닫기", StringComparison.Ordinal), "open heatmap should expose an explicit close action");

            string movedPath = Path.Combine(root, "moved-after-load.png");
            File.Move(heatmapPath, movedPath);
            AssertTrue(File.Exists(movedPath), "OnLoad heatmap decoding should not retain a file lock");
            viewModel.ClosePatchCoreHeatmap();
            AssertTrue(viewModel.PatchCoreHeatmapSource == null, "close should release the preview source from the ViewModel");
            AssertEqual(System.Windows.Visibility.Collapsed, viewModel.PatchCoreHeatmapPreviewVisibility);

            WpfPatchCoreHeatmapAvailability stale = service.Inspect(candidate);
            AssertTrue(stale.IsPatchCoreCandidate && !stale.CanOpen, "moved heatmap should become a visible unavailable state");
            AssertTrue(stale.StatusText.Contains("찾을 수 없습니다", StringComparison.Ordinal), "missing heatmap should provide recovery guidance");
            viewModel.SetPatchCoreHeatmapAvailability(stale);
            AssertTrue(!viewModel.IsPatchCoreHeatmapActionEnabled, "missing heatmap should disable the open action");

            string corruptPath = Path.Combine(root, "corrupt.png");
            File.WriteAllText(corruptPath, "not an image");
            candidate.HeatmapPath = corruptPath;
            WpfPatchCoreHeatmapLoadResult corrupt = service.Load(candidate);
            AssertTrue(!corrupt.Succeeded, "corrupt heatmap should fail closed");
            AssertTrue(corrupt.StatusText.Contains("읽을 수 없습니다", StringComparison.Ordinal), "corrupt heatmap should explain the decode failure");

            var ordinaryCandidate = new YoloWorkerSmokeCandidate
            {
                PredictionType = "detect",
                HeatmapPath = movedPath
            };
            viewModel.SetPatchCoreHeatmapAvailability(service.Inspect(ordinaryCandidate));
            AssertEqual(System.Windows.Visibility.Collapsed, viewModel.PatchCoreHeatmapVisibility);
            AssertTrue(!viewModel.IsPatchCoreHeatmapOpen, "candidate change should close stale PatchCore evidence");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestWpfObjectReviewSelectionService()
    {
        var overlayIds = new List<string> { "roi-a", "roi-b" };
        var selectedObject = new WpfObjectReviewListItem(
            "2. Defect / Box",
            string.Empty,
            WpfObjectReviewSource.ManualRoi.ToString(),
            0,
            WpfObjectReviewItemRef.Manual(0, "roi-b"));

        AssertTrue(
            WpfObjectReviewSelectionService.TryResolveSelectedItem(selectedObject, overlayIds, 2, out WpfObjectReviewItemRef resolved),
            "selected object should resolve from the stable overlay id");
        AssertEqual(1, resolved.Index);
        AssertEqual("roi-b", resolved.SourceId);
        AssertTrue(WpfObjectReviewSelectionService.IsSource(selectedObject, WpfObjectReviewSource.ManualRoi), "source helper should recognize manual ROI rows");
        AssertEqual(0, WpfObjectReviewSelectionService.GetSelectedRowIndex(new List<WpfObjectReviewListItem> { selectedObject }, selectedObject));
        AssertEqual(1, WpfObjectReviewSelectionService.ResolveManualRoiIndex(WpfObjectReviewItemRef.Manual(1, "missing"), overlayIds, 2));
        AssertEqual(-1, WpfObjectReviewSelectionService.ResolveManualRoiIndex(WpfObjectReviewItemRef.Manual(4, "missing"), overlayIds, 2));
        AssertEqual("roi-a", WpfObjectReviewSelectionService.GetManualRoiOverlayId(overlayIds, 0));
        AssertEqual(string.Empty, WpfObjectReviewSelectionService.GetManualRoiOverlayId(overlayIds, 5));

        var rows = new List<WpfObjectReviewListItem>
        {
            new WpfObjectReviewListItem("1. row", string.Empty, WpfObjectReviewSource.ManualRoi.ToString(), 0, WpfObjectReviewItemRef.Manual(0)),
            new WpfObjectReviewListItem("2. row", string.Empty, WpfObjectReviewSource.ManualRoi.ToString(), 1, WpfObjectReviewItemRef.Manual(1))
        };
        AssertTrue(WpfObjectReviewSelectionService.CanReplaceManualRoiRow(rows, 1, 2), "single-row ROI refresh should validate matching row identity");
        AssertTrue(
            WpfObjectReviewSelectionService.ShouldUseIncrementalDelete(WpfObjectReviewSource.ManualRoi, 500000, 10000, 250000, 500001),
            "large manual ROI delete should stay incremental");
        AssertTrue(
            WpfObjectReviewSelectionService.ShouldUseIncrementalDelete(WpfObjectReviewSource.ManualRoi, 2, 10000, 1, 3),
            "small manual ROI delete should also stay incremental");
        AssertTrue(
            !WpfObjectReviewSelectionService.ShouldUseIncrementalDelete(WpfObjectReviewSource.ManualSegment, 500000, 10000, 250000, 500001),
            "non-ROI delete should use the full refresh path");
        AssertEqual(250000, WpfObjectReviewSelectionService.GetSelectionIndexAfterDelete(250000, 500000));
        AssertEqual(499999, WpfObjectReviewSelectionService.GetSelectionIndexAfterDelete(500000, 500000));
        AssertEqual(-1, WpfObjectReviewSelectionService.GetSelectionIndexAfterDelete(0, 0));
    }


    internal static void TestWpfObjectReviewPresentationService()
    {
        var manualRois = new List<Rectangle> { new Rectangle(1, 2, 30, 40) };
        var manualClasses = new List<string> { "Scratch" };
        var manualShapes = new List<CanvasRoiShapeKind> { CanvasRoiShapeKind.Ellipse };
        var overlayIds = new List<string> { "roi-1" };
        var manualSegments = new List<LabelingSegmentationObject>
        {
            new LabelingSegmentationObject(
                new[] { new Point(3, 3), new Point(8, 3), new Point(8, 9) },
                new CClassItem { Text = "Poly" })
        };
        var confirmed = new List<YoloWorkerSmokeCandidate>
        {
            new YoloWorkerSmokeCandidate { Index = 7, ClassName = "AIClass", Confidence = 0.88, X = 9, Y = 10, Width = 11, Height = 12 }
        };
        var service = new WpfObjectReviewPresentationService();
        WpfObjectReviewItemRef preferred = WpfObjectReviewItemRef.Manual(0, "roi-1");

        WpfObjectReviewListPresentation presentation = service.BuildListPresentation(
            manualRois,
            manualClasses,
            manualShapes,
            overlayIds,
            manualSegments,
            confirmed,
            preferred,
            null,
            candidate => new Rectangle((int)candidate.X, (int)candidate.Y, (int)candidate.Width, (int)candidate.Height),
            candidate => "detail-" + candidate.ClassName);

        AssertEqual(3, presentation.Rows.Count);
        AssertTrue(presentation.Summary.Contains("3", StringComparison.Ordinal), "object review presentation should summarize all object sources");
        AssertTrue(ReferenceEquals(preferred, presentation.SelectedItem), "object review presentation should preserve the preferred selected row");
        AssertTrue(presentation.Rows[0].Content.Contains("Scratch", StringComparison.Ordinal), "manual ROI row should include its class name");
        AssertTrue(presentation.Rows[0].Content.Contains("\uD0C0\uC6D0", StringComparison.Ordinal), "manual ROI row should include the ROI shape name");
        AssertTrue(((WpfObjectReviewItemRef)presentation.Rows[0].Payload).SourceId == "roi-1", "manual ROI row should keep the stable overlay id");
        AssertTrue(presentation.Rows[1].Content.Contains("Poly", StringComparison.Ordinal), "manual segment row should include its class name");
        AssertTrue(presentation.Rows[2].Content.Contains("\uD655\uC815 \uB77C\uBCA8 7", StringComparison.Ordinal), "confirmed AI row should present as a committed label while keeping the candidate display index");
        AssertTrue(presentation.Rows[2].ToolTip.Contains("AI \uD6C4\uBCF4 \uD655\uC815", StringComparison.Ordinal), "confirmed AI row should keep the source visible without looking like a pending AI candidate");
        AssertTrue(presentation.Rows[2].ToolTip.Contains("\uC800\uC7A5 \uB77C\uBCA8\uB85C \uBC18\uC601\uB428", StringComparison.Ordinal), "confirmed AI row should state that it is already reflected as a saved label");
        AssertTrue(presentation.Rows[2].ToolTip.Contains("detail-AIClass", StringComparison.Ordinal), "confirmed AI row should use the provided candidate detail text");

        WpfObjectReviewListPresentation empty = service.BuildListPresentation(
            Array.Empty<Rectangle>(),
            Array.Empty<string>(),
            Array.Empty<CanvasRoiShapeKind>(),
            Array.Empty<string>(),
            Array.Empty<LabelingSegmentationObject>(),
            Array.Empty<YoloWorkerSmokeCandidate>(),
            null,
            WpfObjectReviewItemRef.ConfirmedAi(0),
            null,
            null);
        AssertEqual(1, empty.Rows.Count);
        AssertTrue(!empty.Rows[0].IsEnabled, "empty object presentation should expose a disabled row");

        WpfObjectReviewListItem rebuiltManual = service.BuildManualRoiItem(manualRois, manualClasses, manualShapes, overlayIds, 0);
        AssertTrue(rebuiltManual.Content.Contains("Scratch", StringComparison.Ordinal), "single manual ROI row rebuild should use the same presentation rules");
        AssertTrue(service.BuildManualRoiItem(manualRois, manualClasses, manualShapes, overlayIds, 99) == null, "invalid manual ROI row rebuild should fail safely");

        WpfObjectReviewDeleteRefreshPlan incremental = service.BuildDeleteRefreshPlan(
            WpfObjectReviewSource.ManualRoi,
            500000,
            10000,
            250000,
            500001);
        AssertTrue(incremental.UseIncremental, "large manual ROI delete should use the incremental side-list path");
        AssertEqual(250000, incremental.SelectedRowIndex);
        AssertTrue(incremental.Summary.Contains("500000", StringComparison.Ordinal), "incremental delete plan should expose the updated object count");

        WpfObjectReviewDeleteRefreshPlan smallIncremental = service.BuildDeleteRefreshPlan(
            WpfObjectReviewSource.ManualRoi,
            2,
            10000,
            1,
            3);
        AssertTrue(smallIncremental.UseIncremental, "small manual ROI delete should use the same single-row side-list path");
        AssertEqual(1, smallIncremental.SelectedRowIndex);

        WpfObjectReviewDeleteRefreshPlan fullRefresh = service.BuildDeleteRefreshPlan(
            WpfObjectReviewSource.ManualSegment,
            500000,
            10000,
            250000,
            500001);
        AssertTrue(!fullRefresh.UseIncremental, "non-ROI deletes should still use the full refresh path");
        AssertEqual(-1, fullRefresh.SelectedRowIndex);
    }
    internal static void TestWpfCandidateReviewSelectionService()
    {
        var first = new YoloWorkerSmokeCandidate { ClassName = "A", Confidence = 0.90, X = 1, Y = 2, Width = 3, Height = 4 };
        var second = new YoloWorkerSmokeCandidate { ClassName = "B", Confidence = 0.91, X = 5, Y = 6, Width = 7, Height = 8 };
        var third = new YoloWorkerSmokeCandidate { ClassName = "C", Confidence = 0.92, X = 9, Y = 10, Width = 11, Height = 12 };
        var rows = new List<WpfCandidateReviewListItem>
        {
            WpfCandidateReviewListItem.Empty("empty", string.Empty),
            CreateCandidateReviewTestItem("1", first),
            CreateCandidateReviewTestItem("2", second),
            CreateCandidateReviewTestItem("3", third)
        };

        AssertTrue(
            ReferenceEquals(first, WpfCandidateReviewSelectionService.GetSelectedCandidate(rows[1])),
            "candidate selection service should unwrap the selected row payload");

        WpfCandidateNavigationSelection next = WpfCandidateReviewSelectionService.SelectCandidateOffset(rows, rows[1], 1);
        AssertEqual(WpfCandidateNavigationStatus.Selected, next.Status);
        AssertTrue(ReferenceEquals(rows[2], next.SelectedItem), "candidate navigation should move to the next candidate row");

        WpfCandidateNavigationSelection previous = WpfCandidateReviewSelectionService.SelectCandidateOffset(rows, rows[1], -1);
        AssertEqual(WpfCandidateNavigationStatus.Selected, previous.Status);
        AssertTrue(ReferenceEquals(rows[3], previous.SelectedItem), "candidate navigation should wrap to the last candidate row");

        WpfCandidateNavigationSelection missingSelection = WpfCandidateReviewSelectionService.SelectCandidateOffset(rows, null, 1);
        AssertEqual(WpfCandidateNavigationStatus.Selected, missingSelection.Status);
        AssertTrue(ReferenceEquals(rows[1], missingSelection.SelectedItem), "candidate navigation should recover from a missing selected row");

        WpfCandidateNavigationSelection emptySelection = WpfCandidateReviewSelectionService.SelectCandidateOffset(Array.Empty<WpfCandidateReviewListItem>(), null, 1);
        AssertEqual(WpfCandidateNavigationStatus.NoCandidates, emptySelection.Status);

        WpfCandidateNavigationSelection singleSelection = WpfCandidateReviewSelectionService.SelectCandidateOffset(new[] { rows[1] }, rows[1], 1);
        AssertEqual(WpfCandidateNavigationStatus.SingleCandidate, singleSelection.Status);
        AssertTrue(ReferenceEquals(rows[1], singleSelection.SelectedItem), "single-row candidate navigation should keep the existing candidate selected");

        AssertTrue(
            ReferenceEquals(
                second,
                WpfCandidateReviewSelectionService.FindNextVisibleCandidateAfter(new[] { first, second, third }, first, new[] { first })),
            "confirming or skipping the first candidate should select the next visible candidate");
        AssertTrue(
            ReferenceEquals(
                third,
                WpfCandidateReviewSelectionService.FindNextVisibleCandidateAfter(new[] { first, second, third }, second, new[] { second })),
            "confirming or skipping a middle candidate should keep the operator at the same visual row");
        AssertTrue(
            ReferenceEquals(
                second,
                WpfCandidateReviewSelectionService.FindNextVisibleCandidateAfter(new[] { first, second, third }, third, new[] { third })),
            "confirming or skipping the last candidate should fall back to the previous visible candidate");
    }

    internal static void TestWpfCandidateReviewStateService()
    {
        var first = new YoloWorkerSmokeCandidate { ClassName = "A", Confidence = 0.80, X = 1, Y = 2, Width = 3, Height = 4 };
        var second = new YoloWorkerSmokeCandidate { ClassName = "B", Confidence = 0.95, X = 5, Y = 6, Width = 7, Height = 8 };
        var service = new WpfCandidateReviewStateService();

        AssertEqual(2, service.LoadPendingCandidates(new[] { first, null, second }, clearConfirmed: true));
        AssertEqual(2, service.PendingCount);
        AssertEqual(0, service.ConfirmedCount);
        AssertTrue(ReferenceEquals(second, service.GetPendingCandidateAt(1)), "candidate state should expose stable pending indexing");
        AssertEqual(1, service.IndexOfPendingCandidate(second));
        AssertEqual(1, service.GetVisibleCandidates(0.90).Count);

        WpfCandidateConfirmationPlan plan = service.BuildConfirmationPlan(
            new[] { first, second },
            candidate => ReferenceEquals(candidate, second),
            candidate => ReferenceEquals(candidate, first));
        AssertTrue(plan.HasConfirmableCandidates, "candidate state should produce a confirmable plan");
        AssertEqual(1, plan.ConfirmableCandidates.Count);
        AssertEqual(1, plan.DuplicatePendingCount);
        AssertEqual(1, plan.SkippedDuplicateCount);

        service.ApplyConfirmation(plan.ConfirmableCandidates);
        AssertEqual(1, service.PendingCount);
        AssertEqual(1, service.ConfirmedCount);
        AssertTrue(ReferenceEquals(second, service.ConfirmedCandidates[0]), "confirmed candidate should move into confirmed state");
        AssertTrue(service.SkipCandidate(first), "pending candidate should be skippable through the state service");
        AssertEqual(0, service.PendingCount);

        service.LoadPendingCandidates(new[] { first }, clearConfirmed: false);
        AssertEqual(1, service.PendingCount);
        AssertEqual(1, service.ConfirmedCount);
        AssertEqual(1, service.ClearPendingCandidates());
        service.ClearAll();
        AssertEqual(0, service.PendingCount);
        AssertEqual(0, service.ConfirmedCount);
    }
    internal static void TestWpfCandidateConfirmationService()
    {
        var duplicate = new YoloWorkerSmokeCandidate { ClassName = "A", Confidence = 0.90, X = 1, Y = 2, Width = 3, Height = 4 };
        var confirmable = new YoloWorkerSmokeCandidate { ClassName = "B", Confidence = 0.95, X = 5, Y = 6, Width = 7, Height = 8 };
        var state = new WpfCandidateReviewStateService();
        var service = new WpfCandidateConfirmationService();

        state.LoadPendingCandidates(new[] { duplicate }, clearConfirmed: true);
        WpfCandidateConfirmationAttempt duplicateAttempt = service.Prepare(
            state,
            new[] { duplicate },
            candidate => false,
            candidate => ReferenceEquals(candidate, duplicate));
        AssertTrue(!duplicateAttempt.CanConfirm, "duplicate-only attempt should not be confirmable");
        AssertTrue(duplicateAttempt.ReviewHistoryMessage.Contains("\uC911\uBCF5", StringComparison.Ordinal), "duplicate attempt should explain duplicate exclusion");
        AssertTrue(duplicateAttempt.LogMessage.Contains("\uD655\uC815\uD558\uC9C0", StringComparison.Ordinal), "duplicate attempt should produce an operator log message");

        state.LoadPendingCandidates(new[] { duplicate, confirmable }, clearConfirmed: true);
        WpfCandidateConfirmationAttempt readyAttempt = service.Prepare(
            state,
            new[] { duplicate, confirmable },
            candidate => ReferenceEquals(candidate, confirmable),
            candidate => ReferenceEquals(candidate, duplicate));
        AssertTrue(readyAttempt.CanConfirm, "non-overlapping candidate should be confirmable");
        service.ApplyConfirmation(state, readyAttempt.Plan);
        AssertEqual(1, state.PendingCount);
        AssertEqual(1, state.ConfirmedCount);
        AssertTrue(ReferenceEquals(confirmable, state.ConfirmedCandidates[0]), "confirmation service should apply state mutation through the state service");

        WpfCandidateConfirmationResult savedResult = service.BuildConfirmedResult(
            "\uC120\uD0DD",
            readyAttempt.Plan,
            saved: true,
            savedCount: 2,
            labelPathSummary: "labels\\sample.txt");
        AssertEqual(1, savedResult.ConfirmedCount);
        AssertEqual(1, savedResult.SkippedDuplicateCount);
        AssertTrue(savedResult.ReviewHistoryMessage.Contains("\uD655\uC815(\uC120\uD0DD)", StringComparison.Ordinal), "confirmed history should include scope");
        AssertTrue(savedResult.ReviewHistoryMessage.Contains("\uD30C\uC77C \uC800\uC7A5 2\uAC1C", StringComparison.Ordinal), "confirmed history should include file-saved count");
        AssertTrue(savedResult.LogMessage.Contains("\uD30C\uC77C \uBC18\uC601", StringComparison.Ordinal), "confirmed log should say the confirmed candidate labels were reflected to file");
        AssertTrue(savedResult.DuplicateLogMessage.Contains("\uC81C\uC678", StringComparison.Ordinal), "confirmed result should expose duplicate exclusion log text");

        WpfCandidateConfirmationResult skippedSaveResult = service.BuildConfirmedResult(
            "\uD45C\uC2DC \uD6C4\uBCF4 \uC804\uCCB4",
            readyAttempt.Plan,
            saved: false,
            savedCount: 0,
            labelPathSummary: string.Empty);
        AssertTrue(skippedSaveResult.ReviewHistoryMessage.Contains("\uC800\uC7A5 \uAC74\uB108\uB700", StringComparison.Ordinal), "unsaved confirmation should be visible in review history");
    }

    internal static void TestWpfCandidateReviewPresentationService()
    {
        var first = new YoloWorkerSmokeCandidate { Index = 1, ClassName = "A", Confidence = 0.80, X = 1, Y = 2, Width = 3, Height = 4 };
        var second = new YoloWorkerSmokeCandidate { Index = 2, ClassName = "B", Confidence = 0.95, X = 5, Y = 6, Width = 7, Height = 8 };
        var service = new WpfCandidateReviewPresentationService();
        Func<YoloWorkerSmokeCandidate, Rectangle> bounds = candidate => new Rectangle((int)candidate.X, (int)candidate.Y, (int)candidate.Width, (int)candidate.Height);
        Func<Rectangle, WpfCandidateOverlapInfo> overlap = rect => new WpfCandidateOverlapInfo(string.Empty, Rectangle.Empty, 0D);

        WpfCandidateReviewListPresentation empty = service.BuildListPresentation(
            Array.Empty<YoloWorkerSmokeCandidate>(),
            Array.Empty<YoloWorkerSmokeCandidate>(),
            null,
            0.50D,
            0.50F,
            bounds,
            overlap);
        AssertEqual(1, empty.Rows.Count);
        AssertTrue(!empty.Rows[0].IsEnabled, "empty candidate presentation should return a disabled row");
        AssertTrue(empty.Detail.Contains("\uD6C4\uBCF4", StringComparison.Ordinal), "empty candidate presentation should explain the candidate state");

        WpfCandidateReviewListPresentation filtered = service.BuildListPresentation(
            new[] { first, second },
            Array.Empty<YoloWorkerSmokeCandidate>(),
            null,
            0.90D,
            0.50F,
            bounds,
            overlap);
        AssertEqual(1, filtered.Rows.Count);
        AssertTrue(filtered.Rows[0].Title.Contains("\uD544\uD130", StringComparison.Ordinal), "filtered presentation should show the confidence-filter empty row");
        AssertTrue(filtered.Detail.Contains("90", StringComparison.Ordinal), "filtered presentation should include the active confidence filter");

        WpfCandidateReviewListPresentation rows = service.BuildListPresentation(
            new[] { first, second },
            new[] { first, second },
            second,
            0.50D,
            0.50F,
            bounds,
            rect => new WpfCandidateOverlapInfo("manual", new Rectangle(20, 20, 4, 4), 0.10D));
        AssertEqual(2, rows.Rows.Count);
        AssertTrue(ReferenceEquals(second, rows.PreferredCandidate), "candidate presentation should preserve the preferred selection payload");
        AssertTrue(rows.Rows[1].Title.Contains("B", StringComparison.Ordinal), "candidate presentation should include the candidate class in the row title");
        AssertTrue(ReferenceEquals(second, rows.Rows[1].Payload), "candidate presentation row should keep the source candidate payload");

        WpfCandidateComparisonPresentation duplicateComparison = WpfCandidateReviewPresenter.BuildComparison(
            first,
            bounds(first),
            new WpfCandidateOverlapInfo("manual", bounds(first), 0.95D));
        AssertTrue(duplicateComparison.DecisionText.Contains("\uAE30\uC874 \uB77C\uBCA8 \uBC84\uD2BC", StringComparison.Ordinal), "duplicate candidate decision should point to the existing-label action");
        AssertTrue(duplicateComparison.DecisionText.Contains("\uC2A4\uD0B5", StringComparison.Ordinal), "duplicate candidate decision should tell the operator to skip same-object candidates");
        AssertTrue(duplicateComparison.SelectionSummaryText.Contains("\uD604\uC7AC \uB77C\uBCA8", StringComparison.Ordinal), "duplicate candidate summary should identify the overlapping current label");
        AssertTrue(duplicateComparison.SelectionSummaryText.Contains("\uC2A4\uD0B5", StringComparison.Ordinal), "duplicate candidate summary should point to skip");

        WpfCandidateComparisonPresentation newCandidateComparison = WpfCandidateReviewPresenter.BuildComparison(
            second,
            bounds(second),
            new WpfCandidateOverlapInfo(string.Empty, Rectangle.Empty, 0D));
        AssertTrue(newCandidateComparison.DecisionText.Contains("\uD655\uC815", StringComparison.Ordinal), "new candidate decision should point to confirm");
        AssertTrue(newCandidateComparison.DecisionText.Contains("\uC2A4\uD0B5", StringComparison.Ordinal), "new candidate decision should still explain the reject path");
        AssertTrue(newCandidateComparison.SelectionSummaryText.Contains("\uACB9\uCE68 \uC5C6\uC74C", StringComparison.Ordinal), "new candidate summary should say there is no current-label overlap");
        AssertTrue(newCandidateComparison.SelectionSummaryText.Contains("\uD655\uC815", StringComparison.Ordinal), "new candidate summary should point to confirm");

        var imageLevel = new YoloWorkerSmokeCandidate
        {
            Index = 3,
            ClassName = "abnormal",
            Confidence = 0.998,
            ImageLevel = true
        };
        Rectangle emptyBounds = Rectangle.Empty;
        WpfCandidateComparisonPresentation imageLevelComparison = WpfCandidateReviewPresenter.BuildComparison(
            imageLevel,
            emptyBounds,
            new WpfCandidateOverlapInfo(string.Empty, Rectangle.Empty, 0D));
        WpfCandidateReviewListItem imageLevelRow = WpfCandidateReviewPresenter.BuildListItem(
            imageLevel,
            1,
            emptyBounds,
            new WpfCandidateOverlapInfo(string.Empty, Rectangle.Empty, 0D),
            0.25F);
        AssertTrue(imageLevelComparison.CandidateText.Contains("이미지 전체 판정", StringComparison.Ordinal), "image-level candidate comparison should name its classification scope");
        AssertTrue(imageLevelComparison.OverlapText.Contains("해당 없음", StringComparison.Ordinal), "image-level candidate comparison should not report a geometry overlap percentage");
        AssertTrue(!imageLevelComparison.CandidateText.Contains("이미지 밖", StringComparison.Ordinal), "image-level candidate should not be described as outside the image");
        AssertTrue(imageLevelComparison.DecisionText.Contains("OK/NG", StringComparison.Ordinal), "image-level candidate comparison should point to OK/NG review");
        AssertTrue(imageLevelRow.SecondaryText.Contains("이미지 전체 판정", StringComparison.Ordinal), "image-level candidate row should name its classification scope");
        AssertTrue(!imageLevelRow.SecondaryText.Contains("이미지 밖", StringComparison.Ordinal), "image-level candidate row should not use object-detection geometry wording");
        AssertTrue(WpfCandidateReviewPresenter.BuildConfirmDisabledHint(imageLevel, emptyBounds, default).Contains("OK/NG", StringComparison.Ordinal), "image-level confirm hint should point to the image decision controls");

        WpfDetectionOverlayPresentation overlayEmpty = service.BuildOverlayPresentation(
            string.Empty,
            Array.Empty<YoloWorkerSmokeCandidate>(),
            null,
            0.50D,
            candidate => false,
            candidate => false,
            candidate => string.Empty);
        AssertTrue(overlayEmpty.IsEmpty, "empty overlay presentation should request clearing the canvas overlay");

        WpfDetectionOverlayPresentation overlay = service.BuildOverlayPresentation(
            @"C:\data\sample.png",
            new[] { first, second },
            second,
            0.50D,
            candidate => ReferenceEquals(candidate, first),
            candidate => ReferenceEquals(candidate, second),
            candidate => $"secondary-{candidate.ClassName}");
        AssertTrue(!overlay.IsEmpty, "candidate overlay presentation should be visible while pending candidates exist");
        AssertEqual(WpfDetectionOverlayStatus.Confirmable, overlay.Status);
        AssertTrue(overlay.Title.Contains("AI \uD6C4\uBCF4", StringComparison.Ordinal), "candidate overlay title should identify results as AI candidates");
        AssertTrue(overlay.Title.Contains("\uC800\uC7A5 \uC804", StringComparison.Ordinal), "candidate overlay title should say candidates are not saved labels yet");
        AssertTrue(overlay.Summary.Contains("sample.png", StringComparison.Ordinal), "overlay summary should include the active image name");
        AssertTrue(overlay.Summary.Contains("2", StringComparison.Ordinal), "overlay summary should include the pending candidate count");
        AssertTrue(overlay.Summary.Contains("AI \uD6C4\uBCF4", StringComparison.Ordinal), "overlay summary should identify pending detections as AI candidates");
        AssertTrue(overlay.Summary.Contains("\uC800\uC7A5 \uC804", StringComparison.Ordinal), "overlay summary should say pending candidates are not saved labels yet");
        AssertTrue(overlay.SelectedText.Contains("B", StringComparison.Ordinal), "overlay selected text should describe the selected candidate");
        AssertTrue(overlay.SelectedText.Contains("AI \uD6C4\uBCF4", StringComparison.Ordinal), "overlay selected text should keep the AI candidate wording");
        AssertTrue(overlay.SelectedText.Contains("secondary-B", StringComparison.Ordinal), "overlay selected text should include the secondary review text");
        AssertTrue(overlay.Detail.Contains("AI \uD6C4\uBCF4", StringComparison.Ordinal), "overlay detail should keep the AI candidate wording");
        AssertTrue(overlay.Detail.Contains("secondary-A", StringComparison.Ordinal), "overlay detail should summarize the first visible candidates");
    }
    private static WpfCandidateReviewListItem CreateCandidateReviewTestItem(string title, YoloWorkerSmokeCandidate candidate)
        => new WpfCandidateReviewListItem(
            title,
            string.Empty,
            string.Empty,
            candidate,
            MahApps.Metro.IconPacks.PackIconMaterialKind.CheckCircleOutline,
            System.Windows.Media.Brushes.LimeGreen);

    internal static void TestWpfObjectReviewEditService()
    {
        var manualRois = new List<Rectangle>
        {
            new Rectangle(1, 2, 10, 12),
            new Rectangle(3, 4, 20, 24)
        };
        var manualClasses = new List<string> { "Defect" };
        var manualSegments = new List<LabelingSegmentationObject>
        {
            new LabelingSegmentationObject(
                new[]
                {
                    new Point(1, 1),
                    new Point(8, 1),
                    new Point(8, 8)
                },
                new CClassItem { Text = "Poly" })
        };
        var confirmed = new List<YoloWorkerSmokeCandidate>
        {
            new YoloWorkerSmokeCandidate { ClassName = "OK", X = 5, Y = 6, Width = 7, Height = 8 },
            new YoloWorkerSmokeCandidate { ClassName = "NG", X = 9, Y = 10, Width = 11, Height = 12 }
        };

        AssertEqual("Defect", WpfObjectReviewEditService.GetClassName(WpfObjectReviewItemRef.Manual(1), manualClasses, manualSegments, confirmed));
        AssertTrue(
            WpfObjectReviewEditService.TryApplyClass(
                WpfObjectReviewItemRef.Manual(1),
                manualRois,
                manualClasses,
                manualSegments,
                confirmed,
                " Scratch ",
                out string manualClass),
            "manual object class was not applied");
        AssertEqual("Scratch", manualClass);
        AssertEqual("Scratch", manualClasses[1]);

        AssertTrue(
            WpfObjectReviewEditService.TryApplyClass(
                WpfObjectReviewItemRef.ConfirmedAi(0),
                manualRois,
                manualClasses,
                manualSegments,
                confirmed,
                " AIClass ",
                out string aiClass),
            "confirmed object class was not applied");
        AssertEqual("AIClass", aiClass);
        AssertEqual("AIClass", confirmed[0].ClassName);

        manualSegments[0].MaskData = new byte[100];
        manualSegments[0].MaskSize = new Size(10, 10);
        manualSegments[0].MaskBounds = new Rectangle(1, 1, 8, 8);
        manualSegments[0].RenderVersion = 3;
        var segmentClassItem = new CClassItem { Text = "SegmentClass", DrawColor = Color.LimeGreen };
        AssertTrue(
            WpfObjectReviewEditService.TryApplyClass(
                WpfObjectReviewItemRef.ManualSegment(0),
                manualRois,
                manualClasses,
                manualSegments,
                confirmed,
                " SegmentClass ",
                out string segmentClass,
                segmentClassItem),
            "manual segment class was not applied");
        AssertEqual("SegmentClass", segmentClass);
        AssertEqual("SegmentClass", manualSegments[0].ClassName);
        AssertTrue(ReferenceEquals(segmentClassItem, manualSegments[0].ClassItem), "manual segment should use the applied catalog class item");
        AssertEqual(Color.LimeGreen.ToArgb(), manualSegments[0].Color.ToArgb());
        AssertEqual(4, manualSegments[0].RenderVersion);
        AssertEqual(new Rectangle(1, 1, 8, 8), manualSegments[0].RenderDirtyBounds);

        AssertTrue(
            WpfObjectReviewEditService.TryDelete(WpfObjectReviewItemRef.Manual(0), manualRois, manualClasses, manualSegments, confirmed),
            "manual object was not deleted");
        AssertEqual(1, manualRois.Count);
        AssertEqual(1, manualClasses.Count);
        AssertEqual("Scratch", manualClasses[0]);

        AssertTrue(
            WpfObjectReviewEditService.TryDelete(WpfObjectReviewItemRef.ManualSegment(0), manualRois, manualClasses, manualSegments, confirmed),
            "manual segment was not deleted");
        AssertEqual(0, manualSegments.Count);

        AssertTrue(
            WpfObjectReviewEditService.TryDelete(WpfObjectReviewItemRef.ConfirmedAi(1), manualRois, manualClasses, manualSegments, confirmed),
            "confirmed object was not deleted");
        AssertEqual(1, confirmed.Count);
        AssertEqual("AIClass", confirmed[0].ClassName);

        AssertTrue(
            !WpfObjectReviewEditService.TryDelete(WpfObjectReviewItemRef.Manual(3), manualRois, manualClasses, manualSegments, confirmed),
            "invalid manual object delete should fail");
    }
}
