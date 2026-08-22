using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloExternalDatasetIntakeReport
    {
        public YoloExternalDatasetIntakeReport(
            string dataYamlFilePath,
            string datasetRootPath,
            LabelingDatasetPurpose purpose,
            IEnumerable<string> classNames,
            YoloExternalDatasetSplitSummary train,
            YoloExternalDatasetSplitSummary valid,
            YoloExternalDatasetSplitSummary test,
            IReadOnlyDictionary<string, int> annotationCountByClass,
            IEnumerable<string> errors,
            string sourceFingerprintSha256,
            int sourceFileCount,
            bool requiresRuntimeMaterialization = false)
        {
            DataYamlFilePath = dataYamlFilePath ?? string.Empty;
            DatasetRootPath = datasetRootPath ?? string.Empty;
            Purpose = purpose;
            ClassNames = (classNames ?? Array.Empty<string>()).ToArray();
            Train = train ?? new YoloExternalDatasetSplitSummary("train", string.Empty, 0, 0, 0);
            Valid = valid ?? new YoloExternalDatasetSplitSummary("val", string.Empty, 0, 0, 0);
            Test = test ?? new YoloExternalDatasetSplitSummary("test", string.Empty, 0, 0, 0);
            AnnotationCountByClass = new Dictionary<string, int>(
                annotationCountByClass ?? new Dictionary<string, int>(),
                StringComparer.OrdinalIgnoreCase);
            Errors = (errors ?? Array.Empty<string>())
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .ToArray();
            SourceFingerprintSha256 = sourceFingerprintSha256 ?? string.Empty;
            SourceFileCount = Math.Max(0, sourceFileCount);
            RequiresRuntimeMaterialization = requiresRuntimeMaterialization;
        }

        public string DataYamlFilePath { get; }

        public string DatasetRootPath { get; }

        public LabelingDatasetPurpose Purpose { get; }

        public IReadOnlyList<string> ClassNames { get; }

        public YoloExternalDatasetSplitSummary Train { get; }

        public YoloExternalDatasetSplitSummary Valid { get; }

        public YoloExternalDatasetSplitSummary Test { get; }

        public IReadOnlyDictionary<string, int> AnnotationCountByClass { get; }

        public IReadOnlyList<string> Errors { get; }

        public string SourceFingerprintSha256 { get; }

        public int SourceFileCount { get; }

        // A split-list packet with labels outside the native images/labels layout is read-only at intake.
        // Training receives an app-owned standard YOLO copy so the selected source remains untouched.
        public bool RequiresRuntimeMaterialization { get; }

        public bool IsReady => Errors.Count == 0;

        public int TotalImageCount => Train.ImageCount + Valid.ImageCount + Test.ImageCount;

        public int TotalLabelFileCount => Train.LabelFileCount + Valid.LabelFileCount + Test.LabelFileCount;

        public int TotalAnnotationCount => AnnotationCountByClass.Values.Sum();

        public string Summary =>
            $"{YoloExternalDatasetIntakeService.FormatPurpose(Purpose)} / train {Train.ImageCount} / val {Valid.ImageCount} / test {Test.ImageCount} / labels {TotalLabelFileCount} / annotations {TotalAnnotationCount} / classes {ClassNames.Count} / source files {SourceFileCount}"
            + (RequiresRuntimeMaterialization ? " / app runtime copy required" : string.Empty);
    }

    public sealed class YoloExternalDatasetSplitSummary
    {
        public YoloExternalDatasetSplitSummary(
            string name,
            string imageDirectoryPath,
            int imageCount,
            int labelFileCount,
            int emptyLabelFileCount)
        {
            Name = name ?? string.Empty;
            ImageDirectoryPath = imageDirectoryPath ?? string.Empty;
            ImageCount = Math.Max(0, imageCount);
            LabelFileCount = Math.Max(0, labelFileCount);
            EmptyLabelFileCount = Math.Max(0, emptyLabelFileCount);
        }

        public string Name { get; }

        public string ImageDirectoryPath { get; }

        public int ImageCount { get; }

        public int LabelFileCount { get; }

        public int EmptyLabelFileCount { get; }
    }

    /// <summary>
    /// One validated native YOLO source image and its optional label file. This remains
    /// read-only provenance; consumers must write any runtime derivative below an
    /// app-owned artifact directory.
    /// </summary>
    public sealed class YoloExternalDatasetSourceEntry
    {
        public YoloExternalDatasetSourceEntry(string split, string imagePath, string labelPath)
        {
            Split = split ?? string.Empty;
            ImagePath = imagePath ?? string.Empty;
            LabelPath = labelPath ?? string.Empty;
        }

        public string Split { get; }

        public string ImagePath { get; }

        public string LabelPath { get; }
    }

    public sealed class YoloExternalDatasetSourcePacket
    {
        public YoloExternalDatasetSourcePacket(
            YoloExternalDatasetIntakeReport report,
            IEnumerable<YoloExternalDatasetSourceEntry> entries)
        {
            Report = report;
            Entries = (entries ?? Array.Empty<YoloExternalDatasetSourceEntry>()).ToArray();
        }

        public YoloExternalDatasetIntakeReport Report { get; }

        public IReadOnlyList<YoloExternalDatasetSourceEntry> Entries { get; }

        public bool IsReady => Report?.IsReady == true;
    }

    public sealed class YoloExternalRuntimeDatasetResult
    {
        public YoloExternalRuntimeDatasetResult(
            YoloExternalDatasetIntakeReport sourceReport,
            string runtimeDataYamlFilePath,
            string runtimeRootPath,
            bool materialized,
            IEnumerable<string> errors)
        {
            SourceReport = sourceReport;
            RuntimeDataYamlFilePath = runtimeDataYamlFilePath ?? string.Empty;
            RuntimeRootPath = runtimeRootPath ?? string.Empty;
            Materialized = materialized;
            Errors = (errors ?? Array.Empty<string>())
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .ToArray();
        }

        public YoloExternalDatasetIntakeReport SourceReport { get; }

        public string RuntimeDataYamlFilePath { get; }

        public string RuntimeRootPath { get; }

        public bool Materialized { get; }

        public IReadOnlyList<string> Errors { get; }

        public bool IsReady => SourceReport?.IsReady == true
            && !string.IsNullOrWhiteSpace(RuntimeDataYamlFilePath)
            && Errors.Count == 0;
    }
}
