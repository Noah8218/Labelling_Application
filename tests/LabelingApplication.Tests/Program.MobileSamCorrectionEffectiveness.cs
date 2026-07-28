using MvcVisionSystem;
using MvcVisionSystem._1._Core;
using MvcVisionSystem.Yolo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LabelingApplication.Tests;

using static TestSupport;

internal static partial class MobileSamUsabilityMatrixTests
{
    internal static int RunRealMobileSamCorrectionEffectiveness(string[] args)
    {
        try
        {
            string datasetRoot = ResolveKolektorCorrectionRoot(GetArgumentValue(
                args,
                "--dataset-root",
                string.Empty));
            string artifactRoot = Path.GetFullPath(GetArgumentValue(
                args,
                "--artifact-root",
                Path.Combine(FindRepositoryRoot(), "artifacts", "mobile-sam-correction-effectiveness")));
            string runRoot = Path.Combine(
                artifactRoot,
                DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(runRoot);

            ExternalYoloSourceTreeSnapshot sourceBefore = CaptureExternalYoloSourceTree(datasetRoot);
            CorrectionSample[] samples =
            {
                new("development", "kos08", "Part2"),
                new("development", "kos29", "Part0"),
                new("held-out", "kos06", "Part7"),
                new("held-out", "kos14", "Part7"),
                new("held-out", "kos35", "Part5"),
                new("held-out", "kos41", "Part7")
            };
            foreach (CorrectionSample sample in samples)
            {
                sample.Resolve(datasetRoot);
            }

            var selectionRows = new JArray(samples.Select(sample => new JObject
            {
                ["partition"] = sample.Partition,
                ["stem"] = sample.Stem,
                ["imageRelativePath"] = Path.GetRelativePath(datasetRoot, sample.ImagePath).Replace('\\', '/'),
                ["maskRelativePath"] = Path.GetRelativePath(datasetRoot, sample.MaskPath).Replace('\\', '/'),
                ["imageSha256"] = ComputeFileSha256(sample.ImagePath),
                ["maskSha256"] = ComputeFileSha256(sample.MaskPath),
                ["promptBox"] = JArray.FromObject(new[]
                {
                    sample.PromptBounds.X,
                    sample.PromptBounds.Y,
                    sample.PromptBounds.Width,
                    sample.PromptBounds.Height
                })
            }));
            var selectionManifest = new JObject
            {
                ["version"] = 1,
                ["evidenceOrigin"] = "public-industrial-dataset-local-copy",
                ["dataset"] = "KolektorSDD",
                ["selectionPolicy"] = "two fixed development and four fixed held-out irregular non-empty masks; one deterministic error-interior correction pass",
                ["sourceTreeFileCountBefore"] = sourceBefore.FileCount,
                ["sourceTreeSha256Before"] = sourceBefore.TreeSha256,
                ["samples"] = selectionRows
            };
            File.WriteAllText(
                Path.Combine(runRoot, "selection-manifest.json"),
                selectionManifest.ToString(Formatting.Indented));

            var service = new WpfMobileSamBoxPromptService();
            var settings = new PythonModelSettings();
            settings.EnsureDefaults();
            var rows = new List<JObject>(samples.Length);
            foreach (CorrectionSample sample in samples)
            {
                string sampleRoot = Path.Combine(runRoot, sample.Partition, sample.Stem);
                Directory.CreateDirectory(sampleRoot);
                string baselineMaskPath = Path.Combine(sampleRoot, "baseline-mask.png");
                string correctedMaskPath = Path.Combine(sampleRoot, "corrected-mask.png");

                WpfMobileSamBoxPromptResult baseline = RunCorrectionEffectivenessTrial(
                    service,
                    settings,
                    sample,
                    Array.Empty<WpfSmartMaskPromptPoint>());
                MobileSamMaskMetric baselineMetric = ComputeMobileSamMaskMetric(
                    sample.MaskPath,
                    baseline.Candidate.PolygonPoints,
                    baselineMaskPath);

                IReadOnlyList<WpfSmartMaskPromptPoint> correctionPoints =
                    SelectDeterministicCorrectionPoints(
                        sample.MaskPath,
                        baselineMaskPath);
                WpfSmartMaskPromptPoint[] positivePoints = correctionPoints
                    .Where(point => point.Kind == WpfSmartMaskPointKind.Positive)
                    .ToArray();
                WpfSmartMaskPromptPoint[] negativePoints = correctionPoints
                    .Where(point => point.Kind == WpfSmartMaskPointKind.Negative)
                    .ToArray();
                MobileSamMaskMetric positiveOnlyMetric = null;
                MobileSamMaskMetric negativeOnlyMetric = null;
                if (positivePoints.Length > 0)
                {
                    WpfMobileSamBoxPromptResult positiveOnly = RunCorrectionEffectivenessTrial(
                        service,
                        settings,
                        sample,
                        positivePoints);
                    positiveOnlyMetric = ComputeMobileSamMaskMetric(
                        sample.MaskPath,
                        positiveOnly.Candidate.PolygonPoints,
                        Path.Combine(sampleRoot, "positive-only-mask.png"));
                }
                if (negativePoints.Length > 0)
                {
                    WpfMobileSamBoxPromptResult negativeOnly = RunCorrectionEffectivenessTrial(
                        service,
                        settings,
                        sample,
                        negativePoints);
                    negativeOnlyMetric = ComputeMobileSamMaskMetric(
                        sample.MaskPath,
                        negativeOnly.Candidate.PolygonPoints,
                        Path.Combine(sampleRoot, "negative-only-mask.png"));
                }
                WpfMobileSamBoxPromptResult corrected = RunCorrectionEffectivenessTrial(
                    service,
                    settings,
                    sample,
                    correctionPoints);
                MobileSamMaskMetric correctedMetric = ComputeMobileSamMaskMetric(
                    sample.MaskPath,
                    corrected.Candidate.PolygonPoints,
                    correctedMaskPath);
                string comparisonPath = Path.Combine(sampleRoot, "comparison.png");
                SaveCorrectionComparison(
                    sample,
                    baselineMaskPath,
                    correctedMaskPath,
                    baselineMetric,
                    correctedMetric,
                    correctionPoints,
                    comparisonPath);

                double iouDelta = correctedMetric.IoU - baselineMetric.IoU;
                double diceDelta = correctedMetric.Dice - baselineMetric.Dice;
                bool improved = iouDelta >= 0.01D && diceDelta > 0D;
                JObject row = BuildCorrectionResultRow(
                    datasetRoot,
                    runRoot,
                    sample,
                    baseline,
                    corrected,
                    baselineMetric,
                    correctedMetric,
                    positiveOnlyMetric,
                    negativeOnlyMetric,
                    correctionPoints,
                    comparisonPath,
                    improved);
                rows.Add(row);
                Console.WriteLine(FormattableString.Invariant(
                    $"MOBILE_SAM_CORRECTION SAMPLE={sample.Partition}/{sample.Stem} BASE_IOU={baselineMetric.IoU:F6} CORRECTED_IOU={correctedMetric.IoU:F6} DELTA={iouDelta:+0.000000;-0.000000;0.000000} POINTS={correctionPoints.Count} IMPROVED={improved}"));
            }

            ExternalYoloSourceTreeSnapshot sourceAfter = CaptureExternalYoloSourceTree(datasetRoot);
            AssertEqual(sourceBefore.FileCount, sourceAfter.FileCount);
            AssertEqual(sourceBefore.TreeSha256, sourceAfter.TreeSha256);

            JObject[] developmentRows = rows
                .Where(row => string.Equals(row.Value<string>("partition"), "development", StringComparison.Ordinal))
                .ToArray();
            JObject[] heldOutRows = rows
                .Where(row => string.Equals(row.Value<string>("partition"), "held-out", StringComparison.Ordinal))
                .ToArray();
            int developmentImproved = developmentRows.Count(row => row.Value<bool>("improved"));
            int heldOutImproved = heldOutRows.Count(row => row.Value<bool>("improved"));
            double developmentMedianDelta = Median(developmentRows.Select(row => row.Value<double>("iouDelta")));
            double heldOutMedianDelta = Median(heldOutRows.Select(row => row.Value<double>("iouDelta")));
            JObject[] positiveDirectionRows = rows
                .Where(row => row.Value<bool?>("positiveDirectionPassed").HasValue)
                .ToArray();
            JObject[] negativeDirectionRows = rows
                .Where(row => row.Value<bool?>("negativeDirectionPassed").HasValue)
                .ToArray();
            int positiveDirectionPassed = positiveDirectionRows.Count(row => row.Value<bool>("positiveDirectionPassed"));
            int negativeDirectionPassed = negativeDirectionRows.Count(row => row.Value<bool>("negativeDirectionPassed"));
            double positiveDirectionPassRate = positiveDirectionRows.Length == 0
                ? 0D
                : positiveDirectionPassed / (double)positiveDirectionRows.Length;
            double negativeDirectionPassRate = negativeDirectionRows.Length == 0
                ? 0D
                : negativeDirectionPassed / (double)negativeDirectionRows.Length;
            bool allBaselinesPoor = rows.All(row => row.Value<double>("baselineIou") < 0.50D);
            bool gatePassed = developmentImproved >= 1
                && heldOutImproved >= 3
                && heldOutMedianDelta >= 0.05D
                && positiveDirectionPassRate >= 0.75D
                && negativeDirectionPassRate >= 0.50D
                && allBaselinesPoor;

            var summary = new JObject
            {
                ["status"] = gatePassed ? "Complete" : "Incomplete",
                ["scope"] = "Real MobileSAM one-pass positive/negative correction effectiveness",
                ["evidenceOrigin"] = "public-industrial-dataset-local-copy",
                ["fieldValidation"] = "Not evaluated",
                ["productionAccuracyClaimed"] = false,
                ["dataset"] = "KolektorSDD",
                ["sampleCount"] = rows.Count,
                ["developmentSampleCount"] = developmentRows.Length,
                ["heldOutSampleCount"] = heldOutRows.Length,
                ["developmentImprovedCount"] = developmentImproved,
                ["heldOutImprovedCount"] = heldOutImproved,
                ["developmentMedianIouDelta"] = developmentMedianDelta,
                ["heldOutMedianIouDelta"] = heldOutMedianDelta,
                ["allBaselinesPoor"] = allBaselinesPoor,
                ["positiveDirectionTrialCount"] = positiveDirectionRows.Length,
                ["positiveDirectionPassedCount"] = positiveDirectionPassed,
                ["positiveDirectionPassRate"] = positiveDirectionPassRate,
                ["negativeDirectionTrialCount"] = negativeDirectionRows.Length,
                ["negativeDirectionPassedCount"] = negativeDirectionPassed,
                ["negativeDirectionPassRate"] = negativeDirectionPassRate,
                ["gatePassed"] = gatePassed,
                ["sourceTreeFileCountBefore"] = sourceBefore.FileCount,
                ["sourceTreeFileCountAfter"] = sourceAfter.FileCount,
                ["sourceTreeSha256Before"] = sourceBefore.TreeSha256,
                ["sourceTreeSha256After"] = sourceAfter.TreeSha256,
                ["runtime"] = rows[0].Value<string>("runtime"),
                ["weightsSha256"] = rows[0].Value<string>("weightsSha256"),
                ["boundary"] = "Fixed public-dataset replay with ground-truth-guided click selection; proves correction response, not unaided operator click quality or production accuracy."
            };
            string resultsPath = Path.Combine(runRoot, "sample-results.jsonl");
            File.WriteAllLines(resultsPath, rows.Select(row => row.ToString(Formatting.None)));
            string summaryPath = Path.Combine(runRoot, "summary.json");
            File.WriteAllText(summaryPath, summary.ToString(Formatting.Indented));
            File.WriteAllText(
                Path.Combine(runRoot, "summary.md"),
                BuildCorrectionEffectivenessMarkdown(summary, rows));

            Console.WriteLine("MOBILE_SAM_CORRECTION_ROOT=" + runRoot);
            Console.WriteLine("MOBILE_SAM_CORRECTION_SUMMARY=" + summaryPath);
            Console.WriteLine("MOBILE_SAM_CORRECTION_GATE=" + gatePassed);
            AssertTrue(
                gatePassed,
                "real MobileSAM correction-effectiveness gate failed; inspect " + summaryPath);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("REAL_MOBILE_SAM_CORRECTION_EFFECTIVENESS_FAILED=" + error);
            return 1;
        }
    }

    private static WpfMobileSamBoxPromptResult RunCorrectionEffectivenessTrial(
        WpfMobileSamBoxPromptService service,
        PythonModelSettings settings,
        CorrectionSample sample,
        IReadOnlyList<WpfSmartMaskPromptPoint> points)
    {
        WpfMobileSamBoxPromptRequest request = service.BuildRequest(
            settings,
            sample.ImagePath,
            sample.PromptBounds,
            classId: 0,
            className: "Defect",
            promptPoints: points,
            maximumPolygonPoints: 256);
        AssertTrue(request.IsValid, string.Join(" ", request.Errors));
        WpfMobileSamBoxPromptResult result = service.RunAsync(request).GetAwaiter().GetResult();
        AssertTrue(result.Succeeded, result.Error);
        AssertTrue(
            result.Candidate?.PolygonPoints?.Count >= 3,
            "real correction-effectiveness candidate should contain a review polygon");
        return result;
    }

    private static IReadOnlyList<WpfSmartMaskPromptPoint> SelectDeterministicCorrectionPoints(
        string groundTruthMaskPath,
        string predictedMaskPath)
    {
        using var truth = new Bitmap(groundTruthMaskPath);
        using var prediction = new Bitmap(predictedMaskPath);
        bool[] falseNegative = new bool[truth.Width * truth.Height];
        bool[] falsePositive = new bool[truth.Width * truth.Height];
        int falseNegativeCount = 0;
        int falsePositiveCount = 0;
        for (int y = 0; y < truth.Height; y++)
        {
            for (int x = 0; x < truth.Width; x++)
            {
                int index = (y * truth.Width) + x;
                Color truthColor = truth.GetPixel(x, y);
                bool expected = truthColor.R > 16 || truthColor.G > 16 || truthColor.B > 16;
                bool actual = prediction.GetPixel(x, y).R > 16;
                if (expected && !actual)
                {
                    falseNegative[index] = true;
                    falseNegativeCount++;
                }
                else if (!expected && actual)
                {
                    falsePositive[index] = true;
                    falsePositiveCount++;
                }
            }
        }

        var points = new List<WpfSmartMaskPromptPoint>(2);
        if (falseNegativeCount >= 64
            && TrySelectDenseErrorPoint(falseNegative, truth.Width, truth.Height, out Point positive))
        {
            points.Add(new WpfSmartMaskPromptPoint
            {
                Position = positive,
                Kind = WpfSmartMaskPointKind.Positive
            });
        }
        if (falsePositiveCount >= 64
            && TrySelectDenseErrorPoint(falsePositive, truth.Width, truth.Height, out Point negative))
        {
            points.Add(new WpfSmartMaskPromptPoint
            {
                Position = negative,
                Kind = WpfSmartMaskPointKind.Negative
            });
        }

        AssertTrue(points.Count > 0, "poor candidate did not expose a deterministic correction point");
        return points;
    }

    private static bool TrySelectDenseErrorPoint(
        IReadOnlyList<bool> errorMask,
        int width,
        int height,
        out Point point)
    {
        point = Point.Empty;
        int stride = width + 1;
        int[] integral = new int[(width + 1) * (height + 1)];
        for (int y = 0; y < height; y++)
        {
            int rowSum = 0;
            for (int x = 0; x < width; x++)
            {
                if (errorMask[(y * width) + x])
                {
                    rowSum++;
                }
                integral[((y + 1) * stride) + x + 1] =
                    integral[(y * stride) + x + 1] + rowSum;
            }
        }

        const int radius = 7;
        int bestScore = 0;
        for (int y = radius; y < height - radius; y += 2)
        {
            for (int x = radius; x < width - radius; x += 2)
            {
                if (!errorMask[(y * width) + x])
                {
                    continue;
                }

                int left = x - radius;
                int top = y - radius;
                int right = x + radius + 1;
                int bottom = y + radius + 1;
                int score = integral[(bottom * stride) + right]
                    - integral[(top * stride) + right]
                    - integral[(bottom * stride) + left]
                    + integral[(top * stride) + left];
                if (score > bestScore)
                {
                    bestScore = score;
                    point = new Point(x, y);
                }
            }
        }
        return bestScore > 0;
    }

    private static JObject BuildCorrectionResultRow(
        string datasetRoot,
        string runRoot,
        CorrectionSample sample,
        WpfMobileSamBoxPromptResult baseline,
        WpfMobileSamBoxPromptResult corrected,
        MobileSamMaskMetric baselineMetric,
        MobileSamMaskMetric correctedMetric,
        MobileSamMaskMetric positiveOnlyMetric,
        MobileSamMaskMetric negativeOnlyMetric,
        IReadOnlyList<WpfSmartMaskPromptPoint> correctionPoints,
        string comparisonPath,
        bool improved)
    {
        int baselineFalseNegatives = baselineMetric.GroundTruthPixels - baselineMetric.IntersectionPixels;
        int baselineFalsePositives = baselineMetric.PredictedPixels - baselineMetric.IntersectionPixels;
        int? positiveOnlyFalseNegatives = positiveOnlyMetric == null
            ? null
            : positiveOnlyMetric.GroundTruthPixels - positiveOnlyMetric.IntersectionPixels;
        int? negativeOnlyFalsePositives = negativeOnlyMetric == null
            ? null
            : negativeOnlyMetric.PredictedPixels - negativeOnlyMetric.IntersectionPixels;
        return new JObject
        {
            ["partition"] = sample.Partition,
            ["stem"] = sample.Stem,
            ["imageRelativePath"] = Path.GetRelativePath(datasetRoot, sample.ImagePath).Replace('\\', '/'),
            ["maskRelativePath"] = Path.GetRelativePath(datasetRoot, sample.MaskPath).Replace('\\', '/'),
            ["promptBox"] = JArray.FromObject(new[]
            {
                sample.PromptBounds.X,
                sample.PromptBounds.Y,
                sample.PromptBounds.Width,
                sample.PromptBounds.Height
            }),
            ["points"] = new JArray(correctionPoints.Select(point => new JObject
            {
                ["kind"] = point.Kind.ToString(),
                ["x"] = point.Position.X,
                ["y"] = point.Position.Y
            })),
            ["baselineIou"] = baselineMetric.IoU,
            ["correctedIou"] = correctedMetric.IoU,
            ["iouDelta"] = correctedMetric.IoU - baselineMetric.IoU,
            ["baselineDice"] = baselineMetric.Dice,
            ["correctedDice"] = correctedMetric.Dice,
            ["diceDelta"] = correctedMetric.Dice - baselineMetric.Dice,
            ["baselinePixels"] = baselineMetric.PredictedPixels,
            ["correctedPixels"] = correctedMetric.PredictedPixels,
            ["groundTruthPixels"] = baselineMetric.GroundTruthPixels,
            ["baselineFalseNegatives"] = baselineFalseNegatives,
            ["positiveOnlyFalseNegatives"] = positiveOnlyFalseNegatives,
            ["positiveDirectionPassed"] = positiveOnlyFalseNegatives.HasValue
                ? positiveOnlyFalseNegatives.Value < baselineFalseNegatives
                : null,
            ["baselineFalsePositives"] = baselineFalsePositives,
            ["negativeOnlyFalsePositives"] = negativeOnlyFalsePositives,
            ["negativeDirectionPassed"] = negativeOnlyFalsePositives.HasValue
                ? negativeOnlyFalsePositives.Value < baselineFalsePositives
                : null,
            ["positiveOnlyIou"] = positiveOnlyMetric?.IoU,
            ["negativeOnlyIou"] = negativeOnlyMetric?.IoU,
            ["baselinePolygonPoints"] = baseline.Candidate.PolygonPoints.Count,
            ["correctedPolygonPoints"] = corrected.Candidate.PolygonPoints.Count,
            ["baselineElapsedMs"] = baseline.ElapsedMilliseconds,
            ["correctedElapsedMs"] = corrected.ElapsedMilliseconds,
            ["improved"] = improved,
            ["comparisonRelativePath"] = Path.GetRelativePath(runRoot, comparisonPath).Replace('\\', '/'),
            ["runtime"] = corrected.RuntimeSummary,
            ["weightsSha256"] = corrected.WeightsSha256
        };
    }

    private static void SaveCorrectionComparison(
        CorrectionSample sample,
        string baselineMaskPath,
        string correctedMaskPath,
        MobileSamMaskMetric baselineMetric,
        MobileSamMaskMetric correctedMetric,
        IReadOnlyList<WpfSmartMaskPromptPoint> points,
        string outputPath)
    {
        using var source = new Bitmap(sample.ImagePath);
        using var truth = new Bitmap(sample.MaskPath);
        using var baseline = new Bitmap(baselineMaskPath);
        using var corrected = new Bitmap(correctedMaskPath);
        const int panelWidth = 500;
        int panelHeight = source.Height;
        using var comparison = new Bitmap(panelWidth * 3, panelHeight);
        using Graphics graphics = Graphics.FromImage(comparison);
        graphics.Clear(Color.Black);
        DrawCorrectionPanel(graphics, source, truth, null, 0, panelWidth);
        DrawCorrectionPanel(graphics, source, truth, baseline, panelWidth, panelWidth);
        DrawCorrectionPanel(graphics, source, truth, corrected, panelWidth * 2, panelWidth);
        using var labelBrush = new SolidBrush(Color.FromArgb(220, 0, 0, 0));
        using var textBrush = new SolidBrush(Color.White);
        using var font = new Font("Segoe UI", 13F, FontStyle.Bold);
        graphics.FillRectangle(labelBrush, 0, 0, comparison.Width, 34);
        graphics.DrawString("SOURCE + TRUTH", font, textBrush, 8, 7);
        graphics.DrawString(
            FormattableString.Invariant($"BASE IoU {baselineMetric.IoU:F4}"),
            font,
            textBrush,
            panelWidth + 8,
            7);
        graphics.DrawString(
            FormattableString.Invariant($"CORRECTED IoU {correctedMetric.IoU:F4} / {points.Count} point(s)"),
            font,
            textBrush,
            (panelWidth * 2) + 8,
            7);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
        comparison.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void DrawCorrectionPanel(
        Graphics graphics,
        Bitmap source,
        Bitmap truth,
        Bitmap prediction,
        int offsetX,
        int panelWidth)
    {
        graphics.DrawImage(source, new Rectangle(offsetX, 0, panelWidth, source.Height));
        using var truthBrush = new SolidBrush(Color.FromArgb(70, 0, 255, 0));
        using var predictionBrush = new SolidBrush(Color.FromArgb(95, 0, 160, 255));
        for (int y = 0; y < source.Height; y += 2)
        {
            for (int x = 0; x < source.Width; x += 2)
            {
                Color truthColor = truth.GetPixel(x, y);
                if (truthColor.R > 16 || truthColor.G > 16 || truthColor.B > 16)
                {
                    graphics.FillRectangle(truthBrush, offsetX + x, y, 2, 2);
                }
                if (prediction != null && prediction.GetPixel(x, y).R > 16)
                {
                    graphics.FillRectangle(predictionBrush, offsetX + x, y, 2, 2);
                }
            }
        }
    }

    private static string BuildCorrectionEffectivenessMarkdown(
        JObject summary,
        IReadOnlyList<JObject> rows)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("# MobileSAM Real Correction Effectiveness");
        builder.AppendLine();
        builder.AppendLine($"- Status: {summary.Value<string>("status")}");
        builder.AppendLine("- Dataset: KolektorSDD local public-dataset copy");
        builder.AppendLine("- Field validation: Not evaluated");
        builder.AppendLine($"- Source SHA-256 before/after: `{summary.Value<string>("sourceTreeSha256Before")}` / `{summary.Value<string>("sourceTreeSha256After")}`");
        builder.AppendLine($"- Runtime: {summary.Value<string>("runtime")}");
        builder.AppendLine($"- Weight SHA-256: `{summary.Value<string>("weightsSha256")}`");
        builder.AppendLine();
        builder.AppendLine("| Partition | Sample | Points | Baseline IoU | Corrected IoU | Delta | Improved |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | --- |");
        foreach (JObject row in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"| {row.Value<string>("partition")} | {row.Value<string>("stem")} | {((JArray)row["points"]).Count} | {row.Value<double>("baselineIou"):F4} | {row.Value<double>("correctedIou"):F4} | {row.Value<double>("iouDelta"):+0.0000;-0.0000;0.0000} | {row.Value<bool>("improved")} |"));
        }
        builder.AppendLine();
        builder.AppendLine(FormattableString.Invariant(
            $"Development improved {summary.Value<int>("developmentImprovedCount")}/{summary.Value<int>("developmentSampleCount")}, median delta {summary.Value<double>("developmentMedianIouDelta"):+0.0000;-0.0000;0.0000}."));
        builder.AppendLine(FormattableString.Invariant(
            $"Held-out improved {summary.Value<int>("heldOutImprovedCount")}/{summary.Value<int>("heldOutSampleCount")}, median delta {summary.Value<double>("heldOutMedianIouDelta"):+0.0000;-0.0000;0.0000}."));
        builder.AppendLine(FormattableString.Invariant(
            $"Positive-direction pass {summary.Value<int>("positiveDirectionPassedCount")}/{summary.Value<int>("positiveDirectionTrialCount")} ({summary.Value<double>("positiveDirectionPassRate"):P1}); negative-direction pass {summary.Value<int>("negativeDirectionPassedCount")}/{summary.Value<int>("negativeDirectionTrialCount")} ({summary.Value<double>("negativeDirectionPassRate"):P1})."));
        builder.AppendLine();
        builder.AppendLine("Boundary: ground-truth-guided click selection proves deterministic correction response, not unaided operator click quality or production accuracy.");
        return builder.ToString();
    }

    private static string ResolveKolektorCorrectionRoot(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            return Path.GetFullPath(configured);
        }

        string repositoryRoot = FindRepositoryRoot();
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (string root in new[]
        {
            Path.Combine(repositoryRoot, "datasets", "industrial", "KolektorSDD", "raw", "expanded"),
            Path.Combine(userProfile, "LabelingIndustrialDatasets", "KolektorSDD", "raw", "expanded"),
            @"C:\temp\kolektor_test\KolektorSDD\raw\expanded"
        })
        {
            if (Directory.Exists(root))
            {
                return Path.GetFullPath(root);
            }
        }

        throw new DirectoryNotFoundException(
            "KolektorSDD raw expanded dataset was not found in the supported local roots.");
    }

    private sealed class CorrectionSample
    {
        internal CorrectionSample(string partition, string folder, string part)
        {
            Partition = partition;
            Folder = folder;
            Part = part;
        }

        internal string Partition { get; }
        internal string Folder { get; }
        internal string Part { get; }
        internal string Stem => Folder + "_" + Part;
        internal string ImagePath { get; private set; }
        internal string MaskPath { get; private set; }
        internal Rectangle PromptBounds { get; private set; }

        internal void Resolve(string datasetRoot)
        {
            ImagePath = Path.Combine(datasetRoot, Folder, Part + ".jpg");
            MaskPath = Path.Combine(datasetRoot, Folder, Part + "_label.bmp");
            AssertTrue(File.Exists(ImagePath), "correction source image was not found: " + ImagePath);
            AssertTrue(File.Exists(MaskPath), "correction source mask was not found: " + MaskPath);
            using var mask = new Bitmap(MaskPath);
            Rectangle truthBounds = FindNonZeroBounds(mask);
            AssertTrue(!truthBounds.IsEmpty, "correction source mask was empty: " + MaskPath);
            int paddingX = Math.Max(8, (int)Math.Round(truthBounds.Width * 0.08D));
            int paddingY = Math.Max(8, (int)Math.Round(truthBounds.Height * 0.08D));
            Rectangle expanded = Rectangle.FromLTRB(
                Math.Max(0, truthBounds.Left - paddingX),
                Math.Max(0, truthBounds.Top - paddingY),
                Math.Min(mask.Width, truthBounds.Right + paddingX),
                Math.Min(mask.Height, truthBounds.Bottom + paddingY));
            PromptBounds = expanded;
        }

        private static Rectangle FindNonZeroBounds(Bitmap bitmap)
        {
            int minX = bitmap.Width;
            int minY = bitmap.Height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    if (color.R <= 16 && color.G <= 16 && color.B <= 16)
                    {
                        continue;
                    }
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
            return maxX < minX
                ? Rectangle.Empty
                : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }
    }
}
