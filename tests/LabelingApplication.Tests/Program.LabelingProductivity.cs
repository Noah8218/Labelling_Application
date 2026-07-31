using MahApps.Metro.IconPacks;
using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Input;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Visibility = System.Windows.Visibility;

namespace LabelingApplication.Tests;

internal static class LabelingProductivityTests
{
    internal static void TestLabelingProductivity()
    {
        TestShortcutPolicy();
        TestCanvasRepeatAndClassState();
        TestDuplicateGeometry();
        TestSourceIntegration();
    }

    private static void TestShortcutPolicy()
    {
        AssertShortcut(Key.V, ModifierKeys.None, WpfAnnotationShortcutKind.SelectTool, WpfAnnotationTool.Select);
        AssertShortcut(Key.R, ModifierKeys.None, WpfAnnotationShortcutKind.SelectTool, WpfAnnotationTool.Rectangle);
        AssertShortcut(Key.P, ModifierKeys.None, WpfAnnotationShortcutKind.SelectTool, WpfAnnotationTool.Polygon);
        AssertShortcut(Key.B, ModifierKeys.None, WpfAnnotationShortcutKind.SelectTool, WpfAnnotationTool.Brush);
        AssertShortcut(Key.E, ModifierKeys.None, WpfAnnotationShortcutKind.SelectTool, WpfAnnotationTool.Eraser);
        AssertShortcut(Key.H, ModifierKeys.None, WpfAnnotationShortcutKind.SelectTool, WpfAnnotationTool.PanZoom);
        AssertShortcut(Key.N, ModifierKeys.None, WpfAnnotationShortcutKind.RepeatLast);
        AssertShortcut(Key.F1, ModifierKeys.None, WpfAnnotationShortcutKind.ToggleShortcutHelp);
        AssertShortcut(Key.D0, ModifierKeys.None, WpfAnnotationShortcutKind.OpenClassCatalog);
        AssertShortcut(Key.NumPad0, ModifierKeys.None, WpfAnnotationShortcutKind.OpenClassCatalog);
        AssertShortcut(Key.D, ModifierKeys.Control, WpfAnnotationShortcutKind.DuplicateSelected);

        for (int index = 0; index < 9; index++)
        {
            WpfAnnotationShortcut topRow = WpfAnnotationProductivityService.ResolveShortcut(
                (Key)((int)Key.D1 + index),
                ModifierKeys.None);
            AssertEqual(WpfAnnotationShortcutKind.SelectClass, topRow.Kind, $"top-row class shortcut {index + 1}");
            AssertEqual(index, topRow.ClassIndex, $"top-row class index {index + 1}");

            WpfAnnotationShortcut numPad = WpfAnnotationProductivityService.ResolveShortcut(
                (Key)((int)Key.NumPad1 + index),
                ModifierKeys.None);
            AssertEqual(WpfAnnotationShortcutKind.SelectClass, numPad.Kind, $"numpad class shortcut {index + 1}");
            AssertEqual(index, numPad.ClassIndex, $"numpad class index {index + 1}");
        }

        AssertEqual(
            WpfAnnotationShortcutKind.None,
            WpfAnnotationProductivityService.ResolveShortcut(Key.R, ModifierKeys.Shift).Kind,
            "drawing shortcuts with modifiers");
        AssertTrue(
            WpfAnnotationProductivityService.ShortcutHelpText.Contains(
                "자동 윤곽 켜기 → R 박스 → 후보 검토",
                StringComparison.Ordinal)
            && WpfAnnotationProductivityService.ShortcutHelpText.Contains(
                "이전/현재 후보 비교 → 확정 또는 스킵",
                StringComparison.Ordinal)
            && WpfAnnotationProductivityService.ShortcutHelpText.Contains(
                "후보 생성·복원은 저장하지 않으며",
                StringComparison.Ordinal),
            "F1 help should expose the current automatic-contour review and save contract");
    }

