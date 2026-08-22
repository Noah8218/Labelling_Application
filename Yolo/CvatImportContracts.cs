using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class CvatDetectionImportResult
    {
        public string ArchivePath { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedImageCount { get; set; }

        public int LabelFileCount { get; set; }

        public int ImportedBoxCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedImageCount { get; set; }

        public int SkippedBoxCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }

    public sealed class CvatSegmentationImportResult
    {
        public string ArchivePath { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedImageCount { get; set; }

        public int ImportedPolygonCount { get; set; }

        public int ImportedSegmentFileCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedImageCount { get; set; }

        public int SkippedPolygonCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }
}
