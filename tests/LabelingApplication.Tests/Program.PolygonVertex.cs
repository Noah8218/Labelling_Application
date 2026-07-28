using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using OpenVisionLab.ImageCanvas.Canvas;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class PolygonVertexTests
{
    internal static void TestPolygonVertexWorkflow()
    {
        TestDeterministicGeometryAndInvalidDeletion();
        TestShellHistorySessionProtectionAndCanonicalRoundTrip();
    }

    private static void TestDeterministicGeometryAndInvalidDeletion()
    {
        var imageSize = new Size(64, 64);
        var classItem = new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen };
        var polygon = new LabelingSegmentationObject(
            RectanglePoints(4, 4, 44, 36),
            classItem)
        {
            ClassName = "Defect",
            ObjectId = "vertex-geometry",
            ComponentIndex = 3,
            ZOrder = 7,
            LastStructuralOperation = "Original"
        };

        AssertEqual(4, WpfPolygonAnnotationService.ResolveImageHitTolerance(0.5F));
        AssertEqual(16, WpfPolygonAnnotationService.ResolveImageHitTolerance(2F));
        AssertEqual(
            -1,
            WpfPolygonAnnotationService.FindNearestEdgeIndex(
                polygon,
                new Point(20, 10),
                WpfPolygonAnnotationService.ResolveImageHitTolerance(0.5F),
                out _));
        AssertEqual(
            0,
            WpfPolygonAnnotationService.FindNearestEdgeIndex(
                polygon,
                new Point(20, 10),
                WpfPolygonAnnotationService.ResolveImageHitTolerance(1F),
                out Point projected));
        AssertEqual(new Point(20, 4), projected);

        AssertTrue(
            WpfPolygonAnnotationService.TryInsertPoint(
                polygon,
                new Point(20, 5),
                imageSize,
                maxDistancePixels: 8,
                out int insertedIndex,
                out _,
                out string insertError),
            insertError);
        AssertEqual(1, insertedIndex);
        AssertEqual(new Point(20, 4), polygon.Points[insertedIndex]);
        AssertEqual("vertex-geometry", polygon.ObjectId);
        AssertEqual("Defect", polygon.ClassName);
        AssertEqual(3, polygon.ComponentIndex);
        AssertEqual(7, polygon.ZOrder);
        AssertEqual(WpfPolygonAnnotationService.InsertVertexStructuralOperationName, polygon.LastStructuralOperation);

        var invalidDelete = new LabelingSegmentationObject(
            new[]
            {
                new Point(2, 2),
                new Point(20, 2),
                new Point(11, 11),
                new Point(20, 20)
            },
            classItem)
        {
            ClassName = "Defect",
            ObjectId = "invalid-delete",
            ZOrder = 4,
            LastStructuralOperation = "Original"
        };
        Point[] beforeInvalidDelete = invalidDelete.Points.ToArray();
        AssertTrue(
            !WpfPolygonAnnotationService.TryDeletePoint(
                invalidDelete,
                pointIndex: 1,
                imageSize,
                out _,
                out string invalidError),
            "deletion that collapses the remaining polygon should be rejected");
        AssertTrue(invalidError.Contains("\uBA74\uC801", StringComparison.Ordinal), "invalid deletion should explain the geometry failure");
        AssertTrue(invalidDelete.Points.SequenceEqual(beforeInvalidDelete), "rejected deletion must not mutate points");
        AssertEqual("Original", invalidDelete.LastStructuralOperation);

        var triangle = new LabelingSegmentationObject(
            new[] { new Point(2, 2), new Point(20, 2), new Point(8, 18) },
            classItem);
        AssertTrue(
            !WpfPolygonAnnotationService.TryDeletePoint(
                triangle,
                pointIndex: 0,
                imageSize,
                out _,
                out string triangleError),
            "a triangle vertex cannot be deleted");
        AssertTrue(triangleError.Contains("3", StringComparison.Ordinal), "triangle rejection should explain the three-vertex minimum");
        AssertEqual(3, triangle.Points.Count);
    }

    private static void TestShellHistorySessionProtectionAndCanonicalRoundTrip()
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

        var imageSize = new Size(48, 40);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            segments.Add(new LabelingSegmentationObject(
                RectanglePoints(4, 4, 36, 30),
                defect)
            {
                ClassName = "Defect",
                ObjectId = "vertex-shell",
                ComponentIndex = 2,
                ZOrder = 9,
                LastStructuralOperation = "Original"
            });

            InvokePrivate(window, "RefreshObjectList");
            AssertTrue(window.ObjectReviewViewModel.IsPolygonVertexContextVisible, "selected polygon should reveal vertex tools");
            AssertTrue(window.ObjectReviewViewModel.IsVertexEditEnabled, "selected editable polygon should enable vertex tools");
            AssertTrue(window.FindName("BeginInsertVertexButton") is System.Windows.Controls.Button, "object review should expose vertex insertion");
            AssertTrue(window.FindName("BeginDeleteVertexButton") is System.Windows.Controls.Button, "object review should expose vertex deletion");

            window.ObjectReviewViewModel.BeginInsertVertexCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsVertexEditPending, "insert should arm one canvas click");
            RightClick(window, new Point(18, 4));
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditPending, "right-click should cancel vertex input");
            AssertEqual(0, History(window).Count);
            AssertEqual(4, segments[0].Points.Count);

            window.ObjectReviewViewModel.BeginInsertVertexCommand.Execute(null);
            Click(window, new Point(18, 4));
            AssertEqual(5, segments[0].Points.Count);
            AssertEqual(1, History(window).Count);
            AssertEqual("vertex-shell", segments[0].ObjectId);
            AssertEqual("Defect", segments[0].ClassName);
            AssertEqual(2, segments[0].ComponentIndex);
            AssertEqual(9, segments[0].ZOrder);
            AssertEqual(WpfPolygonAnnotationService.InsertVertexStructuralOperationName, segments[0].LastStructuralOperation);
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditPending, "successful insertion should end point input");

            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "vertex insertion should be one undo step");
            AssertEqual(4, segments[0].Points.Count);
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "vertex insertion should be one redo step");
            AssertEqual(5, segments[0].Points.Count);

            InvokePrivate(window, "RefreshObjectList");
            window.ObjectReviewViewModel.BeginDeleteVertexCommand.Execute(null);
            Click(window, new Point(18, 4));
            AssertEqual(4, segments[0].Points.Count);
            AssertEqual(2, History(window).Count);
            AssertEqual(WpfPolygonAnnotationService.DeleteVertexStructuralOperationName, segments[0].LastStructuralOperation);
            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "vertex deletion should be one undo step");
            AssertEqual(5, segments[0].Points.Count);
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "vertex deletion should be one redo step");
            AssertEqual(4, segments[0].Points.Count);

            SaveAndAssertCanonicalRoundTrip(root, bitmap, imageSize, data, segments);

            InvokePrivate(window, "RefreshObjectList");
            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditEnabled, "full lock should disable vertex editing");
            window.ObjectReviewViewModel.BeginInsertVertexCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditPending, "direct command execution must respect full lock");
            AssertEqual(4, segments[0].Points.Count);
            window.ObjectReviewViewModel.ToggleObjectLockedCommand.Execute(null);

            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditEnabled, "hidden polygon should disable vertex editing");
            window.ObjectReviewViewModel.BeginInsertVertexCommand.Execute(null);
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditPending, "direct command execution must respect hidden state");
            window.ObjectReviewViewModel.ToggleObjectHiddenCommand.Execute(null);

            window.ObjectReviewViewModel.ToggleObjectPinnedCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsVertexEditEnabled, "movement pin should preserve vertex editing");
            window.ObjectReviewViewModel.BeginInsertVertexCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsVertexEditPending, "movement-pinned polygon should allow vertex input");
            RightClick(window, new Point(18, 4));

            byte[] maskData = new byte[imageSize.Width * imageSize.Height];
            maskData[(20 * imageSize.Width) + 40] = 255;
            segments.Add(new LabelingSegmentationObject
            {
                ClassName = "Defect",
                ClassItem = defect,
                ObjectId = "vertex-mask-context",
                MaskData = maskData,
                MaskSize = imageSize,
                MaskBounds = new Rectangle(40, 20, 1, 1)
            });
            InvokePrivateResult<object>(
                window,
                "RefreshObjectListWithSelection",
                WpfObjectReviewItemRef.ManualSegment(0));
            window.ObjectReviewViewModel.BeginInsertVertexCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsVertexEditPending, "polygon should arm before a selection-change cancellation check");
            WpfObjectReviewListItem maskRow = window.ObjectReviewViewModel.Objects.Single(item =>
                item.IsManualSegment && !item.IsManualPolygon);
            window.ObjectReviewViewModel.SelectedObject = maskRow;
            window.ObjectReviewViewModel.ObjectSelectionChangedCommand.Execute(maskRow);
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditPending, "selecting a different object should cancel pending vertex input");
            AssertTrue(!window.ObjectReviewViewModel.IsPolygonVertexContextVisible, "mask selection should hide polygon vertex tools");
            AssertTrue(!window.ObjectReviewViewModel.IsVertexEditEnabled, "mask selection should not enable polygon vertex commands");
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static void SaveAndAssertCanonicalRoundTrip(
        string root,
        Bitmap bitmap,
        Size imageSize,
        CData data,
        IReadOnlyList<LabelingSegmentationObject> segments)
    {
        const string imageName = "polygon-vertex.png";
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = segments.ToList()
            },
            data.ClassNamedList,
            data);
        string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", "polygon-vertex.json");
        SegmentationAnnotationFile saved =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
        AssertEqual(3, saved.Version);
        AssertEqual("vertex-shell", saved.Polygons.Single().ObjectId);
        AssertEqual(9, saved.Polygons.Single().ZOrder);
        AssertEqual(WpfPolygonAnnotationService.DeleteVertexStructuralOperationName, saved.Polygons.Single().LastStructuralOperation);

        IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
            YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                Path.Combine(data.OutputRootPath, "data", "train", "masks", "polygon-vertex.png"),
                data.ClassNamedList,
                imageSize);
        LabelingSegmentationObject reopened = loaded["Defect"].Single();
        AssertEqual("vertex-shell", reopened.ObjectId);
        AssertEqual("Defect", reopened.ClassName);
        AssertEqual(9, reopened.ZOrder);
        AssertEqual(4, reopened.Points.Count);
        AssertEqual(WpfPolygonAnnotationService.DeleteVertexStructuralOperationName, reopened.LastStructuralOperation);

        var roundTripData = new CData();
        roundTripData.ConfigureOutputRoot(Path.Combine(root, "roundtrip"));
        roundTripData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        roundTripData.ProjectSettings.YoloDataset.ValidationPercent = 0;
        roundTripData.ProjectSettings.YoloDataset.TestPercent = 0;
        roundTripData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen });
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject> { reopened }
            },
            roundTripData.ClassNamedList,
            roundTripData);
        SegmentationAnnotationFile resaved = JsonConvert.DeserializeObject<SegmentationAnnotationFile>(
            File.ReadAllText(Path.Combine(
                roundTripData.OutputRootPath,
                "data",
                "train",
                "segments",
                "polygon-vertex.json")));
        AssertEqual("vertex-shell", resaved.Polygons.Single().ObjectId);
        AssertEqual(9, resaved.Polygons.Single().ZOrder);
        AssertEqual(WpfPolygonAnnotationService.DeleteVertexStructuralOperationName, resaved.Polygons.Single().LastStructuralOperation);
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

    private static List<Point> RectanglePoints(int left, int top, int right, int bottom)
        => new List<Point>
        {
            new Point(left, top),
            new Point(right, top),
            new Point(right, bottom),
            new Point(left, bottom)
        };
}
