using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public enum YoloDatasetHealthQualityStatus
    {
        NotEvaluated,
        Healthy,
        ProblemsFound
    }

    // Read-only aggregation for the on-demand Dataset Health view. It deliberately
    // reuses the existing readiness/audit services instead of changing labeling paths.
    public sealed class YoloDatasetHealthReport
    {
        public YoloDatasetHealthReport(
            LabelingDatasetPurpose purpose,
            YoloDatasetReadinessReport yoloReadiness,
            AnomalyClassificationTrainingReadinessReport anomalyReadiness,
            YoloDatasetQualityAuditReport qualityAudit,
            IReadOnlyList<YoloDatasetHealthSplitSummary> splits,
            IReadOnlyList<YoloDatasetHealthClassSummary> classes,
            IReadOnlyList<string> issues)
        {
            Purpose = purpose;
            YoloReadiness = yoloReadiness;
            AnomalyReadiness = anomalyReadiness;
            QualityAudit = qualityAudit;
            Splits = splits ?? Array.Empty<YoloDatasetHealthSplitSummary>();
            Classes = classes ?? Array.Empty<YoloDatasetHealthClassSummary>();
            Issues = issues ?? Array.Empty<string>();
        }

        public LabelingDatasetPurpose Purpose { get; }

        public YoloDatasetReadinessReport YoloReadiness { get; }

        public AnomalyClassificationTrainingReadinessReport AnomalyReadiness { get; }

        public YoloDatasetQualityAuditReport QualityAudit { get; }

        public IReadOnlyList<YoloDatasetHealthSplitSummary> Splits { get; }

        public IReadOnlyList<YoloDatasetHealthClassSummary> Classes { get; }

        public IReadOnlyList<string> Issues { get; }

        public bool IsReady => Purpose == LabelingDatasetPurpose.AnomalyDetection
            ? AnomalyReadiness?.IsReady == true
            : YoloReadiness?.IsReady == true;

        public int TotalImageCount => Purpose == LabelingDatasetPurpose.AnomalyDetection
            ? AnomalyReadiness?.SourceImageCount ?? 0
            : YoloReadiness?.Statistics?.TotalImageCount ?? 0;

        public int PrimaryLabelCount => Purpose switch
        {
            LabelingDatasetPurpose.Segmentation => YoloReadiness?.Statistics?.TotalSegmentationObjectCount ?? 0,
            LabelingDatasetPurpose.AnomalyDetection => (AnomalyReadiness?.NormalImageCount ?? 0) + (AnomalyReadiness?.AbnormalImageCount ?? 0),
            _ => YoloReadiness?.Statistics?.TotalObjectCount ?? 0
        };

        public int SplitContentOverlapCount => YoloReadiness?.Statistics?.SplitImageContentOverlapCount ?? 0;

        public int QualityProblemCount => Purpose == LabelingDatasetPurpose.AnomalyDetection
            ? AnomalyReadiness?.UnreviewedImageCount ?? 0
            : Purpose == LabelingDatasetPurpose.Segmentation
                ? (YoloReadiness?.TrainingFiles?.Errors ?? Array.Empty<string>()).Count(IsSegmentationQualityIssue)
            : (QualityAudit?.TotalMissingLabelCount ?? 0)
                + (QualityAudit?.TotalInvalidLabelLineCount ?? 0)
                + (YoloReadiness?.TrainingFiles?.Errors ?? Array.Empty<string>()).Count(IsDatasetIntegrityIssue);

        public YoloDatasetHealthQualityStatus QualityStatus => Purpose switch
        {
            LabelingDatasetPurpose.AnomalyDetection => AnomalyReadiness == null || AnomalyReadiness.SourceImageCount == 0
                ? YoloDatasetHealthQualityStatus.NotEvaluated
                : QualityProblemCount > 0
                    ? YoloDatasetHealthQualityStatus.ProblemsFound
                    : YoloDatasetHealthQualityStatus.Healthy,
            LabelingDatasetPurpose.Segmentation => YoloReadiness?.Configuration?.IsValid != true
                || YoloReadiness?.Statistics?.TotalImageCount <= 0
                ? YoloDatasetHealthQualityStatus.NotEvaluated
                : QualityProblemCount > 0
                    ? YoloDatasetHealthQualityStatus.ProblemsFound
                    : YoloDatasetHealthQualityStatus.Healthy,
            _ => QualityAudit == null || QualityAudit.TotalImageCount == 0
                ? YoloDatasetHealthQualityStatus.NotEvaluated
                : QualityProblemCount > 0
                    ? YoloDatasetHealthQualityStatus.ProblemsFound
                    : YoloDatasetHealthQualityStatus.Healthy
        };

        internal static bool IsSegmentationQualityIssue(string issue)
        {
            string normalized = issue ?? string.Empty;
            return IsDatasetIntegrityIssue(normalized)
                || IsSegmentationMissingAnnotationIssue(normalized)
                || normalized.Contains("segment JSON is invalid", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("segment polygon has fewer than three points", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("segment polygon has invalid class index", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("segment JSON has no polygons", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Segmentation dataset has no segment JSON or mask PNG annotations", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsSegmentationMissingAnnotationIssue(string issue)
        {
            return (issue ?? string.Empty).Contains(
                "segmentation annotation or empty background label is missing",
                StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsDatasetIntegrityIssue(string issue)
            => (issue ?? string.Empty).Contains("dataset integrity:", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class YoloDatasetHealthSplitSummary
    {
        public YoloDatasetHealthSplitSummary(
            string split,
            int imageCount,
            int primaryAnnotationCount,
            int labelFileCount,
            int missingLabelCount,
            int emptyLabelCount,
            int invalidLabelLineCount,
            int segmentFileCount,
            int maskFileCount,
            int auxiliaryBoxObjectCount)
        {
            Split = split ?? string.Empty;
            ImageCount = Math.Max(0, imageCount);
            PrimaryAnnotationCount = Math.Max(0, primaryAnnotationCount);
            LabelFileCount = Math.Max(0, labelFileCount);
            MissingLabelCount = Math.Max(0, missingLabelCount);
            EmptyLabelCount = Math.Max(0, emptyLabelCount);
            InvalidLabelLineCount = Math.Max(0, invalidLabelLineCount);
            SegmentFileCount = Math.Max(0, segmentFileCount);
            MaskFileCount = Math.Max(0, maskFileCount);
            AuxiliaryBoxObjectCount = Math.Max(0, auxiliaryBoxObjectCount);
        }

        public string Split { get; }

        public int ImageCount { get; }

        public int PrimaryAnnotationCount { get; }

        public int LabelFileCount { get; }

        public int MissingLabelCount { get; }

        public int EmptyLabelCount { get; }

        public int InvalidLabelLineCount { get; }

        public int SegmentFileCount { get; }

        public int MaskFileCount { get; }

        public int AuxiliaryBoxObjectCount { get; }
    }

    public sealed class YoloDatasetHealthClassSummary
    {
        public YoloDatasetHealthClassSummary(string className, int count)
        {
            ClassName = className ?? string.Empty;
            Count = Math.Max(0, count);
        }

        public string ClassName { get; }

        public int Count { get; }
    }
}
