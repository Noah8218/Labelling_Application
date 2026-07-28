using MvcVisionSystem;
using MvcVisionSystem.Yolo;
using System;
using System.IO;
using System.Linq;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class BatchDetectionPreflightTests
{
    internal static void TestBatchDetectionPreflightContract()
    {
        string root = CreateTempRoot();
        try
        {
            CData data = CreateReadyData(root);
            string unlabeledPath = Path.Combine(root, "unlabeled.png");
            string labeledPath = Path.Combine(root, "labeled.png");
            File.WriteAllText(unlabeledPath, "image");
            File.WriteAllText(labeledPath, "image");
            WpfImageQueueItem unlabeled = WpfImageQueueItem.CreateShell(unlabeledPath);
            WpfImageQueueItem labeled = WpfImageQueueItem.CreateShell(labeledPath);
            labeled.IsLabeled = true;
            var service = new WpfBatchDetectionPreflightService();

            WpfBatchDetectionPreflightReport defaultReport = service.DryRun(
                new WpfBatchDetectionPreflightRequest
                {
                    Data = data,
                    Items = new[] { unlabeled, labeled },
                    ScopeText = "visible rows",
                    ExistingLabelPolicy = WpfBatchExistingLabelPolicy.SkipLabeled
                });

            AssertTrue(defaultReport.CanStart, string.Join(Environment.NewLine, defaultReport.Issues));
            AssertEqual(2, defaultReport.RequestedCount);
            AssertEqual(1, defaultReport.RunnableItems.Count);
            AssertEqual(1, defaultReport.ExistingLabelCount);
            AssertEqual(1, defaultReport.SkippedExistingLabelCount);
            AssertEqual(unlabeledPath, defaultReport.RunnableItems.Single().ImagePath);
            AssertEqual(2, defaultReport.ClassMappings.Count);
            AssertEqual("className \"OK\" \u2192 Recipe \"OK\"", defaultReport.ClassMappings[0].WorkerMappingText);
            AssertTrue(
                defaultReport.DestinationPolicyText.Contains("Candidate Review", StringComparison.Ordinal),
                "preflight must route results to Candidate Review");
            AssertTrue(
                defaultReport.DestinationPolicyText.Contains("\uC790\uB3D9 \uC2B9\uC778", StringComparison.Ordinal),
                "preflight must state that candidates are not auto-approved");
            AssertTrue(
                defaultReport.DestinationPolicyText.Contains("\uC790\uB3D9 \uC800\uC7A5", StringComparison.Ordinal),
                "preflight must state that labels are not auto-saved");

            WpfBatchDetectionPreflightReport includeReport = service.DryRun(
                new WpfBatchDetectionPreflightRequest
                {
                    Data = data,
                    Items = new[] { unlabeled, labeled },
                    ScopeText = "visible rows",
                    ExistingLabelPolicy = WpfBatchExistingLabelPolicy.IncludeAndKeep
                });

            AssertTrue(includeReport.CanStart, string.Join(Environment.NewLine, includeReport.Issues));
            AssertEqual(2, includeReport.RunnableItems.Count);
            AssertEqual(0, includeReport.SkippedExistingLabelCount);
            AssertTrue(
                includeReport.Warnings.Any(item => item.Contains("\uBCF4\uC874", StringComparison.Ordinal)),
                "include policy must warn that existing labels stay preserved");

            var viewModel = new WpfBatchDetectionPreflightViewModel(
                data,
                new[] { unlabeled, labeled },
                "visible rows",
                service);
            AssertTrue(viewModel.CanStart, "ready dry-run should enable explicit Start");
            WpfBatchDetectionPlan selectedPlan = null;
            viewModel.StartRequested += (_, plan) => selectedPlan = plan;
            viewModel.StartCommand.Execute(null);
            AssertTrue(selectedPlan != null, "Start should emit the approved dry-run plan");
            AssertEqual(1, selectedPlan.Items.Count);

            WpfBatchDetectionPreflightReport missingReport = service.DryRun(
                new WpfBatchDetectionPreflightRequest
                {
                    Data = data,
                    Items = new[] { WpfImageQueueItem.CreateShell(Path.Combine(root, "missing.png")) },
                    ScopeText = "missing"
                });
            AssertTrue(!missingReport.CanStart, "missing image must block Start");
            AssertTrue(
                missingReport.Issues.Any(item => item.Contains("\uD30C\uC77C", StringComparison.Ordinal)),
                "missing image should be surfaced as a blocking file issue");

            data.ClassNamedList.Add(new CClassItem { Text = "ok" });
            WpfBatchDetectionPreflightReport duplicateClassReport = service.DryRun(
                new WpfBatchDetectionPreflightRequest
                {
                    Data = data,
                    Items = new[] { unlabeled },
                    ScopeText = "duplicate class"
                });
            AssertTrue(!duplicateClassReport.CanStart, "ambiguous class mapping must block Start");
            AssertTrue(
                duplicateClassReport.Issues.Any(item => item.Contains("\uC911\uBCF5", StringComparison.Ordinal)),
                "duplicate class issue should explain the ambiguous mapping");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static CData CreateReadyData(string root)
    {
        string modelRoot = Path.Combine(root, "model");
        Directory.CreateDirectory(modelRoot);
        string clientPath = Path.Combine(modelRoot, "worker.py");
        string weightsPath = Path.Combine(modelRoot, "best.pt");
        File.WriteAllText(clientPath, "# worker");
        File.WriteAllText(weightsPath, "weights");
        var data = new CData();
        data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
        data.ProjectSettings.PythonModel = new PythonModelSettings
        {
            ModelEngine = PythonModelSettings.EngineYoloV5,
            ProjectRootPath = modelRoot,
            ClientScriptPath = clientPath,
            WeightsPath = weightsPath,
            MinimumDetectionConfidence = 0.35F
        };
        data.ClassNamedList.Add(new CClassItem { Text = "OK" });
        data.ClassNamedList.Add(new CClassItem { Text = "NG" });
        return data;
    }
}
