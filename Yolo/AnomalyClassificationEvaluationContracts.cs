using System;
using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class AnomalyClassificationEvaluationSample
    {
        public string ImagePath { get; set; } = string.Empty;

        public string ExpectedClassName { get; set; } = string.Empty;

        public string PredictedClassName { get; set; } = string.Empty;

        public double Confidence { get; set; }
    }

    public sealed class AnomalyClassificationEvaluationOptions
    {
        public int MinimumTotalImageCount { get; set; } = 10;

        public int MinimumPerClassImageCount { get; set; } = 5;

        public double MinimumAccuracy { get; set; } = 0.9D;

        public double MinimumPerClassAccuracy { get; set; } = 0.8D;

        public double MinimumConfidence { get; set; } = 0D;
    }

    public sealed class AnomalyClassificationEvaluationReport
    {
        public string ModelName { get; set; } = string.Empty;

        public int TotalImageCount { get; set; }

        public int NormalImageCount { get; set; }

        public int AbnormalImageCount { get; set; }

        public int CorrectImageCount { get; set; }

        public int NormalCorrectCount { get; set; }

        public int AbnormalCorrectCount { get; set; }

        public int LowConfidenceClassMatchCount { get; set; }

        public double Accuracy { get; set; }

        public double NormalAccuracy { get; set; }

        public double AbnormalAccuracy { get; set; }

        public double BalancedAccuracy { get; set; }

        public int FalsePositiveCount { get; set; }

        public int FalseNegativeCount { get; set; }

        public int LocalizationEvidenceCount { get; set; }

        public int HeatmapEvidenceCount { get; set; }

        public string LocalizationGroundTruthStatus { get; set; } = string.Empty;

        public IReadOnlyList<string> HoldReasons { get; set; } = Array.Empty<string>();

        public string Recommendation { get; set; } = string.Empty;

        public bool IsAdoptionCandidate
            => HoldReasons.Count == 0
                && string.Equals(Recommendation, "adopt", StringComparison.OrdinalIgnoreCase);
    }
}
