using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public static class YoloDatasetHealthService
    {
        public static YoloDatasetHealthReport Build(LabelingProjectData data)
        {
            data?.ProjectSettings?.EnsureDefaults();
            LabelingDatasetPurpose purpose = data?.ProjectSettings?.DatasetPurpose ?? LabelingDatasetPurpose.ObjectDetection;
            if (purpose == LabelingDatasetPurpose.AnomalyDetection)
            {
                return BuildAnomalyReport(data);
            }

            YoloDatasetReadinessReport readiness = YoloDatasetReadinessService.Build(data, refreshYaml: false);
            YoloDatasetQualityAuditReport qualityAudit = purpose == LabelingDatasetPurpose.ObjectDetection && data != null
                ? YoloDatasetQualityAuditService.Build(data)
                : null;
            IReadOnlyList<YoloDatasetHealthSplitSummary> splits = purpose == LabelingDatasetPurpose.Segmentation
                ? BuildSegmentationSplits(readiness)
                : BuildDetectionSplits(qualityAudit);
            IReadOnlyList<YoloDatasetHealthClassSummary> classes = BuildClassSummaries(data, readiness?.Statistics, purpose);

            var issues = new List<string>();
            issues.AddRange(readiness?.Errors ?? Array.Empty<string>());
            issues.AddRange(YoloDatasetDiagnosticsService.BuildQualityWarnings(data, readiness?.Statistics));
            if (qualityAudit != null)
            {
                if (qualityAudit.TotalMissingLabelCount > 0)
                {
                    issues.Add($"dataset quality has {qualityAudit.TotalMissingLabelCount} missing label file(s)");
                }

                if (qualityAudit.TotalInvalidLabelLineCount > 0)
                {
                    issues.Add($"dataset quality has {qualityAudit.TotalInvalidLabelLineCount} invalid label line(s)");
                }
            }

            return new YoloDatasetHealthReport(
                purpose,
                readiness,
                anomalyReadiness: null,
                qualityAudit,
                splits,
                classes,
                NormalizeIssues(issues));
        }

        private static YoloDatasetHealthReport BuildAnomalyReport(LabelingProjectData data)
        {
            AnomalyClassificationTrainingReadinessReport readiness = AnomalyClassificationTrainingReadinessService.Build(data);
            var classes = new[]
            {
                new YoloDatasetHealthClassSummary("normal", readiness.NormalImageCount),
                new YoloDatasetHealthClassSummary("abnormal", readiness.AbnormalImageCount)
            };
            var issues = new List<string>(readiness.Errors ?? Array.Empty<string>());
            if (readiness.UnreviewedImageCount > 0)
            {
                issues.Add($"anomaly dataset has {readiness.UnreviewedImageCount} unreviewed image(s)");
            }

            return new YoloDatasetHealthReport(
                LabelingDatasetPurpose.AnomalyDetection,
                yoloReadiness: null,
                readiness,
                qualityAudit: null,
                splits: Array.Empty<YoloDatasetHealthSplitSummary>(),
                classes,
                NormalizeIssues(issues));
        }

        private static IReadOnlyList<YoloDatasetHealthSplitSummary> BuildDetectionSplits(YoloDatasetQualityAuditReport qualityAudit)
        {
            qualityAudit ??= new YoloDatasetQualityAuditReport();
            return qualityAudit.Splits
                .Select(split => new YoloDatasetHealthSplitSummary(
                    split.Split,
                    split.ImageCount,
                    split.ObjectCount,
                    split.LabelFileCount,
                    split.MissingLabelCount,
                    split.EmptyLabelCount,
                    split.InvalidLabelLineCount,
                    segmentFileCount: 0,
                    maskFileCount: 0,
                    auxiliaryBoxObjectCount: 0))
                .ToArray();
        }

        private static IReadOnlyList<YoloDatasetHealthSplitSummary> BuildSegmentationSplits(YoloDatasetReadinessReport readiness)
        {
            YoloDatasetStatistics statistics = readiness?.Statistics;
            statistics ??= new YoloDatasetStatistics();
            IReadOnlyList<string> qualityErrors = readiness?.TrainingFiles?.Errors ?? Array.Empty<string>();
            return new[]
            {
                BuildSegmentationSplit(
                    YoloDatasetSplitService.TrainMode,
                    statistics.TrainImageCount,
                    statistics.TrainSegmentFileCount,
                    statistics.TrainMaskFileCount,
                    statistics.TrainLabelCount,
                    statistics.TrainEmptyLabelFileCount,
                    qualityErrors),
                BuildSegmentationSplit(
                    YoloDatasetSplitService.ValidMode,
                    statistics.ValidImageCount,
                    statistics.ValidSegmentFileCount,
                    statistics.ValidMaskFileCount,
                    statistics.ValidLabelCount,
                    statistics.ValidEmptyLabelFileCount,
                    qualityErrors),
                BuildSegmentationSplit(
                    YoloDatasetSplitService.TestMode,
                    statistics.TestImageCount,
                    statistics.TestSegmentFileCount,
                    statistics.TestMaskFileCount,
                    statistics.TestLabelCount,
                    statistics.TestEmptyLabelFileCount,
                    qualityErrors)
            };
        }

        private static YoloDatasetHealthSplitSummary BuildSegmentationSplit(
            string split,
            int imageCount,
            int segmentFileCount,
            int maskFileCount,
            int labelFileCount,
            int emptyLabelCount,
            IReadOnlyList<string> qualityErrors)
        {
            string splitPrefix = (split ?? string.Empty) + " ";
            IEnumerable<string> splitErrors = (qualityErrors ?? Array.Empty<string>())
                .Where(error => (error ?? string.Empty).StartsWith(splitPrefix, StringComparison.OrdinalIgnoreCase));
            int missingCount = splitErrors.Count(YoloDatasetHealthReport.IsSegmentationMissingAnnotationIssue);
            int invalidCount = splitErrors.Count(YoloDatasetHealthReport.IsSegmentationQualityIssue) - missingCount;
            return new YoloDatasetHealthSplitSummary(
                split,
                imageCount,
                Math.Max(segmentFileCount, maskFileCount),
                labelFileCount,
                missingCount,
                emptyLabelCount,
                Math.Max(0, invalidCount),
                segmentFileCount,
                maskFileCount,
                auxiliaryBoxObjectCount: 0);
        }

        private static IReadOnlyList<YoloDatasetHealthClassSummary> BuildClassSummaries(
            LabelingProjectData data,
            YoloDatasetStatistics statistics,
            LabelingDatasetPurpose purpose)
        {
            statistics ??= new YoloDatasetStatistics();
            IReadOnlyDictionary<string, int> source = purpose == LabelingDatasetPurpose.Segmentation
                ? statistics.SegmentationObjectCountByClass
                : statistics.ObjectCountByClass;
            List<string> classNames = data?.ClassNamedList?
                .Select(item => item?.Text?.Trim() ?? string.Empty)
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            if (classNames.Count == 0)
            {
                classNames = source.Keys.ToList();
            }

            return classNames
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name =>
                {
                    source.TryGetValue(name, out int count);
                    return new YoloDatasetHealthClassSummary(name, count);
                })
                .ToArray();
        }

        private static IReadOnlyList<string> NormalizeIssues(IEnumerable<string> issues)
        {
            return (issues ?? Enumerable.Empty<string>())
                .Select(item => item?.Trim() ?? string.Empty)
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
