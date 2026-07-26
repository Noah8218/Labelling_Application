using OpenCvSharp;
using System;
using System.Collections.Generic;
using Size = System.Drawing.Size;

namespace MvcVisionSystem._1._Core
{
    public sealed class TemplateMatchingAutoLabelOptions
    {
        public double MinimumScore { get; set; } = 0.82D;
        public int MaximumCandidates { get; set; } = 50;
        public double Magnification { get; set; } = 1D;
        public TemplateMatchModes MatchMode { get; set; } = TemplateMatchModes.CCoeffNormed;
        public bool ExcludeSourceRegion { get; set; } = true;
        public double ExcludeSourceIouThreshold { get; set; } = 0.85D;
        public bool UseCanny { get; set; }
        public int CannyLow { get; set; } = 50;
        public int CannyHigh { get; set; } = 150;
    }

    public sealed class TemplateMatchingAutoLabelResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public TimeSpan Elapsed { get; set; }
        public IReadOnlyList<YoloWorkerSmokeCandidate> Candidates { get; set; } = Array.Empty<YoloWorkerSmokeCandidate>();
    }

    public sealed class TemplateMatchingBatchAutoLabelItemResult
    {
        public string ImagePath { get; set; } = string.Empty;
        public bool Saved { get; set; }
        public bool NoCandidate { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CandidateCount { get; set; }
        public Size ImageSize { get; set; } = Size.Empty;
        public TimeSpan Elapsed { get; set; }

        public static TemplateMatchingBatchAutoLabelItemResult CreateSaved(string imagePath, int candidateCount, TimeSpan elapsed, Size imageSize)
        {
            return new TemplateMatchingBatchAutoLabelItemResult
            {
                ImagePath = imagePath ?? string.Empty,
                Saved = true,
                CandidateCount = Math.Max(0, candidateCount),
                ImageSize = imageSize,
                Elapsed = elapsed
            };
        }

        public static TemplateMatchingBatchAutoLabelItemResult NoCandidates(string imagePath, TimeSpan elapsed, Size imageSize)
        {
            return new TemplateMatchingBatchAutoLabelItemResult
            {
                ImagePath = imagePath ?? string.Empty,
                NoCandidate = true,
                Message = "no candidate",
                ImageSize = imageSize,
                Elapsed = elapsed
            };
        }

        public static TemplateMatchingBatchAutoLabelItemResult Failed(string imagePath, string message, TimeSpan elapsed, Size imageSize = default)
        {
            return new TemplateMatchingBatchAutoLabelItemResult
            {
                ImagePath = imagePath ?? string.Empty,
                Message = string.IsNullOrWhiteSpace(message) ? "failed" : message,
                ImageSize = imageSize,
                Elapsed = elapsed
            };
        }

        public static TemplateMatchingBatchAutoLabelItemResult Canceled(string imagePath, TimeSpan elapsed)
        {
            return Failed(imagePath, "canceled", elapsed);
        }
    }
}
