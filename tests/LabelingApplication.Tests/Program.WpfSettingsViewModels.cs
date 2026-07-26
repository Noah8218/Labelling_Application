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

internal static class WpfSettingsViewModelTests
{
    internal static void TestWpfSettingsViewModelsRoundTrip()
    {
        var yoloSettings = new PythonModelSettings
        {
            PythonExecutablePath = @"C:\Python311\python.exe",
            ModelEngine = PythonModelSettings.EngineYoloV5,
            ProjectRootPath = @"C:\Git\yolov5",
            ClientScriptPath = @"C:\Git\yolov5\labelling_tcp_client.py",
            WeightsPath = @"C:\Git\yolov5\best.pt",
            ImageRootPath = @"C:\Git\yolov5\data\train\images",
            MinimumDetectionConfidence = 0.25F,
            MaximumDetectionCandidates = 20,
            InferenceImageSize = 320,
            DetectionTimeoutSeconds = 30,
            AutoStartClient = true
        };
        var yoloViewModel = new WpfYoloModelSettingsPanelViewModel();
        yoloViewModel.LoadFrom(yoloSettings);
        AssertEqual(@"C:\Git\yolov5", yoloViewModel.ProjectRootPath);
        AssertEqual(PythonModelSettings.EngineYoloV5, yoloViewModel.SelectedModelEngine);
        AssertTrue(yoloViewModel.ModelEngineOptions.Contains(PythonModelSettings.EngineYoloV8), "YOLOv8 should be selectable without changing the current default engine");
        AssertTrue(yoloViewModel.ModelEngineOptions.Contains(PythonModelSettings.EngineYolo11), "YOLO11 should be selectable before the runtime adapter is installed");

        yoloViewModel.MinimumConfidenceText = "0.85";
        yoloViewModel.SelectedModelEngine = PythonModelSettings.EngineYolo11;
        yoloViewModel.MaximumCandidatesText = "12";
        yoloViewModel.InferenceImageSizeText = "416";
        yoloViewModel.TimeoutSecondsText = "42";
        yoloViewModel.AutoStartClient = false;
        yoloViewModel.WeightsPath = @"C:\model\custom.pt";
        yoloViewModel.ApplyTo(yoloSettings);

        AssertEqual(0.85F, yoloSettings.MinimumDetectionConfidence);
        AssertEqual(PythonModelSettings.EngineYolo11, yoloSettings.ModelEngine);
        AssertEqual("yolo11", yoloSettings.GetProtocolModelName());
        AssertEqual(12, yoloSettings.MaximumDetectionCandidates);
        AssertEqual(416, yoloSettings.InferenceImageSize);
        AssertEqual(42, yoloSettings.DetectionTimeoutSeconds);
        AssertTrue(!yoloSettings.AutoStartClient, "YOLO auto-start value was not applied from the view model");
        AssertEqual(@"C:\model\custom.pt", yoloSettings.WeightsPath);

        var anomalySettings = new AnomalyClassificationSettings
        {
            NormalClassNames = new List<string> { "OK" },
            AbnormalClassNames = new List<string> { "NG" },
            MinimumConfidence = 0.66D
        };
        yoloViewModel.LoadFrom(yoloSettings, anomalySettings);
        AssertEqual("OK", yoloViewModel.AnomalyNormalClassNamesText);
        AssertEqual("NG", yoloViewModel.AnomalyAbnormalClassNamesText);
        AssertEqual("0.66", yoloViewModel.AnomalyMinimumConfidenceText);
        AssertTrue(yoloViewModel.AnomalyMappingSummaryText.Contains("\uC815\uC0C1 1\uAC1C", StringComparison.Ordinal), "YOLO model settings should summarize configured normal anomaly classes");
        AssertTrue(yoloViewModel.AnomalyMappingSummaryText.Contains("\uC774\uC0C1 1\uAC1C", StringComparison.Ordinal), "YOLO model settings should summarize configured abnormal anomaly classes");
        yoloViewModel.AnomalyNormalClassNamesText = "OK, Good, OK";
        yoloViewModel.AnomalyAbnormalClassNamesText = "NG;Defect";
        yoloViewModel.AnomalyMinimumConfidenceText = "0.72";
        yoloViewModel.ApplyTo(anomalySettings);
        AssertEqual(2, anomalySettings.NormalClassNames.Count);
        AssertEqual("OK", anomalySettings.NormalClassNames[0]);
        AssertEqual("Good", anomalySettings.NormalClassNames[1]);
        AssertEqual(2, anomalySettings.AbnormalClassNames.Count);
        AssertEqual("NG", anomalySettings.AbnormalClassNames[0]);
        AssertEqual("Defect", anomalySettings.AbnormalClassNames[1]);
        AssertEqual(0.72D, anomalySettings.MinimumConfidence);

        yoloViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(yoloViewModel.IsBrowsePythonEnabled, "YOLO python browse should be enabled while idle");
        AssertTrue(yoloViewModel.IsBrowseProjectRootEnabled, "YOLO project browse should be enabled while idle");
        AssertTrue(yoloViewModel.IsBrowseClientScriptEnabled, "YOLO client browse should be enabled while idle");
        AssertTrue(yoloViewModel.IsBrowseWeightsEnabled, "YOLO weights browse should be enabled while idle");
        AssertTrue(yoloViewModel.IsBrowseImageRootEnabled, "YOLO image-root browse should be enabled while idle");
        AssertTrue(yoloViewModel.IsSaveSettingsEnabled, "YOLO settings save should be enabled while idle");
        AssertTrue(yoloViewModel.IsResetSettingsEnabled, "YOLO settings reset should be enabled while idle");
        AssertTrue(yoloViewModel.IsRuntimeProfileActionEnabled, "YOLO runtime profile actions should be enabled while idle");
        yoloViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: true,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!yoloViewModel.IsBrowsePythonEnabled, "YOLO python browse should disable while busy");
        AssertTrue(!yoloViewModel.IsBrowseProjectRootEnabled, "YOLO project browse should disable while busy");
        AssertTrue(!yoloViewModel.IsBrowseClientScriptEnabled, "YOLO client browse should disable while busy");
        AssertTrue(!yoloViewModel.IsBrowseWeightsEnabled, "YOLO weights browse should disable while busy");
        AssertTrue(!yoloViewModel.IsBrowseImageRootEnabled, "YOLO image-root browse should disable while busy");
        AssertTrue(!yoloViewModel.IsSaveSettingsEnabled, "YOLO settings save should disable while busy");
        AssertTrue(!yoloViewModel.IsResetSettingsEnabled, "YOLO settings reset should disable while busy");
        AssertTrue(!yoloViewModel.IsRuntimeProfileActionEnabled, "YOLO runtime profile actions should disable while busy");

        var trainingSettings = new TrainingSettings();
        var datasetSettings = new YoloDatasetSettings();
        var trainingParam = new CYolov5TrainingParam();
        var trainingViewModel = new WpfTrainingSettingsPanelViewModel();
        trainingViewModel.LoadFrom(trainingSettings, datasetSettings);
        AssertTrue(trainingViewModel.TrainingRecommendationText.Contains("이미지 320", StringComparison.Ordinal), "training recommendation should explain the fast image-size preset");
        AssertTrue(trainingViewModel.ImageSizeGuideText.Contains("추천 320", StringComparison.Ordinal), "training image-size guide should include a concrete recommendation");
        AssertTrue(trainingViewModel.BatchGuideText.Contains("추천 4", StringComparison.Ordinal), "training batch guide should include a concrete recommendation");
        AssertTrue(trainingViewModel.ApplyFastRecommendationCommand.CanExecute(null), "training fast recommendation command should be executable while idle");
        trainingViewModel.ApplyFastRecommendationCommand.Execute(null);
        AssertEqual("320", trainingViewModel.ImageSizeText);
        AssertEqual("4", trainingViewModel.BatchText);
        AssertEqual("50", trainingViewModel.EpochText);
        AssertEqual(CYolov5TrainingParam.Cfg.yolov5s.ToString(), trainingViewModel.Cfg);
        AssertEqual(CYolov5TrainingParam.Weight.yolov5s.ToString(), trainingViewModel.Weight);
        AssertEqual("20", trainingViewModel.ValidationPercentText);
        AssertEqual("0", trainingViewModel.TestPercentText);
        AssertEqual("17", trainingViewModel.SplitSeedText);
        trainingViewModel.ImageSizeText = "640";
        trainingViewModel.BatchText = "8";
        trainingViewModel.EpochText = "12";
        trainingViewModel.Cfg = CYolov5TrainingParam.Cfg.yolov5m.ToString();
        trainingViewModel.Weight = CYolov5TrainingParam.Weight.yolov5m.ToString();
        trainingViewModel.ValidationPercentText = "25";
        trainingViewModel.TestPercentText = "10";
        trainingViewModel.SplitSeedText = "99";
        trainingViewModel.ApplyTo(trainingSettings, datasetSettings, trainingParam);

