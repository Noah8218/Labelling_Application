using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class SegmentationRemoveUnderlyingTests
{
    internal static void TestRemoveUnderlyingWorkflow()
    {
        TestAnalysisContract();
        TestWindowPreviewHistoryAndCanonicalRoundTrip();
    }

    private static void TestAnalysisContract()
    {
        CClassItem defect = Class("Defect", Color.LimeGreen);
        var backPartial = Segment("partial", 0, new Rectangle(2, 2, 18, 18), defect);
        var fullyCovered = Segment("covered", 1, new Rectangle(10, 10, 4, 4), defect);
        var selected = Segment("selected", 2, new Rectangle(8, 8, 16, 16), defect);
        var front = Segment("front", 3, new Rectangle(10, 10, 4, 4), defect);
        var segments = new[] { backPartial, fullyCovered, selected, front };
        var service = new WpfSegmentationRemoveUnderlyingService();

        AssertTrue(
            service.TryAnalyze(
                segments,
                2,
                new Size(32, 32),
                out WpfSegmentationRemoveUnderlyingPlan plan,
                out string error),
            $"remove-underlying analysis should succeed: {error}");
        AssertEqual(2, plan.Changes.Count);
        AssertEqual(1, plan.RemovedObjectCount);
        AssertTrue(plan.RemovedPixelCount > 0, "analysis should report removed pixels");
        AssertTrue(plan.Changes.Any(change => change.SourceIndex == 0 && change.Replacement != null),
            "partially covered back object should survive as a replacement");
        AssertTrue(plan.Changes.Any(change => change.SourceIndex == 1 && change.Replacement == null),
            "fully covered back object should be removed");
        AssertTrue(plan.Changes.All(change => change.SourceIndex < 2),
            "only objects behind the selected object should be affected");
        AssertEqual(4, segments.Length);
        AssertTrue(ReferenceEquals(backPartial, segments[0]), "analysis must not mutate source geometry");
        AssertTrue(ReferenceEquals(selected, plan.SelectedSource), "selected geometry is the preserved reference");
        AssertEqual("Original", backPartial.LastStructuralOperation);

        var noOverlap = new[]
        {
            Segment("left", 0, new Rectangle(1, 1, 4, 4), defect),
            Segment("right", 1, new Rectangle(20, 20, 4, 4), defect)
        };
        AssertTrue(
            !service.TryAnalyze(noOverlap, 1, new Size(32, 32), out _, out string noOverlapError),
            "non-overlapping objects should not produce a destructive plan");
        AssertTrue(noOverlapError.Contains("\uACB9\uCE58\uB294", StringComparison.Ordinal),
            "non-overlap result should explain why analysis did not proceed");
    }

    private static void TestWindowPreviewHistoryAndCanonicalRoundTrip()
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
        CClassItem defect = Class("Defect", Color.LimeGreen);
        CClassItem other = Class("Other", Color.DeepSkyBlue);
        data.ClassNamedList.Add(defect);
        data.ClassNamedList.Add(other);
        CGlobal.Inst.Data = data;

        var imageSize = new Size(32, 32);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            LabelingSegmentationObject partial = Segment("partial", 0, new Rectangle(2, 2, 18, 18), defect);
            LabelingSegmentationObject covered = Segment("covered", 1, new Rectangle(10, 10, 4, 4), other);
            LabelingSegmentationObject selected = Segment("selected", 2, new Rectangle(8, 8, 16, 16), defect);
            LabelingSegmentationObject front = Segment("front", 3, new Rectangle(10, 10, 4, 4), other);
            segments.AddRange(new[] { partial, covered, selected, front });

            SelectSegment(window, 2);
            AssertTrue(
                window.FindName("PreviewRemoveUnderlyingButton") is System.Windows.Controls.Button,
                "object review should expose remove-underlying analysis");
            AssertTrue(window.ObjectReviewViewModel.IsRemoveUnderlyingPreviewEnabled,
                "selected segment with lower objects should enable analysis");

            window.ObjectReviewViewModel.PreviewRemoveUnderlyingCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsRemoveUnderlyingPreviewPending,
                "analysis should enter an explicit pending confirmation state");
            AssertTrue(window.ObjectReviewViewModel.RemoveUnderlyingStatusText.Contains("2\uAC1C", StringComparison.Ordinal),
                "preview should report two affected objects");
            AssertTrue(
                window.MainCanvasViewModel.PolygonOverlays.Any(overlay =>
                    overlay.Label.StartsWith("REMOVE PREVIEW", StringComparison.Ordinal)
                    && overlay.Color.ToArgb() == Color.Orange.ToArgb()),
                "affected polygon should be highlighted orange during preview");
            AssertEqual(4, segments.Count);
            AssertEqual(0, GetHistoryCount(window));

            window.ObjectReviewViewModel.CancelRemoveUnderlyingCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsRemoveUnderlyingPreviewPending,
                "cancel should leave the preview state");
            AssertEqual(4, segments.Count);
            AssertTrue(ReferenceEquals(partial, segments[0]), "cancel should not replace geometry");
            AssertEqual(0, GetHistoryCount(window));

            SelectSegment(window, 2);
            window.ObjectReviewViewModel.PreviewRemoveUnderlyingCommand.Execute(null);
            partial.ZOrder = 5;
            window.ObjectReviewViewModel.ApplyRemoveUnderlyingCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsRemoveUnderlyingPreviewPending,
                "stale analysis should be rejected and cleared");
            AssertEqual(4, segments.Count);
            AssertEqual(0, GetHistoryCount(window));
            partial.ZOrder = 0;

            SelectSegment(window, 2);
            window.ObjectReviewViewModel.PreviewRemoveUnderlyingCommand.Execute(null);
            window.ObjectReviewViewModel.ApplyRemoveUnderlyingCommand.Execute(null);
            AssertEqual(3, segments.Count);
            AssertEqual(1, GetHistoryCount(window));
            LabelingSegmentationObject replacement = segments.Single(segment => segment.ObjectId == "partial");
            AssertTrue(replacement.IsRasterMask, "partially covered polygon should become exact raster geometry");
            AssertEqual("Defect", replacement.ClassName);
            AssertEqual(0, replacement.ZOrder);
            AssertEqual(WpfSegmentationRemoveUnderlyingService.StructuralOperationName, replacement.LastStructuralOperation);
            AssertTrue(!segments.Any(segment => segment.ObjectId == "covered"),
                "fully covered underlying object should be removed");
            AssertTrue(segments.Any(segment => ReferenceEquals(segment, selected)),
                "selected top object should be preserved");
            AssertTrue(segments.Any(segment => ReferenceEquals(segment, front)),
                "overlapping object in front of the selection should remain untouched");

            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"),
                "remove-underlying should be one undo step");
            AssertEqual(4, segments.Count);
            AssertTrue(segments.Any(segment => segment.ObjectId == "covered"), "undo should restore fully removed object");
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"),
                "remove-underlying should be one redo step");
            AssertEqual(3, segments.Count);
            replacement = segments.Single(segment => segment.ObjectId == "partial");
            AssertEqual(WpfSegmentationRemoveUnderlyingService.StructuralOperationName, replacement.LastStructuralOperation);

            AssertCanonicalRoundTrip(root, bitmap, imageSize, data, segments);
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static void AssertCanonicalRoundTrip(
        string root,
        Bitmap bitmap,
        Size imageSize,
        CData data,
        IReadOnlyList<LabelingSegmentationObject> segments)
    {
        const string imageName = "remove-underlying.png";
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            segments
                .GroupBy(segment => segment.ClassName)
                .ToDictionary(group => group.Key, group => group.ToList()),
            data.ClassNamedList,
            data);
        string segmentPath = Path.Combine(
            data.OutputRootPath,
            "data",
            "train",
            "segments",
            "remove-underlying.json");
        string maskPath = Path.Combine(
            data.OutputRootPath,
            "data",
            "train",
            "masks",
            "remove-underlying.png");
        SegmentationAnnotationFile saved =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
        AssertEqual(3, saved.Version);
        SegmentationPolygonRecord savedPartial = saved.Polygons.Single(record => record.ObjectId == "partial");
        AssertEqual(0, savedPartial.ZOrder);
        AssertEqual(WpfSegmentationRemoveUnderlyingService.StructuralOperationName, savedPartial.LastStructuralOperation);

        IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
            YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                maskPath,
                data.ClassNamedList,
                imageSize);
        List<LabelingSegmentationObject> loadedSegments = loaded.Values.SelectMany(items => items).ToList();
        LabelingSegmentationObject loadedPartial = loadedSegments.Single(segment => segment.ObjectId == "partial");
        AssertTrue(loadedPartial.IsRasterMask, "canonical v3 load should preserve the exact raster remainder");
        AssertEqual(WpfSegmentationRemoveUnderlyingService.StructuralOperationName, loadedPartial.LastStructuralOperation);

        var resaveData = new CData();
        resaveData.ConfigureOutputRoot(Path.Combine(root, "resave"));
        resaveData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        resaveData.ProjectSettings.YoloDataset.ValidationPercent = 0;
        resaveData.ProjectSettings.YoloDataset.TestPercent = 0;
        resaveData.ClassNamedList.Add(Class("Defect", Color.LimeGreen));
        resaveData.ClassNamedList.Add(Class("Other", Color.DeepSkyBlue));
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            loadedSegments
                .GroupBy(segment => segment.ClassName)
                .ToDictionary(group => group.Key, group => group.ToList()),
            resaveData.ClassNamedList,
            resaveData);
        string resavePath = Path.Combine(
            resaveData.OutputRootPath,
            "data",
            "train",
            "segments",
            "remove-underlying.json");
        SegmentationAnnotationFile resaved =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(resavePath));
        SegmentationPolygonRecord resavedPartial = resaved.Polygons.Single(record => record.ObjectId == "partial");
        AssertEqual(0, resavedPartial.ZOrder);
        AssertEqual(WpfSegmentationRemoveUnderlyingService.StructuralOperationName, resavedPartial.LastStructuralOperation);
    }

    private static void SelectSegment(WpfLabelingShellWindow window, int sourceIndex)
    {
        InvokePrivate(window, "RefreshObjectList");
        WpfObjectReviewListItem row = window.ObjectReviewViewModel.Objects
            .Single(item => item.IsManualSegment && item.SourceIndex == sourceIndex);
        window.ObjectReviewViewModel.SelectedObject = row;
        window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(row);
    }

    private static int GetHistoryCount(WpfLabelingShellWindow window)
        => GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count;

    private static CClassItem Class(string name, Color color)
        => new CClassItem { Text = name, DrawColor = color };

    private static LabelingSegmentationObject Segment(
        string objectId,
        int zOrder,
        Rectangle bounds,
        CClassItem classItem)
        => new LabelingSegmentationObject(
            new[]
            {
                new Point(bounds.Left, bounds.Top),
                new Point(bounds.Right, bounds.Top),
                new Point(bounds.Right, bounds.Bottom),
                new Point(bounds.Left, bounds.Bottom)
            },
            classItem)
        {
            ClassName = classItem.Text,
            ObjectId = objectId,
            ZOrder = zOrder,
            LastStructuralOperation = "Original"
        };
}
