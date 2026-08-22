using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenVisionLab.Vision2D.Property;
using OpenVisionLab.Vision2D.Result;
using OpenVisionLab.Vision2D.Tool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace MvcVisionSystem._1._Core
{
    public sealed class TemplateMatchingAutoLabelService
    {
        public TemplateMatchingAutoLabelResult MatchCurrentImage(
            Bitmap sourceImage,
            Rectangle templateBounds,
            string className,
            TemplateMatchingAutoLabelOptions options = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            options ??= new TemplateMatchingAutoLabelOptions();

            if (sourceImage == null)
            {
                return Failed("source image is not loaded", stopwatch.Elapsed);
            }

            Rectangle sourceBounds = new Rectangle(System.Drawing.Point.Empty, sourceImage.Size);
            Rectangle clippedTemplateBounds = Rectangle.Intersect(templateBounds, sourceBounds);
            if (clippedTemplateBounds.Width <= 1 || clippedTemplateBounds.Height <= 1)
            {
                return Failed("template box is outside the image", stopwatch.Elapsed);
            }

            try
            {
                using (Bitmap templateImage = CloneRegion(sourceImage, clippedTemplateBounds))
                {
                    return MatchImageWithTemplate(
                        sourceImage,
                        templateImage,
                        className,
                        options,
                        options.ExcludeSourceRegion ? clippedTemplateBounds : null,
                        stopwatch);
                }
            }
            catch (Exception ex)
            {
                return Failed(ex.Message, stopwatch.Elapsed);
            }
        }

        public TemplateMatchingAutoLabelResult MatchImageWithTemplate(
            Bitmap sourceImage,
            Bitmap templateImage,
            string className,
            TemplateMatchingAutoLabelOptions options = null,
            Rectangle? excludedSourceRegion = null)
        {
            return MatchImageWithTemplate(
                sourceImage,
                templateImage,
                className,
                options ?? new TemplateMatchingAutoLabelOptions(),
                excludedSourceRegion,
                Stopwatch.StartNew());
        }

        public Bitmap CloneTemplateImage(Bitmap sourceImage, Rectangle templateBounds, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (sourceImage == null)
            {
                errorMessage = "source image is not loaded";
                return null;
            }

            Rectangle sourceBounds = new Rectangle(System.Drawing.Point.Empty, sourceImage.Size);
            Rectangle clippedTemplateBounds = Rectangle.Intersect(templateBounds, sourceBounds);
            if (clippedTemplateBounds.Width <= 1 || clippedTemplateBounds.Height <= 1)
            {
                errorMessage = "template box is outside the image";
                return null;
            }

            return CloneRegion(sourceImage, clippedTemplateBounds);
        }

        private static TemplateMatchingAutoLabelResult MatchImageWithTemplate(
            Bitmap sourceImage,
            Bitmap templateImage,
            string className,
            TemplateMatchingAutoLabelOptions options,
            Rectangle? excludedSourceRegion,
            Stopwatch stopwatch)
        {
            options ??= new TemplateMatchingAutoLabelOptions();

            if (sourceImage == null)
            {
                return Failed("source image is not loaded", stopwatch.Elapsed);
            }

            if (templateImage == null || templateImage.Width <= 1 || templateImage.Height <= 1)
            {
                return Failed("template image is empty", stopwatch.Elapsed);
            }

            Rectangle sourceBounds = new Rectangle(System.Drawing.Point.Empty, sourceImage.Size);
            if (templateImage.Width > sourceBounds.Width || templateImage.Height > sourceBounds.Height)
            {
                return Failed("template image is larger than source image", stopwatch.Elapsed);
            }

            try
            {
                using (Mat sourceMat = BitmapConverter.ToMat(sourceImage))
                using (Mat templateMat = BitmapConverter.ToMat(templateImage))
                using (var tool = new MatchingTool())
                {
                    tool.SetProperty(CreateProperty(options));
                    tool.SetTemplateImage(templateMat);
                    using VisionToolResult toolResult = tool.Execute(sourceMat);
                    if (!toolResult.Success && toolResult.ErrorCode != VisionToolErrorCode.MatchingNoResult)
                    {
                        return Failed(toolResult.Message, stopwatch.Elapsed);
                    }

                    List<YoloWorkerSmokeCandidate> candidates = ConvertResults(
                        tool.results,
                        excludedSourceRegion ?? Rectangle.Empty,
                        sourceBounds,
                        className,
                        options,
                        excludedSourceRegion.HasValue);

                    return new TemplateMatchingAutoLabelResult
                    {
                        Succeeded = true,
                        Message = candidates.Count == 0
                            ? "template matching completed with no candidate"
                            : $"template matching completed. candidates={candidates.Count}",
                        Elapsed = stopwatch.Elapsed,
                        Candidates = candidates
                    };
                }
            }
            catch (Exception ex)
            {
                return Failed(ex.Message, stopwatch.Elapsed);
            }
        }

        private static TemplateMatchingAutoLabelResult Failed(string message, TimeSpan elapsed)
        {
            return new TemplateMatchingAutoLabelResult
            {
                Succeeded = false,
                Message = message ?? string.Empty,
                Elapsed = elapsed,
                Candidates = Array.Empty<YoloWorkerSmokeCandidate>()
            };
        }

        private static Bitmap CloneRegion(Bitmap sourceImage, Rectangle bounds)
        {
            var clone = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(clone))
            {
                graphics.DrawImage(
                    sourceImage,
                    new Rectangle(0, 0, bounds.Width, bounds.Height),
                    bounds,
                    GraphicsUnit.Pixel);
            }

            return clone;
        }

        private static MatchingToolProperty CreateProperty(TemplateMatchingAutoLabelOptions options)
        {
            return new MatchingToolProperty
            {
                NAME = "AutoLabelTemplateMatching",
                MATCH_MODE = options.MatchMode,
                SCORE_MIN = Math.Clamp(options.MinimumScore, 0D, 1D),
                MAGNIFIATION = Math.Max(0.1D, options.Magnification),
                NUM_MATCH = Math.Clamp(options.MaximumCandidates, 1, 200),
                USE_CANNY = options.UseCanny,
                CANNY_LOW = Math.Max(0, options.CannyLow),
                CANNY_HIGH = Math.Max(options.CannyLow + 1, options.CannyHigh),
                USE_FIND_ANGLE = false,
                USE_PYRAMID_POSITION_PROPOSAL = true,
                PYRAMID_POSITION_TOP_N = 32,
                PYRAMID_POSITION_MIN_SCORE = 0.65D
            };
        }

        private static List<YoloWorkerSmokeCandidate> ConvertResults(
            IEnumerable<MatchingResult> matchingResults,
            Rectangle templateBounds,
            Rectangle sourceBounds,
            string className,
            TemplateMatchingAutoLabelOptions options,
            bool hasExcludedSourceRegion)
        {
            string normalizedClassName = string.IsNullOrWhiteSpace(className) ? "Defect" : className.Trim();
            var candidates = new List<YoloWorkerSmokeCandidate>();

            foreach (MatchingResult result in (matchingResults ?? Array.Empty<MatchingResult>())
                .OrderByDescending(item => item.Score))
            {
                Rectangle bounds = Rectangle.Round(result.Bounding);
                bounds = Rectangle.Intersect(bounds, sourceBounds);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    continue;
                }

                if (options.ExcludeSourceRegion
                    && hasExcludedSourceRegion
                    && CalculateIntersectionOverUnion(bounds, templateBounds) >= options.ExcludeSourceIouThreshold)
                {
                    continue;
                }

                if (candidates.Any(existing => CalculateIntersectionOverUnion(existing.ToRectangle(), bounds) >= 0.9D))
                {
                    continue;
                }

                candidates.Add(new YoloWorkerSmokeCandidate
                {
                    Index = candidates.Count + 1,
                    ClassName = normalizedClassName,
                    Confidence = NormalizeScore(result.Score),
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height
                });

                if (candidates.Count >= options.MaximumCandidates)
                {
                    break;
                }
            }

            return candidates;
        }

        private static double NormalizeScore(double score)
            => score > 1D ? Math.Clamp(score / 100D, 0D, 1D) : Math.Clamp(score, 0D, 1D);

        private static double CalculateIntersectionOverUnion(Rectangle left, Rectangle right)
        {
            Rectangle intersection = Rectangle.Intersect(left, right);
            if (intersection.Width <= 0 || intersection.Height <= 0)
            {
                return 0D;
            }

            double intersectionArea = intersection.Width * intersection.Height;
            double unionArea = left.Width * left.Height + right.Width * right.Height - intersectionArea;
            return unionArea <= 0D ? 0D : intersectionArea / unionArea;
        }

    }
}