        AssertEqual(640, trainingSettings.ImageSize);
        AssertEqual(8, trainingSettings.Batch);
        AssertEqual(12, trainingSettings.Epoch);
        AssertEqual(CYolov5TrainingParam.Cfg.yolov5m.ToString(), trainingSettings.Cfg);
        AssertEqual(CYolov5TrainingParam.Weight.yolov5m.ToString(), trainingSettings.Weight);
        AssertEqual(25, datasetSettings.ValidationPercent);
        AssertEqual(10, datasetSettings.TestPercent);
        AssertEqual(99, datasetSettings.SplitSeed);
        AssertTrue(trainingViewModel.SplitPolicyHintText.Contains("\uAC80\uC99D", StringComparison.Ordinal), "training split policy hint should mention validation");
        AssertTrue(trainingViewModel.SplitPolicyHintText.Contains("\uCD5C\uC885 \uAC80\uC99D", StringComparison.Ordinal), "training split policy hint should mention final verification");
        AssertEqual(CYolov5TrainingParam.Cfg.yolov5m, trainingParam.cfg);
        AssertEqual(CYolov5TrainingParam.Weight.yolov5m, trainingParam.weight);
        trainingViewModel.SetTrainingReadinessText("Ready");
        trainingViewModel.SetTrainingProgress("Running", "Epoch 1/2", 42, isIndeterminate: true);
        trainingViewModel.SetTrainingStatusBrushes(System.Windows.Media.Brushes.LimeGreen, System.Windows.Media.Brushes.DodgerBlue);
        AssertEqual("Ready", trainingViewModel.TrainingReadinessText);
        AssertEqual("Running", trainingViewModel.TrainingProgressText);
        AssertEqual("Epoch 1/2", trainingViewModel.TrainingEpochStatusText);
        AssertEqual(42D, trainingViewModel.TrainingProgressValue);
        AssertEqual(true, trainingViewModel.TrainingProgressIsIndeterminate);
        AssertEqual(System.Windows.Media.Brushes.LimeGreen, trainingViewModel.TrainingReadinessForeground);
        AssertEqual(System.Windows.Media.Brushes.DodgerBlue, trainingViewModel.TrainingProgressForeground);
        trainingViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(trainingViewModel.IsApplyFastRecommendationEnabled, "training fast recommendation should be enabled while idle");
        AssertTrue(trainingViewModel.IsApplyFinalVerificationPresetEnabled, "training final-verification preset should be enabled while idle");
        AssertTrue(trainingViewModel.IsRefreshReadinessEnabled, "training refresh should be enabled while idle");
        AssertTrue(trainingViewModel.IsStartTrainingEnabled, "training start should be enabled while idle");
        AssertTrue(!trainingViewModel.IsStopTrainingEnabled, "training stop should be disabled while idle");
        trainingViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: true,
            isTrainingStopAvailable: true,
            hasCurrentRecipeName: true));
        AssertTrue(!trainingViewModel.IsApplyFastRecommendationEnabled, "training fast recommendation should disable while training is running");
        AssertTrue(!trainingViewModel.IsApplyFinalVerificationPresetEnabled, "training final-verification preset should disable while training is running");
        AssertTrue(!trainingViewModel.IsRefreshReadinessEnabled, "training refresh should disable while training is running");
        AssertTrue(!trainingViewModel.IsStartTrainingEnabled, "training start should disable while training is running");
        AssertTrue(trainingViewModel.IsStopTrainingEnabled, "training stop should enable while training is running");
        trainingViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: true,
            hasCurrentRecipeName: true));
        AssertTrue(!trainingViewModel.IsStartTrainingEnabled, "training start should stay disabled while worker status reports live training");
        AssertTrue(trainingViewModel.IsStopTrainingEnabled, "training stop should stay enabled while worker status reports live training");
        trainingViewModel.SetPostTrainingModelActionState(
            "\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378: best.pt",
            "\uC0C8 \uD559\uC2B5 \uBAA8\uB378 \uD6C4\uBCF4: exp7/best.pt",
            "\uBAA8\uB378 \uC801\uC6A9: \uC0C8 \uD6C4\uBCF4 \uAC80\uD1A0 \uD544\uC694",
            "\uB2E4\uC74C: \uBAA8\uB378 \uBE44\uAD50 \uD6C4 \uC800\uC7A5",
            "\uD6C4\uBCF4 \uAC80\uC99D",
            "\uD6C4\uBCF4 \uBAA8\uB378\uC758 \uCD5C\uC885 \uAC80\uC99D\uC744 \uC5FD\uB2C8\uB2E4.",
            canReview: true,
            "\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5",
            "\uC120\uD0DD\uD55C \uBAA8\uB378\uC744 recipe\uC5D0 \uC800\uC7A5\uD569\uB2C8\uB2E4.",
            canConfirm: true);
        trainingViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: false,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(trainingViewModel.IsReviewTrainedModelEnabled, "training settings candidate-review action should enable when commands are idle");
        AssertTrue(trainingViewModel.IsConfirmTrainedModelEnabled, "training settings save-model action should enable when a recipe can be saved");
        trainingViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: false,
            isYoloEnvironmentCommandRunning: true,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!trainingViewModel.IsReviewTrainedModelEnabled, "training settings candidate-review action should disable while another command is running");
        AssertTrue(!trainingViewModel.IsConfirmTrainedModelEnabled, "training settings save-model action should disable while another command is running");
        trainingViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: false,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: false));
        AssertTrue(trainingViewModel.IsReviewTrainedModelEnabled, "training settings candidate-review action should stay enabled without recipe save permission");
        AssertTrue(!trainingViewModel.IsConfirmTrainedModelEnabled, "training settings save-model action should require recipe save permission");

        var shellViewModel = new WpfLabelingShellViewModel();
        AssertTrue(shellViewModel.LoadedCommand.CanExecute(null), "shell loaded command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.ClosedCommand.CanExecute(null), "shell closed command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.PreviewKeyDownCommand.CanExecute(null), "shell preview-key command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.ToggleThemeCommand.CanExecute(null), "shell theme command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.LoadSampleCommand.CanExecute(null), "shell load-sample command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.AddSampleRoiCommand.CanExecute(null), "shell add-ROI command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.SaveAnnotationsCommand.CanExecute(null), "shell save command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.LabelingModeCommand.CanExecute(null), "shell labeling-mode command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.InferenceModeCommand.CanExecute(null), "shell inference-mode command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.DatasetHomeCommand.CanExecute(null), "shell dataset-home stage command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.LabelingWorkbenchCommand.CanExecute(null), "shell labeling-workbench stage command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.InferenceReviewCommand.CanExecute(null), "shell inference-review stage command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.TrainingModelCenterCommand.CanExecute(null), "shell training/model stage command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.ReviewCandidateModelCommand.CanExecute(null), "shell candidate-model review command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.CheckYoloCommand.CanExecute(null), "shell YOLO check command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.DetectCurrentImageCommand.CanExecute(null), "shell current-image detect command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.ChangeDatasetCommand.CanExecute(null), "shell dataset-change command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.OpenDatasetFolderCommand.CanExecute(null), "shell dataset-folder command should be exposed from the ViewModel");
        AssertTrue(shellViewModel.ChangeImageFolderCommand.CanExecute(null), "shell image-folder command should be exposed from the ViewModel");
        string modelRegistryTempRoot = CreateTempRoot();
        try
        {
            string currentWeightsPath = Path.Combine(modelRegistryTempRoot, "current.pt");
            string candidateWeightsPath = Path.Combine(modelRegistryTempRoot, "runs", "train", "exp7", "weights", "best.pt");
            string rejectedWeightsPath = Path.Combine(modelRegistryTempRoot, "runs", "train", "exp6", "weights", "best.pt");
            string pendingWeightsPath = Path.Combine(modelRegistryTempRoot, "runs", "train", "exp8", "weights", "best.pt");
            Directory.CreateDirectory(Path.GetDirectoryName(candidateWeightsPath));
            Directory.CreateDirectory(Path.GetDirectoryName(rejectedWeightsPath));
            Directory.CreateDirectory(Path.GetDirectoryName(pendingWeightsPath));
            File.WriteAllText(currentWeightsPath, "current");
            File.WriteAllText(candidateWeightsPath, "candidate");
            File.WriteAllText(rejectedWeightsPath, "rejected");
            File.WriteAllText(pendingWeightsPath, "pending");

            var modelRegistryPresentation = WpfModelRegistryPresentationService.Build(
                new PythonModelSettings
                {
                    ModelEngine = PythonModelSettings.EngineYoloV5,
                    WeightsPath = currentWeightsPath
                },
                new WpfTrainingWeightsComparison
                {
                    LatestWeightsPath = candidateWeightsPath,
                    CurrentWeightsPath = currentWeightsPath,
                    MetricsStatusText = "mAP50-95 +7.0%p, mAP50 +5.0%p, precision +3.0%p, recall +2.0%p, box loss -0.0123"
                },
                new YoloTrainingGuideHistory
                {
                    LastTrainingState = "completed",
                    LastTrainingProgressPercent = 100,
                    LastTrainingUpdateUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                },
                hasPendingInspectionModelSelection: false);

            AssertTrue(modelRegistryPresentation.ProfileText.Contains("\uBAA8\uB378 \uD504\uB85C\uD544", StringComparison.Ordinal), "model registry should name the model profile row");
            AssertTrue(modelRegistryPresentation.TrainingRunText.Contains("\uCD5C\uADFC \uD559\uC2B5 \uC2E4\uD589", StringComparison.Ordinal), "model registry should name the training-run row");
            AssertTrue(modelRegistryPresentation.CandidateModelText.Contains("\uBAA8\uB378 \uD6C4\uBCF4", StringComparison.Ordinal), "model registry should name the candidate-model row");
            string candidateDisplayName = $"exp7{Path.DirectorySeparatorChar}best.pt";
            AssertTrue(modelRegistryPresentation.CandidateModelText.Contains(candidateDisplayName, StringComparison.Ordinal), "model registry candidate row should keep the training run folder, not only best.pt");
            AssertTrue(modelRegistryPresentation.CandidateModelText.Contains("mAP50-95", StringComparison.Ordinal), "model registry should keep metric context with the candidate model");
            AssertTrue(modelRegistryPresentation.CandidateModelText.Contains("P/R", StringComparison.Ordinal), "model registry should compact precision/recall in the candidate-model row");
            AssertTrue(!modelRegistryPresentation.CandidateModelText.Contains("box loss", StringComparison.OrdinalIgnoreCase), "model registry candidate row should keep detailed loss metrics out of the first-visible row");
            AssertEqual("YOLOv5 / " + candidateDisplayName, WpfInferenceStatusPresentationService.BuildRuntimeModelLabel(new PythonModelSettings
            {
                ModelEngine = PythonModelSettings.EngineYoloV5,
                WeightsPath = candidateWeightsPath
            }));
            AssertTrue(modelRegistryPresentation.InspectionModelText.Contains("\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378", StringComparison.Ordinal), "model registry should name the current inspection model row");
            AssertTrue(modelRegistryPresentation.InspectionModelText.Contains("\uC2E4\uD589\uAE30", StringComparison.Ordinal)
                && modelRegistryPresentation.InspectionModelText.Contains("YOLOv5 repo", StringComparison.Ordinal),
                "model registry current inspection row should show the runtime used for inspection");
            AssertTrue(modelRegistryPresentation.SummaryPrimaryText.Contains("current.pt", StringComparison.Ordinal)
                && modelRegistryPresentation.SummaryPrimaryText.Contains(candidateDisplayName, StringComparison.Ordinal),
                "model registry compact summary should keep current and candidate models visible with the training run folder");
            AssertTrue(modelRegistryPresentation.SummarySecondaryText.Contains("YOLOv5", StringComparison.Ordinal)
                && modelRegistryPresentation.SummarySecondaryText.Contains("\uCD5C\uADFC \uD559\uC2B5", StringComparison.Ordinal),
                "model registry compact summary should keep profile and latest training context visible");
            AssertTrue(modelRegistryPresentation.SummarySecondaryText.Contains("YOLOv5 repo", StringComparison.Ordinal), "model registry compact summary should include the selected runtime family");
            string yolo11RuntimeSummary = WpfModelRegistryPresentationService.BuildSelectedRuntimeSummaryText(new PythonModelSettings
            {
                ModelEngine = PythonModelSettings.EngineYolo11
            });
            AssertTrue(yolo11RuntimeSummary.Contains("YOLO11", StringComparison.Ordinal)
                && yolo11RuntimeSummary.Contains("Ultralytics", StringComparison.Ordinal),
                "model registry runtime summary should identify YOLO11 as an Ultralytics runtime");
            string ultralyticsPythonPath = Path.Combine(modelRegistryTempRoot, "ultralytics-venv", "Scripts", "python.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(ultralyticsPythonPath));
            Directory.CreateDirectory(Path.Combine(modelRegistryTempRoot, "ultralytics-venv", "Lib", "site-packages", "ultralytics"));
            File.WriteAllText(ultralyticsPythonPath, "python");
            string yolo11WeightsPath = Path.Combine(modelRegistryTempRoot, "yolo11n.pt");
            File.WriteAllText(yolo11WeightsPath, "weights");
            string yolo11PartialReadySummary = WpfModelRegistryPresentationService.BuildSelectedRuntimeSummaryText(new PythonModelSettings
            {
                PythonExecutablePath = ultralyticsPythonPath,
                ModelEngine = PythonModelSettings.EngineYolo11,
                ProjectRootPath = PythonModelRuntimeBundledWorkerService.ResolveUltralyticsWorkerRootPath(),
                ClientScriptPath = PythonModelRuntimeBundledWorkerService.ResolveUltralyticsWorkerScriptPath(),
                WeightsPath = yolo11WeightsPath
            });
            AssertTrue(yolo11PartialReadySummary.Contains("\uD604\uC7AC \uAC80\uC0AC \uAC00\uB2A5", StringComparison.Ordinal)
                && yolo11PartialReadySummary.Contains("\uD559\uC2B5 \uBBF8\uC9C0\uC6D0", StringComparison.Ordinal),
                "model registry runtime summary should distinguish YOLO11 current-inspection readiness from unsupported training");
            shellViewModel.SetModelRegistryState(modelRegistryPresentation);
            AssertTrue(shellViewModel.ModelRegistrySummaryPrimaryText.Contains("current.pt", StringComparison.Ordinal)
                && shellViewModel.ModelRegistrySummaryPrimaryText.Contains("best.pt", StringComparison.Ordinal),
                "shell model-registry compact summary should include current and candidate model files");
            AssertTrue(shellViewModel.ModelRegistrySummarySecondaryText.Contains("YOLOv5", StringComparison.Ordinal), "shell model-registry compact summary should include the model adapter family");
            AssertTrue(shellViewModel.ModelRegistrySummarySecondaryText.Contains("YOLOv5 repo", StringComparison.Ordinal), "shell model-registry compact summary should include the runtime family");
            AssertTrue(shellViewModel.ModelRegistryProfileText.Contains("YOLOv5", StringComparison.Ordinal), "shell model-registry profile row should include the selected adapter profile");
            AssertTrue(shellViewModel.ModelRegistryTrainingRunText.Contains("100", StringComparison.Ordinal), "shell model-registry training row should include completed run progress");
            AssertTrue(shellViewModel.ModelRegistryCandidateModelText.Contains("mAP50-95", StringComparison.Ordinal), "shell model-registry candidate row should include metric context");
            AssertTrue(!shellViewModel.ModelRegistryCandidateModelText.Contains("box loss", StringComparison.OrdinalIgnoreCase), "shell model-registry candidate row should stay compact for first-visible review");
            AssertTrue(shellViewModel.ModelRegistryInspectionModelText.Contains("current.pt", StringComparison.Ordinal), "shell model-registry inspection row should include the current model file");
            AssertTrue(shellViewModel.ModelRegistryInspectionModelText.Contains("YOLOv5 repo", StringComparison.Ordinal), "shell model-registry inspection row should include the runtime family");

            var pendingRegistrySettings = new ModelRegistrySettings();
            var pendingRegistryPythonSettings = new PythonModelSettings
            {
                ModelEngine = PythonModelSettings.EngineYoloV5,
                ProjectRootPath = modelRegistryTempRoot,
                WeightsPath = candidateWeightsPath
            };
            ModelRegistryService.RecordTrainingCandidate(
                pendingRegistrySettings,
                pendingRegistryPythonSettings,
                LabelingDatasetPurpose.ObjectDetection,
                modelRegistryTempRoot,
                candidateWeightsPath,
                currentWeightsPath,
                "mAP50-95 +7.0%p",
                "completed",
                100,
                "training completed",
                savedToRecipe: false);
            WpfModelRegistryPresentation pendingModelSelectionPresentation = WpfModelRegistryPresentationService.Build(
                pendingRegistryPythonSettings,
                new WpfTrainingWeightsComparison
                {
                    LatestWeightsPath = candidateWeightsPath,
                    CurrentWeightsPath = currentWeightsPath,
                    MetricsStatusText = "mAP50-95 +7.0%p"
                },
                new YoloTrainingGuideHistory
                {
                    LastTrainingState = "completed",
                    LastTrainingProgressPercent = 100,
                    LastTrainingUpdateUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                },
                pendingRegistrySettings,
                hasPendingInspectionModelSelection: true);
            AssertTrue(pendingModelSelectionPresentation.InspectionModelText.Contains("current.pt", StringComparison.Ordinal),
                "pending model selection should keep the saved current inspection model visible");
            AssertTrue(!pendingModelSelectionPresentation.InspectionModelText.Contains("best.pt", StringComparison.Ordinal),
                "pending model selection should not rename the candidate as the current inspection model before recipe save");
            AssertTrue(pendingModelSelectionPresentation.CandidateModelText.Contains("best.pt", StringComparison.Ordinal),
                "pending model selection should keep the selected trained model in the candidate row");

            var registrySettings = new ModelRegistrySettings();
            var registryPythonSettings = new PythonModelSettings
            {
                ModelEngine = PythonModelSettings.EngineYoloV5,
                ProjectRootPath = modelRegistryTempRoot,
                WeightsPath = candidateWeightsPath
            };
            ModelRegistryService.RecordCandidateDecision(
                registrySettings,
                registryPythonSettings,
                LabelingDatasetPurpose.ObjectDetection,
                modelRegistryTempRoot,
                rejectedWeightsPath,
                currentWeightsPath,
                "mAP50-95 -2.0%p",
                ModelRegistryService.CandidateDecisionRejected,
                "operator rejected",
                savedToRecipe: false);
            ModelRegistryService.RecordCandidateDecision(
                registrySettings,
                registryPythonSettings,
                LabelingDatasetPurpose.ObjectDetection,
                modelRegistryTempRoot,
                candidateWeightsPath,
                currentWeightsPath,
                "mAP50-95 +7.0%p",
                ModelRegistryService.CandidateDecisionAdopted,
                "operator saved",
                savedToRecipe: true);
            ModelRegistryService.RecordTrainingCandidate(
                registrySettings,
                registryPythonSettings,
                LabelingDatasetPurpose.ObjectDetection,
                modelRegistryTempRoot,
                pendingWeightsPath,
                candidateWeightsPath,
                "mAP50-95 +1.0%p",
                "completed",
                100,
                "training completed",
                savedToRecipe: false);
            WpfModelRegistryPresentation registryHistoryPresentation = WpfModelRegistryPresentationService.Build(
                registryPythonSettings,
                new WpfTrainingWeightsComparison
                {
                    LatestWeightsPath = candidateWeightsPath,
                    CurrentWeightsPath = candidateWeightsPath,
                    MetricsStatusText = "mAP50-95 +7.0%p"
                },
                new YoloTrainingGuideHistory
                {
                    LastTrainingState = "completed",
                    LastTrainingProgressPercent = 100,
                    LastTrainingUpdateUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                },
                registrySettings,
                hasPendingInspectionModelSelection: false);
            AssertTrue(registryHistoryPresentation.HistoryItems.Count >= 2, "model registry presentation should expose multiple recent model-history rows");
            AssertTrue(registryHistoryPresentation.HistoryItems.Any(item => item.DecisionText.Contains("\uAC70\uC808", StringComparison.Ordinal)), "model history should include rejected candidates");
            AssertTrue(registryHistoryPresentation.HistoryItems.Any(item => item.DecisionText.Contains("\uD604\uC7AC \uC0AC\uC6A9", StringComparison.Ordinal)), "model history should identify the current inspection model");
            WpfModelRegistryHistoryItem rejectedHistoryItem = registryHistoryPresentation.HistoryItems.FirstOrDefault(item => item.WeightsPath == rejectedWeightsPath);
            AssertTrue(rejectedHistoryItem != null, "model history should keep rejected candidates visible for traceability");
            AssertTrue(!rejectedHistoryItem.CanPromoteToInspectionModel, "rejected model-history rows should not be directly promotable to inspection model");
            AssertTrue(rejectedHistoryItem.ActionText.Contains("\uAC70\uC808", StringComparison.Ordinal), "rejected model-history rows should expose a rejected action state");
            shellViewModel.SetModelRegistryState(registryHistoryPresentation);
            AssertTrue(shellViewModel.IsModelRegistryHistoryVisible, "shell model-registry history should become visible when rows exist");
            AssertTrue(shellViewModel.ModelRegistryHistoryItems.Count >= 2, "shell ViewModel should keep recent model-history rows for the model center");
            AssertTrue(shellViewModel.ModelRegistryHistoryHeaderText.Contains(shellViewModel.ModelRegistryHistoryItems.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
                "model-history header should include the visible row count");
            AssertTrue(shellViewModel.SelectedModelRegistryHistoryItem != null, "shell model-registry history should select a row for detail review");
            AssertTrue(shellViewModel.IsSelectedModelHistoryVisible, "selected model-history detail should be visible when a history row exists");
            AssertEqual(shellViewModel.SelectedModelRegistryHistoryItem.TitleText, shellViewModel.SelectedModelHistoryTitleText);
            AssertTrue(shellViewModel.SelectedModelHistoryComparisonTitleText.Contains("\uD604\uC7AC \uAC80\uC0AC", StringComparison.Ordinal), "selected model-history comparison should name the current inspection model");
            AssertTrue(shellViewModel.SelectedModelHistoryCurrentModelText.Contains("\uD604\uC7AC \uAC80\uC0AC", StringComparison.Ordinal), "selected model-history comparison should include the current model column");
            AssertTrue(shellViewModel.SelectedModelHistorySelectedModelText.Contains("\uD604\uC7AC \uAC80\uC0AC", StringComparison.Ordinal), "the initially selected current model should compare as the current model");

            WpfModelRegistryHistoryItem promotableHistoryItem = shellViewModel.ModelRegistryHistoryItems.FirstOrDefault(item => item.CanPromoteToInspectionModel && item.WeightsPath == pendingWeightsPath);
            AssertTrue(promotableHistoryItem != null, "model history should expose a non-current candidate that can be applied intentionally");
            shellViewModel.SelectedModelRegistryHistoryItem = promotableHistoryItem;
            AssertEqual(promotableHistoryItem.TitleText, shellViewModel.SelectedModelHistoryTitleText);
            AssertTrue(shellViewModel.SelectedModelHistoryDecisionText.Contains("\uACB0\uC815", StringComparison.Ordinal), "selected model-history detail should keep adoption/rejection decision context");
            AssertTrue(shellViewModel.SelectedModelHistoryCurrentModelText.Contains("\uD604\uC7AC \uAC80\uC0AC", StringComparison.Ordinal), "selected model-history comparison should keep the current model visible after selecting another row");
            AssertTrue(shellViewModel.SelectedModelHistorySelectedModelText.Contains("\uC120\uD0DD \uC774\uB825", StringComparison.Ordinal), "selected model-history comparison should identify the selected history column");
            AssertTrue(shellViewModel.SelectedModelHistoryComparisonMetricText.Contains("mAP50-95", StringComparison.Ordinal), "selected model-history comparison should include current and selected metric context");
            AssertTrue(shellViewModel.IsSelectedModelHistoryActionEnabled, "selected model-history apply action should enable for an existing non-current candidate");
            shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
                isInferenceMode: false,
                isYoloEnvironmentCommandRunning: false,
                isDetecting: false,
                isBatchDetectionRunning: false,
                isTrainingCommandRunning: false,
                isTrainingStopAvailable: false,
                hasCurrentRecipeName: false));
            AssertTrue(!shellViewModel.IsSelectedModelHistoryActionEnabled, "selected model-history apply action should require recipe save permission");
            shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
                isInferenceMode: false,
                isYoloEnvironmentCommandRunning: false,
                isDetecting: false,
                isBatchDetectionRunning: false,
                isTrainingCommandRunning: false,
                isTrainingStopAvailable: false,
                hasCurrentRecipeName: true));
        }
        finally
        {
            DeleteDirectoryIfExists(modelRegistryTempRoot);
        }
        shellViewModel.SetModelCenterTrainingState("Training completed", "Epoch 5/5");
        AssertEqual("Training completed", shellViewModel.ModelCenterTrainingStatusText);
        AssertEqual("Epoch 5/5", shellViewModel.ModelCenterTrainingDetailText);
        shellViewModel.SetModelCenterRecoveryState("Training failed", "Worker disconnected", "Restart YOLO then retry");
        AssertTrue(shellViewModel.IsModelCenterRecoveryVisible, "model-center recovery card should become visible when failure guidance is set");
        AssertEqual("Training failed", shellViewModel.ModelCenterRecoveryTitleText);
        AssertEqual("Worker disconnected", shellViewModel.ModelCenterRecoveryDetailText);
        AssertEqual("Restart YOLO then retry", shellViewModel.ModelCenterRecoveryActionText);
        shellViewModel.ClearModelCenterRecoveryState();
        AssertTrue(!shellViewModel.IsModelCenterRecoveryVisible, "model-center recovery card should hide after the recovery state is cleared");
        AssertTrue(!shellViewModel.IsModelCenterAnomalyEvaluationVisible, "model-center anomaly evaluation should start hidden until a summary is loaded");
        AssertTrue(!shellViewModel.IsModelCenterAnomalyEvaluationPickerVisible, "anomaly evaluation summary picker should start hidden");
        shellViewModel.SetModelCenterAnomalyEvaluationPickerVisible(true);
        AssertTrue(shellViewModel.IsModelCenterAnomalyEvaluationPickerVisible, "anomaly evaluation summary picker should become visible for anomaly datasets");
        AssertTrue(shellViewModel.IsModelCenterAnomalyEvaluationPickerEnabled, "anomaly evaluation summary picker should be enabled while model-center review commands can run");
        shellViewModel.SetModelCenterAnomalyEvaluationPickerVisible(false);
        AssertTrue(!shellViewModel.IsModelCenterAnomalyEvaluationPickerEnabled, "hidden anomaly evaluation summary picker should not stay enabled");
        shellViewModel.SetModelCenterAnomalyEvaluationState(new WpfAnomalyClassificationEvaluationPresentation
        {
            RecommendationText = "\uC774\uC0C1 \uBD84\uB958 \uD3C9\uAC00: \uBCF4\uB958",
            MetricsText = "\uD3C9\uAC00 4\uC7A5 / \uC815\uC0C1 1/2, \uC774\uC0C1 0/2 / \uC804\uCCB4 25%",
            DetailText = "\uBCF4\uB958 \uC0AC\uC720: \uAC80\uC99D \uC774\uBBF8\uC9C0 4/10\uC7A5",
            ActionText = "\uC815\uC0C1/\uC774\uC0C1 held-out \uC774\uBBF8\uC9C0\uB97C \uB354 \uBAA8\uC73C\uACE0 \uB2E4\uC2DC \uD3C9\uAC00"
        });
        AssertTrue(shellViewModel.IsModelCenterAnomalyEvaluationVisible, "model-center anomaly evaluation should become visible after a presentation is set");
        AssertTrue(shellViewModel.ModelCenterAnomalyEvaluationRecommendationText.Contains("\uBCF4\uB958", StringComparison.Ordinal), "model-center anomaly evaluation should expose the recommendation");
        AssertTrue(shellViewModel.ModelCenterAnomalyEvaluationDetailText.Contains("\uAC80\uC99D \uC774\uBBF8\uC9C0", StringComparison.Ordinal), "model-center anomaly evaluation should expose blocker detail");
        shellViewModel.ClearModelCenterAnomalyEvaluationState();
        AssertTrue(!shellViewModel.IsModelCenterAnomalyEvaluationVisible, "model-center anomaly evaluation should hide after it is cleared");
        AssertEqual(string.Empty, shellViewModel.ModelCenterAnomalyEvaluationMetricsText);
        shellViewModel.SetModelCenterModelState("Current best.pt", "New candidate.pt", "Save required", "Compare then save");
        AssertEqual("Current best.pt", shellViewModel.ModelCenterCurrentModelText);
        AssertEqual("New candidate.pt", shellViewModel.ModelCenterCandidateModelText);
        AssertEqual("Save required", shellViewModel.ModelCenterAdoptionText);
        AssertEqual("Compare then save", shellViewModel.ModelCenterNextActionText);
        AssertEqual("Current best.pt", shellViewModel.ModelCenterCurrentModelDetailText);
        AssertEqual("New candidate.pt", shellViewModel.ModelCenterCandidateModelDetailText);
        AssertEqual("Save required", shellViewModel.ModelCenterAdoptionDetailText);
        AssertEqual("Compare then save", shellViewModel.ModelCenterNextActionDetailText);
        AssertTrue(shellViewModel.ModelCenterDecisionSummaryText.Contains("Save required", StringComparison.Ordinal), "model-center decision summary should default from adoption status");
        AssertTrue(shellViewModel.ModelCenterDecisionEvidenceText.Contains("Current best.pt", StringComparison.Ordinal), "model-center decision evidence should include the current inspection model");
        AssertTrue(shellViewModel.ModelCenterDecisionEvidenceText.Contains("New candidate.pt", StringComparison.Ordinal), "model-center decision evidence should include the training candidate");
        AssertTrue(shellViewModel.ModelCenterDecisionActionText.Contains("Compare then save", StringComparison.Ordinal), "model-center decision action should default from next action status");
        AssertEqual("\uD604\uC7AC \uAC80\uC0AC", shellViewModel.ModelCenterInspectCurrentImageButtonText);
        AssertTrue(shellViewModel.ModelCenterInspectCurrentImageButtonToolTip.Contains("Current best.pt", StringComparison.Ordinal), "model-center current-inspection tooltip should include the active inspection model");
        AssertTrue(!shellViewModel.IsModelCenterInspectCurrentImageEnabled, "model-center current-inspection button should wait for inference-ready state");
        AssertEqual("\uD6C4\uBCF4 \uC5C6\uC74C", shellViewModel.ModelCenterConfirmModelButtonText);
        AssertTrue(!shellViewModel.IsModelCenterConfirmModelEnabled, "model-center confirm button should stay disabled without a selected candidate");
        AssertEqual("\uD6C4\uBCF4 \uC5C6\uC74C", shellViewModel.ModelCenterReviewCandidateButtonText);
        AssertTrue(!shellViewModel.IsModelCenterReviewCandidateEnabled, "model-center candidate review button should stay disabled without a candidate");
        shellViewModel.SetModelCenterCandidateReviewState(
            "\uD6C4\uBCF4 \uAC80\uC99D",
            "\uD6C4\uBCF4 \uBAA8\uB378\uC758 \uCD5C\uC885 \uAC80\uC99D \uBE44\uAD50 \uD654\uBA74\uC73C\uB85C \uC774\uB3D9\uD569\uB2C8\uB2E4.",
            canReviewCandidate: true);
        AssertEqual("\uD6C4\uBCF4 \uAC80\uC99D", shellViewModel.ModelCenterReviewCandidateButtonText);
        AssertTrue(shellViewModel.IsModelCenterReviewCandidateEnabled, "model-center candidate review button should enable when a candidate can be reviewed");
        shellViewModel.SetModelCenterModelState(
            "\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378: best.pt",
            "\uC0C8 \uD559\uC2B5 \uBAA8\uB378 \uD6C4\uBCF4: exp7/best.pt",
            "\uBAA8\uB378 \uC801\uC6A9: \uC0C8 \uD6C4\uBCF4 \uAC80\uD1A0 \uD544\uC694",
            "\uB2E4\uC74C: \uBAA8\uB378 \uBE44\uAD50 \uD6C4 \uC800\uC7A5",
            "\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5",
            "\uC120\uD0DD\uD55C \uBAA8\uB378\uC744 recipe\uC5D0 \uC800\uC7A5\uD558\uACE0 \uB2E4\uC74C \uCD94\uB860\uBD80\uD130 \uAC80\uC0AC \uBAA8\uB378\uB85C \uC0AC\uC6A9\uD569\uB2C8\uB2E4.",
            canConfirmModel: true,
            decisionSummaryText: "\uD310\uB2E8: \uC0C8 \uBAA8\uB378 \uD6C4\uBCF4 \uAC80\uC99D \uD6C4 \uC801\uC6A9 \uAC00\uB2A5",
            decisionEvidenceText: "\uADFC\uAC70: mAP50-95 +7.0%p / \uCD5C\uC885 \uAC80\uC99D \uC608\uC2DC \uD655\uC778",
            decisionActionText: "\uC800\uC7A5: \uD6C4\uBCF4 \uAC80\uC99D \uD6C4 \uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5; recipe \uC800\uC7A5 \uD6C4 \uB2E4\uC74C \uCD94\uB860\uBD80\uD130 \uC0AC\uC6A9",
            runtimeActionText: "YOLO11 Ultralytics / \uD604\uC7AC \uAC80\uC0AC \uAC00\uB2A5 / \uD559\uC2B5 \uBBF8\uC9C0\uC6D0");
        AssertEqual("best.pt", shellViewModel.ModelCenterCurrentModelDetailText);
        AssertEqual("exp7/best.pt", shellViewModel.ModelCenterCandidateModelDetailText);
        AssertEqual("\uC0C8 \uD6C4\uBCF4 \uAC80\uD1A0 \uD544\uC694", shellViewModel.ModelCenterAdoptionDetailText);
        AssertEqual("\uBAA8\uB378 \uBE44\uAD50 \uD6C4 \uC800\uC7A5", shellViewModel.ModelCenterNextActionDetailText);
        AssertEqual("\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5", shellViewModel.ModelCenterConfirmModelButtonText);
        AssertTrue(shellViewModel.ModelCenterRuntimeActionText.Contains("YOLO11", StringComparison.Ordinal), "model-center should keep selected runtime readiness as ViewModel state");
        AssertTrue(shellViewModel.ModelCenterActionStateText.Contains("\uC2E4\uD589\uAE30", StringComparison.Ordinal)
            && shellViewModel.ModelCenterActionStateText.Contains("\uD604\uC7AC \uAC80\uC0AC \uAC00\uB2A5", StringComparison.Ordinal)
            && shellViewModel.ModelCenterActionStateText.Contains("\uD559\uC2B5 \uBBF8\uC9C0\uC6D0", StringComparison.Ordinal),
            "model-center action state should expose runtime readiness next to current-inspection actions");
        AssertTrue(shellViewModel.ModelCenterConfirmModelButtonToolTip.Contains("recipe", StringComparison.Ordinal), "model-center confirm tooltip should explain recipe persistence");
        AssertTrue(shellViewModel.ModelCenterConfirmModelButtonToolTip.Contains("\uB2E4\uC74C \uCD94\uB860", StringComparison.Ordinal), "model-center confirm tooltip should explain when the selected model is used");
        AssertTrue(shellViewModel.ModelCenterDecisionSummaryText.Contains("\uC801\uC6A9 \uAC00\uB2A5", StringComparison.Ordinal), "model-center decision summary should accept an explicit recommendation");
        AssertTrue(shellViewModel.ModelCenterDecisionEvidenceText.Contains("mAP50-95", StringComparison.Ordinal), "model-center decision evidence should expose metric context");
        AssertTrue(shellViewModel.ModelCenterDecisionActionText.Contains("\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5", StringComparison.Ordinal), "model-center decision action should expose the save step");
        AssertTrue(shellViewModel.IsModelCenterConfirmModelEnabled, "model-center confirm button should enable when a model candidate is selected");
        shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(shellViewModel.IsModelCenterInspectCurrentImageEnabled, "model-center current-inspection button should enable when inference is ready");
        shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: true,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!shellViewModel.IsModelCenterConfirmModelEnabled, "model-center confirm button should disable while another command is running");
        AssertTrue(!shellViewModel.IsModelCenterReviewCandidateEnabled, "model-center candidate review button should disable while another command is running");
        AssertTrue(!shellViewModel.IsModelCenterInspectCurrentImageEnabled, "model-center current-inspection button should disable while another command is running");
        shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(shellViewModel.IsModelCenterConfirmModelEnabled, "model-center confirm button should re-enable after commands are idle");
        AssertTrue(shellViewModel.IsModelCenterReviewCandidateEnabled, "model-center candidate review button should re-enable after commands are idle");
        AssertTrue(shellViewModel.IsModelCenterInspectCurrentImageEnabled, "model-center current-inspection button should re-enable after commands are idle");
        shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: false,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: false));
        AssertTrue(!shellViewModel.IsModelCenterConfirmModelEnabled, "model-center confirm button should stay disabled when no recipe can be saved");
        AssertTrue(shellViewModel.ModelCenterConfirmModelButtonToolTip.Contains("recipe", StringComparison.Ordinal), "disabled model-center save button should explain missing recipe persistence");
        AssertTrue(shellViewModel.IsModelCenterReviewCandidateEnabled, "model-center candidate review button should stay enabled for review navigation when commands are idle");
        AssertEqual("LineA_Defect", WpfDatasetContextPresentationService.BuildDatasetName("LineA_Defect", @"C:\Dataset\Other"));
        AssertEqual("FallbackDataset", WpfDatasetContextPresentationService.BuildDatasetName(" ", @"C:\Dataset\FallbackDataset\"));
        AssertEqual("\uB370\uC774\uD130\uC14B \uBBF8\uC120\uD0DD", WpfDatasetContextPresentationService.BuildDatasetName(" ", " "));
        AssertEqual("\uAC1D\uCCB4 \uD0D0\uC9C0", WpfDatasetContextPresentationService.FormatPurposeName(LabelingDatasetPurpose.ObjectDetection));
        AssertEqual("\uC138\uADF8\uBA58\uD14C\uC774\uC158", WpfDatasetContextPresentationService.FormatPurposeName(LabelingDatasetPurpose.Segmentation));
        AssertEqual("\uC774\uC0C1 \uD0D0\uC9C0", WpfDatasetContextPresentationService.FormatPurposeName(LabelingDatasetPurpose.AnomalyDetection));
        shellViewModel.SetDatasetContext(
            "LineA_Defect",
            "\uAC1D\uCCB4 \uD0D0\uC9C0",
            @"C:\Dataset\LineA_Defect",
            @"C:\Dataset\LineA_Defect\data\train\images",
            canOpenDatasetFolder: true,
            classCount: 2);
        AssertEqual("LineA_Defect", shellViewModel.CurrentDatasetName);
        AssertEqual("\uAC1D\uCCB4 \uD0D0\uC9C0", shellViewModel.CurrentDatasetPurposeText);
        AssertTrue(shellViewModel.CurrentDatasetPathText.Contains("\uB77C\uBCA8/\uB808\uC2DC\uD53C \uC800\uC7A5:", StringComparison.Ordinal), "shell dataset context should show the dataset output root");
        AssertTrue(shellViewModel.CurrentDatasetPathText.Contains("\uC6D0\uBCF8 \uC774\uBBF8\uC9C0 \uD3F4\uB354:", StringComparison.Ordinal), "shell dataset context should show the image folder");
        AssertTrue(shellViewModel.CurrentDatasetStoragePathText.Contains("\uB77C\uBCA8/\uB808\uC2DC\uD53C \uC800\uC7A5:", StringComparison.Ordinal), "shell dataset context should expose the storage path separately");
        AssertTrue(shellViewModel.CurrentDatasetImageRootText.Contains("\uC6D0\uBCF8 \uC774\uBBF8\uC9C0 \uD3F4\uB354:", StringComparison.Ordinal), "shell dataset context should expose the image root separately");
        AssertTrue(shellViewModel.CurrentDatasetSourceText.Contains("\uD074\uB798\uC2A4: \uB808\uC2DC\uD53C 2\uAC1C", StringComparison.Ordinal), "shell dataset context should show that classes come from the recipe");
        AssertTrue(shellViewModel.CurrentDatasetSourceText.Contains("\uB77C\uBCA8: \uC800\uC7A5 \uD3F4\uB354 \uAE30\uC900", StringComparison.Ordinal), "shell dataset context should show that labels come from the storage folder");
        AssertTrue(shellViewModel.CurrentDatasetToolTip.Contains(@"C:\Dataset\LineA_Defect", StringComparison.Ordinal), "shell dataset context tooltip should keep full paths");
        AssertTrue(shellViewModel.CurrentDatasetToolTip.Contains("\uB77C\uBCA8/\uB808\uC2DC\uD53C \uC800\uC7A5 \uD3F4\uB354", StringComparison.Ordinal), "shell dataset context tooltip should name the storage folder role");
        AssertTrue(shellViewModel.CurrentDatasetToolTip.Contains("\uC6D0\uBCF8 \uC774\uBBF8\uC9C0 \uD3F4\uB354", StringComparison.Ordinal), "shell dataset context tooltip should name the image folder role");
        AssertTrue(shellViewModel.CurrentDatasetToolTip.Contains("\uC774\uBBF8\uC9C0 \uD3F4\uB354 \uBCC0\uACBD", StringComparison.Ordinal), "shell dataset context tooltip should explain that image-folder changes do not move label storage");
        AssertTrue(shellViewModel.IsOpenDatasetFolderEnabled, "shell dataset folder should be openable when an output root exists");
        var candidateReviewViewModel = new WpfCandidateReviewPanelViewModel();
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ComparisonVisibility);
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ModelComparisonVisibility);
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ModelComparisonExampleListVisibility);
        AssertTrue(!candidateReviewViewModel.IsModelComparisonExamplesExpanded, "model-comparison examples should start collapsed");
        AssertTrue(candidateReviewViewModel.ModelComparisonExampleHeaderText.Contains("0", StringComparison.Ordinal), "model-comparison example disclosure should start with a zero-count header");
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ReviewHistoryVisibility);
        AssertTrue(!candidateReviewViewModel.IsReviewHistoryExpanded, "candidate review history should start collapsed");
        AssertTrue(candidateReviewViewModel.ReviewHistoryHeaderText.Contains("0", StringComparison.Ordinal), "candidate review history disclosure should start with a zero-count header");
        AssertTrue(candidateReviewViewModel.ModelComparisonStatusText.Contains("\uB300\uAE30", StringComparison.Ordinal), "candidate review should show pending model-comparison state before results exist");
        AssertTrue(candidateReviewViewModel.ModelComparisonSourceText.Contains("\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378", StringComparison.Ordinal), "candidate review should start with a visible model-comparison source placeholder");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("\uD6C4\uBCF4 \uAC80\uC99D", StringComparison.Ordinal), "pending model-comparison state should point to the candidate validation action");
        AssertTrue(!candidateReviewViewModel.IsSaveModelCandidateEnabled, "model candidate save should be disabled until a candidate is staged");
        AssertTrue(!candidateReviewViewModel.IsRejectModelCandidateEnabled, "model candidate reject should be disabled until a candidate is staged");
        WpfModelComparisonReviewExample openedModelComparisonExample = null;
        bool savedModelCandidate = false;
        bool rejectedModelCandidate = false;
        candidateReviewViewModel.ConfigureCommands(
            _ => { },
            () => { },
            () => { },
            () => { },
            () => { },
            () => { },
            () => { },
            () => { },
            _ => { },
            _ => { },
            () => { },
            example => openedModelComparisonExample = example,
            saveModelCandidate: () => savedModelCandidate = true,
            rejectModelCandidate: () => rejectedModelCandidate = true);
        candidateReviewViewModel.SetModelCandidateDecisionState(
            canSave: true,
            canReject: true,
            statusText: "\uD6C4\uBCF4 \uACB0\uC815: \uC800\uC7A5 \uB610\uB294 \uAC70\uC808 \uD544\uC694",
            detailText: "\uBE44\uAD50 \uD6C4 \uACB0\uC815",
            saveToolTip: "\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5",
            rejectToolTip: "\uD6C4\uBCF4 \uAC70\uC808");
        AssertTrue(candidateReviewViewModel.IsSaveModelCandidateEnabled, "model candidate save should become available when a candidate is staged");
        AssertTrue(candidateReviewViewModel.IsRejectModelCandidateEnabled, "model candidate reject should become available when a baseline can be restored");
        AssertTrue(candidateReviewViewModel.ModelCandidateDecisionStatusText.Contains("\uAC70\uC808", StringComparison.Ordinal), "model candidate decision status should name both possible decisions");
        candidateReviewViewModel.SaveModelCandidateCommand.Execute(null);
        candidateReviewViewModel.RejectModelCandidateCommand.Execute(null);
        AssertTrue(savedModelCandidate, "candidate review should route model-candidate save through the configured command");
        AssertTrue(rejectedModelCandidate, "candidate review should route model-candidate reject through the configured command");
        candidateReviewViewModel.SetComparison(new WpfCandidateComparisonPresentation("AI OK\n10x10 @ 1,2", "\uD604\uC7AC \uB77C\uBCA8 OK\n10x10 @ 1,2", "\uC911\uBCF5\n100%", true));
        AssertEqual(System.Windows.Visibility.Visible, candidateReviewViewModel.ComparisonVisibility);
        AssertTrue(candidateReviewViewModel.IsComparisonHighOverlap, "candidate comparison should expose duplicate/high-overlap state");
        AssertTrue(candidateReviewViewModel.ComparisonCandidateText.Contains("AI OK", StringComparison.Ordinal), "candidate comparison should keep AI text");
        AssertTrue(candidateReviewViewModel.ComparisonCurrentText.Contains("\uD604\uC7AC \uB77C\uBCA8 OK", StringComparison.Ordinal), "candidate comparison should keep current-label text");
        AssertTrue(candidateReviewViewModel.ComparisonOverlapText.Contains("100", StringComparison.Ordinal), "candidate comparison should keep overlap text");
        candidateReviewViewModel.ClearComparison();
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ComparisonVisibility);
        AssertTrue(!candidateReviewViewModel.IsComparisonHighOverlap, "candidate comparison clear should reset high-overlap state");
        candidateReviewViewModel.ApplySelectionReview(
            "Selected detail",
            new WpfCandidateComparisonPresentation("AI NG", "Manual NG", "IoU\n25%", false),
            showComparison: true);
        AssertEqual("Selected detail", candidateReviewViewModel.DetailText);
        AssertEqual(System.Windows.Visibility.Visible, candidateReviewViewModel.ComparisonVisibility);
        AssertTrue(candidateReviewViewModel.ComparisonCandidateText.Contains("AI NG", StringComparison.Ordinal), "candidate selection review should update comparison text with detail");
        candidateReviewViewModel.ApplySelectionReview("No selection", default, showComparison: false);
        AssertEqual("No selection", candidateReviewViewModel.DetailText);
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ComparisonVisibility);
        candidateReviewViewModel.SetModelComparisonReview(WpfModelComparisonReviewReport.Empty);
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ModelComparisonVisibility);
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ModelComparisonExampleListVisibility);
        AssertTrue(candidateReviewViewModel.ModelComparisonStatusText.Contains("\uC544\uC9C1 \uC2E4\uD589", StringComparison.Ordinal), "missing model-comparison result should stay visible as a not-run state");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("\uAC80\uC0AC \uBAA8\uB378\uB85C \uC800\uC7A5", StringComparison.Ordinal), "missing model-comparison result should still explain where model adoption happens");
        candidateReviewViewModel.SetModelComparisonReview(new WpfModelComparisonReviewReport(
            hasComparison: true,
            summaryText: "\uBAA8\uB378 \uCC28\uC774 \uC608\uC2DC: \uC900\uBE44",
            detailText: "\uAE30\uC874 \uBAA8\uB378 1\uAC1C, \uC0C8 \uBAA8\uB378 2\uAC1C",
            sourcePath: string.Empty,
            examples: new[]
            {
                new WpfModelComparisonReviewExample(
                    "img-a",
                    "CandidateOnly",
                    "\uC0C8 \uBAA8\uB378\uB9CC \uAC80\uCD9C",
                    "\uC0C8 \uBAA8\uB378 NG 94%",
                        MahApps.Metro.IconPacks.PackIconMaterialKind.PlusCircleOutline,
                        priority: 3,
                    actionText: "\uD655\uC778: \uC2E4\uC81C \uAC1D\uCCB4\uBA74 \uC0C8 \uBAA8\uB378 \uAC1C\uC120",
                    locationText: "\uC704\uCE58: \uC911\uC2EC 50%, 50% / \uD06C\uAE30 10% x 10%")
            }));
        candidateReviewViewModel.SetModelComparisonSourceText("\uBE44\uAD50 \uB300\uC0C1: \uD604\uC7AC \uAC80\uC0AC YOLOv5 / baseline.pt -> \uD559\uC2B5 \uD6C4\uBCF4 YOLOv5 / candidate.pt");
        AssertEqual(System.Windows.Visibility.Visible, candidateReviewViewModel.ModelComparisonVisibility);
        AssertEqual(System.Windows.Visibility.Visible, candidateReviewViewModel.ModelComparisonExampleListVisibility);
        AssertTrue(candidateReviewViewModel.ModelComparisonSourceText.Contains("baseline.pt", StringComparison.Ordinal), "candidate review should show the current inspection model used as the comparison baseline");
        AssertTrue(candidateReviewViewModel.ModelComparisonSourceText.Contains("candidate.pt", StringComparison.Ordinal), "candidate review should show the trained candidate model being evaluated");
        AssertTrue(!candidateReviewViewModel.IsModelComparisonExamplesExpanded, "model-comparison examples should be available but collapsed by default");
        AssertTrue(candidateReviewViewModel.ModelComparisonExampleHeaderText.Contains("1", StringComparison.Ordinal), "model-comparison example disclosure should show the available example count");
        AssertTrue(candidateReviewViewModel.ModelComparisonExampleSummaryText.Contains("\uD3BC\uCCD0", StringComparison.Ordinal), "model-comparison example disclosure should tell the operator to expand only when needed");
        AssertEqual(1, candidateReviewViewModel.ModelComparisonExamples.Count);
        AssertTrue(candidateReviewViewModel.ModelComparisonStatusText.Contains("\uBAA8\uB378 \uCC28\uC774 \uC608\uC2DC", StringComparison.Ordinal), "candidate review should expose model comparison status text");
        AssertTrue(candidateReviewViewModel.ModelComparisonExamples[0].ActionText.Contains("\uD655\uC778", StringComparison.Ordinal), "candidate review model comparison examples should expose action text");
        AssertTrue(candidateReviewViewModel.ModelComparisonExamples[0].LocationText.Contains("\uC704\uCE58", StringComparison.Ordinal), "candidate review model comparison examples should expose visible location text");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("\uB2E4\uC74C", StringComparison.Ordinal), "candidate review model comparison should tell the operator the next action");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("Guide", StringComparison.Ordinal), "candidate review model comparison should point back to the Guide decision after examples are reviewed");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("\uBAA8\uB378\uC13C\uD130", StringComparison.Ordinal), "candidate review model comparison should tell the operator where to confirm the model");
        candidateReviewViewModel.ModelComparisonExampleCommand.Execute(candidateReviewViewModel.ModelComparisonExamples[0]);
        AssertTrue(ReferenceEquals(candidateReviewViewModel.ModelComparisonExamples[0], openedModelComparisonExample), "candidate review should route clicked model-comparison examples through the configured command");
        candidateReviewViewModel.IsModelComparisonExamplesExpanded = true;
        candidateReviewViewModel.SetModelComparisonFocus(candidateReviewViewModel.ModelComparisonExamples[0], "\uD06C\uAE30 10x10 / \uC704\uCE58 x=1, y=2");
        AssertTrue(candidateReviewViewModel.IsModelComparisonExamplesExpanded, "focusing an example should not collapse an already-expanded model-comparison list");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("\uAD50\uCCB4 \uD310\uB2E8", StringComparison.Ordinal), "focused model comparison example should keep the replacement-decision step visible");
        AssertTrue(candidateReviewViewModel.ModelComparisonActionText.Contains("\uBAA8\uB378\uC13C\uD130", StringComparison.Ordinal), "focused model comparison example should keep the model-center confirmation step visible");
        candidateReviewViewModel.ClearModelComparisonReview();
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ModelComparisonVisibility);
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ModelComparisonExampleListVisibility);
        AssertTrue(!candidateReviewViewModel.IsModelComparisonExamplesExpanded, "clearing model-comparison review should collapse the disclosure");
        AssertTrue(candidateReviewViewModel.ModelComparisonStatusText.Contains("\uB300\uAE30", StringComparison.Ordinal), "clearing model-comparison review should reset the pending state without occupying current-image candidate review space");
        AssertTrue(candidateReviewViewModel.ModelComparisonSourceText.Contains("\uD655\uC778 \uD544\uC694", StringComparison.Ordinal), "clearing model-comparison review should reset the comparison source text");
        AssertEqual(0, candidateReviewViewModel.ModelComparisonExamples.Count);
        candidateReviewViewModel.AddReviewHistory("\uB77C\uBCA8 \uD655\uC815 1\uAC74");
        AssertEqual(System.Windows.Visibility.Visible, candidateReviewViewModel.ReviewHistoryVisibility);
        AssertTrue(!candidateReviewViewModel.IsReviewHistoryExpanded, "candidate review history should appear collapsed after the first action");
        AssertTrue(candidateReviewViewModel.ReviewHistoryHeaderText.Contains("1", StringComparison.Ordinal), "candidate review history disclosure should show the action count");
        candidateReviewViewModel.IsReviewHistoryExpanded = true;
        candidateReviewViewModel.AddReviewHistory("\uD6C4\uBCF4 \uC228\uAE40 1\uAC74");
        AssertTrue(candidateReviewViewModel.IsReviewHistoryExpanded, "new review history should not collapse an already-expanded history disclosure");
        AssertTrue(candidateReviewViewModel.ReviewHistoryHeaderText.Contains("2", StringComparison.Ordinal), "candidate review history disclosure should update the action count");
        candidateReviewViewModel.ClearReviewHistory();
        AssertEqual(System.Windows.Visibility.Collapsed, candidateReviewViewModel.ReviewHistoryVisibility);
        AssertTrue(!candidateReviewViewModel.IsReviewHistoryExpanded, "clearing review history should collapse the disclosure");
        candidateReviewViewModel.SetNavigationState(previousEnabled: true, nextEnabled: true, focusEnabled: true);
        AssertTrue(candidateReviewViewModel.IsPreviousCandidateEnabled, "candidate previous navigation should be exposed independently from skip");
        AssertTrue(candidateReviewViewModel.IsNextCandidateEnabled, "candidate next navigation should be exposed independently from skip");
        AssertTrue(candidateReviewViewModel.IsFocusCandidateEnabled, "candidate focus should be exposed independently from skip");
        candidateReviewViewModel.SetNavigationState(previousEnabled: false, nextEnabled: false, focusEnabled: true);
        AssertTrue(!candidateReviewViewModel.IsPreviousCandidateEnabled, "candidate previous navigation should disable when there is only one visible candidate");
        AssertTrue(!candidateReviewViewModel.IsNextCandidateEnabled, "candidate next navigation should disable when there is only one visible candidate");
        AssertTrue(candidateReviewViewModel.IsFocusCandidateEnabled, "candidate focus can remain enabled for a single selected candidate");
        int candidateCollectionChangedCount = 0;
        NotifyCollectionChangedAction candidateCollectionAction = NotifyCollectionChangedAction.Add;
        candidateReviewViewModel.Candidates.CollectionChanged += (_, e) =>
        {
            candidateCollectionChangedCount++;
            candidateCollectionAction = e.Action;
        };
        candidateReviewViewModel.SetCandidates(
            Enumerable.Range(0, 10000).Select(index => WpfCandidateReviewListItem.Empty($"candidate {index}", string.Empty)),
            "10000 candidates");
        AssertEqual(1, candidateCollectionChangedCount);
        AssertEqual(NotifyCollectionChangedAction.Reset, candidateCollectionAction);
        AssertEqual(10000, candidateReviewViewModel.Candidates.Count);
        var objectReviewViewModel = new WpfObjectReviewPanelViewModel();
        objectReviewViewModel.SetObjects(new[]
        {
            new WpfObjectReviewListItem("1. manual", "class", WpfObjectReviewSource.ManualRoi.ToString(), 0, WpfObjectReviewItemRef.Manual(0))
        }, "1 object");
        objectReviewViewModel.SetSelectedObjectClass(new[] { "Defect", "Scratch" }, "scratch");
        AssertEqual("Scratch", objectReviewViewModel.SelectedClassName);
        AssertTrue(objectReviewViewModel.IsApplyClassEnabled, "object review class apply should enable through the view model after selecting an object and class");
        objectReviewViewModel.SetSelectedObjectClass(new[] { "Defect", "Scratch" }, "");
        AssertEqual("Defect", objectReviewViewModel.SelectedClassName);
        int objectCollectionChangedCount = 0;
        NotifyCollectionChangedAction objectCollectionAction = NotifyCollectionChangedAction.Add;
        objectReviewViewModel.Objects.CollectionChanged += (_, e) =>
        {
            objectCollectionChangedCount++;
            objectCollectionAction = e.Action;
        };
        objectReviewViewModel.SetObjects(
            Enumerable.Range(0, 100000).Select(index => new WpfObjectReviewListItem(
                $"{index + 1}. manual",
                "class",
                WpfObjectReviewSource.ManualRoi.ToString(),
                index,
                WpfObjectReviewItemRef.Manual(index))),
            "100000 objects");
        AssertEqual(1, objectCollectionChangedCount);
        AssertEqual(NotifyCollectionChangedAction.Reset, objectCollectionAction);
        AssertEqual(100000, objectReviewViewModel.Objects.Count);
        AssertEqual(0, objectReviewViewModel.GetSelectedRowIndex());
        AssertTrue(objectReviewViewModel.IsSelectedSource(WpfObjectReviewSource.ManualRoi), "object review ViewModel should own selected-source checks for shell workflows");
        AssertTrue(objectReviewViewModel.TryResolveSelectedItem(new[] { string.Empty }, 100000, out WpfObjectReviewItemRef selectedObjectRef), "object review ViewModel should resolve the selected review item");
        AssertEqual(0, selectedObjectRef.Index);
        using (objectReviewViewModel.SuppressSelectionNotifications())
        {
            AssertTrue(objectReviewViewModel.IsSelectionNotificationSuppressed, "object review ViewModel should suppress transient programmatic selection events");
            using (objectReviewViewModel.SuppressSelectionNotifications())
            {
                AssertTrue(objectReviewViewModel.IsSelectionNotificationSuppressed, "object review selection suppression should be nestable");
            }

            AssertTrue(objectReviewViewModel.IsSelectionNotificationSuppressed, "outer object review selection suppression should remain active after inner dispose");
        }

        AssertTrue(!objectReviewViewModel.IsSelectionNotificationSuppressed, "object review selection suppression should clear after dispose");
        objectCollectionChangedCount = 0;
        objectCollectionAction = NotifyCollectionChangedAction.Reset;
        var replacementObject = new WpfObjectReviewListItem(
            "50001. manual / moved",
            "class",
            WpfObjectReviewSource.ManualRoi.ToString(),
            50000,
            WpfObjectReviewItemRef.Manual(50000));
        Stopwatch objectReplaceStopwatch = Stopwatch.StartNew();
        AssertTrue(objectReviewViewModel.TryReplaceObject(50000, replacementObject, select: true), "object review single-row replacement should succeed in a large list");
        objectReplaceStopwatch.Stop();
        AssertEqual(1, objectCollectionChangedCount);
        AssertEqual(NotifyCollectionChangedAction.Replace, objectCollectionAction);
        AssertTrue(ReferenceEquals(replacementObject, objectReviewViewModel.SelectedObject), "single-row replacement should keep the edited ROI selected");
        AssertTrue(objectReplaceStopwatch.Elapsed.TotalMilliseconds < 20.0, "single ROI edit should replace one object-review row without rebuilding the full side list");
        objectCollectionChangedCount = 0;
        objectCollectionAction = NotifyCollectionChangedAction.Reset;
        Stopwatch objectRemoveStopwatch = Stopwatch.StartNew();
        AssertTrue(objectReviewViewModel.TryRemoveObject(50000, "99999 objects", 50000), "object review single-row removal should succeed in a large list");
        objectRemoveStopwatch.Stop();
        AssertEqual(1, objectCollectionChangedCount);
        AssertEqual(NotifyCollectionChangedAction.Remove, objectCollectionAction);
        AssertEqual(99999, objectReviewViewModel.Objects.Count);
        AssertTrue(objectReviewViewModel.SelectedObject?.IsEnabled == true, "single-row removal should keep a neighboring object selected");
        AssertTrue(objectRemoveStopwatch.Elapsed.TotalMilliseconds < 20.0, "single ROI delete should remove one object-review row without resetting the full side list");
        var queueViewModel = new WpfImageQueuePanelViewModel();
        AssertTrue(shellViewModel.IsLabelingModeActive, "shell should start in labeling mode");
        AssertTrue(!shellViewModel.IsInferenceModeActive, "shell inference mode should start inactive");
        AssertTrue(shellViewModel.IsDatasetStageActive, "shell workflow stage should start at the dataset home");
        AssertTrue(!shellViewModel.IsLabelingStageActive, "labeling workflow stage should start inactive until selected");
        AssertTrue(!shellViewModel.IsSavedLabelsViewVisible, "dataset stage should hide saved-label review");
        AssertTrue(!shellViewModel.IsCandidateReviewViewVisible, "dataset stage should hide AI-candidate review");
        AssertTrue(shellViewModel.IsGuideToolsViewVisible, "dataset stage should show guide/tools");
        AssertTrue(shellViewModel.IsClassCatalogViewVisible, "dataset stage should show class catalog");
        AssertTrue(!shellViewModel.IsYoloModelCenterViewVisible, "dataset stage should hide model center");
        AssertTrue(!shellViewModel.IsWorkflowStageModelActionPanelVisible, "dataset stage should hide the top model-action panel");
        AssertTrue(!shellViewModel.IsRightWorkflowSubNavigationVisible, "dataset stage should hide tab headers because compact workflow shortcuts replace them");
        AssertTrue(shellViewModel.IsRightWorkflowShortcutBarVisible, "dataset stage should show compact right workflow shortcuts");
        AssertTrue(shellViewModel.IsRightWorkflowDockExpanded, "dataset stage should keep the right workflow panel expanded for onboarding");
        AssertTrue(!shellViewModel.IsRightWorkflowDockRailVisible, "dataset stage should not show the collapsed right workflow rail");
        AssertEqual(340D, shellViewModel.RightWorkflowPaneGridLength.Value);
        shellViewModel.SetRightWorkflowExpandedPaneWidth(500D);
        AssertEqual(500D, shellViewModel.RightWorkflowPaneGridLength.Value);
        shellViewModel.SetRightWorkflowDockExpanded(false);
        AssertEqual(72D, shellViewModel.RightWorkflowPaneGridLength.Value);
        shellViewModel.SetRightWorkflowDockExpanded(true);
        AssertEqual(500D, shellViewModel.RightWorkflowPaneGridLength.Value);
        shellViewModel.SetRightWorkflowExpandedPaneWidth(340D);
        AssertTrue(shellViewModel.ToggleRightWorkflowDockCommand != null, "shell ViewModel should expose a right workflow dock toggle command");
        AssertTrue(!shellViewModel.IsSavedLabelsShortcutActive, "dataset stage should not keep saved-label shortcut active");
        AssertTrue(shellViewModel.IsLabelingGuideShortcutActive, "dataset stage should default to dataset guide shortcut active");
        AssertTrue(!shellViewModel.IsClassCatalogShortcutActive, "dataset stage should not keep class shortcut active");
        AssertEqual("1/4 데이터셋", shellViewModel.WorkflowStageProgressText);
        AssertTrue(shellViewModel.WorkflowStageTitleText.Contains("데이터셋", StringComparison.Ordinal), "dataset workflow stage should expose a visible summary title");
        AssertEqual("\uB370\uC774\uD130\uC14B \uD648", shellViewModel.RightWorkflowViewTitleText);
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("\uB370\uC774\uD130\uC14B", StringComparison.Ordinal), "dataset right workflow detail should explain dataset preparation");
        AssertEqual("\uD648", shellViewModel.RightWorkflowRailCurrentViewText);
        shellViewModel.SetRightWorkflowShortcut(WpfRightWorkflowShortcut.ClassCatalog);
        AssertTrue(!shellViewModel.IsSavedLabelsShortcutActive, "dataset class shortcut should keep saved-label shortcut inactive");
        AssertTrue(!shellViewModel.IsLabelingGuideShortcutActive, "dataset class shortcut should deactivate guide shortcut");
        AssertTrue(shellViewModel.IsClassCatalogShortcutActive, "dataset class shortcut should become active");
        AssertEqual("\uD074\uB798\uC2A4", shellViewModel.RightWorkflowViewTitleText);
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("\uC0C9\uC0C1", StringComparison.Ordinal), "dataset class detail should identify class color management");
        AssertEqual("\uD074\uB798\uC2A4", shellViewModel.RightWorkflowRailCurrentViewText);
        shellViewModel.SetRightWorkflowShortcut(WpfRightWorkflowShortcut.LabelingGuide);
        AssertTrue(shellViewModel.WorkflowStageNextActionText.Contains("라벨링", StringComparison.Ordinal), "dataset workflow stage should point to the next labeling action");
        AssertTrue(shellViewModel.ToggleThemeCommand != null, "shell ViewModel should expose a theme command");
        AssertTrue(shellViewModel.LoadSampleCommand != null, "shell ViewModel should expose a sample-load command");
        AssertTrue(shellViewModel.AddSampleRoiCommand != null, "shell ViewModel should expose an ROI-add command");
        AssertTrue(shellViewModel.SaveAnnotationsCommand != null, "shell ViewModel should expose a save command");
        AssertTrue(shellViewModel.LabelingModeCommand != null, "shell ViewModel should expose a labeling mode command");
        AssertTrue(shellViewModel.InferenceModeCommand != null, "shell ViewModel should expose an inference mode command");
        AssertTrue(shellViewModel.CheckYoloCommand != null, "shell ViewModel should expose a YOLO check command");
        AssertTrue(shellViewModel.DetectCurrentImageCommand != null, "shell ViewModel should expose a current-image detection command");
        AssertTrue(shellViewModel.IsLabelingModeButtonEnabled, "active labeling mode button should start enabled");
        AssertTrue(shellViewModel.IsInferenceModeButtonEnabled, "inactive inference mode should be switchable when idle");
        shellViewModel.SetWorkflowStage(WpfShellWorkflowStage.Labeling);
        AssertTrue(shellViewModel.IsLabelingStageActive, "labeling workflow stage should become active");
        AssertTrue(!shellViewModel.IsDatasetStageActive, "dataset workflow stage should become inactive");
        AssertTrue(shellViewModel.IsSavedLabelsViewVisible, "labeling stage should show saved-label review");
        AssertTrue(!shellViewModel.IsCandidateReviewViewVisible, "labeling stage should hide AI-candidate review");
        AssertTrue(shellViewModel.IsGuideToolsViewVisible, "labeling stage should show guide/tools");
        AssertTrue(shellViewModel.IsClassCatalogViewVisible, "labeling stage should show class catalog");
        AssertTrue(!shellViewModel.IsYoloModelCenterViewVisible, "labeling stage should hide model center");
        AssertTrue(!shellViewModel.IsWorkflowStageModelActionPanelVisible, "labeling stage should hide the top model-action panel");
        AssertTrue(!shellViewModel.IsRightWorkflowSubNavigationVisible, "labeling stage should hide tab subnavigation because compact shortcuts replace it");
        AssertTrue(shellViewModel.IsRightWorkflowShortcutBarVisible, "labeling stage should show compact right workflow shortcuts");
        AssertTrue(!shellViewModel.IsRightWorkflowDockExpanded, "labeling stage should collapse the right workflow panel by default");
        AssertTrue(shellViewModel.IsRightWorkflowDockRailVisible, "labeling stage should keep the collapsed right workflow rail visible");
        AssertEqual(72D, shellViewModel.RightWorkflowPaneGridLength.Value);
        shellViewModel.ToggleRightWorkflowDockCommand.Execute(null);
        AssertTrue(shellViewModel.IsRightWorkflowDockExpanded, "right workflow dock toggle should expand the labeling rail");
        AssertTrue(!shellViewModel.IsRightWorkflowDockRailVisible, "expanded right workflow dock should hide the rail");
        AssertEqual(340D, shellViewModel.RightWorkflowPaneGridLength.Value);
        AssertTrue(shellViewModel.IsSavedLabelsShortcutActive, "labeling stage should default to saved-label shortcut active");
        AssertTrue(!shellViewModel.IsLabelingGuideShortcutActive, "labeling stage should not default to guide shortcut active");
        AssertTrue(!shellViewModel.IsClassCatalogShortcutActive, "labeling stage should not default to class shortcut active");
        AssertEqual("\uC800\uC7A5 \uB77C\uBCA8", shellViewModel.RightWorkflowViewTitleText);
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("\uC800\uC7A5 \uB77C\uBCA8", StringComparison.Ordinal), "saved-label right workflow detail should identify current-image label work");
        AssertEqual("\uB77C\uBCA8", shellViewModel.RightWorkflowRailCurrentViewText);
        shellViewModel.SetRightWorkflowShortcut(WpfRightWorkflowShortcut.LabelingGuide);
        AssertTrue(!shellViewModel.IsSavedLabelsShortcutActive, "guide shortcut should deactivate saved-label shortcut");
        AssertTrue(shellViewModel.IsLabelingGuideShortcutActive, "guide shortcut should become active");
        AssertTrue(!shellViewModel.IsClassCatalogShortcutActive, "guide shortcut should deactivate class shortcut");
        AssertEqual("\uD604\uC7AC \uC791\uC5C5", shellViewModel.RightWorkflowViewTitleText);
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("\uD604\uC7AC \uC774\uBBF8\uC9C0", StringComparison.Ordinal), "current-task right workflow detail should focus on the current image action");
        AssertEqual("\uC791\uC5C5", shellViewModel.RightWorkflowRailCurrentViewText);
        shellViewModel.SetRightWorkflowShortcut(WpfRightWorkflowShortcut.ClassCatalog);
        AssertTrue(!shellViewModel.IsSavedLabelsShortcutActive, "class shortcut should deactivate saved-label shortcut");
        AssertTrue(!shellViewModel.IsLabelingGuideShortcutActive, "class shortcut should deactivate guide shortcut");
        AssertTrue(shellViewModel.IsClassCatalogShortcutActive, "class shortcut should become active");
        AssertEqual("\uD074\uB798\uC2A4", shellViewModel.RightWorkflowViewTitleText);
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("\uC0C9\uC0C1", StringComparison.Ordinal), "class right workflow detail should identify class name/color management");
        AssertEqual("\uD074\uB798\uC2A4", shellViewModel.RightWorkflowRailCurrentViewText);
        AssertEqual("2/4 라벨링", shellViewModel.WorkflowStageProgressText);
        AssertTrue(shellViewModel.WorkflowStageNextActionText.Contains("AI", StringComparison.Ordinal), "labeling workflow stage should point to the next AI review action");
        shellViewModel.SetWorkflowStage(WpfShellWorkflowStage.Inference);
        AssertTrue(shellViewModel.IsInferenceStageActive, "inference workflow stage should become active");
        AssertTrue(!shellViewModel.IsLabelingStageActive, "labeling workflow stage should become inactive");
        AssertTrue(!shellViewModel.IsSavedLabelsViewVisible, "inference stage should hide saved-label review");
        AssertTrue(shellViewModel.IsCandidateReviewViewVisible, "inference stage should show AI-candidate review");
        AssertTrue(!shellViewModel.IsGuideToolsViewVisible, "inference stage should hide guide/tools");
        AssertTrue(!shellViewModel.IsClassCatalogViewVisible, "inference stage should hide class catalog");
        AssertTrue(!shellViewModel.IsYoloModelCenterViewVisible, "inference stage should hide model center");
        AssertTrue(!shellViewModel.IsWorkflowStageModelActionPanelVisible, "inference stage should hide the top model-action panel");
        AssertTrue(!shellViewModel.IsRightWorkflowSubNavigationVisible, "inference stage should hide subnavigation because AI candidates are the only right-side view");
        AssertTrue(shellViewModel.IsRightWorkflowShortcutBarVisible, "inference stage should show the top workflow subnavigation");
        AssertTrue(shellViewModel.IsRightWorkflowDockExpanded, "inference stage should expand the right workflow panel for candidate review");
        AssertTrue(!shellViewModel.IsRightWorkflowDockRailVisible, "inference stage should hide the collapsed right workflow rail");
        AssertEqual(340D, shellViewModel.RightWorkflowPaneGridLength.Value);
        AssertTrue(!shellViewModel.IsSavedLabelsShortcutActive, "inference stage should clear saved-label shortcut active state");
        AssertTrue(!shellViewModel.IsLabelingGuideShortcutActive, "inference stage should clear guide shortcut active state");
        AssertTrue(!shellViewModel.IsClassCatalogShortcutActive, "inference stage should clear class shortcut active state");
        AssertEqual("3/4 AI 후보(선택)", shellViewModel.WorkflowStageProgressText);
        AssertEqual("AI 후보 검토", shellViewModel.RightWorkflowViewTitleText);
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("모델 후보 검토용", StringComparison.Ordinal), "inference right workflow detail should explain when the candidate panel is used");
        AssertTrue(shellViewModel.RightWorkflowViewDetailText.Contains("건너", StringComparison.Ordinal), "inference right workflow detail should identify manual-label-only use as skippable");
        AssertEqual("AI 후보", shellViewModel.RightWorkflowRailCurrentViewText);
        AssertTrue(shellViewModel.WorkflowStageDetailText.Contains("자동 라벨", StringComparison.Ordinal), "inference workflow stage should explain the automatic-label candidate role");
        AssertTrue(shellViewModel.WorkflowStageDetailText.Contains("건너", StringComparison.Ordinal), "inference workflow stage should identify manual-label-only use as skippable");
        AssertTrue(shellViewModel.WorkflowStageNextActionText.Contains("후보가 없으면", StringComparison.Ordinal), "inference workflow next action should explain the empty-candidate route");
        AssertTrue(shellViewModel.WorkflowStageNextActionText.Contains("학습/모델", StringComparison.Ordinal), "inference workflow next action should explain the route after all images are complete");
        shellViewModel.SetWorkflowStage(WpfShellWorkflowStage.TrainingModel);
        AssertTrue(shellViewModel.IsTrainingModelStageActive, "training/model workflow stage should become active");
        AssertTrue(!shellViewModel.IsInferenceStageActive, "inference workflow stage should become inactive");
        AssertTrue(!shellViewModel.IsSavedLabelsViewVisible, "training/model stage should hide saved-label review");
        AssertTrue(!shellViewModel.IsCandidateReviewViewVisible, "training/model stage should hide AI-candidate review");
        AssertTrue(!shellViewModel.IsGuideToolsViewVisible, "training/model stage should hide guide/tools");
        AssertTrue(!shellViewModel.IsClassCatalogViewVisible, "training/model stage should hide class catalog");
        AssertTrue(shellViewModel.IsYoloModelCenterViewVisible, "training/model stage should show model center");
        AssertTrue(!shellViewModel.IsWorkflowStageModelActionPanelVisible, "training/model stage should hide the top model-action panel while the model center is open");
        AssertTrue(!shellViewModel.IsRightWorkflowSubNavigationVisible, "training/model stage should hide subnavigation because the model center is the only right-side view");
        AssertTrue(shellViewModel.IsRightWorkflowShortcutBarVisible, "training/model stage should show the top workflow subnavigation");
        AssertTrue(shellViewModel.IsRightWorkflowDockExpanded, "training/model stage should expand the right workflow panel for model center");
        AssertTrue(!shellViewModel.IsRightWorkflowDockRailVisible, "training/model stage should hide the collapsed right workflow rail");
        AssertTrue(shellViewModel.IsModelWorkspaceActive, "training/model stage should activate the dedicated model workspace");
        AssertEqual(System.Windows.GridUnitType.Star, shellViewModel.RightWorkflowPaneGridLength.GridUnitType);
        AssertTrue(!shellViewModel.IsCanvasWorkspaceVisible, "training/model stage should hide the annotation canvas");
        AssertTrue(!shellViewModel.IsImageQueueWorkspaceVisible, "training/model stage should hide the image queue");
        AssertTrue(!shellViewModel.IsWorkspaceSplitterVisible, "training/model stage should hide unused workspace splitters");
        AssertTrue(!shellViewModel.IsRightWorkflowDockToggleVisible, "training/model stage should not offer a dock toggle that cannot apply");
        AssertEqual(0D, shellViewModel.ImageQueuePaneGridLength.Value);
        AssertEqual("4/4 학습/모델", shellViewModel.WorkflowStageProgressText);
        AssertEqual("\uBAA8\uB378", shellViewModel.RightWorkflowRailCurrentViewText);
        AssertTrue(shellViewModel.WorkflowStageDetailText.Contains("best.pt", StringComparison.Ordinal), "training/model workflow stage should mention model candidate checking");
        shellViewModel.SetWorkflowModeState(isInferenceMode: true, canSwitchMode: false);
        AssertTrue(!shellViewModel.IsLabelingModeActive, "labeling mode should become inactive after switching to inference");
        AssertTrue(shellViewModel.IsInferenceModeActive, "inference mode should become active");
        AssertTrue(!shellViewModel.IsLabelingModeButtonEnabled, "inactive labeling button should lock while detection is busy");
        AssertTrue(shellViewModel.IsInferenceModeButtonEnabled, "active inference button should stay enabled while detection is busy");
        shellViewModel.SetWorkflowModeState(isInferenceMode: false, canSwitchMode: false);
        AssertTrue(shellViewModel.IsLabelingModeActive, "labeling mode should become active again");
        AssertTrue(shellViewModel.IsLabelingModeButtonEnabled, "active labeling button should stay enabled while busy");
        AssertTrue(!shellViewModel.IsInferenceModeButtonEnabled, "inactive inference button should lock while labeling-side work is busy");
        shellViewModel.SetWorkflowModeState(isInferenceMode: false, canSwitchMode: true);
        AssertTrue(shellViewModel.IsInferenceModeButtonEnabled, "inactive inference button should be switchable when idle");
        AssertTrue(!queueViewModel.IsOpenSelectedImageEnabled, "queue selected image open should start disabled");
        queueViewModel.SetSelectedImageAvailability(true);
        AssertTrue(queueViewModel.IsOpenSelectedImageEnabled, "queue selected image open should enable when a valid image row is selected");
        queueViewModel.SetSelectedImageAvailability(false);
        AssertTrue(!queueViewModel.IsOpenSelectedImageEnabled, "queue selected image open should disable when selection is cleared or invalid");
        AssertEqual("\uC774\uBBF8\uC9C0 \uC120\uD0DD", queueViewModel.CurrentImageTaskTitleText);
        AssertEqual("\uB300\uAE30", queueViewModel.CurrentImageTaskBadgeText);
        var currentTaskItem = WpfImageQueueItem.CreateShell(Path.Combine(Path.GetTempPath(), "current-task-image.jpg"));
        currentTaskItem.LabelStatus = "\uC5C6\uC74C";
        currentTaskItem.DetectStatus = "\uB300\uAE30";
        currentTaskItem.QueueStatusSummary = "\uC800\uC7A5 \uC5C6\uC74C / AI \uB300\uAE30";
        queueViewModel.SelectedQueueItem = currentTaskItem;
        AssertEqual("\uB77C\uBCA8 \uC791\uC5C5 \uD544\uC694", queueViewModel.CurrentImageTaskTitleText);
        AssertEqual("\uC791\uC5C5", queueViewModel.CurrentImageTaskBadgeText);
        AssertEqual("NeedsLabel", queueViewModel.CurrentImageTaskKey);
        AssertTrue(queueViewModel.CurrentImageTaskDetailText.Contains("\uB77C\uBCA8\uC744 \uB9CC\uB4E0 \uB4A4", StringComparison.Ordinal), "queue current-task guidance should fit box, polygon, and brush labeling");
        AssertTrue(!queueViewModel.CurrentImageTaskDetailText.Contains("\uBC15\uC2A4", StringComparison.Ordinal), "queue current-task guidance should not force box wording for segmentation datasets");
        currentTaskItem.QueueBadgeText = "AI 2";
        currentTaskItem.QueueStatusSummary = "AI \uD6C4\uBCF4 2\uAC1C \uAC80\uD1A0 \uD544\uC694";
        currentTaskItem.ReviewState = YoloImageReviewState.Candidate;
        AssertEqual("AI \uD6C4\uBCF4 \uAC80\uD1A0", queueViewModel.CurrentImageTaskTitleText);
        AssertEqual("AI 2", queueViewModel.CurrentImageTaskBadgeText);
        AssertEqual("Candidate", queueViewModel.CurrentImageTaskKey);
        AssertTrue(queueViewModel.CurrentImageTaskToolTip.Contains(currentTaskItem.FileName, StringComparison.Ordinal), "current-image task tooltip should keep the full filename");
        AssertTrue(queueViewModel.CurrentImageTaskToolTip.Contains("\uD6C4\uBCF4\uB97C \uD655\uC815", StringComparison.Ordinal), "current-image task tooltip should include the full operator action, not only the clipped status line");
        AssertTrue(queueViewModel.CurrentImageTaskDetailText.Contains("\uC790\uB3D9 \uBC18\uC601", StringComparison.Ordinal), "candidate current-image task should explain that confirmed candidates are applied to saved labels automatically");
        AssertTrue(!queueViewModel.CurrentImageTaskDetailText.Contains("\uB77C\uBCA8\uC744 \uC800\uC7A5", StringComparison.Ordinal), "candidate current-image task should not imply a manual save after candidate confirmation");
        AssertTrue(queueViewModel.CurrentImageTaskToolTip.Contains("AI \uD6C4\uBCF4 2\uAC1C", StringComparison.Ordinal), "current-image task tooltip should include the queue status summary");
        currentTaskItem.QueueBadgeText = "\uC800\uC7A5 \uD544\uC694";
        currentTaskItem.QueueStatusSummary = "\uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694: \uD074\uB798\uC2A4 \uBCC0\uACBD";
        currentTaskItem.IsSaveRequired = true;
        AssertEqual("\uB77C\uBCA8 \uC800\uC7A5 \uD544\uC694", queueViewModel.CurrentImageTaskTitleText);
        AssertEqual("\uC800\uC7A5 \uD544\uC694", queueViewModel.CurrentImageTaskBadgeText);
        AssertEqual("SaveRequired", queueViewModel.CurrentImageTaskKey);
        currentTaskItem.IsSaveRequired = false;
        currentTaskItem.QueueBadgeText = "\uC800\uC7A5";
        currentTaskItem.ReviewState = YoloImageReviewState.Confirmed;
        AssertEqual("\uB77C\uBCA8 \uC800\uC7A5 \uC644\uB8CC", queueViewModel.CurrentImageTaskTitleText);
        AssertTrue(queueViewModel.CurrentImageTaskDetailText.Contains("\uBBF8\uC644\uB8CC", StringComparison.Ordinal), "saved current-image card should send the operator toward next unfinished work");
        AssertEqual("\uC800\uC7A5", queueViewModel.CurrentImageTaskBadgeText);
        AssertEqual("Saved", queueViewModel.CurrentImageTaskKey);
        currentTaskItem.ReviewState = YoloImageReviewState.NoCandidate;
        AssertEqual("\uAC1D\uCCB4 \uC5C6\uC74C \uC644\uB8CC", queueViewModel.CurrentImageTaskTitleText);
        AssertTrue(queueViewModel.CurrentImageTaskDetailText.Contains("\uBBF8\uC644\uB8CC", StringComparison.Ordinal), "no-object current-image card should send the operator toward next unfinished work");
        AssertEqual("\uAC1D\uCCB4\uC5C6\uC74C", queueViewModel.CurrentImageTaskBadgeText);
        AssertEqual("Saved", queueViewModel.CurrentImageTaskKey);
        queueViewModel.SelectedQueueItem = null;
        AssertEqual("\uC774\uBBF8\uC9C0 \uC120\uD0DD", queueViewModel.CurrentImageTaskTitleText);
        AssertEqual("\uB2E4\uC74C \uBBF8\uC644\uB8CC", queueViewModel.NextUnlabeledActionText);
        AssertTrue(queueViewModel.NextUnlabeledToolTip.Contains("\uC800\uC7A5\uB428", StringComparison.Ordinal)
            && queueViewModel.NextUnlabeledToolTip.Contains("\uAC1D\uCCB4\uC5C6\uC74C", StringComparison.Ordinal),
            "queue next action should explain that saved and no-object images are skipped");
        queueViewModel.SetQuickFilterState(WpfImageQueueFilter.Candidate, 2, 1, 3, 4, 5, 6);
        AssertEqual("6\uC7A5 \uBCF4\uAE30", queueViewModel.QueueFilterUnfinishedText);
        AssertEqual("\uC804\uCCB4", queueViewModel.QueueFilterAllText);
        AssertEqual("AI \uD6C4\uBCF4 2", queueViewModel.QueueFilterCandidateText);
        AssertEqual("\uC2E4\uD328 1", queueViewModel.QueueFilterFailedText);
        AssertEqual("\uC800\uC7A5\uB428 3", queueViewModel.QueueFilterConfirmedText);
        AssertEqual("\uC228\uAE40 4", queueViewModel.QueueFilterSkippedText);
        AssertEqual("\uAC1D\uCCB4\uC5C6\uC74C 5", queueViewModel.QueueFilterNoCandidateText);
        AssertTrue(!queueViewModel.IsQueueFilterUnfinishedActive, "queue unfinished quick filter should stay inactive when candidate filter is selected");
        AssertTrue(!queueViewModel.IsQueueFilterAllActive, "queue all quick filter should become inactive when candidate filter is selected");
        AssertTrue(queueViewModel.IsQueueFilterCandidateActive, "queue candidate quick filter should become active when selected");
        AssertTrue(!queueViewModel.IsQueueFilterFailedActive, "queue failed quick filter should stay inactive when candidate filter is selected");
        shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: false,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        queueViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: false,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!shellViewModel.IsCurrentImageDetectionEnabled, "shell detect should stay disabled in labeling mode");
        AssertTrue(!queueViewModel.IsDetectSelectedEnabled, "queue selected detect should stay disabled in labeling mode");
        AssertTrue(!queueViewModel.IsBatchDetectEnabled, "queue batch detect should stay disabled in labeling mode");
        AssertTrue(!queueViewModel.IsRetryFailedEnabled, "queue retry should stay disabled in labeling mode");
        AssertTrue(!queueViewModel.IsStopBatchEnabled, "queue stop should stay disabled while batch detection is idle");

        shellViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        queueViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(shellViewModel.IsCurrentImageDetectionEnabled, "shell detect should enable in idle inference mode");
        AssertTrue(queueViewModel.IsDetectSelectedEnabled, "queue selected detect should enable in idle inference mode");
        AssertTrue(queueViewModel.IsBatchDetectEnabled, "queue batch detect should enable in idle inference mode");
        AssertTrue(queueViewModel.IsRetryFailedEnabled, "queue retry should enable in idle inference mode");

        queueViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: true,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!queueViewModel.IsBatchDetectEnabled, "queue batch detect should disable while batch detection is running");
        AssertTrue(queueViewModel.IsStopBatchEnabled, "queue stop should enable while batch detection is running");

        var canvasViewModel = new WpfCanvasPanelViewModel();
        canvasViewModel.SetCommandAvailability(hasImage: false, hasSelectedCandidate: false, hasPendingCandidates: false);
        AssertTrue(!canvasViewModel.IsFitEnabled, "canvas fit should stay disabled without an image");
        AssertTrue(!canvasViewModel.IsActualSizeEnabled, "canvas actual-size should stay disabled without an image");
        AssertTrue(!canvasViewModel.IsPanEnabled, "canvas pan should stay disabled without an image");
        AssertTrue(!canvasViewModel.IsFocusCandidateEnabled, "canvas candidate focus should stay disabled without an image");
        AssertTrue(!canvasViewModel.IsResetAiOverlayEnabled, "canvas AI reset should stay disabled without an image");
        canvasViewModel.SetCommandAvailability(hasImage: true, hasSelectedCandidate: false, hasPendingCandidates: true);
        AssertTrue(canvasViewModel.IsFitEnabled, "canvas fit should enable when an image is loaded");
        AssertTrue(canvasViewModel.IsActualSizeEnabled, "canvas actual-size should enable when an image is loaded");
        AssertTrue(canvasViewModel.IsPanEnabled, "canvas pan should enable when an image is loaded");
        AssertTrue(!canvasViewModel.IsFocusCandidateEnabled, "canvas candidate focus should require a selected candidate");
        AssertTrue(canvasViewModel.IsResetAiOverlayEnabled, "canvas AI reset should enable when pending candidates exist");
        canvasViewModel.SetCommandAvailability(hasImage: true, hasSelectedCandidate: true, hasPendingCandidates: false);
        AssertTrue(canvasViewModel.IsFocusCandidateEnabled, "canvas candidate focus should enable for a selected candidate");
        AssertTrue(!canvasViewModel.IsResetAiOverlayEnabled, "canvas AI reset should disable when no pending candidates exist");
        int canvasCandidateCommandCount = 0;
        canvasViewModel.ConfigureCandidateReviewCommands(
            () => canvasCandidateCommandCount++,
            () => canvasCandidateCommandCount++,
            () => canvasCandidateCommandCount++,
            () => canvasCandidateCommandCount++,
            () => canvasCandidateCommandCount++);
        canvasViewModel.SetCandidateReviewState(
            canNavigatePrevious: true,
            canNavigateNext: true,
            canFocusCurrentLabel: true,
            canConfirmSelected: true,
            canSkipSelected: true);
        AssertTrue(canvasViewModel.IsPreviousCandidateEnabled, "canvas detection result card should expose previous candidate state");
        AssertTrue(canvasViewModel.IsNextCandidateEnabled, "canvas detection result card should expose next candidate state");
        AssertTrue(canvasViewModel.IsFocusCurrentLabelEnabled, "canvas detection result card should expose current-label focus state");
        AssertTrue(canvasViewModel.IsConfirmSelectedEnabled, "canvas detection result card should expose confirm state");
        AssertTrue(canvasViewModel.IsSkipSelectedEnabled, "canvas detection result card should expose skip state");
        canvasViewModel.NextCandidateCommand.Execute(null);
        canvasViewModel.ConfirmSelectedCommand.Execute(null);
        AssertEqual(2, canvasCandidateCommandCount);

        var projectViewModel = new WpfProjectConfigPanelViewModel();
        projectViewModel.LoadFrom("MainRecipe", @"C:\App\RECIPE");
        AssertEqual("MainRecipe", projectViewModel.RecipeName);
        AssertEqual(@"C:\App\RECIPE\MainRecipe\VISION.xml", projectViewModel.ConfigPath);
        AssertEqual(@"C:\App\RECIPE\MainRecipe\dataset.manifest.json", projectViewModel.ManifestPath);
        projectViewModel.SetRecipeList(new[] { "Beta", "Alpha" }, "Beta");
        AssertEqual(2, projectViewModel.RecipeNames.Count);
        AssertEqual("Beta", projectViewModel.SelectedRecipeName);
        projectViewModel.SelectRecipeFromList("Alpha");
        AssertEqual("Alpha", projectViewModel.RecipeName);
        AssertEqual(@"C:\App\RECIPE\Alpha\VISION.xml", projectViewModel.ConfigPath);
        AssertEqual(@"C:\App\RECIPE\Alpha\dataset.manifest.json", projectViewModel.ManifestPath);
        projectViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(projectViewModel.IsApplyRecipeEnabled, "project apply should be enabled while idle");
        AssertTrue(projectViewModel.IsRefreshRecipeListEnabled, "project recipe refresh should be enabled while idle");
        AssertTrue(projectViewModel.IsSaveProjectConfigEnabled, "project config save should be enabled when a recipe is active");
        AssertTrue(projectViewModel.IsOpenProjectConfigFolderEnabled, "project config folder should be enabled while idle");
        projectViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: true,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!projectViewModel.IsApplyRecipeEnabled, "project apply should disable while busy");
        AssertTrue(!projectViewModel.IsRefreshRecipeListEnabled, "project recipe refresh should disable while busy");
        AssertTrue(!projectViewModel.IsSaveProjectConfigEnabled, "project config save should disable while busy");
        AssertTrue(!projectViewModel.IsOpenProjectConfigFolderEnabled, "project config folder should disable while busy");

        var classViewModel = new WpfClassCatalogPanelViewModel();
        classViewModel.LoadOutputRoot(@"C:\Dataset\Output");
        classViewModel.SetClasses(new[]
        {
            new CClassItem { Text = "OK", DrawColor = Color.Green },
            new CClassItem { Text = "NG", DrawColor = Color.Red }
        }, "NG");
        AssertEqual(@"C:\Dataset\Output", classViewModel.OutputRootPath);
        AssertTrue(classViewModel.ColorPresets.Count >= 4, "class catalog should expose practical color presets");
        AssertEqual(2, classViewModel.Classes.Count);
        AssertEqual("NG", classViewModel.SelectedClass.Text);
        AssertEqual("NG", classViewModel.ClassName);
        AssertTrue(classViewModel.SelectedColorPreset != null, "class catalog should select a color preset for a matching selected class");
        classViewModel.SelectClass("OK");
        AssertEqual("OK", classViewModel.SelectedClass.Text);
        AssertEqual("OK", classViewModel.ClassName);
        classViewModel.ClearClassName();
        AssertEqual(string.Empty, classViewModel.ClassName);
        classViewModel.StatusText = "Ready";
        AssertEqual("Ready", classViewModel.StatusText);

        var yoloStatusViewModel = new WpfYoloStatusPanelViewModel();
        yoloStatusViewModel.SetSettingsStatus("Ready", "Python: OK");
        AssertEqual("Ready", yoloStatusViewModel.SummaryText);
        AssertEqual("Python: OK", yoloStatusViewModel.DetailText);
        yoloStatusViewModel.SetCommandStatus("Running", isBusy: true);
        AssertEqual("Running", yoloStatusViewModel.CommandStatusText);
        AssertEqual(System.Windows.Visibility.Visible, yoloStatusViewModel.CommandProgressVisibility);
        AssertTrue(yoloStatusViewModel.CommandProgressIsIndeterminate, "YOLO command progress should be indeterminate while busy");
        yoloStatusViewModel.SetCommandStatus("", isBusy: false);
        AssertEqual("\uBAA8\uB378 \uC2E4\uD589\uAE30 \uB300\uAE30", yoloStatusViewModel.CommandStatusText);
        AssertEqual(System.Windows.Visibility.Collapsed, yoloStatusViewModel.CommandProgressVisibility);
        AssertTrue(!yoloStatusViewModel.CommandProgressIsIndeterminate, "model runtime command progress should stop when idle");
        AssertEqual(0D, yoloStatusViewModel.CommandProgressValue);
        yoloStatusViewModel.SetRecoveryState("Worker failed", "No heartbeat", "\uBAA8\uB378 \uC2E4\uD589\uAE30 \uC7AC\uC2DC\uC791");
        AssertTrue(yoloStatusViewModel.IsRecoveryVisible, "model runtime status recovery card should become visible when recovery guidance is set");
        AssertEqual("Worker failed", yoloStatusViewModel.RecoveryTitleText);
        AssertEqual("No heartbeat", yoloStatusViewModel.RecoveryDetailText);
        AssertEqual("\uBAA8\uB378 \uC2E4\uD589\uAE30 \uC7AC\uC2DC\uC791", yoloStatusViewModel.RecoveryActionText);
        yoloStatusViewModel.ClearRecoveryState();
        AssertTrue(!yoloStatusViewModel.IsRecoveryVisible, "YOLO status recovery card should hide after recovery guidance is cleared");
        yoloStatusViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: true,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(!yoloStatusViewModel.IsFirstCheckEnabled, "YOLO first-check should disable through the status ViewModel while busy");
        AssertTrue(!yoloStatusViewModel.IsInstallRequirementsEnabled, "YOLO install should disable through the status ViewModel while busy");
        AssertTrue(!yoloStatusViewModel.IsRunSmokeEnabled, "YOLO smoke should disable through the status ViewModel while busy");
        AssertTrue(!yoloStatusViewModel.IsRestartWorkerEnabled, "YOLO restart should disable through the status ViewModel while busy");
        AssertTrue(!yoloStatusViewModel.IsStopWorkerEnabled, "YOLO stop-worker should disable through the status ViewModel while busy");
        yoloStatusViewModel.ApplyWorkflowCommandState(WpfWorkflowCommandStateService.Build(
            isInferenceMode: true,
            isYoloEnvironmentCommandRunning: false,
            isDetecting: false,
            isBatchDetectionRunning: false,
            isTrainingCommandRunning: false,
            isTrainingStopAvailable: false,
            hasCurrentRecipeName: true));
        AssertTrue(yoloStatusViewModel.IsFirstCheckEnabled, "YOLO first-check should re-enable through the status ViewModel when idle");
        AssertTrue(!string.IsNullOrWhiteSpace(projectViewModel.StatusText), "recipe selection should guide the operator to apply explicitly");
        AssertTrue(WpfProjectRecipeService.BuildManifestPreviewPath(@"C:\App\RECIPE", string.Empty).EndsWith(@"\dataset.manifest.json", StringComparison.Ordinal), "project recipe service should preview the dataset manifest path");
        AssertEqual(@"C:\App\RECIPE\(recipe 선택 필요)\VISION.xml", WpfProjectRecipeService.BuildConfigPreviewPath(@"C:\App\RECIPE", string.Empty));
    }
}
