using OpenVisionLab;
using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MvcVisionSystem
{
    /// <summary>
    /// Keeps dashboard presentation data as catalog keys and stable parameters so
    /// the same readiness result can be rendered again after a language change.
    /// </summary>
    public sealed class WpfDatasetDashboardLocalizationSnapshot
    {
        private readonly WpfDatasetDashboardTextDescriptor statusText;
        private readonly WpfDatasetDashboardTextDescriptor summaryText;
        private readonly IReadOnlyList<WpfDatasetDashboardTextDescriptor> issueTexts;
        private readonly IReadOnlyList<WpfDatasetDashboardMetricItem> metricItems;
        private readonly IReadOnlyList<WpfDatasetDashboardTextDescriptor> actionTexts;

        internal WpfDatasetDashboardLocalizationSnapshot(
            WpfDatasetDashboardTextDescriptor statusText,
            WpfDatasetDashboardTextDescriptor summaryText,
            IEnumerable<WpfDatasetDashboardTextDescriptor> issueTexts,
            IEnumerable<WpfDatasetDashboardMetricItem> metricItems = null,
            IEnumerable<WpfDatasetDashboardTextDescriptor> actionTexts = null)
        {
            this.statusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
            this.summaryText = summaryText ?? throw new ArgumentNullException(nameof(summaryText));
            this.issueTexts = (issueTexts ?? Enumerable.Empty<WpfDatasetDashboardTextDescriptor>())
                .Where(item => item != null)
                .ToList();
            this.metricItems = (metricItems ?? Enumerable.Empty<WpfDatasetDashboardMetricItem>())
                .Where(item => item != null)
                .ToList();
            this.actionTexts = (actionTexts ?? Enumerable.Empty<WpfDatasetDashboardTextDescriptor>())
                .Where(item => item != null)
                .ToList();
        }

        public string StatusText => statusText.Render();

        public string SummaryText => summaryText.Render();

        public string ActionText => string.Join(
            " / ",
            actionTexts
                .Select(item => item.Render())
                .Where(item => !string.IsNullOrWhiteSpace(item)));

        public IReadOnlyList<string> IssueItems => issueTexts
            .Select(item => item.Render())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        public IReadOnlyList<WpfDatasetDashboardMetricItem> MetricItems => metricItems
            .Select(item => item.Localize())
            .ToList();

        internal WpfDatasetDashboardLocalizationSnapshot WithMetricItems(
            IEnumerable<WpfDatasetDashboardMetricItem> metrics)
        {
            return new WpfDatasetDashboardLocalizationSnapshot(
                statusText,
                summaryText,
                issueTexts,
                metrics,
                actionTexts);
        }
    }

    internal sealed class WpfDatasetDashboardTextDescriptor
    {
        private readonly string key;
        private readonly object[] arguments;

        internal WpfDatasetDashboardTextDescriptor(string key, params object[] arguments)
        {
            this.key = key ?? string.Empty;
            this.arguments = arguments ?? Array.Empty<object>();
        }

        internal string Render()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                OpenVisionLanguageService.T(key),
                arguments);
        }
    }

    internal sealed class WpfDatasetDashboardMetricLocalizationDescriptor
    {
        private readonly WpfDatasetDashboardTextDescriptor title;
        private readonly WpfDatasetDashboardTextDescriptor value;
        private readonly WpfDatasetDashboardTextDescriptor detail;
        private readonly WpfDatasetDashboardTextDescriptor state;

        internal WpfDatasetDashboardMetricLocalizationDescriptor(
            WpfDatasetDashboardTextDescriptor title,
            WpfDatasetDashboardTextDescriptor detail,
            WpfDatasetDashboardTextDescriptor state,
            WpfDatasetDashboardTextDescriptor value = null)
        {
            this.title = title ?? throw new ArgumentNullException(nameof(title));
            this.detail = detail ?? throw new ArgumentNullException(nameof(detail));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.value = value;
        }

        internal string RenderTitle() => title.Render();

        internal string RenderValue(string fallbackValue) => value?.Render() ?? fallbackValue ?? string.Empty;

        internal string RenderDetail() => detail.Render();

        internal string RenderState() => state.Render();
    }

    public static class WpfDatasetDashboardLocalizationService
    {
        private const int RecommendedModelReplacementTestImageCount = 10;

        internal static WpfDatasetDashboardMetricItem CreateMetric(
            string titleKey,
            string value,
            string detailKey,
            string stateKey,
            MahApps.Metro.IconPacks.PackIconMaterialKind iconKind,
            bool isProblem,
            bool isWarning,
            WpfDatasetDashboardActionKind actionKind,
            params object[] detailArguments)
        {
            return WpfDatasetDashboardMetricItem.CreateLocalized(
                new WpfDatasetDashboardMetricLocalizationDescriptor(
                    Text(titleKey),
                    Text(detailKey, detailArguments ?? Array.Empty<object>()),
                    Text(stateKey)),
                value,
                iconKind,
                isProblem,
                isWarning,
                actionKind);
        }

        internal static WpfDatasetDashboardMetricItem CreateMetricWithValue(
            string titleKey,
            string valueKey,
            string fallbackValue,
            string detailKey,
            string stateKey,
            MahApps.Metro.IconPacks.PackIconMaterialKind iconKind,
            bool isProblem,
            bool isWarning,
            WpfDatasetDashboardActionKind actionKind,
            params object[] detailArguments)
        {
            return WpfDatasetDashboardMetricItem.CreateLocalized(
                new WpfDatasetDashboardMetricLocalizationDescriptor(
                    Text(titleKey),
                    Text(detailKey, detailArguments ?? Array.Empty<object>()),
                    Text(stateKey),
                    Text(valueKey)),
                fallbackValue,
                iconKind,
                isProblem,
                isWarning,
                actionKind);
        }

        public static WpfDatasetDashboardLocalizationSnapshot CreateInitial()
        {
            return new WpfDatasetDashboardLocalizationSnapshot(
                Text("WpfLearningWorkflow.DatasetDashboard.Status.Before"),
                Text("WpfLearningWorkflow.DatasetDashboard.Summary.Before"),
                new[]
                {
                    Text("WpfLearningWorkflow.DatasetDashboard.Issue.Before")
                });
        }

        public static WpfDatasetDashboardLocalizationSnapshot Build(
            YoloDatasetReadinessReport report,
            YoloDatasetStatistics statistics,
            int classCount,
            IReadOnlyList<string> warnings,
            AnomalyImageReviewSummary anomalySummary,
            YoloDatasetQualityAuditReport qualityAudit,
            string issueKind)
        {
            statistics ??= report?.Statistics ?? new YoloDatasetStatistics();
            warnings ??= Array.Empty<string>();
            qualityAudit ??= new YoloDatasetQualityAuditReport();
            LabelingDatasetPurpose purpose = report?.Purpose ?? LabelingDatasetPurpose.ObjectDetection;
            bool isReady = report?.IsReady == true;
            int anomalyNormalImageCount = anomalySummary?.NormalImageCount ?? statistics.AnomalyNormalImageCount;
            int anomalyAbnormalImageCount = anomalySummary?.AbnormalImageCount ?? statistics.AnomalyAbnormalImageCount;
            int anomalyUnreviewedImageCount = anomalySummary?.UnreviewedImageCount ?? statistics.AnomalyUnreviewedImageCount;

            WpfDatasetDashboardTextDescriptor statusText = Text(
                isReady
                    ? warnings.Count > 0
                        ? "WpfLearningWorkflow.DatasetDashboard.Status.ReadyWithWarnings"
                        : "WpfLearningWorkflow.DatasetDashboard.Status.Ready"
                    : "WpfLearningWorkflow.DatasetDashboard.Status.NotReady");

            WpfDatasetDashboardTextDescriptor summaryText = purpose == LabelingDatasetPurpose.AnomalyDetection
                ? Text(
                    "WpfLearningWorkflow.DatasetDashboard.Summary.Anomaly",
                    purpose,
                    statistics.TotalImageCount,
                    statistics.TrainImageCount,
                    statistics.ValidImageCount,
                    statistics.TestImageCount,
                    classCount,
                    anomalyNormalImageCount,
                    anomalyAbnormalImageCount,
                    anomalyUnreviewedImageCount,
                    qualityAudit.TotalMissingLabelCount,
                    qualityAudit.TotalInvalidLabelLineCount,
                    qualityAudit.TotalEmptyLabelCount)
                : Text(
                    "WpfLearningWorkflow.DatasetDashboard.Summary.Standard",
                    purpose,
                    statistics.TotalImageCount,
                    statistics.TrainImageCount,
                    statistics.ValidImageCount,
                    statistics.TestImageCount,
                    classCount,
                    qualityAudit.TotalMissingLabelCount,
                    qualityAudit.TotalInvalidLabelLineCount,
                    qualityAudit.TotalEmptyLabelCount);

            var issues = new List<WpfDatasetDashboardTextDescriptor>();
            WpfDatasetDashboardTextDescriptor nextAction = BuildObjectDetectionNextAction(report, statistics, classCount);
            if (nextAction != null)
            {
                issues.Add(nextAction);
            }

            if (qualityAudit.TotalMissingLabelCount > 0 || qualityAudit.TotalInvalidLabelLineCount > 0)
            {
                issues.Add(Text(
                    "WpfLearningWorkflow.DatasetDashboard.Issue.Quality",
                    qualityAudit.TotalMissingLabelCount,
                    qualityAudit.TotalInvalidLabelLineCount));
            }

            if (purpose == LabelingDatasetPurpose.AnomalyDetection && anomalySummary != null && anomalySummary.TotalImageCount > 0)
            {
                issues.Add(anomalySummary.UnreviewedImageCount > 0
                    ? Text("WpfLearningWorkflow.DatasetDashboard.Issue.Anomaly.Unreviewed", anomalySummary.UnreviewedImageCount)
                    : Text("WpfLearningWorkflow.DatasetDashboard.Issue.Anomaly.Complete"));
            }

            if (isReady)
            {
                if (warnings.Count > 0)
                {
                    foreach (string warning in warnings.Take(3))
                    {
                        issues.Add(BuildQualityWarning(warning));
                    }
                }
                else
                {
                    issues.Add(Text("WpfLearningWorkflow.DatasetDashboard.Issue.NoIssues"));
                }
            }
            else
            {
                issues.Add(BuildFriendlyIssue(issueKind));
                foreach (string error in report?.Errors?.Take(2) ?? Enumerable.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        issues.Add(Text("WpfLearningWorkflow.DatasetDashboard.Issue.Detail", error));
                    }
                }
            }

            return new WpfDatasetDashboardLocalizationSnapshot(
                statusText,
                summaryText,
                issues,
                actionTexts: BuildPrimaryActionTexts(report, purpose, warnings, issueKind));
        }

        private static IReadOnlyList<WpfDatasetDashboardTextDescriptor> BuildPrimaryActionTexts(
            YoloDatasetReadinessReport report,
            LabelingDatasetPurpose purpose,
            IReadOnlyList<string> warnings,
            string issueKind)
        {
            if (report?.IsReady == true)
            {
                if (warnings != null && warnings.Count > 0)
                {
                    return warnings
                        .Take(2)
                        .Select(BuildPrimaryActionWarning)
                        .ToList();
                }

                return new[]
                {
                    Text(purpose switch
                    {
                        LabelingDatasetPurpose.Segmentation => "WpfLearningWorkflow.DatasetDashboard.Action.Ready.Segmentation",
                        LabelingDatasetPurpose.AnomalyDetection => "WpfLearningWorkflow.DatasetDashboard.Action.Ready.Anomaly",
                        _ => "WpfLearningWorkflow.DatasetDashboard.Action.Ready.ObjectDetection"
                    })
                };
            }

            return new[]
            {
                Text(issueKind switch
                {
                    "Classes" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.Classes",
                    "Labels" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.Labels",
                    "SegmentationPolicy" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.SegmentationPolicy",
                    "SegmentationLabels" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.SegmentationLabels",
                    "ValidImages" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.ValidImages",
                    "Split" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.Split",
                    "DataYaml" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.DataYaml",
                    "LabelFormat" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.LabelFormat",
                    "OutputRoot" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.OutputRoot",
                    "Images" => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.Images",
                    _ => "WpfLearningWorkflow.DatasetDashboard.Action.Failure.Unknown"
                })
            };
        }

        private static WpfDatasetDashboardTextDescriptor BuildPrimaryActionWarning(string warning)
        {
            string normalized = warning?.Trim() ?? string.Empty;
            if (normalized.Contains("Test split is empty", StringComparison.OrdinalIgnoreCase))
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Action.Warning.TestSplitEmpty");
            }

            if (normalized.Contains("YOLO split guide", StringComparison.OrdinalIgnoreCase))
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Action.Warning.SplitGuide");
            }

            return string.IsNullOrWhiteSpace(normalized)
                ? Text("WpfLearningWorkflow.DatasetDashboard.Action.Warning.Unknown")
                : Text("WpfLearningWorkflow.DatasetDashboard.Action.Warning.Raw", normalized);
        }

        private static WpfDatasetDashboardTextDescriptor BuildObjectDetectionNextAction(
            YoloDatasetReadinessReport report,
            YoloDatasetStatistics statistics,
            int classCount)
        {
            if (report == null || report.Purpose != LabelingDatasetPurpose.ObjectDetection)
            {
                return null;
            }

            if (statistics.TotalImageCount <= 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.Images");
            }

            if (classCount <= 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.Classes");
            }

            if (statistics.TotalLabelFileCount <= 0 && statistics.TotalObjectCount <= 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.Labels");
            }

            int completedImageCount = Math.Min(statistics.TotalLabelFileCount, statistics.TotalImageCount);
            if (completedImageCount < statistics.TotalImageCount)
            {
                return Text(
                    "WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.Remaining",
                    Math.Max(0, statistics.TotalImageCount - completedImageCount));
            }

            if (statistics.TrainValidImageContentOverlapCount > 0 || statistics.SplitImageContentOverlapCount > 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.SplitOverlap");
            }

            if (statistics.ValidImageCount <= 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.ValidImages");
            }

            if (statistics.TestImageCount <= 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.TestImages");
            }

            if (statistics.TestLabelCount <= 0)
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.TestLabels");
            }

            int finalVerificationCount = Math.Min(statistics.TestImageCount, statistics.TestLabelCount);
            if (finalVerificationCount < RecommendedModelReplacementTestImageCount)
            {
                return Text(
                    "WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.MoreFinalVerification",
                    RecommendedModelReplacementTestImageCount - finalVerificationCount);
            }

            return report.IsReady
                ? Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.Complete")
                : Text("WpfLearningWorkflow.DatasetDashboard.Issue.Next.ObjectDetection.FixDetails");
        }

        private static WpfDatasetDashboardTextDescriptor BuildQualityWarning(string warning)
        {
            string normalized = warning?.Trim() ?? string.Empty;
            if (normalized.Contains("Test split is empty", StringComparison.OrdinalIgnoreCase))
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Warning.TestSplitEmpty");
            }

            if (normalized.Contains("YOLO split guide", StringComparison.OrdinalIgnoreCase))
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Warning.SplitGuide");
            }

            return Text("WpfLearningWorkflow.DatasetDashboard.Issue.Warning.Raw", normalized);
        }

        private static WpfDatasetDashboardTextDescriptor BuildFriendlyIssue(string issueKind)
        {
            return Text(issueKind switch
            {
                "Classes" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.Classes",
                "Labels" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.Labels",
                "SegmentationPolicy" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.SegmentationPolicy",
                "SegmentationLabels" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.SegmentationLabels",
                "ValidImages" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.ValidImages",
                "Split" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.Split",
                "DataYaml" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.DataYaml",
                "LabelFormat" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.LabelFormat",
                "OutputRoot" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.OutputRoot",
                "Images" => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.Images",
                _ => "WpfLearningWorkflow.DatasetDashboard.Issue.Friendly.Unknown"
            });
        }

        private static WpfDatasetDashboardTextDescriptor Text(string key, params object[] arguments)
        {
            return new WpfDatasetDashboardTextDescriptor(key, arguments);
        }
    }
}
