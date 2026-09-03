using System;

namespace MvcVisionSystem
{
    public enum WpfModelHistoryAdoptionPlanStatus
    {
        MissingSelection,
        MissingWeightsPath,
        CandidateWeightsFileMissing,
        AlreadyCurrent,
        Ready
    }

    public sealed class WpfModelHistoryAdoptionRequest
    {
        public bool HasSelection { get; set; }

        public string CandidateWeightsPath { get; set; } = string.Empty;

        public string CurrentWeightsPath { get; set; } = string.Empty;

        public string FallbackBaselineWeightsPath { get; set; } = string.Empty;

        public string MetricText { get; set; } = string.Empty;

        public string DecisionText { get; set; } = string.Empty;

        public bool CandidateWeightsFileExists { get; set; }
    }

    public sealed class WpfModelHistoryAdoptionPlan
    {
        public WpfModelHistoryAdoptionPlan(
            WpfModelHistoryAdoptionPlanStatus status,
            string candidateWeightsPath = "",
            string baselineWeightsPath = "",
            string metricsSummary = "",
            string decisionSummary = "")
        {
            Status = status;
            CandidateWeightsPath = candidateWeightsPath ?? string.Empty;
            BaselineWeightsPath = baselineWeightsPath ?? string.Empty;
            MetricsSummary = metricsSummary ?? string.Empty;
            DecisionSummary = decisionSummary ?? string.Empty;
        }

        public WpfModelHistoryAdoptionPlanStatus Status { get; }

        public string CandidateWeightsPath { get; }

        public string BaselineWeightsPath { get; }

        public string MetricsSummary { get; }

        public string DecisionSummary { get; }

        public bool IsReady => Status == WpfModelHistoryAdoptionPlanStatus.Ready;

        public bool IsAlreadyCurrent => Status == WpfModelHistoryAdoptionPlanStatus.AlreadyCurrent;
    }

    /// <summary>
    /// Prepares the explicit model-history adoption request without changing
    /// settings, registry history, or Recipe persistence.
    /// </summary>
    public static class WpfModelHistoryAdoptionPlanningService
    {
        public static WpfModelHistoryAdoptionPlan Build(WpfModelHistoryAdoptionRequest request)
        {
            request ??= new WpfModelHistoryAdoptionRequest();
            if (!request.HasSelection)
            {
                return new WpfModelHistoryAdoptionPlan(WpfModelHistoryAdoptionPlanStatus.MissingSelection);
            }

            string candidateWeightsPath = Normalize(request.CandidateWeightsPath);
            if (string.IsNullOrWhiteSpace(candidateWeightsPath))
            {
                return new WpfModelHistoryAdoptionPlan(WpfModelHistoryAdoptionPlanStatus.MissingWeightsPath);
            }

            if (!request.CandidateWeightsFileExists)
            {
                return new WpfModelHistoryAdoptionPlan(
                    WpfModelHistoryAdoptionPlanStatus.CandidateWeightsFileMissing,
                    candidateWeightsPath: candidateWeightsPath);
            }

            string currentWeightsPath = Normalize(request.CurrentWeightsPath);
            if (string.Equals(currentWeightsPath, candidateWeightsPath, StringComparison.OrdinalIgnoreCase))
            {
                return new WpfModelHistoryAdoptionPlan(
                    WpfModelHistoryAdoptionPlanStatus.AlreadyCurrent,
                    candidateWeightsPath: candidateWeightsPath);
            }

            string baselineWeightsPath = !string.IsNullOrWhiteSpace(currentWeightsPath)
                ? currentWeightsPath
                : Normalize(request.FallbackBaselineWeightsPath);
            string metricsSummary = !string.IsNullOrWhiteSpace(request.MetricText)
                ? request.MetricText.Trim()
                : Normalize(request.DecisionText);

            return new WpfModelHistoryAdoptionPlan(
                WpfModelHistoryAdoptionPlanStatus.Ready,
                candidateWeightsPath,
                baselineWeightsPath,
                metricsSummary,
                "모델 이력에서 검사 모델로 적용");
        }

        private static string Normalize(string value)
            => value?.Trim() ?? string.Empty;
    }
}
