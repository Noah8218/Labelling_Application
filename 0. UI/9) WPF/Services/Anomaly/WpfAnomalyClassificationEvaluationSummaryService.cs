using MvcVisionSystem.Yolo;
using System;
using System.IO;
using System.Linq;

namespace MvcVisionSystem
{
    public sealed class WpfAnomalyClassificationEvaluationSummary
    {
        public WpfAnomalyClassificationEvaluationSummary(
            string summaryPath,
            AnomalyClassificationEvaluationReport report,
            AnomalyClassificationEvaluationOptions options)
        {
            SummaryPath = summaryPath ?? string.Empty;
            Report = report ?? new AnomalyClassificationEvaluationReport();
            Options = options ?? new AnomalyClassificationEvaluationOptions();
        }

        public string SummaryPath { get; }

        public AnomalyClassificationEvaluationReport Report { get; }

        public AnomalyClassificationEvaluationOptions Options { get; }
    }

    public sealed class WpfAnomalyClassificationEvaluationSummaryService
    {
        private const string SummaryFileName = "classification-evaluation-summary.json";

        public string ResolveSummaryPath(string outputRootPath, string preferredSummaryPath = "")
        {
            string preferredPath = preferredSummaryPath?.Trim() ?? string.Empty;
            if (File.Exists(preferredPath))
            {
                return preferredPath;
            }

            if (string.IsNullOrWhiteSpace(outputRootPath))
            {
                return string.Empty;
            }

            string root = outputRootPath.Trim();
            string directPath = Path.Combine(root, SummaryFileName);
            if (File.Exists(directPath))
            {
                return directPath;
            }

            string evaluationPath = Path.Combine(root, "classification-evaluation", SummaryFileName);
            if (File.Exists(evaluationPath))
            {
                return evaluationPath;
            }

            try
            {
                if (!Directory.Exists(root))
                {
                    return string.Empty;
                }

                return Directory
                    .EnumerateDirectories(root, "classification-evaluation-*", SearchOption.TopDirectoryOnly)
                    .Select(directory => new FileInfo(Path.Combine(directory, SummaryFileName)))
                    .Where(summary => summary.Exists)
                    .OrderByDescending(summary => summary.LastWriteTimeUtc)
                    .Select(summary => summary.FullName)
                    .FirstOrDefault() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool TryReadSummary(
            string summaryPath,
            out WpfAnomalyClassificationEvaluationSummary summary)
        {
            summary = null;
            if (string.IsNullOrWhiteSpace(summaryPath) || !File.Exists(summaryPath))
            {
                return false;
            }

            try
            {
                AnomalyClassificationEvaluationReport report =
                    AnomalyClassificationEvaluationService.ReadSummaryFile(summaryPath, out AnomalyClassificationEvaluationOptions options);
                summary = new WpfAnomalyClassificationEvaluationSummary(summaryPath, report, options);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
