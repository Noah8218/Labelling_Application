using MvcVisionSystem;
using OpenVisionLab.ImageCanvas.ViewModels;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class ImageDisplayAdjustmentTests
{
    internal static void TestDisplayOnlyAdjustmentContract()
    {
        TestServiceOutputAndSourceImmutability();
        TestShellKeepsCanonicalImageAndOverlayGeometry();
    }

    private static void TestServiceOutputAndSourceImmutability()
    {
        using Bitmap source = CreateLowContrastFixture(160, 96);
        string sourceHash = ComputeBitmapHash(source);
        var service = new WpfImageDisplayAdjustmentService();

        using Bitmap defaultCopy = service.CreateAdjustedCopy(
            source,
            new WpfImageDisplayAdjustmentOptions());
        AssertEqual(sourceHash, ComputeBitmapHash(defaultCopy));
        AssertTrue(!ReferenceEquals(source, defaultCopy), "default display must still use an owned copy");

        var options = new WpfImageDisplayAdjustmentOptions
        {
            Brightness = 18,
            Contrast = 1.35D,
            Gamma = 1.2D,
            Invert = true,
            EqualizeHistogram = true
        };
        using Bitmap adjusted = service.CreateAdjustedCopy(source, options);
        AssertTrue(
            !string.Equals(sourceHash, ComputeBitmapHash(adjusted), StringComparison.Ordinal),
            "non-default display controls should change the display copy");
        AssertEqual(sourceHash, ComputeBitmapHash(source));
        AssertEqual(source.Size, adjusted.Size);

        using Bitmap equalized = service.CreateAdjustedCopy(
            source,
            new WpfImageDisplayAdjustmentOptions { EqualizeHistogram = true });
        AssertTrue(
            GetLuminanceSpan(equalized) > GetLuminanceSpan(source),
            "histogram equalization should expand the low-contrast fixture");
        AssertEqual(sourceHash, ComputeBitmapHash(source));

        using var large = new Bitmap(1920, 1080, PixelFormat.Format24bppRgb);
        using (Graphics graphics = Graphics.FromImage(large))
        {
            graphics.Clear(Color.FromArgb(88, 104, 120));
            graphics.FillEllipse(Brushes.LightGray, 420, 180, 900, 650);
        }
        Stopwatch elapsed = Stopwatch.StartNew();
        using Bitmap largeAdjusted = service.CreateAdjustedCopy(large, options);
        elapsed.Stop();
        AssertTrue(
            elapsed.Elapsed <= TimeSpan.FromMilliseconds(1000),
            $"1920x1080 display adjustment exceeded 1000 ms: {elapsed.Elapsed.TotalMilliseconds:0.0} ms");
        Console.WriteLine($"DISPLAY_ADJUSTMENT_1920X1080_MS={elapsed.Elapsed.TotalMilliseconds:0.0}");
    }

    private static void TestShellKeepsCanonicalImageAndOverlayGeometry()
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
        string imagePath = Path.Combine(root, "display-source.png");
        string secondImagePath = Path.Combine(root, "display-source-next.png");
        using (Bitmap fixture = CreateLowContrastFixture(96, 64))
        {
            fixture.Save(imagePath, ImageFormat.Png);
        }
        using (Bitmap fixture = CreateLowContrastFixture(112, 72))
        {
            fixture.RotateFlip(RotateFlipType.RotateNoneFlipX);
            fixture.Save(secondImagePath, ImageFormat.Png);
        }
        string fileHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(imagePath)));
        string secondFileHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(secondImagePath)));
        var data = new CData();
        data.ConfigureOutputRoot(Path.Combine(root, "dataset"));
        CGlobal.Inst.Data = data;

        var window = new WpfLabelingShellWindow();
        try
        {
            AssertTrue(
                window.TryLoadImage(
                    imagePath,
                    populateQueue: false,
                    refreshQueueDetails: false,
                    refreshActiveStatus: false,
                    appendLoadLog: false),
                "display fixture should load");
            Bitmap canonical = GetPrivateField<Bitmap>(window, "activeImageBitmap");
            string canonicalHash = ComputeBitmapHash(canonical);
            string dirtyReason = GetPrivateField<string>(window, "annotationDirtyReason");
            int historyCount =
                GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count;

            Point[] expectedPoints =
            {
                new Point(11, 9),
                new Point(78, 12),
                new Point(70, 50),
                new Point(15, 53)
            };
            window.MainCanvasViewModel.SetPolygonOverlays(
                new[]
                {
                    new RoiImageCanvasPolygonOverlay(
                        expectedPoints,
                        "DISPLAY CONTRACT",
                        Color.LimeGreen,
                        isClosed: true,
                        isSelected: true)
                });

            AssertTrue(
                window.FindName("DisplayAdjustmentCanvasButton") is System.Windows.Controls.Button,
                "canvas header should expose one compact display-adjustment entry");
            AssertTrue(
                window.FindName("DisplayAdjustmentPopup") is System.Windows.Controls.Primitives.Popup,
                "display controls should stay inside a popup instead of the annotation rail");

            window.CanvasPanelViewModel.DisplayBrightness = 24;
            window.CanvasPanelViewModel.DisplayContrastPercent = 145D;
            window.CanvasPanelViewModel.DisplayGamma = 1.35D;
            window.CanvasPanelViewModel.IsDisplayHistogramEqualized = true;
            InvokePrivate(window, "ApplyDisplayAdjustmentNow");

            Bitmap canonicalAfter = GetPrivateField<Bitmap>(window, "activeImageBitmap");
            AssertTrue(ReferenceEquals(canonical, canonicalAfter), "display adjustment must retain the canonical bitmap instance");
            AssertEqual(canonicalHash, ComputeBitmapHash(canonicalAfter));
            AssertEqual(fileHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(imagePath))));
            AssertEqual(dirtyReason, GetPrivateField<string>(window, "annotationDirtyReason"));
            AssertEqual(
                historyCount,
                GetPrivateField<List<WpfAnnotationHistorySnapshot>>(window, "undoAnnotationHistory").Count);
            AssertTrue(window.CanvasPanelViewModel.IsDisplayAdjustmentActive, "non-default display controls should report an active state");
            AssertEqual(1, window.MainCanvasViewModel.PolygonOverlays.Count);
            AssertTrue(
                expectedPoints.SequenceEqual(window.MainCanvasViewModel.PolygonOverlays[0].ImagePoints),
                "replacing only the base texture must preserve overlay image coordinates");

            AssertTrue(
                window.TryLoadImage(
                    secondImagePath,
                    populateQueue: false,
                    refreshQueueDetails: false,
                    refreshActiveStatus: false,
                    appendLoadLog: false),
                "display settings should survive queue-style image navigation");
            PumpWpfDispatcher(TimeSpan.FromMilliseconds(220));
            AssertEqual(24, window.CanvasPanelViewModel.DisplayBrightness);
            AssertEqual(145D, window.CanvasPanelViewModel.DisplayContrastPercent);
            AssertEqual(1.35D, window.CanvasPanelViewModel.DisplayGamma);
            AssertTrue(
                window.CanvasPanelViewModel.IsDisplayHistogramEqualized,
                "equalization state should remain active across image navigation");
            AssertEqual(
                secondFileHash,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(secondImagePath))));
            Bitmap secondCanonical = GetPrivateField<Bitmap>(window, "activeImageBitmap");
            string secondCanonicalHash = ComputeBitmapHash(secondCanonical);

            window.CanvasPanelViewModel.ResetDisplayAdjustment();
            InvokePrivate(window, "ApplyDisplayAdjustmentNow");
            AssertTrue(!window.CanvasPanelViewModel.IsDisplayAdjustmentActive, "reset should restore the unadjusted display");
            AssertEqual(secondCanonicalHash, ComputeBitmapHash(secondCanonical));
            AssertEqual(
                secondFileHash,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(secondImagePath))));
        }
        finally
        {
            Bitmap active = GetPrivateField<Bitmap>(window, "activeImageBitmap");
            SetPrivateField(window, "activeImageBitmap", null);
            active?.Dispose();
            window.Close();
            CGlobal.Inst.Data = previousData;
            Directory.Delete(root, recursive: true);
        }
    }

    private static Bitmap CreateLowContrastFixture(int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int value = 92 + ((x * 22 / Math.Max(1, width - 1)) + (y * 10 / Math.Max(1, height - 1)));
                bitmap.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        Math.Clamp(value + 5, 0, 255),
                        Math.Clamp(value, 0, 255),
                        Math.Clamp(value - 4, 0, 255)));
            }
        }

        return bitmap;
    }

    private static int GetLuminanceSpan(Bitmap bitmap)
    {
        int minimum = 255;
        int maximum = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                int luminance = (77 * color.R + 150 * color.G + 29 * color.B) >> 8;
                minimum = Math.Min(minimum, luminance);
                maximum = Math.Max(maximum, luminance);
            }
        }

        return maximum - minimum;
    }

    private static string ComputeBitmapHash(Bitmap bitmap)
    {
        var bytes = new byte[bitmap.Width * bitmap.Height * 4];
        int offset = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                int argb = bitmap.GetPixel(x, y).ToArgb();
                bytes[offset++] = (byte)argb;
                bytes[offset++] = (byte)(argb >> 8);
                bytes[offset++] = (byte)(argb >> 16);
                bytes[offset++] = (byte)(argb >> 24);
            }
        }

        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
