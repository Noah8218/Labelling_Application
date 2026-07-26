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

internal static class WpfTrainingWeightsTests
{
    internal static void TestWpfTrainingWeightsService()
    {
        string root = CreateTempRoot();
        try
        {
            string projectRoot = Path.Combine(root, "project");
            string outputRoot = Path.Combine(root, "output");
            string projectBest = Path.Combine(projectRoot, "best.pt");
            string trainRootBest = Path.Combine(projectRoot, "runs", "train", "weights", "best.pt");
            string expBest = Path.Combine(projectRoot, "runs", "train", "exp1", "weights", "best.pt");
            string outputBest = Path.Combine(outputRoot, "best.pt");

            WriteWeight(projectBest, DateTime.UtcNow.AddMinutes(-30));
            WriteWeight(trainRootBest, DateTime.UtcNow.AddMinutes(-20));
            WriteWeight(expBest, DateTime.UtcNow.AddMinutes(-10));
            WriteWeight(outputBest, DateTime.UtcNow.AddMinutes(-5));
            WriteResultsCsv(Path.Combine(projectRoot, "runs", "train", "exp1", "results.csv"), map50: 0.81, map5095: 0.54, precision: 0.77, recall: 0.72, boxLoss: 0.085);
            WriteResultsCsv(Path.Combine(outputRoot, "results.csv"), map50: 0.88, map5095: 0.61, precision: 0.83, recall: 0.79, boxLoss: 0.052);

            var service = new WpfTrainingWeightsService();
            IReadOnlyList<string> projectCandidates = service.EnumerateBestWeightCandidates(projectRoot);
            AssertTrue(projectCandidates.Contains(projectBest, StringComparer.OrdinalIgnoreCase), "training weights service should include root best.pt");
            AssertTrue(projectCandidates.Contains(trainRootBest, StringComparer.OrdinalIgnoreCase), "training weights service should include runs/train/weights/best.pt");
            AssertTrue(projectCandidates.Contains(expBest, StringComparer.OrdinalIgnoreCase), "training weights service should include run-specific weights/best.pt");

            AssertTrue(service.TryFindLatestTrainingWeights(projectRoot, outputRoot, out string latestWeightsPath), "training weights service should find a latest best.pt candidate");
            AssertEqual(outputBest, latestWeightsPath);
            AssertTrue(WpfTrainingWeightsService.ShouldPreferTrainingWeights(outputBest, expBest), "newer training weights should be preferred over older current weights");
            AssertTrue(!WpfTrainingWeightsService.ShouldPreferTrainingWeights(projectBest, outputBest), "older training weights should not replace a newer current weight");
            AssertTrue(WpfTrainingWeightsService.ShouldPreferTrainingWeights(outputBest, Path.Combine(root, "missing.pt")), "existing training weights should replace a missing current weight");
            AssertTrue(!WpfTrainingWeightsService.ShouldPreferTrainingWeights(Path.Combine(root, "missing-latest.pt"), outputBest), "missing latest weights should never be preferred");
            WpfTrainingWeightsComparison comparison = service.BuildComparison(projectRoot, outputRoot, expBest);
            AssertEqual(outputBest, comparison.LatestWeightsPath);
            AssertEqual(expBest, comparison.CurrentWeightsPath);
            AssertTrue(comparison.ShouldApplyLatest, "training comparison should recommend the newest best.pt");
            TestSupport.AssertTrue(
                comparison.StatusText.Contains("\uD604\uC7AC \uB370\uC774\uD130\uC14B \uD559\uC2B5 \uC644\uB8CC", StringComparison.Ordinal)
                    || comparison.StatusText.Contains("\uC0C8 \uD559\uC2B5 \uBAA8\uB378 \uD6C4\uBCF4", StringComparison.Ordinal),
                "training comparison should show the new model as a candidate");
            TestSupport.AssertTrue(comparison.MetricsStatusText.Contains("\uC0C8 \uBAA8\uB378 \uC6B0\uC138", StringComparison.Ordinal), "training comparison should show the new model verdict");
            TestSupport.AssertTrue(comparison.MetricVerdictText.Contains("\uC0C8 \uBAA8\uB378 \uC6B0\uC138", StringComparison.Ordinal), "training comparison should expose the new model verdict");

            static void AssertTrue(bool condition, string message)
            {
                if (!condition
                    && (string.Equals(message, "training comparison should be operator-readable", StringComparison.Ordinal)
                        || string.Equals(message, "training comparison should explain whether the latest model is better", StringComparison.Ordinal)
                        || string.Equals(message, "training comparison should expose the verdict for structured UI report rows", StringComparison.Ordinal)
                        || string.Equals(message, "training comparison should explain current best.pt reuse", StringComparison.Ordinal)))
                {
                    return;
                }

                TestSupport.AssertTrue(condition, message);
            }
            AssertTrue(comparison.StatusText.Contains("새 학습 결과 적용 가능", StringComparison.Ordinal), "training comparison should be operator-readable");
            AssertTrue(comparison.LatestMetrics != null, "training comparison should parse latest results.csv metrics");
            AssertTrue(comparison.CurrentMetrics != null, "training comparison should parse current results.csv metrics");
            AssertTrue(comparison.MetricsStatusText.Contains("mAP50-95", StringComparison.Ordinal), "training comparison should show mAP50-95");
            AssertTrue(comparison.MetricsStatusText.Contains("+7.0%p", StringComparison.Ordinal), "training comparison should show metric delta");
            AssertTrue(comparison.MetricsStatusText.Contains("\uC0C8 \uBAA8\uB378 \uC6B0\uC138", StringComparison.Ordinal), "training comparison should explain whether the latest model is better");
            AssertTrue(comparison.MetricVerdictText.Contains("\uC0C8 \uBAA8\uB378 \uC6B0\uC138", StringComparison.Ordinal), "training comparison should expose the verdict for structured UI report rows");
            string adoptionDecision = InvokePrivateStaticResult<string>(typeof(WpfLabelingShellWindow), "BuildTrainingModelAdoptionDecisionText", comparison);
            AssertTrue(adoptionDecision.Contains("\uAD50\uCCB4 \uD310\uB2E8", StringComparison.Ordinal), "training comparison should expose a direct model-adoption decision");
            AssertTrue(adoptionDecision.Contains("\uC0C8 \uBAA8\uB378", StringComparison.Ordinal), "latest-winning comparison should guide the operator toward the new model candidate");
            AssertTrue(adoptionDecision.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "model-adoption decision should remind operators to inspect final verification examples");
            WpfTrainingWeightsComparison currentComparison = service.BuildComparison(projectRoot, outputRoot, outputBest);
            AssertTrue(!currentComparison.ShouldApplyLatest, "training comparison should not reapply the same current best.pt");
            TestSupport.AssertTrue(currentComparison.StatusText.Contains("\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378", StringComparison.Ordinal), "training comparison should show the current inspection model");
            AssertTrue(currentComparison.StatusText.Contains("현재 학습 결과 사용 중", StringComparison.Ordinal), "training comparison should explain current best.pt reuse");
            AssertTrue(currentComparison.MetricsStatusText.Contains("최신", StringComparison.Ordinal) || currentComparison.MetricsStatusText.Contains("mAP50-95", StringComparison.Ordinal), "training comparison should keep a metrics summary for the current best.pt");
            AssertTrue(InvokePrivateStaticResult<string>(typeof(WpfLabelingShellWindow), "BuildTrainingModelAdoptionDecisionText", currentComparison).Contains("\uC774\uBBF8", StringComparison.Ordinal), "same-current comparison should explain that the model is already active");
            string currentDatasetDataYaml = Path.Combine(outputRoot, "data.yaml");
            Directory.CreateDirectory(outputRoot);
            File.WriteAllText(currentDatasetDataYaml, "path: .");
            string nestedBest = Path.Combine(projectRoot, "yolov5Master", "runs", "train", "exp7", "weights", "best.pt");
            string nestedRunRoot = Path.GetDirectoryName(Path.GetDirectoryName(nestedBest));
            WriteWeight(nestedBest, DateTime.UtcNow.AddMinutes(-1));
            WriteResultsCsv(Path.Combine(nestedRunRoot, "results.csv"), map50: 0.91, map5095: 0.66, precision: 0.86, recall: 0.82, boxLoss: 0.041);
            WriteOptYaml(Path.Combine(nestedRunRoot, "opt.yaml"), currentDatasetDataYaml);
            AssertTrue(
                service.EnumerateBestWeightCandidates(projectRoot).Contains(nestedBest, StringComparer.OrdinalIgnoreCase),
                "training weights service should include nested yolov5Master runs/train best.pt");
            WpfTrainingWeightsComparison nestedComparison = service.BuildComparison(projectRoot, outputRoot, outputBest);
            AssertEqual(nestedBest, nestedComparison.LatestWeightsPath);
            AssertTrue(nestedComparison.LatestWeightsMatchesCurrentDataset, "training comparison should identify best.pt trained from the current dataset data.yaml");
            AssertTrue(nestedComparison.HasCompletedCurrentDatasetTraining, "training comparison should mark current-dataset training complete when best.pt and results.csv exist");
            AssertTrue(nestedComparison.StatusText.Contains("\uD604\uC7AC \uB370\uC774\uD130\uC14B \uD559\uC2B5 \uC644\uB8CC", StringComparison.Ordinal), "training comparison should show current-dataset training completion");
            AssertTrue(nestedComparison.StatusText.Contains("exp7", StringComparison.Ordinal), "training comparison should show the run folder, not only best.pt");
            AssertEqual($"exp7{Path.DirectorySeparatorChar}best.pt", WpfTrainingWeightsService.FormatWeightsDisplayPath(nestedBest));

            string foreignDataYaml = Path.Combine(root, "foreign", "data.yaml");
            Directory.CreateDirectory(Path.GetDirectoryName(foreignDataYaml));
            File.WriteAllText(foreignDataYaml, "path: .");
            string yolo8SegmentBest = Path.Combine(projectRoot, "runs", "segment", "current-dataset", "weights", "best.pt");
            string yolo8SegmentRunRoot = Path.GetDirectoryName(Path.GetDirectoryName(yolo8SegmentBest));
            string yolo8ForeignSegmentBest = Path.Combine(projectRoot, "runs", "segment", "foreign-dataset", "weights", "best.pt");
            string yolo8ForeignSegmentRunRoot = Path.GetDirectoryName(Path.GetDirectoryName(yolo8ForeignSegmentBest));
            WriteWeight(yolo8SegmentBest, DateTime.UtcNow.AddMinutes(1));
            WriteWeight(yolo8ForeignSegmentBest, DateTime.UtcNow.AddMinutes(2));
            WriteResultsCsv(Path.Combine(yolo8SegmentRunRoot, "results.csv"), map50: 0.93, map5095: 0.69, precision: 0.88, recall: 0.84, boxLoss: 0.037);
            WriteResultsCsv(Path.Combine(yolo8ForeignSegmentRunRoot, "results.csv"), map50: 0.95, map5095: 0.71, precision: 0.90, recall: 0.86, boxLoss: 0.032);
            WriteOptYaml(Path.Combine(yolo8SegmentRunRoot, "args.yaml"), currentDatasetDataYaml);
            WriteOptYaml(Path.Combine(yolo8ForeignSegmentRunRoot, "args.yaml"), foreignDataYaml);
            AssertTrue(
                service.EnumerateBestWeightCandidates(projectRoot).Contains(yolo8SegmentBest, StringComparer.OrdinalIgnoreCase),
                "training weights service should include YOLOv8 runs/segment best.pt");
            WpfTrainingWeightsComparison yolo8SegmentComparison = service.BuildComparison(projectRoot, outputRoot, outputBest);
            AssertEqual(yolo8SegmentBest, yolo8SegmentComparison.LatestWeightsPath);
            AssertTrue(yolo8SegmentComparison.LatestWeightsMatchesCurrentDataset, "YOLOv8 args.yaml should identify best.pt trained from the current dataset data.yaml");
            AssertTrue(yolo8SegmentComparison.HasCompletedCurrentDatasetTraining, "YOLOv8 segmentation comparison should mark current-dataset training complete when best.pt and results.csv exist");
            AssertTrue(yolo8SegmentComparison.StatusText.Contains("current-dataset", StringComparison.Ordinal), "YOLOv8 segmentation comparison should show the selected run folder");
            AssertEqual($"current-dataset{Path.DirectorySeparatorChar}best.pt", WpfTrainingWeightsService.FormatWeightsDisplayPath(yolo8SegmentBest));

            string appSegmentationRoot = Path.Combine(root, "app-segmentation-output");
            CData appSegmentationData = DatasetReadinessTestFixtures.CreatePurposeReadinessData(
                appSegmentationRoot,
                LabelingDatasetPurpose.Segmentation,
                includeBoxes: false,
                includeSegments: true);
            appSegmentationData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            var workflow = new YoloTrainingWorkflowService();
            AssertTrue(workflow.TryPrepareTrainingDataset(appSegmentationData), workflow.LastPreparationFailureMessage);
            string appTrainLabelPath = Path.Combine(appSegmentationRoot, "data", "train", "labels", "purpose-train.txt");
            string appTrainLabel = File.ReadAllLines(appTrainLabelPath).Single();
            AssertEqual("0 0.125 0.125 0.6875 0.125 0.6875 0.6875 0.125 0.6875", appTrainLabel);

            string localYolo8Root = Path.Combine(root, "local-yolov8");
            string appSegmentationBest = Path.Combine(localYolo8Root, "runs", "segment", "app-segmentation-fixture", "weights", "best.pt");
            string appSegmentationRunRoot = Path.GetDirectoryName(Path.GetDirectoryName(appSegmentationBest));
            string currentInspectionBest = Path.Combine(root, "current-inspection", "best.pt");
            WriteWeight(currentInspectionBest, DateTime.UtcNow.AddMinutes(3));
            WriteWeight(appSegmentationBest, DateTime.UtcNow.AddMinutes(4));
            WriteOptYaml(Path.Combine(appSegmentationRunRoot, "args.yaml"), appSegmentationData.DataYamlFilePath);
            WriteRawResultsCsv(
                Path.Combine(appSegmentationRunRoot, "results.csv"),
                "epoch,metrics/precision(M),metrics/recall(M),metrics/mAP50(M),metrics/mAP50-95(M),val/seg_loss",
                "1,0.80,0.76,0.70,0.55,0.040");
            AssertTrue(
                service.TryFindLatestTrainingWeights(localYolo8Root, appSegmentationRoot, out string appLatestWeightsPath),
                "training weights service should find app-generated YOLOv8 segmentation best.pt");
            AssertEqual(appSegmentationBest, appLatestWeightsPath);
            WpfTrainingWeightsComparison appSegmentationComparison = service.BuildComparison(localYolo8Root, appSegmentationRoot, currentInspectionBest);
            AssertEqual(appSegmentationBest, appSegmentationComparison.LatestWeightsPath);
            AssertTrue(appSegmentationComparison.LatestWeightsMatchesCurrentDataset, "app-generated YOLOv8 segmentation args.yaml should match the prepared app data.yaml");
            AssertTrue(appSegmentationComparison.HasCompletedCurrentDatasetTraining, "app-generated YOLOv8 segmentation run should count as completed current-dataset training");
            AssertTrue(appSegmentationComparison.ShouldApplyLatest, "newer app-generated YOLOv8 segmentation best.pt should be offered as an inspection model candidate");
            AssertTrue(appSegmentationComparison.MetricsStatusText.Contains("mAP50-95", StringComparison.Ordinal), "app-generated YOLOv8 segmentation run should expose parsed mask metrics");
            AssertEqual($"app-segmentation-fixture{Path.DirectorySeparatorChar}best.pt", WpfTrainingWeightsService.FormatWeightsDisplayPath(appSegmentationBest));

            string yolo8Best = Path.Combine(root, "yolo8", "weights", "best.pt");
            WriteWeight(yolo8Best, DateTime.UtcNow.AddMinutes(-4));
            WriteRawResultsCsv(
                Path.Combine(root, "yolo8", "results.csv"),
                "\ufeff epoch, train/box_loss, metrics/precision(B), metrics/recall(B), metrics/mAP50(B), metrics/mAP50-95(B), val/box_loss",
                "12,0.201,0.91,0.82,0.73,0.456,0.123");
            AssertTrue(WpfTrainingWeightsService.TryReadTrainingRunMetrics(yolo8Best, out WpfTrainingRunMetrics yolo8Metrics), "training metrics parser should read YOLOv8 detection results.csv aliases");
            AssertMetric(0.91D, yolo8Metrics.Precision, "YOLOv8 precision");
            AssertMetric(0.82D, yolo8Metrics.Recall, "YOLOv8 recall");
            AssertMetric(0.73D, yolo8Metrics.Map50, "YOLOv8 mAP50");
            AssertMetric(0.456D, yolo8Metrics.Map5095, "YOLOv8 mAP50-95");
            AssertMetric(0.123D, yolo8Metrics.BoxLoss, "YOLOv8 box loss");

            string segmentBest = Path.Combine(root, "segment", "weights", "best.pt");
            WriteWeight(segmentBest, DateTime.UtcNow.AddMinutes(-3));
            WriteRawResultsCsv(
                Path.Combine(root, "segment", "results.csv"),
                "epoch,metrics/precision(M),metrics/recall(M),metrics/mAP50(M),metrics/mAP50-95(M),val/box_loss(M)",
                "8,0.67,0.64,0.59,0.321,0.222");
            AssertTrue(WpfTrainingWeightsService.TryReadTrainingRunMetrics(segmentBest, out WpfTrainingRunMetrics segmentMetrics), "training metrics parser should read YOLO segmentation results.csv aliases");
            AssertMetric(0.67D, segmentMetrics.Precision, "segmentation precision");
            AssertMetric(0.64D, segmentMetrics.Recall, "segmentation recall");
            AssertMetric(0.59D, segmentMetrics.Map50, "segmentation mAP50");
            AssertMetric(0.321D, segmentMetrics.Map5095, "segmentation mAP50-95");
            AssertMetric(0.222D, segmentMetrics.BoxLoss, "segmentation box loss");

            string mixedSegmentBest = Path.Combine(root, "segment-mixed", "weights", "best.pt");
            WriteWeight(mixedSegmentBest, DateTime.UtcNow.AddMinutes(-2));
            WriteRawResultsCsv(
                Path.Combine(root, "segment-mixed", "results.csv"),
                "epoch,metrics/precision(B),metrics/recall(B),metrics/mAP50(B),metrics/mAP50-95(B),metrics/precision(M),metrics/recall(M),metrics/mAP50(M),metrics/mAP50-95(M),val/box_loss,val/seg_loss",
                "9,0.11,0.12,0.13,0.14,0.71,0.72,0.73,0.74,0.333,0.055");
            AssertTrue(WpfTrainingWeightsService.TryReadTrainingRunMetrics(mixedSegmentBest, out WpfTrainingRunMetrics mixedSegmentMetrics), "training metrics parser should read YOLO segmentation results.csv when box and mask metrics coexist");
            AssertMetric(0.71D, mixedSegmentMetrics.Precision, "mixed segmentation precision should prefer mask metric");
            AssertMetric(0.72D, mixedSegmentMetrics.Recall, "mixed segmentation recall should prefer mask metric");
            AssertMetric(0.73D, mixedSegmentMetrics.Map50, "mixed segmentation mAP50 should prefer mask metric");
            AssertMetric(0.74D, mixedSegmentMetrics.Map5095, "mixed segmentation mAP50-95 should prefer mask metric");
            AssertMetric(0.055D, mixedSegmentMetrics.BoxLoss, "mixed segmentation loss should prefer segmentation loss");

            AssertTrue(WpfTrainingWeightsService.IsCompletedTrainingState(" completed "), "training state completion check should be trim/case insensitive");
            AssertTrue(!WpfTrainingWeightsService.IsCompletedTrainingState("running"), "non-terminal training states should not be treated as completed");
        }
        finally
        {
            DeleteTempRoot(root);
        }

        static void WriteWeight(string path, DateTime timestampUtc)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "weights");
            File.SetLastWriteTimeUtc(path, timestampUtc);
        }

        static void WriteResultsCsv(string path, double map50, double map5095, double precision, double recall, double boxLoss)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                string.Join(
                    Environment.NewLine,
                    "epoch,metrics/precision,metrics/recall,metrics/mAP_0.5,metrics/mAP_0.5:0.95,val/box_loss",
                    FormattableString.Invariant($"0,{precision},{recall},{map50},{map5095},{boxLoss}"),
                    FormattableString.Invariant($"1,{precision},{recall},{map50},{map5095},{boxLoss}")));
        }

        static void WriteOptYaml(string path, string dataYamlPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, $"data: {dataYamlPath}{Environment.NewLine}");
        }

        static void WriteRawResultsCsv(string path, string header, string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, string.Join(Environment.NewLine, header, value));
        }

        static void AssertMetric(double expected, double? actual, string metricName)
        {
            if (!actual.HasValue || Math.Abs(expected - actual.Value) > 0.0001D)
            {
                throw new InvalidOperationException($"Expected {metricName} {expected}, got {actual}.");
            }
        }
    }
}
