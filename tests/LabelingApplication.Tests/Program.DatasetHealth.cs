using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class DatasetHealthTests
{
    internal static void TestYoloDatasetHealthReport()
    {
        string root = CreateTempRoot();
        try
        {
            CData detectionData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "detection"),
                LabelingDatasetPurpose.ObjectDetection,
                includeBoxes: true,
                includeSegments: false);
            YoloDatasetHealthReport detection = YoloDatasetHealthService.Build(detectionData);
            AssertTrue(detection.IsReady, string.Join(Environment.NewLine, detection.Issues));
            AssertEqual(LabelingDatasetPurpose.ObjectDetection, detection.Purpose);
            AssertEqual(2, detection.TotalImageCount);
            AssertEqual(2, detection.PrimaryLabelCount);
            AssertEqual(3, detection.Splits.Count);
            AssertTrue(detection.Classes.Single(item => item.ClassName == "Defect").Count == 2,
                "detection health should use YOLO box-object counts for the class distribution");
            AssertEqual(0, detection.QualityProblemCount);
            WpfDatasetVisualQaCatalog detectionVisualQa = new WpfDatasetVisualQaService().BuildCatalog(detectionData);
            AssertEqual(2, detectionVisualQa.ScannedImageCount);
            AssertEqual(0, detectionVisualQa.ProblemCount);
            AssertEqual(2, detectionVisualQa.Items.Count);
            WpfDatasetVisualQaItem detectionPreviewItem = detectionVisualQa.Items.First();
            string detectionImageHash = ComputeFileSha256(detectionPreviewItem.ImagePath);
            AssertTrue(detectionPreviewItem.PreviewSource != null,
                "visual QA should lazily render the selected image with its saved box overlay");
            AssertEqual(detectionImageHash, ComputeFileSha256(detectionPreviewItem.ImagePath));

            CData segmentationData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "segmentation"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            YoloDatasetHealthReport segmentation = YoloDatasetHealthService.Build(segmentationData);
            AssertTrue(segmentation.IsReady, string.Join(Environment.NewLine, segmentation.Issues));
            AssertEqual(LabelingDatasetPurpose.Segmentation, segmentation.Purpose);
            AssertEqual(2, segmentation.PrimaryLabelCount);
            AssertEqual(YoloDatasetHealthQualityStatus.Healthy, segmentation.QualityStatus);
            AssertEqual(0, segmentation.QualityProblemCount);
            AssertTrue(segmentation.Splits
                    .Where(item => string.Equals(item.Split, YoloDatasetSplitService.TrainMode, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item.Split, YoloDatasetSplitService.ValidMode, StringComparison.OrdinalIgnoreCase))
                    .All(item => item.SegmentFileCount > 0),
                "segmentation health should expose saved train/valid segment files as the primary split artifact");
            AssertTrue(segmentation.Classes.Single(item => item.ClassName == "Defect").Count == 2,
                "segmentation health should use segment-object counts for the class distribution");

            CData missingSegmentationData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-missing"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            string missingSplitRoot = Path.Combine(missingSegmentationData.OutputRootPath, "data", "valid");
            File.Delete(Path.Combine(missingSplitRoot, "segments", "purpose-valid.json"));
            File.Delete(Path.Combine(missingSplitRoot, "masks", "purpose-valid.png"));
            File.Delete(Path.Combine(missingSplitRoot, "labels", "purpose-valid.txt"));
            YoloDatasetHealthReport missingSegmentation = YoloDatasetHealthService.Build(missingSegmentationData);
            AssertTrue(!missingSegmentation.IsReady, "missing SEG annotation must make the dataset not ready");
            AssertEqual(YoloDatasetHealthQualityStatus.ProblemsFound, missingSegmentation.QualityStatus);
            AssertEqual(1, missingSegmentation.QualityProblemCount);
            YoloDatasetHealthSplitSummary missingValidSplit = missingSegmentation.Splits.Single(item => item.Split == YoloDatasetSplitService.ValidMode);
            AssertEqual(1, missingValidSplit.MissingLabelCount);
            AssertEqual(0, missingValidSplit.InvalidLabelLineCount);
            var missingViewModel = new WpfDatasetHealthViewModel(missingSegmentationData);
            missingViewModel.EnsureVisualQaLoaded();
            WpfDatasetHealthMetricItem missingQualityMetric = missingViewModel.Metrics.Single(item => item.Title == "라벨 품질");
            AssertEqual("1", missingQualityMetric.Value);
            AssertTrue(missingQualityMetric.IsProblem, "missing SEG annotation must not be presented as healthy");
            AssertEqual(1, missingViewModel.VisualQaItems.Count(item => item.IsProblem));
            AssertTrue(missingViewModel.VisualQaItems.First().IsProblem,
                "visual QA should prioritize a missing canonical segmentation annotation");

            CData corruptSegmentationData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-corrupt"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            File.WriteAllText(
                Path.Combine(corruptSegmentationData.OutputRootPath, "data", "train", "segments", "purpose-train.json"),
                "{not-json");
            YoloDatasetHealthReport corruptSegmentation = YoloDatasetHealthService.Build(corruptSegmentationData);
            AssertTrue(!corruptSegmentation.IsReady, "corrupt SEG JSON must make the dataset not ready");
            AssertEqual(YoloDatasetHealthQualityStatus.ProblemsFound, corruptSegmentation.QualityStatus);
            AssertEqual(1, corruptSegmentation.QualityProblemCount);
            YoloDatasetHealthSplitSummary corruptTrainSplit = corruptSegmentation.Splits.Single(item => item.Split == YoloDatasetSplitService.TrainMode);
            AssertEqual(0, corruptTrainSplit.MissingLabelCount);
            AssertEqual(1, corruptTrainSplit.InvalidLabelLineCount);
            WpfDatasetVisualQaCatalog corruptVisualQa = new WpfDatasetVisualQaService().BuildCatalog(corruptSegmentationData);
            AssertEqual(1, corruptVisualQa.ProblemCount);
            AssertTrue(corruptVisualQa.Items.First().IsProblem,
                "visual QA should prioritize a corrupt canonical segmentation annotation");

            var unevaluatedSegmentationData = new CData();
            unevaluatedSegmentationData.ConfigureOutputRoot(Path.Combine(root, "segmentation-unevaluated"));
            unevaluatedSegmentationData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            YoloDatasetHealthReport unevaluatedSegmentation = YoloDatasetHealthService.Build(unevaluatedSegmentationData);
            AssertEqual(YoloDatasetHealthQualityStatus.NotEvaluated, unevaluatedSegmentation.QualityStatus);
            var unevaluatedViewModel = new WpfDatasetHealthViewModel(unevaluatedSegmentationData);
            WpfDatasetHealthMetricItem unevaluatedQualityMetric = unevaluatedViewModel.Metrics.Single(item => item.Title == "라벨 품질");
            AssertEqual("미확인", unevaluatedQualityMetric.Value);
            AssertTrue(unevaluatedQualityMetric.IsProblem, "unevaluated SEG quality must not be presented as healthy");

            string anomalyRoot = Path.Combine(root, "anomaly-source");
            string normalRoot = Path.Combine(anomalyRoot, "OK");
            string abnormalRoot = Path.Combine(anomalyRoot, "NG");
            Directory.CreateDirectory(normalRoot);
            Directory.CreateDirectory(abnormalRoot);
            using (Bitmap normalImage = CreateSolidBitmap(20, 20, Color.White))
            using (Bitmap abnormalImage = CreateSolidBitmap(20, 20, Color.Black))
            {
                normalImage.Save(Path.Combine(normalRoot, "normal.png"));
                abnormalImage.Save(Path.Combine(abnormalRoot, "abnormal.png"));
            }

            var anomalyData = new CData();
            anomalyData.ConfigureOutputRoot(Path.Combine(root, "anomaly-output"));
            anomalyData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            anomalyData.ProjectSettings.PythonModel.ImageRootPath = anomalyRoot;
            anomalyData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            anomalyData.ProjectSettings.YoloDataset.TestPercent = 0;
            var anomalyReviewStatus = new AnomalyImageReviewStatusService();
            string normalImagePath = Path.Combine(normalRoot, "normal.png");
            string abnormalImagePath = Path.Combine(abnormalRoot, "abnormal.png");
            anomalyReviewStatus.SetImages(new[] { normalImagePath, abnormalImagePath });
            anomalyReviewStatus.MarkNormal(normalImagePath);
            anomalyReviewStatus.MarkAbnormal(abnormalImagePath);
            anomalyReviewStatus.SaveReviewStatus(anomalyData);

            YoloDatasetHealthReport anomaly = YoloDatasetHealthService.Build(anomalyData);
            AssertTrue(anomaly.IsReady, string.Join(Environment.NewLine, anomaly.Issues));
            AssertEqual(LabelingDatasetPurpose.AnomalyDetection, anomaly.Purpose);
            AssertEqual(2, anomaly.TotalImageCount);
            AssertEqual(2, anomaly.PrimaryLabelCount);
            AssertEqual(0, anomaly.Splits.Count);
            AssertTrue(anomaly.Classes.Single(item => item.ClassName == "normal").Count == 1,
                "anomaly health should report reviewed normal images");
            AssertTrue(anomaly.Classes.Single(item => item.ClassName == "abnormal").Count == 1,
                "anomaly health should report reviewed abnormal images");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestWpfDatasetHealthWindow()
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
            CData data = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "dataset"),
                LabelingDatasetPurpose.ObjectDetection,
                includeBoxes: true,
                includeSegments: false);
            CGlobal.Inst.Data = data;

            var shell = new WpfLabelingShellWindow();
            try
            {
                shell.Show();
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(150));
                var entry = shell.FindName("OpenDatasetHealthWindowButton") as Wpf.Ui.Controls.Button;
                AssertTrue(entry != null, "model-center data tab should expose the Dataset Health entry");
                AssertTrue(entry.Command != null, "Dataset Health entry should bind through the shell ViewModel command");

                entry.Command.Execute(null);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(250));
                WpfDatasetHealthWindow healthWindow = System.Windows.Application.Current.Windows
                    .OfType<WpfDatasetHealthWindow>()
                    .FirstOrDefault(candidate => ReferenceEquals(candidate.Owner, shell));
                AssertTrue(healthWindow != null, "Dataset Health command should open a separate owned window");
                AssertEqual("데이터셋 상태 분석", healthWindow.Title);
                AssertTrue(healthWindow.GetType().BaseType?.FullName == "Wpf.Ui.Controls.FluentWindow",
                    "Dataset Health window should use the existing WPF-UI window library");
                AssertTrue(healthWindow.ViewModel?.Metrics.Count == 4, "Dataset Health should show four compact overview metrics");
                var tabs = healthWindow.FindName("DatasetHealthTabs") as System.Windows.Controls.TabControl;
                AssertTrue(tabs != null && tabs.Items.Count == 4,
                    "Dataset Health should separate overview, split/label, visual QA, and class distribution tabs");
                AssertTrue(healthWindow.FindName("DatasetHealthSplitGrid") is System.Windows.Controls.DataGrid splitGrid && splitGrid.Items.Count == 3,
                    "Dataset Health should show a saved split/label table for YOLO datasets");
                AssertTrue(healthWindow.FindName("DatasetHealthClassGrid") is System.Windows.Controls.DataGrid classGrid && classGrid.Items.Count == 1,
                    "Dataset Health should show the primary-label class distribution");
                AssertTrue(healthWindow.FindName("DatasetHealthRefreshButton") is Wpf.Ui.Controls.Button refreshButton && refreshButton.Command != null,
                    "Dataset Health refresh should bind through the ViewModel");
                AssertTrue(healthWindow.FindName("DatasetHealthVisualQaList") is System.Windows.Controls.ListBox,
                    "Dataset Health should expose an image-level visual QA worklist");
                AssertEqual(0, healthWindow.ViewModel.VisualQaItems.Count);
                tabs.SelectedItem = healthWindow.FindName("DatasetHealthVisualQaTab");
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(150));
                AssertEqual(2, healthWindow.ViewModel.VisualQaItems.Count);
                AssertTrue(healthWindow.ViewModel.SelectedVisualQaItem?.PreviewSource != null,
                    "the selected visual QA row should render a read-only saved-label preview");
                healthWindow.ViewModel.ShowOnlyVisualQaProblems = true;
                AssertEqual(0, healthWindow.ViewModel.VisualQaItems.Count);
                healthWindow.ViewModel.ShowOnlyVisualQaProblems = false;
                AssertEqual(2, healthWindow.ViewModel.VisualQaItems.Count);
                AssertTrue(healthWindow.FindName("DatasetHealthOpenInEditorButton") is Wpf.Ui.Controls.Button openInEditorButton
                    && openInEditorButton.Command != null,
                    "visual QA should expose an explicit route to the existing labeling editor");

                string selectedImagePath = healthWindow.ViewModel.SelectedVisualQaItem.ImagePath;
                string selectedImageHash = ComputeFileSha256(selectedImagePath);
                healthWindow.ViewModel.OpenSelectedVisualQaImageCommand.Execute(null);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(350));
                AssertEqual(selectedImagePath, GetPrivateField<string>(shell, "activeImagePath"));
                AssertTrue(!healthWindow.IsVisible,
                    "opening a visual QA item should close the read-only health window and return to the main editor");
                AssertEqual(selectedImageHash, ComputeFileSha256(selectedImagePath));
            }
            finally
            {
                shell.Close();
            }
        }
        finally
        {
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }
}
