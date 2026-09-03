using MvcVisionSystem._1._Core;
using MvcVisionSystem._3._Communication.TCP;
using MahApps.Metro.IconPacks;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void NotifyYoloPathSelected(string label, string selectedPath)
        {
            SetYoloCommandStatus($"{label} 선택됨. 저장을 눌러 설정에 반영하세요.", isBusy: false);
            AppendLog($"{label} 선택: {selectedPath}");
        }

        private void RefreshYoloStatus()
        {
            global.Data.ProjectSettings ??= new LabelingProjectSettings();
            global.Data.ProjectSettings.PythonModel ??= new PythonModelSettings();
            PythonModelSettings settings = global.Data.ProjectSettings.PythonModel;
            RefreshModelCenterDashboard();
            PythonModelRuntimeState runtimeState = GetPythonModelRuntimeState();
            if (runtimeState.State == PythonModelRuntimeStateKind.NotInstalled)
            {
                WpfModelRuntimeUnavailablePresentation presentation =
                    WpfModelRuntimeUnavailablePresentationService.Build(runtimeState);
                SetGlobalInferenceStatus(string.Empty, isBusy: false, isWarning: true);
                SetPythonStatus(runtimeState.SummaryText);
                SetInspectionModelStatus(presentation.InspectionStatusText, presentation.InspectionStatusToolTip);
                SetModelStatus(presentation.ModelStatusText);
                SetYoloCommandStatus(presentation.CommandStatusText, isBusy: false);
                ApplyModelRuntimeUnavailablePresentation(presentation);
                return;
            }

            PythonModelValidationResult result = PythonModelSettingsValidator.Validate(settings, requireWeights: true);
            SetGlobalInferenceStatus(string.Empty, isBusy: false, isWarning: !result.IsValid);
            SetPythonStatus(WpfInferenceStatusPresentationService.BuildRuntimePythonStatus(result, runtimeState));

            string weightsPath = settings.WeightsPath;
            if (!File.Exists(weightsPath))
            {
                string inspectionModelStatus = WpfInferenceStatusPresentationService.BuildInspectionModelStatusText(settings, hasPendingTrainingWeightsRecipeSave);
                SetInspectionModelStatus(
                    inspectionModelStatus,
                    WpfInferenceStatusPresentationService.BuildInspectionModelToolTip(settings, hasPendingTrainingWeightsRecipeSave));
                SetModelStatus(inspectionModelStatus);
                return;
            }

            SetInspectionModelStatus(
                WpfInferenceStatusPresentationService.BuildInspectionModelStatusText(settings, hasPendingTrainingWeightsRecipeSave),
                WpfInferenceStatusPresentationService.BuildInspectionModelToolTip(settings, hasPendingTrainingWeightsRecipeSave));
            SetModelStatus(WpfInferenceStatusPresentationService.BuildInspectionModelStatusText(settings, hasPendingTrainingWeightsRecipeSave));
        }

        private PythonModelRuntimeState GetPythonModelRuntimeState()
        {
            global.Data.ProjectSettings ??= new LabelingProjectSettings();
            global.Data.ProjectSettings.PythonModel ??= new PythonModelSettings();
            PythonCommunicationStatus status = global.GetPythonCommunicationStatusSnapshot();
            return PythonModelSettingsValidator.GetRuntimeState(
                global.Data.ProjectSettings.PythonModel,
                status?.WorkerSupportedModels,
                status?.WorkerTrainingModels,
                status?.WorkerDetectionModels);
        }

        private bool EnsureModelRuntimeForTraining()
        {
            if (TryAutoConnectAnomalyTrainingRuntime())
            {
                return true;
            }

            PythonModelRuntimeState runtimeState = GetPythonModelRuntimeState();
            if (runtimeState.CanRunTraining)
            {
                return true;
            }

            ShowModelRuntimeUnavailable(
                "\uD559\uC2B5 \uC2DC\uC791 \uB300\uAE30: \uBAA8\uB378 \uC2E4\uD589\uAE30 \uC124\uCE58 \uB610\uB294 \uACBD\uB85C \uC5F0\uACB0 \uD544\uC694",
                runtimeState);
            return false;
        }

        private bool TryAutoConnectAnomalyTrainingRuntime()
        {
            if (global?.Data?.ProjectSettings?.DatasetPurpose != LabelingDatasetPurpose.AnomalyDetection)
            {
                return false;
            }

            global.Data.ProjectSettings.PythonModel ??= new PythonModelSettings();
            if (!PythonModelRuntimeConnectionService.TryBuildAnomalyTrainingConnection(
                    global.Data.ProjectSettings.PythonModel,
                    out PythonModelRuntimeConnectionResult result))
            {
                return false;
            }

            global.Data.ProjectSettings.PythonModel = result.Settings;
            YoloModelSettingsViewModel?.LoadFrom(
                result.Settings,
                global.Data.ProjectSettings.AnomalyClassification);
            TrainingSettingsViewModel?.ApplyModelEngineSelection(result.Settings.ModelEngine);
            SaveProjectConfigFromPanel();
            SetYoloCommandStatus(
                "이상탐지 분류 학습에 맞는 YOLOv8 실행기를 자동 연결했습니다.",
                isBusy: false);
            AppendLog($"이상탐지 학습 실행기 자동 연결: {result.Settings.ProjectRootPath}");
            return true;
        }

        private bool EnsureModelRuntimeForInference()
        {
            PythonModelRuntimeState runtimeState = GetPythonModelRuntimeState();
            if (runtimeState.CanRunInference)
            {
                return true;
            }

            ShowModelRuntimeUnavailable(
                "\uD604\uC7AC \uAC80\uC0AC \uB300\uAE30: \uAC80\uC0AC \uBAA8\uB378 \uD30C\uC77C \uD544\uC694",
                runtimeState);
            return false;
        }

        private void ShowModelRuntimeUnavailable(string statusText, PythonModelRuntimeState runtimeState)
        {
            runtimeState ??= GetPythonModelRuntimeState();
            WpfModelRuntimeUnavailablePresentation presentation =
                WpfModelRuntimeUnavailablePresentationService.Build(runtimeState, statusText);

            SetYoloCommandStatus(presentation.CommandStatusText, isBusy: false);
            SetYoloRecoveryStatus(presentation.RecoveryTitle, presentation.RecoveryDetail, presentation.RecoveryAction);
            SetTrainingReadinessStatus(presentation.ReadinessText);
            SetPythonStatus(runtimeState.SummaryText);
            ApplyModelRuntimeUnavailablePresentation(presentation);
            AppendLog(presentation.LogText);
        }

        private void ApplyModelRuntimeUnavailablePresentation(PythonModelRuntimeState runtimeState, string statusText = null)
        {
            runtimeState ??= GetPythonModelRuntimeState();
            WpfModelRuntimeUnavailablePresentation presentation =
                WpfModelRuntimeUnavailablePresentationService.Build(runtimeState, statusText);
            ApplyModelRuntimeUnavailablePresentation(presentation);
        }

        private void ApplyModelRuntimeUnavailablePresentation(WpfModelRuntimeUnavailablePresentation presentation)
        {
            if (presentation == null)
            {
                return;
            }

            SetTrainingReadinessStatus(presentation.ReadinessText);
            SetTrainingProgressStatus(WpfTrainingProgressPresentationService.BuildIdleProgressText(), string.Empty, 0D, isIndeterminate: false);
            LearningWorkflowViewModel?.SetTrainingModelLifecycleState(
                presentation.CurrentModelText,
                presentation.CandidateModelText,
                presentation.AdoptionText,
                presentation.NextActionText);
            ShellViewModel?.SetModelCenterModelState(
                presentation.CurrentModelText,
                presentation.CandidateModelText,
                presentation.AdoptionText,
                presentation.NextActionText,
                presentation.NoCandidateText,
                presentation.CandidateReviewDetailText,
                canConfirmModel: false,
                presentation.DecisionTitleText,
                presentation.DecisionEvidenceText,
                presentation.NextActionText);
            ShellViewModel?.SetModelCenterCandidateReviewState(
                presentation.NoCandidateText,
                presentation.CandidateReviewDetailText,
                canReviewCandidate: false);
            ShellViewModel?.SetModelRegistryState(new WpfModelRegistryPresentation
            {
                ProfileText = presentation.ProfileText,
                TrainingRunText = presentation.TrainingRunText,
                CandidateModelText = presentation.CandidateModelText,
                InspectionModelText = presentation.CurrentModelText,
                ActionText = presentation.NextActionText,
                SummaryPrimaryText = presentation.SummaryPrimaryText,
                SummarySecondaryText = presentation.SummarySecondaryText,
                HistoryItems = new WpfModelRegistryHistoryItem[0]
            });
            ShellViewModel?.SetModelCenterRecoveryState(
                presentation.RecoveryTitle,
                presentation.RecoveryDetail,
                presentation.NextActionText);
            TrainingSettingsViewModel?.SetPostTrainingModelActionState(
                presentation.CurrentModelText,
                presentation.CandidateModelText,
                presentation.AdoptionText,
                presentation.NextActionText,
                presentation.NoCandidateText,
                presentation.CandidateReviewDetailText,
                canReview: false,
                presentation.NoCandidateText,
                presentation.CandidateReviewDetailText,
                canConfirm: false);
        }

        private void SaveYoloEditorFields()
        {
            EnsureProjectSettings();
            PythonModelSettings settings = global.Data.ProjectSettings.PythonModel;
            YoloModelSettingsViewModel?.ApplyTo(settings);
            YoloModelSettingsViewModel?.ApplyTo(global.Data.ProjectSettings.AnomalyClassification);
            CandidateConfidenceSlider.Value = System.Math.Clamp(settings.MinimumDetectionConfidence, 0F, 1F);
        }

        private void SaveTrainingEditorFields()
        {
            EnsureProjectSettings();
            TrainingSettings training = global.Data.ProjectSettings.Training;
            TrainingSettingsViewModel?.ApplyTo(training, global.Data.ProjectSettings.YoloDataset, global.Data.TrainingParam);
        }

        private void RefreshCandidateConfidenceFilterFromAppliedSettings()
        {
            if (CandidateConfidenceSlider == null)
            {
                return;
            }

            CandidateConfidenceSlider.Value = System.Math.Clamp(
                global.Data.ProjectSettings.PythonModel.MinimumDetectionConfidence,
                0F,
                1F);
            UpdateCandidateConfidenceText();
        }

        // Settings-panel capability/package checks and runtime status labels
        // share one YOLO status owner; the async check remains distinct from
        // the cheap label refresh inside this owner.
        private async Task RefreshYoloSettingsPanelAsync(PythonModelValidationResult validation = null)
        {
            global.Data.ProjectSettings ??= new LabelingProjectSettings();
            global.Data.ProjectSettings.PythonModel ??= new PythonModelSettings();
            PythonModelSettings settings = global.Data.ProjectSettings.PythonModel;
            PythonCommunicationStatus communicationStatus = global.GetPythonCommunicationStatusSnapshot();
            PythonModelRuntimeState runtimeState = PythonModelSettingsValidator.GetRuntimeState(
                settings,
                communicationStatus?.WorkerSupportedModels,
                communicationStatus?.WorkerTrainingModels,
                communicationStatus?.WorkerDetectionModels);
            validation ??= runtimeState.State == PythonModelRuntimeStateKind.NotInstalled
                ? new PythonModelValidationResult(new[] { runtimeState.NextActionText }, Array.Empty<string>())
                : PythonModelSettingsValidator.Validate(settings, requireWeights: true);

            YoloModelSettingsViewModel?.ApplyRuntimeCapabilities(
                communicationStatus?.WorkerSupportedModels,
                communicationStatus?.WorkerTrainingModels,
                communicationStatus?.WorkerDetectionModels);

            PythonEnvironmentCheckResult environment = null;
            string environmentCheckError = string.Empty;
            if (runtimeState.IsRuntimeInstalled)
            {
                try
                {
                    environment = await PythonEnvironmentService
                        .CheckRequirementsAsync(settings)
                        .ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    environmentCheckError = ex.Message;
                }
            }

            if (isApplicationCloseApproved)
            {
                return;
            }

            string detail = WpfYoloSettingsPanelStatusPresentationService.BuildDetail(
                settings,
                validation,
                runtimeState,
                communicationStatus,
                global.ModelRuntime.PythonClientProcess?.IsRunning == true,
                environment,
                environmentCheckError);

            YoloStatusViewModel.SetSettingsStatus(runtimeState.SummaryText, detail);
        }

        // Inference status text and its cosmetic progress pulse are one
        // runtime-status boundary; the timer remains framework-bound by design.
        private void SetGlobalInferenceStatus(string text, bool isBusy, bool isWarning = false)
        {
            if (InferenceStatusText == null || InferenceStatusBorder == null)
            {
                return;
            }

            string statusText = string.IsNullOrWhiteSpace(text) ? "\uB300\uAE30" : text;
            InferenceStatusText.Text = WpfInferenceStatusPresentationService.BuildStatusText(
                statusText,
                global?.Data?.ProjectSettings?.PythonModel,
                hasPendingTrainingWeightsRecipeSave);
            InferenceStatusBorder.ToolTip = WpfInferenceStatusPresentationService.BuildToolTip(
                statusText,
                global?.Data?.ProjectSettings?.PythonModel,
                hasPendingTrainingWeightsRecipeSave);
            InferenceStatusProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            InferenceStatusProgressBar.IsIndeterminate = false;
            if (isBusy)
            {
                StartInferenceStatusPulse();
            }
            else
            {
                StopInferenceStatusPulse();
            }

            InferenceStatusIcon.Kind = isBusy
                ? PackIconMaterialKind.ProgressClock
                : isWarning
                    ? PackIconMaterialKind.AlertCircleOutline
                    : PackIconMaterialKind.RobotIndustrial;

            InferenceStatusBorder.SetResourceReference(
                System.Windows.Controls.Border.BackgroundProperty,
                isBusy ? "DetectionOverlaySelectedBackgroundBrush" : "ToolbarButtonBrush");
            InferenceStatusBorder.SetResourceReference(
                System.Windows.Controls.Border.BorderBrushProperty,
                isBusy || isWarning ? "AccentBrush" : "BorderBrushDark");
        }

        private void StartInferenceStatusPulse()
        {
            if (InferenceStatusProgressBar == null)
            {
                return;
            }

            if (!inferenceStatusPulseTimer.IsEnabled)
            {
                // The progress is cosmetic: keep it timer-driven so inference work does not force layout updates from hot paths.
                inferenceStatusPulseStopwatch.Restart();
                InferenceStatusProgressBar.Value = 8;
                inferenceStatusPulseTimer.Start();
            }
        }

        private void StopInferenceStatusPulse()
        {
            inferenceStatusPulseTimer.Stop();
            inferenceStatusPulseStopwatch.Reset();
            if (InferenceStatusProgressBar != null)
            {
                InferenceStatusProgressBar.Value = 0;
            }
        }

        private void InferenceStatusPulseTimer_Tick(object sender, EventArgs e)
        {
            if (isApplicationCloseApproved)
            {
                StopInferenceStatusPulse();
                return;
            }

            if (InferenceStatusProgressBar == null || InferenceStatusProgressBar.Visibility != Visibility.Visible)
            {
                StopInferenceStatusPulse();
                return;
            }

            const double cycleMilliseconds = 1400D;
            double elapsed = inferenceStatusPulseStopwatch.Elapsed.TotalMilliseconds;
            double phase = (elapsed % cycleMilliseconds) / cycleMilliseconds;
            InferenceStatusProgressBar.Value = 8D + (phase * 84D);
        }
    }
}
