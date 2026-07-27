using System;
using MvcVisionSystem._1._Core;

namespace MvcVisionSystem.Yolo
{
    public static class YoloDatasetReadinessService
    {
        public static YoloDatasetReadinessReport Build(CData data, bool refreshYaml)
        {
            LabelingDatasetPurpose purpose = ResolveDatasetPurpose(data);
            YoloDatasetValidationResult configuration = purpose == LabelingDatasetPurpose.AnomalyDetection
                ? YoloDatasetValidator.ValidateAnomalyClassificationConfiguration(data)
                : YoloDatasetValidator.ValidateConfiguration(data);
            if (!configuration.IsValid)
            {
                return new YoloDatasetReadinessReport(
                    configuration,
                    new YoloDatasetValidationResult(Array.Empty<string>()),
                    new YoloDatasetStatistics(),
                    purpose);
            }

            if (purpose == LabelingDatasetPurpose.AnomalyDetection)
            {
                AnomalyClassificationTrainingReadinessReport anomaly =
                    AnomalyClassificationTrainingReadinessService.Build(data);
                var anomalyStatistics = new YoloDatasetStatistics
                {
                    TrainImageCount = anomaly.TrainImageCount,
                    ValidImageCount = anomaly.ValidImageCount,
                    TestImageCount = anomaly.TestImageCount,
                    TrainLabelCount = anomaly.TrainImageCount,
                    ValidLabelCount = anomaly.ValidImageCount,
                    TestLabelCount = anomaly.TestImageCount,
                    AnomalyNormalImageCount = anomaly.NormalImageCount,
                    AnomalyAbnormalImageCount = anomaly.AbnormalImageCount,
                    AnomalyUnreviewedImageCount = anomaly.UnreviewedImageCount
                };
                return new YoloDatasetReadinessReport(
                    configuration,
                    new YoloDatasetValidationResult(anomaly.Errors),
                    anomalyStatistics,
                    purpose);
            }

            if (refreshYaml)
            {
                data.SaveYoloDataYaml();
            }

            YoloDatasetValidationResult files = YoloDatasetValidator.ValidateTrainingFiles(data);
            // Keep statistics even when readiness fails so the operator sees the scale of the issue
            // (for example, 125 duplicated train/valid images) instead of a vague "not ready" state.
            YoloDatasetStatistics statistics = YoloDatasetValidator.BuildStatistics(data);

            return new YoloDatasetReadinessReport(configuration, files, statistics, purpose);
        }

        private static LabelingDatasetPurpose ResolveDatasetPurpose(CData data)
        {
            data?.ProjectSettings?.EnsureDefaults();
            return data?.ProjectSettings?.DatasetPurpose ?? LabelingDatasetPurpose.ObjectDetection;
        }
    }
}
