using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MvcVisionSystem.Yolo
{
    public static class AnomalyClassificationEvaluationService
    {
        public const string NormalClassName = "normal";
        public const string AbnormalClassName = "abnormal";

        public static AnomalyClassificationEvaluationReport ReadSummaryFile(string summaryPath)
            => ReadSummaryFile(summaryPath, out _);

        public static AnomalyClassificationEvaluationReport ReadSummaryFile(
            string summaryPath,
            out AnomalyClassificationEvaluationOptions options)
        {
            if (string.IsNullOrWhiteSpace(summaryPath) || !File.Exists(summaryPath))
            {
                throw new FileNotFoundException("Anomaly classification evaluation summary was not found.", summaryPath);
            }

            return ParseSummaryJson(File.ReadAllText(summaryPath), out options);
        }

        public static AnomalyClassificationEvaluationReport ParseSummaryJson(string summaryJson)
            => ParseSummaryJson(summaryJson, out _);

        public static AnomalyClassificationEvaluationReport ParseSummaryJson(
            string summaryJson,
            out AnomalyClassificationEvaluationOptions options)
        {
            options = new AnomalyClassificationEvaluationOptions();
            if (string.IsNullOrWhiteSpace(summaryJson))
            {
                return new AnomalyClassificationEvaluationReport();
            }

            using JsonDocument document = JsonDocument.Parse(summaryJson);
            JsonElement root = document.RootElement;
            JsonElement metrics = TryGetProperty(root, "metrics");
            JsonElement promotion = TryGetProperty(root, "promotion");
            JsonElement localization = TryGetProperty(root, "localization");
            JsonElement thresholds = TryGetProperty(root, "thresholds");

            options.MinimumTotalImageCount = ReadInt(
                thresholds,
                "minimumTotalImageCount",
                options.MinimumTotalImageCount);
            options.MinimumPerClassImageCount = ReadInt(
                thresholds,
                "minimumPerClassImageCount",
                options.MinimumPerClassImageCount);
            options.MinimumAccuracy = ReadDouble(
                thresholds,
                "minimumAccuracy",
                options.MinimumAccuracy);
            options.MinimumPerClassAccuracy = ReadDouble(
                thresholds,
                "minimumPerClassAccuracy",
                options.MinimumPerClassAccuracy);
            options.MinimumConfidence = ReadDouble(
                thresholds,
                "minimumConfidence",
                options.MinimumConfidence);

            var report = new AnomalyClassificationEvaluationReport
            {
                ModelName = ReadString(root, "modelName"),
                TotalImageCount = ReadInt(metrics, "totalImageCount"),
                NormalImageCount = ReadInt(metrics, "normalImageCount"),
                AbnormalImageCount = ReadInt(metrics, "abnormalImageCount"),
                CorrectImageCount = ReadInt(metrics, "correctImageCount"),
                NormalCorrectCount = ReadInt(metrics, "normalCorrectCount"),
                AbnormalCorrectCount = ReadInt(metrics, "abnormalCorrectCount"),
                LowConfidenceClassMatchCount = ReadInt(metrics, "lowConfidenceClassMatchCount"),
                Accuracy = ReadDouble(metrics, "accuracy"),
                NormalAccuracy = ReadDouble(metrics, "normalAccuracy"),
                AbnormalAccuracy = ReadDouble(metrics, "abnormalAccuracy"),
                BalancedAccuracy = ReadDouble(metrics, "balancedAccuracy"),
                FalsePositiveCount = ReadInt(metrics, "falsePositiveCount"),
                FalseNegativeCount = ReadInt(metrics, "falseNegativeCount"),
                LocalizationEvidenceCount = ReadInt(metrics, "localizationEvidenceCount"),
                HeatmapEvidenceCount = ReadInt(metrics, "heatmapEvidenceCount"),
                LocalizationGroundTruthStatus = ReadString(localization, "groundTruthStatus"),
                Recommendation = ReadString(promotion, "recommendation"),
                HoldReasons = ReadStringArray(promotion, "reasons")
            };
            if (!HasProperty(metrics, "balancedAccuracy"))
            {
                report.BalancedAccuracy = (report.NormalAccuracy + report.AbnormalAccuracy) / 2D;
            }

            if (!HasProperty(metrics, "falsePositiveCount"))
            {
                report.FalsePositiveCount = Math.Max(0, report.NormalImageCount - report.NormalCorrectCount);
            }

            if (!HasProperty(metrics, "falseNegativeCount"))
            {
                report.FalseNegativeCount = Math.Max(0, report.AbnormalImageCount - report.AbnormalCorrectCount);
            }

            return report;
        }

        public static AnomalyClassificationEvaluationReport Build(
            IEnumerable<AnomalyClassificationEvaluationSample> samples,
            AnomalyClassificationEvaluationOptions options = null)
        {
            options ??= new AnomalyClassificationEvaluationOptions();
            AnomalyClassificationEvaluationSample[] items = (samples ?? Enumerable.Empty<AnomalyClassificationEvaluationSample>())
                .Where(sample => sample != null)
                .ToArray();

            int totalCount = items.Length;
            int normalCount = CountExpected(items, NormalClassName);
            int abnormalCount = CountExpected(items, AbnormalClassName);
            int normalCorrect = CountCorrect(items, NormalClassName, options.MinimumConfidence);
            int abnormalCorrect = CountCorrect(items, AbnormalClassName, options.MinimumConfidence);
            int correctCount = normalCorrect + abnormalCorrect;
            int lowConfidenceClassMatchCount = CountLowConfidenceClassMatches(items, options.MinimumConfidence);

            double accuracy = SafeRatio(correctCount, totalCount);
            double normalAccuracy = SafeRatio(normalCorrect, normalCount);
            double abnormalAccuracy = SafeRatio(abnormalCorrect, abnormalCount);
            double balancedAccuracy = (normalAccuracy + abnormalAccuracy) / 2D;
            var holdReasons = new List<string>();

            if (totalCount < Math.Max(1, options.MinimumTotalImageCount))
            {
                holdReasons.Add($"Evaluation uses {totalCount} images; collect at least {options.MinimumTotalImageCount} held-out images.");
            }

            if (normalCount < Math.Max(1, options.MinimumPerClassImageCount))
            {
                holdReasons.Add($"Evaluation uses {normalCount} normal images; collect at least {options.MinimumPerClassImageCount} normal held-out images.");
            }

            if (abnormalCount < Math.Max(1, options.MinimumPerClassImageCount))
            {
                holdReasons.Add($"Evaluation uses {abnormalCount} abnormal images; collect at least {options.MinimumPerClassImageCount} abnormal held-out images.");
            }

            if (accuracy < Clamp01(options.MinimumAccuracy))
            {
                holdReasons.Add($"Accuracy {FormatRatio(accuracy)} is below minimum {FormatRatio(options.MinimumAccuracy)}.");
            }

            if (lowConfidenceClassMatchCount > 0)
            {
                holdReasons.Add($"{lowConfidenceClassMatchCount} class-matching predictions were below minimum confidence {FormatRatio(options.MinimumConfidence)}.");
            }

            if (normalAccuracy < Clamp01(options.MinimumPerClassAccuracy))
            {
                holdReasons.Add($"Normal accuracy {FormatRatio(normalAccuracy)} is below minimum {FormatRatio(options.MinimumPerClassAccuracy)}.");
            }

            if (abnormalAccuracy < Clamp01(options.MinimumPerClassAccuracy))
            {
                holdReasons.Add($"Abnormal accuracy {FormatRatio(abnormalAccuracy)} is below minimum {FormatRatio(options.MinimumPerClassAccuracy)}.");
            }

            return new AnomalyClassificationEvaluationReport
            {
                TotalImageCount = totalCount,
                NormalImageCount = normalCount,
                AbnormalImageCount = abnormalCount,
                CorrectImageCount = correctCount,
                NormalCorrectCount = normalCorrect,
                AbnormalCorrectCount = abnormalCorrect,
                LowConfidenceClassMatchCount = lowConfidenceClassMatchCount,
                Accuracy = accuracy,
                NormalAccuracy = normalAccuracy,
                AbnormalAccuracy = abnormalAccuracy,
                BalancedAccuracy = balancedAccuracy,
                FalsePositiveCount = Math.Max(0, normalCount - normalCorrect),
                FalseNegativeCount = Math.Max(0, abnormalCount - abnormalCorrect),
                Recommendation = holdReasons.Count == 0 ? "adopt" : "hold",
                HoldReasons = holdReasons
            };
        }

        private static int CountExpected(IEnumerable<AnomalyClassificationEvaluationSample> samples, string className)
            => samples.Count(sample => IsClass(sample.ExpectedClassName, className));

        private static JsonElement TryGetProperty(JsonElement element, string propertyName)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement value)
                ? value
                : default;

        private static bool HasProperty(JsonElement element, string propertyName)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out _);

        private static int ReadInt(JsonElement element, string propertyName)
            => ReadInt(element, propertyName, 0);

        private static int ReadInt(JsonElement element, string propertyName, int fallback)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement value))
            {
                return fallback;
            }

            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : fallback;
        }

        private static double ReadDouble(JsonElement element, string propertyName)
            => ReadDouble(element, propertyName, 0D);

        private static double ReadDouble(JsonElement element, string propertyName, double fallback)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement value))
            {
                return fallback;
            }

            return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double result) ? Clamp01(result) : fallback;
        }

        private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out JsonElement value)
                || value.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement value))
            {
                return string.Empty;
            }

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static int CountCorrect(IEnumerable<AnomalyClassificationEvaluationSample> samples, string className, double minimumConfidence)
        {
            double threshold = Clamp01(minimumConfidence);
            return samples.Count(sample =>
                IsClass(sample.ExpectedClassName, className)
                && IsClass(sample.PredictedClassName, className)
                && sample.Confidence >= threshold);
        }

        private static int CountLowConfidenceClassMatches(IEnumerable<AnomalyClassificationEvaluationSample> samples, double minimumConfidence)
        {
            double threshold = Clamp01(minimumConfidence);
            if (threshold <= 0D)
            {
                return 0;
            }

            return samples.Count(sample =>
                IsClass(sample.ExpectedClassName, sample.PredictedClassName)
                && sample.Confidence < threshold);
        }

        private static bool IsClass(string value, string className)
            => string.Equals((value ?? string.Empty).Trim(), className, StringComparison.OrdinalIgnoreCase);

        private static double SafeRatio(int numerator, int denominator)
            => denominator <= 0 ? 0D : Math.Clamp((double)numerator / denominator, 0D, 1D);

        private static double Clamp01(double value)
            => double.IsNaN(value) ? 0D : Math.Clamp(value, 0D, 1D);

        private static string FormatRatio(double value)
            => Clamp01(value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
