using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class IntelligentScissorsTests
{
    internal static void TestIntelligentScissorsContract()
    {
        TestDeterministicAccuracyLatencyAndStalePlan();
        TestShellPreviewApplyHistoryAndProtection();
    }

    private static void TestDeterministicAccuracyLatencyAndStalePlan()
    {
        var imageSize = new Size(128, 128);
        using Bitmap bitmap = CreateBoundaryFixture(imageSize);
        var classItem = new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen };
        var polygon = new LabelingSegmentationObject(
            new[]
            {
                new Point(16, 48),
                new Point(112, 48),
                new Point(112, 112),
                new Point(16, 112)
            },
            classItem)
        {
            ClassName = "Defect",
            ObjectId = "scissors-fixture",
            ComponentIndex = 4,
            ZOrder = 11,
            LastStructuralOperation = "Original"
        };
        var service = new WpfIntelligentScissorsService(new WpfIntelligentScissorsOptions
        {
            SearchRadiusPixels = 24,
            MaximumSearchPixelCount = 20_000,
            SimplificationTolerancePixels = 0.75D
        });

        var elapsed = Stopwatch.StartNew();
        AssertTrue(
            service.TryBuildPlan(
                bitmap,
                polygon,
                new Point(64, 47),
                imageSize,
                edgeHitTolerancePixels: 8,
                out WpfIntelligentScissorsPlan first,
                out string firstError),
            firstError);
        elapsed.Stop();
        AssertTrue(
            elapsed.Elapsed <= TimeSpan.FromMilliseconds(250),
            $"128x128 scissors fixture exceeded 250 ms: {elapsed.Elapsed.TotalMilliseconds:0.0} ms");
        AssertEqual(0, first.EdgeIndex);
        AssertTrue(first.PathPoints.Count >= 3, "edge path should contain an interior correction point");
        AssertEqual(new Point(16, 48), first.PathPoints[0]);
        AssertEqual(new Point(112, 48), first.PathPoints[first.PathPoints.Count - 1]);
        AssertTrue(first.ReplacementPoints.Count > polygon.Points.Count, "edge refinement should add boundary detail");

        AssertTrue(
            service.TryBuildPlan(
                bitmap,
                polygon,
                new Point(64, 47),
                imageSize,
                edgeHitTolerancePixels: 8,
                out WpfIntelligentScissorsPlan second,
                out string secondError),
            secondError);
        AssertTrue(
            first.PathPoints.SequenceEqual(second.PathPoints),
            "identical image, geometry, and click must return an identical path");
        AssertTrue(
            first.ReplacementPoints.SequenceEqual(second.ReplacementPoints),
            "identical inputs must return an identical replacement polygon");

        int accuratePointCount = first.PathPoints.Count(point =>
            Math.Abs(point.Y - ExpectedBoundaryY(point.X)) <= 2.5D);
        double accuracy = accuratePointCount / (double)first.PathPoints.Count;
        AssertTrue(
            accuracy >= 0.90D,
            $"at least 90% of simplified path points must stay within 2.5 px of the fixture edge; actual={accuracy:P1}");

        Point[] beforeApply = polygon.Points.ToArray();
        AssertTrue(
            service.TryApplyPlan(
                polygon,
                first,
                imageSize,
                out Rectangle changedBounds,
                out string applyError),
            applyError);
        AssertTrue(!changedBounds.IsEmpty, "applying the path should report changed bounds");
        AssertEqual("scissors-fixture", polygon.ObjectId);
        AssertEqual("Defect", polygon.ClassName);
        AssertEqual(4, polygon.ComponentIndex);
        AssertEqual(11, polygon.ZOrder);
        AssertEqual(
            WpfPolygonAnnotationService.IntelligentScissorsStructuralOperationName,
            polygon.LastStructuralOperation);
        AssertTrue(
            polygon.Points.SequenceEqual(first.ReplacementPoints),
            "apply should use exactly the previewed replacement points");

        AssertTrue(
            !service.TryApplyPlan(
                polygon,
                second,
                imageSize,
                out _,
                out string staleError),
            "a preview must become stale after the source geometry changes");
        AssertTrue(
            staleError.Contains("\uBCC0\uACBD", StringComparison.Ordinal),
            "stale preview rejection should explain that geometry changed");
        AssertTrue(
            !polygon.Points.SequenceEqual(beforeApply),
            "the valid apply should remain intact after a stale apply is rejected");

        var uniformPolygon = new LabelingSegmentationObject(beforeApply, classItem)
        {
            ObjectId = "uniform"
        };
        using var uniform = new Bitmap(imageSize.Width, imageSize.Height);
        using (Graphics graphics = Graphics.FromImage(uniform))
        {
            graphics.Clear(Color.Gray);
        }
        AssertTrue(
            !service.TryBuildPlan(
                uniform,
                uniformPolygon,
                new Point(64, 48),
                imageSize,
                edgeHitTolerancePixels: 8,
                out _,
                out string uniformError),
            "a straight path with no distinct edge correction should not create a preview");
        AssertTrue(!string.IsNullOrWhiteSpace(uniformError), "rejected preview should explain why it was not created");
        AssertTrue(uniformPolygon.Points.SequenceEqual(beforeApply), "rejected planning must not mutate geometry");
        AssertEqual(string.Empty, uniformPolygon.LastStructuralOperation);
    }

    private static void TestShellPreviewApplyHistoryAndProtection()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        string root = CreateTempRoot();
        var data = new CData();
        data.ConfigureOutputRoot(Path.Combine(root, "source"));
        data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        data.ProjectSettings.YoloDataset.ValidationPercent = 0;
        data.ProjectSettings.YoloDataset.TestPercent = 0;
        var defect = new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen };
        data.ClassNamedList.Add(defect);
        CGlobal.Inst.Data = data;

        var imageSize = new Size(128, 128);
        using Bitmap bitmap = CreateBoundaryFixture(imageSize);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            segments.Add(new LabelingSegmentationObject(
                new[]
                {
                    new Point(16, 48),
                    new Point(112, 48),
                    new Point(112, 112),
                    new Point(16, 112)
                },
                defect)
            {
                ClassName = "Defect",
                ObjectId = "scissors-shell",
                ComponentIndex = 5,
                ZOrder = 13,
                LastStructuralOperation = "Original"
            });

            InvokePrivate(window, "RefreshObjectList");
            AssertTrue(window.ObjectReviewViewModel.IsPolygonVertexContextVisible, "selected polygon should reveal precision tools");
            AssertTrue(window.ObjectReviewViewModel.IsIntelligentScissorsEnabled, "selected editable polygon should enable scissors");
            AssertTrue(window.FindName("BeginIntelligentScissorsButton") is System.Windows.Controls.Button, "object review should expose contextual edge following");
            AssertTrue(window.FindName("ApplyIntelligentScissorsButton") is System.Windows.Controls.Button, "object review should expose explicit preview apply");
            AssertTrue(window.FindName("CancelIntelligentScissorsButton") is System.Windows.Controls.Button, "object review should expose preview cancel");

            window.ObjectReviewViewModel.BeginIntelligentScissorsCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsIntelligentScissorsPending, "scissors should arm one edge click");
            RightClick(window, new Point(64, 48));
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsPending, "right-click should cancel edge selection");
            AssertEqual(0, History(window).Count);
            AssertEqual(4, segments[0].Points.Count);

            window.ObjectReviewViewModel.BeginIntelligentScissorsCommand.Execute(null);
            Click(window, new Point(64, 47));
            AssertTrue(window.ObjectReviewViewModel.IsIntelligentScissorsPending, "preview should remain pending until explicit apply or cancel");
            AssertTrue(window.ObjectReviewViewModel.HasIntelligentScissorsPreview, "successful edge analysis should enable explicit apply");
            AssertTrue(
                window.MainCanvasViewModel.PolygonOverlays.Any(overlay =>
                    string.Equals(overlay.Label, "EDGE PREVIEW", StringComparison.Ordinal)),
                "canvas should show the preview path before mutation");
            AssertEqual(0, History(window).Count);
            AssertEqual(4, segments[0].Points.Count);

            byte[] maskData = new byte[imageSize.Width * imageSize.Height];
            maskData[(96 * imageSize.Width) + 96] = 255;
            segments.Add(new LabelingSegmentationObject
            {
                ClassName = "Defect",
                ClassItem = defect,
                ObjectId = "scissors-mask",
                ZOrder = 14,
                MaskData = maskData,
                MaskSize = imageSize,
                MaskBounds = new Rectangle(96, 96, 1, 1)
            });
            InvokePrivate(window, "RefreshObjectList");
            WpfObjectReviewListItem maskRow = window.ObjectReviewViewModel.Objects.Single(item =>
                item.IsManualSegment && !item.IsManualPolygon);
            window.ObjectReviewViewModel.SelectedObject = maskRow;
            window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(maskRow);
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsPending, "selection change should cancel a pending preview");
            AssertEqual(4, segments[0].Points.Count);
            AssertEqual(0, History(window).Count);

            WpfObjectReviewListItem polygonRow = window.ObjectReviewViewModel.Objects.Single(item =>
                item.IsManualPolygon);
            window.ObjectReviewViewModel.SelectedObject = polygonRow;
            window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(polygonRow);
            window.ObjectReviewViewModel.BeginIntelligentScissorsCommand.Execute(null);
            Click(window, new Point(64, 47));
            window.ObjectReviewViewModel.ApplyIntelligentScissorsCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsPending, "apply should close the preview session");
            AssertTrue(segments[0].Points.Count > 4, "apply should replace one straight edge with image-boundary detail");
            AssertEqual(1, History(window).Count);
            AssertEqual("scissors-shell", segments[0].ObjectId);
            AssertEqual("Defect", segments[0].ClassName);
            AssertEqual(5, segments[0].ComponentIndex);
            AssertEqual(13, segments[0].ZOrder);
            AssertEqual(
                WpfPolygonAnnotationService.IntelligentScissorsStructuralOperationName,
                segments[0].LastStructuralOperation);

            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "scissors apply should be one undo step");
            AssertEqual(4, segments[0].Points.Count);
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "scissors apply should be one redo step");
            AssertTrue(segments[0].Points.Count > 4, "redo should restore the exact previewed path");
            AssertEqual("scissors-shell", segments[0].ObjectId);

            YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                "scissors-shell.png",
                bitmap,
                new Dictionary<string, List<LabelingSegmentationObject>>
                {
                    ["Defect"] = segments.ToList()
                },
                data.ClassNamedList,
                data);
            string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "scissors-shell.json");
            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    segmentPath,
                    Path.Combine(data.OutputRootPath, "data", "train", "masks", "scissors-shell.png"),
                    data.ClassNamedList,
                    imageSize);
            LabelingSegmentationObject reopened = loaded["Defect"].Single(item => item.ObjectId == "scissors-shell");
            AssertEqual(13, reopened.ZOrder);
            AssertEqual(5, reopened.ComponentIndex);
            AssertEqual(
                WpfPolygonAnnotationService.IntelligentScissorsStructuralOperationName,
                reopened.LastStructuralOperation);
            AssertTrue(reopened.Points.Count > 4, "canonical reopen should retain boundary detail after minimum-distance normalization");
            AssertTrue(
                WpfPolygonAnnotationService.IsValidSimplePolygon(reopened.Points, imageSize),
                "canonical reopen should preserve a valid refined polygon");
            AssertEqual(segments[0].Bounds, reopened.Bounds);

            InvokePrivate(window, "RefreshObjectList");
            polygonRow = window.ObjectReviewViewModel.Objects.Single(item => item.IsManualPolygon);
            window.ObjectReviewViewModel.SelectedObject = polygonRow;
            window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(polygonRow);
            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsEnabled, "full lock should disable scissors");
            window.ObjectReviewViewModel.BeginIntelligentScissorsCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsPending, "direct command execution must respect full lock");
            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);

            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsEnabled, "hidden polygon should disable scissors");
            window.ObjectReviewViewModel.BeginIntelligentScissorsCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsIntelligentScissorsPending, "direct command execution must respect hidden state");
            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);

            window.ObjectReviewViewModel.ToggleObjectPinnedCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsIntelligentScissorsEnabled, "movement pin should preserve boundary refinement");
            window.ObjectReviewViewModel.BeginIntelligentScissorsCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsIntelligentScissorsPending, "movement-pinned polygon should allow edge selection");
            RightClick(window, new Point(64, 48));
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    internal static Bitmap CreateBoundaryFixture(Size imageSize)
    {
        var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        for (int y = 0; y < imageSize.Height; y++)
        {
            for (int x = 0; x < imageSize.Width; x++)
            {
                int boundaryY = (int)Math.Round(ExpectedBoundaryY(x));
                bitmap.SetPixel(x, y, y >= boundaryY ? Color.White : Color.Black);
            }
        }

        return bitmap;
    }

    internal static double ExpectedBoundaryY(int x)
    {
        double normalized = Math.Clamp((x - 16D) / 96D, 0D, 1D);
        return 48D - (16D * Math.Sin(Math.PI * normalized));
    }

    private static List<WpfAnnotationHistorySnapshot> History(WpfLabelingShellWindow window)
        => GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory");

    private static void Click(WpfLabelingShellWindow window, Point point)
        => InvokePrivateResult<object>(
            window,
            "MainCanvasViewModel_ImagePointClicked",
            window.MainCanvasViewModel,
            new CanvasImagePointEventArgs(CanvasPointerButton.Left, 1, 0, 0, point, PointF.Empty));

    private static void RightClick(WpfLabelingShellWindow window, Point point)
        => InvokePrivateResult<object>(
            window,
            "MainCanvasViewModel_ImagePointClicked",
            window.MainCanvasViewModel,
            new CanvasImagePointEventArgs(CanvasPointerButton.Right, 1, 0, 0, point, PointF.Empty));
}
