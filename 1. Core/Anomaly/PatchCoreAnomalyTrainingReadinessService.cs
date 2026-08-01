using MvcVisionSystem.Yolo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MvcVisionSystem._1._Core
{
    public sealed class PatchCoreAnomalyTrainingReadinessReport
    {
        public IReadOnlyList<string> SourceImagePaths { get; init; } = Array.Empty<string>();
        public int ReviewedNormalCount { get; init; }
        public int ReviewedAbnormalCount { get; init; }
        public int UnreviewedCount { get; init; }
        public int TrainNormalCount { get; init; }
        public int ValidationNormalCount { get; init; }
        public int TestNormalCount { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool IsReady => Errors.Count == 0;
    }

    public static class PatchCoreAnomalyTrainingReadinessService
    {
        public const string WrongPurposeError = "PatchCore training requires an anomaly-detection recipe";
        public const string NoSourceImagesError = "PatchCore training needs source images";
        public const string NeedsReviewedNormalError = "PatchCore training needs at least two reviewed normal train images";
        public const string NoIndependentCalibrationWarning = "PatchCore threshold will use train-normal fallback because no reviewed normal validation image exists";

        public static PatchCoreAnomalyTrainingReadinessReport Build(CData data)
        {
            string[] sourceImages = EnumerateSourceImages(data).ToArray();
            var errors = new List<string>();
            var warnings = new List<string>();
            if (data?.ProjectSettings?.DatasetPurpose != LabelingDatasetPurpose.AnomalyDetection)
            {
                errors.Add(WrongPurposeError);
            }

            if (sourceImages.Length == 0)
            {
                errors.Add(NoSourceImagesError);
                return new PatchCoreAnomalyTrainingReadinessReport
                {
                    SourceImagePaths = sourceImages,
                    Errors = errors
                };
            }

            var reviewStatus = new AnomalyImageReviewStatusService();
            reviewStatus.LoadReviewStatus(data, sourceImages);
            int normal = 0;
            int abnormal = 0;
            int unreviewed = 0;
            int trainNormal = 0;
            int validationNormal = 0;
            int testNormal = 0;
            foreach (AnomalyImageReviewStatus item in reviewStatus.GetItems())
            {
                if (!item.IsReviewed)
                {
                    unreviewed++;
                    continue;
                }

                if (item.ReviewState == AnomalyImageReviewState.Abnormal)
                {
                    abnormal++;
                    continue;
                }

                normal++;
                string split = YoloDatasetSplitService.SelectModesForImage(
                    item.ImageName,
                    data?.ProjectSettings?.YoloDataset).FirstOrDefault() ?? YoloDatasetSplitService.TrainMode;
                if (string.Equals(split, YoloDatasetSplitService.TrainMode, StringComparison.OrdinalIgnoreCase))
                {
                    trainNormal++;
                }
                else if (string.Equals(split, YoloDatasetSplitService.ValidMode, StringComparison.OrdinalIgnoreCase))
                {
                    validationNormal++;
                }
                else if (string.Equals(split, YoloDatasetSplitService.TestMode, StringComparison.OrdinalIgnoreCase))
                {
                    testNormal++;
                }
            }

            if (trainNormal < 2)
            {
                errors.Add($"{NeedsReviewedNormalError}. TrainNormal:{trainNormal}, ReviewedNormal:{normal}, Unreviewed:{unreviewed}");
            }

            if (validationNormal == 0)
            {
                warnings.Add(NoIndependentCalibrationWarning);
            }

            return new PatchCoreAnomalyTrainingReadinessReport
            {
                SourceImagePaths = sourceImages,
                ReviewedNormalCount = normal,
                ReviewedAbnormalCount = abnormal,
                UnreviewedCount = unreviewed,
                TrainNormalCount = trainNormal,
                ValidationNormalCount = validationNormal,
                TestNormalCount = testNormal,
                Errors = errors,
                Warnings = warnings
            };
        }

        private static IEnumerable<string> EnumerateSourceImages(CData data)
        {
            return new[]
                {
                    data?.ProjectSettings?.PythonModel?.ImageRootPath,
                    data?.TrainImagesPath,
                    data?.ValidImagesPath,
                    data?.TestImagesPath
                }
                .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                .SelectMany(path => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                .Where(path => new[] { ".bmp", ".jpg", ".jpeg", ".png", ".tif", ".tiff" }
                    .Contains(Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
        }
    }
}
