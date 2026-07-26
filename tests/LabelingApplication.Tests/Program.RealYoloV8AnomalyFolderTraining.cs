using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static class RealYoloAnomalyFolderTrainingSmokeTests
{
    internal static int RunRealYoloV8AnomalyFolderTraining(string[] args)
    {
        string artifactRoot = string.Empty;
        Process pythonProcess = null;
        CCommunicationLearning communication = null;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        try
        {
            string root = FindRepositoryRoot();
            string sourceRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--source-root",
                @"D:\circular_defect_labeling_dataset_v1\images"));
            string modelName = GetArgumentValue(args, "--engine", "yolov8").Trim().ToLowerInvariant();
            AssertTrue(
                modelName is "yolov8" or "yolo11",
                "anomaly classification training engine must be yolov8 or yolo11: " + modelName);
            string modelDisplayName = modelName == "yolo11" ? "YOLO11" : "YOLOv8";
            string yoloRoot = Path.GetFullPath(GetArgumentValue(args, "--yolov8-root", @"C:\Git\yolov8"));
            string pythonPath = Path.Combine(yoloRoot, ".venv", "Scripts", "python.exe");
            string clientScriptPath = modelName == "yolo11"
                ? Path.Combine(root, "Runtime", "Python", "openvisionlab_ultralytics_worker.py")
                : Path.Combine(yoloRoot, "labeling_tcp_client.py");
            string seedWeightsPath = Path.Combine(yoloRoot, modelName == "yolo11" ? "yolo11n-cls.pt" : "yolov8n-cls.pt");
            int epochCount = GetPositiveArgument(args, "--epochs", 20);
            int imageSize = GetPositiveArgument(args, "--image-size", 128);
            int batchSize = GetPositiveArgument(args, "--batch", 4);
            int timeoutSeconds = GetPositiveArgument(args, "--timeout-seconds", 900);
            string runName = GetArgumentValue(
                args,
                "--run-name",
                "openvisionlab-" + modelName + "-classify-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            string runDirectory = Path.Combine(yoloRoot, "runs", "classify", runName);
            artifactRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--artifact-root",
                Path.Combine(root, "artifacts", "real-" + modelName + "-anomaly-folder-training", DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture))));

            AssertTrue(Directory.Exists(sourceRoot), "anomaly source image root was not found: " + sourceRoot);
            AssertTrue(Directory.Exists(yoloRoot), "Ultralytics root was not found: " + yoloRoot);
            AssertTrue(File.Exists(pythonPath), "Ultralytics Python was not found: " + pythonPath);
            AssertTrue(File.Exists(clientScriptPath), modelDisplayName + " TCP worker was not found: " + clientScriptPath);
            AssertTrue(File.Exists(seedWeightsPath), modelDisplayName + " classification seed was not found: " + seedWeightsPath);
            AssertTrue(!string.IsNullOrWhiteSpace(runName) && string.Equals(runName, Path.GetFileName(runName), StringComparison.Ordinal), "run name must be a single folder name: " + runName);
            AssertTrue(!Directory.Exists(runDirectory), "refusing to overwrite an existing classification run: " + runDirectory);
            AssertTrue(!Directory.Exists(artifactRoot), "artifact root already exists: " + artifactRoot);

            Directory.CreateDirectory(artifactRoot);
            ExternalYoloSourceTreeSnapshot sourceTreeBefore = CaptureExternalYoloSourceTree(sourceRoot);
            File.WriteAllLines(Path.Combine(artifactRoot, "source-tree-before.tsv"), sourceTreeBefore.ManifestLines);
            string outputRoot = Path.Combine(artifactRoot, "app-output");
            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.AnomalyDetection;
            data.ProjectSettings.PythonModel.ModelEngine = modelName == "yolo11"
                ? PythonModelSettings.EngineYolo11
                : PythonModelSettings.EngineYoloV8;
            data.ProjectSettings.PythonModel.ProjectRootPath = yoloRoot;
            data.ProjectSettings.PythonModel.PythonExecutablePath = pythonPath;
            data.ProjectSettings.PythonModel.ClientScriptPath = clientScriptPath;
            data.ProjectSettings.PythonModel.WeightsPath = seedWeightsPath;
            data.ProjectSettings.PythonModel.ImageRootPath = sourceRoot;
            data.ProjectSettings.PythonModel.AutoStartClient = false;
            data.ProjectSettings.YoloDataset.ValidationPercent = 20;
            data.ProjectSettings.YoloDataset.TestPercent = 10;
            data.ProjectSettings.YoloDataset.SplitSeed = 17;
            data.TranningParam.imageSize = imageSize;
            data.TranningParam.batch = batchSize;
            data.TranningParam.epoch = epochCount;
            data.ProjectSettings.AnomalyClassification.NormalClassNames.Clear();
            data.ProjectSettings.AnomalyClassification.AbnormalClassNames.Clear();
            data.ProjectSettings.AnomalyClassification.NormalClassNames.Add("normal");
            data.ProjectSettings.AnomalyClassification.AbnormalClassNames.Add("abnormal");

            string[] sourceImages = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                .Where(IsAnomalyTrainingImage)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            AssertTrue(sourceImages.Length > 0, "anomaly source image root contains no supported image files: " + sourceRoot);

            var reviewStatus = new AnomalyImageReviewStatusService();
            reviewStatus.LoadReviewStatus(data, sourceImages);
            AnomalyImageReviewFolderImportResult import = reviewStatus.ImportUnreviewedStatesFromParentFolders();
            reviewStatus.SaveReviewStatus(data);
            AssertTrue(import.NormalImageCount > 0, "OK parent folder did not import normal anomaly review states");
            AssertTrue(import.AbnormalImageCount > 0, "NG parent folder did not import abnormal anomaly review states");

            AnomalyClassificationTrainingReadinessReport readiness = AnomalyClassificationTrainingReadinessService.Build(data);
            AssertTrue(readiness.IsReady, "anomaly classification readiness failed: " + string.Join("; ", readiness.Errors));
            AssertTrue(readiness.NormalImageCount > 0 && readiness.AbnormalImageCount > 0, "anomaly source needs both normal and abnormal images");
            AssertTrue(readiness.TrainNormalImageCount > 0 && readiness.TrainAbnormalImageCount > 0, "anomaly source needs both normal and abnormal train images");

            int port = GetAvailableTcpPort();
            communication = new CCommunicationLearning(startListen: false, port: port);
            AssertTrue(communication.Start(), modelDisplayName + " anomaly training TCP listener did not start");
            pythonProcess = StartRealYoloTrainingClient(
                pythonPath,
                clientScriptPath,
                yoloRoot,
                sourceRoot,
                seedWeightsPath,
                port,
                imageSize,
                stdout,
                stderr,
                modelName);
            AssertTrue(
                WaitUntil(() => communication.GetStatusSnapshot().IsClientConnected, TimeSpan.FromSeconds(30)),
                BuildRealYoloSmokeFailure(modelDisplayName + " anomaly training client did not connect", stdout, stderr));

            var workflow = new YoloTrainingWorkflowService();
            AssertTrue(
                workflow.TryStartTraining(data, communication, runName),
                BuildRealYoloSmokeFailure(modelDisplayName + " anomaly training request was not sent: " + workflow.LastPreparationFailureMessage, stdout, stderr));

            string classificationRoot = Path.Combine(outputRoot, AnomalyClassificationDatasetExportService.DefaultFolderName);
            AssertTrue(HasAnomalyTrainingImages(classificationRoot, "train", "normal"), "application anomaly export did not write train/normal images");
            AssertTrue(HasAnomalyTrainingImages(classificationRoot, "train", "abnormal"), "application anomaly export did not write train/abnormal images");
            AssertTrue(HasAnomalyTrainingImages(classificationRoot, "valid", "normal"), "application anomaly export did not write valid/normal images");
            AssertTrue(HasAnomalyTrainingImages(classificationRoot, "test", "abnormal"), "application anomaly export did not write test/abnormal images");

            bool terminal = WaitUntil(
                () => IsAnomalyTrainingTerminal(communication.GetStatusSnapshot().LastTrainingState),
                TimeSpan.FromSeconds(timeoutSeconds));
            PythonCommunicationStatus finalStatus = communication.GetStatusSnapshot();
            AssertTrue(
                terminal && string.Equals(finalStatus.LastTrainingState, "completed", StringComparison.OrdinalIgnoreCase),
                BuildRealYoloSmokeFailure(
                    modelDisplayName + " anomaly training did not complete. State=" + finalStatus.LastTrainingState + " Message=" + finalStatus.LastTrainingMessage,
                    stdout,
                    stderr));

            string bestWeightsPath = Path.Combine(runDirectory, "weights", "best.pt");
            AssertTrue(File.Exists(bestWeightsPath), modelDisplayName + " anomaly training completed without best.pt: " + bestWeightsPath);
            string copiedWeightsPath = Path.Combine(artifactRoot, "best.pt");
            File.Copy(bestWeightsPath, copiedWeightsPath, overwrite: false);
            ExternalYoloSourceTreeSnapshot sourceTreeAfter = CaptureExternalYoloSourceTree(sourceRoot);
            File.WriteAllLines(Path.Combine(artifactRoot, "source-tree-after.tsv"), sourceTreeAfter.ManifestLines);
            AssertEqual(sourceTreeBefore.FileCount, sourceTreeAfter.FileCount);
            AssertEqual(sourceTreeBefore.TreeSha256, sourceTreeAfter.TreeSha256);

            string summaryPath = Path.Combine(artifactRoot, "summary.txt");
            File.WriteAllLines(summaryPath, new[]
            {
                "REAL_ULTRALYTICS_ANOMALY_FOLDER_TRAINING completed.",
                "model=" + modelName,
                "sourceRoot=" + sourceRoot,
                "sourceImageCount=" + sourceImages.Length.ToString(CultureInfo.InvariantCulture),
                "sourceTreeFileCountBefore=" + sourceTreeBefore.FileCount.ToString(CultureInfo.InvariantCulture),
                "sourceTreeSha256Before=" + sourceTreeBefore.TreeSha256,
                "sourceTreeFileCountAfter=" + sourceTreeAfter.FileCount.ToString(CultureInfo.InvariantCulture),
                "sourceTreeSha256After=" + sourceTreeAfter.TreeSha256,
                "folderImportNormal=" + import.NormalImageCount.ToString(CultureInfo.InvariantCulture),
                "folderImportAbnormal=" + import.AbnormalImageCount.ToString(CultureInfo.InvariantCulture),
                "classificationRoot=" + classificationRoot,
                "normalImageCount=" + readiness.NormalImageCount.ToString(CultureInfo.InvariantCulture),
                "abnormalImageCount=" + readiness.AbnormalImageCount.ToString(CultureInfo.InvariantCulture),
                "trainNormalImageCount=" + readiness.TrainNormalImageCount.ToString(CultureInfo.InvariantCulture),
                "trainAbnormalImageCount=" + readiness.TrainAbnormalImageCount.ToString(CultureInfo.InvariantCulture),
                "epochs=" + epochCount.ToString(CultureInfo.InvariantCulture),
                "imageSize=" + imageSize.ToString(CultureInfo.InvariantCulture),
                "batch=" + batchSize.ToString(CultureInfo.InvariantCulture),
                "runName=" + runName,
                "workerTrainingState=" + finalStatus.LastTrainingState,
                "workerTrainingMessage=" + finalStatus.LastTrainingMessage,
                "worker=" + clientScriptPath,
                "workerSha256=" + ComputeFileSha256(clientScriptPath),
                "seedWeights=" + seedWeightsPath,
                "seedWeightsSha256=" + ComputeFileSha256(seedWeightsPath),
                "bestWeights=" + bestWeightsPath,
                "bestWeightsSha256=" + ComputeFileSha256(bestWeightsPath),
                "copiedWeights=" + copiedWeightsPath
            });
            WriteRealYoloProcessLog(artifactRoot, stdout, stderr);

            Console.WriteLine("REAL_ULTRALYTICS_ANOMALY_FOLDER_TRAINING weights=" + bestWeightsPath);
            Console.WriteLine("REAL_ULTRALYTICS_ANOMALY_FOLDER_TRAINING summary=" + summaryPath);
            return 0;
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(artifactRoot))
            {
                Directory.CreateDirectory(artifactRoot);
                File.WriteAllText(Path.Combine(artifactRoot, "failure.txt"), ex.ToString());
            }

            Console.Error.WriteLine("FAIL REAL YOLOv8 anomaly folder training: " + ex.Message);
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            communication?.Close();
            StopRealYoloClient(pythonProcess);
            WriteRealYoloProcessLog(artifactRoot, stdout, stderr);
            communication?.Dispose();
        }
    }

    private static bool IsAnomalyTrainingTerminal(string state)
    {
        return string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state, "failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state, "canceled", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAnomalyTrainingImage(string path)
    {
        string extension = Path.GetExtension(path) ?? string.Empty;
        return extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnomalyTrainingImages(string datasetRoot, string split, string className)
    {
        string directory = Path.Combine(datasetRoot, split, className);
        return Directory.Exists(directory) && Directory.EnumerateFiles(directory).Any(IsAnomalyTrainingImage);
    }

}
