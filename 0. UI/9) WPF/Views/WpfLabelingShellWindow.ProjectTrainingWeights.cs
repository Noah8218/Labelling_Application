using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Training weight auto-apply is separated from recipe persistence because it only stages settings until save.
        private bool TryApplyLatestTrainingWeightsFromProject(bool logIfUnchanged)
        {
            EnsureProjectSettings();
            PythonModelSettings settings = global.Data.ProjectSettings.PythonModel;
            WpfTrainingWeightsComparison comparison = trainingWeightsService.BuildComparison(
                settings.ProjectRootPath,
                global.Data.OutputRootPath,
                settings.WeightsPath);
            string comparisonStatusText = WpfTrainingComparisonPresentationService
                .BuildComparisonStatusText(comparison);
            if (LearningWorkflowViewModel != null)
            {
                UpdateTrainingComparisonViewModel(comparison, comparisonStatusText);
            }
            RefreshModelCenterDashboard(comparison);

            string latestWeightsPath = comparison.LatestWeightsPath;
            if (!comparison.HasLatestWeights)
            {
                if (logIfUnchanged)
                {
                    SetYoloCommandStatus($"{comparisonStatusText}. 모델 설정에서 학습 결과 모델을 직접 선택하세요.", isBusy: false);
                    AppendLog("학습 결과 모델 후보를 찾지 못했습니다.");
                }

                return false;
            }

            string currentWeightsPath = settings.WeightsPath?.Trim() ?? string.Empty;
            if (string.Equals(currentWeightsPath, latestWeightsPath, StringComparison.OrdinalIgnoreCase))
            {
                if (logIfUnchanged)
                {
                    SetYoloCommandStatus(comparisonStatusText, isBusy: false);
                    AppendLog($"현재 검사 모델 유지: {latestWeightsPath}");
                }

                return false;
            }

            if (!comparison.ShouldApplyLatest)
            {
                if (logIfUnchanged)
                {
                    SetYoloCommandStatus(comparisonStatusText, isBusy: false);
                    AppendLog($"현재 검사 모델 유지: {currentWeightsPath}");
                }

                return false;
            }

            if (!string.IsNullOrWhiteSpace(currentWeightsPath)
                && File.Exists(currentWeightsPath)
                && !string.Equals(currentWeightsPath, latestWeightsPath, StringComparison.OrdinalIgnoreCase))
            {
                pendingTrainingBaselineWeightsPath = currentWeightsPath;
            }

            settings.WeightsPath = latestWeightsPath;
            YoloModelSettingsViewModel?.LoadFrom(settings);
            string latestDisplayName = WpfTrainingWeightsService.FormatWeightsDisplayPath(latestWeightsPath);
            SetModelStatus($"모델 후보: {Path.GetFileName(latestWeightsPath)}");
            hasPendingTrainingWeightsRecipeSave = true;
            RefreshModelCenterDashboard(comparison);
            SetGlobalInferenceStatus(string.Empty, isBusy: false);
            SetModelStatus($"모델 후보: {latestDisplayName}");
            UpdateAppliedTrainingWeightsHistory(latestWeightsPath, savedToRecipe: false);
            FocusYoloModelSettingsTab();
            SaveYoloSettingsButton?.Focus();
            SetProjectConfigStatus("새 학습 모델 후보를 검사 모델 설정에 올렸습니다. 모델 비교 후 저장하면 프로젝트에 반영됩니다.");
            SetYoloCommandStatus($"새 학습 모델 후보: {Path.GetFileName(latestWeightsPath)} / {comparison.MetricsStatusText} / 모델 비교 후 저장 필요", isBusy: false);

            SetYoloCommandStatus($"현재 데이터셋 학습 완료: {latestDisplayName} / {comparison.MetricsStatusText} / 모델 비교 및 저장 필요", isBusy: false);

            if (!string.Equals(lastAutoAppliedTrainingWeightsPath, latestWeightsPath, StringComparison.OrdinalIgnoreCase))
            {
                lastAutoAppliedTrainingWeightsPath = latestWeightsPath;
                AppendLog($"새 학습 모델 후보 등록: {latestWeightsPath} / baseline={pendingTrainingBaselineWeightsPath} / {comparison.MetricsStatusText} / 모델 비교 후 저장 필요");
            }

            return true;
        }

        private string GetTrainingComparisonCurrentWeightsPath(string configuredWeightsPath)
        {
            string pendingBaseline = pendingTrainingBaselineWeightsPath?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pendingBaseline)
                && File.Exists(pendingBaseline)
                && !string.Equals(pendingBaseline, configuredWeightsPath?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return pendingBaseline;
            }

            return configuredWeightsPath ?? string.Empty;
        }

        private WpfTrainingWeightsComparison BuildCurrentTrainingWeightsComparison()
        {
            EnsureProjectSettings();
            PythonModelSettings settings = global.Data.ProjectSettings.PythonModel;
            return trainingWeightsService.BuildComparison(
                settings.ProjectRootPath,
                global.Data.OutputRootPath,
                GetTrainingComparisonCurrentWeightsPath(settings.WeightsPath));
        }

        private void UpdateTrainingComparisonViewModel(WpfTrainingWeightsComparison comparison, string comparisonStatusText = null)
        {
            if (LearningWorkflowViewModel == null)
            {
                return;
            }

            WpfTrainingComparisonPresentation presentation = WpfTrainingComparisonPresentationService.Build(comparison);
            comparisonStatusText ??= presentation.StatusText;
            LearningWorkflowViewModel.SetTrainingComparisonResultTexts(
                summaryText: presentation.SummaryText,
                comparisonText: comparisonStatusText,
                adoptionDecisionText: presentation.AdoptionDecisionText);
            LearningWorkflowViewModel.SetTrainingResultReportItems(presentation.ResultReportItems);
            UpdateCandidateModelComparisonReviewPanel(comparison);
        }

        private void UpdateCandidateModelComparisonReviewPanel(WpfTrainingWeightsComparison comparison = null)
        {
            if (CandidateReviewViewModel == null)
            {
                return;
            }

            comparison ??= BuildCurrentTrainingWeightsComparison();
            IReadOnlyList<string> classNames = global.Data?.ClassNamedList == null
                ? Array.Empty<string>()
                : global.Data.ClassNamedList
                    .Select(item => item?.Text ?? string.Empty)
                    .ToList();
            double confidence = global.Data?.ProjectSettings?.PythonModel?.MinimumDetectionConfidence ?? 0.25D;
            CandidateReviewViewModel.SetModelComparisonSourceText(
                WpfInferenceStatusPresentationService.BuildModelComparisonSourceText(
                    global.Data?.ProjectSettings?.PythonModel,
                    comparison?.CurrentWeightsPath,
                    comparison?.LatestWeightsPath));
            // The latest matching artifact remains authoritative; older matching runs are read-only history.
            WpfModelComparisonHistoryItem historyItem = RefreshModelComparisonHistoryItems(
                comparison?.CurrentWeightsPath,
                comparison?.LatestWeightsPath);
            WpfModelComparisonReviewReport report = historyItem == null
                ? WpfModelComparisonReviewReport.Empty
                : modelComparisonReviewService.BuildFromSummaryFile(
                    historyItem.SourcePath,
                    classNames,
                    confidence,
                    maxExamples: 5);
            CandidateReviewViewModel.SetModelComparisonReview(
                report,
                isHistoricalSelection: historyItem?.IsLatest == false);
            UpdateCandidateModelDecisionPanel(comparison);
        }

    }
}