    private static void TestCanvasRepeatAndClassState()
    {
        var viewModel = new WpfCanvasPanelViewModel();
        CClassItem[] classes = Enumerable.Range(1, 11)
            .Select(index => new CClassItem
            {
                Text = $"Class{index:00}",
                DrawColor = Color.FromArgb(20 + index, 80, 160)
            })
            .ToArray();
        viewModel.SetLabelClasses(classes);

        AssertEqual("1 Class01", viewModel.LabelClasses[0].DisplayText, "first numbered class");
        AssertEqual("9 Class09", viewModel.LabelClasses[8].DisplayText, "ninth numbered class");
        AssertEqual("Class10", viewModel.LabelClasses[9].DisplayText, "larger-catalog class fallback presentation");
        AssertTrue(viewModel.TrySelectLabelClassByShortcut(8), "class shortcut 9 should select a class");
        AssertEqual("Class09", viewModel.SelectedLabelClass.Text, "class shortcut 9 selection");
        AssertTrue(!viewModel.TrySelectLabelClassByShortcut(9), "class shortcut must stop after 9");

        var rectangle = new WpfAnnotationToolItem(
            WpfAnnotationTool.Rectangle,
            "박스",
            PackIconMaterialKind.VectorRectangle,
            "박스를 그립니다.");
        var polygon = new WpfAnnotationToolItem(
            WpfAnnotationTool.Polygon,
            "폴리곤",
            PackIconMaterialKind.VectorPolygon,
            "폴리곤을 그립니다.");
        viewModel.ConfigureAnnotationTools(new[] { rectangle, polygon }, rectangle, _ => { });
        viewModel.SetSelectedAnnotationTool(polygon);

        AssertTrue(
            viewModel.TryGetRepeatSelection(out WpfAnnotationTool repeatTool, out string repeatClass),
            "last drawing tool and class should be repeatable");
        AssertEqual(WpfAnnotationTool.Polygon, repeatTool, "repeat drawing tool");
        AssertEqual("Class09", repeatClass, "repeat drawing class");
        AssertTrue(
            polygon.ToolTip.Contains("단축키 P", StringComparison.Ordinal),
            "tool tooltip should expose its shortcut");

        AssertEqual(Visibility.Collapsed, viewModel.ShortcutHelpVisibility, "shortcut help initial state");
        viewModel.ToggleShortcutHelp();
        AssertEqual(Visibility.Visible, viewModel.ShortcutHelpVisibility, "shortcut help open state");
        viewModel.ToggleShortcutHelpCommand.Execute(null);
        AssertEqual(Visibility.Collapsed, viewModel.ShortcutHelpVisibility, "shortcut help command close state");
    }

    private static void TestDuplicateGeometry()
    {
        Rectangle sourceRectangle = new Rectangle(82, 84, 18, 16);
        Rectangle duplicateRectangle = WpfAnnotationProductivityService.CreateOffsetRectangle(
            sourceRectangle,
            new Size(100, 100));
        AssertEqual(new Rectangle(70, 72, 18, 16), duplicateRectangle, "edge-clamped rectangle duplicate");
        AssertEqual(new Rectangle(82, 84, 18, 16), sourceRectangle, "rectangle source remains unchanged");

        var sourcePolygon = new LabelingSegmentationObject(
            new[] { new Point(2, 2), new Point(8, 2), new Point(8, 8), new Point(2, 8) },
            new CClassItem { Text = "Defect", DrawColor = Color.OrangeRed })
        {
            CutoutPolygons = new List<List<Point>>
            {
                new List<Point> { new Point(4, 4), new Point(5, 4), new Point(5, 5) }
            }
        };
        LabelingSegmentationObject polygonDuplicate = WpfAnnotationProductivityService.CreateOffsetSegment(
            sourcePolygon,
            new Size(40, 40),
            new WpfMaskAnnotationService());
        AssertTrue(!ReferenceEquals(sourcePolygon, polygonDuplicate), "polygon duplicate should be a new object");
        AssertTrue(!ReferenceEquals(sourcePolygon.ClassItem, polygonDuplicate.ClassItem), "polygon class metadata should be cloned");
        AssertEqual("Defect", polygonDuplicate.ClassName, "polygon duplicate class");
        AssertEqual(new Point(14, 14), polygonDuplicate.Points[0], "polygon duplicate offset");
        AssertEqual(new Point(2, 2), sourcePolygon.Points[0], "polygon source remains unchanged");
        AssertEqual(new Point(16, 16), polygonDuplicate.CutoutPolygons[0][0], "polygon cutout offset");

        var maskData = new byte[20 * 20];
        for (int y = 5; y < 8; y++)
        {
            for (int x = 5; x < 8; x++)
            {
                maskData[(y * 20) + x] = 255;
            }
        }

        var sourceMask = new LabelingSegmentationObject
        {
            ClassName = "Scratch",
            ClassItem = new CClassItem { Text = "Scratch", DrawColor = Color.Lime },
            MaskData = maskData,
            MaskSize = new Size(20, 20),
            MaskBounds = new Rectangle(5, 5, 3, 3)
        };
        byte[] sourceMaskSnapshot = sourceMask.MaskData.ToArray();
        LabelingSegmentationObject maskDuplicate = WpfAnnotationProductivityService.CreateOffsetSegment(
            sourceMask,
            new Size(20, 20),
            new WpfMaskAnnotationService());
        AssertTrue(maskDuplicate.IsRasterMask, "raster duplicate should remain a raster mask");
        AssertTrue(!ReferenceEquals(sourceMask.MaskData, maskDuplicate.MaskData), "raster mask buffer should be cloned");
        AssertEqual("Scratch", maskDuplicate.ClassName, "raster duplicate class");
        AssertEqual(new Rectangle(17, 17, 3, 3), maskDuplicate.Bounds, "raster duplicate offset");
        AssertTrue(sourceMaskSnapshot.SequenceEqual(sourceMask.MaskData), "raster source buffer remains unchanged");
    }

