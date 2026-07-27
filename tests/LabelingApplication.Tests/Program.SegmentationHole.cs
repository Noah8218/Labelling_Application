using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using OpenVisionLab.ImageCanvas.Canvas;
using System.Drawing;
using System.IO;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class SegmentationHoleTests
{
    internal static void TestHoleWorkflow()
    {
        TestPolygonHoleAddRemoveHistoryAndCanonicalRoundTrip();
        TestInvalidRasterHoleEditsDoNotMutateSource();
    }

    private static void TestPolygonHoleAddRemoveHistoryAndCanonicalRoundTrip()
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

        var imageSize = new Size(36, 28);
        using var bitmap = new Bitmap(imageSize.Width, imageSize.Height);
        var window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", imageSize);
            SetPrivateField(window, "activeImageBitmap", bitmap);
            List<LabelingSegmentationObject> segments =
                GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            segments.Add(new LabelingSegmentationObject(
                RectanglePoints(2, 2, 32, 24),
                defect)
            {
                ClassName = "Defect",
                ObjectId = "hole-source",
                ZOrder = 6
            });

            InvokePrivate(window, "RefreshObjectList");
            AssertTrue(window.ObjectReviewViewModel.IsHoleEditEnabled, "selected polygon should enable hole editing");
            AssertTrue(
                window.FindName("BeginAddHoleButton") is System.Windows.Controls.Button,
                "object review should expose the add-hole button");
            window.ObjectReviewViewModel.BeginAddHoleCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsHoleEditPending, "add-hole should arm polygon input");
            InvokePrivateResult<object>(
                window,
                "MainCanvasViewModel_ImagePointClicked",
                window.MainCanvasViewModel,
                new CanvasImagePointEventArgs(
                    CanvasPointerButton.Right,
                    1,
                    0,
                    0,
                    new Point(10, 8),
                    PointF.Empty));
            AssertTrue(!window.ObjectReviewViewModel.IsHoleEditPending, "right-click should cancel hole input");
            AssertEqual(1, segments.Count);
            AssertEqual(
                0,
                GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count);
            window.ObjectReviewViewModel.BeginAddHoleCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsHoleEditPending, "add-hole should re-arm after cancellation");

            Click(window, new Point(10, 8));
            Click(window, new Point(21, 8));
            Click(window, new Point(21, 17));
            Click(window, new Point(10, 17));
            Click(window, new Point(10, 17), clicks: 2);

            AssertEqual(1, segments.Count);
            LabelingSegmentationObject withHole = segments[0];
            AssertTrue(withHole.IsRasterMask, "polygon hole edit should publish canonical raster geometry");
            AssertEqual("hole-source", withHole.ObjectId);
            AssertEqual(6, withHole.ZOrder);
            AssertEqual(WpfSegmentationHoleService.AddStructuralOperationName, withHole.LastStructuralOperation);
            AssertTrue(IsMaskSet(withHole, 5, 5), "pixels outside the hole should remain");
            AssertTrue(!IsMaskSet(withHole, 15, 12), "drawn inner polygon should become a hole");
            AssertTrue(!window.ObjectReviewViewModel.IsHoleEditPending, "successful hole add should end input mode");

            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "hole add should be one undo step");
            AssertTrue(!segments[0].IsRasterMask, "undo should restore the original polygon representation");
            AssertEqual("hole-source", segments[0].ObjectId);
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "hole add should be one redo step");
            AssertTrue(segments[0].IsRasterMask, "redo should restore raster hole geometry");
            AssertTrue(!IsMaskSet(segments[0], 15, 12), "redo should restore the empty hole");

            SaveAndAssertCanonicalRoundTrip(
                root,
                "hole-add.png",
                bitmap,
                imageSize,
                data,
                segments,
                WpfSegmentationHoleService.AddStructuralOperationName);

            InvokePrivate(window, "RefreshObjectList");
            window.ObjectReviewViewModel.BeginRemoveHoleCommand.Execute(null);
            AssertTrue(window.ObjectReviewViewModel.IsHoleEditPending, "fill-hole should arm point input");
            Click(window, new Point(15, 12));

            AssertEqual(1, segments.Count);
            LabelingSegmentationObject filled = segments[0];
            AssertEqual("hole-source", filled.ObjectId);
            AssertEqual(WpfSegmentationHoleService.RemoveStructuralOperationName, filled.LastStructuralOperation);
            AssertTrue(IsMaskSet(filled, 15, 12), "clicked enclosed hole should be filled");
            AssertTrue(InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"), "hole fill should be one undo step");
            AssertTrue(!IsMaskSet(segments[0], 15, 12), "undo should restore the hole");
            AssertTrue(InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"), "hole fill should be one redo step");
            AssertTrue(IsMaskSet(segments[0], 15, 12), "redo should fill the hole again");

            SaveAndAssertCanonicalRoundTrip(
                root,
                "hole-remove.png",
                bitmap,
                imageSize,
                data,
                segments,
                WpfSegmentationHoleService.RemoveStructuralOperationName);
        }
        finally
        {
            SetPrivateField(window, "activeImageBitmap", null);
            window.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static void TestInvalidRasterHoleEditsDoNotMutateSource()
    {
        var imageSize = new Size(24, 24);
        byte[] mask = new byte[imageSize.Width * imageSize.Height];
        FillMaskRectangle(mask, imageSize, new Rectangle(4, 4, 16, 16));
        var source = new LabelingSegmentationObject
        {
            ClassName = "Defect",
            ObjectId = "invalid-source",
            MaskData = mask,
            MaskSize = imageSize,
            MaskBounds = new Rectangle(4, 4, 16, 16)
        };
        var service = new WpfSegmentationHoleService();

        bool add = service.TryAddHole(
            source,
            RectanglePoints(2, 2, 8, 8),
            imageSize,
            out _,
            out string addError);
        AssertTrue(!add, "partially external hole polygon should be rejected");
        AssertTrue(addError.Contains("\uC548\uCABD", StringComparison.Ordinal), "invalid add should explain the interior rule");
        AssertTrue(IsMaskSet(source, 5, 5), "invalid add should not mutate source pixels");
        AssertEqual("invalid-source", source.ObjectId);

        bool removeForeground = service.TryRemoveHole(
            source,
            new Point(8, 8),
            imageSize,
            out _,
            out string removeError);
        AssertTrue(!removeForeground, "foreground click should not fill a hole");
        AssertTrue(removeError.Contains("\uBE48 \uB0B4\uBD80", StringComparison.Ordinal), "invalid fill should explain the empty-hole rule");
        AssertTrue(IsMaskSet(source, 8, 8), "invalid fill should not mutate source pixels");

        byte[] openMask = mask.ToArray();
        for (int y = 4; y <= 12; y++)
        {
            openMask[(y * imageSize.Width) + 12] = 0;
        }
        for (int y = 10; y <= 14; y++)
        {
            for (int x = 10; x <= 14; x++)
            {
                openMask[(y * imageSize.Width) + x] = 0;
            }
        }
        var openSource = new LabelingSegmentationObject
        {
            ClassName = "Defect",
            ObjectId = "open-background",
            MaskData = openMask,
            MaskSize = imageSize,
            MaskBounds = new Rectangle(4, 4, 16, 16)
        };
        bool removeOpen = service.TryRemoveHole(
            openSource,
            new Point(11, 13),
            imageSize,
            out _,
            out string openError);
        AssertTrue(!removeOpen, "background channel connected to the exterior is not an internal hole");
        AssertTrue(openError.Contains("\uC678\uBD80", StringComparison.Ordinal), "open background rejection should explain exterior connectivity");
        AssertTrue(!IsMaskSet(openSource, 11, 13), "rejected open background should stay unchanged");
    }

    private static void SaveAndAssertCanonicalRoundTrip(
        string root,
        string imageName,
        Bitmap bitmap,
        Size imageSize,
        CData data,
        IReadOnlyList<LabelingSegmentationObject> segments,
        string expectedOperation)
    {
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = segments.ToList()
            },
            data.ClassNamedList,
            data);
        string baseName = Path.GetFileNameWithoutExtension(imageName);
        string segmentPath = Path.Combine(data.OutputRootPath, "data", "train", "segments", baseName + ".json");
        string maskPath = Path.Combine(data.OutputRootPath, "data", "train", "masks", baseName + ".png");
        SegmentationAnnotationFile saved =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(segmentPath));
        AssertEqual(3, saved.Version);
        AssertTrue(saved.Polygons.Count > 0, "canonical v3 should contain at least one geometry record");
        AssertTrue(saved.Polygons.All(record => record.ObjectId == "hole-source"), "canonical save should preserve the edited object id");
        AssertTrue(saved.Polygons.All(record => record.LastStructuralOperation == expectedOperation), "canonical save should preserve hole provenance");

        IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
            YoloSegmentationAnnotationService.LoadSegmentationObjects(
                segmentPath,
                maskPath,
                data.ClassNamedList,
                imageSize);
        AssertTrue(loaded["Defect"].Count > 0, "canonical reopen should restore geometry");
        AssertTrue(loaded["Defect"].All(segment => segment.ObjectId == "hole-source"), "canonical reopen should preserve the edited object id");
        AssertTrue(loaded["Defect"].All(segment => segment.LastStructuralOperation == expectedOperation), "canonical reopen should preserve hole provenance");

        var roundTripData = new CData();
        roundTripData.ConfigureOutputRoot(Path.Combine(root, "roundtrip-" + baseName));
        roundTripData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        roundTripData.ProjectSettings.YoloDataset.ValidationPercent = 0;
        roundTripData.ProjectSettings.YoloDataset.TestPercent = 0;
        roundTripData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.LimeGreen });
        YoloSegmentationAnnotationService.SaveSegmentationAnnotations(
            imageName,
            bitmap,
            new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = loaded["Defect"].ToList()
            },
            roundTripData.ClassNamedList,
            roundTripData);
        string roundTripPath = Path.Combine(
            roundTripData.OutputRootPath,
            "data",
            "train",
            "segments",
            baseName + ".json");
        SegmentationAnnotationFile roundTrip =
            JsonConvert.DeserializeObject<SegmentationAnnotationFile>(File.ReadAllText(roundTripPath));
        AssertTrue(roundTrip.Polygons.All(record => record.ObjectId == "hole-source"), "canonical resave should preserve the edited object id");
        AssertTrue(roundTrip.Polygons.All(record => record.LastStructuralOperation == expectedOperation), "canonical resave should preserve hole provenance");
    }

    private static void Click(WpfLabelingShellWindow window, Point point, int clicks = 1)
    {
        InvokePrivateResult<object>(
            window,
            "MainCanvasViewModel_ImagePointClicked",
            window.MainCanvasViewModel,
            new CanvasImagePointEventArgs(
                CanvasPointerButton.Left,
                clicks,
                0,
                0,
                point,
                PointF.Empty));
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
