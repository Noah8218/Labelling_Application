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

internal static class AnomalyClassificationTests
{
    internal static void TestAnomalyClassificationDecisionService()
    {
        var options = new AnomalyClassificationDecisionOptions
        {
            NormalClassNames = new[] { "Normal", "OK" },
            AbnormalClassNames = new[] { "Abnormal", "NG" },
            MinimumConfidence = 0.6D
        };

        var normalCandidate = new YoloWorkerSmokeCandidate
        {
            ClassName = "Normal",
            Confidence = 0.91D,
            CandidateType = "imageClassification",
            PredictionType = "classification",
            ImageLevel = true
        };
        AnomalyClassificationDecision normal = AnomalyClassificationDecisionService.Build(normalCandidate, options);
        AssertTrue(normal.IsMapped, "configured normal class should map to a review state");
        AssertEqual(AnomalyImageReviewState.Normal, normal.ReviewState);

        var abnormalCandidate = new DefectInfo
        {
            ClassName = "NG",
            Confidence = 0.87F,
            CandidateType = "imageClassification",
            PredictionType = "classification",
            ImageLevel = true
        };
        AnomalyClassificationDecision abnormal = AnomalyClassificationDecisionService.Build(abnormalCandidate, options);
        AssertTrue(abnormal.IsMapped, "configured abnormal class should map to a review state");
        AssertEqual(AnomalyImageReviewState.Abnormal, abnormal.ReviewState);

        var unknown = new YoloWorkerSmokeCandidate
        {
            ClassName = "Scratch",
            Confidence = 0.95D,
            CandidateType = "imageClassification",
            PredictionType = "classification",
            ImageLevel = true
        };
        AssertTrue(!AnomalyClassificationDecisionService.Build(unknown, options).IsMapped, "unconfigured class should not be guessed as anomaly state");

        var lowConfidence = new YoloWorkerSmokeCandidate
        {
            ClassName = "Normal",
            Confidence = 0.3D,
            CandidateType = "imageClassification",
            PredictionType = "classification",
            ImageLevel = true
        };
        AssertTrue(!AnomalyClassificationDecisionService.Build(lowConfidence, options).IsMapped, "low-confidence classification should not be mapped");

        var nonImageLevel = new YoloWorkerSmokeCandidate
        {
            ClassName = "NG",
            Confidence = 0.9D,
            CandidateType = "detection",
            PredictionType = "detection",
            ImageLevel = false
        };
        AssertTrue(!AnomalyClassificationDecisionService.Build(nonImageLevel, options).IsMapped, "box detections should not be treated as image-level anomaly classifications");

        var ambiguousOptions = new AnomalyClassificationDecisionOptions
        {
            NormalClassNames = new[] { "Mixed" },
            AbnormalClassNames = new[] { "Mixed" }
        };
        var ambiguous = new YoloWorkerSmokeCandidate
        {
            ClassName = "Mixed",
            Confidence = 0.99D,
            CandidateType = "imageClassification",
            PredictionType = "classification",
            ImageLevel = true
        };
        AssertTrue(!AnomalyClassificationDecisionService.Build(ambiguous, ambiguousOptions).IsMapped, "ambiguous mapping should not be applied automatically");
        AssertTrue(!AnomalyClassificationDecisionService.Build(normalCandidate, null).IsMapped, "missing mapping configuration should not guess anomaly state");

        AnomalyClassificationDecision aggregateNormal = AnomalyClassificationDecisionService.Build(
            new[] { unknown, normalCandidate },
            options);
        AssertTrue(aggregateNormal.IsMapped, "aggregate anomaly decision should use configured image-level classification candidates");
        AssertEqual(AnomalyImageReviewState.Normal, aggregateNormal.ReviewState);
        AnomalyClassificationDecision conflictingAggregate = AnomalyClassificationDecisionService.Build(
            new[]
            {
                normalCandidate,
                new YoloWorkerSmokeCandidate
                {
                    ClassName = "NG",
                    Confidence = 0.9D,
                    CandidateType = "imageClassification",
                    PredictionType = "classification",
                    ImageLevel = true
                }
            },
            options);
        AssertTrue(!conflictingAggregate.IsMapped, "conflicting aggregate anomaly classification should not auto-apply a review state");

        VerifyAnomalyClassificationSettingsPersistence();
    }

    private static void VerifyAnomalyClassificationSettingsPersistence()
    {
        var defaultProjectSettings = new LabelingProjectSettings
        {
            AnomalyClassification = null
        };
        defaultProjectSettings.EnsureDefaults();
        AssertTrue(defaultProjectSettings.AnomalyClassification != null, "project settings should include anomaly classification settings");
        AssertEqual(0, defaultProjectSettings.AnomalyClassification.NormalClassNames.Count);
        AssertEqual(0, defaultProjectSettings.AnomalyClassification.AbnormalClassNames.Count);
        AssertEqual(0D, defaultProjectSettings.AnomalyClassification.MinimumConfidence);

        defaultProjectSettings.AnomalyClassification.MinimumConfidence = 2D;
        defaultProjectSettings.EnsureDefaults();
        AssertEqual(1D, defaultProjectSettings.AnomalyClassification.MinimumConfidence);
        defaultProjectSettings.AnomalyClassification.MinimumConfidence = double.NaN;
        defaultProjectSettings.EnsureDefaults();
        AssertEqual(0D, defaultProjectSettings.AnomalyClassification.MinimumConfidence);

        defaultProjectSettings.AnomalyClassification.NormalClassNames.Add("Good");
        defaultProjectSettings.AnomalyClassification.AbnormalClassNames.Add("Bad");
        defaultProjectSettings.AnomalyClassification.MinimumConfidence = 0.5D;
        AnomalyClassificationDecision mappedFromSettings = AnomalyClassificationDecisionService.Build(
            new YoloWorkerSmokeCandidate
            {
                ClassName = "Bad",
                Confidence = 0.8D,
                CandidateType = "imageClassification",
                PredictionType = "classification",
                ImageLevel = true
            },
            defaultProjectSettings.AnomalyClassification.ToDecisionOptions());
        AssertTrue(mappedFromSettings.IsMapped, "configured project anomaly settings should build decision options");
        AssertEqual(AnomalyImageReviewState.Abnormal, mappedFromSettings.ReviewState);

        string recipeName = "codex_anomaly_classification_" + Guid.NewGuid().ToString("N");
        string recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
        string configPath = Path.Combine(recipeDirectory, "VISION.xml");

        try
        {
            var data = new CData();
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.AnomalyClassification.NormalClassNames.Add("OK");
            data.ProjectSettings.AnomalyClassification.AbnormalClassNames.Add("NG");
            data.ProjectSettings.AnomalyClassification.MinimumConfidence = 0.7D;
            data.SaveConfig(recipeName);

            AssertTrue(File.Exists(configPath), $"anomaly classification config was not saved: {configPath}");
            string xml = File.ReadAllText(configPath);
            AssertTrue(xml.Contains("<AnomalyClassification>", StringComparison.Ordinal), "anomaly classification settings should be serialized with project settings");
            AssertTrue(xml.Contains("<string>OK</string>", StringComparison.Ordinal), "normal anomaly class mapping should be serialized");
            AssertTrue(xml.Contains("<string>NG</string>", StringComparison.Ordinal), "abnormal anomaly class mapping should be serialized");

            CData loaded = new CData().LoadConfig(recipeName);
            loaded.ProjectSettings.EnsureDefaults();
            AssertEqual(LabelingDatasetPurpose.AnomalyDetection, loaded.ProjectSettings.DatasetPurpose);
            AssertEqual(1, loaded.ProjectSettings.AnomalyClassification.NormalClassNames.Count);
            AssertEqual("OK", loaded.ProjectSettings.AnomalyClassification.NormalClassNames[0]);
            AssertEqual(1, loaded.ProjectSettings.AnomalyClassification.AbnormalClassNames.Count);
            AssertEqual("NG", loaded.ProjectSettings.AnomalyClassification.AbnormalClassNames[0]);
            AssertEqual(0.7D, loaded.ProjectSettings.AnomalyClassification.MinimumConfidence);

            AnomalyClassificationDecision loadedDecision = AnomalyClassificationDecisionService.Build(
                new DefectInfo
                {
                    ClassName = "OK",
                    Confidence = 0.75F,
                    CandidateType = "imageClassification",
                    PredictionType = "classification",
                    ImageLevel = true
                },
                loaded.ProjectSettings.AnomalyClassification.ToDecisionOptions());
            AssertTrue(loadedDecision.IsMapped, "loaded anomaly classification settings should build decision options");
            AssertEqual(AnomalyImageReviewState.Normal, loadedDecision.ReviewState);
        }
        finally
        {
            if (Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }
        }
    }

    internal static void TestAnomalyFolderAutoReview()
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
            string sourceRoot = Path.Combine(root, "images");
            string normalRoot = Path.Combine(sourceRoot, "OK");
            string abnormalRoot = Path.Combine(sourceRoot, "NG");
            string nestedNormalRoot = Path.Combine(normalRoot, "circular");
            string nestedAbnormalRoot = Path.Combine(abnormalRoot, "circular");
            string unmatchedRoot = Path.Combine(sourceRoot, "pending");
            Directory.CreateDirectory(normalRoot);
            Directory.CreateDirectory(abnormalRoot);
            Directory.CreateDirectory(nestedNormalRoot);
            Directory.CreateDirectory(nestedAbnormalRoot);
            Directory.CreateDirectory(unmatchedRoot);

            string normalImagePath = Path.Combine(normalRoot, "normal.png");
            string nestedNormalImagePath = Path.Combine(nestedNormalRoot, "nested-normal.png");
            string manualOverrideImagePath = Path.Combine(normalRoot, "manual-override.png");
            string abnormalImagePath = Path.Combine(nestedAbnormalRoot, "abnormal.png");
            string unmatchedImagePath = Path.Combine(unmatchedRoot, "unmatched.png");
            using (Bitmap normalImage = CreateSolidBitmap(16, 12, Color.White))
            using (Bitmap nestedNormalImage = CreateSolidBitmap(16, 12, Color.WhiteSmoke))
            using (Bitmap manualOverrideImage = CreateSolidBitmap(16, 12, Color.LightGray))
            using (Bitmap abnormalImage = CreateSolidBitmap(16, 12, Color.Black))
            using (Bitmap unmatchedImage = CreateSolidBitmap(16, 12, Color.Gray))
            {
                normalImage.Save(normalImagePath);
                nestedNormalImage.Save(nestedNormalImagePath);
                manualOverrideImage.Save(manualOverrideImagePath);
                abnormalImage.Save(abnormalImagePath);
                unmatchedImage.Save(unmatchedImagePath);
            }

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "dataset"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            data.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            string[] sourceImagePaths =
            {
                normalImagePath,
                nestedNormalImagePath,
                manualOverrideImagePath,
                abnormalImagePath,
                unmatchedImagePath
            };