    private static void TestSourceIntegration()
    {
        string root = TestSupport.FindRepositoryRoot();
        string shellInput = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.ShellInputCommands.cs"));
        string duplicateOwner = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfLabelingShellWindow.AnnotationToolSelectionCommands.cs"));
        string canvasXaml = File.ReadAllText(Path.Combine(
            root,
            "0. UI",
            "9) WPF",
            "Views",
            "WpfCanvasPanel.xaml"));

        AssertTrue(
            shellInput.IndexOf("IsTextEditingElement(e.OriginalSource)", StringComparison.Ordinal)
                < shellInput.IndexOf("ResolveShortcut(e.Key, e.Modifiers)", StringComparison.Ordinal),
            "text editing suppression must run before drawing shortcut resolution");
        AssertTrue(
            shellInput.Contains("ResolveSelectableAnnotationTool(shortcut.Tool)", StringComparison.Ordinal),
            "tool shortcuts must use purpose-filtered tool selection");
        AssertTrue(
            duplicateOwner.Contains("RegisterAnnotationHistoryBeforeChange(\"라벨 복제\")", StringComparison.Ordinal),
            "duplicate must register one annotation history snapshot");
        AssertTrue(
            duplicateOwner.Contains("QueueActiveImageQueueStatusRefresh", StringComparison.Ordinal),
            "duplicate must refresh the canonical image completion state");
        AssertTrue(
            canvasXaml.Contains("AutomationId=\"CanvasShortcutHelpButton\"", StringComparison.Ordinal)
                && canvasXaml.Contains("Visibility=\"{Binding ShortcutHelpVisibility}\"", StringComparison.Ordinal),
            "canvas should expose an accessible shortcut help surface");
    }

    private static void AssertShortcut(
        Key key,
        ModifierKeys modifiers,
        WpfAnnotationShortcutKind expectedKind,
        WpfAnnotationTool expectedTool = WpfAnnotationTool.Select)
    {
        WpfAnnotationShortcut shortcut = WpfAnnotationProductivityService.ResolveShortcut(key, modifiers);
        AssertEqual(expectedKind, shortcut.Kind, $"shortcut kind for {modifiers}+{key}");
        if (expectedKind == WpfAnnotationShortcutKind.SelectTool)
        {
            AssertEqual(expectedTool, shortcut.Tool, $"shortcut tool for {modifiers}+{key}");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
        }
    }
}

internal static partial class Program
{
    private static int RunExeLabelingProductivitySmoke(string[] args)
    {
        Process process = null;
        try
        {
            string root = FindRepositoryRoot();
            string exePath = Path.GetFullPath(GetArgumentValue(
                args,
                "--exe",
                Path.Combine(root, "artifacts", "run", "Debug", "OpenVisionLab.LabelingStudio.exe")));
            string outputPath = Path.GetFullPath(GetArgumentValue(
                args,
                "--output",
                Path.Combine(
                    root,
                    "artifacts",
                    "ui",
                    "labeling-productivity-p0a-20260727",
                    "after-actual-exe-1920x1080.png")));
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException("EXE productivity smoke target was not found.", exePath);
            }

            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });
            AssertTrue(process != null, "failed to start labeling productivity smoke EXE");

            IntPtr handle = WaitForMainWindowHandle(process, TimeSpan.FromSeconds(25));
            AssertTrue(handle != IntPtr.Zero, "labeling productivity smoke window did not appear");
            PlaceExeSmokeWindowOnLeftmostMonitor(handle);
            BringNativeWindowToFront(handle);
            var automationRoot = RefreshAutomationRoot(process, handle);
            WaitForAutomationText(automationRoot, "캔버스", TimeSpan.FromSeconds(10));
            System.Windows.Automation.AutomationElement shortcutHelpButton =
                FindAutomationElementByAutomationId(automationRoot, "CanvasShortcutHelpButton");
            AssertTrue(
                IsAutomationElementVisible(shortcutHelpButton),
                "current EXE shortcut help button was not visible");
            BringNativeWindowToFront(handle);
            NativeClick(GetAutomationCenter(shortcutHelpButton));
            Thread.Sleep(400);
            automationRoot = RefreshAutomationRoot(process, handle, bringToFront: false);
            CaptureAutomationRoot(automationRoot, outputPath);
            AssertTrue(
                IsAutomationElementVisible(FindAutomationElementByAutomationId(
                    automationRoot,
                    "CanvasShortcutHelpText")),
                "current EXE did not show the labeling shortcut help card");
            Console.WriteLine($"EXE labeling productivity smoke captured: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL EXE labeling productivity smoke: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
        finally
        {
            CloseExeSmokeProcess(process);
        }
    }
}
