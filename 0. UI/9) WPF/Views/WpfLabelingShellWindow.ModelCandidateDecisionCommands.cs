using MvcVisionSystem._1._Core;
using System;
using System.IO;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private void ExecuteSaveModelCandidateCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            if (CandidateReviewViewModel?.IsModelPromotionHeld == true)
            {
                string status = WpfModelCandidateDecisionPresentationService.BuildHeldCandidateSaveBlockedStatus();
                SetYoloCommandStatus(status, isBusy: false);
                AppendLog(status);
                return;
            }

            ExecuteSaveYoloSettingsCommand();
        }

        private void ExecuteRejectModelCandidateCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            PythonModelSettings settings = null;
            string candidateWeightsPath = string.Empty;
            string baselineWeightsPath = string.Empty;
            bool candidateDecisionCommitted = false;
            try
            {
                EnsureProjectSettings();
                settings = global.Data.ProjectSettings.PythonModel;
                candidateWeightsPath = settings.WeightsPath?.Trim() ?? string.Empty;
                baselineWeightsPath = pendingTrainingBaselineWeightsPath?.Trim() ?? string.Empty;

                if (!hasPendingTrainingWeightsRecipeSave || string.IsNullOrWhiteSpace(candidateWeightsPath))
                {
                    SetYoloCommandStatus(WpfModelCandidateDecisionPresentationService.BuildNoRejectCandidateStatus(), isBusy: false);
                    UpdateCandidateModelDecisionPanel();
                    return;
                }

                using RecipeSettingsStateTransaction recipeSettingsTransaction = new RecipeSettingsStateTransaction(global.Data);
                using ModelRegistryStateTransaction registryTransaction = new ModelRegistryStateTransaction(global.Data.ProjectSettings.ModelRegistry);
                WpfTrainingWeightsComparison comparison = BuildCurrentTrainingWeightsComparison();
                string decisionSummary = WpfModelCandidateDecisionPresentationService.BuildRejectDecisionSummary();
                ModelRegistryService.RecordCandidateDecision(
                    global.Data.ProjectSettings.ModelRegistry,
                    settings,
                    global.Data.ProjectSettings.DatasetPurpose,
                    global.Data.OutputRootPath,
                    candidateWeightsPath,
                    baselineWeightsPath,
                    WpfTrainingComparisonPresentationService.BuildComparisonStatusText(comparison),
                    ModelRegistryService.CandidateDecisionRejected,
                    decisionSummary,
                    savedToRecipe: false,
                    datasetVersionId: global.Data.ProjectSettings.TrainingGuide.LastTrainingDatasetVersionId,
                    datasetContentSha256: global.Data.ProjectSettings.TrainingGuide.LastTrainingDatasetContentSha256);

                if (!string.IsNullOrWhiteSpace(baselineWeightsPath) && File.Exists(baselineWeightsPath))
                {
                    settings.WeightsPath = baselineWeightsPath;
                    YoloModelSettingsViewModel?.LoadFrom(settings);
                }

                bool configSaved = SaveModelMetadataConfigFromPanel();
                if (configSaved)
                {
                    recipeSettingsTransaction.Commit();
                    registryTransaction.Commit();
                    hasPendingTrainingWeightsRecipeSave = false;
                    pendingTrainingBaselineWeightsPath = string.Empty;
                    candidateDecisionCommitted = true;
                }
                else
                {
                    recipeSettingsTransaction.Rollback();
                    registryTransaction.Rollback();
                    RestorePendingCandidateModelState(settings, candidateWeightsPath, baselineWeightsPath);
                }

                PopulateYoloEditorFields();
                RefreshYoloStatus();
                UpdateYoloTrainingHistoryText();
                RefreshModelCenterDashboard();

                SetYoloCommandStatus(WpfModelCandidateDecisionPresentationService.BuildRejectCommandStatus(candidateWeightsPath, configSaved), isBusy: false);
                SetProjectConfigStatus(WpfModelCandidateDecisionPresentationService.BuildRejectProjectConfigStatus(configSaved));
                AppendLog(WpfModelCandidateDecisionPresentationService.BuildRejectLog(candidateWeightsPath, baselineWeightsPath));
            }
            catch (Exception ex)
            {
                if (!candidateDecisionCommitted)
                {
                    RestorePendingCandidateModelState(settings, candidateWeightsPath, baselineWeightsPath);
                }

                string failureStatus = WpfModelCandidateDecisionPresentationService.BuildRejectFailureStatus(ex.Message);
                SetYoloCommandStatus(failureStatus, isBusy: false);
                AppendLog(failureStatus);
            }
        }

        private void UpdateCandidateModelDecisionPanel(WpfTrainingWeightsComparison comparison = null)
        {
            if (CandidateReviewViewModel == null)
            {
                return;
            }

            EnsureProjectSettings();
            PythonModelSettings settings = global.Data.ProjectSettings.PythonModel;
            comparison ??= BuildCurrentTrainingWeightsComparison();
            string currentWeightsPath = settings.WeightsPath?.Trim() ?? string.Empty;
            string baselineWeightsPath = pendingTrainingBaselineWeightsPath?.Trim() ?? string.Empty;
            ModelCandidate latestCandidate = ModelRegistryService.FindLatestCandidate(global.Data.ProjectSettings.ModelRegistry);
            ApplyModelCandidateDecisionPresentation(
                WpfModelCandidateDecisionPresentationService.Build(new WpfModelCandidateDecisionSnapshot
                {
                    HasPendingRecipeSave = hasPendingTrainingWeightsRecipeSave,
                    IsPromotionHeld = CandidateReviewViewModel.IsModelPromotionHeld,
                    CandidateWeightsPath = currentWeightsPath,
                    BaselineWeightsPath = baselineWeightsPath,
                    CandidateWeightsFileExists = File.Exists(currentWeightsPath),
                    BaselineWeightsFileExists = File.Exists(baselineWeightsPath),
                    HasLatestCandidate = latestCandidate != null,
                    LatestCandidateWeightsPath = latestCandidate?.WeightsPath,
                    LatestCandidateDecision = latestCandidate?.Decision,
                    LatestCandidateDecisionSummary = latestCandidate?.DecisionSummary,
                    LatestCandidateSavedToRecipe = latestCandidate?.SavedToRecipe == true,
                    HasLatestWeights = comparison?.HasLatestWeights == true
                }));
        }

        private void ApplyModelCandidateDecisionPresentation(WpfModelCandidateDecisionPresentation presentation)
        {
            if (CandidateReviewViewModel == null || presentation == null)
            {
                return;
            }

            CandidateReviewViewModel.SetModelCandidateDecisionState(
                presentation.CanSave,
                presentation.CanReject,
                presentation.StatusText,
                presentation.DetailText,
                presentation.SaveToolTip,
                presentation.RejectToolTip);
        }

        // Promoting a registry history item is the other candidate-decision
        // path, so adoption and rejection share one lifecycle owner.
        private void ExecutePromoteSelectedModelHistoryCommand()
        {
            if (isApplicationCloseApproved)
            {
                return;
            }

            PythonModelSettings settings = null;
            string candidateWeightsPath = string.Empty;
            string baselineWeightsPath = string.Empty;
            bool adoptionCommitted = false;
            try
            {
                EnsureProjectSettings();
                WpfModelRegistryHistoryItem selected = ShellViewModel?.SelectedModelRegistryHistoryItem;
                settings = global.Data.ProjectSettings.PythonModel;
                string previousWeightsPath = settings.WeightsPath?.Trim() ?? string.Empty;
                WpfModelHistoryAdoptionPlan plan = WpfModelHistoryAdoptionPlanningService.Build(
                    new WpfModelHistoryAdoptionRequest
                    {
                        HasSelection = selected != null,
                        CandidateWeightsPath = selected?.WeightsPath,
                        CurrentWeightsPath = previousWeightsPath,
                        FallbackBaselineWeightsPath = selected?.BaselineWeightsPath,
                        MetricText = selected?.MetricText,
                        DecisionText = selected?.DecisionText,
                        CandidateWeightsFileExists = selected != null && File.Exists(selected.WeightsPath?.Trim() ?? string.Empty)
                    });

                if (plan.Status == WpfModelHistoryAdoptionPlanStatus.MissingSelection)
                {
                    SetYoloCommandStatus("\uBAA8\uB378 \uC774\uB825\uC744 \uC120\uD0DD\uD558\uC138\uC694.", isBusy: false);
                    return;
                }

                if (plan.Status == WpfModelHistoryAdoptionPlanStatus.MissingWeightsPath)
                {
                    SetModelCenterHistoryApplyFailure(
                        "\uBAA8\uB378 \uC774\uB825 \uC801\uC6A9 \uBD88\uAC00",
                        "\uC120\uD0DD\uD55C \uBAA8\uB378 \uC774\uB825\uC5D0 \uAC00\uC911\uCE58 \uD30C\uC77C \uACBD\uB85C\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.");
                    return;
                }

                if (plan.Status == WpfModelHistoryAdoptionPlanStatus.CandidateWeightsFileMissing)
                {
                    SetModelCenterHistoryApplyFailure(
                        "\uBAA8\uB378 \uC774\uB825 \uC801\uC6A9 \uBD88\uAC00",
                        $"\uC120\uD0DD\uD55C \uBAA8\uB378 \uD30C\uC77C\uC774 \uC5C6\uC2B5\uB2C8\uB2E4: {plan.CandidateWeightsPath}");
                    return;
                }

                candidateWeightsPath = plan.CandidateWeightsPath;
                if (plan.IsAlreadyCurrent)
                {
                    SetYoloCommandStatus($"\uC774\uBBF8 \uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378\uC785\uB2C8\uB2E4: {Path.GetFileName(candidateWeightsPath)}", isBusy: false);
                    RefreshModelCenterDashboard();
                    return;
                }

                baselineWeightsPath = plan.BaselineWeightsPath;
                string decisionSummary = plan.DecisionSummary;
                string metricsSummary = plan.MetricsSummary;
                using RecipeSettingsStateTransaction recipeSettingsTransaction = new RecipeSettingsStateTransaction(global.Data);
                using ModelRegistryStateTransaction registryTransaction = new ModelRegistryStateTransaction(global.Data.ProjectSettings.ModelRegistry);

                settings.WeightsPath = candidateWeightsPath;
                YoloModelSettingsViewModel?.LoadFrom(settings);
                ModelRegistryService.RecordCandidateDecision(
                    global.Data.ProjectSettings.ModelRegistry,
                    settings,
                    global.Data.ProjectSettings.DatasetPurpose,
                    global.Data.OutputRootPath,
                    candidateWeightsPath,
                    baselineWeightsPath,
                    metricsSummary,
                    ModelRegistryService.CandidateDecisionAdopted,
                    decisionSummary,
                    savedToRecipe: true,
                    datasetVersionId: global.Data.ProjectSettings.TrainingGuide.LastTrainingDatasetVersionId,
                    datasetContentSha256: global.Data.ProjectSettings.TrainingGuide.LastTrainingDatasetContentSha256);

                // Model adoption changes Recipe model metadata, not dataset content.
                // Keep the existing dataset version manifest untouched for this save.
                bool configSaved = SaveModelMetadataConfigFromPanel();
                if (configSaved)
                {
                    recipeSettingsTransaction.Commit();
                    registryTransaction.Commit();
                    adoptionCommitted = true;
                    hasPendingTrainingWeightsRecipeSave = false;
                    pendingTrainingBaselineWeightsPath = string.Empty;
                    lastAutoAppliedTrainingWeightsPath = candidateWeightsPath;
                }
                else
                {
                    recipeSettingsTransaction.Rollback();
                    registryTransaction.Rollback();
                    RestorePendingCandidateModelState(settings, candidateWeightsPath, baselineWeightsPath);
                }

                PopulateYoloEditorFields();
                RefreshYoloStatus();
                UpdateYoloTrainingHistoryText();
                RefreshModelCenterDashboard();

                string modelName = Path.GetFileName(candidateWeightsPath);
                if (configSaved)
                {
                    ShellViewModel?.ClearModelCenterRecoveryState();
                    SetModelStatus($"\uD604\uC7AC \uAC80\uC0AC \uBAA8\uB378: {modelName}");
                    SetYoloCommandStatus($"\uBAA8\uB378 \uC774\uB825 \uC801\uC6A9 \uC644\uB8CC: {modelName}. \uB2E4\uC74C \uAC80\uC0AC\uBD80\uD130 \uC774 \uBAA8\uB378\uC744 \uC0AC\uC6A9\uD569\uB2C8\uB2E4.", isBusy: false);
                    AppendLog($"Model history adopted as inspection model: {candidateWeightsPath} / previous={baselineWeightsPath}");
                    return;
                }

                SetModelCenterHistoryApplyFailure(
                    "\uBAA8\uB378 \uC774\uB825 \uC801\uC6A9\uC740 \uBA54\uBAA8\uB9AC\uC5D0\uB9CC \uBC18\uC601\uB428",
                    "\uC120\uD0DD\uD55C \uBAA8\uB378\uC744 recipe\uC5D0 \uC800\uC7A5\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uC800\uC7A5 \uACBD\uB85C\uC640 recipe \uC774\uB984\uC744 \uD655\uC778\uD55C \uB4A4 \uBAA8\uB378 \uC124\uC815\uC744 \uC800\uC7A5\uD558\uC138\uC694.");
            }
            catch (Exception ex)
            {
                if (adoptionCommitted)
                {
                    string refreshFailureStatus = WpfModelCandidateDecisionPresentationService.BuildAdoptionRefreshFailureStatus(ex.Message);
                    SetYoloCommandStatus(refreshFailureStatus, isBusy: false);
                    AppendLog(refreshFailureStatus);
                    return;
                }

                RestorePendingCandidateModelState(settings, candidateWeightsPath, baselineWeightsPath);

                SetModelCenterHistoryApplyFailure(
                    "\uBAA8\uB378 \uC774\uB825 \uC801\uC6A9 \uC2E4\uD328",
                    ex.Message);
            }
        }

        private void RestorePendingCandidateModelState(PythonModelSettings settings, string candidateWeightsPath, string baselineWeightsPath)
        {
            if (settings == null || string.IsNullOrWhiteSpace(candidateWeightsPath))
            {
                return;
            }

            settings.WeightsPath = candidateWeightsPath;
            YoloModelSettingsViewModel?.LoadFrom(settings);
            hasPendingTrainingWeightsRecipeSave = true;
            pendingTrainingBaselineWeightsPath = baselineWeightsPath;
        }

        private void SetModelCenterHistoryApplyFailure(string titleText, string detailText)
        {
            ShellViewModel?.SetModelCenterRecoveryState(
                titleText,
                detailText,
                "\uBAA8\uB378 \uC774\uB825\uC758 \uD30C\uC77C \uACBD\uB85C\uC640 recipe \uC800\uC7A5 \uC0C1\uD0DC\uB97C \uD655\uC778\uD558\uC138\uC694.");
            SetYoloCommandStatus($"{titleText}: {detailText}", isBusy: false);
            AppendLog($"{titleText}: {detailText}");
            RefreshModelCenterDashboard();
        }
    }
}
