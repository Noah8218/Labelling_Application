using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.DrawObject;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using OpenVisionLab.ImageCanvas.Canvas;
using OpenVisionLab.ImageCanvas.CanvasShapes;
using OpenVisionLab.ImageCanvas.Model;
using OpenVisionLab.ImageCanvas.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CvMat = OpenCvSharp.Mat;
using CvMatType = OpenCvSharp.MatType;
using CvScalar = OpenCvSharp.Scalar;

namespace LabelingApplication.Tests;

using static LabelingApplication.Tests.TestSupport;
using static LabelingApplication.Tests.TemplateAutoLabelFixtures;

internal static class TemplateAutoLabelTests
{
    internal static void TestTemplateMatchingBatchAutoLabelSaveAndSkip()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Clear();
            var partClass = new CClassItem { Text = "Part", DrawColor = Color.Blue };
            data.ClassNamedList.Add(partClass);
            data.EnsureYoloOutputDirectories();

            string sourceDirectory = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceDirectory);
            string activeImagePath = Path.Combine(sourceDirectory, "active-template.png");
            string targetImagePath = Path.Combine(sourceDirectory, "batch-target.png");
            string existingImagePath = Path.Combine(sourceDirectory, "skip-existing.png");

            using (Bitmap activeImage = CreateTemplateBatchAutoLabelImage(new Point(12, 12)))
            using (Bitmap targetImage = CreateTemplateBatchAutoLabelImage(new Point(56, 34)))
            using (Bitmap existingImage = CreateTemplateBatchAutoLabelImage(new Point(72, 48)))
            {
                activeImage.Save(activeImagePath, System.Drawing.Imaging.ImageFormat.Png);
                targetImage.Save(targetImagePath, System.Drawing.Imaging.ImageFormat.Png);
                existingImage.Save(existingImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            string existingLabelPath = Path.Combine(root, "data", "train", "labels", "skip-existing.txt");
            File.WriteAllText(existingLabelPath, "sentinel");
            data.LastSelectImageName = Path.GetFileNameWithoutExtension(activeImagePath);
            data.LastSelectImagePath = activeImagePath;

            var service = new TemplateMatchingBatchAutoLabelService();
            IReadOnlyList<string> queue = service.BuildUnlabeledImagePathQueue(
                new[] { activeImagePath, targetImagePath, targetImagePath, existingImagePath },
                data,
                activeImagePath);

            AssertEqual(1, queue.Count);
            AssertEqual(targetImagePath, queue[0]);

            using Bitmap templateImage = CreateTemplateBatchAutoLabelPattern();
            var options = new TemplateMatchingAutoLabelOptions
            {
                MinimumScore = 0.7D,
                MaximumCandidates = 3,
                ExcludeSourceRegion = false
            };

            TemplateMatchingBatchAutoLabelItemResult saved = service.MatchAndSaveImage(
                targetImagePath,
                templateImage,
                partClass,
                partClass.Text,
                data,
                options,
                CancellationToken.None);

            AssertTrue(saved.Saved, $"template batch target should be saved: {saved.Message}");
            AssertTrue(saved.CandidateCount > 0, "template batch target should save at least one matched box");

            string targetLabelPath = Path.Combine(root, "data", "train", "labels", "batch-target.txt");
            string targetPngPath = Path.Combine(root, "data", "train", "images", "batch-target.png");
            string targetJpegPath = Path.Combine(root, "data", "train", "images", "batch-target.jpeg");
            AssertTrue(File.Exists(targetLabelPath), "template batch target label was not created");
            AssertTrue(File.Exists(targetPngPath), "template batch should preserve the target source PNG extension");
            AssertTrue(!File.Exists(targetJpegPath), "template batch should not create a duplicate JPEG copy for a PNG source");

            IReadOnlyDictionary<string, List<Rectangle>> loaded = YoloAnnotationService.LoadAnnotationRectanglesForImage(
                targetPngPath,
                data.ClassNamedList,
                data,
                new Size(120, 90));

            AssertTrue(loaded.TryGetValue("Part", out List<Rectangle> loadedPartRects) && loadedPartRects.Count > 0, "saved template batch label should reload as the selected class");

            TemplateMatchingBatchAutoLabelItemResult skipped = service.MatchAndSaveImage(
                existingImagePath,
                templateImage,
                partClass,
                partClass.Text,
                data,
                options,
                CancellationToken.None);

            AssertTrue(!skipped.Saved, "template batch should not save an image that already has a label file");
            AssertEqual("label file already exists", skipped.Message);
            AssertEqual("sentinel", File.ReadAllText(existingLabelPath));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestTemplateMatchingBatchAutoLabelSavesSegmentationArtifacts()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Clear();
            var partClass = new CClassItem { Text = "Part", DrawColor = Color.Blue };
            data.ClassNamedList.Add(partClass);
            data.EnsureYoloOutputDirectories();

            string sourceDirectory = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceDirectory);
            string activeImagePath = Path.Combine(sourceDirectory, "active-template.png");
            string targetImagePath = Path.Combine(sourceDirectory, "batch-seg-target.png");

            using (Bitmap activeImage = CreateTemplateBatchAutoLabelImage(new Point(12, 12)))
            using (Bitmap targetImage = CreateTemplateBatchAutoLabelImage(new Point(56, 34)))
            {
                activeImage.Save(activeImagePath, System.Drawing.Imaging.ImageFormat.Png);
                targetImage.Save(targetImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            string staleBoxLabelPath = Path.Combine(root, "data", "train", "labels", "batch-seg-target.txt");
            File.WriteAllText(staleBoxLabelPath, "0 0.5 0.5 0.25 0.25");
            data.LastSelectImageName = Path.GetFileNameWithoutExtension(activeImagePath);
            data.LastSelectImagePath = activeImagePath;

            var service = new TemplateMatchingBatchAutoLabelService();
            IReadOnlyList<string> queue = service.BuildUnlabeledImagePathQueue(
                new[] { activeImagePath, targetImagePath },
                data,
                activeImagePath);
            AssertEqual(1, queue.Count);
            AssertEqual(targetImagePath, queue[0]);

            using Bitmap templateImage = CreateTemplateBatchAutoLabelPattern();
            var options = new TemplateMatchingAutoLabelOptions
            {
                MinimumScore = 0.7D,
                MaximumCandidates = 3,
                ExcludeSourceRegion = false
            };

            TemplateMatchingBatchAutoLabelItemResult saved = service.MatchAndSaveImage(
                targetImagePath,
                templateImage,
                partClass,
                partClass.Text,
                data,
                options,
                CancellationToken.None,
                new Rectangle(12, 12, 24, 18),
                new[]
                {
                    new Point(15, 16),
                    new Point(30, 18),
                    new Point(20, 27)
                });

            AssertTrue(saved.Saved, $"SEG template batch target should be saved: {saved.Message}");
            AssertTrue(saved.CandidateCount > 0, "SEG template batch target should save at least one matched segment");

            string segmentPath = Path.Combine(root, "data", "train", "segments", "batch-seg-target.json");
            string maskPath = Path.Combine(root, "data", "train", "masks", "batch-seg-target.png");
            string imageCopyPath = Path.Combine(root, "data", "train", "images", "batch-seg-target.png");
            AssertTrue(File.Exists(segmentPath), "SEG template batch target segment json was not created");
            AssertTrue(File.Exists(maskPath), "SEG template batch target mask png was not created");
            AssertTrue(File.Exists(imageCopyPath), "SEG template batch should still copy the source image into the dataset split");
            AssertEqual(string.Empty, File.ReadAllText(staleBoxLabelPath).Trim());

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    segmentPath,
                    data.ClassNamedList,
                    new Size(120, 90));
            AssertTrue(loaded.TryGetValue("Part", out List<LabelingSegmentationObject> loadedSegments) && loadedSegments.Count > 0,
                "saved SEG template batch label should reload as segmentation objects");
            AssertTrue(loadedSegments.All(segment => segment.Points.Count >= 3 && !segment.Bounds.IsEmpty),
                "saved SEG template batch segments should have polygon geometry");
            AssertTrue(loadedSegments.All(segment => segment.Points.Count == 3),
                "SEG template batch should transfer the selected source polygon shape instead of saving rectangle fallbacks");

            YoloImageLabelStatus labelStatus = YoloImageLabelStatusService.Build(targetImagePath, new Size(120, 90), data);
            AssertTrue(labelStatus.HasObjects, "SEG queue status should count segment json objects, not stale box txt labels");
            AssertEqual(loadedSegments.Count, labelStatus.ObjectCount);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestTemplateMatchingBatchAutoLabelTransfersRasterMaskShape()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            data.ClassNamedList.Clear();
            var partClass = new CClassItem { Text = "Part", DrawColor = Color.Blue };
            data.ClassNamedList.Add(partClass);
            data.EnsureYoloOutputDirectories();

            string sourceDirectory = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceDirectory);
            string activeImagePath = Path.Combine(sourceDirectory, "active-raster-template.png");
            string targetImagePath = Path.Combine(sourceDirectory, "batch-raster-target.png");

            using (Bitmap activeImage = CreateTemplateBatchAutoLabelImage(new Point(12, 12)))
            using (Bitmap targetImage = CreateTemplateBatchAutoLabelImage(new Point(56, 34)))
            {
                activeImage.Save(activeImagePath, System.Drawing.Imaging.ImageFormat.Png);
                targetImage.Save(targetImagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            var sourceMaskSize = new Size(120, 90);
            var sourceMaskBounds = new Rectangle(12, 12, 24, 18);
            byte[] sourceMaskData = CreateTemplateSourceLShapeMask(sourceMaskSize, sourceMaskBounds);

            using Bitmap templateImage = CreateTemplateBatchAutoLabelPattern();
            var service = new TemplateMatchingBatchAutoLabelService();
            TemplateMatchingBatchAutoLabelItemResult saved = service.MatchAndSaveImage(
                targetImagePath,
                templateImage,
                partClass,
                partClass.Text,
                data,
                new TemplateMatchingAutoLabelOptions
                {
                    MinimumScore = 0.7D,
                    MaximumCandidates = 1,
                    ExcludeSourceRegion = false
                },
                CancellationToken.None,
                sourceMaskData: sourceMaskData,
                sourceMaskSize: sourceMaskSize,
                sourceMaskBounds: sourceMaskBounds);

            AssertTrue(saved.Saved, $"SEG raster template batch target should be saved: {saved.Message}");

            string segmentPath = Path.Combine(root, "data", "train", "segments", "batch-raster-target.json");
            string maskPath = Path.Combine(root, "data", "train", "masks", "batch-raster-target.png");
            AssertTrue(File.Exists(segmentPath), "SEG raster template batch target segment json was not created");
            AssertTrue(File.Exists(maskPath), "SEG raster template batch target mask png was not created");

            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> loaded =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    segmentPath,
                    data.ClassNamedList,
                    sourceMaskSize);
            AssertTrue(loaded.TryGetValue("Part", out List<LabelingSegmentationObject> loadedSegments) && loadedSegments.Count > 0,
                "saved SEG raster template batch label should reload as segmentation objects");
            AssertTrue(loadedSegments.Any(segment => segment.Points.Count > 4),
                "SEG raster template batch should store the transferred mask outline, not only a rectangle fallback");

            Rectangle bounds = loadedSegments[0].Bounds;
            AssertTrue(bounds.Width > 8 && bounds.Height > 8, "transferred raster mask bounds should be large enough to verify shape holes");
            using var savedMask = new Bitmap(maskPath);
            AssertTrue(savedMask.GetPixel(bounds.Left + 2, bounds.Top + 2).R > 0, "transferred raster mask left bar should be filled");
            AssertEqual(0, savedMask.GetPixel(bounds.Right - 3, bounds.Top + 2).R);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestTemplateMatchingAutoLabelGuide()
    {
        string root = FindRepositoryRoot();
        var viewModel = new WpfTemplateMatchingAutoLabelViewModel();
        using var activeImage = new Bitmap(40, 40);
        var presentationService = new WpfTemplateMatchingAutoLabelPresentationService();
        AssertTrue(
            presentationService.BuildTemplateRegisteredStatus("Part", new Rectangle(1, 2, 30, 40)).Contains("Part 30x40", StringComparison.Ordinal),
            "template presentation service should format the registered template size and class");
        AssertTrue(
            presentationService.BuildApplyResultStatus(2, 2).Contains("라벨 저장", StringComparison.Ordinal),
            "template presentation service should direct the operator to save current-image draft labels");
        AssertTrue(
            presentationService.BuildBatchCompletionCommandStatus(false, 1, 2, 3, 4).Contains("저장 1장", StringComparison.Ordinal),
            "template presentation service should summarize batch auto-save counts");
        AssertTrue(
            presentationService.BuildGuideBody("템플릿 안내").Contains("사용 순서", StringComparison.Ordinal),
            "template presentation service should own the reusable guide step text");

        var noImageHost = new TemplateAutoLabelGuideHost
        {
            HasActiveAutoLabelImageValue = false,
            ActiveAutoLabelImageValue = activeImage
        };
        viewModel.ConfigureHost(noImageHost);
        viewModel.RunCurrentImage();
        AssertEqual(1, noImageHost.GuideCount);
        AssertTrue(noImageHost.LastGuideTitle.Contains("\uC774\uBBF8\uC9C0", StringComparison.Ordinal), "template guide should name the missing image");
        AssertTrue(noImageHost.LastGuideMessage.Contains("\uC0AC\uC6A9 \uC21C\uC11C", StringComparison.Ordinal), "template guide should include direct usage steps");
        AssertTrue(noImageHost.LastGuideMessage.Contains("현재 이미지 라벨 초안 생성", StringComparison.Ordinal), "template guide should name the current-image draft-label action");
        AssertTrue(noImageHost.LastGuideMessage.Contains("전체 이미지 자동 저장", StringComparison.Ordinal), "template guide should name the batch auto-save action");
        AssertTrue(noImageHost.LastGuideMessage.Contains("라벨 초안", StringComparison.Ordinal), "template guide should distinguish template drafts from AI candidates");
        AssertTrue(noImageHost.LastGuideMessage.Contains("바로 저장", StringComparison.Ordinal), "template guide should explain that batch mode saves unlabeled images directly");
        AssertTrue(noImageHost.LastGuideMessage.Contains("\uAC80\uD1A0", StringComparison.Ordinal), "template guide should include the review step");
        AssertTrue(noImageHost.LastGuideMessage.Contains("\uB77C\uBCA8 \uC800\uC7A5", StringComparison.Ordinal), "template guide should include the final save step");
        AssertTrue(noImageHost.LastGlobalStatus.Contains("\uC774\uBBF8\uC9C0", StringComparison.Ordinal), "template guide should update visible inference status");

        var noSourceHost = new TemplateAutoLabelGuideHost
        {
            HasActiveAutoLabelImageValue = true,
            ActiveAutoLabelImageValue = activeImage,
            ActiveAutoLabelImagePathValue = "no-source.png",
            HasTemplateSource = false
        };
        viewModel.ConfigureHost(noSourceHost);
        viewModel.RunCurrentImage();
        AssertEqual(1, noSourceHost.GuideCount);
        AssertTrue(noSourceHost.LastGuideTitle.Contains("\uB77C\uBCA8", StringComparison.Ordinal), "template guide should name the missing source label");
        AssertTrue(noSourceHost.LastGuideMessage.Contains("\uAC1D\uCCB4 \uAC80\uD1A0", StringComparison.Ordinal), "template guide should tell the operator where to select the source box");

        viewModel.RunBatch();
        AssertEqual(2, noSourceHost.GuideCount);
        AssertTrue(noSourceHost.LastGuideMessage.Contains("\uB77C\uBCA8 \uC5C6\uB294", StringComparison.Ordinal), "template batch guide should explain the unlabeled queue target");

        using var templateSourceImage = CreateTemplateCurrentImageSource();
        var sourceHost = new TemplateAutoLabelGuideHost
        {
            HasActiveAutoLabelImageValue = true,
            ActiveAutoLabelImageValue = templateSourceImage,
            ActiveAutoLabelImagePathValue = "template-source.png",
            HasTemplateSource = true,
            TemplateSourceBounds = new Rectangle(8, 10, 24, 22)
        };
        viewModel.ConfigureHost(sourceHost);
        viewModel.RunCurrentImage();
        AssertEqual(0, sourceHost.GuideCount);
        AssertTrue(!sourceHost.ApplyCandidatesCalled, "template source click should register the template instead of applying it to the same image");
        AssertTrue(sourceHost.LastGlobalStatus.Contains("\uD15C\uD50C\uB9BF \uB4F1\uB85D", StringComparison.Ordinal), "template source click should show the registered state");
        AssertTrue(sourceHost.LastGlobalStatus.Contains("라벨 초안 생성", StringComparison.Ordinal), "template source registration should point to the next draft-label generation step");

        using var templateTargetImage = CreateTemplateTargetImage();
        var targetHost = new TemplateAutoLabelGuideHost
        {
            HasActiveAutoLabelImageValue = true,
            ActiveAutoLabelImageValue = templateTargetImage,
            ActiveAutoLabelImagePathValue = "template-target.png",
            HasTemplateSource = false
        };
        viewModel.ConfigureHost(targetHost);
        viewModel.RunCurrentImage();
        AssertTrue(targetHost.ApplyCandidatesCalled, "registered template should be applied on a different image without requiring a target-side source box");
        AssertTrue(targetHost.LastApplySucceeded, "registered template apply should not be marked as failed");
        AssertTrue(targetHost.LastCandidateCount > 0, "registered template should find the shifted target pattern");
        AssertTrue(targetHost.LastCandidates.Any(candidate => Math.Abs(candidate.X - 44) <= 4 && Math.Abs(candidate.Y - 42) <= 4),
            "registered template should find the target pattern near the shifted location");
        AssertTrue(targetHost.LastGlobalStatus.Contains("템플릿 라벨 초안", StringComparison.Ordinal), "registered template apply should report draft-label creation");
        AssertTrue(targetHost.LastGlobalStatus.Contains("\uB77C\uBCA8 \uC800\uC7A5", StringComparison.Ordinal), "registered template apply should direct the operator to save labels after review");
        AssertTrue(!targetHost.LastGlobalStatus.Contains("AI \uD6C4\uBCF4", StringComparison.Ordinal), "template apply status should not look like AI-candidate review");

        string batchRoot = CreateTempRoot();
        try
        {
            string imageRoot = Path.Combine(batchRoot, "images");
            string outputRoot = Path.Combine(batchRoot, "dataset");
            Directory.CreateDirectory(imageRoot);

            string batchSourcePath = Path.Combine(imageRoot, "template-batch-source.png");
            string batchTargetPath = Path.Combine(imageRoot, "template-batch-target.png");
            using (Bitmap batchSourceImage = CreateTemplateCurrentImageSource())
            using (Bitmap batchTargetImage = CreateTemplateTargetImage())
            {
                batchSourceImage.Save(batchSourcePath, System.Drawing.Imaging.ImageFormat.Png);
                batchTargetImage.Save(batchTargetPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            var batchData = new CData();
            batchData.ConfigureOutputRoot(outputRoot);
            batchData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            batchData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            batchData.ProjectSettings.YoloDataset.TestPercent = 0;
            batchData.ProjectSettings.PythonModel.ImageRootPath = imageRoot;
            batchData.ClassNamedList.Clear();
            batchData.ClassNamedList.Add(new CClassItem { Text = "Part", DrawColor = Color.Blue });
            batchData.EnsureYoloOutputDirectories();

            var batchViewModel = new WpfTemplateMatchingAutoLabelViewModel();
            using var batchTemplateSourceImage = CreateTemplateCurrentImageSource();
            var batchSourceHost = new TemplateAutoLabelGuideHost
            {
                HasActiveAutoLabelImageValue = true,
                ActiveAutoLabelImageValue = batchTemplateSourceImage,
                ActiveAutoLabelImagePathValue = batchSourcePath,
                HasTemplateSource = true,
                TemplateSourceBounds = new Rectangle(8, 10, 24, 22),
                TemplateSourceSegmentPoints = new[]
                {
                    new Point(10, 12),
                    new Point(28, 15),
                    new Point(18, 28)
                },
                AutoLabelDataValue = batchData
            };
            batchViewModel.ConfigureHost(batchSourceHost);
            batchViewModel.RunCurrentImage();

            using var batchActiveImage = CreateTemplateTargetImage();
            var batchHost = new TemplateAutoLabelGuideHost
            {
                HasActiveAutoLabelImageValue = true,
                ActiveAutoLabelImageValue = batchActiveImage,
                ActiveAutoLabelImagePathValue = batchTargetPath,
                HasTemplateSource = false,
                AutoLabelDataValue = batchData,
                AllQueueItems = new[]
                {
                    WpfImageQueueItem.CreateShell(batchSourcePath),
                    WpfImageQueueItem.CreateShell(batchTargetPath)
                }
            };

            batchViewModel.ConfigureHost(batchHost);
            batchViewModel.RunBatch();
            AssertTrue(WaitUntilWpf(() => batchHost.BatchCompleted, TimeSpan.FromSeconds(5)), "registered template batch should complete");
            AssertEqual(1, batchHost.StartedBatchTotalCount);
            AssertEqual(1, batchHost.BatchCompletedCount);
            AssertEqual(1, batchHost.BatchResults.Count);
            AssertTrue(batchHost.BatchResults[0].Saved, $"registered template batch should save the target label: {batchHost.BatchResults[0].Message}");

            string batchTargetLabelPath = Path.Combine(outputRoot, "data", "train", "labels", "template-batch-target.txt");
            string batchSourceLabelPath = Path.Combine(outputRoot, "data", "train", "labels", "template-batch-source.txt");
            string batchTargetSegmentPath = Path.Combine(outputRoot, "data", "train", "segments", "template-batch-target.json");
            AssertTrue(File.Exists(batchTargetLabelPath), "registered template batch should create a label file for the unlabeled target image");
            AssertTrue(!File.Exists(batchSourceLabelPath), "registered template batch should not relabel the registered source image");
            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> batchSegments =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    batchTargetSegmentPath,
                    batchData.ClassNamedList,
                    new Size(80, 80));
            AssertTrue(batchSegments.TryGetValue("Part", out List<LabelingSegmentationObject> transferredSegments) && transferredSegments.Count > 0,
                "registered template batch should save a SEG artifact for the target image");
            AssertTrue(transferredSegments.All(segment => segment.Points.Count == 3),
                "registered template batch should transfer the registered source polygon shape");
        }
        finally
        {
            DeleteTempRoot(batchRoot);
        }

        string maskBatchRoot = CreateTempRoot();
        try
        {
            string imageRoot = Path.Combine(maskBatchRoot, "images");
            string outputRoot = Path.Combine(maskBatchRoot, "dataset");
            Directory.CreateDirectory(imageRoot);

            string batchSourcePath = Path.Combine(imageRoot, "template-mask-source.png");
            string batchTargetPath = Path.Combine(imageRoot, "template-mask-target.png");
            using (Bitmap batchSourceImage = CreateTemplateCurrentImageSource())
            using (Bitmap batchTargetImage = CreateTemplateTargetImage())
            {
                batchSourceImage.Save(batchSourcePath, System.Drawing.Imaging.ImageFormat.Png);
                batchTargetImage.Save(batchTargetPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            var batchData = new CData();
            batchData.ConfigureOutputRoot(outputRoot);
            batchData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            batchData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            batchData.ProjectSettings.YoloDataset.TestPercent = 0;
            batchData.ProjectSettings.PythonModel.ImageRootPath = imageRoot;
            batchData.ClassNamedList.Clear();
            batchData.ClassNamedList.Add(new CClassItem { Text = "Part", DrawColor = Color.Blue });
            batchData.EnsureYoloOutputDirectories();

            var maskBatchViewModel = new WpfTemplateMatchingAutoLabelViewModel();
            var sourceBounds = new Rectangle(8, 10, 24, 22);
            using var batchTemplateSourceImage = CreateTemplateCurrentImageSource();
            var batchSourceHost = new TemplateAutoLabelGuideHost
            {
                HasActiveAutoLabelImageValue = true,
                ActiveAutoLabelImageValue = batchTemplateSourceImage,
                ActiveAutoLabelImagePathValue = batchSourcePath,
                HasTemplateSource = true,
                TemplateSourceBounds = sourceBounds,
                TemplateSourceMaskData = CreateTemplateSourceLShapeMask(new Size(80, 80), sourceBounds),
                TemplateSourceMaskSize = new Size(80, 80),
                TemplateSourceMaskBounds = sourceBounds,
                AutoLabelDataValue = batchData
            };
            maskBatchViewModel.ConfigureHost(batchSourceHost);
            maskBatchViewModel.RunCurrentImage();

            using var batchActiveImage = CreateTemplateTargetImage();
            var batchHost = new TemplateAutoLabelGuideHost
            {
                HasActiveAutoLabelImageValue = true,
                ActiveAutoLabelImageValue = batchActiveImage,
                ActiveAutoLabelImagePathValue = batchTargetPath,
                HasTemplateSource = false,
                AutoLabelDataValue = batchData,
                AllQueueItems = new[]
                {
                    WpfImageQueueItem.CreateShell(batchSourcePath),
                    WpfImageQueueItem.CreateShell(batchTargetPath)
                }
            };

            maskBatchViewModel.ConfigureHost(batchHost);
            maskBatchViewModel.RunBatch();
            AssertTrue(WaitUntilWpf(() => batchHost.BatchCompleted, TimeSpan.FromSeconds(5)), "registered raster template batch should complete");
            AssertEqual(1, batchHost.BatchResults.Count);
            AssertTrue(batchHost.BatchResults[0].Saved, $"registered raster template batch should save the target label: {batchHost.BatchResults[0].Message}");

            string batchTargetSegmentPath = Path.Combine(outputRoot, "data", "train", "segments", "template-mask-target.json");
            string batchTargetMaskPath = Path.Combine(outputRoot, "data", "train", "masks", "template-mask-target.png");
            IReadOnlyDictionary<string, List<LabelingSegmentationObject>> batchSegments =
                YoloSegmentationAnnotationService.LoadSegmentationObjects(
                    batchTargetSegmentPath,
                    batchData.ClassNamedList,
                    new Size(80, 80));
            AssertTrue(batchSegments.TryGetValue("Part", out List<LabelingSegmentationObject> transferredSegments) && transferredSegments.Count > 0,
                "registered raster template batch should save a target SEG artifact");
            AssertTrue(transferredSegments.Any(segment => segment.Points.Count > 4),
                "registered raster template batch should preserve a non-rectangular mask outline");
            Rectangle bounds = transferredSegments[0].Bounds;
            using var savedMask = new Bitmap(batchTargetMaskPath);
            AssertTrue(savedMask.GetPixel(bounds.Left + 2, bounds.Top + 2).R > 0, "registered raster template target mask should keep the left bar");
            AssertEqual(0, savedMask.GetPixel(bounds.Right - 3, bounds.Top + 2).R);
        }
        finally
        {
            DeleteTempRoot(maskBatchRoot);
        }

        using var noMatchImage = CreateSolidBitmap(40, 40, Color.FromArgb(230, 230, 230));
        var noCandidateHost = new TemplateAutoLabelGuideHost
        {
            HasActiveAutoLabelImageValue = true,
            ActiveAutoLabelImageValue = noMatchImage,
            ActiveAutoLabelImagePathValue = "template-no-match.png",
            HasTemplateSource = false
        };
        viewModel.ConfigureHost(noCandidateHost);
        viewModel.RunCurrentImage();
        AssertTrue(noCandidateHost.ApplyCandidatesCalled, "registered template no-match result should still clear stale candidate state");
        AssertTrue(noCandidateHost.LastApplySucceeded, "registered template no-match result should not be marked as a failed detection");
        AssertEqual(0, noCandidateHost.LastCandidateCount);
        AssertTrue(noCandidateHost.LastGlobalStatus.Contains("\uCC3E\uC9C0 \uBABB\uD588", StringComparison.Ordinal), "registered template no-match result should explain that no target position was found");
        AssertTrue(noCandidateHost.LastGlobalIsWarning, "registered template no-match result should use the warning status style");

        string projectSource = File.ReadAllText(Path.Combine(root, "OpenVisionLab.LabelingStudio.csproj"));
        string solutionSource = File.ReadAllText(Path.Combine(root, "OpenVisionLab.LabelingStudio.sln"));
        string templateCommandsSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "Views", "WpfLabelingShellWindow.TemplateMatchingCommands.cs"));
        string templateViewModelSource = File.ReadAllText(Path.Combine(root, "0. UI", "9) WPF", "ViewModels", "Labeling", "WpfTemplateMatchingAutoLabelViewModel.cs"));
        string templatePresentationPath = Path.Combine(root, "0. UI", "9) WPF", "Services", "Detection", "WpfTemplateMatchingAutoLabelPresentationService.cs");
        string dialogControlPath = Path.Combine(root, "OpenVisionLab", "Library", "OpenVisionLab.Wpf.MessageDialogs", "WpfMessageDialogControl.xaml");
        AssertTrue(File.Exists(templatePresentationPath), "template auto-label wording should live in a WPF presentation service");
        AssertTrue(templateViewModelSource.Contains("WpfTemplateMatchingAutoLabelPresentationService", StringComparison.Ordinal), "template auto-label ViewModel should use the presentation service for status text");
        AssertTrue(!templateViewModelSource.Contains("전체 이미지 템플릿 자동 저장 시작:", StringComparison.Ordinal), "template auto-label ViewModel should not inline the batch start status template");
        AssertTrue(File.Exists(dialogControlPath), "template guide should use the reusable WPF message dialog UserControl project");
        AssertTrue(projectSource.Contains("OpenVisionLab.Wpf.MessageDialogs", StringComparison.Ordinal), "app project should reference the reusable WPF message dialog library");
        AssertTrue(solutionSource.Contains("OpenVisionLab.Wpf.MessageDialogs", StringComparison.Ordinal), "solution should include the reusable WPF message dialog library project");
        AssertTrue(templateCommandsSource.Contains("OpenVisionLab.Wpf.MessageDialogs", StringComparison.Ordinal), "template guide host should import the reusable WPF dialog namespace");
        AssertTrue(templateCommandsSource.Contains("WpfMessageDialog.ShowInfo", StringComparison.Ordinal), "template guide host should show the reusable WPF dialog");
        AssertTrue(!templateCommandsSource.Contains("MessageBox.Show", StringComparison.Ordinal), "template guide host should not fall back to the stock WPF MessageBox");
    }

    private static Bitmap CreateTemplateCurrentImageSource()
    {
        Bitmap image = CreateSolidBitmap(80, 80, Color.FromArgb(230, 230, 230));
        using Graphics graphics = Graphics.FromImage(image);
        DrawTemplatePattern(graphics, 8, 10);
        graphics.FillRectangle(Brushes.Red, 52, 48, 12, 16);
        graphics.DrawLine(Pens.Blue, 4, 72, 72, 8);
        return image;
    }

    private static Bitmap CreateTemplateTargetImage()
    {
        Bitmap image = CreateSolidBitmap(80, 80, Color.FromArgb(230, 230, 230));
        using Graphics graphics = Graphics.FromImage(image);
        DrawTemplatePattern(graphics, 44, 42);
        graphics.FillRectangle(Brushes.Red, 6, 6, 12, 16);
        graphics.DrawLine(Pens.Blue, 3, 75, 75, 12);
        return image;
    }

    private static void DrawTemplatePattern(Graphics graphics, int x, int y)
    {
        graphics.FillRectangle(Brushes.Black, x, y, 24, 22);
        graphics.FillEllipse(Brushes.DimGray, x + 4, y + 3, 15, 14);
        graphics.DrawLine(Pens.White, x + 2, y + 2, x + 21, y + 18);
        graphics.DrawLine(Pens.LightGray, x + 5, y + 19, x + 20, y + 4);
        graphics.FillRectangle(Brushes.White, x + 14, y + 6, 4, 5);
    }

    internal static void TestWpfTemplateSourceAcceptsManualSegmentationObject()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        var data = new CData();
        data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
        data.ClassNamedList.Add(new CClassItem { Text = "SEG", DrawColor = Color.LimeGreen });
        CGlobal.Inst.Data = data;

        WpfLabelingShellWindow window = new WpfLabelingShellWindow();
        try
        {
            SetPrivateField(window, "activeImageSize", new Size(60, 60));
            SetPrivateField(window, "activeImageBitmap", new Bitmap(60, 60));
            var manualSegments = GetPrivateField<List<LabelingSegmentationObject>>(window, "manualSegments");
            var segment = new LabelingSegmentationObject(
                new[]
                {
                    new Point(10, 12),
                    new Point(36, 12),
                    new Point(34, 38),
                    new Point(12, 36)
                },
                data.ClassNamedList[0])
            {
                ClassName = "SEG"
            };
            manualSegments.Add(segment);
            InvokePrivateResult<object>(window, "RefreshObjectList");

            var objectReviewPanel = (WpfObjectReviewPanel)window.FindName("ObjectReviewPanelControl");
            WpfObjectReviewListItem segmentItem = objectReviewPanel.ViewModel.Objects.FirstOrDefault(item =>
                item.IsEnabled
                && string.Equals(item.SourceKey, WpfObjectReviewSource.ManualSegment.ToString(), StringComparison.OrdinalIgnoreCase));
            AssertTrue(segmentItem != null, "manual SEG object should be selectable from object review before template auto-labeling");
            objectReviewPanel.ViewModel.SelectedObject = segmentItem;

            bool resolved = ((IWpfTemplateMatchingAutoLabelHost)window).TryResolveTemplateMatchingSource(
                out Rectangle templateBounds,
                out string className);
            AssertTrue(resolved, "selected manual SEG object should resolve as a template matching source");
            AssertEqual(segment.Bounds, templateBounds);
            AssertEqual("SEG", className);

            bool shapeResolved = ((IWpfTemplateMatchingAutoLabelHost)window).TryResolveTemplateMatchingSourceSegment(
                out IReadOnlyList<Point> sourcePoints,
                out IReadOnlyList<IReadOnlyList<Point>> sourceCutouts);
            AssertTrue(shapeResolved, "selected manual SEG polygon should expose source points for template batch shape transfer");
            AssertEqual(segment.Points.Count, sourcePoints.Count);
            AssertEqual(segment.Points[0], sourcePoints[0]);
            AssertEqual(0, sourceCutouts.Count);

            var rasterBounds = new Rectangle(10, 12, 26, 26);
            manualSegments.Clear();
            manualSegments.Add(new LabelingSegmentationObject(Array.Empty<Point>(), data.ClassNamedList[0])
            {
                ClassName = "SEG",
                MaskData = CreateTemplateSourceLShapeMask(new Size(60, 60), rasterBounds),
                MaskSize = new Size(60, 60),
                MaskBounds = rasterBounds
            });
            InvokePrivateResult<object>(window, "RefreshObjectList");
            segmentItem = objectReviewPanel.ViewModel.Objects.FirstOrDefault(item =>
                item.IsEnabled
                && string.Equals(item.SourceKey, WpfObjectReviewSource.ManualSegment.ToString(), StringComparison.OrdinalIgnoreCase));
            AssertTrue(segmentItem != null, "manual raster SEG object should be selectable from object review before template auto-labeling");
            objectReviewPanel.ViewModel.SelectedObject = segmentItem;

            bool rasterResolved = ((IWpfTemplateMatchingAutoLabelHost)window).TryResolveTemplateMatchingSourceMask(
                out byte[] sourceMask,
                out Size sourceMaskSize,
                out Rectangle sourceMaskBounds);
            AssertTrue(rasterResolved, "selected manual raster SEG object should expose mask data for template batch shape transfer");
            AssertEqual(new Size(60, 60), sourceMaskSize);
            AssertEqual(rasterBounds, sourceMaskBounds);
            AssertTrue(sourceMask[(rasterBounds.Top * sourceMaskSize.Width) + rasterBounds.Left] > 0, "raster source mask copy should preserve painted pixels");

            var targetBounds = new Rectangle(36, 30, 18, 18);
            int added = ((IWpfTemplateMatchingAutoLabelHost)window).ApplyAutoLabelCandidates(
                new[]
                {
                    new YoloWorkerSmokeCandidate
                    {
                        Index = 1,
                        ClassName = "SEG",
                        Confidence = 0.95D,
                        X = targetBounds.X,
                        Y = targetBounds.Y,
                        Width = targetBounds.Width,
                        Height = targetBounds.Height
                    }
                },
                succeeded: true,
                sourceSegmentBounds: rasterBounds,
                sourceMaskData: sourceMask,
                sourceMaskSize: sourceMaskSize,
                sourceMaskBounds: sourceMaskBounds);
            AssertEqual(1, added);
            AssertEqual(2, manualSegments.Count);
            LabelingSegmentationObject transferredMask = manualSegments[1];
            AssertTrue(transferredMask.IsRasterMask, "current-image SEG template matching should preserve brush-mask geometry instead of creating a rectangle label");
            AssertTrue(transferredMask.MaskData[(targetBounds.Top * transferredMask.MaskSize.Width) + targetBounds.Left] > 0,
                "current-image SEG template mask should preserve the source mask's filled left bar");
            AssertEqual(0, transferredMask.MaskData[((targetBounds.Top + 1) * transferredMask.MaskSize.Width) + targetBounds.Right - 2]);
        }
        finally
        {
            window.Close();
            CGlobal.Inst.Data = previousData;
        }
    }

    internal static void TestWpfTemplateCurrentImageNoCandidatePreservesSavedLabelStatus()
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
        try
        {
            string imageRoot = Path.Combine(root, "images");
            string outputRoot = Path.Combine(root, "dataset");
            Directory.CreateDirectory(imageRoot);
            string imagePath = Path.Combine(imageRoot, "template-current.png");
            using (Bitmap image = CreateTemplateCurrentImageSource())
            {
                image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
            data.ProjectSettings.PythonModel.ImageRootPath = imageRoot;
            data.ClassNamedList.Clear();
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.LimeGreen });
            CGlobal.Inst.Data = data;

            WpfLabelingShellWindow window = new WpfLabelingShellWindow();
            try
            {
                AssertTrue(window.TryLoadImage(imagePath, populateQueue: true, refreshQueueDetails: false), "WPF template no-candidate test image load failed");
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(20));

                var manualRect = new CanvasRect<float>(5, 6, 15, 14)
                {
                    UniqueId = "template-current-source",
                    ShapeKind = CanvasRoiShapeKind.Rectangle
                };
                InvokePrivateResult<object>(
                    window,
                    "MainCanvasViewModel_RoiAdded",
                    window.MainCanvasViewModel,
                    new OpenVisionLab.ImageCanvas.Model.RoiChangedEventArgs { RoiRect = manualRect });

                object[] saveArgs = { 0 };
                AssertTrue(InvokePrivateResult<bool>(window, "SaveCurrentAnnotations", saveArgs), "template source label should be saveable before no-candidate run");
                AssertEqual(1, (int)saveArgs[0]);
                InvokePrivateResult<object>(window, "RefreshActiveImageQueueStatus", false);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(20));

                WpfImageQueueItem item = window.ImageQueueItems.FirstOrDefault(queueItem =>
                    string.Equals(queueItem.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));
                AssertTrue(item != null, "template no-candidate test queue item was not created");
                AssertEqual(YoloImageReviewState.Confirmed, item.ReviewState);

                ((IWpfTemplateMatchingAutoLabelHost)window).ApplyAutoLabelCandidates(Array.Empty<YoloWorkerSmokeCandidate>(), succeeded: true);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(20));

                item = window.ImageQueueItems.FirstOrDefault(queueItem =>
                    string.Equals(queueItem.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));
                AssertTrue(item != null, "template no-candidate test queue item disappeared after applying result");
                AssertEqual(YoloImageReviewState.Confirmed, item.ReviewState);
                AssertTrue(item.LabelStatus.Contains("1", StringComparison.Ordinal), "template no-candidate should keep the saved label count visible");
                AssertTrue(!item.DetectStatus.Contains("\uAC80\uCD9C\uC5C6\uC74C", StringComparison.Ordinal), "template no-candidate should not overwrite a saved label row as no-candidate detection");

                WpfCandidateReviewStateService candidateState = GetPrivateField<WpfCandidateReviewStateService>(window, "candidateReviewState");
                AssertEqual(0, candidateState.PendingCandidates.Count);

                int added = ((IWpfTemplateMatchingAutoLabelHost)window).ApplyAutoLabelCandidates(
                    new[]
                    {
                        new YoloWorkerSmokeCandidate
                        {
                            Index = 1,
                            ClassName = "OK",
                            Confidence = 0.93D,
                            X = 38,
                            Y = 42,
                            Width = 18,
                            Height = 16
                        }
                    },
                    succeeded: true);
                AssertEqual(1, added);
                var manualRois = GetPrivateField<List<Rectangle>>(window, "manualRois");
                AssertTrue(manualRois.Any(bounds => bounds.X == 38 && bounds.Y == 42 && bounds.Width == 18 && bounds.Height == 16),
                    "template candidate should be materialized as a manual label on the current image");
                AssertTrue(window.CanvasPanelViewModel.IsAnnotationSaveEnabled, "template-added manual labels should require an explicit label save");
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private sealed class TemplateAutoLabelGuideHost : IWpfTemplateMatchingAutoLabelHost
    {
        public bool HasActiveAutoLabelImageValue { get; set; }
        public Bitmap ActiveAutoLabelImageValue { get; set; }
        public string ActiveAutoLabelImagePathValue { get; set; } = string.Empty;
        public bool HasTemplateSource { get; set; }
        public Rectangle? TemplateSourceBounds { get; set; }
        public IReadOnlyList<Point> TemplateSourceSegmentPoints { get; set; } = Array.Empty<Point>();
        public IReadOnlyList<IReadOnlyList<Point>> TemplateSourceSegmentCutouts { get; set; } = Array.Empty<IReadOnlyList<Point>>();
        public byte[] TemplateSourceMaskData { get; set; } = Array.Empty<byte>();
        public Size TemplateSourceMaskSize { get; set; } = Size.Empty;
        public Rectangle TemplateSourceMaskBounds { get; set; } = Rectangle.Empty;
        public int GuideCount { get; private set; }
        public string LastGuideTitle { get; private set; } = string.Empty;
        public string LastGuideMessage { get; private set; } = string.Empty;
        public string LastGlobalStatus { get; private set; } = string.Empty;
        public bool LastGlobalIsWarning { get; private set; }
        public bool ApplyCandidatesCalled { get; private set; }
        public bool LastApplySucceeded { get; private set; }
        public int LastCandidateCount { get; private set; }
        public IReadOnlyList<YoloWorkerSmokeCandidate> LastCandidates { get; private set; } = Array.Empty<YoloWorkerSmokeCandidate>();
        public CData AutoLabelDataValue { get; set; } = new CData();
        public IReadOnlyList<WpfImageQueueItem> AllQueueItems { get; set; } = Array.Empty<WpfImageQueueItem>();
        public IReadOnlyList<TemplateMatchingBatchAutoLabelItemResult> BatchResults { get; private set; } = Array.Empty<TemplateMatchingBatchAutoLabelItemResult>();
        public bool BatchCompleted { get; private set; }
        public int StartedBatchTotalCount { get; private set; }
        public int BatchCompletedCount { get; private set; }

        public bool IsAutoLabelBusy => false;
        public bool HasActiveAutoLabelImage => HasActiveAutoLabelImageValue;
        public Bitmap ActiveAutoLabelImage => ActiveAutoLabelImageValue;
        public string ActiveAutoLabelImagePath => ActiveAutoLabelImagePathValue;
        public CData AutoLabelData => AutoLabelDataValue;
        public int MaximumTemplateMatchingCandidateCount => 20;

        public bool TryResolveTemplateMatchingSource(out Rectangle templateBounds, out string className)
        {
            templateBounds = HasTemplateSource ? TemplateSourceBounds ?? new Rectangle(1, 1, 10, 10) : Rectangle.Empty;
            className = "Part";
            return HasTemplateSource;
        }

        public bool TryResolveTemplateMatchingSourceSegment(
            out IReadOnlyList<Point> points,
            out IReadOnlyList<IReadOnlyList<Point>> cutouts)
        {
            points = TemplateSourceSegmentPoints ?? Array.Empty<Point>();
            cutouts = TemplateSourceSegmentCutouts ?? Array.Empty<IReadOnlyList<Point>>();
            return HasTemplateSource && points.Count >= 3;
        }

        public bool TryResolveTemplateMatchingSourceMask(
            out byte[] maskData,
            out Size maskSize,
            out Rectangle maskBounds)
        {
            maskData = TemplateSourceMaskData ?? Array.Empty<byte>();
            maskSize = TemplateSourceMaskSize;
            maskBounds = TemplateSourceMaskBounds;
            return HasTemplateSource
                && maskData.Length == Math.Max(0, maskSize.Width * maskSize.Height)
                && maskSize.Width > 0
                && maskSize.Height > 0
                && !maskBounds.IsEmpty;
        }

        public CClassItem EnsureAutoLabelClassItem(string className)
            => new CClassItem { Text = string.IsNullOrWhiteSpace(className) ? "Part" : className, DrawColor = Color.Blue };

        public IReadOnlyList<WpfImageQueueItem> GetVisibleAutoLabelQueueItems()
            => AllQueueItems;

        public IReadOnlyList<WpfImageQueueItem> GetAllAutoLabelQueueItems()
            => AllQueueItems;

        public IReadOnlyList<WpfImageQueueItem> BuildAutoLabelBatchQueue(IEnumerable<WpfImageQueueItem> items)
            => (items ?? Array.Empty<WpfImageQueueItem>()).ToList();

        public void AppendAutoLabelLog(string message)
        {
        }

        public void ShowAutoLabelGuide(string title, string message)
        {
            GuideCount++;
            LastGuideTitle = title ?? string.Empty;
            LastGuideMessage = message ?? string.Empty;
        }

        public int ApplyAutoLabelCandidates(
            IReadOnlyList<YoloWorkerSmokeCandidate> candidates,
            bool succeeded,
            Rectangle? sourceSegmentBounds = null,
            IReadOnlyList<Point> sourceSegmentPoints = null,
            IReadOnlyList<IReadOnlyList<Point>> sourceSegmentCutouts = null,
            byte[] sourceMaskData = null,
            Size sourceMaskSize = default,
            Rectangle sourceMaskBounds = default)
        {
            ApplyCandidatesCalled = true;
            LastApplySucceeded = succeeded;
            LastCandidateCount = candidates?.Count ?? 0;
            LastCandidates = (candidates ?? Array.Empty<YoloWorkerSmokeCandidate>()).ToList();
            return LastCandidateCount;
        }

        public void SetAutoLabelPythonStatus(string text)
        {
        }

        public void SetAutoLabelCommandStatus(string text, bool isBusy)
        {
        }

        public void SetAutoLabelGlobalInferenceStatus(string text, bool isBusy, bool isWarning = false)
        {
            LastGlobalStatus = text ?? string.Empty;
            LastGlobalIsWarning = isWarning;
        }

        public CancellationToken StartAutoLabelBatch(int totalCount, string scopeText)
        {
            StartedBatchTotalCount = totalCount;
            BatchCompleted = false;
            BatchResults = Array.Empty<TemplateMatchingBatchAutoLabelItemResult>();
            return CancellationToken.None;
        }

        public void MarkAutoLabelBatchItemRequested(WpfImageQueueItem item)
        {
        }

        public void UpdateAutoLabelBatchProgress(string scopeText, string currentFileName, int completedCount, int totalCount)
        {
        }

        public void ApplyAutoLabelBatchResult(WpfImageQueueItem item, TemplateMatchingBatchAutoLabelItemResult result, bool saveReviewStatus)
        {
            BatchResults = BatchResults.Concat(new[] { result }).ToList();
        }

        public void SaveAutoLabelReviewStatus()
        {
        }

        public void CompleteAutoLabelBatch(bool canceled, int completedCount, int totalCount, string scopeText)
        {
            BatchCompleted = true;
            BatchCompletedCount = completedCount;
        }

        public void NotifyAutoLabelDataChanged()
        {
        }

        public Task YieldAutoLabelBatchFrameAsync(CancellationToken token)
            => Task.CompletedTask;
    }

}

