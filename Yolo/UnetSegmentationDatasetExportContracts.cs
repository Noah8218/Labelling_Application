using System;
using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class UnetSegmentationDatasetExportResult
    {
        public string OutputRootPath { get; internal set; } = string.Empty;

        public string DatasetFingerprint { get; internal set; } = string.Empty;

        public string SourceDataTreeSha256Before { get; internal set; } = string.Empty;

        public string SourceDataTreeSha256After { get; internal set; } = string.Empty;

        public string ClassContractSha256 { get; internal set; } = string.Empty;

        public bool ReusedExistingArtifact { get; internal set; }

        public int ImageCount { get; internal set; }

        public int PositiveMaskImageCount { get; internal set; }

        public List<UnetSegmentationDatasetExportSplitSummary> Splits { get; } =
            new List<UnetSegmentationDatasetExportSplitSummary>();

        public List<string> Errors { get; } = new List<string>();

        public bool IsReady => Errors.Count == 0
            && ImageCount > 0
            && !string.IsNullOrWhiteSpace(OutputRootPath)
            && !string.IsNullOrWhiteSpace(DatasetFingerprint)
            && string.Equals(
                SourceDataTreeSha256Before,
                SourceDataTreeSha256After,
                StringComparison.OrdinalIgnoreCase);
    }

    public sealed class UnetSegmentationDatasetExportSplitSummary
    {
        public UnetSegmentationDatasetExportSplitSummary(string split)
        {
            Split = split ?? string.Empty;
        }

        public string Split { get; }

        public int ImageCount { get; internal set; }

        public int PositiveMaskImageCount { get; internal set; }

        public int BackgroundMaskImageCount { get; internal set; }
    }

    public sealed class UnetClassContractItem
    {
        public int Index { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class UnetSegmentationDatasetExportManifest
    {
        public int Version { get; set; }

        public string DatasetFingerprint { get; set; } = string.Empty;

        public string SourceRecipeRootPath { get; set; } = string.Empty;

        public string SourceDataTreeSha256 { get; set; } = string.Empty;

        public string ClassContractSha256 { get; set; } = string.Empty;

        public List<UnetClassContractItem> Classes { get; set; } =
            new List<UnetClassContractItem>();

        public List<UnetSegmentationDatasetExportManifestSplit> Splits { get; set; } =
            new List<UnetSegmentationDatasetExportManifestSplit>();
    }

    public sealed class UnetSegmentationDatasetExportManifestSplit
    {
        public string Split { get; set; } = string.Empty;

        public List<UnetSegmentationDatasetExportManifestImage> Images { get; set; } =
            new List<UnetSegmentationDatasetExportManifestImage>();
    }

    public sealed class UnetSegmentationDatasetExportManifestImage
    {
        public string SourceRelativeImagePath { get; set; } = string.Empty;

        public string ImageSha256 { get; set; } = string.Empty;

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public string ExportImageRelativePath { get; set; } = string.Empty;

        public string ExportImageSha256 { get; set; } = string.Empty;

        public string ExportMaskRelativePath { get; set; } = string.Empty;

        public string ExportMaskSha256 { get; set; } = string.Empty;

        public bool HasForeground { get; set; }
    }
}
