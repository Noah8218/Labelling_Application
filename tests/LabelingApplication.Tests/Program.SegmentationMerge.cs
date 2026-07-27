using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class SegmentationMergeTests
{
    internal static void TestMergeWorkflow()
    {
        TestSameClassMixedGeometryMergeAndHistory();
        TestMixedClassRejection();
    }

    private static void TestSameClassMixedGeometryMergeAndHistory()
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

        var imageSize = new Size(40, 40);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            segments.Add(new LabelingSegmentationObject(
                RectanglePoints(2, 2, 15, 15),
                defect)
            {
                ClassName = "Defect",
                ObjectId = "source-polygon",
                CutoutPolygons = new List<List<Point>>
                {
                    RectanglePoints(5, 5, 11, 11)
                }
            });

            byte[] islandMask = new byte[imageSize.Width * imageSize.Height];
            FillMaskRectangle(islandMask, imageSize, new Rectangle(6, 6, 2, 2));
            segments.Add(new LabelingSegmentationObject
            {
                ClassName = "Defect",
                ClassItem = defect,
                ObjectId = "source-island",
                MaskData = islandMask,
                MaskSize = imageSize,
                MaskBounds = new Rectangle(6, 6, 2, 2)
            });

            byte[] distantMask = new byte[imageSize.Width * imageSize.Height];
            FillMaskRectangle(distantMask, imageSize, new Rectangle(24, 24, 5, 5));
            segments.Add(new LabelingSegmentationObject
            {
                ClassName = "Defect",
                ClassItem = defect,
                ObjectId = "source-distant",
                ZOrder = 7,
                MaskData = distantMask,
                MaskSize = imageSize,
                MaskBounds = new Rectangle(24, 24, 5, 5)
            });

            InvokePrivate(window, "RefreshObjectList");
            List<WpfObjectReviewListItem> segmentRows = window.ObjectReviewViewModel.Objects
                .Where(item => item.IsManualSegment)
                .ToList();
            AssertEqual(3, segmentRows.Count);
            foreach (WpfObjectReviewListItem row in segmentRows)
            {
                row.IsMergeSelected = true;
                window.ObjectReviewViewModel.MergeSelectionChangedCommand.Execute(row);
            }

            AssertTrue(window.ObjectReviewViewModel.IsMergeSelectedSegmentsEnabled, "three segment selections should enable merge");
            AssertTrue(
                window.FindName("MergeSelectedSegmentsButton") is System.Windows.Controls.Button,
                "object review should expose the merge button");
            window.ObjectReviewViewModel.MergeSelectedSegmentsCommand.Execute(null);

            AssertEqual(1, segments.Count);
            LabelingSegmentationObject merged = segments[0];
            AssertTrue(merged.IsRasterMask, "mixed polygon/raster merge should produce one raster object");
            AssertTrue(!string.IsNullOrWhiteSpace(merged.ObjectId), "merge should assign a new object id");
            AssertTrue(
                merged.ObjectId != "source-polygon"
                    && merged.ObjectId != "source-island"
                    && merged.ObjectId != "source-distant",
                "merge should not reuse a source object id");
            AssertEqual(WpfSegmentationMergeService.StructuralOperationName, merged.LastStructuralOperation);
            AssertEqual(-1, merged.ComponentIndex);
            AssertEqual(7, merged.ZOrder);
            AssertTrue(IsMaskSet(merged, 3, 3), "outer polygon pixels should survive merge");
            AssertTrue(!IsMaskSet(merged, 9, 9), "unfilled polygon cutout pixels should stay empty");
            AssertTrue(IsMaskSet(merged, 6, 6), "another source object should be allowed to fill a polygon cutout");
            AssertTrue(IsMaskSet(merged, 25, 25), "disconnected raster component should survive merge");

            string mergedObjectId = merged.ObjectId;
            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "merge should be one undo step");
            AssertEqual(3, segments.Count);
            AssertTrue(
                segments.Select(segment => segment.ObjectId).OrderBy(value => value).SequenceEqual(
                    new[] { "source-distant", "source-island", "source-polygon" }),
                "undo should restore all source objects and identities");
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "merge should be one redo step");
            AssertEqual(1, segments.Count);
            AssertEqual(mergedObjectId, segments[0].ObjectId);
            AssertEqual(WpfSegmentationMergeService.StructuralOperationName, segments[0].LastStructuralOperation);

            var segmentsByClass = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject> { segments[0] }
            };
            YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                "merge.png",
                bitmap,
                segmentsByClass,
                data.ClassNamedList,
                data);
            string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "merge.json");
            string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "merge.png");
            SegmentationAnnotationFile saved =
                JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
            AssertEqual(3, saved.Version);
            AssertTrue(saved.Polygons.Count >= 2, "disconnected merge should save multiple component records");
            AssertTrue(saved.Polygons.All(record => record.ObjectId == mergedObjectId), "saved components should share merge object id");
            AssertTrue(
                saved.Polygons.Select(record => record.ComponentIndex).OrderBy(index => index)
                    .SequenceEqual(Enumerable.Range(0, saved.Polygons.Count)),
                "saved merge components should have stable sequential component indices");
            AssertTrue(
                saved.Polygons.All(record => record.LastStructuralOperation == WpfSegmentationMergeService.StructuralOperationName),
                "saved components should preserve merge provenance");

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    segmentPath,
                    maskPath,
                    data.ClassNamedList,
                    imageSize);
            AssertTrue(loaded["Defect"].Count >= 2, "canonical reopen should restore disconnected merge components");
            AssertTrue(loaded["Defect"].All(segment => segment.ObjectId == mergedObjectId), "reopened components should share merge object id");

            var roundTripData = new CData();
            roundTripData.ConfigureOutputRoot(Path.Combine(root, "roundtrip"));
            roundTripData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            roundTripData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            roundTripData.ProjectSettings.YoloDataset.TestPercent = 0;
            roundTripData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen });
            YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
                "merge.png",
                bitmap,
                new Dictionary<string, List<LabelingSegmentationObject>>
                {
                    ["Defect"] = loaded["Defect"].ToList()
                },
                roundTripData.ClassNamedList,
                roundTripData);
            string roundTripPath = Path.Combine(roundTripData.OutputRootPath, "data", "train", "segments", "merge.json");
            SegmentationAnnotationFile roundTrip =
                JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(roundTripPath));
            AssertTrue(roundTrip.Polygons.All(record => record.ObjectId == mergedObjectId), "resave should preserve merge object id");
            AssertTrue(
                roundTrip.Polygons.All(record => record.LastStructuralOperation == WpfSegmentationMergeService.StructuralOperationName),
                "resave should preserve merge provenance");
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static void TestMixedClassRejection()
    {
        var defect = new CClassItem { Text = "Defect", DrawColor = Color.Red };
        var other = new CClassItem { Text = "Other", DrawColor = Color.Blue };
        var segments = new List<LabelingSegmentationObject>
        {
            new LabelingSegmentationObject(RectanglePoints(1, 1, 8, 8), defect) { ClassName = "Defect" },
            new LabelingSegmentationObject(RectanglePoints(12, 12, 18, 18), other) { ClassName = "Other" }
        };
        bool merged = new WpfSegmentationMergeService().TryMerge(
            segments,
            new[] { 0, 1 },
            new Size(24, 24),
            out _,
            out string error);
        AssertTrue(!merged, "mixed classes must be rejected");
        AssertTrue(error.Contains("같은 클래스", StringComparison.Ordinal), "mixed-class rejection should explain the rule");
        AssertEqual(2, segments.Count);
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