internal static class TemplateAutoLabelFixtures
{
    internal static Bitmap CreateTemplateBatchAutoLabelImage(params Point[] patternOrigins)
    {
        Bitmap image = CreateSolidBitmap(120, 90, Color.FromArgb(235, 235, 235));
        using Graphics graphics = Graphics.FromImage(image);
        using Bitmap pattern = CreateTemplateBatchAutoLabelPattern();
        foreach (Point origin in patternOrigins ?? Array.Empty<Point>())
        {
            graphics.DrawImageUnscaled(pattern, origin);
        }

        return image;
    }

    internal static Bitmap CreateTemplateBatchAutoLabelPattern()
    {
        var pattern = new Bitmap(24, 18);
        using Graphics graphics = Graphics.FromImage(pattern);
        graphics.Clear(Color.FromArgb(235, 235, 235));
        graphics.FillRectangle(Brushes.Black, 3, 4, 18, 9);
        graphics.FillRectangle(Brushes.White, 7, 6, 5, 4);
        graphics.FillRectangle(Brushes.Red, 16, 3, 3, 12);
        graphics.DrawLine(Pens.Blue, 2, 15, 21, 15);
        graphics.DrawRectangle(Pens.DarkGreen, 1, 1, 22, 16);
        return pattern;
    }

    internal static byte[] CreateTemplateSourceLShapeMask(Size maskSize, Rectangle bounds)
    {
        byte[] maskData = new byte[Math.Max(0, maskSize.Width * maskSize.Height)];
        Rectangle clipped = Rectangle.Intersect(bounds, new Rectangle(Point.Empty, maskSize));
        for (int y = clipped.Top; y < clipped.Bottom; y++)
        {
            for (int x = clipped.Left; x < clipped.Right; x++)
            {
                bool leftBar = x < clipped.Left + Math.Max(2, clipped.Width / 3);
                bool bottomBar = y >= clipped.Top + Math.Max(2, (clipped.Height * 2) / 3);
                if (leftBar || bottomBar)
                {
                    maskData[(y * maskSize.Width) + x] = 1;
                }
            }
        }

        return maskData;
    }
}
