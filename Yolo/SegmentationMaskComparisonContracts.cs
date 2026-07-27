using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class SegmentationMaskComparisonRequest
    {
        public string DatasetExportRootPath { get; set; } = string.Empty;

        public string BaselinePredictionManifestPath { get; set; } = string.Empty;

        public string CandidatePredictionManifestPath { get; set; } = string.Empty;

        public string Split { get; set; } = "test";

        public string OutputRootPath { get; set; } = string.Empty;

        public double ComponentIouThreshold { get; set; } = 0.5D;
    }

    public sealed class SegmentationMaskComparisonResult
    {
        public List<string> Errors { get; } = new List<string>();

        public string ReportPath { get; internal set; } = string.Empty;

        public string DatasetFingerprint { get; internal set; } = string.Empty;

        public string SourceDataTreeSha256 { get; internal set; } = string.Empty;

        public string ClassContractSha256 { get; internal set; } = string.Empty;

        public string Split { get; internal set; } = string.Empty;

        public SegmentationPredictionRunSummary Baseline { get; internal set; } = new SegmentationPredictionRunSummary();

        public SegmentationPredictionRunSummary Candidate { get; internal set; } = new SegmentationPredictionRunSummary();

        public List<SegmentationMaskComparisonClassResult> Classes { get; } = new List<SegmentationMaskComparisonClassResult>();

        public bool IsReady => Errors.Count == 0 && Classes.Count > 0;
    }

    public sealed class SegmentationPredictionRunSummary
    {
        public string AdapterKey { get; set; } = string.Empty;

        public string Engine { get; set; } = string.Empty;

        public string CheckpointPath { get; set; } = string.Empty;

        public string CheckpointSha256 { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public double MeanDice { get; set; }

        public double MeanIoU { get; set; }
    }

    public sealed class SegmentationMaskComparisonClassResult
    {
        public int ClassIndex { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public SegmentationMaskMetrics Baseline { get; set; } = new SegmentationMaskMetrics();

        public SegmentationMaskMetrics Candidate { get; set; } = new SegmentationMaskMetrics();
    }

    public sealed class SegmentationMaskMetrics
    {
        public long TruePositivePixels { get; set; }

        public long FalsePositivePixels { get; set; }

        public long FalseNegativePixels { get; set; }

        public int TruePositiveComponents { get; set; }

        public int FalsePositiveComponents { get; set; }

        public int FalseNegativeComponents { get; set; }

        public double Dice { get; set; }

        public double IoU { get; set; }
    }

    public sealed class SegmentationPredictionManifestRecord
    {
        public int Version { get; set; }

        public string AdapterKey { get; set; } = string.Empty;

        public string Engine { get; set; } = string.Empty;

        public string DatasetFingerprint { get; set; } = string.Empty;

        public string SourceDataTreeSha256 { get; set; } = string.Empty;

        public string ClassContractSha256 { get; set; } = string.Empty;

        public string Split { get; set; } = string.Empty;

        public string CheckpointSha256 { get; set; } = string.Empty;

        public string CheckpointPath { get; set; } = string.Empty;

        public string ImageSha256 { get; set; } = string.Empty;

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public string PredictionMaskRelativePath { get; set; } = string.Empty;

        public string PredictionMaskSha256 { get; set; } = string.Empty;
    }
}
