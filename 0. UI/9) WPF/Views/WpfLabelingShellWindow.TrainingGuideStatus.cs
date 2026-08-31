using MahApps.Metro.IconPacks;
using MvcVisionSystem._3._Communication.TCP;
using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem
{
    public partial class WpfLabelingShellWindow
    {
        private const int RecommendedModelReplacementTestImageCount = 10;

        // Training guide status owns dataset checklist and history persistence; live worker polling stays in TrainingStatus.
        private void UpdateYoloTrainingChecklist(YoloDatasetReadinessReport report, bool recordHistory)
        {
            if (LearningWorkflowViewModel == null || report == null)
            {
                return;
            }

            lastYoloTrainingReadinessReport = report;
            YoloTrainingIssuePresentation presentation = BuildYoloTrainingIssuePresentation(report);
            LearningWorkflowViewModel.SetTrainingChecklistLocalization(presentation.Localization);
            LearningWorkflowViewModel.SetTrainingChecklistActionLocalization(presentation.ActionLocalization);
            UpdateDatasetStatusDashboard(report, presentation);
            UpdateYoloTrainingGuideDatasetHistory(report, presentation, recordHistory);
            UpdateYoloTrainingHistoryText();
            RefreshYoloTrainingStepCompletion(report);
        }

        private void UpdateDatasetStatusDashboard(
            YoloDatasetReadinessReport report,
            YoloTrainingIssuePresentation presentation)
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
                ? AnomalyImageReviewStatusService.LoadPersistedSummary(global.Data, statistics.TotalImageCount)
                : null;
            YoloDatasetQualityAuditReport qualityAudit = YoloDatasetQualityAuditService.Build(global.Data);
            WpfDatasetDashboardLocalizationSnapshot dashboardLocalization = WpfDatasetDashboardLocalizationService.Build(
                report,
                statistics,
                classCount,
                warnings,
                anomalySummary,
                qualityAudit,
                ClassifyYoloTrainingIssue(report.Errors));

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

        private static string BuildModelReplacementStatusText(YoloDatasetReadinessReport report, YoloDatasetStatistics statistics)
        {
            return BuildModelReplacementLocalization(report, statistics).StatusText;
        }

        private static string BuildModelReplacementDetailText(YoloDatasetReadinessReport report, YoloDatasetStatistics statistics)
        {
            return BuildModelReplacementLocalization(report, statistics).DetailText;
        }

        private YoloTrainingIssuePresentation BuildYoloTrainingIssuePresentation(YoloDatasetReadinessReport report)
        {
            if (report?.IsReady == true)
            {
                YoloDatasetStatistics statistics = report.Statistics;
                LabelingDatasetPurpose purpose = report.Purpose;
                int classCount = global.Data?.ClassNamedList?.Count ?? 0;
                IReadOnlyList<string> warnings = YoloDatasetDiagnosticsService.BuildQualityWarnings(global.Data, statistics);
                bool hasWarnings = warnings.Count > 0;
                WpfTrainingChecklistLocalizationSnapshot localization = WpfTrainingChecklistLocalizationService.BuildReady(
                    statistics,
                    classCount,
                    purpose,
                    hasWarnings);
                return new YoloTrainingIssuePresentation(
                    hasWarnings ? "ReadyWithWarnings" : "Ready",
                    localization,
                    hasWarnings
                        ? WpfTrainingChecklistLocalizationService.BuildQualityWarningAction(warnings)
                        : WpfTrainingChecklistLocalizationService.BuildReadyAction(purpose));
            }

            string firstError = report?.Errors?.FirstOrDefault() ?? "원인 미확인";
            string issueKind = ClassifyYoloTrainingIssue(report?.Errors ?? Array.Empty<string>());
            LabelingDatasetPurpose failurePurpose = report?.Purpose ?? LabelingDatasetPurpose.ObjectDetection;
            WpfTrainingChecklistLocalizationSnapshot failureLocalization = WpfTrainingChecklistLocalizationService.BuildFailure(
                issueKind,
                firstError,
                report?.Statistics,
                failurePurpose);
            return new YoloTrainingIssuePresentation(
                issueKind,
                failureLocalization,
                WpfTrainingChecklistLocalizationService.BuildFailureAction(issueKind));
        }

        private static string BuildReadyDatasetStatusText(
            YoloDatasetStatistics statistics,
            LabelingDatasetPurpose purpose,
            bool hasWarnings)
        {
            return WpfTrainingChecklistLocalizationService
                .BuildReady(statistics, classCount: 0, purpose: purpose, hasWarnings: hasWarnings)
                .StatusText;
        }

        private static string BuildReadyDatasetDetail(YoloDatasetStatistics statistics, int classCount, LabelingDatasetPurpose purpose)
        {
            return WpfTrainingChecklistLocalizationService
                .BuildReady(statistics, classCount, purpose, hasWarnings: false)
                .DetailText;
        }

        private static string BuildReadyDatasetActionText(LabelingDatasetPurpose purpose)
        {
            return WpfTrainingChecklistLocalizationService.BuildReadyAction(purpose).ActionText;
        }

        private static string BuildQualityWarningActionText(IReadOnlyList<string> warnings)
        {
            return WpfTrainingChecklistLocalizationService.BuildQualityWarningAction(warnings).ActionText;
        }

        private static string FormatDatasetQualityWarning(string warning)
        {
            return WpfTrainingChecklistLocalizationService.FormatQualityWarning(warning);
        }

        private static string BuildDatasetFailureDetail(string firstError, YoloDatasetStatistics statistics, LabelingDatasetPurpose purpose)
        {
            return WpfTrainingChecklistLocalizationService
                .BuildFailure("Unknown", firstError, statistics, purpose)
                .DetailText;
        }

        private static string ClassifyYoloTrainingIssue(IEnumerable<string> errors)
        {
            List<string> normalized = (errors ?? Array.Empty<string>())
                .Select(error => error?.Trim() ?? string.Empty)
                .Where(error => error.Length > 0)
                .Select(error => error.ToLowerInvariant())
                .ToList();

            if (normalized.Any(error => error.Contains("invalid yolo format", StringComparison.Ordinal)
                || error.Contains("invalid class index", StringComparison.Ordinal)
                || error.Contains("out-of-range normalized value", StringComparison.Ordinal)
                || error.Contains("label width must", StringComparison.Ordinal)
                || error.Contains("label height must", StringComparison.Ordinal)))
            {
                return "LabelFormat";
            }

            if (normalized.Any(error => error.Contains("at least one class", StringComparison.Ordinal)
                || error.Contains("class names", StringComparison.Ordinal)
                || error.Contains("duplicate class", StringComparison.Ordinal)))
            {
                return "Classes";
            }

            if (normalized.Any(error => error.Contains("label file is missing", StringComparison.Ordinal)
                || error.Contains("label directory", StringComparison.Ordinal)))
            {
                return "Labels";
            }

            if (normalized.Any(error => error.Contains("segmentation annotations", StringComparison.Ordinal)
                && error.Contains("no yolo box labels", StringComparison.Ordinal)))
            {
                return "SegmentationPolicy";
            }

            if (normalized.Any(error => error.Contains("segmentation dataset", StringComparison.Ordinal)
                || error.Contains("segmentation annotation is missing", StringComparison.Ordinal)
                || error.Contains("segment json", StringComparison.Ordinal)
                || error.Contains("mask png", StringComparison.Ordinal)))
            {
                return "SegmentationLabels";
            }

            if (normalized.Any(error => error.Contains("valid image directory", StringComparison.Ordinal)))
            {
                return "ValidImages";
            }

            if (normalized.Any(error => error.Contains("train/valid image split", StringComparison.Ordinal)
                || error.Contains("train/test image split", StringComparison.Ordinal)
                || error.Contains("valid/test image split", StringComparison.Ordinal)
                || error.Contains("duplicate image content", StringComparison.Ordinal)
                || error.Contains("different validation images", StringComparison.Ordinal)))
            {
                return "Split";
            }

            if (normalized.Any(error => error.Contains("data.yaml", StringComparison.Ordinal)))
            {
                return "DataYaml";
            }

            if (normalized.Any(error => error.Contains("output root", StringComparison.Ordinal)))
            {
                return "OutputRoot";
            }

            if (normalized.Any(error => error.Contains("image directory", StringComparison.Ordinal)
                || error.Contains("supported images", StringComparison.Ordinal)))
            {
                return "Images";
            }

            return "Unknown";
        }

        private sealed class YoloTrainingIssuePresentation
        {
            public YoloTrainingIssuePresentation(
                string issueKind,
                WpfTrainingChecklistLocalizationSnapshot localization,
                WpfTrainingChecklistActionLocalizationSnapshot actionLocalization)
            {
                IssueKind = issueKind ?? string.Empty;
                Localization = localization ?? throw new ArgumentNullException(nameof(localization));
                ActionLocalization = actionLocalization ?? throw new ArgumentNullException(nameof(actionLocalization));
            }

            public string IssueKind { get; }

            public WpfTrainingChecklistLocalizationSnapshot Localization { get; }

            public WpfTrainingChecklistActionLocalizationSnapshot ActionLocalization { get; }

            public string StatusText => Localization.StatusText;

            public string DetailText => Localization.DetailText;

            public string ActionText => ActionLocalization.ActionText;
        }
    }
}
