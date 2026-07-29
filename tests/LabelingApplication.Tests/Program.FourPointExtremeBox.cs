using MahApps.Metro.IconPacks;
using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using OpenVisionLab.ImageCanvas.Canvas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LabelingApplication.Tests;

internal static partial class Program
{
    internal static void TestFourPointExtremeBoxWorkflow()
    {
        TestFourPointExtremeBoxGeometryAndDraftState();
        TestFourPointExtremeBoxViewModelAndRecipePersistence();
        TestFourPointExtremeBoxShellCreatesOneOrdinaryRectangle();
    }

    private static void TestFourPointExtremeBoxGeometryAndDraftState()
    {
        var service = new WpfFourPointBoxService();
        Size imageSize = new Size(100, 80);

        AssertEqual(
            WpfFourPointBoxInputResult.Rejected,
            service.TryAddPoint(new Point(-1, 10), imageSize, out _, out _));
        AssertEqual(0, service.PointCount);

        AssertEqual(
            WpfFourPointBoxInputResult.PointAccepted,
            service.TryAddPoint(new Point(70, 12), imageSize, out _, out _));
        AssertEqual("아래", service.NextRoleName);
        AssertEqual(
            WpfFourPointBoxInputResult.PointAccepted,
            service.TryAddPoint(new Point(20, 62), imageSize, out _, out _));
        AssertEqual(
            WpfFourPointBoxInputResult.PointAccepted,
            service.TryAddPoint(new Point(80, 40), imageSize, out _, out _));
        AssertEqual(
            WpfFourPointBoxInputResult.Completed,
            service.TryAddPoint(new Point(15, 30), imageSize, out Rectangle bounds, out _));
        AssertEqual(new Rectangle(15, 12, 65, 50), bounds);
        AssertEqual(0, service.PointCount);

        AssertTrue(
            WpfFourPointBoxService.TryBuildBounds(
                new[]
                {
                    new Point(99, 60),
                    new Point(0, 10),
                    new Point(90, 79),
                    new Point(10, 0)
                },
                imageSize,
                out Rectangle reversedBounds),
            "reversed extreme positions should still produce deterministic axis-aligned bounds");
        AssertEqual(new Rectangle(10, 10, 80, 50), reversedBounds);

        service.TryAddPoint(new Point(10, 20), imageSize, out _, out _);
        service.TryAddPoint(new Point(20, 20), imageSize, out _, out _);
        service.TryAddPoint(new Point(30, 40), imageSize, out _, out _);
        AssertEqual(
            WpfFourPointBoxInputResult.Rejected,
            service.TryAddPoint(new Point(60, 50), imageSize, out Rectangle degenerate, out _));
        AssertTrue(degenerate.IsEmpty, "a degenerate fourth point must not create geometry");
        AssertEqual(3, service.PointCount);
        AssertTrue(service.RemoveLastPoint(), "Backspace behavior should remove only the latest pending point");
        AssertEqual(2, service.PointCount);
        AssertTrue(service.Reset(), "draft cancellation should clear accepted points");
        AssertEqual(0, service.PointCount);
    }

