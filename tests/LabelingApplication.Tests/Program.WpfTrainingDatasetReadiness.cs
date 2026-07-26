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

using static TestSupport;

internal static class WpfTrainingDatasetReadinessTests
{
    internal static void TestWpfYoloTrainingChecklistDatasetQualityPresentation()
    {
        if (System.Windows.Application.Current == null)
        {
            _ = new System.Windows.Application
            {
                ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
            };
        }

        CData previousData = CGlobal.Inst.Data;
        using IDisposable startupRestoreScope = SuppressLastOpenedDatasetRestore();
        string root = CreateTempRoot();
        WpfLabelingShellWindow window = null;
        try
        {
            CData warningData = CreateDatasetQualityPresentationData(Path.Combine(root, "warning"), duplicateSplit: false, includeMissingClass: true);
            CGlobal.Inst.Data = warningData;
            window = new WpfLabelingShellWindow();

            YoloDatasetReadinessReport warningReport = YoloDatasetReadinessService.Build(warningData, refreshYaml: true);
            AssertTrue(warningReport.IsReady, string.Join(Environment.NewLine, warningReport.Errors));
            InvokePrivateResult<object>(window, "UpdateYoloTrainingChecklist", warningReport, false);

            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistStatusText.Contains("주의 후 학습 가능", StringComparison.Ordinal), "ready dataset warnings should be visible in the guide status");
            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistDetailText.Contains("train 1", StringComparison.Ordinal), "ready warning detail should keep train count");
            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistActionText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "ready warning action should explain missing final verification data");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardStatusText.Contains("\uD559\uC2B5", StringComparison.Ordinal), "ready dataset dashboard should show a trainable status");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementStatusText.Contains("\uBCF4\uB958", StringComparison.Ordinal), "ready dataset without test split should allow training but hold model replacement");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementDetailText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "model replacement detail should name the missing final verification data");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Count >= 8, "ready dataset dashboard should expose image, labeling progress, split, replacement, label, class, and duplicate metrics");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uAD50\uCCB4", StringComparison.Ordinal) && item.IsWarning), "ready dataset without test split should mark replacement as a warning metric");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uC9C4\uD589", StringComparison.Ordinal) && item.ActionKind == WpfDatasetDashboardActionKind.OpenLabelingProgress), "ready dataset dashboard should show labeling progress as a clickable tool shortcut");
            WpfDatasetDashboardMetricItem qualityMetric = window.LearningWorkflowViewModel.DatasetDashboardMetrics.First(item => item.Title.Contains("\uD488\uC9C8", StringComparison.Ordinal));
            AssertEqual("\uD488\uC9C8 \uBCF4\uACE0\uC11C", qualityMetric.Title);
            AssertEqual(WpfDatasetDashboardActionKind.ExportQualityAudit, qualityMetric.ActionKind);
            AssertTrue(qualityMetric.Detail.Contains("Markdown", StringComparison.Ordinal), "quality audit metric should explain that clicking saves a Markdown report");
            AssertTrue(qualityMetric.Value == "OK" && !qualityMetric.IsProblem, "ready dataset dashboard should show a non-problem quality audit metric");
            window.LearningWorkflowViewModel.DatasetDashboardMetricCommand.Execute(qualityMetric);
            string qualityAuditPath = YoloDatasetQualityAuditExportService.ResolveDefaultOutputPath(warningData);
            AssertTrue(File.Exists(qualityAuditPath), "quality audit metric should save the current dataset report");
            AssertTrue(File.ReadAllText(qualityAuditPath).Contains("# Dataset Quality Audit", StringComparison.Ordinal), "saved quality audit metric report should contain the audit title");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardSummaryText.Contains("quality missing 0", StringComparison.Ordinal), "ready dataset dashboard summary should include quality audit counts");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.First().Contains("\uB2E4\uC74C:", StringComparison.Ordinal), "ready dataset dashboard should begin with a learner-facing next action");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.First().Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "ready dataset next action should explain the final verification data before model replacement");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.Any(item => item.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal)), "ready dataset dashboard should surface quality warnings in the operator issue list");
            InvokePrivateResult<object>(window, "UpdateYoloCommandButtons");
            AssertTrue(!window.LearningWorkflowViewModel.IsRunModelComparisonEnabled, "model comparison should stay disabled when the held-out test split is empty");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonActionText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "model comparison action text should explain that final verification data is required");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonToolTipText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "model comparison tooltip should explain the final verification requirement");

            CData duplicateData = CreateDatasetQualityPresentationData(Path.Combine(root, "duplicate"), duplicateSplit: true, includeMissingClass: false);
            CGlobal.Inst.Data = duplicateData;
            YoloDatasetReadinessReport duplicateReport = YoloDatasetReadinessService.Build(duplicateData, refreshYaml: true);
            AssertTrue(!duplicateReport.IsReady, "duplicate split should not be marked ready");
            InvokePrivateResult<object>(window, "UpdateYoloTrainingChecklist", duplicateReport, false);

            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistStatusText.Contains("학습 불가", StringComparison.Ordinal), "blocking split issue should be shown as not trainable");
            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistStatusText.Contains("중복", StringComparison.Ordinal), "blocking split issue should name duplicate separation");
            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistDetailText.Contains("train 1", StringComparison.Ordinal), "blocking detail should keep train count");
            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistDetailText.Contains("valid 1", StringComparison.Ordinal), "blocking detail should keep valid count");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardStatusText.Contains("\uD559\uC2B5", StringComparison.Ordinal), "blocking dataset dashboard should show the learning-readiness category");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardStatusText.Contains("\uBD88\uAC00", StringComparison.Ordinal), "blocking dataset dashboard should show that training is not allowed");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementStatusText.Contains("\uBD88\uAC00", StringComparison.Ordinal), "blocking dataset should mark model replacement impossible");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementDetailText.Contains("\uD559\uC2B5 \uBD88\uAC00", StringComparison.Ordinal), "model replacement detail should point back to blocking training issues");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uAD50\uCCB4", StringComparison.Ordinal) && item.IsProblem), "blocking dataset dashboard should mark replacement as a problem metric");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uC911\uBCF5", StringComparison.Ordinal) && item.IsProblem), "blocking dataset dashboard should mark duplicate split as a problem metric");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uC9C4\uD589", StringComparison.Ordinal) && item.Value.Contains("/", StringComparison.Ordinal)), "blocking dataset dashboard should keep visible labeling progress");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uD488\uC9C8", StringComparison.Ordinal) && !item.IsProblem), "duplicate-only blocking dataset should keep quality audit separate from split overlap problems");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.First().Contains("\uB2E4\uC74C:", StringComparison.Ordinal), "blocking dataset dashboard should begin with a learner-facing next action");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.First().Contains("\uD559\uC2B5/\uAC80\uC99D/\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "blocking split next action should point at split separation");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.Any(item => item.Contains("\uD559\uC2B5/\uAC80\uC99D/\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal)), "blocking dataset dashboard should give a plain duplicate split action");
            InvokePrivateResult<object>(window, "UpdateYoloCommandButtons");
            AssertTrue(!window.LearningWorkflowViewModel.IsRunModelComparisonEnabled, "model comparison should stay disabled while dataset readiness is blocking");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonActionText.Contains("\uD559\uC2B5", StringComparison.Ordinal), "blocking comparison action should point back to training readiness");
            AssertTrue(window.LearningWorkflowViewModel.TrainingChecklistActionText.Contains("달라야", StringComparison.Ordinal), "blocking split action should tell the operator to separate validation/test images");
        }
        finally
        {
            window?.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    internal static void TestWpfModelComparisonButtonRequiresHeldOutTestSplit()
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
        WpfLabelingShellWindow window = null;
        try
        {
            CData unlabeledTestData = CreateDatasetQualityPresentationData(
                Path.Combine(root, "test-unlabeled"),
                duplicateSplit: false,
                includeMissingClass: true,
                includeTestSplit: true,
                testImageCount: 3,
                includeTestLabels: false);
            CGlobal.Inst.Data = unlabeledTestData;
            window = new WpfLabelingShellWindow();

            YoloDatasetReadinessReport unlabeledReport = YoloDatasetReadinessService.Build(unlabeledTestData, refreshYaml: true);
            AssertTrue(!unlabeledReport.IsReady, "unlabeled final verification images should make the dataset check fail before model comparison");
            AssertTrue(unlabeledReport.Errors.Any(error => error.Contains("test label file is missing", StringComparison.OrdinalIgnoreCase)), "unlabeled final verification images should report missing test label files");
            AssertTrue(unlabeledReport.Statistics.TestImageCount > 0, "unlabeled test dataset should include held-out images");
            AssertEqual(0, unlabeledReport.Statistics.TestLabelCount);

            InvokePrivateResult<object>(window, "UpdateYoloTrainingChecklist", unlabeledReport, false);
            InvokePrivateResult<object>(window, "UpdateYoloCommandButtons");

            AssertTrue(!window.LearningWorkflowViewModel.IsRunModelComparisonEnabled, "model comparison should stay disabled when held-out test images have no answer labels");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonActionText.Contains("\uD559\uC2B5", StringComparison.Ordinal), "unlabeled final verification data should keep model comparison blocked by dataset readiness");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementStatusText.Contains("\uBD88\uAC00", StringComparison.Ordinal), "unlabeled final verification data should make replacement impossible until dataset errors are fixed");
            AssertTrue(window.LearningWorkflowViewModel.ModelComparisonBasisText.Contains("\uD559\uC2B5 \uAC00\uB2A5", StringComparison.Ordinal), "unlabeled final verification basis should point back to training readiness before comparison");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uD488\uC9C8", StringComparison.Ordinal) && item.IsProblem && item.Value.Contains("3", StringComparison.Ordinal)), "unlabeled final verification dashboard should mark missing labels in the quality metric");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.Any(item => item.Contains("\uB204\uB77D \uB77C\uBCA8 3", StringComparison.Ordinal)), "unlabeled final verification dashboard should surface quality-audit missing label count");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.Any(item => item.Contains("test label file is missing", StringComparison.OrdinalIgnoreCase)), "unlabeled final verification dashboard should surface missing test label files");

            CData data = CreateDatasetQualityPresentationData(
                Path.Combine(root, "test-ready"),
                duplicateSplit: false,
                includeMissingClass: true,
                includeTestSplit: true);
            CGlobal.Inst.Data = data;
            window.Close();
            window = new WpfLabelingShellWindow();

            YoloDatasetReadinessReport report = YoloDatasetReadinessService.Build(data, refreshYaml: true);
            AssertTrue(report.IsReady, string.Join(Environment.NewLine, report.Errors));
            AssertTrue(report.Statistics.TestImageCount > 0, "test-ready dataset should include a held-out test image");

            InvokePrivateResult<object>(window, "UpdateYoloTrainingChecklist", report, false);
            InvokePrivateResult<object>(window, "UpdateYoloCommandButtons");

            AssertTrue(window.LearningWorkflowViewModel.IsRunModelComparisonEnabled, "model comparison should enable only after dataset readiness and a non-empty test split");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonActionText.Contains("\uBAA8\uB378", StringComparison.Ordinal), "ready comparison action should show the normal model comparison command");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonToolTipText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "ready comparison tooltip should name the final verification image count");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementStatusText.Contains("\uADFC\uAC70 \uBD80\uC871", StringComparison.Ordinal), "one held-out image should allow comparison but warn that model-adoption evidence is weak");
            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementDetailText.Contains("\uAD8C\uC7A5", StringComparison.Ordinal), "weak model-adoption evidence should explain the recommended final verification count");
            AssertTrue(window.LearningWorkflowViewModel.RunModelComparisonToolTipText.Contains("\uADFC\uAC70\uAC00 \uC57D", StringComparison.Ordinal), "model comparison tooltip should warn when final verification evidence is weak");
            AssertTrue(window.LearningWorkflowViewModel.ModelComparisonBasisText.Contains("\uCD5C\uC885 \uAC80\uC99D \uB77C\uBCA8 1\uC7A5", StringComparison.Ordinal), "weak comparison basis should show the exact held-out label count");
            AssertTrue(window.LearningWorkflowViewModel.ModelComparisonBasisText.Contains("\uAD8C\uC7A5 10\uC7A5", StringComparison.Ordinal), "weak comparison basis should show the recommended held-out label count");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uAD50\uCCB4", StringComparison.Ordinal) && item.IsWarning && item.Value.Contains("\uC8FC\uC758", StringComparison.Ordinal)), "weak final verification evidence should mark the replacement metric as a warning");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.First().Contains("\uB2E4\uC74C:", StringComparison.Ordinal), "weak evidence dashboard should begin with a next action");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardIssueItems.First().Contains("\uB354 \uD655\uBCF4", StringComparison.Ordinal), "weak evidence next action should ask for more final verification images");

            CData strongData = CreateDatasetQualityPresentationData(
                Path.Combine(root, "test-strong"),
                duplicateSplit: false,
                includeMissingClass: true,
                includeTestSplit: true,
                testImageCount: 10);
            CGlobal.Inst.Data = strongData;
            YoloDatasetReadinessReport strongReport = YoloDatasetReadinessService.Build(strongData, refreshYaml: true);
            AssertTrue(strongReport.IsReady, string.Join(Environment.NewLine, strongReport.Errors));
            AssertTrue(strongReport.Statistics.TestImageCount >= 10, "strong test-ready dataset should include enough final verification images");

            InvokePrivateResult<object>(window, "UpdateYoloTrainingChecklist", strongReport, false);
            InvokePrivateResult<object>(window, "UpdateYoloCommandButtons");

            AssertTrue(window.LearningWorkflowViewModel.ModelReplacementStatusText.Contains("\uAC00\uB2A5", StringComparison.Ordinal), "recommended final verification count should mark model replacement as possible");
            AssertTrue(!window.LearningWorkflowViewModel.ModelReplacementDetailText.Contains("\uADFC\uAC70\uAC00 \uC57D", StringComparison.Ordinal), "strong model-adoption evidence should not keep the weak-evidence warning");
            AssertTrue(window.LearningWorkflowViewModel.ModelComparisonBasisText.Contains("\uCD5C\uC885 \uAC80\uC99D \uB77C\uBCA8 10\uC7A5", StringComparison.Ordinal), "strong comparison basis should show the held-out label count");
            AssertTrue(window.LearningWorkflowViewModel.ModelComparisonBasisText.Contains("\uAD50\uCCB4 \uD310\uB2E8 \uAC00\uB2A5", StringComparison.Ordinal), "strong comparison basis should explain that replacement decision is now possible");
            AssertTrue(window.LearningWorkflowViewModel.DatasetDashboardMetrics.Any(item => item.Title.Contains("\uAD50\uCCB4", StringComparison.Ordinal) && !item.IsWarning && item.Value.Contains("\uAC00\uB2A5", StringComparison.Ordinal)), "recommended final verification count should clear the replacement warning metric");
        }
        finally
        {
            window?.Close();
            CGlobal.Inst.Data = previousData;
            DeleteTempRoot(root);
        }
    }

    private static CData CreateDatasetQualityPresentationData(string root, bool duplicateSplit, bool includeMissingClass, bool includeTestSplit = false, int testImageCount = 0, bool includeTestLabels = true)
    {
        var data = new CData();
        data.ConfigureOutputRoot(root);
        data.ClassNamedList.Add(new CClassItem { Text = "OK", DrawColor = Color.Green });
        if (includeMissingClass)
        {
            data.ClassNamedList.Add(new CClassItem { Text = "NG", DrawColor = Color.Red });
        }

        var rois = new Dictionary<string, List<CRectangleObject>>
        {
            ["OK"] = new List<CRectangleObject>
            {
                new CRectangleObject { Roi = new Rectangle(5, 5, 10, 10), cClassItem = data.ClassNamedList[0] }
            }
        };

        using (Bitmap trainImage = CreateSolidBitmap(40, 40, Color.Black))
        using (Bitmap validImage = CreateSolidBitmap(40, 40, duplicateSplit ? Color.Black : Color.White))
        {
            data.ProjectSettings.YoloDataset.ValidationPercent = 0;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            YoloAnnotationService.SaveAnnotations("train-sample.png", trainImage, rois, data.ClassNamedList, data);
            data.ProjectSettings.YoloDataset.ValidationPercent = 100;
            data.ProjectSettings.YoloDataset.TestPercent = 0;
            YoloAnnotationService.SaveAnnotations("valid-sample.png", validImage, rois, data.ClassNamedList, data);
            if (includeTestSplit)
            {
                int finalVerificationImageCount = Math.Max(1, testImageCount);
                for (int i = 0; i < finalVerificationImageCount; i++)
                {
                    data.ProjectSettings.YoloDataset.ValidationPercent = 0;
                    data.ProjectSettings.YoloDataset.TestPercent = 100;
                    using Bitmap testImage = CreateSolidBitmap(40, 40, Color.FromArgb(0, 0, Math.Min(255, 40 + i)));
                    if (includeTestLabels)
                    {
                        YoloAnnotationService.SaveAnnotations($"test-sample-{i}.png", testImage, rois, data.ClassNamedList, data);
                    }
                    else
                    {
                        Directory.CreateDirectory(data.TestImagesPath);
                        testImage.Save(Path.Combine(data.TestImagesPath, $"test-sample-{i}.png"));
                    }
                }
            }
        }

        return data;
    }
}
