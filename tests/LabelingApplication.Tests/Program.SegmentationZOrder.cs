using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class SegmentationZOrderTests
{
    internal static void TestZOrderWorkflow()
    {
        TestMovePlanningAndBoundaries();
        TestNewRasterStartsAtFront();
        TestWindowHistoryAndCanonicalRoundTrip();
    }

    private static void TestMovePlanningAndBoundaries()
    {
        var back = Segment("back", 0, 1);
        var middle = Segment("middle", 1, 4);
        var front = Segment("front", 2, 7);
        var segments = new[] { back, middle, front };
        var service = new WpfSegmentationZOrderService();

        AssertPlan(service, segments, 1, WpfSegmentationZOrderMove.SendToBack, 0, "middle", "back", "front");
        AssertPlan(service, segments, 1, WpfSegmentationZOrderMove.SendBackward, 0, "middle", "back", "front");
        AssertPlan(service, segments, 1, WpfSegmentationZOrderMove.BringForward, 2, "back", "front", "middle");
        AssertPlan(service, segments, 1, WpfSegmentationZOrderMove.BringToFront, 2, "back", "front", "middle");

        bool movedBack = service.TryPlanMove(
            segments,
            0,
            WpfSegmentationZOrderMove.SendBackward,
            out _,
            out string backError);
        AssertTrue(!movedBack, "backmost segment should not move backward");
        AssertTrue(backError.Contains("\uAC00\uC7A5 \uB4A4", StringComparison.Ordinal), "back boundary should explain its state");

        bool movedFront = service.TryPlanMove(
            segments,
            2,
            WpfSegmentationZOrderMove.BringToFront,
            out _,
            out string frontError);
        AssertTrue(!movedFront, "frontmost segment should not move forward");
        AssertTrue(frontError.Contains("\uAC00\uC7A5 \uC55E", StringComparison.Ordinal), "front boundary should explain its state");
        AssertSequence(segments, "back", "middle", "front");
        AssertEqual(0, back.ZOrder);
        AssertEqual(1, middle.ZOrder);
        AssertEqual(2, front.ZOrder);
    }

    private static void TestNewRasterStartsAtFront()
    {
        var existing = Segment("existing", 4, 2);
        var segments = new List<LabelingSegmentationObject> { existing };
        var maskClass = new CClassItem { Text = "Mask", DrawColor = Color.Red };
        bool painted = new WpfMaskAnnotationService().Paint(
            segments,
            new[] { new Point(10, 10) },
            2,
            new Size(24, 24),
            maskClass,
            out LabelingSegmentationObject created,
            out _);
        AssertTrue(painted, "new raster segment should be created");
        AssertEqual(2, segments.Count);
        AssertTrue(ReferenceEquals(created, segments[1]), "new raster should append to the saved-object stack");
        AssertEqual(5, created.ZOrder);
    }

    private static void TestWindowHistoryAndCanonicalRoundTrip()
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
        var other = new CClassItem { Text = "Other", DrawColor = Color.DeepSkyBlue };
        data.ClassNamedList.Add(defect);
        data.ClassNamedList.Add(other);
        CGlobal.Inst.Data = data;

        var imageSize = new Size(40, 32);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            LabelingSegmentationObject back = Segment("back", 0, 1, defect);
            LabelingSegmentationObject middle = Segment("middle", 1, 4, other);
            LabelingSegmentationObject front = Segment("front", 2, 7, defect);
            segments.AddRange(new[] { back, middle, front });

            InvokePrivate(window, "RefreshObjectList");
            WpfObjectReviewListItem middleRow = window.ObjectReviewViewModel.Objects
                .Single(item => item.IsManualSegment && item.SourceIndex == 1);
            window.ObjectReviewViewModel.SelectedObject = middleRow;
            window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(middleRow);

            AssertTrue(
                window.FindName("BringSegmentationToFrontButton") is System.Windows.Controls.Button,
                "object review should expose the bring-to-front button");
            AssertTrue(window.ObjectReviewViewModel.IsBringToFrontEnabled, "middle segment should move to front");
            AssertTrue(window.ObjectReviewViewModel.IsSendToBackEnabled, "middle segment should move to back");
            window.ObjectReviewViewModel.SendToBackCommand.Execute(null);

            AssertSequence(segments, "middle", "back", "front");
            AssertZOrders(segments);
            AssertEqual(4, segments[0].Points[0].X);
            AssertEqual(WpfSegmentationZOrderService.StructuralOperationName, back.LastStructuralOperation);
            AssertEqual(WpfSegmentationZOrderService.StructuralOperationName, middle.LastStructuralOperation);
            AssertEqual("Original", front.LastStructuralOperation);
            AssertEqual(1, GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count);
            AssertTrue(!window.ObjectReviewViewModel.IsSendToBackEnabled, "backmost selected segment should disable backward moves");

            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "z-order change should be one undo step");
            AssertSequence(segments, "back", "middle", "front");
            AssertZOrders(segments);
            AssertTrue(segments.All(segment => segment.LastStructuralOperation == "Original"), "undo should restore prior provenance");
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "z-order change should be one redo step");
            AssertSequence(segments, "middle", "back", "front");
            AssertZOrders(segments);

            int historyCount = GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count;
            window.ObjectReviewViewModel.SendToBackCommand.Execute(null);
            AssertEqual(
                historyCount,
                GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count);
            AssertSequence(segments, "middle", "back", "front");

            SaveAndAssertCanonicalRoundTrip(root, bitmap, imageSize, data, segments);
            segments.Clear();
            int loadedCount = InvokePrivateResult<int>(
                window,
                "LoadSavedSegmentationAnnotationsForActiveImage",
                Path.Combine(root, "zorder.png"));
            AssertEqual(3, loadedCount);
            AssertSequence(segments, "middle", "back", "front");
            AssertZOrders(segments);
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static void AssertPlan(
        WpfSegmentationZOrderService service,
        IReadOnlyList<LabelingSegmentationObject> segments,
        int selectedIndex,
        WpfSegmentationZOrderMove move,
        int expectedSelectedIndex,
        params string[] expectedIds)
    {
        AssertTrue(
            service.TryPlanMove(segments, selectedIndex, move, out WpfSegmentationZOrderResult result, out string error),
            $"z-order plan should succeed: {move} / {error}");
        AssertEqual(expectedSelectedIndex, result.SelectedIndex);
        AssertSequence(result.OrderedSegments, expectedIds);
        AssertSequence(segments, "back", "middle", "front");
    }

    private static void SaveAndAssertCanonicalRoundTrip(
        string root,
        Bitmap bitmap,
        Size imageSize,
        CData data,
        IReadOnlyList<LabelingSegmentationObject> segments)
    {
        const string imageName = "zorder.png";
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            segments
                .GroupBy(segment => segment.ClassName)
                .ToDictionary(group => group.Key, group => group.ToList()),
            data.ClassNamedList,
            data);
        string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "zorder.json");
        string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", "zorder.png");
        SegmentationAnnotationFile saved =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
        AssertEqual(3, saved.Version);
        AssertEqual(3, saved.Polygons.Count);
        Dictionary<string, SegmentationPolygonRecord> savedById =
            saved.Polygons.ToDictionary(record => record.ObjectId);
        AssertEqual(0, savedById["middle"].ZOrder);
        AssertEqual(1, savedById["back"].ZOrder);
        AssertEqual(2, savedById["front"].ZOrder);

        IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
            YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                maskPath,
                data.ClassNamedList,
                imageSize);
        List<LabelingSegmentationObject> orderedLoaded = loaded.Values
            .SelectMany(items => items)
            .OrderBy(segment => segment.ZOrder)
            .ToList();
        AssertSequence(orderedLoaded, "middle", "back", "front");
        AssertEqual("Other", orderedLoaded[0].ClassName);
        AssertEqual("Defect", orderedLoaded[1].ClassName);

        var roundTripData = new CData();
        roundTripData.ConfigureOutputRoot(Path.Combine(root, "roundtrip"));
        roundTripData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        roundTripData.ProjectSettings.YoloDataset.ValidationPercent = 0;
        roundTripData.ProjectSettings.YoloDataset.TestPercent = 0;
        roundTripData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen });
        roundTripData.ClassNamedList.Add(new CClassItem { Text = "Other", DrawColor = Color.DeepSkyBlue });
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            orderedLoaded
                .GroupBy(segment => segment.ClassName)
                .ToDictionary(group => group.Key, group => group.ToList()),
            roundTripData.ClassNamedList,
            roundTripData);
        string roundTripPath = Path.Combine(
            roundTripData.OutputRootPath,
            "data",
            "train",
            "segments",
            "zorder.json");
        SegmentationAnnotationFile roundTrip =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(roundTripPath));
        Dictionary<string, SegmentationPolygonRecord> roundTripById =
            roundTrip.Polygons.ToDictionary(record => record.ObjectId);
        AssertEqual(0, roundTripById["middle"].ZOrder);
        AssertEqual(1, roundTripById["back"].ZOrder);
        AssertEqual(2, roundTripById["front"].ZOrder);
    }

    private static LabelingSegmentationObject Segment(
        string objectId,
        int zOrder,
        int offset,
        CClassItem classItem = null)
        => new LabelingSegmentationObject(
            new[]
            {
                new Point(offset, offset),
                new Point(offset + 12, offset),
                new Point(offset + 12, offset + 12),
                new Point(offset, offset + 12)
            },
            classItem ?? new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen })
        {
            ClassName = classItem?.Text ?? "Defect",
            ObjectId = objectId,
            ZOrder = zOrder,
            LastStructuralOperation = "Original"
        };

    private static void AssertZOrders(IReadOnlyList<LabelingSegmentationObject> segments)
    {
        for (int index = 0; index < segments.Count; index++)
        {
            AssertEqual(index, segments[index].ZOrder);
        }
    }

    private static void AssertSequence(
        IEnumerable<LabelingSegmentationObject> segments,
        params string[] expectedIds)
        => AssertTrue(
            segments.Select(segment => segment.ObjectId).SequenceEqual(expectedIds),
            $"segment order mismatch: expected {string.Join(",", expectedIds)}");
}