            List<string> interleavedImagePaths = new WpfImageQueueSelectionService()
                .InterleaveTopLevelFolderImages(sourceRoot, sourceImagePaths, CancellationToken.None);
            AssertEqual(5, interleavedImagePaths.Count);
            string[] firstQueueFolders = interleavedImagePaths
                .Take(3)
                .Select(path => Path.GetRelativePath(sourceRoot, path)
                    .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })[0])
                .ToArray();
            AssertEqual("NG", firstQueueFolders[0]);
            AssertEqual("OK", firstQueueFolders[1]);
            AssertEqual("pending", firstQueueFolders[2]);

            var manualReviewStatus = new AnomalyImageReviewStatusService();
            manualReviewStatus.SetImages(sourceImagePaths);
            manualReviewStatus.MarkAbnormal(manualOverrideImagePath);
            manualReviewStatus.SaveReviewStatus(data);

            var previewReviewStatus = new AnomalyImageReviewStatusService();
            previewReviewStatus.LoadReviewStatus(data, sourceImagePaths);
            AnomalyImageReviewFolderImportResult previewResult = previewReviewStatus.PreviewUnreviewedStatesFromParentFolders();
            AssertEqual(2, previewResult.NormalImageCount);
            AssertEqual(1, previewResult.AbnormalImageCount);
            AssertEqual(1, previewResult.ExistingReviewCount);
            AssertEqual(1, previewResult.UnmatchedImageCount);
            Dictionary<string, AnomalyImageReviewStatus> previewItems = previewReviewStatus.GetItems()
                .ToDictionary(item => item.ImagePath, StringComparer.OrdinalIgnoreCase);
            AssertEqual(AnomalyImageReviewState.Unreviewed, previewItems[normalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, previewItems[nestedNormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, previewItems[abnormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Abnormal, previewItems[manualOverrideImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, previewItems[unmatchedImagePath].ReviewState);

            AnomalyClassificationTrainingReadinessReport preConsentReadiness =
                AnomalyClassificationTrainingReadinessService.Build(data);
            AssertTrue(!preConsentReadiness.IsReady, "folder names must not make anomaly training ready before an operator approves them");
            AssertEqual(5, preConsentReadiness.SourceImageCount);
            AssertEqual(0, preConsentReadiness.NormalImageCount);
            AssertEqual(1, preConsentReadiness.AbnormalImageCount);
            AssertEqual(4, preConsentReadiness.UnreviewedImageCount);
            AssertTrue(preConsentReadiness.Errors.Any(error => error.Contains(AnomalyClassificationTrainingReadinessService.NeedsReviewedNormalAndAbnormalError, StringComparison.Ordinal)),
                "training readiness should explain that a reviewed normal class is still required before consent");

            AnomalyClassificationDatasetExportResult preConsentExport =
                new AnomalyClassificationDatasetExportService().Export(
                    data,
                    preConsentReadiness.SourceImagePaths,
                    Path.Combine(root, "pre-consent-export"));
            AssertEqual(0, preConsentExport.NormalImageCount);
            AssertEqual(1, preConsentExport.AbnormalImageCount);
            AssertEqual(4, preConsentExport.SkippedImageCount);
            AssertTrue(File.Exists(Path.Combine(preConsentExport.DatasetRootPath, "train", "abnormal", "manual-override.png")),
                "export must use the saved manual state without importing folder names");

            CGlobal.Inst.Data = data;
            WpfLabelingShellWindow rootRetentionWindow = new WpfLabelingShellWindow();
            try
            {
                AssertEqual(5, rootRetentionWindow.LoadImageQueueFromRoot(sourceRoot, loadFirstImage: true, refreshDetails: false));
                AssertEqual(sourceRoot, rootRetentionWindow.ImageQueueViewModel.CurrentImageFolderPath);
                AssertEqual(5, rootRetentionWindow.ImageQueueItems.Count);
                AssertEqual(interleavedImagePaths[0], GetPrivateField<string>(rootRetentionWindow, "activeImagePath"));
                AssertTrue(rootRetentionWindow.ImageQueueViewModel.IsAnomalyImageReviewMode,
                    "anomaly image queue should expose the dedicated image-level OK/NG review mode");
                AssertEqual("판정", rootRetentionWindow.ImageQueueViewModel.QueueDecisionColumnHeaderText);
                AssertEqual("상태", rootRetentionWindow.ImageQueueViewModel.QueueSecondaryColumnHeaderText);
                AssertEqual(System.Windows.Visibility.Collapsed, rootRetentionWindow.CanvasPanelViewModel.AnnotationWorkspaceVisibility);
                AssertEqual(0D, rootRetentionWindow.CanvasPanelViewModel.AnnotationToolRailWidth.Value);
                AssertTrue(rootRetentionWindow.ShellViewModel.IsAnomalyImageReviewMode,
                    "shell should hide object-label editing surfaces for anomaly image review");
                AssertTrue(!rootRetentionWindow.ShellViewModel.IsAnnotationWorkflowVisible,
                    "anomaly image review should hide the global label-save action");
                AssertTrue(!rootRetentionWindow.ShellViewModel.IsSavedLabelsViewVisible,
                    "anomaly image review should hide the saved-object editor tab");
                WpfImageQueueItem firstAnomalyItem = rootRetentionWindow.ImageQueueItems
                    .Single(item => string.Equals(item.ImagePath, interleavedImagePaths[0], StringComparison.OrdinalIgnoreCase));
                AssertEqual(AnomalyImageReviewState.Unreviewed, firstAnomalyItem.AnomalyReviewState);
                AssertEqual("미판정", firstAnomalyItem.LabelStatus);
                rootRetentionWindow.ImageQueueViewModel.MarkAnomalyAbnormalCommand.Execute(null);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));
                AssertTrue(!string.Equals(interleavedImagePaths[0], GetPrivateField<string>(rootRetentionWindow, "activeImagePath"), StringComparison.OrdinalIgnoreCase),
                    "saving an NG decision should advance to the next unreviewed image");
                AssertEqual(AnomalyImageReviewState.Abnormal, firstAnomalyItem.AnomalyReviewState);
                AssertEqual("NG", firstAnomalyItem.LabelStatus);

                rootRetentionWindow.ImageQueueViewModel.SelectedQueueItem = firstAnomalyItem;
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));
                rootRetentionWindow.ImageQueueViewModel.MarkAnomalyNormalCommand.Execute(null);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));
                AssertEqual(AnomalyImageReviewState.Normal, firstAnomalyItem.AnomalyReviewState);
                AssertEqual("OK", firstAnomalyItem.LabelStatus);

                rootRetentionWindow.ImageQueueViewModel.SelectedQueueItem = firstAnomalyItem;
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));
                rootRetentionWindow.ImageQueueViewModel.ClearAnomalyReviewCommand.Execute(null);
                AssertEqual(AnomalyImageReviewState.Unreviewed, firstAnomalyItem.AnomalyReviewState);
                AssertEqual("미판정", firstAnomalyItem.LabelStatus);
                var restoredButtonReview = new AnomalyImageReviewStatusService();
                restoredButtonReview.LoadReviewStatus(data, sourceImagePaths);
                AssertEqual(
                    AnomalyImageReviewState.Unreviewed,
                    restoredButtonReview.GetItems().Single(item => string.Equals(item.ImagePath, interleavedImagePaths[0], StringComparison.OrdinalIgnoreCase)).ReviewState);

                data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
                InvokePrivateResult<object>(rootRetentionWindow, "RefreshCanvasAnnotationToolScope");
                AssertTrue(!rootRetentionWindow.ImageQueueViewModel.IsAnomalyImageReviewMode,
                    "leaving anomaly purpose should restore the standard queue workflow");
                AssertEqual(System.Windows.Visibility.Visible, rootRetentionWindow.CanvasPanelViewModel.AnnotationWorkspaceVisibility);
                AssertEqual(46D, rootRetentionWindow.CanvasPanelViewModel.AnnotationToolRailWidth.Value);
                AssertTrue(rootRetentionWindow.ShellViewModel.IsAnnotationWorkflowVisible,
                    "leaving anomaly purpose should restore annotation actions");

                data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
                InvokePrivateResult<object>(rootRetentionWindow, "RefreshCanvasAnnotationToolScope");
                AssertTrue(rootRetentionWindow.ImageQueueViewModel.IsAnomalyImageReviewMode,
                    "returning to anomaly purpose should restore image-level review presentation");
                AssertEqual(AnomalyImageReviewState.Unreviewed, firstAnomalyItem.AnomalyReviewState);
                AssertEqual("미판정", firstAnomalyItem.LabelStatus);
                AssertTrue(rootRetentionWindow.ImageQueueItems.Any(item => string.Equals(item.ImagePath, normalImagePath, StringComparison.OrdinalIgnoreCase)),
                    "opening the first nested image must not replace the selected images root with NG");

                WpfImageQueueItem normalQueueItem = rootRetentionWindow.ImageQueueItems
                    .Single(item => string.Equals(item.ImagePath, normalImagePath, StringComparison.OrdinalIgnoreCase));
                rootRetentionWindow.ImageQueueViewModel.SelectedQueueItem = normalQueueItem;
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));

                AssertEqual(sourceRoot, rootRetentionWindow.ImageQueueViewModel.CurrentImageFolderPath);
                AssertEqual(5, rootRetentionWindow.ImageQueueItems.Count);
                AssertEqual(normalImagePath, rootRetentionWindow.ImageQueueViewModel.SelectedQueueItem.ImagePath);
                AssertEqual(normalImagePath, GetPrivateField<string>(rootRetentionWindow, "activeImagePath"));
            }
            finally
            {
                rootRetentionWindow.Close();
            }

            WpfLabelingShellWindow dismissWindow = new WpfLabelingShellWindow();
            try
            {
                AssertEqual(5, dismissWindow.LoadImageQueueFromRoot(sourceRoot, loadFirstImage: false, refreshDetails: false));
                AssertTrue(dismissWindow.ImageQueueViewModel.IsAnomalyFolderStateSuggestionVisible,
                    "anomaly image queue should offer, but not apply, a detected OK/NG folder mapping");
                AssertTrue(dismissWindow.ImageQueueViewModel.AnomalyFolderStateSuggestionTitleText.Contains("OK/NG 폴더 구조", StringComparison.Ordinal),
                    "anomaly image queue should explain that it detected an optional OK/NG folder mapping");
                AssertTrue(dismissWindow.ImageQueueViewModel.AnomalyFolderStateSuggestionText.Contains("총 5장", StringComparison.Ordinal),
                    "anomaly image queue should state that nested-folder images are included");
                AssertTrue(dismissWindow.ImageQueueViewModel.AnomalyFolderStateSuggestionText.Contains("OK/normal 2", StringComparison.Ordinal),
                    "folder suggestion should state the detected normal-image count");
                AssertEqual("3장 일괄 판정", dismissWindow.ImageQueueViewModel.AnomalyFolderStateSuggestionApplyText);
                string[] visibleQueueFolders = dismissWindow.ImageQueueItems
                    .Take(3)
                    .Select(item => Path.GetRelativePath(sourceRoot, item.ImagePath)
                        .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })[0])
                    .ToArray();
                AssertEqual("NG", visibleQueueFolders[0]);
                AssertEqual("OK", visibleQueueFolders[1]);
                AssertEqual("pending", visibleQueueFolders[2]);

                dismissWindow.ImageQueueViewModel.DismissAnomalyFolderStateSuggestionCommand.Execute(null);
                AssertTrue(!dismissWindow.ImageQueueViewModel.IsAnomalyFolderStateSuggestionVisible,
                    "direct-review choice should hide the suggestion for the current folder session");
                AssertEqual(5, dismissWindow.LoadImageQueueFromRoot(sourceRoot, loadFirstImage: false, refreshDetails: false));
                AssertTrue(!dismissWindow.ImageQueueViewModel.IsAnomalyFolderStateSuggestionVisible,
                    "a dismissed suggestion should stay hidden while the same image root is open");
            }
            finally
            {
                dismissWindow.Close();
            }

            var beforeConsentPersistence = new AnomalyImageReviewStatusService();
            beforeConsentPersistence.LoadReviewStatus(data, sourceImagePaths);
            Dictionary<string, AnomalyImageReviewStatus> beforeConsentItems = beforeConsentPersistence.GetItems()
                .ToDictionary(item => item.ImagePath, StringComparer.OrdinalIgnoreCase);
            AssertEqual(AnomalyImageReviewState.Unreviewed, beforeConsentItems[normalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, beforeConsentItems[nestedNormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, beforeConsentItems[abnormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Abnormal, beforeConsentItems[manualOverrideImagePath].ReviewState);

            WpfLabelingShellWindow consentWindow = new WpfLabelingShellWindow();
            try
            {
                AssertEqual(5, consentWindow.LoadImageQueueFromRoot(sourceRoot, loadFirstImage: false, refreshDetails: false));
                AssertTrue(consentWindow.ImageQueueViewModel.IsAnomalyFolderStateSuggestionVisible,
                    "a new image-queue session should make the optional mapping available again");
                consentWindow.ImageQueueViewModel.ApplyAnomalyFolderStateSuggestionCommand.Execute(null);
                AssertTrue(!consentWindow.ImageQueueViewModel.IsAnomalyFolderStateSuggestionVisible,
                    "approved mapping should close the temporary suggestion card");
            }
            finally
            {
                consentWindow.Close();
            }

            var approvedReviewStatus = new AnomalyImageReviewStatusService();
            approvedReviewStatus.LoadReviewStatus(data, sourceImagePaths);
            Dictionary<string, AnomalyImageReviewStatus> approvedItems = approvedReviewStatus.GetItems()
                .ToDictionary(item => item.ImagePath, StringComparer.OrdinalIgnoreCase);
            AssertEqual(AnomalyImageReviewState.Normal, approvedItems[normalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Normal, approvedItems[nestedNormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Abnormal, approvedItems[abnormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Abnormal, approvedItems[manualOverrideImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, approvedItems[unmatchedImagePath].ReviewState);

            AnomalyClassificationTrainingReadinessReport approvedReadiness =
                AnomalyClassificationTrainingReadinessService.Build(data);
            AssertTrue(approvedReadiness.IsReady, "approved folder states plus the saved manual review should satisfy anomaly classification readiness");
            AssertEqual(2, approvedReadiness.NormalImageCount);
            AssertEqual(2, approvedReadiness.AbnormalImageCount);
            AssertEqual(1, approvedReadiness.UnreviewedImageCount);
            AssertEqual(2, approvedReadiness.TrainNormalImageCount);
            AssertEqual(2, approvedReadiness.TrainAbnormalImageCount);

            AnomalyClassificationDatasetExportResult approvedExport =
                new AnomalyClassificationDatasetExportService().Export(
                    data,
                    approvedReadiness.SourceImagePaths,
                    Path.Combine(root, "approved-export"));
            AssertEqual(2, approvedExport.NormalImageCount);
            AssertEqual(2, approvedExport.AbnormalImageCount);
            AssertEqual(1, approvedExport.SkippedImageCount);
            AssertTrue(File.Exists(Path.Combine(approvedExport.DatasetRootPath, "train", "normal", "normal.png")),
                "approved OK-folder image should export as a normal classification sample");
            AssertTrue(File.Exists(Path.Combine(approvedExport.DatasetRootPath, "train", "normal", "nested-normal.png")),
                "approved nested OK-folder image should export as a normal classification sample");
            AssertTrue(File.Exists(Path.Combine(approvedExport.DatasetRootPath, "train", "abnormal", "abnormal.png")),
                "approved NG-folder image should export as an abnormal classification sample");
            AssertTrue(File.Exists(Path.Combine(approvedExport.DatasetRootPath, "train", "abnormal", "manual-override.png")),
                "saved manual anomaly state should not be overwritten by the parent-folder suggestion");
        }
        finally
        {
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    internal static void TestWpfAnomalyPurposeFlow()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        string previousRecipeName = CGlobal.Inst.Recipe.Name;
        string root = CreateTempRoot();
        try
        {
            var viewModel = new WpfLearningWorkflowPanelViewModel();
            viewModel.ApplyDatasetPurpose(LabelingDatasetPurpose.AnomalyDetection);
            AssertEqual(LabelingDatasetPurpose.AnomalyDetection, viewModel.GetSelectedDatasetPurpose());
            AssertTrue(viewModel.DatasetSetupFirstActionText.Contains("\uC815\uC0C1/\uC774\uC0C1", StringComparison.Ordinal), "anomaly first action should mention normal/abnormal image review");

            var data = new CData();
            data.ConfigureOutputRoot(root);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.EnsureYoloOutputDirectories();

            string normalImagePath = Path.Combine(data.TrainImagesPath, "normal.png");
            string abnormalImagePath = Path.Combine(data.TrainImagesPath, "abnormal.png");
            string unreviewedImagePath = Path.Combine(data.TrainImagesPath, "unreviewed.png");
            using (Bitmap normalImage = CreateSolidBitmap(16, 12, Color.White))
            using (Bitmap abnormalImage = CreateSolidBitmap(16, 12, Color.Black))
            using (Bitmap unreviewedImage = CreateSolidBitmap(16, 12, Color.Gray))
            {
                normalImage.Save(normalImagePath);
                abnormalImage.Save(abnormalImagePath);
                unreviewedImage.Save(unreviewedImagePath);
            }

            var imagePaths = new[] { normalImagePath, abnormalImagePath, unreviewedImagePath };
            var service = new AnomalyImageReviewStatusService();
            service.SetImages(imagePaths);
            AssertEqual(3, service.BuildSummary().TotalImageCount);
            AssertEqual(3, service.BuildSummary().UnreviewedImageCount);

            AssertEqual(AnomalyImageReviewState.Normal, service.MarkNormal(normalImagePath).ReviewState);
            AssertEqual(AnomalyImageReviewState.Abnormal, service.MarkAbnormal(abnormalImagePath).ReviewState);
            AssertTrue(service.TryFindNextUnreviewed(imagePaths, normalImagePath, out string nextImagePath), "anomaly review should find the next unreviewed image");
            AssertEqual(unreviewedImagePath, nextImagePath);

            service.SaveReviewStatus(data);
            string statusFilePath = AnomalyImageReviewStatusService.ResolveReviewStatusFilePath(data);
            AssertTrue(File.Exists(statusFilePath), "anomaly image review status file was not written");
            string statusJson = File.ReadAllText(statusFilePath);
            AssertTrue(statusJson.Contains("\"reviewStateName\": \"Normal\"", StringComparison.Ordinal), "anomaly review status should persist Normal by name");
            AssertTrue(statusJson.Contains("\"reviewStateName\": \"Abnormal\"", StringComparison.Ordinal), "anomaly review status should persist Abnormal by name");
            AssertTrue(!statusJson.Contains("unreviewed.png", StringComparison.OrdinalIgnoreCase), "unreviewed anomaly images should not be persisted as reviewed items");

            var restoredService = new AnomalyImageReviewStatusService();
            restoredService.LoadReviewStatus(data, imagePaths);
            Dictionary<string, AnomalyImageReviewStatus> restoredItems = restoredService.GetItems()
                .ToDictionary(item => item.ImagePath, StringComparer.OrdinalIgnoreCase);
            AssertEqual(AnomalyImageReviewState.Normal, restoredItems[normalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Abnormal, restoredItems[abnormalImagePath].ReviewState);
            AssertEqual(AnomalyImageReviewState.Unreviewed, restoredItems[unreviewedImagePath].ReviewState);

            AnomalyImageReviewSummary summary = restoredService.BuildSummary();
            AssertEqual(3, summary.TotalImageCount);
            AssertEqual(2, summary.ReviewedImageCount);
            AssertEqual(1, summary.NormalImageCount);
            AssertEqual(1, summary.AbnormalImageCount);
            AssertEqual(1, summary.UnreviewedImageCount);

            LabelingDatasetManifest manifest = LabelingDatasetManifestService.Build(data, "anomaly-review");
            AssertEqual(LabelingDatasetPurpose.AnomalyDetection.ToString(), manifest.DatasetPurpose);
            AssertEqual("image-level-normal-abnormal", manifest.AnnotationProfile);
            AssertEqual("panZoom", string.Join(",", manifest.VisibleTools));
            AssertEqual("image-level-normal-abnormal", manifest.ArtifactSummary.PrimaryLabelKind);
            AssertEqual(2, manifest.ArtifactSummary.PrimaryLabelCount);
            AssertEqual(3, manifest.ArtifactSummary.ImageCount);
            AssertEqual(2, manifest.ArtifactSummary.AnomalyReviewedImageCount);
            AssertEqual(1, manifest.ArtifactSummary.AnomalyNormalImageCount);
            AssertEqual(1, manifest.ArtifactSummary.AnomalyAbnormalImageCount);
            AssertEqual(1, manifest.ArtifactSummary.AnomalyUnreviewedImageCount);

            WpfDatasetDashboardMetricItem anomalyMetric = WpfAnomalyDashboardPresentationService.BuildReviewStateMetric(summary);
            AssertEqual("\uC815\uC0C1/\uC774\uC0C1", anomalyMetric.Title);
            AssertEqual("1/1/1", anomalyMetric.Value);
            AssertTrue(anomalyMetric.Detail.Contains("\uBBF8\uAC80\uD1A0 1", StringComparison.Ordinal), "anomaly dashboard metric should show unreviewed image count");
            AssertTrue(anomalyMetric.IsProblem, "anomaly dashboard metric should flag unreviewed images");

            CGlobal.Inst.Data = data;
            WpfLabelingShellWindow dashboardWindow = new WpfLabelingShellWindow();
            try
            {
                var dashboardReport = new YoloDatasetReadinessReport(
                    new YoloDatasetValidationResult(Array.Empty<string>()),
                    new YoloDatasetValidationResult(Array.Empty<string>()),
                    YoloDatasetValidator.BuildStatistics(data),
                    LabelingDatasetPurpose.AnomalyDetection);
                InvokePrivateResult<object>(dashboardWindow, "UpdateYoloTrainingChecklist", dashboardReport, false);
                WpfDatasetDashboardMetricItem dashboardMetric = dashboardWindow.LearningWorkflowViewModel.DatasetDashboardMetrics
                    .FirstOrDefault(item => item.Title.Contains("\uC815\uC0C1/\uC774\uC0C1", StringComparison.Ordinal));
                AssertTrue(dashboardMetric != null, "WPF dataset dashboard should include anomaly normal/abnormal distribution");
                AssertEqual("1/1/1", dashboardMetric.Value);
                AssertTrue(dashboardWindow.LearningWorkflowViewModel.DatasetDashboardSummaryText.Contains("anomaly normal 1", StringComparison.Ordinal), "anomaly dashboard summary should include normal count");
                AssertTrue(dashboardWindow.LearningWorkflowViewModel.DatasetDashboardIssueItems.Any(item => item.Contains("\uBBF8\uAC80\uD1A0 1", StringComparison.Ordinal)), "anomaly dashboard issues should point to unreviewed images");
            }
            finally
            {
                SetPrivateField(dashboardWindow, "isApplicationCloseApproved", true);
                dashboardWindow.Close();
            }

            string shellImageRoot = Path.Combine(root, "shell-images");
            string shellOutputRoot = Path.Combine(root, "shell-output");
            Directory.CreateDirectory(shellImageRoot);
            string shellNormalImagePath = Path.Combine(shellImageRoot, "shell-normal.png");
            string shellNextImagePath = Path.Combine(shellImageRoot, "shell-next.png");
            using (Bitmap normalImage = CreateSolidBitmap(24, 18, Color.White))
            using (Bitmap nextImage = CreateSolidBitmap(24, 18, Color.DarkGray))
            {
                normalImage.Save(shellNormalImagePath);
                nextImage.Save(shellNextImagePath);
            }

            var shellData = new CData();
            shellData.ConfigureOutputRoot(shellOutputRoot);
            shellData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            shellData.ProjectSettings.PythonModel.ImageRootPath = shellImageRoot;
            shellData.ProjectSettings.AnomalyClassification.NormalClassNames.Add("OK");
            shellData.ProjectSettings.AnomalyClassification.AbnormalClassNames.Add("NG");
            shellData.ProjectSettings.AnomalyClassification.MinimumConfidence = 0.6D;
            CGlobal.Inst.Data = shellData;
            SetPrivateField(CGlobal.Inst.Recipe, "m_strName", string.Empty);

            WpfLabelingShellWindow window = new WpfLabelingShellWindow();
            try
            {
                AssertTrue(window.TryLoadImage(shellNormalImagePath, populateQueue: true, refreshQueueDetails: false), "WPF anomaly normal-completion image load failed");
                InvokePrivateResult<object>(window, "ExecuteCompleteNoObjectAndNextCommand");
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(120));

                AssertEqual(shellNextImagePath, GetPrivateField<string>(window, "activeImagePath"));
                string shellEmptyLabelPath = Path.Combine(shellOutputRoot, "data", "train", "labels", "shell-normal.txt");
                AssertTrue(File.Exists(shellEmptyLabelPath), "WPF anomaly normal completion should save an empty YOLO label file for compatibility");
                AssertEqual(0, File.ReadAllLines(shellEmptyLabelPath).Length);

                var shellReviewStatus = new AnomalyImageReviewStatusService();
                shellReviewStatus.LoadReviewStatus(shellData, new[] { shellNormalImagePath, shellNextImagePath });
                Dictionary<string, AnomalyImageReviewStatus> shellStatuses = shellReviewStatus.GetItems()
                    .ToDictionary(item => item.ImagePath, StringComparer.OrdinalIgnoreCase);
                AssertEqual(AnomalyImageReviewState.Normal, shellStatuses[shellNormalImagePath].ReviewState);
                AssertEqual(AnomalyImageReviewState.Unreviewed, shellStatuses[shellNextImagePath].ReviewState);

                InvokePrivateResult<object>(
                    window,
                    "ApplyDetectionCandidates",
                    new[]
                    {
                        new YoloWorkerSmokeCandidate
                        {
                            ClassName = "NG",
                            Confidence = 0.93D,
                            CandidateType = "imageClassification",
                            PredictionType = "classification",
                            ImageLevel = true
                        }
                    },
                    true);
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(120));

                var classifiedReviewStatus = new AnomalyImageReviewStatusService();
                classifiedReviewStatus.LoadReviewStatus(shellData, new[] { shellNormalImagePath, shellNextImagePath });
                Dictionary<string, AnomalyImageReviewStatus> classifiedStatuses = classifiedReviewStatus.GetItems()
                    .ToDictionary(item => item.ImagePath, StringComparer.OrdinalIgnoreCase);
                AssertEqual(AnomalyImageReviewState.Abnormal, classifiedStatuses[shellNextImagePath].ReviewState);
            }
            finally
            {
                SetPrivateField(window, "isApplicationCloseApproved", true);
                window.Close();
            }
        }
        finally
        {
            CGlobal.Inst.Data = previousData;
            SetPrivateField(CGlobal.Inst.Recipe, "m_strName", previousRecipeName);
            DeleteTempRoot(root);
        }
    }

    internal static void TestWpfYoloV8AnomalyClassificationRuntimeSmoke()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        string previousRecipeName = CGlobal.Inst.Recipe.Name;
        string root = CreateTempRoot();
        try
        {
            string yoloRoot = GetEnvironmentValue("LABELING_YOLOV8_CLASSIFICATION_SMOKE_ROOT", @"C:\Git\yolov8");
            string pythonPath = GetEnvironmentValue(
                "LABELING_YOLOV8_CLASSIFICATION_SMOKE_PYTHON_EXE",
                Path.Combine(yoloRoot, ".venv", "Scripts", "python.exe"));
            string clientScriptPath = GetEnvironmentValue(
                "LABELING_YOLOV8_CLASSIFICATION_SMOKE_CLIENT",
                Path.Combine(yoloRoot, "labeling_tcp_client.py"));
            string weightsPath = GetEnvironmentValue(
                "LABELING_YOLOV8_CLASSIFICATION_SMOKE_WEIGHTS",
                Path.Combine(yoloRoot, "yolov8n-cls.pt"));
            string imagePath = GetEnvironmentValue("LABELING_YOLOV8_CLASSIFICATION_SMOKE_IMAGE", string.Empty);
            string expectedClassName = GetEnvironmentValue("LABELING_YOLOV8_CLASSIFICATION_SMOKE_EXPECTED_CLASS", string.Empty);
            string expectedStateText = GetEnvironmentValue("LABELING_YOLOV8_CLASSIFICATION_SMOKE_EXPECTED_STATE", "abnormal");
            bool expectNormal = string.Equals(expectedStateText, "normal", StringComparison.OrdinalIgnoreCase);
            AssertTrue(
                expectNormal || string.Equals(expectedStateText, "abnormal", StringComparison.OrdinalIgnoreCase),
                $"Unsupported expected anomaly review state: {expectedStateText}");
            string imageSizeText = GetEnvironmentValue("LABELING_YOLOV8_CLASSIFICATION_SMOKE_IMAGE_SIZE", "64");
            AssertTrue(int.TryParse(imageSizeText, out int inferenceImageSize) && inferenceImageSize > 0,
                $"Invalid classification smoke image size: {imageSizeText}");
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                imagePath = Path.Combine(root, "classification-smoke.png");
                using Bitmap image = CreateSolidBitmap(106, 106, Color.DarkGray);
                image.Save(imagePath);
            }
            else
            {
                imagePath = Path.GetFullPath(imagePath);
            }

            AssertTrue(Directory.Exists(yoloRoot), $"YOLOv8 root was not found: {yoloRoot}");
            AssertTrue(File.Exists(pythonPath), $"YOLOv8 smoke Python was not found: {pythonPath}");
            AssertTrue(File.Exists(clientScriptPath), $"YOLOv8 TCP adapter was not found: {clientScriptPath}");
            AssertTrue(File.Exists(weightsPath), $"YOLOv8 classification weights were not found: {weightsPath}");
            AssertTrue(File.Exists(imagePath), $"YOLOv8 classification smoke image was not found: {imagePath}");

            string outputRoot = Path.Combine(root, "dataset");
            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            data.ProjectSettings.PythonModel.ProjectRootPath = yoloRoot;
            data.ProjectSettings.PythonModel.PythonExecutablePath = pythonPath;
            data.ProjectSettings.PythonModel.ClientScriptPath = clientScriptPath;
            data.ProjectSettings.PythonModel.WeightsPath = weightsPath;
            data.ProjectSettings.PythonModel.ImageRootPath = Path.GetDirectoryName(imagePath) ?? string.Empty;
            data.ProjectSettings.PythonModel.MinimumDetectionConfidence = 0F;
            data.ProjectSettings.PythonModel.MaximumDetectionCandidates = 20;
            data.ProjectSettings.PythonModel.InferenceImageSize = inferenceImageSize;
            data.ProjectSettings.PythonModel.DetectionTimeoutSeconds = 120;
            data.ProjectSettings.PythonModel.AutoStartClient = false;

            YoloWorkerSmokeTestResult probe = YoloWorkerSmokeTestService
                .RunAsync(data.ProjectSettings.PythonModel, imagePath, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            AssertTrue(probe.Succeeded, $"YOLOv8 classification probe failed: {probe.Summary} {probe.Error}");
            YoloWorkerSmokeCandidate mappedClass = probe.Candidates.FirstOrDefault(candidate =>
                candidate.ImageLevel
                && string.Equals(candidate.CandidateType, "imageClassification", StringComparison.OrdinalIgnoreCase));
            AssertTrue(mappedClass != null, "YOLOv8 classification probe did not return an image-level classification candidate");
            AssertTrue(!string.IsNullOrWhiteSpace(mappedClass.ClassName), "YOLOv8 classification probe did not return a class name");
            if (!string.IsNullOrWhiteSpace(expectedClassName))
            {
                AssertEqual(expectedClassName, mappedClass.ClassName);
            }

            if (expectNormal)
            {
                data.ProjectSettings.AnomalyClassification.NormalClassNames.Add(mappedClass.ClassName);
            }
            else
            {
                data.ProjectSettings.AnomalyClassification.AbnormalClassNames.Add(mappedClass.ClassName);
            }
            data.ProjectSettings.AnomalyClassification.MinimumConfidence = Math.Max(0D, mappedClass.Confidence - 0.0001D);
            CGlobal.Inst.Data = data;
            SetPrivateField(CGlobal.Inst.Recipe, "m_strName", string.Empty);

            WpfLabelingShellWindow window = new WpfLabelingShellWindow();
            try
            {
                AssertTrue(window.TryLoadImage(imagePath, populateQueue: true, refreshQueueDetails: false), "WPF YOLOv8 anomaly classification smoke image load failed");
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(20));

                Task<YoloWorkerSmokeTestResult> detectionTask = InvokePrivateResult<Task<YoloWorkerSmokeTestResult>>(
                    window,
                    "RunDetectionForImageAsync",
                    imagePath,
                    true,
                    CancellationToken.None);
                AssertTrue(WaitUntilWpf(() => detectionTask.IsCompleted, TimeSpan.FromSeconds(180)), "WPF YOLOv8 anomaly classification runtime smoke did not complete");

                YoloWorkerSmokeTestResult result = detectionTask.GetAwaiter().GetResult();
                AssertTrue(result.Succeeded, $"WPF YOLOv8 anomaly classification runtime smoke failed: {result.Summary} {result.Error}");
                YoloWorkerSmokeCandidate runtimeCandidate = result.Candidates.FirstOrDefault(candidate =>
                    candidate.ImageLevel
                    && string.Equals(candidate.CandidateType, "imageClassification", StringComparison.OrdinalIgnoreCase));
                AssertTrue(runtimeCandidate != null, "WPF YOLOv8 anomaly classification runtime smoke did not return an image-level classification candidate");
                AssertEqual(mappedClass.ClassName, runtimeCandidate.ClassName);

                WpfCandidateReviewStateService candidateState = GetPrivateField<WpfCandidateReviewStateService>(window, "candidateReviewState");
                AssertTrue(candidateState.PendingCandidates.Any(candidate =>
                        candidate.ImageLevel
                        && string.Equals(candidate.CandidateType, "imageClassification", StringComparison.OrdinalIgnoreCase)),
                    "WPF YOLOv8 anomaly classification candidate was not loaded into Candidate Review state");

                var reviewStatus = new AnomalyImageReviewStatusService();
                reviewStatus.LoadReviewStatus(data, new[] { imagePath });
                AnomalyImageReviewStatus status = reviewStatus.GetItems().FirstOrDefault(item =>
                    string.Equals(item.ImagePath, imagePath, StringComparison.OrdinalIgnoreCase));
                AssertTrue(status != null, "WPF YOLOv8 anomaly classification smoke did not persist anomaly review status");
                AssertEqual(
                    expectNormal ? AnomalyImageReviewState.Normal : AnomalyImageReviewState.Abnormal,
                    status.ReviewState);
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            CGlobal.Inst.Data = previousData;
            SetPrivateField(CGlobal.Inst.Recipe, "m_strName", previousRecipeName);
            DeleteTempRoot(root);
        }
    }

    internal static void TestAnomalyClassificationDatasetExportService()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "dataset"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;

            string sourceRoot = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceRoot);
            string normalPath = Path.Combine(sourceRoot, "normal-a.png");
            string abnormalPath = Path.Combine(sourceRoot, "abnormal-a.png");
            string unreviewedPath = Path.Combine(sourceRoot, "unreviewed-a.png");
            string missingPath = Path.Combine(sourceRoot, "missing-a.png");
            using (Bitmap normalImage = CreateSolidBitmap(12, 10, Color.White))
            using (Bitmap abnormalImage = CreateSolidBitmap(12, 10, Color.Black))
            using (Bitmap unreviewedImage = CreateSolidBitmap(12, 10, Color.Gray))
            {
                normalImage.Save(normalPath);
                abnormalImage.Save(abnormalPath);
                unreviewedImage.Save(unreviewedPath);
            }

            string[] imagePaths = { normalPath, abnormalPath, unreviewedPath, missingPath };
            var reviewStatus = new AnomalyImageReviewStatusService();
            reviewStatus.SetImages(imagePaths);
            reviewStatus.MarkNormal(normalPath);
            reviewStatus.MarkAbnormal(abnormalPath);
            reviewStatus.MarkAbnormal(missingPath);
            reviewStatus.SaveReviewStatus(data);

            var service = new AnomalyClassificationDatasetExportService();
            AnomalyClassificationDatasetExportResult result = service.Export(data, imagePaths);

            AssertEqual(2, result.TotalExportedImageCount);
            AssertEqual(1, result.NormalImageCount);
            AssertEqual(1, result.AbnormalImageCount);
            AssertEqual(2, result.SkippedImageCount);
            AssertTrue(File.Exists(Path.Combine(result.DatasetRootPath, "train", "normal", "normal-a.png")), "normal reviewed image should be copied into classification train/normal");
            AssertTrue(File.Exists(Path.Combine(result.DatasetRootPath, "train", "abnormal", "abnormal-a.png")), "abnormal reviewed image should be copied into classification train/abnormal");
            AssertTrue(!File.Exists(Path.Combine(result.DatasetRootPath, "train", "normal", "unreviewed-a.png")), "unreviewed anomaly images should not be exported for classification training");

            AnomalyClassificationDatasetExportResult repeated = service.Export(data, imagePaths);
            AssertEqual(2, repeated.TotalExportedImageCount);
            AssertEqual(1, Directory.EnumerateFiles(Path.Combine(result.DatasetRootPath, "train", "normal")).Count());
            AssertEqual(1, Directory.EnumerateFiles(Path.Combine(result.DatasetRootPath, "train", "abnormal")).Count());
            AssertTrue(!File.Exists(Path.Combine(result.DatasetRootPath, "train", "normal", "normal-a-2.png")), "repeated anomaly export should replace generated splits instead of accumulating duplicate images");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestAnomalyClassificationTrainingWorkflow()
    {
        string root = CreateTempRoot();
        try
        {
            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "dataset"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYolo11;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;

            string sourceRoot = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceRoot);
            string normalPath = Path.Combine(sourceRoot, "normal-train.png");
            string abnormalPath = Path.Combine(sourceRoot, "abnormal-train.png");
            using (Bitmap normalImage = CreateSolidBitmap(12, 10, Color.White))
            using (Bitmap abnormalImage = CreateSolidBitmap(12, 10, Color.Black))
            {
                normalImage.Save(normalPath);
                abnormalImage.Save(abnormalPath);
            }

            data.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            var reviewStatus = new AnomalyImageReviewStatusService();
            reviewStatus.SetImages(new[] { normalPath, abnormalPath });
            reviewStatus.MarkNormal(normalPath);
            reviewStatus.MarkAbnormal(abnormalPath);
            reviewStatus.SaveReviewStatus(data);

            var incompatibleWorkflow = new YoloTrainingWorkflowService();
            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV5;
            AssertTrue(!incompatibleWorkflow.TryPrepareTrainingDataset(data), "YOLOv5 must not receive anomaly classification folder training requests");
            AssertTrue(
                incompatibleWorkflow.LastPreparationFailureMessage.Contains(YoloTrainingWorkflowService.AnomalyClassificationRuntimeError, StringComparison.Ordinal),
                "incompatible anomaly runtime should expose the exact YOLOv8/YOLO11 requirement");
            string incompatibleRuntimeStatus = WpfTrainingCommandPresentationService.BuildStartCommandResultStatus(
                started: false,
                incompatibleWorkflow.LastPreparationFailureMessage);
            AssertTrue(incompatibleRuntimeStatus.Contains("YOLOv8", StringComparison.Ordinal)
                && incompatibleRuntimeStatus.Contains("YOLO11", StringComparison.Ordinal),
                "incompatible anomaly runtime should be translated into an actionable operator message");

            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYolo11;
            YoloDatasetReadinessReport anomalyDatasetReport = YoloDatasetReadinessService.Build(data, refreshYaml: true);
            AssertTrue(anomalyDatasetReport.IsReady, string.Join(Environment.NewLine, anomalyDatasetReport.Errors));
            AssertEqual(2, anomalyDatasetReport.Statistics.TrainImageCount);
            AssertEqual(1, anomalyDatasetReport.Statistics.AnomalyNormalImageCount);
            AssertEqual(1, anomalyDatasetReport.Statistics.AnomalyAbnormalImageCount);
            AssertEqual(0, anomalyDatasetReport.Statistics.TotalObjectCount);
            string anomalyReadyStatus = InvokePrivateStaticResult<string>(
                typeof(WpfLabelingShellWindow),
                "BuildReadyDatasetStatusText",
                anomalyDatasetReport.Statistics,
                LabelingDatasetPurpose.AnomalyDetection,
                false);
            AssertTrue(anomalyReadyStatus.Contains("정상 1", StringComparison.Ordinal)
                && anomalyReadyStatus.Contains("이상 1", StringComparison.Ordinal)
                && !anomalyReadyStatus.Contains("결함 박스", StringComparison.Ordinal),
                "anomaly readiness should use image decisions rather than object-detection box counts");

            int port = GetAvailableTcpPort();
            using var communication = new CCommunicationLearning(startListen: false, port: port);
            using var requestReceived = new ManualResetEventSlim(false);
            AssertTrue(communication.Start(), "test TCP listener for anomaly classification training did not start");
            Task mockClient = Task.Run(() => RunMockTrainingPacketCaptureClient(
                port,
                requestReceived,
                request =>
                {
                    AssertEqual("yolo11", request.model);
                    AssertEqual("classify", request.task);
                    AssertEqual("yolo11n-cls.pt", request.weight);
                    AssertTrue(request.dataYaml.Replace("\\", "/").EndsWith("/classification", StringComparison.Ordinal), "anomaly training should send the classification dataset root as data path");
                    AssertTrue(File.Exists(Path.Combine(request.dataYaml, "train", "normal", "normal-train.png")), "normal classification training image was not exported before StartTraining");
                    AssertTrue(File.Exists(Path.Combine(request.dataYaml, "train", "abnormal", "abnormal-train.png")), "abnormal classification training image was not exported before StartTraining");
                }));
            AssertTrue(WaitUntil(() => communication.GetStatusSnapshot().IsClientConnected, TimeSpan.FromSeconds(5)), "mock anomaly training client did not connect");

            var workflow = new YoloTrainingWorkflowService();
            AssertTrue(workflow.TryStartTraining(data, communication), "anomaly classification workflow should send StartTraining when normal and abnormal examples exist");
            AssertTrue(requestReceived.Wait(TimeSpan.FromSeconds(5)), "mock anomaly training client did not receive StartTraining");
            AssertTrue(mockClient.Wait(TimeSpan.FromSeconds(5)), "mock anomaly training client did not finish");
            if (mockClient.IsFaulted && mockClient.Exception != null)
            {
                throw mockClient.Exception;
            }

            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            int yolo8Port = GetAvailableTcpPort();
            using var yolo8Communication = new CCommunicationLearning(startListen: false, port: yolo8Port);
            using var yolo8RequestReceived = new ManualResetEventSlim(false);
            AssertTrue(yolo8Communication.Start(), "test TCP listener for YOLOv8 anomaly classification training did not start");
            Task yolo8MockClient = Task.Run(() => RunMockTrainingPacketCaptureClient(
                yolo8Port,
                yolo8RequestReceived,
                request =>
                {
                    AssertEqual("yolov8", request.model);
                    AssertEqual("classify", request.task);
                    AssertEqual("yolov8n-cls.pt", request.weight);
                    AssertEqual("workflow-anomaly-yolov8", request.runName);
                    AssertTrue(request.dataYaml.Replace("\\", "/").EndsWith("/classification", StringComparison.Ordinal), "YOLOv8 anomaly training should send the classification dataset root as data path");
                    AssertTrue(File.Exists(Path.Combine(request.dataYaml, "train", "normal", "normal-train.png")), "YOLOv8 normal classification training image was not exported before StartTraining");
                    AssertTrue(File.Exists(Path.Combine(request.dataYaml, "train", "abnormal", "abnormal-train.png")), "YOLOv8 abnormal classification training image was not exported before StartTraining");
                }));
            AssertTrue(WaitUntil(() => yolo8Communication.GetStatusSnapshot().IsClientConnected, TimeSpan.FromSeconds(5)), "mock YOLOv8 anomaly training client did not connect");

            AssertTrue(workflow.TryStartTraining(data, yolo8Communication, "workflow-anomaly-yolov8"), "YOLOv8 anomaly classification workflow should send StartTraining when normal and abnormal examples exist");
            AssertTrue(yolo8RequestReceived.Wait(TimeSpan.FromSeconds(5)), "mock YOLOv8 anomaly training client did not receive StartTraining");
            AssertTrue(yolo8MockClient.Wait(TimeSpan.FromSeconds(5)), "mock YOLOv8 anomaly training client did not finish");
            if (yolo8MockClient.IsFaulted && yolo8MockClient.Exception != null)
            {
                throw yolo8MockClient.Exception;
            }

            string insufficientRoot = Path.Combine(root, "insufficient");
            var insufficientData = new CData();
            insufficientData.ConfigureOutputRoot(Path.Combine(insufficientRoot, "dataset"));
            insufficientData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            insufficientData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            insufficientData.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            var insufficientReviewStatus = new AnomalyImageReviewStatusService();
            insufficientReviewStatus.SetImages(new[] { normalPath, abnormalPath });
            insufficientReviewStatus.MarkNormal(normalPath);
            insufficientReviewStatus.SaveReviewStatus(insufficientData);
            AnomalyClassificationTrainingReadinessReport insufficientReadiness =
                AnomalyClassificationTrainingReadinessService.Build(insufficientData);
            AssertTrue(!insufficientReadiness.IsReady, "anomaly classification readiness should block missing abnormal examples");
            AssertEqual(1, insufficientReadiness.NormalImageCount);
            AssertEqual(0, insufficientReadiness.AbnormalImageCount);
            AssertTrue(!workflow.TryPrepareTrainingDataset(insufficientData), "anomaly classification training should require both normal and abnormal reviewed images");
            AssertTrue(
                workflow.LastPreparationFailureMessage.Contains(AnomalyClassificationTrainingReadinessService.NeedsReviewedNormalAndAbnormalError, StringComparison.Ordinal),
                "anomaly classification workflow should expose the specific readiness failure reason");
            string anomalyFailureStatus = WpfTrainingCommandPresentationService.BuildStartCommandResultStatus(
                started: false,
                workflow.LastPreparationFailureMessage);
            AssertTrue(anomalyFailureStatus.Contains("\uC815\uC0C1", StringComparison.Ordinal)
                && anomalyFailureStatus.Contains("\uC774\uC0C1", StringComparison.Ordinal),
                "anomaly training start failure should explain reviewed normal/abnormal requirements");
            AssertTrue(!anomalyFailureStatus.Contains(AnomalyClassificationTrainingReadinessService.NeedsReviewedNormalAndAbnormalError, StringComparison.Ordinal),
                "anomaly training start failure should not expose raw readiness keys");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestAnomalyClassificationEvaluationService()
    {
        AssertTrue(!new AnomalyClassificationEvaluationReport().IsAdoptionCandidate, "empty anomaly evaluation report should not be adoptable");
        WpfAnomalyClassificationEvaluationPresentation emptyPresentation =
            WpfAnomalyClassificationEvaluationPresentationService.Build(new AnomalyClassificationEvaluationReport());
        AssertTrue(emptyPresentation.RecommendationText.Contains("\uBCF4\uB958", StringComparison.Ordinal), "empty anomaly evaluation presentation should be hold");
        var weakSamples = new[]
        {
            new AnomalyClassificationEvaluationSample
            {
                ImagePath = "abnormal-0.png",
                ExpectedClassName = "abnormal",
                PredictedClassName = "abnormal",
                Confidence = 0.67D
            },
            new AnomalyClassificationEvaluationSample
            {
                ImagePath = "normal-0.png",
                ExpectedClassName = "normal",
                PredictedClassName = "normal",
                Confidence = 0.55D
            }
        };

        AnomalyClassificationEvaluationReport weakReport = AnomalyClassificationEvaluationService.Build(weakSamples);
        AssertTrue(!weakReport.IsAdoptionCandidate, "tiny anomaly classification evaluation should not be an adoption candidate");
        AssertEqual(2, weakReport.TotalImageCount);
        AssertEqual(1, weakReport.NormalImageCount);
        AssertEqual(1, weakReport.AbnormalImageCount);
        AssertTrue(weakReport.HoldReasons.Any(reason => reason.Contains("at least 10", StringComparison.Ordinal)), "evaluation should require enough held-out images");
        AssertTrue(weakReport.HoldReasons.Any(reason => reason.Contains("normal", StringComparison.OrdinalIgnoreCase) && reason.Contains("at least 5", StringComparison.Ordinal)), "evaluation should require enough normal held-out images");
        AssertTrue(weakReport.HoldReasons.Any(reason => reason.Contains("abnormal", StringComparison.OrdinalIgnoreCase) && reason.Contains("at least 5", StringComparison.Ordinal)), "evaluation should require enough abnormal held-out images");

        var imbalancedSamples = new List<AnomalyClassificationEvaluationSample>();
        for (int index = 0; index < 5; index++)
        {
            imbalancedSamples.Add(new AnomalyClassificationEvaluationSample
            {
                ImagePath = $"normal-{index}.png",
                ExpectedClassName = "normal",
                PredictedClassName = "normal",
                Confidence = 0.95D
            });
            imbalancedSamples.Add(new AnomalyClassificationEvaluationSample
            {
                ImagePath = $"abnormal-{index}.png",
                ExpectedClassName = "abnormal",
                PredictedClassName = index == 0 ? "abnormal" : "normal",
                Confidence = 0.95D
            });
        }

        AnomalyClassificationEvaluationReport imbalancedReport = AnomalyClassificationEvaluationService.Build(imbalancedSamples);
        AssertTrue(!imbalancedReport.IsAdoptionCandidate, "poor abnormal recall should block anomaly classification adoption");
        AssertEqual(10, imbalancedReport.TotalImageCount);
        AssertEqual(0.6D, imbalancedReport.Accuracy);
        AssertEqual(0.2D, imbalancedReport.AbnormalAccuracy);
        AssertEqual(0.6D, imbalancedReport.BalancedAccuracy);
        AssertEqual(4, imbalancedReport.FalseNegativeCount);
        AssertTrue(imbalancedReport.HoldReasons.Any(reason => reason.Contains("Abnormal accuracy", StringComparison.Ordinal)), "evaluation should expose abnormal-class accuracy blockers");

        var lowConfidenceSamples = new List<AnomalyClassificationEvaluationSample>();
        for (int index = 0; index < 5; index++)
        {
            lowConfidenceSamples.Add(new AnomalyClassificationEvaluationSample
            {
                ImagePath = $"normal-low-confidence-{index}.png",
                ExpectedClassName = "normal",
                PredictedClassName = "normal",
                Confidence = 0.79D
            });
            lowConfidenceSamples.Add(new AnomalyClassificationEvaluationSample
            {
                ImagePath = $"abnormal-low-confidence-{index}.png",
                ExpectedClassName = "abnormal",
                PredictedClassName = "abnormal",
                Confidence = 0.79D
            });
        }

        AnomalyClassificationEvaluationReport lowConfidenceReport = AnomalyClassificationEvaluationService.Build(
            lowConfidenceSamples,
            new AnomalyClassificationEvaluationOptions { MinimumConfidence = 0.8D });
        AssertTrue(!lowConfidenceReport.IsAdoptionCandidate, "low-confidence correct anomaly predictions should not be adoption candidates");
        AssertEqual(10, lowConfidenceReport.TotalImageCount);
        AssertEqual(0, lowConfidenceReport.CorrectImageCount);
        AssertEqual(10, lowConfidenceReport.LowConfidenceClassMatchCount);
        AssertEqual(0D, lowConfidenceReport.Accuracy);
        AssertTrue(lowConfidenceReport.HoldReasons.Any(reason => reason.Contains("Accuracy", StringComparison.Ordinal)), "low-confidence evaluation should surface accuracy blockers");
        AssertTrue(lowConfidenceReport.HoldReasons.Any(reason => reason.Contains("minimum confidence", StringComparison.OrdinalIgnoreCase) && reason.Contains("10", StringComparison.Ordinal)), "low-confidence evaluation should explain how many class matches failed confidence");
        WpfAnomalyClassificationEvaluationPresentation lowConfidencePresentation =
            WpfAnomalyClassificationEvaluationPresentationService.Build(
                lowConfidenceReport,
                new AnomalyClassificationEvaluationOptions { MinimumConfidence = 0.8D });
        AssertTrue(lowConfidencePresentation.RecommendationText.Contains("\uBCF4\uB958", StringComparison.Ordinal), "low-confidence anomaly evaluation should be presented as hold");
        AssertTrue(lowConfidencePresentation.MetricsText.Contains("\uB0AE\uC740 \uC2E0\uB8B0\uB3C4", StringComparison.Ordinal) && lowConfidencePresentation.MetricsText.Contains("10", StringComparison.Ordinal), "presentation metrics should show low-confidence class-match count");
        AssertTrue(lowConfidencePresentation.DetailText.Contains("\uC2E0\uB8B0\uB3C4 \uBBF8\uB2EC", StringComparison.Ordinal), "presentation detail should explain confidence blockers");

        string summaryRoot = CreateTempRoot();
        string summaryPath = Path.Combine(summaryRoot, "classification-evaluation-summary.json");
        File.WriteAllText(
            summaryPath,
            JsonConvert.SerializeObject(new
            {
                modelName = "patchcore",
                metrics = new
                {
                    totalImageCount = 4,
                    normalImageCount = 2,
                    abnormalImageCount = 2,
                    correctImageCount = 1,
                    normalCorrectCount = 1,
                    abnormalCorrectCount = 0,
                    lowConfidenceClassMatchCount = 2,
                    accuracy = 0.25,
                    normalAccuracy = 0.5,
                    abnormalAccuracy = 0.0,
                    balancedAccuracy = 0.25,
                    falsePositiveCount = 1,
                    falseNegativeCount = 2,
                    localizationEvidenceCount = 1,
                    heatmapEvidenceCount = 4
                },
                localization = new { groundTruthStatus = "not-evaluated" },
                promotion = new
                {
                    recommendation = "hold",
                    reasons = new[]
                    {
                        "2 class-matching predictions were below minimum confidence 0.8."
                    }
                }
            }));
        AnomalyClassificationEvaluationReport parsedSummary = AnomalyClassificationEvaluationService.ReadSummaryFile(summaryPath);
        AssertEqual(4, parsedSummary.TotalImageCount);
        AssertEqual(2, parsedSummary.LowConfidenceClassMatchCount);
        AssertEqual(0.25D, parsedSummary.Accuracy);
        AssertEqual("patchcore", parsedSummary.ModelName);
        AssertEqual(1, parsedSummary.LocalizationEvidenceCount);
        AssertEqual(4, parsedSummary.HeatmapEvidenceCount);
        AssertEqual("not-evaluated", parsedSummary.LocalizationGroundTruthStatus);
        AssertTrue(parsedSummary.HoldReasons.Any(reason => reason.Contains("minimum confidence", StringComparison.OrdinalIgnoreCase)), "summary loader should preserve confidence hold reasons");
        WpfAnomalyClassificationEvaluationPresentation parsedPresentation =
            WpfAnomalyClassificationEvaluationPresentationService.Build(
                parsedSummary,
                new AnomalyClassificationEvaluationOptions { MinimumConfidence = 0.8D });
        AssertTrue(parsedPresentation.DetailText.Contains("\uC2E0\uB8B0\uB3C4 \uBBF8\uB2EC", StringComparison.Ordinal), "parsed summary should feed operator-readable confidence detail");
        string holdWithoutReasonsJson = JsonConvert.SerializeObject(new
        {
            metrics = new
            {
                totalImageCount = 10,
                normalImageCount = 5,
                abnormalImageCount = 5,
                correctImageCount = 10,
                normalCorrectCount = 5,
                abnormalCorrectCount = 5,
                lowConfidenceClassMatchCount = 0,
                accuracy = 1.0,
                normalAccuracy = 1.0,
                abnormalAccuracy = 1.0
            },
            promotion = new
            {
                recommendation = "hold",
                reasons = Array.Empty<string>()
            }
        });
        AnomalyClassificationEvaluationReport holdWithoutReasonsReport =
            AnomalyClassificationEvaluationService.ParseSummaryJson(holdWithoutReasonsJson);
        AssertTrue(!holdWithoutReasonsReport.IsAdoptionCandidate, "summary loader should honor explicit hold recommendation even when reasons are absent");
        AssertEqual(1D, holdWithoutReasonsReport.BalancedAccuracy);
        AssertEqual(0, holdWithoutReasonsReport.FalsePositiveCount);
        AssertEqual(0, holdWithoutReasonsReport.FalseNegativeCount);
        WpfAnomalyClassificationEvaluationPresentation holdWithoutReasonsPresentation =
            WpfAnomalyClassificationEvaluationPresentationService.Build(holdWithoutReasonsReport);
        AssertTrue(holdWithoutReasonsPresentation.RecommendationText.Contains("\uBCF4\uB958", StringComparison.Ordinal), "explicit hold recommendation should remain visible as hold");
        string missingPromotionJson = JsonConvert.SerializeObject(new
        {
            metrics = new
            {
                totalImageCount = 10,
                normalImageCount = 5,
                abnormalImageCount = 5,
                correctImageCount = 10,
                normalCorrectCount = 5,
                abnormalCorrectCount = 5,
                lowConfidenceClassMatchCount = 0,
                accuracy = 1.0,
                normalAccuracy = 1.0,
                abnormalAccuracy = 1.0
            }
        });
        AnomalyClassificationEvaluationReport missingPromotionReport =
            AnomalyClassificationEvaluationService.ParseSummaryJson(missingPromotionJson);
        AssertTrue(!missingPromotionReport.IsAdoptionCandidate, "summary loader should require an explicit adopt recommendation");
        string modelCenterLookupRoot = Path.Combine(summaryRoot, "model-center-lookup");
        string oldEvaluationDirectory = Path.Combine(modelCenterLookupRoot, "classification-evaluation-20260708-010000");
        string latestEvaluationDirectory = Path.Combine(modelCenterLookupRoot, "classification-evaluation-20260708-020000");
        Directory.CreateDirectory(oldEvaluationDirectory);
        Directory.CreateDirectory(latestEvaluationDirectory);
        string oldEvaluationSummaryPath = Path.Combine(oldEvaluationDirectory, "classification-evaluation-summary.json");
        string latestEvaluationSummaryPath = Path.Combine(latestEvaluationDirectory, "classification-evaluation-summary.json");
        File.WriteAllText(oldEvaluationSummaryPath, File.ReadAllText(summaryPath));
        File.WriteAllText(latestEvaluationSummaryPath, File.ReadAllText(summaryPath));
        File.SetLastWriteTimeUtc(oldEvaluationSummaryPath, new DateTime(2026, 7, 8, 1, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(latestEvaluationSummaryPath, new DateTime(2026, 7, 8, 2, 0, 0, DateTimeKind.Utc));
        string modelCenterSummaryPath = InvokePrivateStaticResult<string>(
            typeof(WpfLabelingShellWindow),
            "FindModelCenterAnomalyEvaluationSummaryPath",
            modelCenterLookupRoot);
        AssertEqual(latestEvaluationSummaryPath, modelCenterSummaryPath);

        var strongSamples = new List<AnomalyClassificationEvaluationSample>();
        for (int index = 0; index < 5; index++)
        {
            strongSamples.Add(new AnomalyClassificationEvaluationSample
            {
                ImagePath = $"normal-good-{index}.png",
                ExpectedClassName = "normal",
                PredictedClassName = "normal",
                Confidence = 0.91D
            });
            strongSamples.Add(new AnomalyClassificationEvaluationSample
            {
                ImagePath = $"abnormal-good-{index}.png",
                ExpectedClassName = "abnormal",
                PredictedClassName = "abnormal",
                Confidence = 0.93D
            });
        }

        AnomalyClassificationEvaluationReport strongReport = AnomalyClassificationEvaluationService.Build(strongSamples);
        AssertTrue(strongReport.IsAdoptionCandidate, "balanced high-accuracy anomaly classification evaluation should be an adoption candidate");
        AssertEqual(10, strongReport.TotalImageCount);
        AssertEqual(5, strongReport.NormalImageCount);
        AssertEqual(5, strongReport.AbnormalImageCount);
        AssertEqual(1D, strongReport.Accuracy);
        AssertEqual(0, strongReport.HoldReasons.Count);
        WpfAnomalyClassificationEvaluationPresentation strongPresentation =
            WpfAnomalyClassificationEvaluationPresentationService.Build(strongReport);
        AssertTrue(strongPresentation.RecommendationText.Contains("\uCC44\uD0DD \uAC00\uB2A5", StringComparison.Ordinal), "strong anomaly evaluation should be presented as adoptable");
        AssertTrue(strongPresentation.ActionText.Contains("\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378", StringComparison.Ordinal), "adoptable presentation should guide model save action");

        string evaluationScript = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "evaluate-yolo-classification.ps1"));
        AssertTrue(evaluationScript.Contains("--smoke-test", StringComparison.Ordinal), "classification evaluation script should run the local YOLO adapter smoke path");
        AssertTrue(evaluationScript.Contains("persistent-adapter-batch", StringComparison.Ordinal), "classification evaluation script should reuse one loaded adapter model for large held-out datasets");
        AssertTrue(evaluationScript.Contains("UseLegacyPerImageWorker", StringComparison.Ordinal), "classification evaluation script should retain the per-image adapter path for equivalence checks");
        AssertTrue(evaluationScript.Contains("evaluationElapsedMs", StringComparison.Ordinal), "classification evaluation summary should record the measured adapter evaluation duration");
        AssertTrue(evaluationScript.Contains("imageClassification", StringComparison.Ordinal), "classification evaluation script should read image-level classification candidates");
        AssertTrue(evaluationScript.Contains("classification-evaluation-summary.json", StringComparison.Ordinal), "classification evaluation script should write a stable summary artifact");
        AssertTrue(evaluationScript.Contains("fingerprintSha256", StringComparison.Ordinal), "classification evaluation summary should persist a content fingerprint for held-out class images");
        AssertTrue(evaluationScript.Contains("weightsSha256", StringComparison.Ordinal), "classification evaluation summary should persist the evaluated weights fingerprint");
        AssertTrue(evaluationScript.Contains("minimumTotalImageCount", StringComparison.Ordinal), "classification evaluation summary should persist total-image adoption thresholds");
        AssertTrue(evaluationScript.Contains("minimumPerClassImageCount", StringComparison.Ordinal), "classification evaluation summary should persist per-class adoption thresholds");
        AssertTrue(evaluationScript.Contains("minimumConfidence", StringComparison.Ordinal), "classification evaluation summary should persist the confidence adoption threshold");
        AssertTrue(evaluationScript.Contains("lowConfidenceClassMatchCount", StringComparison.Ordinal), "classification evaluation summary should persist low-confidence class-match counts");
        AssertTrue(evaluationScript.Contains("$confidenceValue -ge $effectiveMinimumConfidence", StringComparison.Ordinal), "anomaly evaluation should apply class confidence only when the selected model uses that decision rule");
        AssertTrue(evaluationScript.Contains("checkpoint-anomaly-threshold", StringComparison.Ordinal), "PatchCore evaluation should record its checkpoint threshold decision rule instead of comparing raw scores with YOLO confidence");
        AssertTrue(evaluationScript.Contains("balancedAccuracy", StringComparison.Ordinal), "anomaly evaluation should persist balanced accuracy for class-balanced comparison");
        AssertTrue(evaluationScript.Contains("falsePositiveCount", StringComparison.Ordinal) && evaluationScript.Contains("falseNegativeCount", StringComparison.Ordinal), "anomaly evaluation should persist normal false positives and abnormal misses");
        AssertTrue(evaluationScript.Contains("groundTruthStatus = \"not-evaluated\"", StringComparison.Ordinal), "PatchCore localization should fail closed as not evaluated when location ground truth is absent");
        AssertTrue(evaluationScript.Contains("class-matching predictions were below minimum confidence", StringComparison.Ordinal), "classification evaluation script should explain confidence-gated hold reasons");
        AssertTrue(evaluationScript.Contains("recommendation = $recommendation", StringComparison.Ordinal), "classification evaluation summary should persist adopt/hold recommendation");
        AssertTrue(evaluationScript.Contains("reasons = $holdReasons", StringComparison.Ordinal), "classification evaluation summary should persist hold reasons");
        string batchEvaluationScript = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Runtime", "Python", "openvisionlab_yolo_classification_batch.py"));
        AssertTrue(batchEvaluationScript.Contains("spec_from_file_location", StringComparison.Ordinal), "batch classification evaluation should load the selected local YOLO adapter instead of a second model implementation");
        AssertTrue(batchEvaluationScript.Contains("build_detector", StringComparison.Ordinal), "batch classification evaluation should build the detector through the selected adapter");
        AssertTrue(batchEvaluationScript.Contains("detector.detect_path", StringComparison.Ordinal), "batch classification evaluation should preserve adapter-owned candidate mapping");
        AssertTrue(batchEvaluationScript.Contains("anomalyLocalization", StringComparison.Ordinal), "batch anomaly evaluation should retain PatchCore location evidence");
        AssertTrue(batchEvaluationScript.Contains("evidence_output", StringComparison.Ordinal), "PatchCore heatmaps should be routed into the evaluation artifact instead of the model checkpoint folder");

        string runRoot = CreateTempRoot();
        try
        {
            string sourceRoot = Path.Combine(runRoot, "source");
            string yoloRoot = Path.Combine(runRoot, "yolov8");
            string pythonPath = Path.Combine(yoloRoot, ".venv", "Scripts", "python.exe");
            string workerPath = Path.Combine(yoloRoot, "labeling_tcp_client.py");
            string weightsPath = Path.Combine(yoloRoot, "runs", "classify", "normal-abnormal", "weights", "best.pt");
            Directory.CreateDirectory(sourceRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(pythonPath) ?? yoloRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(weightsPath) ?? yoloRoot);
            File.WriteAllText(pythonPath, string.Empty);
            File.WriteAllText(workerPath, string.Empty);
            File.WriteAllText(weightsPath, string.Empty);

            string normalPath = Path.Combine(sourceRoot, "normal-eval.png");
            string abnormalPath = Path.Combine(sourceRoot, "abnormal-eval.png");
            using (Bitmap normalImage = CreateSolidBitmap(12, 10, Color.White))
            using (Bitmap abnormalImage = CreateSolidBitmap(12, 10, Color.Black))
            {
                normalImage.Save(normalPath);
                abnormalImage.Save(abnormalPath);
            }

            var runData = new CData();
            runData.ConfigureOutputRoot(Path.Combine(runRoot, "dataset"));
            runData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            runData.ProjectSettings.YoloDataset.ValidationPercent = 0;
            runData.ProjectSettings.YoloDataset.TestPercent = 100;
            runData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            runData.ProjectSettings.PythonModel.ProjectRootPath = yoloRoot;
            runData.ProjectSettings.PythonModel.PythonExecutablePath = pythonPath;
            runData.ProjectSettings.PythonModel.ClientScriptPath = workerPath;
            runData.ProjectSettings.PythonModel.WeightsPath = weightsPath;
            runData.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            runData.ProjectSettings.PythonModel.InferenceImageSize = 96;
            runData.ProjectSettings.AnomalyClassification.MinimumConfidence = 0.8D;

            var runReviewStatus = new AnomalyImageReviewStatusService();
            runReviewStatus.SetImages(new[] { normalPath, abnormalPath });
            runReviewStatus.MarkNormal(normalPath);
            runReviewStatus.MarkAbnormal(abnormalPath);
            runReviewStatus.SaveReviewStatus(runData);

            var runService = new WpfAnomalyClassificationEvaluationRunService(FindRepositoryRoot());
            WpfAnomalyClassificationEvaluationRunRequest request = runService.BuildRequest(runData);
            AssertEqual("yolov8", request.ModelName);
            AssertEqual("test", request.Split);
            AssertEqual(96, request.ImageSize);
            AssertEqual(0.8D, request.MinimumConfidence);
            AssertTrue(request.DatasetRootPath.Contains("classification-evaluation-input", StringComparison.Ordinal), "WPF anomaly evaluation runner should export to a fresh evaluation input folder");
            AssertTrue(File.Exists(Path.Combine(request.DatasetRootPath, "test", "normal", "normal-eval.png")), "WPF anomaly evaluation runner should export reviewed normal test images");
            AssertTrue(File.Exists(Path.Combine(request.DatasetRootPath, "test", "abnormal", "abnormal-eval.png")), "WPF anomaly evaluation runner should export reviewed abnormal test images");
            AssertEqual(0, runService.ValidateRequest(request).Count);

            string exportedNormalPath = Path.Combine(request.DatasetRootPath, "test", "normal", "normal-eval.png");
            string exportedAbnormalPath = Path.Combine(request.DatasetRootPath, "test", "abnormal", "abnormal-eval.png");
            File.Copy(exportedNormalPath, exportedAbnormalPath, overwrite: true);
            IReadOnlyList<string> sameClassContentErrors = runService.ValidateRequest(request);
            AssertTrue(
                sameClassContentErrors.Any(error =>
                    error.Contains("test normal/abnormal", StringComparison.Ordinal) &&
                    error.Contains("SHA-256", StringComparison.Ordinal)),
                "anomaly evaluation should reject exact image content reused as both normal and abnormal");
            WpfAnomalyClassificationEvaluationRunResult blockedResult =
                runService.RunAsync(request).GetAwaiter().GetResult();
            AssertTrue(!blockedResult.Succeeded, "anomaly evaluation should fail before starting the worker when exact class leakage exists");
            AssertTrue(
                blockedResult.Error.Contains("test normal/abnormal", StringComparison.Ordinal),
                "anomaly evaluation should return the actionable class-leakage validation error before worker execution");

            File.Copy(abnormalPath, exportedAbnormalPath, overwrite: true);
            string trainNormalRoot = Path.Combine(request.DatasetRootPath, "train", "normal");
            Directory.CreateDirectory(trainNormalRoot);
            File.Copy(exportedNormalPath, Path.Combine(trainNormalRoot, "renamed-train-normal.png"), overwrite: true);
            IReadOnlyList<string> crossSplitContentErrors = runService.ValidateRequest(request);
            AssertTrue(
                crossSplitContentErrors.Any(error =>
                    error.Contains("train/test", StringComparison.Ordinal) &&
                    error.Contains("SHA-256", StringComparison.Ordinal)),
                "anomaly evaluation should reject exact image content reused across train and test under a different file name");
            Directory.Delete(Path.Combine(request.DatasetRootPath, "train"), recursive: true);
            AssertEqual(0, runService.ValidateRequest(request).Count);

            IReadOnlyList<string> arguments = runService.BuildPowerShellArguments(request);
            AssertTrue(arguments.Contains("-WorkerScript"), "WPF anomaly evaluation runner should pass the local YOLOv8 TCP adapter");
            AssertTrue(arguments.Contains(workerPath), "WPF anomaly evaluation runner should pass the configured local adapter path");
            AssertTrue(arguments.Contains("-DatasetRoot"), "WPF anomaly evaluation runner should pass the exported classification dataset root");
            AssertTrue(arguments.Contains(request.DatasetRootPath), "WPF anomaly evaluation runner should pass the exported dataset path");
            AssertTrue(arguments.Contains("-MinimumConfidence"), "WPF anomaly evaluation runner should pass the confidence adoption threshold");
            AssertTrue(arguments.Contains("0.8"), "WPF anomaly evaluation runner should pass the configured confidence adoption threshold value");
            AssertTrue(arguments.Contains("-ModelName") && arguments.Contains("yolov8"), "WPF anomaly evaluation runner should pass the selected engine explicitly");

            runData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYolo11;
            WpfAnomalyClassificationEvaluationRunRequest yolo11Request = runService.BuildRequest(runData);
            AssertEqual("yolo11", yolo11Request.ModelName);
            AssertEqual(0, runService.ValidateRequest(yolo11Request).Count);
            AssertTrue(
                runService.BuildPowerShellArguments(yolo11Request).Contains(workerPath),
                "WPF anomaly evaluation runner should preserve the selected YOLO11-compatible adapter path");

            runData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EnginePatchCore;
            runData.ProjectSettings.PythonModel.ProjectRootPath = yoloRoot;
            runData.ProjectSettings.PythonModel.ClientScriptPath = workerPath;
            runData.ProjectSettings.PythonModel.WeightsPath = weightsPath;
            WpfAnomalyClassificationEvaluationRunRequest patchCoreRequest = runService.BuildRequest(runData);
            AssertEqual("patchcore", patchCoreRequest.ModelName);
            AssertEqual("cpu", patchCoreRequest.Device);
            AssertEqual(0, runService.ValidateRequest(patchCoreRequest).Count);
            IReadOnlyList<string> patchCoreArguments = runService.BuildPowerShellArguments(patchCoreRequest);
            AssertTrue(patchCoreArguments.Contains("patchcore"), "WPF anomaly evaluation runner should route the selected PatchCore worker through the shared evaluation contract");
            AssertTrue(patchCoreArguments.Contains("-MaximumCandidates"), "PatchCore evaluation should bound review-only location candidates");

            patchCoreRequest.ModelName = "unsupported";
            AssertTrue(
                runService.ValidateRequest(patchCoreRequest).Any(error =>
                    error.Contains("YOLOv8", StringComparison.Ordinal) &&
                    error.Contains("YOLO11", StringComparison.Ordinal) &&
                    error.Contains("PatchCore", StringComparison.Ordinal)),
                "WPF anomaly evaluation runner should reject model engines outside the verified YOLOv8/YOLO11/PatchCore scope");
        }
        finally
        {
            DeleteTempRoot(runRoot);
        }
    }

    internal static void TestPatchCoreAnomalyPilotContract()
    {
        AssertEqual(PythonModelSettings.EnginePatchCore, PythonModelSettings.NormalizeModelEngine("patch-core"));
        var settings = new PythonModelSettings
        {
            ModelEngine = PythonModelSettings.EnginePatchCore
        };
        AssertEqual("patchcore", settings.GetProtocolModelName());
        AssertTrue(
            PythonModelSettings.GetSupportedModelEngines().Contains(PythonModelSettings.EnginePatchCore),
            "PatchCore should be a selectable model profile");
        string workerPath = PythonModelRuntimeBundledWorkerService.ResolvePatchCoreWorkerScriptPath();
        AssertTrue(File.Exists(workerPath), $"bundled PatchCore worker is missing: {workerPath}");
        string workerSource = File.ReadAllText(workerPath);
        AssertTrue(workerSource.Contains("openvisionlab-patchcore-v1", StringComparison.Ordinal), "PatchCore worker should persist a versioned checkpoint contract");
        AssertTrue(workerSource.Contains("anomalyLocalization", StringComparison.Ordinal), "PatchCore worker should return review-only localization candidates");
        AssertTrue(workerSource.Contains("heatmapPath", StringComparison.Ordinal), "PatchCore worker should expose a heatmap artifact path");
        AssertTrue(workerSource.Contains("Resize((self.image_size, self.image_size))", StringComparison.Ordinal), "PatchCore preprocessing should preserve the full frame instead of center-cropping border defects");

        string root = CreateTempRoot();
        try
        {
            string sourceRoot = Path.Combine(root, "source");
            Directory.CreateDirectory(sourceRoot);
            string normalOnePath = Path.Combine(sourceRoot, "normal-01.png");
            string normalTwoPath = Path.Combine(sourceRoot, "normal-02.png");
            string abnormalPath = Path.Combine(sourceRoot, "abnormal-reference.png");
            using (Bitmap normalOne = CreateSolidBitmap(18, 14, Color.LightGray))
            using (Bitmap normalTwo = CreateSolidBitmap(18, 14, Color.Silver))
            using (Bitmap abnormal = CreateSolidBitmap(18, 14, Color.Black))
            {
                normalOne.Save(normalOnePath);
                normalTwo.Save(normalTwoPath);
                abnormal.Save(abnormalPath);
            }

            var data = new CData();
            data.ConfigureOutputRoot(Path.Combine(root, "dataset"));
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EnginePatchCore;
            data.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;

            var reviewStatus = new AnomalyImageReviewStatusService();
            reviewStatus.SetImages(new[] { normalOnePath, normalTwoPath, abnormalPath });
            reviewStatus.MarkNormal(normalOnePath);
            reviewStatus.MarkNormal(normalTwoPath);
            reviewStatus.MarkAbnormal(abnormalPath);
            reviewStatus.SaveReviewStatus(data);

            PatchCoreAnomalyTrainingReadinessReport readiness = PatchCoreAnomalyTrainingReadinessService.Build(data);
            AssertTrue(readiness.IsReady, string.Join(Environment.NewLine, readiness.Errors));
            AssertEqual(2, readiness.TrainNormalCount);
            AssertEqual(1, readiness.ReviewedAbnormalCount);
            AssertTrue(
                readiness.Warnings.Contains(PatchCoreAnomalyTrainingReadinessService.NoIndependentCalibrationWarning),
                "missing normal validation images should be disclosed as train-normal threshold fallback");

            var preparation = new YoloTrainingDatasetPreparationService();
            AssertTrue(preparation.TryPrepare(data), preparation.LastPreparationFailureMessage);
            string exportRoot = Path.Combine(data.OutputRootPath, "patchcore");
            AssertTrue(File.Exists(Path.Combine(exportRoot, "train", "normal", "normal-01.png")), "PatchCore export should include reviewed normal train images");
            AssertTrue(File.Exists(Path.Combine(exportRoot, "train", "abnormal", "abnormal-reference.png")), "abnormal review evidence may be exported for evaluation even though the worker excludes it from learning");
            string preparationSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "1. Core", "Model", "YoloTrainingDatasetPreparationService.cs"));
            AssertTrue(preparationSource.Contains("Task = \"anomaly\"", StringComparison.Ordinal), "PatchCore training should use the explicit anomaly task contract");

            string envelope = "{\"type\":\"DetectImageResult\",\"ok\":true,\"candidates\":[{\"className\":\"abnormal\",\"confidence\":0.82,\"x\":4,\"y\":5,\"width\":8,\"height\":6,\"candidateType\":\"anomalyLocalization\",\"predictionType\":\"patchcore\",\"imageLevel\":true,\"anomalyScore\":0.67,\"anomalyThreshold\":0.42,\"heatmapPath\":\"D:\\\\evidence\\\\heatmap.png\"}]}";
            DetectionResultParseResult parsed = PythonDetectionResultProtocol.Parse(envelope);
            AssertEqual(DetectionResultParseStatus.Parsed, parsed.Status);
            DefectInfo defect = parsed.Defects.Single();
            AssertEqual(0.67D, defect.AnomalyScore.Value);
            AssertEqual(0.42D, defect.AnomalyThreshold.Value);
            AssertTrue(defect.HeatmapPath.EndsWith("heatmap.png", StringComparison.OrdinalIgnoreCase), "PatchCore heatmap path should survive the TCP result contract");

            var candidate = new YoloWorkerSmokeCandidate
            {
                ClassName = defect.ClassName,
                Confidence = defect.Confidence,
                X = defect.X,
                Y = defect.Y,
                Width = defect.Width,
                Height = defect.Height,
                CandidateType = defect.CandidateType,
                PredictionType = defect.PredictionType,
                ImageLevel = defect.ImageLevel,
                AnomalyScore = defect.AnomalyScore,
                AnomalyThreshold = defect.AnomalyThreshold,
                HeatmapPath = defect.HeatmapPath
            };
            string detail = WpfCandidateReviewPresenter.BuildDetail(
                candidate,
                candidate.ToRectangle(),
                new WpfCandidateOverlapInfo(string.Empty, Rectangle.Empty, 0D),
                0F);
            AssertTrue(detail.Contains("PatchCore 점수", StringComparison.Ordinal), "PatchCore candidate detail should distinguish raw score from decision confidence");
            AssertTrue(detail.Contains("미확정 검토 결과", StringComparison.Ordinal), "PatchCore location must remain a review-only result");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestRealPatchCoreAppSmoke()
    {
        string root = Environment.GetEnvironmentVariable("OPENVISIONLAB_PATCHCORE_SMOKE_ROOT") ?? string.Empty;
        string python = Environment.GetEnvironmentVariable("OPENVISIONLAB_PATCHCORE_PYTHON") ?? string.Empty;
        AssertTrue(Directory.Exists(root), "OPENVISIONLAB_PATCHCORE_SMOKE_ROOT must point to the prepared D-drive evidence root");
        AssertTrue(File.Exists(python), "OPENVISIONLAB_PATCHCORE_PYTHON must point to the verified Python executable");

        string weights = Path.Combine(root, "runtime", "runs", "anomaly", "pilot", "weights", "best.pt");
        string image = Path.Combine(root, "test", "abnormal-test.png");
        var settings = new PythonModelSettings
        {
            ModelEngine = PythonModelSettings.EnginePatchCore,
            PythonExecutablePath = python,
            ProjectRootPath = Path.Combine(root, "runtime"),
            ClientScriptPath = PythonModelRuntimeBundledWorkerService.ResolvePatchCoreWorkerScriptPath(),
            WeightsPath = weights,
            ImageRootPath = Path.GetDirectoryName(image) ?? string.Empty,
            InferenceImageSize = 128,
            DetectionTimeoutSeconds = 120
        };

        YoloWorkerSmokeTestResult result = YoloWorkerSmokeTestService.RunAsync(settings, image)
            .GetAwaiter()
            .GetResult();
        AssertTrue(result.Succeeded, result.Summary + Environment.NewLine + result.Error + Environment.NewLine + result.Output);
        YoloWorkerSmokeCandidate candidate = result.Candidates.FirstOrDefault();
        AssertTrue(candidate != null, "real PatchCore app smoke should return an abnormal location candidate");
        AssertEqual("patchcore", candidate.PredictionType);
        AssertTrue(candidate.ImageLevel, "PatchCore location must remain an image-level review result");
        AssertTrue(
            candidate.AnomalyScore.HasValue
                && candidate.AnomalyThreshold.HasValue
                && candidate.AnomalyScore.Value > candidate.AnomalyThreshold.Value,
            "real PatchCore defect score should exceed the learned threshold");
        AssertTrue(candidate.Width > 0 && candidate.Height > 0, "real PatchCore app smoke should preserve location bounds");
        AssertTrue(File.Exists(candidate.HeatmapPath), "real PatchCore app smoke should preserve the generated heatmap path");
    }
}