    private static void TestFourPointExtremeBoxViewModelAndRecipePersistence()
    {
        var viewModel = new WpfCanvasPanelViewModel();
        int changedCount = 0;
        LabelingBoxDrawingMethod changedMethod = LabelingBoxDrawingMethod.TwoPointDrag;
        viewModel.ConfigureBoxDrawingMethod(method =>
        {
            changedCount++;
            changedMethod = method;
        });
        AssertEqual(LabelingBoxDrawingMethod.TwoPointDrag, viewModel.SelectedBoxDrawingMethod.Method);

        viewModel.RestoreBoxDrawingMethod(LabelingBoxDrawingMethod.FourPointExtreme);
        AssertEqual(0, changedCount);
        AssertEqual(LabelingBoxDrawingMethod.FourPointExtreme, viewModel.SelectedBoxDrawingMethod.Method);

        var rectangleTool = new WpfAnnotationToolItem(
            WpfAnnotationTool.Rectangle,
            "박스",
            PackIconMaterialKind.VectorRectangle,
            "객체 박스");
        viewModel.ConfigureAnnotationTools(
            new[] { rectangleTool },
            rectangleTool,
            _ => { });
        AssertEqual(System.Windows.Visibility.Visible, viewModel.BoxDrawingMethodVisibility);
        viewModel.SetFourPointBoxProgress(2);
        AssertEqual(System.Windows.Visibility.Visible, viewModel.FourPointBoxProgressVisibility);
        AssertTrue(
            viewModel.FourPointBoxProgressText.Contains("왼쪽 2/4", StringComparison.Ordinal),
            "four-point progress should expose the next semantic edge");

        WpfBoxDrawingMethodItem twoPoint = viewModel.BoxDrawingMethods.Single(
            item => item.Method == LabelingBoxDrawingMethod.TwoPointDrag);
        viewModel.SelectedBoxDrawingMethod = twoPoint;
        viewModel.BoxDrawingMethodSelectionChangedCommand.Execute(twoPoint);
        AssertEqual(1, changedCount);
        AssertEqual(LabelingBoxDrawingMethod.TwoPointDrag, changedMethod);
        viewModel.RestoreBoxDrawingMethod((LabelingBoxDrawingMethod)999);
        AssertEqual(LabelingBoxDrawingMethod.TwoPointDrag, viewModel.SelectedBoxDrawingMethod.Method);
        AssertEqual(1, changedCount);

        string recipeName = "codex_four_point_box_" + Guid.NewGuid().ToString("N");
        string recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
        string outputRoot = Path.Combine(
            Path.GetTempPath(),
            "OpenVisionLab.LabelingStudio.Tests",
            recipeName);
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.BoxDrawingMethod = LabelingBoxDrawingMethod.FourPointExtreme;
            data.SaveConfig(recipeName, refreshDatasetVersion: false);

            CData loaded = new CData().LoadConfig(recipeName);
            AssertEqual(
                LabelingBoxDrawingMethod.FourPointExtreme,
                loaded.ProjectSettings.BoxDrawingMethod);
            AssertEqual(
                LabelingBoxDrawingMethod.TwoPointDrag,
                new LabelingProjectSettings().BoxDrawingMethod);

            var stale = new LabelingProjectSettings
            {
                BoxDrawingMethod = (LabelingBoxDrawingMethod)999
            };
            stale.EnsureDefaults();
            AssertEqual(LabelingBoxDrawingMethod.TwoPointDrag, stale.BoxDrawingMethod);
        }
        finally
        {
            if (Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }

            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    private static void TestFourPointExtremeBoxShellCreatesOneOrdinaryRectangle()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        var isolatedData = new CData();
        isolatedData.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.DeepSkyBlue });
        CGlobal.Inst.Data = isolatedData;

        WpfLabelingShellWindow window = null;
        Bitmap bitmap = new Bitmap(100, 80);
        using var canvasImage = new OpenCvSharp.Mat(
            80,
            100,
            OpenCvSharp.MatType.CV_8UC3,
            OpenCvSharp.Scalar.All(0));
        try
        {
            window = new WpfLabelingShellWindow();
            window.MainCanvasViewModel.LoadImage(canvasImage, "four-point-test.png");
            SetPrivateField(window, "activeImageSize", new Size(100, 80));
            SetPrivateField(window, "activeImageBitmap", bitmap);
            SetPrivateField(window, "activeAnnotationTool", WpfAnnotationTool.Rectangle);
            window.CanvasPanelViewModel.RestoreBoxDrawingMethod(LabelingBoxDrawingMethod.FourPointExtreme);
            InvokePrivateResult<object>(window, "ApplyRectangleDrawingInputMode");
            AssertTrue(window.MainCanvasViewModel.IsImagePointInputMode, "four-point mode should use image-pixel click input");
            AssertTrue(!window.MainCanvasViewModel.IsTeachingMode, "four-point mode should not arm the drag path");

            InvokeFourPointClick(window, new Point(70, 12));
            InvokeFourPointClick(window, new Point(20, 62));
            InvokeFourPointClick(window, new Point(80, 40));
            List<Rectangle> manualRois = GetPrivateField<List<Rectangle>>(window, "manualRois");
            var undoHistory = GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory");
            AssertEqual(0, manualRois.Count);
            AssertEqual(0, undoHistory.Count);
            AssertEqual(6, window.MainCanvasViewModel.PolygonOverlays.Count);

            InvokeFourPointClick(window, new Point(15, 30));
            AssertEqual(1, manualRois.Count);
            AssertEqual(new Rectangle(15, 12, 65, 50), manualRois[0]);
            AssertEqual(1, undoHistory.Count);
            AssertEqual(0, window.MainCanvasViewModel.PolygonOverlays.Count);

            AssertTrue(
                InvokePrivateResult<bool>(window, "UndoWpfAnnotationHistory"),
                "the completed four-point box should undo as one existing history step");
            AssertEqual(0, manualRois.Count);
            AssertTrue(
                InvokePrivateResult<bool>(window, "RedoWpfAnnotationHistory"),
                "the completed four-point box should redo as one existing history step");
            AssertEqual(1, manualRois.Count);

            InvokeFourPointClick(window, new Point(10, 10));
            AssertTrue(
                InvokePrivateResult<bool>(window, "CancelFourPointBoxDraft", false),
                "right-click/Esc lifecycle cancellation should clear a pending draft");
            AssertEqual(1, manualRois.Count);
            AssertEqual(1, undoHistory.Count);
        }
        finally
        {
            if (window != null)
            {
                SetPrivateField(window, "activeImageBitmap", null);
                SetPrivateField(window, "isApplicationCloseApproved", true);
                window.Close();
            }

            bitmap.Dispose();
            CGlobal.Inst.Data = previousData;
        }
    }

    private static void InvokeFourPointClick(WpfLabelingShellWindow window, Point point)
        => InvokePrivateResult<object>(
            window,
            "MainCanvasViewModel_ImagePointClicked",
            window.MainCanvasViewModel,
            new CanvasImagePointEventArgs(
                CanvasPointerButton.Left,
                1,
                0,
                0,
                point,
                PointF.Empty));
}
