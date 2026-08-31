using MvcVisionSystem._1._Core;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MvcVisionSystem.Yolo
{
    public sealed class YoloSegmentationTemplateContourMigrationPlan
    {
        public string OutputRootPath { get; internal set; } = string.Empty;

        public string SourceImagePath { get; internal set; } = string.Empty;

        public string SourceSegmentPath { get; internal set; } = string.Empty;

        public string SourceMaskPath { get; internal set; } = string.Empty;

        public string SourceClassName { get; internal set; } = string.Empty;

        public int SourceClassIndex { get; internal set; } = -1;

        public int SourceMaskPixelCount { get; internal set; }

        public string BackupRootPath { get; internal set; } = string.Empty;

        public List<YoloSegmentationTemplateContourMigrationItem> Items { get; } = new List<YoloSegmentationTemplateContourMigrationItem>();

        public List<string> Errors { get; } = new List<string>();

        public bool CanApply => Errors.Count == 0 && Items.Count > 0;

        internal LabelingProjectData Data { get; set; }

        internal byte[] SourceMaskData { get; set; }

        internal Size SourceMaskSize { get; set; }

        internal Rectangle SourceMaskBounds { get; set; }

        internal Dictionary<string, string> SourceArtifactHashes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class YoloSegmentationTemplateContourMigrationItem
    {
        public string Split { get; internal set; } = string.Empty;

        public string FileStem { get; internal set; } = string.Empty;

        public string ImagePath { get; internal set; } = string.Empty;

        public string SegmentPath { get; internal set; } = string.Empty;

        public string MaskPath { get; internal set; } = string.Empty;

        public string LabelPath { get; internal set; } = string.Empty;

        public Rectangle TargetBounds { get; internal set; }

        public int ClassIndex { get; internal set; } = -1;

        public string ClassName { get; internal set; } = string.Empty;

        public int OriginalClassPixelCount { get; internal set; }

        public string SegmentSha256 { get; internal set; } = string.Empty;

        public string MaskSha256 { get; internal set; } = string.Empty;

        public string LabelSha256 { get; internal set; } = string.Empty;
    }

    public sealed class YoloSegmentationTemplateContourMigrationResult
    {
        public string BackupRootPath { get; internal set; } = string.Empty;

        public string ManifestPath { get; internal set; } = string.Empty;

        public int MigratedImageCount { get; internal set; }

        public int MigratedRecordCount { get; internal set; }
    }
}
