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
using static LabelingApplication.Tests.DatasetReadinessTestFixtures;

internal static class YoloDatasetReadinessTests
{
    internal static void TestYoloDatasetReadinessSegmentationOnlyPolicy()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });

            var emptyBoxes = new Dictionary<string, List<CRectangleObject>>();
            var segments = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject>
                {
                    new LabelingSegmentationObject(
                        new[]
                        {
                            new Point(4, 4),
                            new Point(20, 4),
                            new Point(20, 20),
                            new Point(4, 20)
                        },
                        data.ClassNamedList[0])
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(32, 32, Color.Black))
            using (Bitmap validImage = CreateSolidBitmap(32, 32, Color.White))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                YoloAnnotationService.SaveAnnotations("seg-train.png", trainImage, emptyBoxes, data.ClassNamedList, data);
                YoloSegmentationAnnotationService.SaveSegmentationAnnotations("seg-train.png", trainImage, segments, data.ClassNamedList, data);

                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                YoloAnnotationService.SaveAnnotations("seg-valid.png", validImage, emptyBoxes, data.ClassNamedList, data);
                YoloSegmentationAnnotationService.SaveSegmentationAnnotations("seg-valid.png", validImage, segments, data.ClassNamedList, data);
            }

            YoloDatasetReadinessReport report = YoloDatasetReadinessService.Build(data, refreshYaml: true);

            AssertTrue(!report.IsReady, "segmentation-only data should not be reported as detection-training ready");
            AssertTrue(
                report.Errors.Any(error => error.Contains("segmentation annotations", StringComparison.OrdinalIgnoreCase)
                    && error.Contains("no YOLO box labels", StringComparison.OrdinalIgnoreCase)),
                "segmentation-only policy error was not reported");
            AssertEqual(0, report.Statistics.TotalObjectCount);
            AssertEqual(2, report.Statistics.TotalSegmentationObjectCount);
            AssertEqual(1, report.Statistics.TrainSegmentFileCount);
            AssertEqual(1, report.Statistics.ValidSegmentFileCount);
            AssertTrue(report.SummaryLines.Any(line => line.Contains("Segments:2", StringComparison.Ordinal)), "readiness summary did not include segmentation count");
            AssertTrue(report.SummaryLines.Any(line => line.Contains("Defect:2", StringComparison.Ordinal)), "readiness summary did not include segmentation class count");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestYoloDatasetReadinessPurposePolicy()
    {
        string root = CreateTempRoot();
        try
        {
            CData objectData = CreatePurposeReadinessData(
                Path.Combine(root, "object-detection"),
                LabelingDatasetPurpose.ObjectDetection,
                includeBoxes: true,
                includeSegments: true);

            YoloDatasetReadinessReport objectReport = YoloDatasetReadinessService.Build(objectData, refreshYaml: true);

            AssertTrue(objectReport.IsReady, string.Join(Environment.NewLine, objectReport.Errors));
            AssertEqual(LabelingDatasetPurpose.ObjectDetection, objectReport.Purpose);
            AssertEqual(2, objectReport.Statistics.TotalObjectCount);
            AssertEqual(2, objectReport.Statistics.TotalSegmentationObjectCount);
            AssertTrue(
                objectReport.SummaryLines.Any(line => line.Contains("SegmentationArtifactsExcluded", StringComparison.Ordinal)),
                "object-detection readiness should explain that stale segmentation artifacts are excluded");
            AssertTrue(
                YoloDatasetDiagnosticsService.BuildQualityWarnings(objectData, objectReport.Statistics)
                    .Any(line => line.Contains("ObjectDetection ignores segmentation artifacts", StringComparison.Ordinal)),
                "object-detection diagnostics should warn that segmentation artifacts are ignored");

            CData boxOnlySegmentationData = CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-box-only"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: true,
                includeSegments: false);

            YoloDatasetReadinessReport boxOnlySegmentationReport = YoloDatasetReadinessService.Build(boxOnlySegmentationData, refreshYaml: true);

            AssertTrue(!boxOnlySegmentationReport.IsReady, "segmentation purpose should not be ready with box labels only");
            AssertTrue(
                boxOnlySegmentationReport.Errors.Any(error => error.Contains("Segmentation dataset", StringComparison.Ordinal)
                    || error.Contains("segmentation annotation is missing", StringComparison.Ordinal)),
                "segmentation purpose should explain that mask/polygon annotations are required");

            CData segmentationData = CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-ready"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);

            YoloDatasetReadinessReport segmentationReport = YoloDatasetReadinessService.Build(segmentationData, refreshYaml: true);

            AssertTrue(segmentationReport.IsReady, string.Join(Environment.NewLine, segmentationReport.Errors));
            AssertEqual(LabelingDatasetPurpose.Segmentation, segmentationReport.Purpose);
            AssertEqual(0, segmentationReport.Statistics.TotalObjectCount);
            AssertEqual(2, segmentationReport.Statistics.TotalSegmentationObjectCount);
            AssertTrue(
                segmentationReport.SummaryLines.Any(line => line.Contains("Segmentation uses segment JSON/mask PNG annotations as primary labels", StringComparison.Ordinal)),
                "segmentation readiness should name segment/mask annotations as primary labels");

            segmentationData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYolo11;
            int port = GetAvailableTcpPort();
            using var communication = new CCommunicationLearning(startListen: false, port: port);
            using var requestReceived = new ManualResetEventSlim(false);
            AssertTrue(communication.Start(), "test TCP listener for segmentation training did not start");
            Task mockClient = Task.Run(() => RunMockTrainingPacketCaptureClient(
                port,
                requestReceived,
                request =>
                {
                    AssertEqual("yolo11", request.model);
                    AssertEqual("segment", request.task);
                    AssertEqual("yolo11n-seg.pt", request.weight);
                    AssertEqual(
                        segmentationData.DataYamlFilePath.Replace("\\", "/"),
                        request.dataYaml);
                }));
            AssertTrue(WaitUntil(() => communication.GetStatusSnapshot().IsClientConnected, TimeSpan.FromSeconds(5)), "mock segmentation training client did not connect");

            var workflow = new YoloTrainingWorkflowService();
            AssertTrue(workflow.TryStartTraining(segmentationData, communication), "segmentation workflow should send StartTraining when segment artifacts exist");
            AssertTrue(
                segmentationData.ProjectSettings.TrainingGuide.LastTrainingDatasetVersionId.StartsWith("dsv2-", StringComparison.Ordinal),
                "recipe-owned training should retain the exact Dataset Version v2 used at send time");
            AssertEqual(64, segmentationData.ProjectSettings.TrainingGuide.LastTrainingDatasetContentSha256.Length);
            AssertTrue(requestReceived.Wait(TimeSpan.FromSeconds(5)), "mock segmentation training client did not receive StartTraining");
            AssertTrue(mockClient.Wait(TimeSpan.FromSeconds(5)), "mock segmentation training client did not finish");
            if (mockClient.IsFaulted && mockClient.Exception != null)
            {
                throw mockClient.Exception;
            }

            string trainSegmentLabelPath = Path.Combine(segmentationData.OutputRootPath, "data", "train", "labels", "purpose-train.txt");
            string validSegmentLabelPath = Path.Combine(segmentationData.OutputRootPath, "data", "valid", "labels", "purpose-valid.txt");
            AssertTrue(File.Exists(trainSegmentLabelPath), "segmentation training should export train polygon labels for Ultralytics");
            AssertTrue(File.Exists(validSegmentLabelPath), "segmentation training should export valid polygon labels for Ultralytics");
            string trainSegmentLabel = File.ReadAllLines(trainSegmentLabelPath).Single();
            string validSegmentLabel = File.ReadAllLines(validSegmentLabelPath).Single();
            AssertEqual("0 0.125 0.125 0.6875 0.125 0.6875 0.6875 0.125 0.6875", trainSegmentLabel);
            AssertEqual(trainSegmentLabel, validSegmentLabel);
            AssertEqual(9, trainSegmentLabel.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length);

            segmentationData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            int yolo8Port = GetAvailableTcpPort();
            using var yolo8Communication = new CCommunicationLearning(startListen: false, port: yolo8Port);
            using var yolo8RequestReceived = new ManualResetEventSlim(false);
            AssertTrue(yolo8Communication.Start(), "test TCP listener for YOLOv8 segmentation training did not start");
            Task yolo8MockClient = Task.Run(() => RunMockTrainingPacketCaptureClient(
                yolo8Port,
                yolo8RequestReceived,
                request =>
                {
                    AssertEqual("yolov8", request.model);
                    AssertEqual("segment", request.task);
                    AssertEqual("yolov8n-seg.pt", request.weight);
                    AssertEqual(
                        segmentationData.DataYamlFilePath.Replace("\\", "/"),
                        request.dataYaml);
                }));
            AssertTrue(WaitUntil(() => yolo8Communication.GetStatusSnapshot().IsClientConnected, TimeSpan.FromSeconds(5)), "mock YOLOv8 segmentation training client did not connect");

            AssertTrue(workflow.TryStartTraining(segmentationData, yolo8Communication), "YOLOv8 segmentation workflow should send StartTraining when segment artifacts exist");
            AssertTrue(yolo8RequestReceived.Wait(TimeSpan.FromSeconds(5)), "mock YOLOv8 segmentation training client did not receive StartTraining");
            AssertTrue(yolo8MockClient.Wait(TimeSpan.FromSeconds(5)), "mock YOLOv8 segmentation training client did not finish");
            if (yolo8MockClient.IsFaulted && yolo8MockClient.Exception != null)
            {
                throw yolo8MockClient.Exception;
            }

            CData backgroundSegmentationData = CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-ok-background"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            backgroundSegmentationData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            backgroundSegmentationData.ProjectSettings.YoloDataset.ValidationPercent = 50;
            backgroundSegmentationData.ProjectSettings.YoloDataset.TestPercent = 0;
            string sourceRoot = Path.Combine(root, "operator-ok-ng-images");
            string okRoot = Path.Combine(sourceRoot, "OK");
            Directory.CreateDirectory(okRoot);
            string okTrainStem = FindImageStemForSplit("ok-background-train", YoloDatasetSplitService.TrainMode, backgroundSegmentationData.ProjectSettings.YoloDataset);
            string okValidStem = FindImageStemForSplit("ok-background-valid", YoloDatasetSplitService.ValidMode, backgroundSegmentationData.ProjectSettings.YoloDataset);
            using (Bitmap okTrainImage = CreateSolidBitmap(32, 32, Color.FromArgb(20, 40, 80)))
            using (Bitmap okValidImage = CreateSolidBitmap(32, 32, Color.FromArgb(80, 60, 20)))
            {
                okTrainImage.Save(Path.Combine(okRoot, $"{okTrainStem}.png"), System.Drawing.Imaging.ImageFormat.Png);
                okValidImage.Save(Path.Combine(okRoot, $"{okValidStem}.png"), System.Drawing.Imaging.ImageFormat.Png);
            }

            for (int index = 0; index < 3; index++)
            {
                string extraTrainStem = FindImageStemForSplit($"ok-background-train-extra-{index}", YoloDatasetSplitService.TrainMode, backgroundSegmentationData.ProjectSettings.YoloDataset);
                string extraValidStem = FindImageStemForSplit($"ok-background-valid-extra-{index}", YoloDatasetSplitService.ValidMode, backgroundSegmentationData.ProjectSettings.YoloDataset);
                using (Bitmap okTrainExtraImage = CreateSolidBitmap(32, 32, Color.FromArgb(20 + index, 40, 80)))
                using (Bitmap okValidExtraImage = CreateSolidBitmap(32, 32, Color.FromArgb(80, 60 + index, 20)))
                {
                    okTrainExtraImage.Save(Path.Combine(okRoot, $"{extraTrainStem}.png"), System.Drawing.Imaging.ImageFormat.Png);
                    okValidExtraImage.Save(Path.Combine(okRoot, $"{extraValidStem}.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            backgroundSegmentationData.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            var backgroundWorkflow = new YoloTrainingWorkflowService();
            AssertTrue(backgroundWorkflow.TryPrepareTrainingDataset(backgroundSegmentationData), backgroundWorkflow.LastPreparationFailureMessage);
            string okTrainOutputImage = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "train", "images", $"{okTrainStem}.png");
            string okTrainOutputLabel = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "train", "labels", $"{okTrainStem}.txt");
            string okValidOutputImage = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "valid", "images", $"{okValidStem}.png");
            string okValidOutputLabel = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "valid", "labels", $"{okValidStem}.txt");
            AssertTrue(File.Exists(okTrainOutputImage), "YOLOv8 SEG preparation should copy OK folder train background image into the dataset output");
            AssertTrue(File.Exists(okValidOutputImage), "YOLOv8 SEG preparation should copy OK folder valid background image into the dataset output");
            AssertTrue(File.Exists(okTrainOutputLabel) && File.ReadAllText(okTrainOutputLabel).Length == 0, "YOLOv8 SEG preparation should write an empty train label for OK background images");
            AssertTrue(File.Exists(okValidOutputLabel) && File.ReadAllText(okValidOutputLabel).Length == 0, "YOLOv8 SEG preparation should write an empty valid label for OK background images");
            YoloDatasetReadinessReport backgroundReport = YoloDatasetReadinessService.Build(backgroundSegmentationData, refreshYaml: true);
            AssertTrue(backgroundReport.IsReady, string.Join(Environment.NewLine, backgroundReport.Errors));
            AssertEqual(2, backgroundReport.Statistics.TotalSegmentationObjectCount);
            AssertEqual(4, backgroundReport.Statistics.TrainEmptyLabelFileCount);
            AssertEqual(4, backgroundReport.Statistics.ValidEmptyLabelFileCount);
            IReadOnlyList<string> backgroundWarnings = YoloDatasetDiagnosticsService.BuildQualityWarnings(backgroundSegmentationData, backgroundReport.Statistics);
            AssertTrue(
                backgroundWarnings.Any(line => line.Contains("segmentation train split has only 1 positive mask image", StringComparison.Ordinal)),
                "YOLOv8 SEG readiness should warn when train has too few positive mask images");
            AssertTrue(
                backgroundWarnings.Any(line => line.Contains("segmentation valid split has only 1 positive mask image", StringComparison.Ordinal)),
                "YOLOv8 SEG readiness should warn when valid has too few positive mask images");
            AssertTrue(
                backgroundWarnings.Any(line => line.Contains("segmentation train split has 4 OK/background image", StringComparison.Ordinal)
                    && line.Contains("only 1 positive mask image", StringComparison.Ordinal)),
                "YOLOv8 SEG readiness should warn when train OK/background images dominate positive masks");
            AssertTrue(
                backgroundWarnings.Any(line => line.Contains("segmentation valid split has 4 OK/background image", StringComparison.Ordinal)
                    && line.Contains("only 1 positive mask image", StringComparison.Ordinal)),
                "YOLOv8 SEG readiness should warn when valid OK/background images dominate positive masks");

            backgroundSegmentationData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            backgroundSegmentationData.ProjectSettings.YoloDataset.TestPercent = 100;
            YoloSegmentationTrainingLabelExportResult shiftedBackgroundExport = YoloSegmentationTrainingLabelService.Export(backgroundSegmentationData);
            string okTrainTestOutputImage = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "test", "images", $"{okTrainStem}.png");
            string okTrainTestOutputLabel = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "test", "labels", $"{okTrainStem}.txt");
            string okValidTestOutputImage = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "test", "images", $"{okValidStem}.png");
            string okValidTestOutputLabel = Path.Combine(backgroundSegmentationData.OutputRootPath, "data", "test", "labels", $"{okValidStem}.txt");
            AssertTrue(!File.Exists(okTrainOutputImage), "YOLOv8 SEG preparation should remove stale OK background train image after split settings move it");
            AssertTrue(!File.Exists(okTrainOutputLabel), "YOLOv8 SEG preparation should remove stale OK background train empty label after split settings move it");
            AssertTrue(!File.Exists(okValidOutputImage), "YOLOv8 SEG preparation should remove stale OK background valid image after split settings move it");
            AssertTrue(!File.Exists(okValidOutputLabel), "YOLOv8 SEG preparation should remove stale OK background valid empty label after split settings move it");
            AssertTrue(File.Exists(okTrainTestOutputImage), "YOLOv8 SEG preparation should copy moved OK train-background image into the test split");
            AssertTrue(File.Exists(okValidTestOutputImage), "YOLOv8 SEG preparation should copy moved OK valid-background image into the test split");
            AssertTrue(File.Exists(okTrainTestOutputLabel) && File.ReadAllText(okTrainTestOutputLabel).Length == 0, "YOLOv8 SEG preparation should write an empty test label after moving OK train-background image");
            AssertTrue(File.Exists(okValidTestOutputLabel) && File.ReadAllText(okValidTestOutputLabel).Length == 0, "YOLOv8 SEG preparation should write an empty test label after moving OK valid-background image");
            AssertTrue(shiftedBackgroundExport.BackgroundImageCount >= 2, "YOLOv8 SEG preparation should report moved OK background images");
            YoloDatasetReadinessReport shiftedBackgroundReport = YoloDatasetReadinessService.Build(backgroundSegmentationData, refreshYaml: true);
            IReadOnlyList<string> shiftedBackgroundWarnings = YoloDatasetDiagnosticsService.BuildQualityWarnings(backgroundSegmentationData, shiftedBackgroundReport.Statistics);
            AssertTrue(
                shiftedBackgroundWarnings.Any(line => line.Contains("segmentation test split has OK/background image", StringComparison.Ordinal)
                    && line.Contains("no positive mask image", StringComparison.Ordinal)),
                "YOLOv8 SEG readiness should warn when held-out test split has only OK/background images");

            CData maskOnlySegmentationData = CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-mask-only"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: false,
                includeMaskOnly: true);

            YoloDatasetReadinessReport maskOnlySegmentationReport = YoloDatasetReadinessService.Build(maskOnlySegmentationData, refreshYaml: true);
            IReadOnlyList<string> maskOnlyWarnings = YoloDatasetDiagnosticsService.BuildQualityWarnings(maskOnlySegmentationData, maskOnlySegmentationReport.Statistics);
            string maskOnlyStatus = InvokePrivateStaticResult<string>(
                typeof(WpfLabelingShellWindow),
                "BuildReadyDatasetStatusText",
                maskOnlySegmentationReport.Statistics,
                LabelingDatasetPurpose.Segmentation,
                maskOnlyWarnings.Count > 0);

            AssertTrue(maskOnlySegmentationReport.IsReady, string.Join(Environment.NewLine, maskOnlySegmentationReport.Errors));
            AssertEqual(0, maskOnlySegmentationReport.Statistics.TotalSegmentationObjectCount);
            AssertEqual(2, maskOnlySegmentationReport.Statistics.TotalMaskFileCount);
            AssertTrue(
                maskOnlyWarnings.Any(line => line.Contains("mask PNG files", StringComparison.Ordinal)
                    && line.Contains("class/object balance requires segment JSON", StringComparison.Ordinal)),
                "mask-only segmentation should explain that class/object balance needs segment JSON");
            AssertTrue(
                !maskOnlyWarnings.Any(line => line.Contains("class 'Defect' has only 0", StringComparison.Ordinal)),
                "mask-only segmentation should not report every class as zero objects");
            AssertTrue(maskOnlyStatus.Contains("마스크 2파일", StringComparison.Ordinal), "WPF ready status should show mask file count for mask-only segmentation");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static string FindImageStemForSplit(string prefix, string split, YoloDatasetSettings settings)
    {
        for (int index = 0; index < 500; index++)
        {
            string stem = $"{prefix}-{index:000}";
            if (YoloDatasetSplitService.SelectModesForImage(stem, settings)
                .Contains(split, StringComparer.OrdinalIgnoreCase))
            {
                return stem;
            }
        }

        throw new InvalidOperationException($"Could not find a stable image stem for split {split}.");
    }

    internal static void TestYoloV8SegmentationAppDatasetFixture()
    {
        string root = Path.Combine(FindRepositoryRoot(), "artifacts", "yolov8-app-segmentation-dataset");
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        CData data = CreatePurposeReadinessData(
            root,
            LabelingDatasetPurpose.Segmentation,
            includeBoxes: false,
            includeSegments: true);
        data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
        data.ProjectSettings.YoloDataset.ValidationPercent = 0;
        data.ProjectSettings.YoloDataset.TestPercent = 100;
        using (Bitmap testImage = CreateSolidBitmap(32, 32, Color.Gray))
        {
            var emptyRois = new Dictionary<string, List<CRectangleObject>>();
            var testSegments = new Dictionary<string, List<LabelingSegmentationObject>>
            {
                ["Defect"] = new List<LabelingSegmentationObject>
                {
                    new LabelingSegmentationObject(
                        new[]
                        {
                            new Point(6, 6),
                            new Point(24, 6),
                            new Point(24, 24),
                            new Point(6, 24)
                        },
                        data.ClassNamedList[0])
                }
            };
            YoloAnnotationService.SaveAnnotations("purpose-test.png", testImage, emptyRois, data.ClassNamedList, data);
            YoloSegmentationAnnotationService.SaveSegmentationAnnotations("purpose-test.png", testImage, testSegments, data.ClassNamedList, data);
        }

        string templateSourceDirectory = Path.Combine(root, "template-source");
        Directory.CreateDirectory(templateSourceDirectory);
        string templatePolygonTargetPath = Path.Combine(templateSourceDirectory, "template-polygon-train-target.png");
        string templateMaskTargetPath = Path.Combine(templateSourceDirectory, "template-mask-valid-target.png");
        using (Bitmap polygonTargetImage = TemplateAutoLabelFixtures.CreateTemplateBatchAutoLabelImage(new Point(56, 34)))
        using (Bitmap maskTargetImage = TemplateAutoLabelFixtures.CreateTemplateBatchAutoLabelImage(new Point(56, 34)))
        {
            using (Graphics graphics = Graphics.FromImage(maskTargetImage))
            {
                graphics.FillRectangle(Brushes.Navy, 2, 2, 5, 5);
            }

            polygonTargetImage.Save(templatePolygonTargetPath, System.Drawing.Imaging.ImageFormat.Png);
            maskTargetImage.Save(templateMaskTargetPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        using (Bitmap templateImage = TemplateAutoLabelFixtures.CreateTemplateBatchAutoLabelPattern())
        {
            var templateBatchService = new TemplateMatchingBatchAutoLabelService();
            var templateOptions = new TemplateMatchingAutoLabelOptions
            {
                MinimumScore = 0.7D,
                MaximumCandidates = 1,
                ExcludeSourceRegion = false
            };

            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            TemplateMatchingBatchAutoLabelItemResult polygonBatch = templateBatchService.MatchAndSaveImage(
                templatePolygonTargetPath,
                templateImage,
                data.ClassNamedList[0],
                data.ClassNamedList[0].Text,
                data,
                templateOptions,
                CancellationToken.None,
                new Rectangle(12, 12, 24, 18),
                new[]
                {
                    new Point(15, 16),
                    new Point(30, 18),
                    new Point(20, 27)
                });
            AssertTrue(polygonBatch.Saved, $"YOLOv8 SEG fixture should save template polygon batch target: {polygonBatch.Message}");

            data.ProjectSettings.YoloDataset.ValidationPercent = 100;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            TemplateMatchingBatchAutoLabelItemResult maskBatch = templateBatchService.MatchAndSaveImage(
                templateMaskTargetPath,
                templateImage,
                data.ClassNamedList[0],
                data.ClassNamedList[0].Text,
                data,
                templateOptions,
                CancellationToken.None,
                sourceMaskData: TemplateAutoLabelFixtures.CreateTemplateSourceLShapeMask(new Size(120, 90), new Rectangle(12, 12, 24, 18)),
                sourceMaskSize: new Size(120, 90),
                sourceMaskBounds: new Rectangle(12, 12, 24, 18));
            AssertTrue(maskBatch.Saved, $"YOLOv8 SEG fixture should save template raster-mask batch target: {maskBatch.Message}");
        }

        data.ProjectSettings.YoloDataset.ValidationPercent = 0;
        data.ProjectSettings.YoloDataset.TestPercent = 100;
        var workflow = new YoloTrainingWorkflowService();
        AssertTrue(workflow.TryPrepareTrainingDataset(data), workflow.LastPreparationFailureMessage);
        YoloSegmentationTrainingLabelExportResult exportResult = YoloSegmentationTrainingLabelService.Export(data);

        string trainLabelPath = Path.Combine(root, "data", "train", "labels", "purpose-train.txt");
        string validLabelPath = Path.Combine(root, "data", "valid", "labels", "purpose-valid.txt");
        string testLabelPath = Path.Combine(root, "data", "test", "labels", "purpose-test.txt");
        string templatePolygonLabelPath = Path.Combine(root, "data", "train", "labels", "template-polygon-train-target.txt");
        string templateMaskLabelPath = Path.Combine(root, "data", "valid", "labels", "template-mask-valid-target.txt");
        string trainLabel = File.ReadAllLines(trainLabelPath).Single();
        string validLabel = File.ReadAllLines(validLabelPath).Single();
        string testLabel = File.ReadAllLines(testLabelPath).Single();
        string templatePolygonLabel = File.ReadAllLines(templatePolygonLabelPath).Single();
        string templateMaskLabel = File.ReadAllLines(templateMaskLabelPath).Single();

        AssertTrue(File.Exists(data.DataYamlFilePath), "YOLOv8 app segmentation fixture should write data.yaml");
        AssertEqual("0 0.125 0.125 0.6875 0.125 0.6875 0.6875 0.125 0.6875", trainLabel);
        AssertEqual(trainLabel, validLabel);
        AssertEqual("0 0.1875 0.1875 0.75 0.1875 0.75 0.75 0.1875 0.75", testLabel);
        AssertTrue(exportResult.TestPolygonCount > 0, "YOLOv8 segmentation fixture should export held-out test segment labels for model comparison");
        AssertTrue(trainLabel.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 5, "YOLOv8 segmentation fixture labels should contain polygon points");
        AssertYoloSegmentationLabelLine(templatePolygonLabel, minimumCoordinatePairs: 3, "template polygon batch label");
        AssertYoloSegmentationLabelLine(templateMaskLabel, minimumCoordinatePairs: 5, "template raster-mask batch label");

        YoloDatasetReadinessReport readiness = YoloDatasetReadinessService.Build(data, refreshYaml: true);
        AssertTrue(readiness.IsReady, string.Join(Environment.NewLine, readiness.Errors));
        AssertTrue(readiness.Statistics.TotalSegmentationObjectCount >= 5, "YOLOv8 SEG readiness should count manual and template-batch segment objects");

        Console.WriteLine($"YOLOV8_APP_SEGMENTATION_DATASET={root}");
        Console.WriteLine($"YOLOV8_APP_SEGMENTATION_DATA_YAML={data.DataYamlFilePath}");
    }

    private static void AssertYoloSegmentationLabelLine(string line, int minimumCoordinatePairs, string labelName)
    {
        string[] parts = (line ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        AssertTrue(parts.Length >= 1 + (minimumCoordinatePairs * 2), $"{labelName} should contain at least {minimumCoordinatePairs} polygon points");
        AssertEqual("0", parts[0]);
        AssertTrue((parts.Length - 1) % 2 == 0, $"{labelName} should contain paired x/y coordinates");
        for (int index = 1; index < parts.Length; index++)
        {
            double value = double.Parse(parts[index], CultureInfo.InvariantCulture);
            AssertTrue(value >= 0D && value <= 1D, $"{labelName} coordinate should be normalized: {parts[index]}");
        }
    }

    internal static void TestYoloDatasetReadinessReport()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            var rois = new Dictionary<string, List<CRectangleObject>>
            {
                ["OK"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(40, 40, Color.Black))
            using (Bitmap validImage = CreateSolidBitmap(40, 40, Color.White))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                YoloAnnotationService.SaveAnnotations("train-sample.png", trainImage, rois, data.ClassNamedList, data);
                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                YoloAnnotationService.SaveAnnotations("valid-sample.png", validImage, rois, data.ClassNamedList, data);
            }

            YoloDatasetReadinessReport report = YoloDatasetReadinessService.Build(data, refreshYaml: true);

            AssertTrue(report.IsReady, string.Join(Environment.NewLine, report.Errors));
            AssertEqual(1, report.Statistics.TrainImageCount);
            AssertEqual(1, report.Statistics.ValidImageCount);
            AssertEqual(0, report.Statistics.TestImageCount);
            AssertTrue(report.SummaryLines.Any(line => line.Contains("TrainImages:1")), "readiness summary did not include train image count");
            AssertTrue(report.SummaryLines.Any(line => line.Contains("TestImages:0")), "readiness summary did not include test image count");
            AssertTrue(report.SummaryLines.Any(line => line.Contains("OK:2")), "readiness summary did not include class object count");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestYoloDatasetDiagnosticsReport()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);

            IReadOnlyList<string> lines = YoloDatasetDiagnosticsService.BuildOperatorReport(data, refreshYaml: false);

            AssertTrue(lines.Any(line => line.Contains("NOT READY")), "diagnostics did not report not-ready state");
            AssertTrue(lines.Any(line => line.Contains("YOLO output root")), "diagnostics did not include output root");
            AssertTrue(lines.Any(line => line.Contains("At least one class")), "diagnostics did not include class issue");

            data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });
            data.ClassNamedList.Add(new CClassItem { Text = "NG", DrawColor = Color.Red });

            var rois = new Dictionary<string, List<CRectangleObject>>
            {
                ["OK"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
                }
            };

            using (Bitmap trainImage = CreateSolidBitmap(40, 40, Color.Black))
            using (Bitmap validImage = CreateSolidBitmap(40, 40, Color.White))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations("train-sample.png", trainImage, rois, data.ClassNamedList, data);
                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                data.ProjectSettings.YoloDataset.TestPercent = 0;
                YoloAnnotationService.SaveAnnotations("valid-sample.png", validImage, rois, data.ClassNamedList, data);
            }

            lines = YoloDatasetDiagnosticsService.BuildOperatorReport(data, refreshYaml: true);

            AssertTrue(lines.Any(line => line.Contains("READY")), "diagnostics did not report ready state");
            AssertTrue(lines.Any(line => line.Contains("YOLO split guide")), "diagnostics did not explain validation/test split use");
            AssertTrue(lines.Any(line => line.Contains("Test split is empty")), "diagnostics did not warn about missing test split");
            AssertTrue(lines.Any(line => line.Contains("class 'NG' has only 0")), "diagnostics did not warn about missing NG examples");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestYoloDatasetReadinessStatisticsOnDuplicateSplit()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });

            var rois = new Dictionary<string, List<CRectangleObject>>
            {
                ["OK"] = new List<CRectangleObject>
                {
                    new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
                }
            };

            using (Bitmap image = CreateSolidBitmap(40, 40, Color.Black))
            {
                data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                YoloAnnotationService.SaveAnnotations("train-duplicate.png", image, rois, data.ClassNamedList, data);
                data.ProjectSettings.YoloDataset.ValidationPercent = 100;
                YoloAnnotationService.SaveAnnotations("valid-duplicate.png", image, rois, data.ClassNamedList, data);
            }

            YoloDatasetReadinessReport report = YoloDatasetReadinessService.Build(data, refreshYaml: true);

            AssertTrue(!report.IsReady, "duplicate split should block training readiness");
            AssertTrue(report.Errors.Any(error => error.Contains("duplicate image content", StringComparison.OrdinalIgnoreCase)), "duplicate split issue was not reported");
            AssertEqual(1, report.Statistics.TrainImageCount);
            AssertEqual(1, report.Statistics.ValidImageCount);
            AssertEqual(1, report.Statistics.TrainValidImageContentOverlapCount);
            AssertTrue(report.Statistics.TrainValidImageOverlapExample.Contains("duplicate", StringComparison.OrdinalIgnoreCase), "duplicate statistics should keep an operator-readable example");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}

