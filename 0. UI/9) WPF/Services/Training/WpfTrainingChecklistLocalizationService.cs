using MvcVisionSystem.Yolo;
using OpenVisionLab;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MvcVisionSystem
{
    /// <summary>
    /// Keeps the visible Training Checklist text as catalog descriptors so the
    /// same readiness result can be rendered again after a language change.
    /// </summary>
    public sealed class WpfTrainingChecklistLocalizationSnapshot
    {
        private readonly WpfTrainingChecklistTextDescriptor statusText;
        private readonly WpfTrainingChecklistTextDescriptor detailText;

        internal WpfTrainingChecklistLocalizationSnapshot(
            WpfTrainingChecklistTextDescriptor statusText,
            WpfTrainingChecklistTextDescriptor detailText)
        {
            this.statusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
            this.detailText = detailText ?? throw new ArgumentNullException(nameof(detailText));
        }

        public string StatusText => statusText.Render();

        public string DetailText => detailText.Render();
    }

    /// <summary>
    /// Keeps Training Checklist action text as catalog descriptors so the same
    /// readiness result can be rendered again after a language change.
    /// </summary>
    public sealed class WpfTrainingChecklistActionLocalizationSnapshot
    {
        private readonly IReadOnlyList<WpfTrainingChecklistTextDescriptor> actionTexts;

        internal WpfTrainingChecklistActionLocalizationSnapshot(
            IEnumerable<WpfTrainingChecklistTextDescriptor> actionTexts)
        {
            this.actionTexts = (actionTexts ?? Enumerable.Empty<WpfTrainingChecklistTextDescriptor>())
                .Where(item => item != null)
                .ToList();
        }

        public string ActionText => string.Join(
            " / ",
            actionTexts
                .Select(item => item.Render())
                .Where(item => !string.IsNullOrWhiteSpace(item)));
    }

    internal sealed class WpfTrainingChecklistTextDescriptor
    {
        private readonly string key;
        private readonly object[] arguments;

        internal WpfTrainingChecklistTextDescriptor(string key, params object[] arguments)
        {
            this.key = key ?? string.Empty;
            this.arguments = arguments ?? Array.Empty<object>();
        }

        internal string Render()
        {
            object[] renderedArguments = arguments
                .Select(RenderArgument)
                .ToArray();
            return string.Format(
                CultureInfo.InvariantCulture,
                OpenVisionLanguageService.T(key),
                renderedArguments);
        }

        private static object RenderArgument(object argument)
        {
            return argument switch
            {
                WpfTrainingChecklistTextDescriptor descriptor => descriptor.Render(),
                WpfTrainingChecklistLocalizedArgument localizedArgument => localizedArgument.Render(),
                _ => argument ?? string.Empty
            };
        }
    }

    internal sealed class WpfTrainingChecklistLocalizedArgument
    {
        private readonly Func<string> render;

        internal WpfTrainingChecklistLocalizedArgument(Func<string> render)
        {
            this.render = render ?? throw new ArgumentNullException(nameof(render));
        }

        internal string Render() => render() ?? string.Empty;
    }

    public static class WpfTrainingChecklistLocalizationService
    {
        public static WpfTrainingChecklistLocalizationSnapshot CreateInitial()
        {
            return new WpfTrainingChecklistLocalizationSnapshot(
                Text("WpfLearningWorkflow.TrainingChecklist.Status.Initial"),
                Text("WpfLearningWorkflow.TrainingChecklist.Detail.Initial"));
        }

        public static WpfTrainingChecklistActionLocalizationSnapshot CreateInitialAction()
        {
            return new WpfTrainingChecklistActionLocalizationSnapshot(
                new[]
                {
                    Text("WpfLearningWorkflow.TrainingChecklist.Action.Initial")
                });
        }

        public static WpfTrainingChecklistActionLocalizationSnapshot BuildReadyAction(
            LabelingDatasetPurpose purpose)
        {
            return new WpfTrainingChecklistActionLocalizationSnapshot(
                new[]
                {
                    Text(purpose switch
                    {
                        LabelingDatasetPurpose.Segmentation => "WpfLearningWorkflow.DatasetDashboard.Action.Ready.Segmentation",
                        LabelingDatasetPurpose.AnomalyDetection => "WpfLearningWorkflow.DatasetDashboard.Action.Ready.Anomaly",
                        _ => "WpfLearningWorkflow.DatasetDashboard.Action.Ready.ObjectDetection"
                    })
                });
        }

        public static WpfTrainingChecklistActionLocalizationSnapshot BuildQualityWarningAction(
            IReadOnlyList<string> warnings)
        {
            if (warnings == null || warnings.Count == 0)
            {
                return BuildReadyAction(LabelingDatasetPurpose.ObjectDetection);
            }

            return new WpfTrainingChecklistActionLocalizationSnapshot(
                warnings
                    .Take(2)
                    .Select(BuildQualityWarningActionText));
        }

        public static WpfTrainingChecklistActionLocalizationSnapshot BuildFailureAction(string issueKind)
        {
            return new WpfTrainingChecklistActionLocalizationSnapshot(
                new[]
                {
                    Text(GetFailureActionKey(issueKind))
                });
        }

        internal static string FormatQualityWarning(string warning)
        {
            string normalized = warning?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return OpenVisionLanguageService.T("WpfLearningWorkflow.DatasetDashboard.Action.Warning.Unknown");
            }

            if (normalized.Contains("Test split is empty", StringComparison.OrdinalIgnoreCase))
            {
                return OpenVisionLanguageService.T("WpfLearningWorkflow.DatasetDashboard.Action.Warning.TestSplitEmpty");
            }

            if (normalized.Contains("YOLO split guide", StringComparison.OrdinalIgnoreCase))
            {
                return OpenVisionLanguageService.T("WpfLearningWorkflow.DatasetDashboard.Action.Warning.SplitGuide");
            }

            if (OpenVisionLanguageService.CurrentLanguage == OpenVisionLanguage.English)
            {
                return normalized;
            }

            return normalized
                .Replace("train/valid/test", "학습/검증/최종 검증", StringComparison.OrdinalIgnoreCase)
                .Replace("training", "학습", StringComparison.OrdinalIgnoreCase)
                .Replace("validation", "검증", StringComparison.OrdinalIgnoreCase)
                .Replace("test split", "최종 검증", StringComparison.OrdinalIgnoreCase)
                .Replace("train", "학습", StringComparison.OrdinalIgnoreCase)
                .Replace("valid", "검증", StringComparison.OrdinalIgnoreCase)
                .Replace("test", "최종", StringComparison.OrdinalIgnoreCase);
        }

        public static WpfTrainingChecklistLocalizationSnapshot BuildReady(
            YoloDatasetStatistics statistics,
            int classCount,
            LabelingDatasetPurpose purpose,
            bool hasWarnings)
        {
            statistics ??= new YoloDatasetStatistics();
            WpfTrainingChecklistTextDescriptor state = Text(
                hasWarnings
                    ? "WpfLearningWorkflow.TrainingChecklist.State.Warning"
                    : "WpfLearningWorkflow.TrainingChecklist.State.Ready");

            WpfTrainingChecklistTextDescriptor status = purpose switch
            {
                LabelingDatasetPurpose.Segmentation => Text(
                    "WpfLearningWorkflow.TrainingChecklist.Status.Segmentation",
                    state,
                    statistics.TotalImageCount,
                    BuildSegmentationPrimaryLabel(statistics),
                    statistics.TotalObjectCount),
                LabelingDatasetPurpose.AnomalyDetection => Text(
                    "WpfLearningWorkflow.TrainingChecklist.Status.Anomaly",
                    state,
                    statistics.TotalImageCount,
                    statistics.AnomalyNormalImageCount,
                    statistics.AnomalyAbnormalImageCount,
                    statistics.AnomalyUnreviewedImageCount),
                _ => Text(
                    "WpfLearningWorkflow.TrainingChecklist.Status.ObjectDetection",
                    state,
                    statistics.TotalImageCount,
                    statistics.TotalObjectCount,
                    statistics.TotalSegmentationObjectCount)
            };

            WpfTrainingChecklistTextDescriptor detail = purpose == LabelingDatasetPurpose.AnomalyDetection
                ? Text(
                    "WpfLearningWorkflow.TrainingChecklist.Detail.Ready.Anomaly",
                    BuildPurposeText(purpose),
                    statistics.TrainImageCount,
                    statistics.ValidImageCount,
                    statistics.TestImageCount,
                    statistics.AnomalyNormalImageCount,
                    statistics.AnomalyAbnormalImageCount,
                    statistics.AnomalyUnreviewedImageCount)
                : Text(
                    "WpfLearningWorkflow.TrainingChecklist.Detail.Ready.Standard",
                    BuildPurposeText(purpose),
                    statistics.TrainImageCount,
                    statistics.ValidImageCount,
                    statistics.TestImageCount,
                    statistics.TotalObjectCount,
                    statistics.TotalLabelFileCount,
                    statistics.TotalSegmentationObjectCount,
                    statistics.TotalSegmentFileCount,
                    statistics.TotalMaskFileCount,
                    classCount);

            return new WpfTrainingChecklistLocalizationSnapshot(status, detail);
        }

        public static WpfTrainingChecklistLocalizationSnapshot BuildFailure(
            string issueKind,
            string firstError,
            YoloDatasetStatistics statistics,
            LabelingDatasetPurpose purpose)
        {
            statistics ??= new YoloDatasetStatistics();
            object issueArgument = string.IsNullOrWhiteSpace(firstError)
                ? Text("WpfLearningWorkflow.TrainingChecklist.Issue.Unknown")
                : firstError.Trim();
            WpfTrainingChecklistTextDescriptor status = Text(GetFailureStatusKey(issueKind));
            WpfTrainingChecklistTextDescriptor detail = Text(
                "WpfLearningWorkflow.TrainingChecklist.Detail.Failure",
                BuildPurposeText(purpose),
                issueArgument,
                statistics.TrainImageCount,
                statistics.ValidImageCount,
                statistics.TestImageCount,
                statistics.TotalObjectCount,
                statistics.TotalSegmentationObjectCount,
                statistics.TotalMaskFileCount);
            return new WpfTrainingChecklistLocalizationSnapshot(status, detail);
        }

        private static WpfTrainingChecklistTextDescriptor BuildSegmentationPrimaryLabel(YoloDatasetStatistics statistics)
        {
            if (statistics.TotalSegmentationObjectCount > 0)
            {
                return Text(
                    "WpfLearningWorkflow.TrainingChecklist.Label.Segments",
                    statistics.TotalSegmentationObjectCount);
            }

            if (statistics.TotalMaskFileCount > 0)
            {
                return Text(
                    "WpfLearningWorkflow.TrainingChecklist.Label.Masks",
                    statistics.TotalMaskFileCount);
            }

            return Text("WpfLearningWorkflow.TrainingChecklist.Label.Segments", 0);
        }

        private static WpfTrainingChecklistTextDescriptor BuildPurposeText(LabelingDatasetPurpose purpose)
        {
            return Text(purpose switch
            {
                LabelingDatasetPurpose.Segmentation => "WpfLearningWorkflow.TrainingChecklist.Purpose.Segmentation",
                LabelingDatasetPurpose.AnomalyDetection => "WpfLearningWorkflow.TrainingChecklist.Purpose.Anomaly",
                _ => "WpfLearningWorkflow.TrainingChecklist.Purpose.ObjectDetection"
            });
        }

        private static string GetFailureStatusKey(string issueKind)
        {
            return issueKind switch
            {
                "Classes" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.Classes",
                "Labels" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.Labels",
                "SegmentationPolicy" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.SegmentationPolicy",
                "SegmentationLabels" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.SegmentationLabels",
                "ValidImages" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.ValidImages",
                "Split" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.Split",
                "DataYaml" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.DataYaml",
                "LabelFormat" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.LabelFormat",
                "OutputRoot" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.OutputRoot",
                "Images" => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.Images",
                _ => "WpfLearningWorkflow.TrainingChecklist.Status.Failure.Unknown"
            };
        }

        private static string GetFailureActionKey(string issueKind)
        {
            return issueKind switch
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
            };
        }

        private static WpfTrainingChecklistTextDescriptor BuildQualityWarningActionText(string warning)
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

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Text("WpfLearningWorkflow.DatasetDashboard.Action.Warning.Unknown");
            }

            return Text(
                "WpfLearningWorkflow.TrainingChecklist.Action.Warning.Raw",
                new WpfTrainingChecklistLocalizedArgument(() => FormatQualityWarning(normalized)));
        }

        private static WpfTrainingChecklistTextDescriptor Text(string key, params object[] arguments)
        {
            return new WpfTrainingChecklistTextDescriptor(key, arguments);
        }
    }
}
