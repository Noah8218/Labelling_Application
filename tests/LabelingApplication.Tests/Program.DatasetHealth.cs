using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
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

            CData classFilterData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "detection-class-filter"),
                LabelingDatasetPurpose.ObjectDetection,
                includeBoxes: true,
                includeSegments: false);
            classFilterData.ClassNamedList.Add(new CClassItem
            {
                Text = "Scratch",
                DrawColor = Color.Orange
            });
            string classFilterValidLabel = Directory.EnumerateFiles(
                Path.Combine(classFilterData.OutputRootPath, "data", "valid", "labels")).Single();
            string originalValidLine = File.ReadAllText(classFilterValidLabel);
            File.WriteAllText(
                classFilterValidLabel,
                "1" + originalValidLine.Substring(originalValidLine.IndexOf(' ')));
            var classFilterTreeBefore = Directory
                .EnumerateFiles(classFilterData.OutputRootPath, "*", SearchOption.AllDirectories)
                .ToDictionary(
                    path => Path.GetRelativePath(classFilterData.OutputRootPath, path),
                    ComputeFileSha256,
                    StringComparer.OrdinalIgnoreCase);
            var classFilterViewModel = new WpfDatasetHealthViewModel(classFilterData);
            classFilterViewModel.EnsureVisualQaLoaded();
            AssertTrue(
                classFilterViewModel.VisualQaClassFilters.Select(item => item.Text).SequenceEqual(new[]
                {
                    WpfDatasetHealthViewModel.AllVisualQaClasses,
                    "0 · Defect",
                    "1 · Scratch"
                }),
                "visual QA class filters should preserve canonical Recipe index order");
            WpfDatasetVisualQaClassFilterItem scratchFilter =
                classFilterViewModel.VisualQaClassFilters.Single(item => item.ClassIndex == 1);
            classFilterViewModel.SelectedVisualQaClassFilter = scratchFilter;
            AssertEqual(1, classFilterViewModel.VisualQaItems.Count);
            AssertTrue(
                classFilterViewModel.VisualQaItems.Single().ContainsClassIndex(1),
                "Scratch filter should rebuild the bounded catalog with only Scratch images");
            AssertTrue(
                classFilterViewModel.VisualQaStatusText.Contains("클래스 1 · Scratch", StringComparison.Ordinal),
                "class-filtered worklist status should name the canonical class");
            classFilterViewModel.SelectedVisualQaSplitFilter = YoloDatasetSplitService.TrainMode;
            AssertEqual(0, classFilterViewModel.VisualQaItems.Count);
            classFilterViewModel.SelectedVisualQaSplitFilter = YoloDatasetSplitService.ValidMode;
            AssertEqual(1, classFilterViewModel.VisualQaItems.Count);
            classFilterViewModel.ShowOnlyVisualQaProblems = true;
            AssertEqual(0, classFilterViewModel.VisualQaItems.Count);
            classFilterViewModel.ShowOnlyVisualQaProblems = false;
            classFilterViewModel.Refresh(classFilterData);
            AssertEqual(1, classFilterViewModel.SelectedVisualQaClassFilter.ClassIndex);
            AssertEqual(YoloDatasetSplitService.ValidMode, classFilterViewModel.SelectedVisualQaSplitFilter);
            classFilterData.ClassNamedList.RemoveAt(1);
            classFilterViewModel.Refresh(classFilterData);
            AssertTrue(
                !classFilterViewModel.SelectedVisualQaClassFilter.ClassIndex.HasValue,
                "refresh should fall back to all classes when the selected class no longer exists");
            var classFilterTreeAfter = Directory
                .EnumerateFiles(classFilterData.OutputRootPath, "*", SearchOption.AllDirectories)
                .ToDictionary(
                    path => Path.GetRelativePath(classFilterData.OutputRootPath, path),
                    ComputeFileSha256,
                    StringComparer.OrdinalIgnoreCase);
            AssertEqual(classFilterTreeBefore.Count, classFilterTreeAfter.Count);
            AssertTrue(
                classFilterTreeBefore.All(pair =>
                    classFilterTreeAfter.TryGetValue(pair.Key, out string hash)
                    && string.Equals(pair.Value, hash, StringComparison.Ordinal)),
                "class, split, and problem filtering plus refresh must leave every dataset file byte-identical");

            string trainImageDirectory = Path.Combine(detectionData.OutputRootPath, "data", "train", "images");
            string trainLabelDirectory = Path.Combine(detectionData.OutputRootPath, "data", "train", "labels");
            string testImageDirectory = Path.Combine(detectionData.OutputRootPath, "data", "test", "images");
            string testLabelDirectory = Path.Combine(detectionData.OutputRootPath, "data", "test", "labels");
            string trainSourceImage = Directory.EnumerateFiles(trainImageDirectory).First();
            string trainSourceLabel = Directory.EnumerateFiles(trainLabelDirectory).First();
            string validSourceImage = Directory.EnumerateFiles(
                Path.Combine(detectionData.OutputRootPath, "data", "valid", "images")).First();
            string validSourceLabel = Directory.EnumerateFiles(
                Path.Combine(detectionData.OutputRootPath, "data", "valid", "labels")).First();
            for (int index = 0; index < WpfDatasetVisualQaService.HealthySampleCount + 4; index++)
            {
                string stem = $"train-extra-{index:00}";
                File.Copy(
                    trainSourceImage,
                    Path.Combine(trainImageDirectory, stem + Path.GetExtension(trainSourceImage)));
                File.Copy(trainSourceLabel, Path.Combine(trainLabelDirectory, stem + ".txt"));
            }
            Directory.CreateDirectory(testImageDirectory);
            Directory.CreateDirectory(testLabelDirectory);
            File.Copy(
                validSourceImage,
                Path.Combine(testImageDirectory, "test-sample" + Path.GetExtension(validSourceImage)));
            File.Copy(validSourceLabel, Path.Combine(testLabelDirectory, "test-sample.txt"));
            WpfDatasetVisualQaCatalog balancedVisualQa = new WpfDatasetVisualQaService().BuildCatalog(detectionData);
            AssertEqual(WpfDatasetVisualQaService.HealthySampleCount, balancedVisualQa.Items.Count);
            AssertTrue(
                new[] { "train", "valid", "test" }.All(split =>
                    balancedVisualQa.Items.Any(item =>
                        string.Equals(item.SplitText, split, StringComparison.OrdinalIgnoreCase))),
                "healthy visual QA sampling should keep every existing split reachable even when train exceeds the sample budget");

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

            CData segmentationClassFilterData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-class-filter"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            segmentationClassFilterData.ClassNamedList.Add(new CClassItem
            {
                Text = "Scratch",
                DrawColor = Color.Orange
            });
            string segmentationClassFilterPath = Directory.EnumerateFiles(
                Path.Combine(segmentationClassFilterData.OutputRootPath, "data", "valid", "segments")).Single();
            SegmentationAnnotationFile segmentationClassFilterAnnotation =
                JsonConvert.DeserializeObject<SegmentationAnnotationFile>(
                    File.ReadAllText(segmentationClassFilterPath));
            segmentationClassFilterAnnotation.Polygons[0].ClassIndex = 1;
            segmentationClassFilterAnnotation.Polygons[0].ClassName = "Scratch";
            File.WriteAllText(
                segmentationClassFilterPath,
                JsonConvert.SerializeObject(segmentationClassFilterAnnotation, Formatting.Indented));
            WpfDatasetVisualQaCatalog segmentationScratchCatalog =
                new WpfDatasetVisualQaService().BuildCatalog(segmentationClassFilterData, classIndex: 1);
            AssertEqual(2, segmentationScratchCatalog.ScannedImageCount);
            AssertEqual(1, segmentationScratchCatalog.MatchedImageCount);
            AssertEqual(1, segmentationScratchCatalog.Items.Count);
            AssertTrue(
                segmentationScratchCatalog.Items.Single().ContainsClassIndex(1),
                "segmentation class filter should use canonical segment JSON class indexes");

            CData missingSegmentationData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                Path.Combine(root, "segmentation-missing"),
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            string missingSplitRoot = Path.Combine(missingSegmentationData.OutputRootPath, "data", "valid");
            File.Delete(Path.Combine(missingSplitRoot, "segments", "purpose-valid.json"));
            File.Delete(Path.Combine(missingSplitRoot, "masks", "purpose-valid.png"));
            File.Delete(Path.Combine(missingSplitRoot, "labels", "purpose-valid.txt"));
            var missingTreeBeforeFiltering = Directory
                .EnumerateFiles(missingSegmentationData.OutputRootPath, "*", SearchOption.AllDirectories)
                .ToDictionary(
                    path => Path.GetRelativePath(missingSegmentationData.OutputRootPath, path),
                    ComputeFileSha256,
                    StringComparer.OrdinalIgnoreCase);
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
            AssertTrue(
                missingViewModel.VisualQaSplitFilters.SequenceEqual(new[]
                {
                    WpfDatasetHealthViewModel.AllVisualQaSplits,
                    YoloDatasetSplitService.TrainMode,
                    YoloDatasetSplitService.ValidMode
                }),
                "visual QA split filters should be derived from existing catalog items in canonical order");
            missingViewModel.SelectedVisualQaSplitFilter = YoloDatasetSplitService.ValidMode;
            AssertEqual(1, missingViewModel.VisualQaItems.Count);
            AssertTrue(missingViewModel.VisualQaItems.Single().IsProblem,
                "valid split filter should retain the missing valid annotation");
            missingViewModel.ShowOnlyVisualQaProblems = true;
            AssertEqual(1, missingViewModel.VisualQaItems.Count);
            missingViewModel.SelectedVisualQaSplitFilter = YoloDatasetSplitService.TrainMode;
            AssertEqual(0, missingViewModel.VisualQaItems.Count);
            AssertTrue(
                missingViewModel.VisualQaStatusText.Contains("train 분할 · 문제만", StringComparison.Ordinal),
                "combined split/problem filter should remain visible in the worklist status");
            missingViewModel.ShowOnlyVisualQaProblems = false;
            AssertEqual(1, missingViewModel.VisualQaItems.Count);
            missingViewModel.Refresh(missingSegmentationData);
            AssertEqual(YoloDatasetSplitService.TrainMode, missingViewModel.SelectedVisualQaSplitFilter);
            AssertEqual(1, missingViewModel.VisualQaItems.Count);
            AssertTrue(
                missingViewModel.VisualQaItems.All(item => item.SplitText == YoloDatasetSplitService.TrainMode),
                "refresh should retain a still-valid split filter and rebuild the visible list safely");
            var missingTreeAfterFiltering = Directory
                .EnumerateFiles(missingSegmentationData.OutputRootPath, "*", SearchOption.AllDirectories)
                .ToDictionary(
                    path => Path.GetRelativePath(missingSegmentationData.OutputRootPath, path),
                    ComputeFileSha256,
                    StringComparer.OrdinalIgnoreCase);
            AssertEqual(missingTreeBeforeFiltering.Count, missingTreeAfterFiltering.Count);
            AssertTrue(
                missingTreeBeforeFiltering.All(pair =>
                    missingTreeAfterFiltering.TryGetValue(pair.Key, out string hash)
                    && string.Equals(pair.Value, hash, StringComparison.Ordinal)),
                "split/problem filtering and refresh must leave every dataset file byte-identical");

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
            data.ClassNamedList.Add(new CClassItem
            {
                Text = "Scratch",
                DrawColor = Color.Orange
            });
            string validLabelPath = Directory.EnumerateFiles(
                Path.Combine(data.OutputRootPath, "data", "valid", "labels")).Single();
            string validLabelLine = File.ReadAllText(validLabelPath);
            File.WriteAllText(
                validLabelPath,
                "1" + validLabelLine.Substring(validLabelLine.IndexOf(' ')));
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
                AssertTrue(healthWindow.FindName("DatasetHealthClassGrid") is System.Windows.Controls.DataGrid classGrid && classGrid.Items.Count == 2,
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
                var splitFilter = healthWindow.FindName("DatasetHealthVisualQaSplitFilter") as System.Windows.Controls.ComboBox;
                AssertTrue(
                    splitFilter != null && splitFilter.Items.Count == 3,
                    "Dataset Health visual QA should expose all plus the existing train/valid split filters");
                AssertEqual(WpfDatasetHealthViewModel.AllVisualQaSplits, splitFilter.SelectedItem as string);
                var classFilter = healthWindow.FindName("DatasetHealthVisualQaClassFilter")
                    as System.Windows.Controls.ComboBox;
                AssertTrue(
                    classFilter != null && classFilter.Items.Count == 3,
                    "Dataset Health visual QA should expose all plus the two canonical Recipe classes");
                AssertTrue(
                    classFilter.SelectedItem is WpfDatasetVisualQaClassFilterItem allClass
                    && !allClass.ClassIndex.HasValue,
                    "Dataset Health class filter should start at all classes");
                healthWindow.ViewModel.SelectedVisualQaClassFilter =
                    healthWindow.ViewModel.VisualQaClassFilters.Single(item => item.ClassIndex == 1);
                AssertEqual(1, healthWindow.ViewModel.VisualQaItems.Count);
                AssertTrue(
                    healthWindow.ViewModel.VisualQaItems.Single().ContainsClassIndex(1),
                    "WPF class selection should narrow the read-only worklist to the chosen class");
                healthWindow.ViewModel.SelectedVisualQaSplitFilter = YoloDatasetSplitService.ValidMode;
                AssertEqual(YoloDatasetSplitService.ValidMode, splitFilter.SelectedItem as string);
                AssertEqual(1, healthWindow.ViewModel.VisualQaItems.Count);
                AssertTrue(
                    healthWindow.ViewModel.VisualQaItems.All(item => item.SplitText == YoloDatasetSplitService.ValidMode),
                    "valid split selection should narrow the read-only visual QA worklist");
                healthWindow.ViewModel.RefreshCommand.Execute(null);
                AssertEqual(YoloDatasetSplitService.ValidMode, healthWindow.ViewModel.SelectedVisualQaSplitFilter);
                AssertEqual(1, healthWindow.ViewModel.SelectedVisualQaClassFilter.ClassIndex);
                AssertEqual(1, healthWindow.ViewModel.VisualQaItems.Count);
                healthWindow.ViewModel.ShowOnlyVisualQaProblems = true;
                AssertEqual(0, healthWindow.ViewModel.VisualQaItems.Count);
                healthWindow.ViewModel.ShowOnlyVisualQaProblems = false;
                AssertEqual(1, healthWindow.ViewModel.VisualQaItems.Count);
                healthWindow.ViewModel.SelectedVisualQaClassFilter =
                    healthWindow.ViewModel.VisualQaClassFilters.First(item => !item.ClassIndex.HasValue);
                healthWindow.ViewModel.SelectedVisualQaSplitFilter = WpfDatasetHealthViewModel.AllVisualQaSplits;
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
