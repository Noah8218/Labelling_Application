using System.Collections.Generic;

namespace MvcVisionSystem.Yolo
{
    public sealed class LabelStudioDetectionImportResult
    {
        public string TaskJsonPath { get; set; } = string.Empty;

        public string ImageRoot { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedTaskCount { get; set; }

        public int LabelFileCount { get; set; }

        public int ImportedResultCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedTaskCount { get; set; }

        public int SkippedResultCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }

    public sealed class LabelStudioSegmentationImportResult
    {
        public string TaskJsonPath { get; set; } = string.Empty;

        public string ImageRoot { get; set; } = string.Empty;

        public string TargetSplit { get; set; } = string.Empty;

        public int ImportedTaskCount { get; set; }

        public int ImportedResultCount { get; set; }

        public int ImportedSegmentFileCount { get; set; }

        public int CategoryCount { get; set; }

        public int SkippedTaskCount { get; set; }

        public int SkippedResultCount { get; set; }

        public List<string> ImportedImagePaths { get; } = new List<string>();
    }
}
