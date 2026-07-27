using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using OpenVisionLab.ImageCanvas.Canvas;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class SegmentationSplitTests
{
    internal static void TestSplitWorkflow()
    {
        TestPolygonSplitHistoryAndCanonicalRoundTrip();
        TestRasterSplitAndInvalidCut();
    }

    private static void TestPolygonSplitHistoryAndCanonicalRoundTrip()
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

        var imageSize = new Size(32, 24);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            segments.Add(new LabelingSegmentationObject(
                RectanglePoints(2, 2, 28, 20),
                defect)
            {
                ClassName = "Defect",
                ObjectId = "source-polygon",
                ZOrder = 4,
                CutoutPolygons = new List<List<Point>>
                {
                    RectanglePoints(5, 6, 8, 10)
                }
            });

            InvokePrivate(window, "RefreshObjectList");
            AssertTrue(window.ObjectReviewViewModel.IsSplitEnabled, "selected polygon should enable split");
            AssertTrue(
                window.FindName("BeginVerticalSplitButton") is System.Windows.Controls.Button,
                "object review should expose the vertical split button");
            window.ObjectReviewViewModel.BeginVerticalSplitCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsSplitPending, "vertical split should arm one canvas click");

            InvokePrivateResult<object>(
                window,
                "MainCanvasViewModel_ImagePointClicked",
                window.MainCanvasViewModel,
                new CanvasImagePointEventArgs(
                    CanvasPointerButton.Left,
                    1,
                    0,
                    0,
                    new Point(2, 12),
                    PointF.Empty));
            AssertEqual(1, segments.Count);
            AssertEqual("source-polygon", segments[0].ObjectId);
            AssertTrue(window.ObjectReviewViewModel.IsSplitPending, "invalid cut should keep location input armed");
            AssertEqual(
                0,
                GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count);

            InvokePrivateResult<object>(
                window,
                "MainCanvasViewModel_ImagePointClicked",
                window.MainCanvasViewModel,
                new CanvasImagePointEventArgs(
                    CanvasPointerButton.Left,
                    1,
                    0,
                    0,
                    new Point(15, 12),
                    PointF.Empty));

            AssertEqual(2, segments.Count);
            AssertTrue(!window.ObjectReviewViewModel.IsSplitPending, "successful split should leave point-input mode");
            AssertTrue(segments.All(segment => segment.IsRasterMask), "polygon split should publish raster components");
            AssertTrue(segments.All(segment => segment.ObjectId != "source-polygon"), "split should assign new object ids");
            AssertEqual(2, segments.Select(segment => segment.ObjectId).Distinct().Count());
            AssertTrue(
                segments.All(segment => segment.LastStructuralOperation == WpfSegmentationSplitService.StructuralOperationName),
                "split provenance should be recorded on every result");
            AssertTrue(segments.All(segment => segment.ComponentIndex == -1), "split results should remain independent objects");
            AssertTrue(segments.All(segment => segment.ZOrder == 4), "split should preserve source z-order");
            AssertTrue(segments.All(segment => !IsMaskSet(segment, 15, 12)), "the 1-pixel cut line should be empty");
            AssertTrue(!segments.Any(segment => IsMaskSet(segment, 6, 8)), "polygon cutout pixels should stay empty");

            string[] splitIds = segments.Select(segment => segment.ObjectId).OrderBy(value => value).ToArray();
            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "split should be one undo step");
            AssertEqual(1, segments.Count);
            AssertEqual("source-polygon", segments[0].ObjectId);
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "split should be one redo step");
            AssertEqual(2, segments.Count);
            AssertTrue(
                segments.Select(segment => segment.ObjectId).OrderBy(value => value).SequenceEqual(splitIds),
                "redo should restore the generated split identities");

            SaveAndAssertCanonicalRoundTrip(root, bitmap, imageSize, data, segments, splitIds);
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static void TestRasterSplitAndInvalidCut()
    {
        var imageSize = new Size(20, 20);
        var defect = new CClassItem { Text = "Defect", DrawColor = Color.Red };
        byte[] mask = new byte[imageSize.Width * imageSize.Height];
        FillMaskRectangle(mask, imageSize, new Rectangle(3, 3, 12, 12));
        var source = new LabelingSegmentationObject
        {
            ClassName = "Defect",
            ClassItem = defect,
            ObjectId = "source-raster",
            MaskData = mask,
            MaskSize = imageSize,
            MaskBounds = new Rectangle(3, 3, 12, 12)
        };
        var service = new WpfSegmentationSplitService();

        bool invalid = service.TrySplit(
            source,
            WpfSegmentationSplitOrientation.Vertical,
            coordinate: 3,
            imageSize,
            out _,
            out string invalidError);
        AssertTrue(!invalid, "edge cut should not mutate or return split objects");
        AssertTrue(invalidError.Contains("\uC548\uCABD", StringComparison.Ordinal), "invalid edge cut should explain the inside-bounds rule");
        AssertEqual("source-raster", source.ObjectId);
        AssertTrue(IsMaskSet(source, 3, 3), "invalid cut should leave source pixels unchanged");

        byte[] disconnectedMask = new byte[imageSize.Width * imageSize.Height];
        FillMaskRectangle(disconnectedMask, imageSize, new Rectangle(2, 2, 3, 3));
        FillMaskRectangle(disconnectedMask, imageSize, new Rectangle(12, 2, 3, 3));
        var disconnected = new LabelingSegmentationObject
        {
            ClassName = "Defect",
            ClassItem = defect,
            ObjectId = "disconnected-source",
            MaskData = disconnectedMask,
            MaskSize = imageSize,
            MaskBounds = new Rectangle(2, 2, 13, 3)
        };
        bool noEffectSplit = service.TrySplit(
            disconnected,
            WpfSegmentationSplitOrientation.Vertical,
            coordinate: 8,
            imageSize,
            out _,
            out string noEffectError);
        AssertTrue(!noEffectSplit, "an empty cut through an already disconnected source must not count as a split");
        AssertTrue(noEffectError.Contains("\uBD84\uB9AC", StringComparison.Ordinal), "no-effect cut should explain that no new split was created");
        AssertEqual("disconnected-source", disconnected.ObjectId);

        bool split = service.TrySplit(
            source,
            WpfSegmentationSplitOrientation.Horizontal,
            coordinate: 9,
            imageSize,
            out WpfSegmentationSplitResult result,
            out string error);
        AssertTrue(split, $"horizontal raster split failed: {error}");
        AssertEqual(2, result.Segments.Count);
        AssertTrue(result.Segments.All(segment => !IsMaskSet(segment, 8, 9)), "horizontal cut row should be empty");
        AssertTrue(IsMaskSet(source, 8, 9), "split service should not mutate its source object");
    }

    private static void SaveAndAssertCanonicalRoundTrip(
        string root,
        Bitmap bitmap,
        Size imageSize,
        CData data,
        IReadOnlyList<LabelingSegmentationObject> segments,
        IReadOnlyList<string> splitIds)
    {
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            "split.png",
            bitmap,
            new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = segments.ToList()
            },
            data.ClassNamedList,
            data);
        string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "split.json");
        string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "split.png");
        SegmentationAnnotationFile saved =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
        AssertEqual(3, saved.Version);
        AssertTrue(
            saved.Polygons.All(record => splitIds.Contains(record.ObjectId)),
            "canonical v3 records should preserve split object ids");
        AssertTrue(
            saved.Polygons.All(record => record.LastStructuralOperation == WpfSegmentationSplitService.StructuralOperationName),
            "canonical v3 records should preserve split provenance");

        IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
            YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                maskPath,
                data.ClassNamedList,
                imageSize);
        AssertEqual(2, loaded["Defect"].Select(segment => segment.ObjectId).Distinct().Count());
        AssertTrue(
            loaded["Defect"].All(segment => splitIds.Contains(segment.ObjectId)),
            "canonical reopen should preserve both split identities");

        var roundTripData = new CData();
        roundTripData.ConfigureOutputRoot(Path.Combine(root, "roundtrip"));
        roundTripData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        roundTripData.ProjectSettings.YoloDataset.ValidationPercent = 0;
        roundTripData.ProjectSettings.YoloDataset.TestPercent = 0;
        roundTripData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen });
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            "split.png",
            bitmap,
            new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = loaded["Defect"].ToList()
            },
            roundTripData.ClassNamedList,
            roundTripData);
        string roundTripPath = Path.Combine(roundTripData.OutputRootPath, "data", "train", "segments", "split.json");
        SegmentationAnnotationFile roundTrip =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(roundTripPath));
        AssertTrue(roundTrip.Polygons.All(record => splitIds.Contains(record.ObjectId)), "resave should preserve split object ids");
        AssertTrue(
            roundTrip.Polygons.All(record => record.LastStructuralOperation == WpfSegmentationSplitService.StructuralOperationName),
            "resave should preserve split provenance");
    }

    private static bool IsMaskSet(LabelingSegmentationObject segment, int x, int y)
        => segment?.MaskData != null
            && segment.MaskSize.Width > x
            && segment.MaskSize.Height > y
            && segment.MaskData[(y * segment.MaskSize.Width) + x] != 0;

    private static void FillMaskRectangle(byte[] mask, Size size, Rectangle bounds)
    {
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (int x = bounds.Left; x < bounds.Right; x++)
            {
                mask[(y * size.Width) + x] = 255;
            }
        }
    }

    private static List<Point> RectanglePoints(int left, int top, int right, int bottom)
        => new List<Point>
        {
            new Point(left, top),
            new Point(right, top),
            new Point(right, bottom),
            new Point(left, bottom)
        };
}