internal static class DatasetReadinessTestFixtures
{
    internal static CData CreatePurposeReadinessData(
        string root,
        LabelingDatasetPurpose purpose,
        bool includeBoxes,
        bool includeSegments,
        bool includeMaskOnly = false)
    {
        var data = new CData();
        data.ConfigureOutputRoot(root);
        data.ProjectSettings.DatasetPurpose = purpose;
        data.ProjectSettings.YoloDataset.ValidationPercent = 0;
        data.ProjectSettings.YoloDataset.TestPercent = 0;
        data.ClassNamedList.Add(new CClassItem { Text = "Defect", DrawColor = Color.Red });

        var rois = new Dictionary<string, List<CRectangleObject>>();
        if (includeBoxes)
        {
            rois["Defect"] = new List<CRectangleObject>
            {
                new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
            };
        }

        var segments = new Dictionary<string, List<LabelingSegmentationObject>>();
        if (includeSegments)
        {
            segments["Defect"] = new List<LabelingSegmentationObject>
            {
                new LabelingSegmentationObject(
                    new[]
                    {
                        new Point(4, 4),
                        new Point(22, 4),
                        new Point(22, 22),
                        new Point(4, 22)
                    },
                    data.ClassNamedList[0])
            };
        }

        using (Bitmap trainImage = CreateSolidBitmap(32, 32, Color.Black))
        using (Bitmap validImage = CreateSolidBitmap(32, 32, Color.White))
        {
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            YoloAnnotationService.SaveAnnotations("purpose-train.png", trainImage, rois, data.ClassNamedList, data);
            if (includeSegments)
            {
                YoloSegmentationAnnotationService.SaveSegmentationAnnotations("purpose-train.png", trainImage, segments, data.ClassNamedList, data);
            }
            else if (includeMaskOnly)
            {
                SaveMaskOnlySegmentationArtifact(data, "train", "purpose-train");
            }

            data.ProjectSettings.YoloDataset.ValidationPercent = 100;
            YoloAnnotationService.SaveAnnotations("purpose-valid.png", validImage, rois, data.ClassNamedList, data);
            if (includeSegments)
            {
                YoloSegmentationAnnotationService.SaveSegmentationAnnotations("purpose-valid.png", validImage, segments, data.ClassNamedList, data);
            }
            else if (includeMaskOnly)
            {
                SaveMaskOnlySegmentationArtifact(data, "valid", "purpose-valid");
            }
        }

        return data;
    }

    private static void SaveMaskOnlySegmentationArtifact(CData data, string mode, string fileStem)
    {
        string maskDirectory = Path.Combine(data.OutputRootPath, "data", mode, "masks");
        Directory.CreateDirectory(maskDirectory);
        using Bitmap mask = CreateSolidBitmap(32, 32, Color.White);
        mask.Save(Path.Combine(maskDirectory, $"{fileStem}.png"), System.Drawing.Imaging.ImageFormat.Png);
    }
}
