using MvcVisionSystem.Yolo;
using OpenVisionLab;
using System;
using System.Globalization;
using System.Linq;

namespace MvcVisionSystem
{
    /// <summary>
    /// Keeps Model Replacement status/detail as catalog descriptors so the same
    /// held-out evidence can be rendered again after a language change.
    /// </summary>
    public sealed class WpfModelReplacementLocalizationSnapshot
    {
        private readonly WpfModelReplacementTextDescriptor statusText;
        private readonly WpfModelReplacementTextDescriptor detailText;

        internal WpfModelReplacementLocalizationSnapshot(
            WpfModelReplacementTextDescriptor statusText,
            WpfModelReplacementTextDescriptor detailText)
        {
            this.statusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
            this.detailText = detailText ?? throw new ArgumentNullException(nameof(detailText));
        }

        public string StatusText => statusText.Render();

        public string DetailText => detailText.Render();
    }

    internal sealed class WpfModelReplacementTextDescriptor
    {
        private readonly string key;
        private readonly object[] arguments;

        internal WpfModelReplacementTextDescriptor(string key, params object[] arguments)
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
            return argument is WpfModelReplacementTextDescriptor descriptor
                ? descriptor.Render()
                : argument ?? string.Empty;
        }
    }

    public static class WpfModelReplacementLocalizationService
    {
        private const int RecommendedTestImageCount = 10;

        public static WpfModelReplacementLocalizationSnapshot CreateInitial()
        {
            return new WpfModelReplacementLocalizationSnapshot(
                Text("WpfLearningWorkflow.ModelReplacement.Status.Initial"),
                Text("WpfLearningWorkflow.ModelReplacement.Detail.Initial"));
        }

        public static WpfModelReplacementLocalizationSnapshot Build(
            YoloDatasetReadinessReport report,
            YoloDatasetStatistics statistics)
        {
            statistics ??= report?.Statistics ?? new YoloDatasetStatistics();
            if (report?.IsReady != true)
            {
                return new WpfModelReplacementLocalizationSnapshot(
                    Text("WpfLearningWorkflow.ModelReplacement.Status.Unavailable"),
                    Text("WpfLearningWorkflow.ModelReplacement.Detail.Unavailable"));
            }

            int testImageCount = statistics.TestImageCount;
            int testLabelCount = statistics.TestLabelCount;
            int finalVerificationCount = Math.Min(testImageCount, testLabelCount);
            WpfModelReplacementTextDescriptor status = testImageCount > 0 && testLabelCount > 0
                ? Text(
                    finalVerificationCount >= RecommendedTestImageCount
                        ? "WpfLearningWorkflow.ModelReplacement.Status.Available"
                        : "WpfLearningWorkflow.ModelReplacement.Status.EvidenceInsufficient")
                : Text("WpfLearningWorkflow.ModelReplacement.Status.Hold");

            WpfModelReplacementTextDescriptor detail;
            if (testImageCount > 0 && testLabelCount <= 0)
            {
                detail = Text("WpfLearningWorkflow.ModelReplacement.Detail.NoLabels");
            }
            else if (finalVerificationCount > 0 && finalVerificationCount < RecommendedTestImageCount)
            {
                detail = Text(
                    "WpfLearningWorkflow.ModelReplacement.Detail.WeakEvidence",
                    finalVerificationCount,
                    RecommendedTestImageCount,
                    RecommendedTestImageCount - finalVerificationCount);
            }
            else if (finalVerificationCount >= RecommendedTestImageCount)
            {
                detail = Text(
                    "WpfLearningWorkflow.ModelReplacement.Detail.Available",
                    finalVerificationCount);
            }
            else
            {
                detail = Text("WpfLearningWorkflow.ModelReplacement.Detail.NoTest");
            }

            return new WpfModelReplacementLocalizationSnapshot(status, detail);
        }

        private static WpfModelReplacementTextDescriptor Text(string key, params object[] arguments)
        {
            return new WpfModelReplacementTextDescriptor(key, arguments);
        }
    }
}
