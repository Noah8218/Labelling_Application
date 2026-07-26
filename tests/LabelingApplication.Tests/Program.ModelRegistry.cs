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

internal static class ModelRegistryTests
{
    internal static void TestModelRegistryServicePersistence()
    {
        string root = CreateTempRoot();
        string recipeName = "codex_model_registry_" + Guid.NewGuid().ToString("N");
        string recipeDirectory = Path.Combine(AppContext.BaseDirectory, "RECIPE", recipeName);
        string configPath = Path.Combine(recipeDirectory, "VISION.xml");

        try
        {
            string outputRoot = Path.Combine(root, "dataset");
            string projectRoot = Path.Combine(root, "yolov5");
            string baselineWeightsPath = Path.Combine(projectRoot, "best.pt");
            string candidateWeightsPath = Path.Combine(projectRoot, "runs", "train", "exp42", "weights", "best.pt");
            Directory.CreateDirectory(Path.GetDirectoryName(candidateWeightsPath));
            File.WriteAllText(baselineWeightsPath, "baseline");
            File.WriteAllText(candidateWeightsPath, "candidate");

            var data = new CData();
            data.ConfigureOutputRoot(outputRoot);
            data.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
            data.ProjectSettings.PythonModel.ModelEngine = PythonModelSettings.EngineYoloV5;
            data.ProjectSettings.PythonModel.ProjectRootPath = projectRoot;
            data.ProjectSettings.PythonModel.WeightsPath = candidateWeightsPath;
            data.ProjectSettings.TrainingGuide.LastTrainingState = "completed";
            data.ProjectSettings.TrainingGuide.LastTrainingProgressPercent = 100;
            data.ProjectSettings.TrainingGuide.LastTrainingMessage = "done";

            ModelCandidate staged = ModelRegistryService.RecordTrainingCandidate(
                data.ProjectSettings.ModelRegistry,
                data.ProjectSettings.PythonModel,
                data.ProjectSettings.DatasetPurpose,
                outputRoot,
                candidateWeightsPath,
                baselineWeightsPath,
                "mAP50-95 +7.0%p",
                "completed",
                100,
                "done",
                savedToRecipe: false);

            AssertTrue(staged != null, "model registry should create a staged model candidate");
            AssertTrue(!staged.SavedToRecipe, "staged candidate should start as not saved to recipe");
            AssertEqual(1, data.ProjectSettings.ModelRegistry.Profiles.Count);
            AssertEqual(1, data.ProjectSettings.ModelRegistry.TrainingRuns.Count);
            AssertTrue(data.ProjectSettings.ModelRegistry.Candidates.Count >= 2, "registry should keep the baseline inspection model and the staged candidate");
            ModelCandidate currentBeforeSave = ModelRegistryService.FindCurrentInspectionModel(data.ProjectSettings.ModelRegistry);
            AssertTrue(currentBeforeSave != null && string.Equals(currentBeforeSave.WeightsPath, Path.GetFullPath(baselineWeightsPath), StringComparison.OrdinalIgnoreCase),
                "unsaved candidate should not replace the current inspection model");

            ModelCandidate rejected = ModelRegistryService.RecordCandidateDecision(
                data.ProjectSettings.ModelRegistry,
                data.ProjectSettings.PythonModel,
                data.ProjectSettings.DatasetPurpose,
                outputRoot,
                candidateWeightsPath,
                baselineWeightsPath,
                "mAP50-95 +7.0%p",
                ModelRegistryService.CandidateDecisionRejected,
                "operator kept baseline",
                savedToRecipe: false);

            AssertEqual(ModelRegistryService.CandidateDecisionRejected, rejected.Decision);
            AssertEqual(1, data.ProjectSettings.ModelRegistry.CandidateDecisions.Count);
            ModelCandidate currentAfterReject = ModelRegistryService.FindCurrentInspectionModel(data.ProjectSettings.ModelRegistry);
            AssertTrue(currentAfterReject != null && string.Equals(currentAfterReject.WeightsPath, Path.GetFullPath(baselineWeightsPath), StringComparison.OrdinalIgnoreCase),
                "rejected candidate should keep the baseline inspection model current");

            ModelCandidate adopted = ModelRegistryService.RecordCandidateDecision(
                data.ProjectSettings.ModelRegistry,
                data.ProjectSettings.PythonModel,
                data.ProjectSettings.DatasetPurpose,
                outputRoot,
                candidateWeightsPath,
                baselineWeightsPath,
                "mAP50-95 +7.0%p",
                ModelRegistryService.CandidateDecisionAdopted,
                "operator saved candidate",
                savedToRecipe: true);

            AssertTrue(adopted.SavedToRecipe, "adopted candidate should be marked as saved to recipe");
            AssertTrue(adopted.IsCurrentInspectionModel, "adopted candidate should become the current inspection model");
            AssertEqual(ModelRegistryService.CandidateDecisionAdopted, adopted.Decision);
            AssertEqual(adopted.CandidateId, data.ProjectSettings.ModelRegistry.CurrentInspectionModelId);
            AssertEqual(2, data.ProjectSettings.ModelRegistry.CandidateDecisions.Count);
            AssertEqual(1, data.ProjectSettings.ModelRegistry.AdoptionHistory.Count);
            AssertTrue(data.ProjectSettings.ModelRegistry.AdoptionHistory[0].PreviousWeightsPath.Contains("best.pt", StringComparison.OrdinalIgnoreCase),
                "adoption history should preserve the previous inspection model path");

            data.SaveConfig(recipeName);
            AssertTrue(File.Exists(configPath), $"model registry recipe config was not saved: {configPath}");
            string xml = File.ReadAllText(configPath);
            AssertTrue(xml.Contains("<ModelRegistry>", StringComparison.Ordinal), "model registry root should be serialized");
            AssertTrue(xml.Contains("<ModelProfile>", StringComparison.Ordinal), "model profile should be serialized");
            AssertTrue(xml.Contains("<TrainingRun>", StringComparison.Ordinal), "training run should be serialized");
            AssertTrue(xml.Contains("<ModelCandidate>", StringComparison.Ordinal), "model candidate should be serialized");
            AssertTrue(xml.Contains("<ModelCandidateDecision>", StringComparison.Ordinal), "model candidate decision should be serialized");
            AssertTrue(xml.Contains("<InspectionModelAdoption>", StringComparison.Ordinal), "inspection-model adoption should be serialized");

            CData loaded = new CData().LoadConfig(recipeName);
            loaded.ProjectSettings.EnsureDefaults();
            AssertEqual(1, loaded.ProjectSettings.ModelRegistry.Profiles.Count);
            AssertEqual(1, loaded.ProjectSettings.ModelRegistry.TrainingRuns.Count);
            AssertEqual(2, loaded.ProjectSettings.ModelRegistry.CandidateDecisions.Count);
            AssertEqual(1, loaded.ProjectSettings.ModelRegistry.AdoptionHistory.Count);
            ModelCandidate loadedCurrent = ModelRegistryService.FindCurrentInspectionModel(loaded.ProjectSettings.ModelRegistry);
            AssertTrue(loadedCurrent != null, "loaded registry should resolve the current inspection model");
            AssertTrue(loadedCurrent.WeightsPath.Contains("exp42", StringComparison.OrdinalIgnoreCase), "loaded current inspection model should be the adopted training candidate");
            ModelCandidateDecision latestDecision = ModelRegistryService.FindLatestCandidateDecision(loaded.ProjectSettings.ModelRegistry);
            AssertTrue(latestDecision != null && string.Equals(latestDecision.Decision, ModelRegistryService.CandidateDecisionAdopted, StringComparison.Ordinal),
                "loaded registry should expose the latest saved/rejected candidate decision");

            WpfModelRegistryPresentation presentation = WpfModelRegistryPresentationService.Build(
                loaded.ProjectSettings.PythonModel,
                new WpfTrainingWeightsComparison
                {
                    LatestWeightsPath = candidateWeightsPath,
                    CurrentWeightsPath = candidateWeightsPath,
                    MetricsStatusText = "mAP50-95 +7.0%p, mAP50 +5.0%p, precision +3.0%p, recall +2.0%p, box loss -0.0123"
                },
                loaded.ProjectSettings.TrainingGuide,
                loaded.ProjectSettings.ModelRegistry,
                hasPendingInspectionModelSelection: false);
            AssertTrue(presentation.ProfileText.Contains("\uB4F1\uB85D \uD504\uB85C\uD544", StringComparison.Ordinal), "presentation should expose persisted profile count");
            AssertTrue(presentation.CandidateModelText.Contains("recipe", StringComparison.Ordinal), "presentation should show candidate recipe state");
            AssertTrue(presentation.CandidateModelText.Contains("P/R", StringComparison.Ordinal), "presentation should compact precision/recall for first-visible candidate review");
            AssertTrue(!presentation.CandidateModelText.Contains("box loss", StringComparison.OrdinalIgnoreCase), "presentation should keep detailed loss metrics out of the first-visible candidate row");
            AssertTrue(presentation.CandidateModelText.Contains("\uACB0\uC815", StringComparison.Ordinal), "presentation should expose the latest candidate decision");
            AssertTrue(presentation.ActionText.Contains("\uACB0\uC815 \uC774\uB825", StringComparison.Ordinal), "presentation should expose saved/rejected decision history count");
            AssertTrue(presentation.ActionText.Contains("\uCC44\uD0DD \uC774\uB825", StringComparison.Ordinal), "presentation should expose adoption-history count");

            string yoloV8Root = Path.Combine(root, "yolov8");
            string yoloV8WeightsPath = Path.Combine(yoloV8Root, "runs", "detect", "test01", "weights", "best.pt");
            Directory.CreateDirectory(Path.GetDirectoryName(yoloV8WeightsPath));
            File.WriteAllText(yoloV8WeightsPath, "yolov8 candidate");
            var configuredRegistry = new ModelRegistrySettings();
            var configuredYoloV5 = new PythonModelSettings
            {
                ModelEngine = PythonModelSettings.EngineYoloV5,
                ProjectRootPath = projectRoot,
                WeightsPath = candidateWeightsPath
            };
            var configuredYoloV8 = new PythonModelSettings
            {
                ModelEngine = PythonModelSettings.EngineYoloV8,
                ProjectRootPath = yoloV8Root,
                WeightsPath = yoloV8WeightsPath
            };

            ModelRegistryService.RecordConfiguredInspectionModel(
                configuredRegistry,
                configuredYoloV5,
                LabelingDatasetPurpose.ObjectDetection);
            ModelCandidate configuredCurrent = ModelRegistryService.RecordConfiguredInspectionModel(
                configuredRegistry,
                configuredYoloV8,
                LabelingDatasetPurpose.ObjectDetection,
                candidateWeightsPath);

            AssertEqual(2, configuredRegistry.Profiles.Count);
            AssertEqual(0, configuredRegistry.TrainingRuns.Count);
            AssertEqual(2, configuredRegistry.Candidates.Count);
            AssertEqual(configuredCurrent.CandidateId, configuredRegistry.CurrentInspectionModelId);
            AssertTrue(configuredRegistry.Candidates.Any(item => string.Equals(item.WeightsPath, Path.GetFullPath(candidateWeightsPath), StringComparison.OrdinalIgnoreCase)),
                "saving a YOLOv8 profile should preserve the configured YOLOv5 inspection model");
            int configuredDecisionCount = configuredRegistry.CandidateDecisions.Count;
            int configuredAdoptionCount = configuredRegistry.AdoptionHistory.Count;
            ModelRegistryService.RecordConfiguredInspectionModel(
                configuredRegistry,
                configuredYoloV8,
                LabelingDatasetPurpose.ObjectDetection,
                yoloV8WeightsPath);
            AssertEqual(configuredDecisionCount, configuredRegistry.CandidateDecisions.Count);
            AssertEqual(configuredAdoptionCount, configuredRegistry.AdoptionHistory.Count);
            AssertEqual(Path.GetFullPath(candidateWeightsPath), configuredCurrent.BaselineWeightsPath);

            var configuredData = new CData();
            configuredData.ConfigureOutputRoot(outputRoot);
            configuredData.ProjectSettings.DatasetPurpose = LabelingDatasetPurpose.ObjectDetection;
            configuredData.ProjectSettings.PythonModel = configuredYoloV8;
            configuredData.ProjectSettings.ModelRegistry = configuredRegistry;
            WpfModelComparisonRunRequest configuredRequest = new WpfModelComparisonRunService(root)
                .BuildYoloV5YoloV8DetectionRequest(configuredData, task: "val");
            AssertEqual(Path.GetFullPath(candidateWeightsPath), configuredRequest.BaselineWeightsPath);
            AssertEqual(Path.GetFullPath(yoloV8WeightsPath), configuredRequest.CandidateWeightsPath);
        }
        finally
        {
            DeleteTempRoot(root);
            if (Directory.Exists(recipeDirectory))
            {
                Directory.Delete(recipeDirectory, recursive: true);
            }
        }
    }
}
