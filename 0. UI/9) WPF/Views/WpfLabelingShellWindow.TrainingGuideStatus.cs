using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        // Training guide status owns dataset checklist and history persistence; live worker polling stays in TrainingStatus.
        private void UpdateYoloTrainingChecklist(YoloDatasetReadinessReport report, bool recordHistory)
        {
            if (LearningWorkflowViewModel == null || report == null)
            {
                return;
            }

            lastYoloTrainingReadinessReport = report;
            WpfTrainingChecklistPresentation presentation = WpfTrainingReadinessPresentationService.BuildChecklistPresentation(
                global.Data,
                report);
            LearningWorkflowViewModel.SetTrainingChecklistLocalization(presentation.Localization);
            LearningWorkflowViewModel.SetTrainingChecklistActionLocalization(presentation.ActionLocalization);
            UpdateDatasetStatusDashboard(report, presentation);
            UpdateYoloTrainingGuideDatasetHistory(report, presentation, recordHistory);
            UpdateYoloTrainingHistoryText();
            RefreshYoloTrainingStepCompletion(report);
        }

        // The Shell remains a UI adapter: it snapshots live state, applies the
        // service result to the existing workflow ViewModel, and owns no step policy.
        private void RefreshYoloTrainingStepCompletion(YoloDatasetReadinessReport report = null)
        {
            if (LearningWorkflowViewModel == null)
            {
                return;
            }

            report ??= lastYoloTrainingReadinessReport;
            string recipeName = GetCurrentRecipeName();
            bool hasDatasetSetup = (!string.IsNullOrWhiteSpace(recipeName)
                    && File.Exists(LabelingDatasetManifestService.GetManifestPath(recipeName)))
                || (!string.IsNullOrWhiteSpace(global.Data?.OutputRootPath)
                    && Directory.Exists(global.Data.OutputRootPath));
            bool hasCompletedCurrentDatasetTraining = false;
            PythonCommunicationStatus status = global.GetPythonCommunicationStatusSnapshot();
            string trainingState = status?.LastTrainingState?.Trim() ?? string.Empty;
            bool trainingCompletedFromWorker = WpfTrainingWeightsService.IsCompletedTrainingState(trainingState);
            if (!trainingCompletedFromWorker)
            {
                hasCompletedCurrentDatasetTraining = BuildCurrentTrainingWeightsComparison()?.HasCompletedCurrentDatasetTraining == true;
            }

            IEnumerable<WpfImageQueueItem> queueSource = imageQueueItems == null
                ? Enumerable.Empty<WpfImageQueueItem>()
                : imageQueueItems;
            IReadOnlyList<WpfTrainingStepQueueState> queueItems = queueSource
                .Where(item => item != null)
                .Select(item => new WpfTrainingStepQueueState(
                    item.IsLabeled,
                    item.IsSaveRequired,
                    item.ReviewState,
                    item.QualityReviewState))
                .ToList();
            WpfTrainingStepCompletionSnapshot completion = WpfTrainingStepCompletionService.Build(
                report,
                queueItems,
                activeImagePath,
                global.Data?.ClassNamedList?.Count ?? 0,
                manualRois.Count,
                confirmedDetectionCandidates.Count,
                hasDatasetSetup,
                hasCompletedCurrentDatasetTraining,
                status,
                pendingDetectionCandidates.Count);

            foreach (WpfTrainingStepState step in completion.Steps)
            {
                LearningWorkflowViewModel.SetYoloTrainingStepState(step.Order, step.IsCompleted, step.StateText);
            }

            LearningWorkflowViewModel.SetYoloFixActionAvailability(
                canFixClasses: true,
                canFixLabels: completion.HasImages,
                canFixDataset: true);
        }

        private void UpdateDatasetStatusDashboard(
            YoloDatasetReadinessReport report,
            WpfTrainingChecklistPresentation presentation)
        {
            if (LearningWorkflowViewModel == null || report == null)
            {
                return;
            }

            YoloDatasetStatistics statistics = report.Statistics ?? new YoloDatasetStatistics();
            int classCount = global.Data?.ClassNamedList?.Count ?? 0;
            IReadOnlyList<string> warnings = report.IsReady
                ? YoloDatasetDiagnosticsService.BuildQualityWarnings(global.Data, statistics)
                : Array.Empty<string>();
            AnomalyImageReviewSummary anomalySummary = report.Purpose == LabelingDatasetPurpose.AnomalyDetection
                ? anomalyImageReviewWorkflowService.LoadPersistedSummary(global.Data, statistics.TotalImageCount)
                : null;
            YoloDatasetQualityAuditReport qualityAudit = YoloDatasetQualityAuditService.Build(global.Data);
            WpfDatasetDashboardLocalizationSnapshot dashboardLocalization = WpfDatasetDashboardLocalizationService.Build(
                report,
                statistics,
                classCount,
                warnings,
                anomalySummary,
                qualityAudit,
                WpfTrainingReadinessPresentationService.ClassifyIssue(report.Errors));

            LearningWorkflowViewModel.SetModelReplacementLocalization(
                BuildModelReplacementLocalization(report, statistics));
            LearningWorkflowViewModel.SetDatasetDashboard(
                dashboardLocalization.StatusText,
                dashboardLocalization.SummaryText,
                presentation?.ActionText ?? string.Empty,
                WpfDatasetDashboardPresentationService.BuildMetrics(report, statistics, classCount, anomalySummary, qualityAudit),
                dashboardLocalization.IssueItems,
                dashboardLocalization);
        }



        private static WpfModelReplacementLocalizationSnapshot BuildModelReplacementLocalization(
            YoloDatasetReadinessReport report,
            YoloDatasetStatistics statistics)
        {
            return WpfModelReplacementLocalizationService.Build(report, statistics);
        }

    }
}
