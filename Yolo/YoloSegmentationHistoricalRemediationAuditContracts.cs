using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloSegmentationHistoricalRemediationAuditReport
    {
        public string OutputRootPath { get; internal set; } = string.Empty;

        public string ExcludedSourceImagePath { get; internal set; } = string.Empty;

        public string ProposedBackupRootPath { get; internal set; } = string.Empty;

        public int ExcludedSourceImageCount { get; internal set; }

        public List<YoloSegmentationHistoricalRemediationAuditImage> Images { get; } = new List<YoloSegmentationHistoricalRemediationAuditImage>();

        public List<string> Errors { get; } = new List<string>();

        public int CandidateImageCount => Images.Count;

        public int CandidateRecordCount => Images.Sum(item => item.Records.Count);

        public int ChangedYoloLabelImageCount => Images.Count(item => string.Equals(item.YoloLabelDiffKind, "Changed", StringComparison.Ordinal));

        public int UnresolvedRecordCount => Images.Sum(item => item.Records.Count(record => !record.CanProposeGeometry));

        public bool HasErrors => Errors.Count > 0 || Images.Any(item => item.Errors.Count > 0);
    }

    public sealed class YoloSegmentationHistoricalRemediationAuditImage
    {
        public string Split { get; internal set; } = string.Empty;

        public string ImageName { get; internal set; } = string.Empty;

        public string ImagePath { get; internal set; } = string.Empty;

        public string SegmentPath { get; internal set; } = string.Empty;

        public string MaskPath { get; internal set; } = string.Empty;

        public string LabelPath { get; internal set; } = string.Empty;

        public string ProposedBackupDirectory { get; internal set; } = string.Empty;

        public List<YoloSegmentationHistoricalRemediationAuditRecord> Records { get; } = new List<YoloSegmentationHistoricalRemediationAuditRecord>();

        public IReadOnlyList<string> ExistingYoloLabelLines { get; internal set; } = Array.Empty<string>();

        public IReadOnlyList<string> ProposedYoloLabelLines { get; internal set; } = Array.Empty<string>();

        public string YoloLabelDiffKind { get; internal set; } = "NotCompared";

        public string YoloLabelDiff { get; internal set; } = string.Empty;

        public List<string> Errors { get; } = new List<string>();
    }

    public sealed class YoloSegmentationHistoricalRemediationAuditRecord
    {
        public int ClassIndex { get; internal set; }

        public string ClassName { get; internal set; } = string.Empty;

        public string OldGeometryType { get; internal set; } = "LegacyUntypedRectangle";

        public int OldPointCount { get; internal set; }

        public string ProposedGeometryType { get; internal set; } = "Unavailable";

        public int ProposedPolygonCount { get; internal set; }

        public int ProposedPointCount { get; internal set; }

        public int MaskPixelCount { get; internal set; }

        public string Error { get; internal set; } = string.Empty;

        public bool CanProposeGeometry => string.IsNullOrWhiteSpace(Error) && ProposedPolygonCount > 0;
    }

    public sealed class YoloSegmentationHistoricalRemediationAuditExportResult
    {
        public string OutputPath { get; internal set; } = string.Empty;

        public int CandidateImageCount { get; internal set; }

        public int CandidateRecordCount { get; internal set; }
    }
}
