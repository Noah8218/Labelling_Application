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

internal static class WpfModelComparisonTests
{
    internal static void TestWpfModelBenchmarkWindow()
    {
        string root = CreateTempRoot();
        try
        {
            string comparisonRoot = Path.Combine(root, "artifacts", "yolo-model-comparison");
            string preferredSummaryPath = string.Empty;
            string previewImageDirectory = Path.Combine(root, "datasets", "detect", "test", "images");
            Directory.CreateDirectory(previewImageDirectory);
            using (var previewImage = new Bitmap(16, 12))
            {
                previewImage.Save(Path.Combine(previewImageDirectory, "missed-ng.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                previewImage.Save(Path.Combine(previewImageDirectory, "false-ng.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            for (int index = 0; index < 3; index++)
            {
                string runRoot = Path.Combine(comparisonRoot, "run-" + index.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(runRoot);
                string summaryPath = Path.Combine(runRoot, "comparison-summary.json");
                string task = index == 2 ? "segment" : "detect";
                string baselineEngine = index == 0 ? "YOLOv5" : index == 1 ? "DetectNet A" : "SegmentNet A";
                string candidateEngine = index == 0 ? "YOLOv8" : index == 1 ? "DetectNet B" : "SegmentNet B";
                object evidence = index == 2
                    ? new
                    {
                        split = "test",
                        imageCount = 12,
                        comparisonLabelCount = 12
                    }
                    : new
                    {
                        split = "test",
                        fingerprintAlgorithm = "sha256-image-label-pairs-v1",
                        fingerprintSha256 = new string((char)('a' + index), 64),
                        imageCount = 12,
                        comparisonLabelCount = 12
                    };
                object baselineGroundTruthReview = index == 0
                    ? new
                    {
                        schemaVersion = 2,
                        schema = "detection-ground-truth-review-v2",
                        geometryCoordinateSystem = "normalized-xyxy-v1",
                        confidence = 0.25,
                        predictionNmsIouThreshold = 0.45,
                        iouThreshold = 0.5,
                        imageCount = 12,
                        truePositiveCount = 7,
                        falsePositiveCount = 1,
                        falseNegativeCount = 5,
                        perClass = new[]
                        {
                            new { classId = 0, className = "OK", groundTruthCount = 7, predictionCount = 8, truePositiveCount = 7, falsePositiveCount = 1, falseNegativeCount = 0 },
                            new { classId = 1, className = "NG", groundTruthCount = 5, predictionCount = 0, truePositiveCount = 0, falsePositiveCount = 0, falseNegativeCount = 5 }
                        },
                        thresholdSweep = new object[]
                        {
                            new { confidence = (object)0.25, groundTruthCount = (object)12, predictionCount = (object)8, truePositiveCount = (object)7, falsePositiveCount = (object)1, falseNegativeCount = (object)5, precision = (object)(7D / 8D), recall = (object)(7D / 12D), f1 = (object)(14D / 20D) },
                            new { confidence = (object)0.10, groundTruthCount = (object)12, predictionCount = (object)10, truePositiveCount = (object)8, falsePositiveCount = (object)2, falseNegativeCount = (object)4, precision = (object)0.8D, recall = (object)(8D / 12D), f1 = (object)(16D / 22D) },
                            new { confidence = (object)"invalid", groundTruthCount = (object)12, predictionCount = (object)0, truePositiveCount = (object)0, falsePositiveCount = (object)0, falseNegativeCount = (object)12, precision = (object)0D, recall = (object)0D, f1 = (object)0D }
                        },
                        examples = new[]
                        {
                            new
                            {
                                imagePath = Path.Combine(root, "datasets", task, "test", "images", "missed-ng.jpg"),
                                imageName = "missed-ng.jpg",
                                errorType = "false-negative",
                                classId = 1,
                                className = "NG",
                                confidence = (double?)null,
                                bestIou = (double?)0.1,
                                predictionBox = (object)null,
                                groundTruthBox = new { classId = 1, xMin = 0.2D, yMin = 0.25D, xMax = 0.6D, yMax = 0.75D }
                            }
                        }
                    }
                    : null;
                object candidateGroundTruthReview = index == 0
                    ? new
                    {
                        schemaVersion = 2,
                        schema = "detection-ground-truth-review-v2",
                        geometryCoordinateSystem = "normalized-xyxy-v1",
                        confidence = 0.25,
                        predictionNmsIouThreshold = 0.45,
                        iouThreshold = 0.5,
                        imageCount = 12,
                        truePositiveCount = 11,
                        falsePositiveCount = 1,
                        falseNegativeCount = 1,
                        perClass = new[]
                        {
                            new { classId = 0, className = "OK", groundTruthCount = 7, predictionCount = 7, truePositiveCount = 7, falsePositiveCount = 0, falseNegativeCount = 0 },
                            new { classId = 1, className = "NG", groundTruthCount = 5, predictionCount = 5, truePositiveCount = 4, falsePositiveCount = 1, falseNegativeCount = 1 }
                        },
                        thresholdSweep = new object[]
                        {
                            new { confidence = (object)0.25, groundTruthCount = (object)12, predictionCount = (object)12, truePositiveCount = (object)11, falsePositiveCount = (object)1, falseNegativeCount = (object)1, precision = (object)(11D / 12D), recall = (object)(11D / 12D), f1 = (object)(11D / 12D) },
                            new { confidence = (object)0.10, groundTruthCount = (object)12, predictionCount = (object)14, truePositiveCount = (object)12, falsePositiveCount = (object)2, falseNegativeCount = (object)0, precision = (object)(12D / 14D), recall = (object)1D, f1 = (object)(12D / 13D) },
                            new { confidence = (object)"invalid", groundTruthCount = (object)12, predictionCount = (object)0, truePositiveCount = (object)0, falsePositiveCount = (object)0, falseNegativeCount = (object)12, precision = (object)0D, recall = (object)0D, f1 = (object)0D }
                        },
                        examples = new
                        {
                            imagePath = Path.Combine(root, "datasets", task, "test", "images", "false-ng.jpg"),
                            imageName = "false-ng.jpg",
                            errorType = "false-positive",
                            classId = 1,
                            className = "NG",
                            confidence = (double?)0.73,
                            bestIou = (double?)0.2,
                            predictionBox = new { classId = 1, xMin = 0.3D, yMin = 0.2D, xMax = 0.7D, yMax = 0.65D, confidence = 0.73D },
                            groundTruthBox = new { classId = 1, xMin = 0.35D, yMin = 0.22D, xMax = 0.72D, yMax = 0.68D }
                        }
                    }
                    : null;
                object summary = new
                {
                    createdAt = DateTimeOffset.UtcNow.AddMinutes(-index).ToString("O", CultureInfo.InvariantCulture),
                    dataYaml = Path.Combine(root, "datasets", task, "data.yaml"),
                    task = "test",
                    modelTask = task,
                    comparisonKind = "engine-benchmark",
                    imageSize = 320,
                    batchSize = 1,
                    benchmarkRepeatCount = 5,
                    uiConfidence = 0.25,
                    evidence,
                    baseline = new
                    {
                        engine = baselineEngine,
                        weights = Path.Combine(root, "runs", "baseline-" + index, "weights", "best.pt"),
                        weightsSha256 = new string('d', 64),
                        metrics = new { precision = 0.8, recall = 0.7, map50 = 0.75, map5095 = 0.6 },
                        classMetrics = new[]
                        {
                            new { classId = 0, className = "OK", imageCount = 7, instanceCount = 7, precision = 0.9, recall = 1.0, map50 = 0.95, map5095 = 0.8 },
                            new { classId = 1, className = "NG", imageCount = 5, instanceCount = 5, precision = 1.0, recall = 0.0, map50 = 0.0, map5095 = 0.0 }
                        },
                        groundTruthReview = baselineGroundTruthReview,
                        benchmark = new
                        {
                            taktMs = 15.0 + index,
                            taktMinMs = 14.0 + index,
                            taktMaxMs = 16.0 + index,
                            repeatCount = 5,
                            source = "native-validation-speed-median"
                        },
                        confidence = new { uiCandidateCount = 12 }
                    },
                    candidate = new
                    {
                        engine = candidateEngine,
                        weights = Path.Combine(root, "runs", "candidate-" + index, "weights", "best.pt"),
                        weightsSha256 = new string('e', 64),
                        metrics = new { precision = 0.85, recall = 0.8, map50 = 0.82, map5095 = 0.7 },
                        classMetrics = new[]
                        {
                            new { classId = 0, className = "OK", imageCount = 7, instanceCount = 7, precision = 1.0, recall = 1.0, map50 = 0.99, map5095 = 0.9 },
                            new { classId = 1, className = "NG", imageCount = 5, instanceCount = 5, precision = 0.8, recall = 0.8, map50 = 0.75, map5095 = 0.5 }
                        },
                        groundTruthReview = candidateGroundTruthReview,
                        benchmark = new
                        {
                            taktMs = 12.0 + index,
                            taktMinMs = 11.0 + index,
                            taktMaxMs = 13.0 + index,
                            repeatCount = 5,
                            source = "native-validation-speed-median"
                        },
                        confidence = new { uiCandidateCount = 13 }
                    },
                    promotion = new { recommendation = "benchmark" }
                };
                File.WriteAllText(summaryPath, JsonConvert.SerializeObject(summary, Formatting.Indented));
                if (index == 0)
                {
                    preferredSummaryPath = summaryPath;
                }
            }

            string anomalyRoot = Path.Combine(root, "artifacts", "yolo-classification-evaluation", "run-0");
            Directory.CreateDirectory(anomalyRoot);
            string anomalySummaryPath = Path.Combine(anomalyRoot, "classification-evaluation-summary.json");
            object anomalySummary = new
            {
                generatedUtc = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O", CultureInfo.InvariantCulture),
                weightsPath = Path.Combine(root, "runs", "yolov8n-cls-anomaly", "weights", "best.pt"),
                weightsSha256 = new string('f', 64),
                datasetRoot = Path.Combine(root, "datasets", "anomaly"),
                split = "test",
                evidence = new
                {
                    fingerprintAlgorithm = "sha256-class-image-pairs-v1",
                    fingerprintSha256 = new string('c', 64)
                },
                thresholds = new { minimumConfidence = 0.8 },
                metrics = new
                {
                    totalImageCount = 15,
                    correctImageCount = 14,
                    accuracy = 14D / 15D,
                    normalAccuracy = 1D,
                    abnormalAccuracy = 0.9D
                },
                promotion = new { recommendation = "hold" }
            };
            File.WriteAllText(anomalySummaryPath, JsonConvert.SerializeObject(anomalySummary, Formatting.Indented));

            var service = new WpfModelBenchmarkCatalogService();
            IReadOnlyList<WpfModelBenchmarkRun> runs = service.Load(root);
            AssertEqual(7, runs.Count);
            AssertTrue(runs.Any(run => run.TaskKey == "object-detection"), "benchmark catalog should normalize object detection runs");
            AssertTrue(runs.Any(run => run.TaskKey == "segmentation"), "benchmark catalog should normalize segmentation runs");
            AssertTrue(runs.Any(run => run.TaskKey == "anomaly-classification"), "benchmark catalog should normalize anomaly classification runs");
            WpfModelBenchmarkRun anomalyRun = runs.Single(run => run.TaskKey == "anomaly-classification");
            AssertTrue(anomalyRun.Metrics.Any(metric => metric.Key == "accuracy"), "anomaly evaluation should expose task-specific accuracy without a YOLO-only table contract");
            AssertTrue(!anomalyRun.TaktMs.HasValue, "anomaly evaluation without timing evidence should stay explicitly unmeasured");
            AssertEqual(new string('f', 64), anomalyRun.WeightsSha256);
            AssertEqual(new string('c', 64), anomalyRun.EvidenceFingerprintSha256);
            AssertTrue(runs.Where(run => run.TaskKey == "segmentation").All(run => string.IsNullOrEmpty(run.EvidenceFingerprintSha256)), "historical reports without evidence fingerprints should remain loadable");

            string projectOutputRoot = Path.Combine(root, "current-anomaly-output");
            string yoloEvaluationRoot = Path.Combine(projectOutputRoot, "classification-evaluation-20260803-010000");
            string patchCoreEvaluationRoot = Path.Combine(projectOutputRoot, "classification-evaluation-20260803-020000");
            Directory.CreateDirectory(yoloEvaluationRoot);
            Directory.CreateDirectory(patchCoreEvaluationRoot);
            string yoloEvaluationPath = Path.Combine(yoloEvaluationRoot, "classification-evaluation-summary.json");
            string patchCoreEvaluationPath = Path.Combine(patchCoreEvaluationRoot, "classification-evaluation-summary.json");
            object BuildAnomalyComparisonSummary(string model, string weights, double takt)
            {
                bool patchCore = string.Equals(model, "patchcore", StringComparison.OrdinalIgnoreCase);
                object[] samples =
                {
                    new
                    {
                        imagePath = Path.Combine(previewImageDirectory, "missed-ng.jpg"),
                        expectedClassName = "normal",
                        predictedClassName = patchCore ? "normal" : "abnormal",
                        confidence = patchCore ? 0.55D : 0.70D,
                        anomalyScore = patchCore ? (double?)0.20D : null,
                        anomalyThreshold = patchCore ? (double?)0.40D : null,
                        heatmapPath = patchCore ? Path.Combine(previewImageDirectory, "missed-ng.jpg") : string.Empty,
                        localizationCount = 0,
                        correct = patchCore
                    },
                    new
                    {
                        imagePath = Path.Combine(previewImageDirectory, "false-ng.jpg"),
                        expectedClassName = "abnormal",
                        predictedClassName = "abnormal",
                        confidence = patchCore ? 0.69D : 0.65D,
                        anomalyScore = patchCore ? (double?)0.70D : null,
                        anomalyThreshold = patchCore ? (double?)0.40D : null,
                        heatmapPath = patchCore ? Path.Combine(previewImageDirectory, "false-ng.jpg") : string.Empty,
                        localizationCount = patchCore ? 1 : 0,
                        correct = patchCore
                    }
                };
                double accuracy = patchCore ? 1D : 0D;
                return new
                {
                generatedUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                modelName = model,
                weightsPath = weights,
                weightsSha256 = model == "patchcore" ? new string('1', 64) : new string('2', 64),
                datasetRoot = Path.Combine(projectOutputRoot, "classification-evaluation-input", "fixed"),
                split = "test",
                averageEvaluationMsPerImage = takt,
                runtime = new
                {
                    imageSize = 224,
                    batchSize = 1,
                    timingSource = "persistent-adapter-wall-clock",
                    timingRepeatCount = 1,
                    device = "cpu",
                    hardware = new { machineName = "ANOMALY-BENCH" }
                },
                evidence = new { fingerprintSha256 = new string('9', 64) },
                thresholds = new { minimumConfidence = model == "patchcore" ? 0D : 0.8D },
                metrics = new
                {
                    totalImageCount = 2,
                    normalImageCount = 1,
                    abnormalImageCount = 1,
                    correctImageCount = patchCore ? 2 : 0,
                    normalCorrectCount = patchCore ? 1 : 0,
                    abnormalCorrectCount = patchCore ? 1 : 0,
                    accuracy,
                    balancedAccuracy = accuracy,
                    normalAccuracy = patchCore ? 1D : 0D,
                    abnormalAccuracy = patchCore ? 1D : 0D,
                    falsePositiveCount = patchCore ? 0 : 1,
                    falseNegativeCount = patchCore ? 0 : 1,
                    localizationEvidenceCount = patchCore ? 1 : 0,
                    heatmapEvidenceCount = patchCore ? 2 : 0
                },
                localization = new { groundTruthStatus = "not-evaluated" },
                promotion = new { recommendation = "hold" },
                samples
                };
            }
            File.WriteAllText(yoloEvaluationPath, JsonConvert.SerializeObject(
                BuildAnomalyComparisonSummary("yolov8", Path.Combine(root, "weights", "yolov8-anomaly.pt"), 12D),
                Formatting.Indented));
            File.WriteAllText(patchCoreEvaluationPath, JsonConvert.SerializeObject(
                BuildAnomalyComparisonSummary("patchcore", Path.Combine(root, "weights", "patchcore-anomaly.pt"), 18D),
                Formatting.Indented));

            IReadOnlyList<WpfModelBenchmarkRun> projectRuns = service.Load(root, patchCoreEvaluationPath);
            WpfModelBenchmarkRun projectPatchCore = projectRuns.Single(run => run.SourcePath == patchCoreEvaluationPath);
            WpfModelBenchmarkRun projectYolo = projectRuns.Single(run => run.SourcePath == yoloEvaluationPath);
            AssertEqual("PatchCore", projectPatchCore.RuntimeName);
            AssertEqual(224, projectPatchCore.ImageSize);
            AssertEqual(18D, projectPatchCore.TaktMs.Value);
            AssertTrue(projectPatchCore.Metrics.Any(metric => metric.Key == "balancedAccuracy"), "PatchCore summary should expose balanced accuracy in the shared benchmark table");
            AssertTrue(projectPatchCore.Metrics.Any(metric => metric.Key == "localizationEvidenceCount" && metric.Value == 1D), "PatchCore summary should retain review-only location evidence counts");
            AssertEqual(2, projectPatchCore.GroundTruthReview.Examples.Count);
            AssertTrue(projectPatchCore.GroundTruthReview.Examples.Any(example => example.EvidencePath.EndsWith("false-ng.jpg", StringComparison.OrdinalIgnoreCase)
                    && example.DetailText.Contains("\uC704\uCE58 1", StringComparison.Ordinal)),
                "PatchCore image outcomes should retain heatmap and location review evidence");
            AssertTrue(projectYolo.GroundTruthReview.Examples.First().ErrorType == "false-positive",
                "anomaly image outcomes should prioritize errors before correct samples");
            var anomalyComparisonViewModel = new WpfModelBenchmarkViewModel(service, root, patchCoreEvaluationPath);
            AssertEqual(2, anomalyComparisonViewModel.SelectedRuns.Count);
            AssertTrue(anomalyComparisonViewModel.SelectedRuns.Any(item => item.RuntimeName == "PatchCore")
                && anomalyComparisonViewModel.SelectedRuns.Any(item => item.RuntimeName == "Ultralytics YOLOv8"),
                "opening a project anomaly summary should preselect all current-project runs with the same evidence fingerprint");
            AssertTrue(anomalyComparisonViewModel.ComparisonNoticeText.Contains("\uC815\uD655\uB3C4/Takt \uBE44\uAD50 \uAC00\uB2A5", StringComparison.Ordinal),
                "same-split same-runtime-condition anomaly reports should state that accuracy and timing are comparable");
            AssertEqual(4, anomalyComparisonViewModel.GroundTruthExamples.Count);
            AssertTrue(anomalyComparisonViewModel.GroundTruthExamples.First().ErrorTypeText.Contains("FP", StringComparison.Ordinal)
                    && anomalyComparisonViewModel.GroundTruthExamples.Any(example => example.DetailText.Contains("\uC774\uC0C1 \uC810\uC218", StringComparison.Ordinal)),
                "anomaly comparison should expose error-first image decisions and PatchCore score evidence");
            AssertTrue(anomalyComparisonViewModel.GroundTruthExamples.Any(example => example.ErrorTypeText == "\uC784\uACC4\uAC12 \uBBF8\uB2EC"),
                "class-matching predictions below the decision threshold should not be mislabeled as a class mismatch");
            AssertEqual(2, anomalyComparisonViewModel.DashboardOutcomeRows.Count);
            AssertTrue(anomalyComparisonViewModel.DashboardOutcomeRows.All(row => row.TruePositiveText.StartsWith("\uC815\uB2F5 ", StringComparison.Ordinal)),
                "anomaly dashboard outcomes should use image-decision labels instead of detection TP wording");

            WpfModelBenchmarkRun preferredBaseline = runs.Single(run => run.SourcePath == preferredSummaryPath && run.SourceRole == "baseline");
            WpfModelBenchmarkRun otherDetectionBaseline = runs.Single(run => run.TaskKey == "object-detection" && run.SourceRole == "baseline" && run.SourcePath != preferredSummaryPath);
            AssertTrue(!string.Equals(preferredBaseline.QualityComparisonKey, otherDetectionBaseline.QualityComparisonKey, StringComparison.OrdinalIgnoreCase), "different evidence fingerprints should produce different quality-comparison identities even when the legacy path/count fields match");
            AssertEqual(2, preferredBaseline.ClassMetrics.Count);
            AssertEqual(5, preferredBaseline.GroundTruthReview.FalseNegativeCount);
            AssertEqual(0.45D, preferredBaseline.GroundTruthReview.PredictionNmsIouThreshold.Value);
            AssertEqual("missed-ng.jpg", preferredBaseline.GroundTruthReview.Examples.Single().ImageName);
            AssertEqual(2, preferredBaseline.GroundTruthReview.SchemaVersion);
            AssertEqual("normalized-xyxy-v1", preferredBaseline.GroundTruthReview.GeometryCoordinateSystem);
            AssertEqual(2, preferredBaseline.GroundTruthReview.ThresholdSweep.Count);
            AssertTrue(preferredBaseline.GroundTruthReview.Examples.Single().GroundTruthBox != null,
                "v2 review should preserve the normalized ground-truth error box");
            AssertTrue(preferredBaseline.GroundTruthReview.Examples.Single().PredictionBox == null,
                "missing predictions should remain absent instead of being converted to a synthetic box");
            WpfModelBenchmarkRun preferredCandidate = runs.Single(run => run.SourcePath == preferredSummaryPath && run.SourceRole == "candidate");
            AssertTrue(preferredCandidate.GroundTruthReview.Examples.Single().PredictionBox != null,
                "v2 review should preserve the normalized prediction error box");

            var viewModel = new WpfModelBenchmarkViewModel(service, root, preferredSummaryPath);
            AssertEqual(2, viewModel.SelectedRuns.Count);
            AssertTrue(viewModel.CatalogRuns.Count(item => item.IsBaseline) == 1, "preferred pair should select exactly one baseline");
            AssertTrue(viewModel.SelectedRuns.All(item => item.TaskText == "\uAC1D\uCCB4 \uD0D0\uC9C0"), "preferred detection report should preselect its two runs");
            AssertTrue(viewModel.MetricRows.Any(row => row.DisplayName == "mAP50-95"), "metric matrix should be driven by selected run metrics");
            AssertTrue(viewModel.CatalogTaskSummaryText.Contains("\uC138\uADF8\uBA58\uD14C\uC774\uC158", StringComparison.Ordinal)
                && viewModel.CatalogTaskSummaryText.Contains("\uC774\uC0C1 \uBD84\uB958", StringComparison.Ordinal),
                "catalog summary should make heterogeneous task coverage visible");
            AssertTrue(viewModel.ComparisonNoticeText.Contains("Takt", StringComparison.Ordinal), "comparison notice should disclose timing comparability");
            AssertTrue(viewModel.ComparisonNoticeText.Contains("\uB370\uC774\uD130 \uC9C0\uBB38 \uC77C\uCE58", StringComparison.Ordinal), "comparison notice should confirm matching evidence fingerprints");
            AssertTrue(viewModel.SelectedRuns.All(run => run.EvidenceFingerprintText.Length == 12), "execution conditions should expose compact evidence fingerprints");
            AssertTrue(viewModel.SelectedRuns.All(run => run.WeightsSha256Text.Length == 12), "execution conditions should expose compact weights fingerprints");
            AssertEqual(4, viewModel.ClassMetricRows.Count);
            AssertEqual(2, viewModel.GroundTruthExamples.Count);
            AssertEqual(4, viewModel.ThresholdReviewRows.Count);
            AssertTrue(viewModel.HasThresholdReviewRows, "v2 detection reports should expose stored threshold-review rows");
            AssertTrue(viewModel.ThresholdReviewStatusText.Contains("\uCD94\uB860", StringComparison.Ordinal),
                "threshold review should explicitly disclose that it does not rerun inference");
            AssertEqual("missed-ng.jpg", viewModel.SelectedGroundTruthExample.ImageName);
            AssertTrue(viewModel.HasSelectedGroundTruthPreview, "class detail should load the selected ground-truth error image lazily from the recorded path");
            AssertTrue(viewModel.HasSelectedGroundTruthPreviewOverlay, "v2 ground-truth examples should expose an overlay state");
            AssertTrue(viewModel.HasSelectedGroundTruthPreviewGroundTruthBox && !viewModel.HasSelectedGroundTruthPreviewPredictionBox,
                "a missed detection should show only its saved ground-truth box");
            var missedPreview = viewModel.SelectedGroundTruthExample.PreviewSource as System.Windows.Media.DrawingImage;
            var missedDrawing = missedPreview?.Drawing as System.Windows.Media.DrawingGroup;
            AssertTrue(missedDrawing?.Children.Count == 2,
                "a missed detection preview should compose the source image with exactly one saved ground-truth outline");
            var missedOutline = missedDrawing?.Children.OfType<System.Windows.Media.GeometryDrawing>().SingleOrDefault();
            AssertTrue(missedOutline?.Pen?.Brush is System.Windows.Media.SolidColorBrush missedBrush
                && missedBrush.Color == System.Windows.Media.Color.FromRgb(57, 217, 138)
                && missedOutline.Pen.DashStyle?.Dashes.Count == 0,
                "a missed detection preview should render the saved ground-truth outline as a solid green box");
            viewModel.SelectedGroundTruthExample = viewModel.GroundTruthExamples.Last();
            AssertEqual("false-ng.jpg", viewModel.GroundTruthPreviewTitleText);
            AssertTrue(viewModel.HasSelectedGroundTruthPreview, "selecting another ground-truth error should load its own recorded image");
            AssertTrue(viewModel.HasSelectedGroundTruthPreviewGroundTruthBox && viewModel.HasSelectedGroundTruthPreviewPredictionBox,
                "a false-positive example should preserve both saved answer and prediction boxes");
            var falsePositivePreview = viewModel.SelectedGroundTruthExample.PreviewSource as System.Windows.Media.DrawingImage;
            var falsePositiveDrawing = falsePositivePreview?.Drawing as System.Windows.Media.DrawingGroup;
            AssertTrue(falsePositiveDrawing?.Children.Count == 3,
                "a false-positive preview should compose the source image with both saved box outlines without re-running inference");
            var falsePositiveOutlines = falsePositiveDrawing?.Children.OfType<System.Windows.Media.GeometryDrawing>().ToList();
            AssertTrue(falsePositiveOutlines?.Count == 2
                && falsePositiveOutlines[0].Pen?.Brush is System.Windows.Media.SolidColorBrush falsePositiveGroundTruthBrush
                && falsePositiveGroundTruthBrush.Color == System.Windows.Media.Color.FromRgb(57, 217, 138)
                && falsePositiveOutlines[0].Pen.DashStyle?.Dashes.Count == 0
                && falsePositiveOutlines[1].Pen?.Brush is System.Windows.Media.SolidColorBrush falsePositivePredictionBrush
                && falsePositivePredictionBrush.Color == System.Windows.Media.Color.FromRgb(50, 184, 255)
                && falsePositiveOutlines[1].Pen.DashStyle?.Dashes.Count > 0,
                "a false-positive preview should preserve solid green ground-truth and dashed blue prediction outline semantics");
            AssertTrue(viewModel.ClassMetricRows.Any(row => row.ClassName == "NG" && row.GroundTruthReviewText.Contains("FN 5", StringComparison.Ordinal)), "class detail should expose the baseline NG miss count");
            AssertTrue(viewModel.GroundTruthReviewNoticeText.Contains("IoU", StringComparison.Ordinal), "class detail should disclose the ground-truth matching threshold");
            AssertTrue(viewModel.GroundTruthReviewNoticeText.Contains("NMS IoU 45%", StringComparison.Ordinal), "class detail should disclose the UI prediction NMS threshold");
            AssertEqual(2, viewModel.DashboardQualityTaktPoints.Count);
            AssertTrue(viewModel.HasDashboardQualityTaktPoints, "dashboard should plot the same-evidence and same-timing benchmark pair");
            AssertTrue(viewModel.DashboardQualityTaktPoints.Any(point => point.IsBaseline), "dashboard quality/takt plot should retain the selected baseline");
            AssertEqual(2, viewModel.DashboardOutcomeRows.Count);
            AssertTrue(viewModel.HasDashboardOutcomeRows, "dashboard should expose the selected reports' TP/FP/FN rows");
            AssertTrue(viewModel.DashboardQualityText.Contains("mAP50-95", StringComparison.Ordinal), "dashboard should name the comparable primary metric");
            AssertTrue(viewModel.DashboardTaktText.Contains("->", StringComparison.Ordinal), "dashboard should disclose the compared Takt values");

            WpfModelBenchmarkRunItemViewModel preferredCandidateItem = viewModel.CatalogRuns.Single(item => item.IsSelected && !item.IsBaseline);
            WpfModelBenchmarkRunItemViewModel historicalSegmentationItem = viewModel.CatalogRuns.First(item => item.Run.TaskKey == "segmentation");
            viewModel.ClearSelectionCommand.Execute(null);
            historicalSegmentationItem.IsSelected = true;
            AssertTrue(!viewModel.HasThresholdReviewRows, "historical non-detection reports should not fabricate threshold-review rows");
            AssertTrue(viewModel.ThresholdReviewStatusText.Contains("v2", StringComparison.Ordinal),
                "historical reports should direct the operator to rerun an object-detection comparison");
            AssertTrue(viewModel.GroundTruthReviewNoticeText.Contains("\uD3F4\uB9AC\uACE4/\uB9C8\uC2A4\uD06C", StringComparison.Ordinal)
                && viewModel.DashboardOutcomeStatusText.Contains("\uD3F4\uB9AC\uACE4/\uB9C8\uC2A4\uD06C", StringComparison.Ordinal),
                "segmentation reports without box review evidence should explain their polygon/mask evidence path");
            historicalSegmentationItem.IsSelected = false;
            WpfModelBenchmarkRunItemViewModel restoredV2BaselineItem = viewModel.CatalogRuns.Single(item => item.Run.Id == preferredBaseline.Id);
            restoredV2BaselineItem.IsSelected = true;
            preferredCandidateItem.IsSelected = true;
            viewModel.SetBaselineCommand.Execute(restoredV2BaselineItem);
            AssertEqual(4, viewModel.ThresholdReviewRows.Count);

            WpfModelBenchmarkRunItemViewModel otherDetectionItem = viewModel.CatalogRuns.Single(item => item.Run.Id == otherDetectionBaseline.Id);
            preferredCandidateItem.IsSelected = false;
            otherDetectionItem.IsSelected = true;
            AssertTrue(viewModel.ComparisonNoticeText.Contains("\uD3C9\uAC00 \uB370\uC774\uD130 \uC9C0\uBB38", StringComparison.Ordinal), "different evidence fingerprints should block quality deltas in the operator notice");
            AssertEqual(0, viewModel.DashboardQualityTaktPoints.Count);
            AssertTrue(!viewModel.HasDashboardQualityTaktPoints, "dashboard should not chart runs with different evaluation fingerprints as a quality/takt comparison");
            otherDetectionItem.IsSelected = false;
            preferredCandidateItem.IsSelected = true;
            AssertEqual(2, viewModel.DashboardQualityTaktPoints.Count);

            List<WpfModelBenchmarkRunItemViewModel> remaining = viewModel.CatalogRuns.Where(item => !item.IsSelected).ToList();
            foreach (WpfModelBenchmarkRunItemViewModel item in remaining.Take(4))
            {
                item.IsSelected = true;
            }

            AssertEqual(WpfModelBenchmarkViewModel.MaximumSelectedRunCount, viewModel.SelectedRuns.Count);
            WpfModelBenchmarkRunItemViewModel rejectedSeventh = remaining[4];
            rejectedSeventh.IsSelected = true;
            AssertTrue(!rejectedSeventh.IsSelected, "benchmark selection should reject a seventh active run");
            AssertTrue(viewModel.StatusText.Contains("6", StringComparison.Ordinal), "benchmark selection limit should be visible in status text");

            WpfModelBenchmarkRunItemViewModel newBaseline = viewModel.CatalogRuns.Last(item => item.IsSelected);
            viewModel.SetBaselineCommand.Execute(newBaseline);
            AssertTrue(newBaseline.IsBaseline, "baseline command should move the single comparison baseline");
            AssertTrue(viewModel.CatalogRuns.Count(item => item.IsBaseline) == 1, "baseline command should keep exactly one baseline");

            viewModel.ClearSelectionCommand.Execute(null);
            WpfModelBenchmarkRunItemViewModel restoredBaselineItem = viewModel.CatalogRuns.Single(item => item.Run.Id == preferredBaseline.Id);
            restoredBaselineItem.IsSelected = true;
            preferredCandidateItem.IsSelected = true;
            viewModel.SetBaselineCommand.Execute(restoredBaselineItem);
            AssertEqual(2, viewModel.DashboardQualityTaktPoints.Count);

            if (System.Windows.Application.Current == null)
            {
                _ = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                };
            }

            var window = new WpfModelBenchmarkWindow(viewModel);
            try
            {
                window.Show();
                window.UpdateLayout();
                PumpWpfDispatcher(TimeSpan.FromMilliseconds(100));
                AssertEqual("\uBAA8\uB378 \uC131\uB2A5 \uBE44\uAD50", window.Title);
                AssertTrue(window.GetType().BaseType?.FullName == "Wpf.Ui.Controls.FluentWindow", "benchmark window should use the existing WPF-UI window library");
                AssertTrue(window.FindName("ModelBenchmarkCatalogList") != null, "benchmark window should expose the run catalog");
                AssertTrue(window.FindName("ModelBenchmarkSummaryGrid") != null, "benchmark window should expose a selected-run summary");
                var qualityTaktCanvas = window.FindName("ModelBenchmarkQualityTaktCanvas") as System.Windows.Controls.Canvas;
                AssertTrue(qualityTaktCanvas != null, "benchmark window should expose the quality/takt dashboard canvas");
                AssertTrue(qualityTaktCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().Count() >= 2,
                    "benchmark dashboard should render a marker for each comparable selected run");
                AssertTrue(window.FindName("ModelBenchmarkQualityTaktLegend") is System.Windows.Controls.ItemsControl qualityTaktLegend
                    && qualityTaktLegend.Items.Count == 2,
                    "benchmark dashboard should render a legend for comparable runs");
                AssertTrue(window.FindName("ModelBenchmarkOutcomeRows") is System.Windows.Controls.ItemsControl outcomeRows
                    && outcomeRows.Items.Count == 2,
                    "benchmark dashboard should expose TP/FP/FN rows");
                AssertTrue(window.FindName("ModelBenchmarkMetricRows") != null, "benchmark window should expose a dynamic metric matrix");
                AssertTrue(window.FindName("ModelBenchmarkClassMetricGrid") != null, "benchmark window should expose per-class validation metrics");
                AssertTrue(window.FindName("ModelBenchmarkGroundTruthExampleGrid") != null, "benchmark window should expose ground-truth error examples");
                AssertTrue(window.FindName("ModelBenchmarkGroundTruthPreview") != null, "benchmark window should expose the selected ground-truth image preview");
                AssertTrue(window.FindName("ModelBenchmarkGroundTruthPreviewImage") is System.Windows.Controls.Image, "benchmark window should render the selected ground-truth image preview");
                AssertTrue(window.FindName("ModelBenchmarkGroundTruthPreviewOverlayLegend") is System.Windows.FrameworkElement overlayLegend
                    && overlayLegend.Visibility == System.Windows.Visibility.Visible,
                    "benchmark preview should expose the saved ground-truth/prediction overlay legend when a v2 example is selected");
                AssertTrue(window.FindName("ModelBenchmarkConditionGrid") != null, "benchmark window should expose evaluation and timing conditions");
                AssertTrue(window.FindName("ModelBenchmarkThresholdReviewStatusText") != null, "benchmark window should disclose stored threshold-review state");
                AssertTrue(window.FindName("ModelBenchmarkThresholdReviewGrid") is System.Windows.Controls.DataGrid thresholdReviewGrid
                    && thresholdReviewGrid.Items.Count == 4,
                    "benchmark window should expose v2 threshold TP/FP/FN rows without rerunning inference");
                AssertTrue(window.FindName("ModelBenchmarkTabs") is System.Windows.Controls.TabControl benchmarkTabs
                    && benchmarkTabs.Items.Count == 5,
                    "benchmark window should retain the existing class/error and execution-condition tabs before the threshold tab");
                AssertTrue(window.FindName("RefreshModelBenchmarkButton") is Wpf.Ui.Controls.Button refreshButton
                    && refreshButton.Command != null,
                    "benchmark refresh action should bind through the ViewModel");
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    internal static void TestWpfModelComparisonReviewService()
    {
        string root = CreateTempRoot();
        string comparisonArtifactsRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "artifacts",
            "yolo-model-comparison",
            "unit-latest-filter-" + Guid.NewGuid().ToString("N"));
        try
        {
            string baselineLabels = Path.Combine(root, "baseline", "labels");
            string candidateLabels = Path.Combine(root, "candidate", "labels");
            string datasetRoot = Path.Combine(root, "dataset");
            string testImages = Path.Combine(datasetRoot, "data", "test", "images");
            Directory.CreateDirectory(baselineLabels);
            Directory.CreateDirectory(candidateLabels);
            Directory.CreateDirectory(testImages);

            WriteLabel(baselineLabels, "same", "0 0.50 0.50 0.20 0.20 0.90");
            WriteLabel(candidateLabels, "same", "0 0.50 0.50 0.20 0.20 0.92");
            WriteLabel(baselineLabels, "class_changed", "0 0.30 0.30 0.20 0.20 0.88");
            WriteLabel(candidateLabels, "class_changed", "1 0.30 0.30 0.20 0.20 0.91");
            WriteLabel(baselineLabels, "baseline_only", "0 0.70 0.70 0.15 0.15 0.89");
            WriteLabel(candidateLabels, "candidate_only", "1 0.20 0.75 0.12 0.12 0.94");
            WriteLabel(candidateLabels, "low_confidence", "1 0.80 0.20 0.12 0.12 0.10");
            WriteImageFile(testImages, "class_changed.jpg");
            WriteImageFile(testImages, "baseline_only.png");
            WriteImageFile(testImages, "candidate_only.bmp");

            string dataYamlPath = Path.Combine(datasetRoot, "data.yaml");
            File.WriteAllText(
                dataYamlPath,
                string.Join(
                    Environment.NewLine,
                    "path: .",
                    "train: data/train/images",
                    "val: data/valid/images",
                    "test: data/test/images",
                    "nc: 2",
                    "names: [OK, NG]"));
            string summaryPath = Path.Combine(root, "comparison-summary.json");
            File.WriteAllText(
                summaryPath,
                JsonConvert.SerializeObject(new
                {
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    baseline = new
                    {
                        labelsPath = baselineLabels,
                        classMetrics = new[]
                        {
                            new { classId = 0, className = "native_contamination" },
                            new { classId = 1, className = "native_scratch" }
                        }
                    },
                    candidate = new { labelsPath = candidateLabels },
                    promotion = new
                    {
                        recommendation = "hold",
                        reason = "Candidate precision 0.016 is below the minimum 0.1; review labels/training before promotion."
                    }
                }));

            var service = new WpfModelComparisonReviewService();
            string expectedBaselineWeights = Path.Combine(root, "weights", "baseline.pt");
            string expectedCandidateWeights = Path.Combine(root, "weights", "candidate.pt");
            string staleBaselineWeights = Path.Combine(root, "weights", "stale-baseline.pt");
            string staleCandidateWeights = Path.Combine(root, "weights", "stale-candidate.pt");
            string matchingSummaryDirectory = Path.Combine(comparisonArtifactsRoot, "20260708-older-matching");
            string olderMatchingSummaryDirectory = Path.Combine(comparisonArtifactsRoot, "20260708-oldest-matching");
            string staleSummaryDirectory = Path.Combine(comparisonArtifactsRoot, "20260708-newer-stale");
            string malformedSummaryDirectory = Path.Combine(comparisonArtifactsRoot, "20260708-malformed");
            Directory.CreateDirectory(matchingSummaryDirectory);
            Directory.CreateDirectory(olderMatchingSummaryDirectory);
            Directory.CreateDirectory(staleSummaryDirectory);
            Directory.CreateDirectory(malformedSummaryDirectory);
            string matchingSummaryPath = Path.Combine(matchingSummaryDirectory, "comparison-summary.json");
            string olderMatchingSummaryPath = Path.Combine(olderMatchingSummaryDirectory, "comparison-summary.json");
            string staleSummaryPath = Path.Combine(staleSummaryDirectory, "comparison-summary.json");
            string malformedSummaryPath = Path.Combine(malformedSummaryDirectory, "comparison-summary.json");
            File.WriteAllText(
                matchingSummaryPath,
                JsonConvert.SerializeObject(new
                {
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    baseline = new { weights = expectedBaselineWeights, labelsPath = baselineLabels },
                    candidate = new { weights = expectedCandidateWeights, labelsPath = candidateLabels },
                    promotion = new
                    {
                        recommendation = "hold",
                        reason = "Candidate precision 0.016 is below the minimum 0.1; review labels/training before promotion."
                    }
                }));
            File.WriteAllText(
                staleSummaryPath,
                JsonConvert.SerializeObject(new
                {
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    baseline = new { weights = staleBaselineWeights, labelsPath = baselineLabels },
                    candidate = new { weights = staleCandidateWeights, labelsPath = candidateLabels },
                    promotion = new
                    {
                        recommendation = "promote",
                        reason = "Candidate improves mAP and does not regress precision or recall; review examples before saving it as the inspection model."
                    }
                }));
            File.Copy(matchingSummaryPath, olderMatchingSummaryPath);
            File.WriteAllText(malformedSummaryPath, "{ invalid json");
            File.SetLastWriteTimeUtc(matchingSummaryPath, DateTime.UtcNow.AddMinutes(-10));
            File.SetLastWriteTimeUtc(olderMatchingSummaryPath, DateTime.UtcNow.AddMinutes(-20));
            File.SetLastWriteTimeUtc(staleSummaryPath, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(malformedSummaryPath, DateTime.UtcNow.AddMinutes(10));
            IReadOnlyList<WpfModelComparisonHistoryItem> matchingHistory = service.BuildHistory(
                expectedBaselineWeights,
                expectedCandidateWeights,
                maxItems: 8);
            AssertEqual(2, matchingHistory.Count);
            AssertEqual(matchingSummaryPath, matchingHistory[0].SourcePath);
            AssertTrue(matchingHistory[0].IsLatest, "model comparison history should mark the newest matching summary as latest");
            AssertTrue(!matchingHistory[1].IsLatest, "older matching model comparison summaries should be read-only history");
            AssertTrue(matchingHistory[0].DisplayText.Contains("\uCD5C\uC2E0", StringComparison.Ordinal), "model comparison history should identify the latest run");
            AssertTrue(matchingHistory[0].DetailText.Contains("test", StringComparison.Ordinal), "model comparison history should identify the evidence split");
            AssertEqual(1, service.BuildHistory(expectedBaselineWeights, expectedCandidateWeights, maxItems: 1).Count);
            AssertEqual(0, service.BuildHistory(expectedBaselineWeights, Path.Combine(root, "weights", "unmatched-candidate.pt")).Count);
            WpfModelComparisonReviewReport matchedLatestReport = service.BuildLatestReport(
                new[] { "OK", "NG" },
                confidenceThreshold: 0.25D,
                maxExamples: 10,
                baselineWeightsPath: expectedBaselineWeights,
                candidateWeightsPath: expectedCandidateWeights);
            AssertTrue(matchedLatestReport.HasComparison, "latest model comparison lookup should find the summary that matches the current baseline/candidate weights");
            AssertTrue(matchedLatestReport.DetailText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal), "latest model comparison lookup should not use a newer summary from another candidate");
            AssertTrue(!matchedLatestReport.DetailText.Contains("\uAD50\uCCB4 \uCD94\uCC9C", StringComparison.Ordinal), "stale promote summaries from another candidate should not drive the current review");
            WpfModelComparisonReviewReport unmatchedLatestReport = service.BuildLatestReport(
                new[] { "OK", "NG" },
                confidenceThreshold: 0.25D,
                maxExamples: 10,
                baselineWeightsPath: expectedBaselineWeights,
                candidateWeightsPath: Path.Combine(root, "weights", "unmatched-candidate.pt"));
            AssertTrue(!unmatchedLatestReport.HasComparison, "latest model comparison lookup should fail closed when no summary matches the current candidate weights");

            WpfModelComparisonReviewReport report = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: null,
                maxExamples: 10);

            AssertTrue(report.HasComparison, "model comparison report should be visible when a summary exists");
            AssertTrue(report.SummaryText.Contains("\uBAA8\uB378 \uCC28\uC774 \uC608\uC2DC", StringComparison.Ordinal), "model comparison report should use learner-facing difference-example wording");
            AssertTrue(report.SummaryText.Contains("3", StringComparison.Ordinal), "model comparison report should count images with disagreements");
            AssertTrue(report.DetailText.Contains("\uAE30\uC874 \uBAA8\uB378 3", StringComparison.Ordinal), "model comparison report should count baseline detections above confidence");
            AssertTrue(report.DetailText.Contains("\uC0C8 \uBAA8\uB378 3", StringComparison.Ordinal), "model comparison report should count candidate detections above confidence");
            AssertTrue(report.DetailText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal), "model comparison report should surface the promotion hold decision");
            AssertTrue(report.DetailText.Contains("\uC815\uBC00\uB3C4", StringComparison.Ordinal), "model comparison report should translate low-precision promotion reasons for operators");
            AssertTrue(report.DetailText.Contains("1.6", StringComparison.Ordinal), "translated low-precision reason should preserve the current evidence value");
            AssertTrue(!report.DetailText.Contains("Candidate precision", StringComparison.Ordinal), "model comparison report should not expose raw English promotion reasons");
            AssertTrue(
                report.Examples.Any(example => example.ReviewText.Contains("native_scratch", StringComparison.Ordinal)),
                "self-describing comparison class metrics should override unrelated recipe class names in review examples");
            File.WriteAllText(
                summaryPath,
                JsonConvert.SerializeObject(new
                {
                    comparisonKind = "engine-benchmark",
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    imageSize = 320,
                    batchSize = 1,
                    benchmarkRepeatCount = 5,
                    baseline = new
                    {
                        engine = "YOLOv5",
                        labelsPath = baselineLabels,
                        metrics = new { precision = 0.81, recall = 0.74, map50 = 0.79, map5095 = 0.52 },
                        benchmark = new { preprocessMs = 0.7, inferenceMs = 6.8, postprocessMs = 0.8, taktMs = 8.3, taktMinMs = 7.9, taktMaxMs = 9.1, repeatCount = 5 }
                    },
                    candidate = new
                    {
                        engine = "YOLOv8",
                        labelsPath = candidateLabels,
                        metrics = new { precision = 0.84, recall = 0.78, map50 = 0.82, map5095 = 0.57 },
                        benchmark = new { preprocessMs = 0.5, inferenceMs = 5.1, postprocessMs = 0.6, taktMs = 6.2, taktMinMs = 5.8, taktMaxMs = 7.0, repeatCount = 5 }
                    },
                    promotion = new
                    {
                        recommendation = "hold",
                        reason = "Held-out comparison uses 9 labeled images; collect at least 10 before promotion."
                    }
                }));
            WpfModelComparisonReviewReport weakEvidenceReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: null,
                maxExamples: 10);
            AssertTrue(weakEvidenceReport.IsEngineComparison, "cross-engine summaries should be identified separately from candidate promotion comparisons");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("YOLOv5", StringComparison.Ordinal) && weakEvidenceReport.BenchmarkText.Contains("YOLOv8", StringComparison.Ordinal), "cross-engine summary should identify both runtimes");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("8.30", StringComparison.Ordinal) && weakEvidenceReport.BenchmarkText.Contains("6.20", StringComparison.Ordinal), "cross-engine summary should preserve per-model takt values");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("7.90-9.10", StringComparison.Ordinal) && weakEvidenceReport.BenchmarkText.Contains("n=5", StringComparison.Ordinal), "cross-engine summary should expose repeated takt range and sample count");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("\uBC18\uBCF5 5\uD68C \uC911\uC559\uAC12", StringComparison.Ordinal), "cross-engine summary should identify repeated takt values as medians");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("batch 1", StringComparison.Ordinal), "cross-engine summary should disclose the batch-one timing condition");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("test", StringComparison.Ordinal), "cross-engine summary should identify the test split");
            AssertTrue(weakEvidenceReport.BenchmarkText.Contains("\uD604\uC7A5 \uAC80\uC99D \uC544\uB2D8", StringComparison.Ordinal), "cross-engine test summary should not imply field validation");
            var engineComparisonViewModel = new WpfCandidateReviewPanelViewModel();
            engineComparisonViewModel.SetModelComparisonReview(weakEvidenceReport);
            AssertEqual(System.Windows.Visibility.Visible, engineComparisonViewModel.ModelComparisonBenchmarkVisibility);
            AssertTrue(engineComparisonViewModel.ModelComparisonBenchmarkText.Contains("mAP50-95", StringComparison.Ordinal), "candidate review should expose accuracy beside model takt");
            AssertTrue(engineComparisonViewModel.ModelComparisonActionText.Contains("Python", StringComparison.Ordinal), "cross-engine review should explain that engine adoption also changes runtime settings");
            AssertTrue(weakEvidenceReport.DetailText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "model comparison report should translate weak held-out evidence reasons");
            AssertTrue(weakEvidenceReport.DetailText.Contains("9", StringComparison.Ordinal) && weakEvidenceReport.DetailText.Contains("10", StringComparison.Ordinal), "translated weak-evidence reason should preserve evidence counts");
            AssertTrue(!weakEvidenceReport.DetailText.Contains("Held-out comparison", StringComparison.Ordinal), "model comparison report should not expose raw held-out evidence reasons");
            File.WriteAllText(
                summaryPath,
                JsonConvert.SerializeObject(new
                {
                    comparisonKind = "engine-benchmark",
                    dataYaml = dataYamlPath,
                    task = "val",
                    uiConfidence = 0.25,
                    imageSize = 320,
                    batchSize = 1,
                    evidence = new { split = "val", imageCount = 28, comparisonLabelCount = 28 },
                    baseline = new
                    {
                        engine = "YOLOv5",
                        labelsPath = baselineLabels,
                        metrics = new { precision = 0.81, recall = 0.74, map50 = 0.79, map5095 = 0.52 },
                        benchmark = new { taktMs = 8.3 }
                    },
                    candidate = new
                    {
                        engine = "YOLOv8",
                        labelsPath = candidateLabels,
                        metrics = new { precision = 0.84, recall = 0.78, map50 = 0.82, map5095 = 0.57 },
                        benchmark = new { taktMs = 6.2 }
                    },
                    promotion = new
                    {
                        recommendation = "promote",
                        reason = "Candidate improves mAP and does not regress precision or recall; review examples before saving it as the inspection model."
                    }
                }));
            WpfModelComparisonReviewReport validationEngineReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: null,
                maxExamples: 10);
            AssertTrue(validationEngineReport.BenchmarkText.Contains("\uD604\uC7A5 \uAC80\uC99D", StringComparison.Ordinal), "cross-engine validation summary should disclose the field-validation boundary");
            AssertTrue(validationEngineReport.BenchmarkText.Contains("val 28", StringComparison.Ordinal), "validation fallback should disclose its split and labeled image count");
            AssertTrue(validationEngineReport.BenchmarkText.Contains("\uAD50\uCCB4 \uD310\uB2E8 \uC544\uB2D8", StringComparison.Ordinal), "validation fallback should disclose that it is not model-adoption evidence");
            AssertTrue(validationEngineReport.RecommendationText.Contains("\uC5D4\uC9C4 \uBD84\uC11D", StringComparison.Ordinal), "validation fallback should be presented as an engine benchmark");
            AssertTrue(!validationEngineReport.RecommendationText.Contains("\uAD50\uCCB4 \uCD94\uCC9C", StringComparison.Ordinal), "validation fallback must not recommend model promotion");
            WpfModelComparisonReviewReport staleValidationEngineReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: 0.20,
                maxExamples: 10);
            AssertTrue(staleValidationEngineReport.RecommendationText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal), "validation engine benchmark should still reject a stale confidence basis");
            AssertTrue(staleValidationEngineReport.RecommendationText.Contains("25.0", StringComparison.Ordinal) && staleValidationEngineReport.RecommendationText.Contains("20.0", StringComparison.Ordinal), "stale validation benchmark should disclose the compared and current confidence values");
            File.WriteAllText(
                summaryPath,
                JsonConvert.SerializeObject(new
                {
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    baseline = new { labelsPath = baselineLabels },
                    candidate = new { labelsPath = candidateLabels },
                    promotion = new
                    {
                        recommendation = "hold",
                        reason = "Candidate produced 0 UI-threshold candidates at confidence 0.25; lower the review threshold or retrain before promotion."
                    }
                }));
            WpfModelComparisonReviewReport noUiCandidateReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: null,
                maxExamples: 10);
            AssertTrue(noUiCandidateReport.DetailText.Contains("\uAC80\uD1A0 \uAE30\uC900 \uC2E0\uB8B0\uB3C4", StringComparison.Ordinal), "model comparison report should translate zero UI-threshold candidate reasons");
            AssertTrue(noUiCandidateReport.DetailText.Contains("25.0", StringComparison.Ordinal), "translated zero-candidate reason should preserve the UI confidence threshold");
            AssertTrue(!noUiCandidateReport.DetailText.Contains("UI-threshold candidates", StringComparison.Ordinal), "model comparison report should not expose raw zero-candidate promotion reasons");
            File.WriteAllText(
                summaryPath,
                JsonConvert.SerializeObject(new
                {
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    baseline = new { labelsPath = baselineLabels },
                    candidate = new
                    {
                        labelsPath = candidateLabels,
                        confidence = new
                        {
                            thresholdSweep = new[]
                            {
                                new { confidence = 0.25, uiCandidateCount = 2 },
                                new { confidence = 0.10, uiCandidateCount = 10 }
                            }
                        }
                    },
                    promotion = new
                    {
                        recommendation = "promote",
                        reason = "Candidate improves mAP and does not regress precision or recall; review examples before saving it as the inspection model.",
                        reasons = new[]
                        {
                            "Candidate improves mAP and does not regress precision or recall; review examples before saving it as the inspection model."
                        }
                    }
                }));
            WpfModelComparisonReviewReport promoteReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: null,
                maxExamples: 10);
            AssertTrue(promoteReport.DetailText.Contains("\uAD50\uCCB4 \uCD94\uCC9C", StringComparison.Ordinal), "model comparison report should surface the promote decision");
            AssertTrue(promoteReport.DetailText.Contains("mAP", StringComparison.Ordinal), "translated promote reason should preserve the mAP improvement context");
            AssertTrue(promoteReport.DetailText.Contains("\uC815\uBC00\uB3C4", StringComparison.Ordinal), "translated promote reason should mention precision");
            AssertTrue(promoteReport.DetailText.Contains("\uC7AC\uD604\uC728", StringComparison.Ordinal), "translated promote reason should mention recall");
            AssertTrue(promoteReport.DetailText.Contains("\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5", StringComparison.Ordinal), "translated promote reason should point to saving as the inspection model");
            AssertTrue(promoteReport.DetailText.Contains("\uAC80\uD1A0 \uAE30\uC900\uBCC4 \uD6C4\uBCF4", StringComparison.Ordinal), "promote detail should still include the threshold sweep");
            AssertTrue(!promoteReport.DetailText.Contains("Candidate improves", StringComparison.Ordinal), "model comparison report should not expose raw English promote reasons");
            AssertEqual("promote", promoteReport.PromotionDecision);
            AssertTrue(promoteReport.RecommendationText.Contains("\uAD50\uCCB4 \uCD94\uCC9C", StringComparison.Ordinal), "model comparison report should expose the translated recommendation separately for summary cards");
            var historicalViewModel = new WpfCandidateReviewPanelViewModel();
            historicalViewModel.SetModelComparisonHistory(matchingHistory, olderMatchingSummaryPath);
            AssertEqual(System.Windows.Visibility.Visible, historicalViewModel.ModelComparisonHistoryVisibility);
            AssertEqual(olderMatchingSummaryPath, historicalViewModel.SelectedModelComparisonHistoryItem.SourcePath);
            historicalViewModel.SetModelComparisonReview(promoteReport, isHistoricalSelection: true);
            AssertTrue(historicalViewModel.IsHistoricalModelComparisonSelection, "selecting an older comparison should be visibly marked as historical");
            AssertTrue(historicalViewModel.IsModelPromotionHeld, "historical comparison results must not enable model adoption");
            AssertTrue(historicalViewModel.ModelComparisonDecisionText.Contains("\uACFC\uAC70 \uC2E4\uD589", StringComparison.Ordinal), "historical comparison results should identify their read-only decision boundary");
            AssertTrue(historicalViewModel.ModelComparisonActionText.Contains("\uCD5C\uC2E0 \uC2E4\uD589", StringComparison.Ordinal), "historical comparison guidance should route adoption back to the latest run");
            historicalViewModel.SetModelComparisonReview(promoteReport);
            AssertTrue(!historicalViewModel.IsHistoricalModelComparisonSelection && !historicalViewModel.IsModelPromotionHeld, "returning to the latest promotable comparison should clear the historical-only hold");
            WpfModelComparisonReviewReport confidenceMismatchReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: 0.20D,
                maxExamples: 10);
            AssertEqual("hold", confidenceMismatchReport.PromotionDecision);
            AssertTrue(confidenceMismatchReport.RecommendationText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal), "comparison confidence mismatch should fail closed as hold");
            AssertTrue(confidenceMismatchReport.RecommendationText.Contains("25.0", StringComparison.Ordinal)
                && confidenceMismatchReport.RecommendationText.Contains("20.0", StringComparison.Ordinal),
                "comparison confidence mismatch should preserve compared and current confidence values");
            AssertTrue(confidenceMismatchReport.RecommendationText.Contains("\uB2E4\uC2DC \uC2E4\uD589", StringComparison.Ordinal), "comparison confidence mismatch should direct the operator to rerun validation");
            AssertTrue(!confidenceMismatchReport.RecommendationText.Contains("Model comparison confidence", StringComparison.Ordinal), "comparison confidence mismatch should not expose raw English service text");
            var confidenceMismatchViewModel = new WpfCandidateReviewPanelViewModel();
            confidenceMismatchViewModel.SetModelComparisonReview(confidenceMismatchReport);
            AssertTrue(confidenceMismatchViewModel.IsModelPromotionHeld, "comparison confidence mismatch should block candidate adoption commands");
            File.WriteAllText(
                summaryPath,
                JsonConvert.SerializeObject(new
                {
                    dataYaml = dataYamlPath,
                    task = "test",
                    uiConfidence = 0.25,
                    baseline = new { labelsPath = baselineLabels },
                    candidate = new
                    {
                        labelsPath = candidateLabels,
                        confidence = new
                        {
                            thresholdSweep = new[]
                            {
                                new { confidence = 0.25, uiCandidateCount = 0 },
                                new { confidence = 0.10, uiCandidateCount = 0 },
                                new { confidence = 0.05, uiCandidateCount = 3 },
                                new { confidence = 0.01, uiCandidateCount = 259 }
                            }
                        }
                    },
                    promotion = new
                    {
                        recommendation = "hold",
                        reason = "Held-out comparison uses 9 labeled images; collect at least 10 before promotion.",
                        reasons = new[]
                        {
                            "Held-out comparison uses 9 labeled images; collect at least 10 before promotion.",
                            "Segment held-out comparison uses 3 positive segmentation labels; collect at least 5 positive mask labels before promotion.",
                            "Segment held-out comparison uses 3 positive segmentation images; collect at least 5 positive mask images before promotion.",
                            "Segment held-out comparison uses 3 background segmentation images; collect at least 5 background images before promotion.",
                            "Candidate produced 0 UI-threshold candidates at confidence 0.25; lower the review threshold or retrain before promotion.",
                            "Candidate UI-threshold positive image coverage 2/10 (0.2) is below minimum 0.5 at confidence 0.25; add varied training data or tune the model before promotion.",
                            "Candidate UI-threshold background candidate rate 2/6 (0.333) exceeds maximum 0.1 at confidence 0.25; add background data or tune the model before promotion."
                        }
                    }
                }));
            WpfModelComparisonReviewReport multiReasonReport = service.BuildFromSummaryFile(
                summaryPath,
                new[] { "OK", "NG" },
                confidenceThreshold: null,
                maxExamples: 10);
            AssertTrue(multiReasonReport.DetailText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "model comparison report should keep weak-evidence blockers when multiple promotion reasons exist");
            AssertTrue(multiReasonReport.DetailText.Contains("\uC591\uC131 \uB9C8\uC2A4\uD06C", StringComparison.Ordinal), "model comparison report should keep positive segmentation evidence blockers when multiple promotion reasons exist");
            AssertTrue(multiReasonReport.DetailText.Contains("\uC774\uBBF8\uC9C0", StringComparison.Ordinal), "model comparison report should keep positive segmentation image evidence blockers when multiple promotion reasons exist");
            AssertTrue(multiReasonReport.DetailText.Contains("\uC815\uC0C1 \uC774\uBBF8\uC9C0", StringComparison.Ordinal), "model comparison report should keep background segmentation evidence blockers when multiple promotion reasons exist");
            AssertTrue(multiReasonReport.DetailText.Contains("\uAC80\uD1A0 \uAE30\uC900 \uC2E0\uB8B0\uB3C4", StringComparison.Ordinal), "model comparison report should keep zero UI-candidate blockers when multiple promotion reasons exist");
            AssertTrue(multiReasonReport.DetailText.Contains("20.0", StringComparison.Ordinal) && multiReasonReport.DetailText.Contains("50.0", StringComparison.Ordinal), "model comparison report should preserve UI positive-image coverage and minimum rate");
            AssertTrue(multiReasonReport.DetailText.Contains("33.3", StringComparison.Ordinal) && multiReasonReport.DetailText.Contains("10.0", StringComparison.Ordinal), "model comparison report should preserve UI background-candidate rate and maximum rate");
            AssertTrue(multiReasonReport.DetailText.Contains("9", StringComparison.Ordinal) && multiReasonReport.DetailText.Contains("10", StringComparison.Ordinal), "multi-reason promotion detail should preserve held-out evidence counts");
            AssertTrue(multiReasonReport.DetailText.Contains("3", StringComparison.Ordinal) && multiReasonReport.DetailText.Contains("5", StringComparison.Ordinal), "multi-reason promotion detail should preserve positive segmentation evidence counts");
            AssertTrue(multiReasonReport.DetailText.Contains("25.0", StringComparison.Ordinal), "multi-reason promotion detail should preserve the UI confidence threshold");
            AssertTrue(multiReasonReport.DetailText.Contains("\uAC80\uD1A0 \uAE30\uC900\uBCC4 \uD6C4\uBCF4", StringComparison.Ordinal), "multi-reason promotion detail should include candidate threshold sweep guidance");
            AssertTrue(multiReasonReport.DetailText.Contains("5.0", StringComparison.Ordinal) && multiReasonReport.DetailText.Contains("3", StringComparison.Ordinal), "threshold sweep guidance should preserve lower-threshold candidate counts");
            AssertTrue(multiReasonReport.DetailText.Contains("1.0", StringComparison.Ordinal) && multiReasonReport.DetailText.Contains("259", StringComparison.Ordinal), "threshold sweep guidance should show the low-threshold candidate flood risk");
            AssertTrue(!multiReasonReport.DetailText.Contains("Held-out comparison", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw held-out text");
            AssertTrue(!multiReasonReport.DetailText.Contains("positive segmentation labels", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw positive segmentation text");
            AssertTrue(!multiReasonReport.DetailText.Contains("positive segmentation images", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw positive segmentation image text");
            AssertTrue(!multiReasonReport.DetailText.Contains("background segmentation images", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw background segmentation image text");
            AssertTrue(!multiReasonReport.DetailText.Contains("UI-threshold candidates", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw zero-candidate text");
            AssertTrue(!multiReasonReport.DetailText.Contains("UI-threshold positive image coverage", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw positive-image coverage text");
            AssertTrue(!multiReasonReport.DetailText.Contains("UI-threshold background candidate rate", StringComparison.Ordinal), "multi-reason promotion detail should not expose raw background-candidate text");
            AssertEqual("hold", multiReasonReport.PromotionDecision);
            AssertTrue(multiReasonReport.RecommendationText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal), "model comparison report should expose the translated hold recommendation separately for summary cards");
            var holdViewModel = new WpfCandidateReviewPanelViewModel();
            holdViewModel.SetModelComparisonReview(multiReasonReport);
            AssertTrue(holdViewModel.IsModelPromotionHeld, "model comparison ViewModel should preserve a fail-closed hold decision for candidate adoption commands");
            AssertTrue(holdViewModel.ModelComparisonDecisionText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal), "model-quality decision card should show the held-out recommendation instead of candidate workflow state");
            AssertTrue(holdViewModel.ModelComparisonActionText.Contains("\uB2E4\uC2DC \uC2E4\uD589", StringComparison.Ordinal), "held model comparison should direct the operator to improve data or tune and rerun validation");
            AssertTrue(!holdViewModel.ModelComparisonActionText.Contains("\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5", StringComparison.Ordinal), "held model comparison should not direct the operator to save the candidate as the inspection model");
            AssertEqual(3, report.Examples.Count);
            AssertTrue(
                report.Examples.Any(item => item.Kind == "ClassChanged"
                    && item.Detail.Contains("native_contamination", StringComparison.Ordinal)
                    && item.Detail.Contains("native_scratch", StringComparison.Ordinal)),
                "model comparison review should use self-describing comparison classes when surfacing class changes");
            AssertTrue(report.Examples.Any(item => item.Kind == "CandidateOnly" && item.ImageKey == "candidate_only"), "model comparison review should surface new-model-only candidates");
            AssertTrue(report.Examples.Any(item => item.Kind == "BaselineOnly" && item.ImageKey == "baseline_only"), "model comparison review should surface baseline-only candidates");
            AssertTrue(report.Examples.Any(item => item.Kind == "CandidateOnly" && item.ActionText.Contains("\uACFC\uAC80\uCD9C", StringComparison.Ordinal)), "new-model-only examples should explain the false-positive check");
            AssertTrue(report.Examples.Any(item => item.Kind == "BaselineOnly" && item.ActionText.Contains("\uAD50\uCCB4 \uBCF4\uB958", StringComparison.Ordinal)), "baseline-only examples should explain the replacement-hold risk");
            AssertTrue(report.Examples.Any(item => item.Kind == "ClassChanged" && item.ActionText.Contains("\uB77C\uBCA8 \uAE30\uC900", StringComparison.Ordinal)), "class-change examples should tell operators to check the label rule");
            AssertTrue(report.Examples.All(item => item.HasFocusBox), "model comparison examples should keep a focus box for click-to-review");
            AssertTrue(report.Examples.All(item => item.LocationText.Contains("\uC704\uCE58", StringComparison.Ordinal)), "model comparison examples should expose learner-facing location text");
            AssertTrue(!report.Examples.Any(item => item.ImageKey == "low_confidence"), "model comparison review should ignore candidates below the comparison confidence threshold");
            AssertTrue(report.Examples.All(item => !string.IsNullOrWhiteSpace(item.ImagePath) && File.Exists(item.ImagePath)), "model comparison examples should resolve source image paths through data.yaml");
            AssertTrue(report.Examples.Any(item => item.ImagePath.EndsWith("candidate_only.bmp", StringComparison.OrdinalIgnoreCase)), "model comparison image resolver should preserve source image extensions");

            string segmentBaselineLabels = Path.Combine(root, "segment-baseline", "labels");
            string segmentCandidateLabels = Path.Combine(root, "segment-candidate", "labels");
            Directory.CreateDirectory(segmentBaselineLabels);
            Directory.CreateDirectory(segmentCandidateLabels);
            WriteLabel(segmentBaselineLabels, "seg_shifted", "0 0.10 0.10 0.30 0.10 0.30 0.30 0.10 0.30");
            WriteLabel(segmentCandidateLabels, "seg_shifted", "0 0.60 0.60 0.80 0.60 0.80 0.80 0.60 0.80 0.93");
            WpfModelComparisonReviewReport segmentReport = service.BuildFromLabelDirectories(
                segmentBaselineLabels,
                segmentCandidateLabels,
                new[] { "SEG" },
                confidenceThreshold: 0.25D,
                iouThreshold: 0.5D,
                maxExamples: 10);
            AssertTrue(segmentReport.HasComparison, "segmentation label rows should produce a visible comparison report");
            AssertTrue(segmentReport.SummaryText.Contains("1", StringComparison.Ordinal), "segmentation comparison should count the shifted image as a difference");
            AssertEqual(2, segmentReport.Examples.Count);
            AssertTrue(segmentReport.Examples.Any(item => item.Kind == "CandidateOnly" && item.ImageKey == "seg_shifted"), "segmentation comparison should surface candidate-only polygon changes");
            AssertTrue(segmentReport.Examples.Any(item => item.Kind == "BaselineOnly" && item.ImageKey == "seg_shifted"), "segmentation comparison should surface baseline-only polygon changes");
            AssertTrue(segmentReport.Examples.All(item => item.HasFocusBox), "segmentation comparison examples should convert polygons to focus boxes");
            AssertTrue(segmentReport.Examples.Any(item => item.Kind == "CandidateOnly" && item.Detail.Contains("SEG", StringComparison.Ordinal) && item.Detail.Contains("93", StringComparison.Ordinal)), "segmentation comparison should preserve polygon class and confidence details");

            string malformedSegmentBaselineLabels = Path.Combine(root, "malformed-segment-baseline", "labels");
            string malformedSegmentCandidateLabels = Path.Combine(root, "malformed-segment-candidate", "labels");
            Directory.CreateDirectory(malformedSegmentBaselineLabels);
            Directory.CreateDirectory(malformedSegmentCandidateLabels);
            WriteLabel(malformedSegmentBaselineLabels, "bad_segment_fallback", "0 0.50 0.50 0.20 0.20 0.91 bad 0.10");
            WpfModelComparisonReviewReport malformedSegmentReport = service.BuildFromLabelDirectories(
                malformedSegmentBaselineLabels,
                malformedSegmentCandidateLabels,
                new[] { "SEG" },
                confidenceThreshold: 0.25D,
                iouThreshold: 0.5D,
                maxExamples: 10);
            AssertEqual(0, malformedSegmentReport.Examples.Count);
            AssertTrue(malformedSegmentReport.DetailText.Contains("\uAE30\uC874 \uBAA8\uB378 0\uAC1C", StringComparison.Ordinal), "malformed segmentation rows should not fall back to bbox confidence parsing");

            WpfModelComparisonReviewReport missingLabelsReport = service.BuildFromLabelDirectories(
                Path.Combine(root, "missing-baseline"),
                candidateLabels,
                new[] { "OK", "NG" });
            AssertTrue(missingLabelsReport.HasComparison, "missing labels should still produce an operator-visible comparison status");
            AssertEqual(0, missingLabelsReport.Examples.Count);
        }
        finally
        {
            if (Directory.Exists(comparisonArtifactsRoot))
            {
                Directory.Delete(comparisonArtifactsRoot, recursive: true);
            }

            DeleteTempRoot(root);
        }

        static void WriteLabel(string labelsRoot, string imageKey, string line)
        {
            File.WriteAllText(Path.Combine(labelsRoot, imageKey + ".txt"), line + Environment.NewLine);
        }

        static void WriteImageFile(string imageRoot, string fileName)
        {
            File.WriteAllBytes(Path.Combine(imageRoot, fileName), new byte[] { 1, 2, 3 });
        }
    }

    internal static void TestWpfModelComparisonRunService()
    {
        string root = CreateTempRoot();
        try
        {
            string scriptsRoot = Path.Combine(root, "scripts");
            string projectRoot = Path.Combine(root, "yolo");
            string sourceRoot = Path.Combine(projectRoot, "yolov5Master");
            string outputRoot = Path.Combine(root, "dataset-output");
            string candidateWeights = Path.Combine(outputRoot, "runs", "train", "exp", "weights", "best.pt");
            string baselineWeights = Path.Combine(projectRoot, "best.pt");
            string pythonPath = Path.Combine(projectRoot, ".venv", "Scripts", "python.exe");
            Directory.CreateDirectory(scriptsRoot);
            Directory.CreateDirectory(sourceRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(candidateWeights));
            Directory.CreateDirectory(Path.GetDirectoryName(pythonPath));
            File.WriteAllText(Path.Combine(scriptsRoot, "compare-yolo-models.ps1"), "param()");
            File.WriteAllText(Path.Combine(sourceRoot, "val.py"), "# val");
            File.WriteAllText(pythonPath, string.Empty);
            File.WriteAllText(baselineWeights, "baseline");
            File.WriteAllText(candidateWeights, "candidate");
            File.SetLastWriteTimeUtc(candidateWeights, DateTime.UtcNow.AddMinutes(1));

            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            Directory.CreateDirectory(data.TrainImagesPath);
            Directory.CreateDirectory(data.ValidImagesPath);
            Directory.CreateDirectory(data.TestImagesPath);
            string testLabelsPath = Path.Combine(Path.GetDirectoryName(data.TestImagesPath) ?? outputRoot, "labels");
            Directory.CreateDirectory(testLabelsPath);
            File.WriteAllBytes(Path.Combine(data.TestImagesPath, "heldout.bmp"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(testLabelsPath, "heldout.txt"), "0 0.5 0.5 0.25 0.25" + Environment.NewLine);
            data.ProjectSettings.PythonModel.ProjectRootPath = projectRoot;
            data.ProjectSettings.PythonModel.PythonExecutablePath = pythonPath;
            data.ProjectSettings.PythonModel.WeightsPath = baselineWeights;
            data.ProjectSettings.PythonModel.MinimumDetectionConfidence = 0.42F;
            data.TranningParam.imageSize = 640;
            data.TranningParam.batch = 4;
            File.WriteAllText(
                data.DataYamlFilePath,
                string.Join(
                    Environment.NewLine,
                    "train: data/train/images",
                    "val: data/valid/images",
                    "test: data/test/images",
                    "nc: 1",
                    "names: [Defect]"));

            var service = new WpfModelComparisonRunService(root);
            WpfModelComparisonRunRequest request = service.BuildRequest(data, new WpfTrainingWeightsService(), task: "test");

            AssertEqual(Path.Combine(scriptsRoot, "compare-yolo-models.ps1"), request.ScriptPath);
            AssertEqual(sourceRoot, request.YoloSourceRootPath);
            AssertEqual(data.DataYamlFilePath, request.DataYamlPath);
            AssertEqual(baselineWeights, request.BaselineWeightsPath);
            AssertEqual(candidateWeights, request.CandidateWeightsPath);
            AssertEqual(640, request.ImageSize);
            AssertEqual(4, request.BatchSize);
            AssertEqual(1, request.BenchmarkRepeatCount);
            AssertEqual("test", request.Task);
            AssertEqual("detect", request.ModelTask);
            AssertTrue(Math.Abs(request.UiConfidence - 0.42D) < 0.0001D, "model comparison request should preserve UI confidence");
            AssertEqual(0, service.ValidateRequest(request).Count);
            request.BenchmarkRepeatCount = 0;
            AssertTrue(service.ValidateRequest(request).Any(error => error.Contains("1~10", StringComparison.Ordinal)), "model comparison should reject an invalid benchmark repeat count");
            request.BenchmarkRepeatCount = 1;

            IReadOnlyList<string> arguments = service.BuildPowerShellArguments(request);
            AssertTrue(arguments.Contains("-Task"), "model comparison run should pass the task argument");
            AssertTrue(arguments.Contains("test"), "model comparison run should default to held-out test comparison");
            AssertTrue(arguments.Contains("-ModelTask"), "model comparison run should pass the model task argument");
            AssertTrue(arguments.Contains("-BenchmarkRepeatCount") && arguments.Contains("1"), "candidate validation should keep a single native timing measurement by default");
            AssertTrue(arguments.Contains("detect"), "model comparison run should default object detection projects to detect validation");
            AssertTrue(arguments.Contains("-DataYaml"), "model comparison run should pass data.yaml explicitly");
            AssertTrue(arguments.Contains(data.DataYamlFilePath), "model comparison run should use the current project data.yaml");
            AssertTrue(arguments.Contains("-CandidateWeights"), "model comparison run should pass candidate weights explicitly");
            AssertTrue(arguments.Contains(candidateWeights), "model comparison run should use the latest training best.pt as candidate");
            AssertTrue(!arguments.Contains("-SegmentationPositiveClassName"), "detection comparison should not pass a segmentation-only positive class filter");
            string realScriptSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "compare-yolo-models.ps1"));
            AssertTrue(realScriptSource.Contains("Assert-ModelClassCountsMatchData", StringComparison.Ordinal), "model comparison script should preflight model/data label-count compatibility");
            AssertTrue(realScriptSource.Contains("Get-YoloV5LabelCacheArtifactPaths", StringComparison.Ordinal)
                && realScriptSource.Contains("Remove-YoloV5CreatedLabelCacheArtifacts", StringComparison.Ordinal)
                && realScriptSource.Contains("$cacheSidecarPath = $cachePath + \".npy\"", StringComparison.Ordinal)
                && realScriptSource.Contains("$yoloV5SourceCacheArtifactsBefore", StringComparison.Ordinal)
                && realScriptSource.Contains("$yoloV5CreatedCacheArtifactsRemoved", StringComparison.Ordinal)
                && realScriptSource.Contains("Remove-Item -LiteralPath $cacheArtifactPath", StringComparison.Ordinal)
                && realScriptSource.Contains("runtimePreflight", StringComparison.Ordinal),
                "YOLOv5 comparison should preserve existing source caches and remove only cache artifacts created by the current run");
            AssertTrue(realScriptSource.Contains("Read-WeightsClassCount", StringComparison.Ordinal), "model comparison script should read each weights file label count before YOLO val");
            AssertTrue(realScriptSource.Contains("Read-DataYamlClassNames", StringComparison.Ordinal), "model comparison script should read data.yaml class names before YOLO val");
            AssertTrue(realScriptSource.Contains("return @((Read-DataYamlClassNames $Path)).Count", StringComparison.Ordinal), "model comparison script should accept valid names-only data.yaml files without an nc field");
            AssertTrue(realScriptSource.Contains("Read-WeightsClassInfo", StringComparison.Ordinal), "model comparison script should read each weights file label names before YOLO val");
            AssertTrue(realScriptSource.Contains("Test-ClassNamesEqual", StringComparison.Ordinal), "model comparison script should reject same-count but different-name label lists");
            AssertTrue(realScriptSource.Contains("dataset labels=", StringComparison.Ordinal), "model comparison preflight should explain label-count mismatch clearly");
            AssertTrue(realScriptSource.Contains("Invoke-UltralyticsVal", StringComparison.Ordinal), "model comparison script should support local Ultralytics validation");
            AssertTrue(realScriptSource.Contains("OPENVISIONLAB_METRICS_JSON", StringComparison.Ordinal), "Ultralytics validation should emit parseable metrics for comparison reports");
            AssertTrue(realScriptSource.Contains("OPENVISIONLAB_BENCHMARK_JSON", StringComparison.Ordinal), "Ultralytics validation should emit parseable native timing metrics");
            AssertTrue(realScriptSource.Contains("Read-ValBenchmark", StringComparison.Ordinal), "model comparison should normalize native YOLO validation timing");
            AssertTrue(realScriptSource.Contains("comparisonKind", StringComparison.Ordinal), "model comparison summary should distinguish engine benchmarks from candidate validation");
            AssertTrue(realScriptSource.Contains("Held-out test results compare engine profiles only", StringComparison.Ordinal), "cross-engine test comparison should not emit a model-replacement recommendation");
            AssertTrue(realScriptSource.Contains("BenchmarkRepeatCount", StringComparison.Ordinal), "model comparison should support repeated native timing measurements");
            AssertTrue(realScriptSource.Contains("native-validation-speed-median", StringComparison.Ordinal), "repeated model takt should be reported as a median");
            AssertTrue(realScriptSource.Contains("taktSamplesMs", StringComparison.Ordinal), "model comparison summary should preserve the repeated native timing samples");
            AssertTrue(realScriptSource.Contains("requested $RequestedRepeatCount timing samples", StringComparison.Ordinal), "repeated model takt should fail closed when any requested timing sample is missing");
            AssertTrue(realScriptSource.Contains("requested task=", StringComparison.Ordinal), "model comparison should reject detect/segment task mismatches before validation");
            AssertTrue(realScriptSource.Contains("Model Takt", StringComparison.Ordinal), "model comparison report should disclose native per-image model takt");
            AssertTrue(realScriptSource.Contains("BaselinePythonExe", StringComparison.Ordinal) && realScriptSource.Contains("CandidatePythonExe", StringComparison.Ordinal), "cross-engine comparison should use each engine's own Python runtime");
            AssertTrue(realScriptSource.Contains("model.predict", StringComparison.Ordinal), "Ultralytics comparison should count UI candidates from predict labels, not validation labels");
            AssertTrue(realScriptSource.Contains("New-PredictionSourceManifest", StringComparison.Ordinal)
                && realScriptSource.Contains("Get-ChildItem -LiteralPath $ResolvedPath -Recurse -File", StringComparison.Ordinal)
                && realScriptSource.Contains("predict_source_path.suffix.lower() == \".txt\"", StringComparison.Ordinal),
                "cross-engine comparison should recurse nested split folders and pass their image list to each prediction runtime");
            AssertTrue(realScriptSource.Contains("New-ComparisonRuntimeDataYaml", StringComparison.Ordinal)
                && realScriptSource.Contains("$script:RuntimeDataYaml", StringComparison.Ordinal)
                && realScriptSource.Contains("runtimeDataYaml", StringComparison.Ordinal),
                "native relative data.yaml roots should be rewritten only in an artifact-local runtime YAML and recorded in comparison provenance");
            AssertTrue(realScriptSource.Contains("duplicate image stem", StringComparison.Ordinal)
                && realScriptSource.Contains("generated_label_path = labels_path / f\"image{index}.txt\"", StringComparison.Ordinal)
                && realScriptSource.Contains("generated_label_path.replace(destination_label_path)", StringComparison.Ordinal),
                "Ultralytics list-source prediction labels should be restored to unique source stems before ground-truth review");
            AssertTrue(realScriptSource.Contains("$RunName-predict\\labels", StringComparison.Ordinal), "Ultralytics comparison should expose predict labels for Candidate Review examples");
            AssertTrue(realScriptSource.Contains("validationLabelsPath", StringComparison.Ordinal), "Ultralytics comparison should keep validation labels separate from UI review labels");
            AssertTrue(realScriptSource.Contains("ModelTask", StringComparison.Ordinal), "model comparison script should keep split task separate from detect/segment task");
            AssertTrue(realScriptSource.Contains("Training validation (val) results are for engine performance analysis", StringComparison.Ordinal), "cross-engine validation reports should be benchmark-only before the summary is written");
            AssertTrue(realScriptSource.Contains("not model-replacement evidence", StringComparison.Ordinal), "cross-engine validation markdown should disclose that val is not adoption evidence");
            AssertTrue(realScriptSource.Contains("New-PromotionRecommendation", StringComparison.Ordinal), "model comparison script should write a promotion recommendation");
            AssertTrue(realScriptSource.Contains("minimumPrecision", StringComparison.Ordinal), "model comparison recommendation should guard low-precision candidates");
            AssertTrue(realScriptSource.Contains("New-ComparisonEvidence", StringComparison.Ordinal), "model comparison script should count held-out evidence before writing a promotion recommendation");
            AssertTrue(realScriptSource.Contains("comparisonLabelCount", StringComparison.Ordinal), "model comparison summary should persist the held-out labeled image count");
            AssertTrue(realScriptSource.Contains("fingerprintSha256", StringComparison.Ordinal), "model comparison summary should persist a content fingerprint for evaluation images and labels");
            AssertTrue(realScriptSource.Contains("weightsSha256", StringComparison.Ordinal), "model comparison summary should persist each evaluated weights fingerprint");
            AssertTrue(realScriptSource.Contains("minimumHeldoutLabelCount", StringComparison.Ordinal), "model comparison recommendation should block promotion when held-out evidence is too small");
            AssertTrue(realScriptSource.Contains("minimumPositiveSegmentationLabelLineCount", StringComparison.Ordinal), "model comparison recommendation should block promotion when positive segmentation evidence is too small");
            AssertTrue(realScriptSource.Contains("minimumPositiveSegmentationImageCount", StringComparison.Ordinal), "model comparison recommendation should block promotion when positive segmentation image evidence is too small");
            AssertTrue(realScriptSource.Contains("minimumBackgroundSegmentationImageCount", StringComparison.Ordinal), "model comparison recommendation should require background segmentation evidence before promotion");
            AssertTrue(realScriptSource.Contains("uiCandidateCount", StringComparison.Ordinal), "model comparison recommendation should inspect UI-threshold candidate count");
            AssertTrue(realScriptSource.Contains("UI-threshold candidates", StringComparison.Ordinal), "model comparison recommendation should block promotion when the candidate produces no UI-visible candidates");
            AssertTrue(realScriptSource.Contains("uiPositiveImageCoverage", StringComparison.Ordinal), "model comparison summary should persist UI-threshold positive-image coverage");
            AssertTrue(realScriptSource.Contains("UI-threshold positive image coverage", StringComparison.Ordinal), "model comparison recommendation should block low positive-image coverage");
            AssertTrue(realScriptSource.Contains("uiBackgroundCandidateRate", StringComparison.Ordinal), "model comparison summary should persist UI-threshold background-candidate rate");
            AssertTrue(realScriptSource.Contains("UI-threshold background candidate rate", StringComparison.Ordinal), "model comparison recommendation should block excessive background candidates");
            AssertTrue(realScriptSource.Contains("SegmentationPositiveClassName", StringComparison.Ordinal), "model comparison script should support class-specific SEG operating evidence");
            AssertTrue(realScriptSource.Contains("Test-SegmentationLabelLineForClass", StringComparison.Ordinal), "SEG answer evidence should filter positive images by the configured defect class");
            AssertTrue(realScriptSource.Contains("Test-YoloLabelLineClass", StringComparison.Ordinal), "SEG prediction evidence should filter UI candidates by the same defect class");
            AssertTrue(realScriptSource.Contains("segmentationPositiveClassName", StringComparison.Ordinal), "model comparison summary should record the class used for SEG operating evidence");
            AssertTrue(realScriptSource.Contains("thresholdSweep", StringComparison.Ordinal), "model comparison confidence summary should persist review-threshold candidate counts");
            AssertTrue(realScriptSource.Contains("Read-ValClassMetrics", StringComparison.Ordinal), "YOLOv5 comparison should preserve native per-class validation metrics");
            AssertTrue(realScriptSource.Contains("payload[\"perClass\"]", StringComparison.Ordinal), "Ultralytics comparison should preserve native per-class validation metrics");
            AssertTrue(realScriptSource.Contains("New-GroundTruthReview", StringComparison.Ordinal), "detection comparison should calculate UI-threshold ground-truth errors");
            AssertTrue(realScriptSource.Contains("iouThreshold", StringComparison.Ordinal), "ground-truth error evidence should disclose its IoU threshold");
            AssertTrue(realScriptSource.Contains("predictionNmsIouThreshold", StringComparison.Ordinal), "ground-truth error evidence should disclose its prediction NMS threshold");
            AssertTrue(realScriptSource.Contains("schemaVersion = 2", StringComparison.Ordinal), "detection comparison should version its stored ground-truth review schema");
            AssertTrue(realScriptSource.Contains("geometryCoordinateSystem = \"normalized-xyxy-v1\"", StringComparison.Ordinal), "detection comparison should disclose normalized error-box coordinates");
            AssertTrue(realScriptSource.Contains("predictionBox = ConvertTo-GroundTruthReviewBox", StringComparison.Ordinal)
                && realScriptSource.Contains("groundTruthBox = ConvertTo-GroundTruthReviewBox", StringComparison.Ordinal),
                "detection comparison should retain prediction and answer geometry for stored error examples");
            AssertTrue(realScriptSource.Contains("ConvertTo-Json -Depth 10", StringComparison.Ordinal), "comparison JSON depth should preserve v2 threshold and geometry fields");
            AssertTrue(realScriptSource.Contains("\"--iou-thres\", $UiNmsIou", StringComparison.Ordinal), "YOLOv5 error examples should use the configured UI NMS threshold without changing validation metrics");
            AssertTrue(realScriptSource.Contains("iou=prediction_nms_iou", StringComparison.Ordinal), "Ultralytics error examples should use the same UI NMS threshold");
            AssertTrue(realScriptSource.Contains("reasons = @($reasonList)", StringComparison.Ordinal), "model comparison recommendation should persist every promotion blocker");
            AssertTrue(realScriptSource.Contains("promotion = $promotion", StringComparison.Ordinal), "model comparison summary should persist the promotion recommendation");
            AssertTrue(realScriptSource.Contains("evidence = $evidence", StringComparison.Ordinal), "model comparison summary should persist held-out comparison evidence");
            AssertTrue(realScriptSource.Contains("## Recommendation", StringComparison.Ordinal), "model comparison report should show the promotion recommendation");
            AssertTrue(!realScriptSource.Any(ch => ch >= '\uF900' && ch <= '\uFAFF'), "model comparison script should avoid PowerShell 5 mojibake-prone diagnostic text");
            string runServiceSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "0. UI", "9) WPF", "Services", "Model", "WpfModelComparisonRunService.cs"));
            AssertTrue(runServiceSource.Contains("CountDataYamlLabelFiles", StringComparison.Ordinal), "model comparison run service should require held-out answer label files before launching validation");
            ProcessStartInfo startInfo = service.CreateStartInfo(request);
            AssertEqual("powershell.exe", startInfo.FileName);
            AssertTrue(!startInfo.UseShellExecute, "model comparison process should redirect output for UI status");

            string yolo8DetectRoot = Path.Combine(root, "yolov8-detect");
            string yolo8DetectSourceRoot = Path.Combine(yolo8DetectRoot, "ultralyticsMaster");
            string yolo8DetectPythonPath = Path.Combine(yolo8DetectRoot, ".venv", "Scripts", "python.exe");
            string yolo8DetectWeightsPath = Path.Combine(yolo8DetectRoot, "runs", "detect", "exp", "weights", "best.pt");
            Directory.CreateDirectory(Path.Combine(yolo8DetectSourceRoot, "ultralytics"));
            Directory.CreateDirectory(Path.GetDirectoryName(yolo8DetectPythonPath));
            Directory.CreateDirectory(Path.GetDirectoryName(yolo8DetectWeightsPath));
            File.WriteAllText(yolo8DetectPythonPath, string.Empty);
            File.WriteAllText(yolo8DetectWeightsPath, "yolov8-detect");
            data.ProjectSettings.ModelRegistry.EnsureDefaults();
            data.ProjectSettings.ModelRegistry.Profiles.Add(new ModelProfile
            {
                ProfileId = "profile-yolov8-detect",
                DisplayName = "YOLOv8 Detect",
                ModelEngine = PythonModelSettings.EngineYoloV8,
                DatasetPurpose = LabelingDatasetPurpose.ObjectDetection.ToString(),
                ProjectRootPath = yolo8DetectRoot,
                LastUsedUtc = DateTime.UtcNow.ToString("o")
            });
            data.ProjectSettings.ModelRegistry.Candidates.Add(new ModelCandidate
            {
                CandidateId = "candidate-yolov8-detect",
                ProfileId = "profile-yolov8-detect",
                WeightsPath = yolo8DetectWeightsPath,
                LastSeenUtc = DateTime.UtcNow.ToString("o")
            });

            WpfModelComparisonRunRequest automaticTestEngineRequest = service.BuildYoloV5YoloV8DetectionRequest(data);
            AssertEqual("test", automaticTestEngineRequest.Task);
            WpfModelComparisonRunRequest engineRequest = service.BuildYoloV5YoloV8DetectionRequest(data, task: "test");
            AssertTrue(engineRequest.IsEngineComparison, "YOLOv5/YOLOv8 comparison should be marked as a cross-engine benchmark");
            AssertEqual(PythonModelSettings.EngineYoloV5, engineRequest.BaselineModelEngine);
            AssertEqual(PythonModelSettings.EngineYoloV8, engineRequest.CandidateModelEngine);
            AssertEqual(sourceRoot, engineRequest.BaselineYoloSourceRootPath);
            AssertEqual(yolo8DetectSourceRoot, engineRequest.CandidateYoloSourceRootPath);
            AssertEqual(pythonPath, engineRequest.BaselinePythonExecutablePath);
            AssertEqual(yolo8DetectPythonPath, engineRequest.CandidatePythonExecutablePath);
            AssertEqual(baselineWeights, engineRequest.BaselineWeightsPath);
            AssertEqual(yolo8DetectWeightsPath, engineRequest.CandidateWeightsPath);
            AssertEqual(1, engineRequest.BatchSize);
            AssertEqual(5, engineRequest.BenchmarkRepeatCount);
            AssertEqual("detect", engineRequest.ModelTask);
            AssertEqual(0, service.ValidateRequest(engineRequest).Count);
            IReadOnlyList<string> engineArguments = service.BuildPowerShellArguments(engineRequest);
            AssertTrue(engineArguments.Contains("-BaselinePythonExe") && engineArguments.Contains(pythonPath), "cross-engine comparison should pass the YOLOv5 Python runtime");
            AssertTrue(engineArguments.Contains("-BaselineYoloSourceRoot") && engineArguments.Contains(sourceRoot), "cross-engine comparison should pass the YOLOv5 source root");
            AssertTrue(engineArguments.Contains("-BaselineEngine") && engineArguments.Contains(PythonModelSettings.EngineYoloV5), "cross-engine comparison should identify the YOLOv5 runtime");
            AssertTrue(engineArguments.Contains("-CandidatePythonExe") && engineArguments.Contains(yolo8DetectPythonPath), "cross-engine comparison should pass the YOLOv8 Python runtime");
            AssertTrue(engineArguments.Contains("-CandidateYoloSourceRoot") && engineArguments.Contains(yolo8DetectSourceRoot), "cross-engine comparison should pass the YOLOv8 source root");
            AssertTrue(engineArguments.Contains("-CandidateEngine") && engineArguments.Contains(PythonModelSettings.EngineYoloV8), "cross-engine comparison should identify the YOLOv8 runtime");
            AssertTrue(engineArguments.Contains("-BenchmarkRepeatCount") && engineArguments.Contains("5"), "cross-engine comparison should request five native timing measurements");

            string yolo11DetectWeightsPath = Path.Combine(yolo8DetectRoot, "runs", "detect", "yolo11", "weights", "best.pt");
            Directory.CreateDirectory(Path.GetDirectoryName(yolo11DetectWeightsPath));
            File.WriteAllText(yolo11DetectWeightsPath, "yolo11-detect");
            data.ProjectSettings.ModelRegistry.Profiles.Add(new ModelProfile
            {
                ProfileId = "profile-yolo11-detect",
                DisplayName = "YOLO11 Detect",
                ModelEngine = PythonModelSettings.EngineYolo11,
                DatasetPurpose = LabelingDatasetPurpose.ObjectDetection.ToString(),
                ProjectRootPath = yolo8DetectRoot,
                LastUsedUtc = DateTime.UtcNow.AddMinutes(1).ToString("o")
            });
            data.ProjectSettings.ModelRegistry.Candidates.Add(new ModelCandidate
            {
                CandidateId = "candidate-yolo11-detect",
                ProfileId = "profile-yolo11-detect",
                WeightsPath = yolo11DetectWeightsPath,
                LastSeenUtc = DateTime.UtcNow.AddMinutes(1).ToString("o")
            });
            WpfModelComparisonRunRequest yolo11EngineRequest = service.BuildYoloV8Yolo11DetectionRequest(data, task: "test");
            AssertTrue(yolo11EngineRequest.IsEngineComparison, "YOLOv8/YOLO11 comparison should be marked as a cross-engine benchmark");
            AssertEqual(PythonModelSettings.EngineYoloV8, yolo11EngineRequest.BaselineModelEngine);
            AssertEqual(PythonModelSettings.EngineYolo11, yolo11EngineRequest.CandidateModelEngine);
            AssertEqual(yolo8DetectWeightsPath, yolo11EngineRequest.BaselineWeightsPath);
            AssertEqual(yolo11DetectWeightsPath, yolo11EngineRequest.CandidateWeightsPath);
            AssertEqual(yolo8DetectSourceRoot, yolo11EngineRequest.BaselineYoloSourceRootPath);
            AssertEqual(yolo8DetectSourceRoot, yolo11EngineRequest.CandidateYoloSourceRootPath);
            AssertEqual(0, service.ValidateRequest(yolo11EngineRequest).Count);
            IReadOnlyList<string> yolo11EngineArguments = service.BuildPowerShellArguments(yolo11EngineRequest);
            AssertTrue(yolo11EngineArguments.Contains("-CandidateEngine") && yolo11EngineArguments.Contains(PythonModelSettings.EngineYolo11), "cross-engine comparison should identify the YOLO11 runtime");

            string yolo8Root = Path.Combine(root, "yolov8");
            string ultralyticsRoot = Path.Combine(yolo8Root, "ultralyticsMaster");
            string segOutputRoot = Path.Combine(root, "seg-output");
            string segCandidateWeights = Path.Combine(yolo8Root, "runs", "segment", "exp", "weights", "best.pt");
            string segBaselineWeights = Path.Combine(yolo8Root, "yolov8n-seg.pt");
            string segPythonPath = Path.Combine(yolo8Root, ".venv", "Scripts", "python.exe");
            Directory.CreateDirectory(Path.Combine(ultralyticsRoot, "ultralytics"));
            Directory.CreateDirectory(Path.GetDirectoryName(segCandidateWeights));
            Directory.CreateDirectory(Path.GetDirectoryName(segPythonPath));
            File.WriteAllText(segPythonPath, string.Empty);
            File.WriteAllText(segBaselineWeights, "baseline-seg");
            File.WriteAllText(segCandidateWeights, "candidate-seg");
            File.SetLastWriteTimeUtc(segCandidateWeights, DateTime.UtcNow.AddMinutes(2));

            var segData = new CData();
            segData.ConfigureOutputRoot(segOutputRoot);
            segData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.Segmentation;
            segData.ClassNamedList.Clear();
            segData.ClassNamedList.Add(new CClassItem { Text = "OK" });
            segData.ClassNamedList.Add(new CClassItem { Text = "NG" });
            Directory.CreateDirectory(segData.TrainImagesPath);
            Directory.CreateDirectory(segData.ValidImagesPath);
            Directory.CreateDirectory(segData.TestImagesPath);
            string segTestLabelsPath = Path.Combine(Path.GetDirectoryName(segData.TestImagesPath) ?? segOutputRoot, "labels");
            Directory.CreateDirectory(segTestLabelsPath);
            File.WriteAllBytes(Path.Combine(segData.TestImagesPath, "seg-heldout.bmp"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(segTestLabelsPath, "seg-heldout.txt"), "1 0.1 0.1 0.9 0.1 0.9 0.9 0.1 0.9" + Environment.NewLine);
            segData.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV8;
            segData.ProjectSettings.PythonModel.ProjectRootPath = yolo8Root;
            segData.ProjectSettings.PythonModel.PythonExecutablePath = segPythonPath;
            segData.ProjectSettings.PythonModel.WeightsPath = segBaselineWeights;
            File.WriteAllText(
                segData.DataYamlFilePath,
                string.Join(
                    Environment.NewLine,
                    "train: data/train/images",
                    "val: data/valid/images",
                    "test: data/test/images",
                    "nc: 2",
                    "names: [OK, NG]"));

            WpfModelComparisonRunRequest segRequest = service.BuildRequest(segData, new WpfTrainingWeightsService(), task: "test");
            AssertEqual(ultralyticsRoot, segRequest.YoloSourceRootPath);
            AssertEqual("segment", segRequest.ModelTask);
            AssertEqual("NG", segRequest.SegmentationPositiveClassName);
            AssertEqual(0, service.ValidateRequest(segRequest).Count);
            IReadOnlyList<string> segArguments = service.BuildPowerShellArguments(segRequest);
            AssertTrue(segArguments.Contains("-ModelTask"), "YOLOv8 segmentation comparison should pass the model task switch");
            AssertTrue(segArguments.Contains("segment"), "YOLOv8 segmentation comparison should run Ultralytics segment validation");
            AssertTrue(segArguments.Contains(ultralyticsRoot), "YOLOv8 segmentation comparison should use the local ultralyticsMaster checkout");
            AssertTrue(segArguments.Contains("-SegmentationPositiveClassName"), "YOLOv8 segmentation comparison should pass the defect-class operating filter");
            AssertTrue(segArguments.Contains("NG"), "YOLOv8 segmentation comparison should use NG as the positive operating class when the catalog exposes OK/NG");
            string segHeldoutLabelPath = Path.Combine(segTestLabelsPath, "seg-heldout.txt");
            File.WriteAllText(segHeldoutLabelPath, string.Empty);
            AssertTrue(service.ValidateRequest(segRequest).Any(error => error.Contains("segmentation label", StringComparison.OrdinalIgnoreCase)), "YOLOv8 segmentation comparison should reject held-out splits that only contain empty OK/background labels");
            File.WriteAllText(segHeldoutLabelPath, "1 0.5 0.5 0.25 0.25" + Environment.NewLine);
            AssertTrue(service.ValidateRequest(segRequest).Any(error => error.Contains("segmentation label", StringComparison.OrdinalIgnoreCase)), "YOLOv8 segmentation comparison should reject bbox-only labels for segment validation");
            File.WriteAllText(segHeldoutLabelPath, "1 0.1 0.1 0.9 0.1 0.9 0.9 0.2" + Environment.NewLine);
            AssertTrue(service.ValidateRequest(segRequest).Any(error => error.Contains("segmentation label", StringComparison.OrdinalIgnoreCase)), "YOLOv8 segmentation comparison should reject malformed segment labels with an unpaired coordinate");
            File.WriteAllText(segHeldoutLabelPath, "1 0.1 0.1 0.9 0.1 0.9 0.9 0.1 0.9" + Environment.NewLine);
            AssertEqual(0, service.ValidateRequest(segRequest).Count);

            request.CandidateWeightsPath = Path.Combine(root, "missing.pt");
            AssertTrue(service.ValidateRequest(request).Any(error => error.Contains("\uC0C8 \uBAA8\uB378", StringComparison.OrdinalIgnoreCase)), "model comparison validation should reject a missing candidate model file");

            request.CandidateWeightsPath = baselineWeights;
            AssertTrue(service.ValidateRequest(request).Any(error => error.Contains("\uAC19", StringComparison.OrdinalIgnoreCase)), "model comparison validation should reject identical baseline and candidate weights");

            request.CandidateWeightsPath = candidateWeights;
            File.Delete(Path.Combine(testLabelsPath, "heldout.txt"));
            File.WriteAllText(Path.Combine(testLabelsPath, "stale-heldout.txt"), "0 0.5 0.5 0.25 0.25" + Environment.NewLine);
            AssertTrue(service.ValidateRequest(request).Any(error => error.Contains("\uC815\uB2F5 \uB77C\uBCA8", StringComparison.OrdinalIgnoreCase)), "model comparison validation should reject an unlabeled held-out test split");
            File.Delete(Path.Combine(testLabelsPath, "stale-heldout.txt"));
            File.WriteAllText(Path.Combine(testLabelsPath, "heldout.txt"), "0 0.5 0.5 0.25 0.25" + Environment.NewLine);
            File.Delete(Path.Combine(data.TestImagesPath, "heldout.bmp"));
            AssertTrue(service.ValidateRequest(request).Any(error => error.Contains("test", StringComparison.OrdinalIgnoreCase) && error.Contains("\uC774\uBBF8\uC9C0\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4", StringComparison.OrdinalIgnoreCase)), "model comparison validation should reject an empty held-out test split");

            string validLabelsPath = Path.Combine(Path.GetDirectoryName(data.ValidImagesPath) ?? outputRoot, "labels");
            Directory.CreateDirectory(validLabelsPath);
            File.WriteAllBytes(Path.Combine(data.ValidImagesPath, "validation.bmp"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(validLabelsPath, "validation.txt"), "0 0.5 0.5 0.25 0.25" + Environment.NewLine);
            WpfModelComparisonRunRequest validationFallbackRequest = service.BuildYoloV5YoloV8DetectionRequest(data);
            AssertEqual("val", validationFallbackRequest.Task);
            AssertEqual(0, service.ValidateRequest(validationFallbackRequest).Count);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }
}
